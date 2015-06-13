using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Diagnostics;
using System.Threading;
using System.Collections.Concurrent;

namespace bitLab.LaserCat.Grbl
{
  public enum ECommands
  {
    //SEND COMMANDS
    CONNECT_COMMAND = 1, //Ok
    RESET_COMMAND = 2, //Ok
    SETSETTINGS_COMMAND = 3, //Ok
    WAKEUP_COMMAND = 4, //Ok
    GOIDLE_COMMAND = 5, //Ok
    STOREBLOCK_COMMAND = 6, //Ok
    STORESEGMENT_COMMAND = 7, //Ok
    ASKPOSITION_COMMAND = 10, //OKPOSITION_COMMAND
    ASKHASMORESEGMENTBUFFER_COMMAND = 12, //OKSEGMENTBUFFER_COMMAND
		SETSPEED_COMMAND = 15,
		MANUALSTEP_COMMAND = 16,

    //RECEIVE COMMANDS
    OK_COMMAND = 8,
    ERROR_COMMAND = 9,
    OKPOSITION_COMMAND = 11,
    OKSEGMENTBUFFER_COMMAND = 13,
    OKSTORESEGMENT_COMMAND = 14
  }

  class CCommunicationManager
  {
    private const int REPLY_TIMEOUT_MSEC = 0;

    private string mPortName;
    private SerialPort mSerialPort;
    private byte mCurrentTxPacketId = 255;

    private BlockingCollection<CProtocolMessage> mRxMessageBuffer;

    public CCommunicationManager(String portName)
    {
      mRxMessageBuffer = new BlockingCollection<CProtocolMessage>();

      mPortName = portName;

      mReadingState = EReadingState.WaitingFor_StartChar;
      mCharToRead = 0;
      mReceiveBuffer = new Queue<byte>();
    }

    private const byte START_CHAR = 35; //#
    private const byte END_CHAR = 36; //$

    public void OpenPort()
    {
      if (mSerialPort == null)
      {
        mSerialPort = new SerialPort(mPortName);
        mSerialPort.BaudRate = 115200;
        mSerialPort.Parity = Parity.None;
        mSerialPort.DataBits = 8;
        mSerialPort.StopBits = StopBits.One;
        mSerialPort.DataReceived += ReceiveFromPIC;
        mSerialPort.Open();
      }
      else
      {
        Logging.Log.LogError("Port is already open");
      }
    }

    public void Send(ECommands cmd)
    {
      Send(cmd, null);
    }

    //TODO Inline on callers
    public void Send(ECommands cmd, List<Byte> data)
    {
      SendAndMatch(cmd, data, ECommands.OK_COMMAND);
    }

    public void SendAndMatch(ECommands cmd, List<Byte> data, ECommands matchCmd)
    {
      List<Byte> readData;
      SendAndRead(cmd, data, matchCmd, out readData);
    }

    public bool SendAndRead(ECommands cmd, List<Byte> data, ECommands matchCmd, out List<Byte> matchData)
    {
      var txMsg = new CProtocolMessage(mGetNextTxPacketId(), cmd, data);

      byte expectedResponsePacketId;
      //The machine always responds with a packet Ok with id 255 upon reset
      if (cmd == ECommands.RESET_COMMAND)
      {
        mCurrentTxPacketId = 255;
        expectedResponsePacketId = 255;
      }
      else
        expectedResponsePacketId = mCurrentTxPacketId;

      for (int i = 1; i <= 10; i++)
      {
        mSendDo(txMsg);
        CProtocolMessage rxMsg = Read();
        byte correctCRC;
        if (rxMsg.ID != expectedResponsePacketId)
          Logging.Log.LogError("Received invalid packet ID, desired " + expectedResponsePacketId + ", received " + rxMsg.ID);
        else if (!rxMsg.VerifyCRC(out correctCRC))
          Logging.Log.LogError("Received invalid packet CRC, desired " + correctCRC + ", received " + rxMsg.CRC);
        else if (rxMsg.Cmd != matchCmd)
          Logging.Log.LogError("Received invalid response command, desired " + matchCmd.ToString() + ", received " + rxMsg.Cmd.ToString());
        else
        {
          matchData = rxMsg.Data;
          return true;
        }
      }

      matchData = null;
      return false;
    }

