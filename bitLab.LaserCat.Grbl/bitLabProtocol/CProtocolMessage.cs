﻿using System;
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

    public CProtocolMessage(byte ID, ECommands cmd, List<Byte> data)
    {
      this.ID = ID;
      this.Cmd = cmd;
      if (data == null)
        data = new List<byte>();
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

    public bool VerifyCRC(out byte correctCRC)
    {
      correctCRC = GetCalculatedCrc();
      return (CRC == correctCRC);
    }

    private byte GetCalculatedCrc()
    {
      var CRC = (byte)(ID ^ (DataLength + 1) ^ (byte)Cmd);
      foreach (byte b in Data)
      {
        CRC = (byte)(CRC ^ b);
      }
      return CRC;
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
