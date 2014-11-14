using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.ViewModel
{

  [Description("Monitors a collection of items that expose an ISelectable interface and maintains the list of selected items")]
  public class CSelectionVM<T> : CBaseVM where T : ISelectable
  {
    public event Action<object, CItemSelectionChangedEventArgs> ItemSelectionChanged;

    private ObservableCollection<T> mItemsCollection;
    private ObservableCollection<T> mSelectedItems;
    private HashSet<T> mAttachedItems;

    public CSelectionVM(ObservableCollection<T> collection)
    {
      mItemsCollection = collection;
      mItemsCollection.CollectionChanged += mCollection_CollectionChanged;

      mSelectedItems = new ObservableCollection<T>();
      mAttachedItems = new HashSet<T>();

      foreach (var item in mItemsCollection)
        Attach(item);
    }

    public ObservableCollection<T> SelectedItems { get { return mSelectedItems; } }

    public void Clear()
    {
      foreach (var shape in mSelectedItems.ToList())
      {
        shape.IsSelected = false;
      }
    }

    void mCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      Dispatcher.Invoke(() =>
      {
        switch (e.Action)
        {
          case NotifyCollectionChangedAction.Add:
            foreach (T item in e.NewItems)
            {
              mAttachedItems.Add(item);
              Attach(item);
            }
            break;

          case NotifyCollectionChangedAction.Remove:
            foreach (T item in e.OldItems)
            {
              mAttachedItems.Remove(item);
              Detach(item);
            }
            break;

          case NotifyCollectionChangedAction.Reset:
            foreach (T item in mAttachedItems)
              Detach(item);
            mAttachedItems.Clear();
            break;

          default:
            throw new NotSupportedException("Value of NotifyCollectionChangedAction is unhandled");
        }
      });
    }

    void item_SelectionChanged(object sender, EventArgs e)
    {
      var item = (T)sender;
      if (item.IsSelected)
      {
        mSelectedItems.Add(item);
        RaiseItemSelectionChanged(item, true);
      }
      else
      {
        mSelectedItems.Remove(item);
        RaiseItemSelectionChanged(item, false);
      }
    }

    private void RaiseItemSelectionChanged(ISelectable item, bool newStateIsSelected)
    {
      if (ItemSelectionChanged != null)
        ItemSelectionChanged(this, new CItemSelectionChangedEventArgs(item, newStateIsSelected));
    }

    #region Attach-Detach
    private void Attach(T item)
    {
      item.SelectionChanged += item_SelectionChanged;
      mAttachedItems.Add(item);
    }

    private void Detach(T item)
    {
      item.SelectionChanged -= item_SelectionChanged;
    }
    #endregion
  }
}
