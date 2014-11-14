using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace bitLab.ViewModel
{
  public class CBaseVM : INotifyPropertyChanged
  {
    private static Dispatcher mDispatcher;
    public static Dispatcher Dispatcher
    {
      get { return mDispatcher; }
      set { mDispatcher = value; }
    }

    protected void Invoke(Action target)
    {
      if (mDispatcher == null)
        target();
      else
        mDispatcher.Invoke(target);
    }

    protected void BeginInvoke(Action target)
    {
      if (mDispatcher == null)
        target();
      else
        mDispatcher.BeginInvoke(target);
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void Notify(string propertyName)
    {
      if (mDispatcher != null)
        mDispatcher.BeginInvoke(new Action(() => NotifyDo(propertyName)));
      else
        NotifyDo(propertyName);
    }

    private void NotifyDo(string propertyName)
    {
      if (PropertyChanged != null)
        PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected void SetAndNotify<T>(ref T field, T value, [CallerMemberName] string property = "")
    {
      if (!object.Equals(field, value))
      {
        field = value;
        Notify(property);
      }
    }
  }
}
