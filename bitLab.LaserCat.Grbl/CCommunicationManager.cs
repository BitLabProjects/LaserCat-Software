using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Diagnostics;
using System.Threading;

namespace bitLab.LaserCat.Grbl
{
  public enum ECommands
  {
    //SEND COMMANDS
		INIT_COMMAND = 1,
		RESET_COMMAND = 2,
		SETSETTINGS_COMMAND = 3,
		WAKEUP_COMMAND = 4,
		GOIDLE_COMMAND = 5,
		STOREBLOCK_COMMAND = 6,
		STORESEGMENT_COMMAND = 7,
		ASKPOSITION_COMMAND = 10,
		ASKHASMORESEGMENTBUFFER_COMMAND = 12,

		//RECEIVE COMMANDS
		OK_COMMAND = 8,
		ERROR_COMMAND = 9,
		OKPOSITION_COMMAND = 11,
		OKSEGMENTBUFFER_COMMAND = 13
  }

  class CCommunicationManager
  {
    private const int REPLY_TIMEOUT_MSEC = 0;

    private SerialPort mSerialPort;
    private AutoResetEvent mCommandReceived;

    private byte mLastCommandReceived = 0;
    public byte mLastCommandSent = 0;

    //TODO Move to call stack 
    public Int32 mPositionX;
    public Int32 mPositionY;
    public Int32 mPositionZ;
    public Int32 mSegmentBufferCount;
    public Int32 mHasMoreSegmentBuffer;

    public CCommunicationManager(String portName)
    {
      mCommandReceived = new AutoResetEvent(false);

      var mPortName = portName;
      mSerialPort = new SerialPort(mPortName);
      mSerialPort.BaudRate = 115200;
      mSerialPort.Parity = Parity.None;
      mSerialPort.DataBits = 8;
      mSerialPort.StopBits = StopBits.One;
      mSerialPort.DataReceived += ReceiveFromPIC;
      mSerialPort.Open();

      mReadingState = EReadingState.WaitingFor_StartChar;
      mCharToRead = 0;
      mReceiveBuffer = new Queue<byte>();
    }

    private const byte START_CHAR = 35; //#
    private const byte END_CHAR = 36; //$

    public void Send(ECommands cmd)
    {
      Send(cmd, new List<byte>());
    }

    public void Send(ECommands cmd, List<Byte> data)
    {
      mLastCommandSent = (byte)cmd;
      //Send((new byte[] { (byte)cmd }).Concat(data).ToArray());

      if (mCurrentTxPacketId == 255) mCurrentTxPacketId = 0;
      else mCurrentTxPacketId++;
      if (WriteDebugTxRxInfo)
      {
        Debug.WriteLine("TX Packet id=" + mCurrentTxPacketId);
        Debug.WriteLine("TX Data=" + string.Join(",", data));
      }

      var msg = new CProtocolMessage(mCurrentTxPacketId, cmd, data);
      List<byte> rawMessage = msg.GetRawData();
      rawMessage.Insert(0, START_CHAR);
      rawMessage.Add(END_CHAR);

      Debug.WriteLine("SendToPic, Data=" + string.Join(",", rawMessage));
      mSerialPort.Write(rawMessage.ToArray(), 0, rawMessage.Count);

      Debug.WriteLine("Waiting event");
      mCommandReceived.WaitOne();
      Debug.WriteLine("Waiting completed");
    }

//    public void Send(CProtocolMessage msg)
//    {
//      dynamic bytes = new List<byte>();
//      bytes.Add(START_CHAR); 
//      bytes.Add(msg.ID);
//      bytes.Add(msg.Cmd);
//      bytes.Add(msg.DataLength);
//      bytes.AddRange(msg.Data);
//      bytes.Add(msg.CRC);
//      bytes.Add(END_CHAR);

//#if DEBUG
//      Console.WriteLine("SerialPort TX: " + String.Join(",", bytes));
//#endif
//      mSerialPort.Write(bytes.ToArray(), 0, bytes.Count);
//    }

    //public bool SendAndMatch(CProtocolMessage msg)
    //{
    //  Send(msg);

    //  //TODO Wait for pic response
    //  //TODO Match
    //  return false;
    //}

    private const bool WriteDebugTxRxInfo = false;
    
