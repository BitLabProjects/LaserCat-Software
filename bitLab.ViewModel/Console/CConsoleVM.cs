using bitLab.Math;
using bitLab.Logging;
using bitLab.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace bitLab.ViewModel.Console
{
  public class CConsoleVM: CBaseVM, ILogListener
  {
    public CConsoleVM()
    {
      Lines = new ObservableCollection<CConsoleLineVM>();
      Log.Register(this);
    }

    public ObservableCollection<CConsoleLineVM> Lines { get; private set; }

    public void LogMessage(LogMessage message)
    {
      AddConsoleLine(new CConsoleLineVM(message.Message, CColors.Orange));
    }

    private void AddConsoleLine(CConsoleLineVM line)
    {
      Invoke(() => Lines.Add(line));
      RemoveLineWithDelay(line, 5000);
    }

    private void RemoveLineWithDelay(CConsoleLineVM line, int delayMillisec)
    {
      Task.Factory.StartNew(() =>
      {
        Thread.Sleep(delayMillisec);
        Invoke(() => Lines.Remove(line));
      });
    }
  }
}
