using bitLab.Logging;
using bitLab.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.ViewModel.Console
{
  public class CConsoleLineVM
  {
    private LogMessage mMessage;

    internal CConsoleLineVM(LogMessage message)
    {
      mMessage = message;
    }

    public string Text { get { return mMessage.Text; } }
    public ELogMessageType Type { get { return mMessage.Type; } }
    public DateTime Date { get { return mMessage.Date; } }
    public CColor Color
    {
      get
      {
        switch (mMessage.Type)
        {
          case ELogMessageType.Info: return CColors.SteelBlue;
          case ELogMessageType.Warning: return CColors.Orange;
          case ELogMessageType.Error: return CColors.Red;
          default: return CColors.Black;
        }
      }
    }
  }
}
