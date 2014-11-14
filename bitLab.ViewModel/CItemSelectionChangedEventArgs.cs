using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bitLab.ViewModel
{
  public class CItemSelectionChangedEventArgs : EventArgs
  {
    public CItemSelectionChangedEventArgs(ISelectable item, bool newStateIsSelected)
    {
      Item = item;
      NewStateIsSelected = newStateIsSelected;
    }

    public ISelectable Item { get; private set; }
    public bool NewStateIsSelected { get; private set; }
  }
}
