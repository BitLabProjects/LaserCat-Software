using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.Logging
{
  public class Log
  {
    private static Log mInstance;
    
    private List<ILogListener> mListeners;

    private Log()
    {
      mListeners = new List<ILogListener>();
    }

    private static Log Instance
    {
      get
      {
        if (mInstance == null)
          mInstance = new Log();
        return mInstance;
      }
    }

    public static void Register(ILogListener listener)
    {
      Instance.mListeners.Add(listener);
    }

    public static void LogInfo(string message)
    {
      DispatchLogMessage(new LogMessage(DateTime.Now, message, ELogMessageType.Info));
    }

    public static void LogError(string message)
    {
      DispatchLogMessage(new LogMessage(DateTime.Now, message, ELogMessageType.Info));
    }

    private static void DispatchLogMessage(LogMessage message)
    {
      foreach (var listener in Instance.mListeners)
        listener.LogMessage(message);
    }
  }
}
