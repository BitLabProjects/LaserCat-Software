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
    public ECommands Cmd;
    public byte DataLength;
    public List<Byte> Data;
    public byte CRC;

    public const byte MSG_PING = 60;
    public const byte MSG_PONG = 70;

    public CProtocolMessage(byte ID, ECommands cmd, List<Byte> data)
    {
      this.ID = ID;
      this.Cmd = cmd;
      this.Data = data;
      this.DataLength = (byte)data.Count;
      CalculateCrc();
    }

    private void CalculateCrc()
    {
      CRC = (byte)(ID ^ (DataLength+1) ^ (byte)Cmd);
      foreach (byte b in Data)
      {
        CRC = (byte)(CRC ^ b);
      }
    }

    internal List<byte> GetRawData()
    {
      List<byte> dataToSend = new List<byte>();
      dataToSend.Add(ID);
      dataToSend.Add((byte)(DataLength+1));
      dataToSend.Add((byte)Cmd);
      dataToSend.AddRange(Data);
      dataToSend.Add(CRC);
      return dataToSend;
    }
  }
}
