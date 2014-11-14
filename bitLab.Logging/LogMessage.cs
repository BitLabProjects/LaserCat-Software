using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.Logging
{
  public class LogMessage
  {
    private DateTime mDate;
    private string mText;
    private ELogMessageType mType;

    internal LogMessage(DateTime date, string text, ELogMessageType type)
    {
      mDate = date;
      mText = text;
      mType = type;
    }

    public DateTime Date
    {
      get { return mDate; }
    }

    public string Text
    {
      get { return mText; }
    }

    public ELogMessageType Type
    {
      get { return mType; }
    }
  }
}
