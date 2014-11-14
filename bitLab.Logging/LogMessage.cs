using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.Logging
{
  public class LogMessage
  {
    private string mMessage;
    private ELogMessageType mType;

    internal LogMessage(string message, ELogMessageType type)
    {
      mMessage = message;
      mType = type;
    }

    public string Message
    {
      get { return mMessage; }
    }

    public ELogMessageType Type
    {
      get { return mType; }
    }
  }
}