    private CProtocolMessage Read()
    {
      Debug.WriteLine("Waiting event");
      var msg = mRxMessageBuffer.Take();
      Debug.WriteLine("Waiting completed");
      return msg;
    }

    private byte mGetNextTxPacketId()
    {
      if (mCurrentTxPacketId == 255) mCurrentTxPacketId = 0;
      else mCurrentTxPacketId++;
      return mCurrentTxPacketId;
    }

    private void mSendDo(CProtocolMessage msg)
    {
      mCheckPortIsOpen();

      List<byte> rawMessage = msg.GetRawData();
      rawMessage.Insert(0, START_CHAR);
      rawMessage.Add(END_CHAR);

      Debug.WriteLine("SendToPic, Data=" + string.Join(",", rawMessage));
      mSerialPort.Write(rawMessage.ToArray(), 0, rawMessage.Count);
    }

    private void mCheckPortIsOpen()
    {
      if (mSerialPort == null)
      {
        throw new InvalidOperationException("Port not open");
      }
    }

    private byte CheckSum(List<Byte> data)
    {
      byte sum = 0;
      foreach (byte d in data)
      {
        sum = (byte)(sum ^ d);
      }
      return sum;
    }

    private enum EReadingState
    {
      WaitingFor_StartChar,
      WaitingFor_PacketID,
      WaitingFor_Length,
      Reading_Data,
      WaitingFor_CheckSum,
      WaitingFor_EndChar,
    };
    private EReadingState mReadingState;
    private int mCharToRead;
    private Queue<byte> mReceiveBuffer;

    private byte mRxPacketId;
    private byte mRxChecksum;

    private void ReceiveFromPIC(object sender, SerialDataReceivedEventArgs e)
    {
      SerialPort sp = (SerialPort)sender;
      int bufferLength = sp.BytesToRead;
      byte[] buffer = new byte[bufferLength];
      sp.Read(buffer, 0, bufferLength);

      Debug.WriteLine("ReceiveFromPic, data=" + string.Join(",", buffer));

      int i;
      for (i = 0; i < bufferLength; i++)
      {
        byte currChar = buffer[i];

        switch (mReadingState)
        {
          case EReadingState.WaitingFor_StartChar:
            {
              if (currChar == START_CHAR)
              {
                mReadingState = EReadingState.WaitingFor_PacketID;
                mReceiveBuffer = new Queue<byte>();
              }
              else
                Logging.Log.LogError("Invalid start char: " + currChar);
            }
            break;
          case EReadingState.WaitingFor_PacketID:
            {
              mRxPacketId = currChar;
              mReadingState = EReadingState.WaitingFor_Length;
            }
            break;
          case EReadingState.WaitingFor_Length:
            {
              mCharToRead = currChar;
              mReadingState = EReadingState.Reading_Data;
            }
            break;
          case EReadingState.Reading_Data:
            {
              mCharToRead--;
              mReceiveBuffer.Enqueue(buffer[i]);
              if (mCharToRead == 0)
              {
                mReadingState = EReadingState.WaitingFor_CheckSum;
              }
            }
            break;
          case EReadingState.WaitingFor_CheckSum:
            {
              mRxChecksum = currChar;
              mReadingState = EReadingState.WaitingFor_EndChar;
            }
            break;
          case EReadingState.WaitingFor_EndChar:
            {
              mReadingState = EReadingState.WaitingFor_StartChar;
              if (currChar == END_CHAR)
              {
                mRxMessageBuffer.Add(new CProtocolMessage(mRxPacketId, (ECommands)mReceiveBuffer.ElementAt(0), mReceiveBuffer.Skip(1).ToList()));
              }
              else
                Logging.Log.LogError("Invalid RX End char: " + currChar);
            }
            break;
        }
      }
    }
  }
}
