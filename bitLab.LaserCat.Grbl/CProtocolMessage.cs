using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.LaserCat.Grbl
{
  public class CProtocolMessage
  {
    //Content
    public byte ID;
    public byte Cmd;
    public byte DataLength;
    public byte[] Data;
    public byte CRC;

    public const byte MSG_PING = 60;
    public const byte MSG_PONG = 70;

    public static CProtocolMessage CreatePingMessage(byte id)
    {
      var msg = new CProtocolMessage();
      msg.ID = id;
      msg.DataLength = 6;
      msg.Data = new byte[] { MSG_PING, 44, 55, 66, 77, 88 };
      msg.CalculateCrc();
      return msg;
    }

    private void CalculateCrc()
    {
      CRC = (byte)(ID ^ DataLength);
      foreach (byte b in Data)
      {
        CRC = (byte)(CRC ^ b);
      }
    }
  }
}