    private byte mCurrentTxPacketId = 255;
    private byte[] FormatCommandForSend(byte[] data)
    {


      //List<byte> dataToCheck = new List<byte>();
      //dataToCheck.Add(mCurrentTxPacketId);
      //dataToCheck.Add(Convert.ToByte(data.Length));
      //dataToCheck.AddRange(data);
      //var checksum = CheckSum(dataToCheck);

      return null;
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
    List<byte> mBufferToCheck = new List<byte>();
    private Queue<byte> mReceiveBuffer;
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
                mReadingState = EReadingState.WaitingFor_PacketID;
              else
                Logging.Log.LogError("Invalid start char: " + currChar);
            }
            break;
          case EReadingState.WaitingFor_PacketID:
            {
              if (currChar == mCurrentTxPacketId)
                mReadingState = EReadingState.WaitingFor_Length;
              else
              {
                mReadingState = EReadingState.WaitingFor_StartChar;
                Logging.Log.LogError("Invalid RX Packet id: " + currChar);
              }

              if (WriteDebugTxRxInfo)
              {
                Debug.WriteLine("RX Packet id=" + currChar);
              }

              mBufferToCheck.Clear();
              mBufferToCheck.Add(mCurrentTxPacketId);
            }
            break;
          case EReadingState.WaitingFor_Length:
            {
              mCharToRead = currChar;
              mReadingState = EReadingState.Reading_Data;
              mBufferToCheck.Add(currChar);
            }
            break;
          case EReadingState.Reading_Data:
            {
              mCharToRead--;
              mReceiveBuffer.Enqueue(buffer[i]);
              mBufferToCheck.Add(buffer[i]);
              if (mCharToRead == 0)
              {
                mReadingState = EReadingState.WaitingFor_CheckSum;
              }
            }
            break;
          case EReadingState.WaitingFor_CheckSum:
            {
              if (currChar == CheckSum(mBufferToCheck))
                mReadingState = EReadingState.WaitingFor_EndChar;
              else
              {
                mReadingState = EReadingState.WaitingFor_StartChar;
                Logging.Log.LogError("Invalid RX checksum: " + currChar);
              }
            }
            break;
          case EReadingState.WaitingFor_EndChar:
            {
              mReadingState = EReadingState.WaitingFor_StartChar;
              if (currChar == END_CHAR)
              {
                ParseCommand();
                mReceiveBuffer = new Queue<byte>();
              }
              else
                Logging.Log.LogError("Invalid RX End char: " + currChar);
            }
            break;
        }
      }
    }

    private void ParseCommand()
    {
      //TODO
      String message = "";
      mLastCommandReceived = mReceiveBuffer.ElementAt(0);

      if (WriteDebugTxRxInfo)
      {
        Debug.WriteLine("RX Data=" + string.Join(",", mReceiveBuffer));
      }

      if (mLastCommandSent == (byte)ECommands.ASKPOSITION_COMMAND)
      {
        if (mLastCommandReceived == (byte)ECommands.OKPOSITION_COMMAND)
        {
          message = mLastCommandSent + ":OK";
          int index = 1;
          //SB!Extracted and corrected, << has higher precedence than +, () needed
          mPositionX = QueueReadInt32(mReceiveBuffer, ref index);
          mPositionY = QueueReadInt32(mReceiveBuffer, ref index);
          mPositionZ = QueueReadInt32(mReceiveBuffer, ref index);
        }
      }

      if (mLastCommandSent == (byte)ECommands.ASKHASMORESEGMENTBUFFER_COMMAND && mLastCommandReceived == (byte)ECommands.OKSEGMENTBUFFER_COMMAND)
      {
        message = mLastCommandSent + ":OK";
        mHasMoreSegmentBuffer = mReceiveBuffer.ElementAt(1);
      }

      if (mLastCommandReceived == (byte)ECommands.OK_COMMAND) message = mLastCommandSent + ":OK";
      if (mLastCommandReceived == (byte)ECommands.ERROR_COMMAND) message = mLastCommandSent + ":ERRORE";

      Logging.Log.LogInfo(message);
      lock (this)
      {
        Debug.WriteLine("Setting event");
        mCommandReceived.Set();
      }
    }

    private int QueueReadInt32(Queue<byte> queue, ref int index)
    {
      var i = index;
      index += 4;
      return ((int)mReceiveBuffer.ElementAt(i)) +
             ((int)mReceiveBuffer.ElementAt(i + 1) << 8) +
             ((int)mReceiveBuffer.ElementAt(i + 2) << 16) +
             ((int)mReceiveBuffer.ElementAt(i + 3) << 24);
    }
  }
}
