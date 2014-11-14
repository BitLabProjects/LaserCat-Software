using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace bitLab.ViewModel
{
  public class CDelegateCommand: ICommand
  {
    private Action<object> mAction;

    public CDelegateCommand(Action<object> action)
    {
      mAction = action;
    }

    public bool CanExecute(object parameter)
    {
      return true;
    }

    public event EventHandler CanExecuteChanged;

    public void Execute(object parameter)
    {
      mAction(parameter);
    }
  }
}
