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
		INIT_COMMAND = 1, //Ok
		RESET_COMMAND = 2, //Ok
		SETSETTINGS_COMMAND = 3, //Ok
		WAKEUP_COMMAND = 4, //Ok
		GOIDLE_COMMAND = 5, //Ok
		STOREBLOCK_COMMAND = 6, //Ok
		STORESEGMENT_COMMAND = 7, //Ok
		ASKPOSITION_COMMAND = 10, //OKPOSITION_COMMAND
		ASKHASMORESEGMENTBUFFER_COMMAND = 12, //OKSEGMENTBUFFER_COMMAND

		//RECEIVE COMMANDS
		OK_COMMAND = 8,
		ERROR_COMMAND = 9,
		OKPOSITION_COMMAND = 11,
		OKSEGMENTBUFFER_COMMAND = 13
	}

	class CCommunicationManager
	{
		private const int REPLY_TIMEOUT_MSEC = 0;
		private const bool WriteDebugTxRxInfo = false;

		private SerialPort mSerialPort;

		private byte mLastCommandReceived = 0;
		public byte mLastCommandSent = 0;

		private byte mCurrentTxPacketId = 255;

		//TODO Move to call stack 
		public Int32 mPositionX;
		public Int32 mPositionY;
		public Int32 mPositionZ;
		public Int32 mSegmentBufferCount;
		public Int32 mHasMoreSegmentBuffer;

    private string mPortName;

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

		public void Send(ECommands cmd)
		{
			Send(cmd, new List<byte>());
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
			for (int i = 1; i <= 10; i++)
			{
				mSendDo(txMsg);
        CProtocolMessage rxMsg = Read();
        byte correctCRC;
        if (rxMsg.ID != mCurrentTxPacketId)
          Logging.Log.LogError("Received invalid packet ID, desired " + mCurrentTxPacketId + ", received " + rxMsg.ID);
        else if (!rxMsg.VerifyCRC(out correctCRC))
          Logging.Log.LogError("Received invalid packet CRC, desired " + correctCRC + ", received " + rxMsg.CRC);
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
      mMaybeOpenPort();
			mLastCommandSent = (byte)msg.Cmd;

			if (WriteDebugTxRxInfo)
			{
				Debug.WriteLine("TX Packet id=" + mCurrentTxPacketId);
				Debug.WriteLine("TX Data=" + string.Join(",", msg.Data));
			}

			List<byte> rawMessage = msg.GetRawData();
			rawMessage.Insert(0, START_CHAR);
			rawMessage.Add(END_CHAR);

			Debug.WriteLine("SendToPic, Data=" + string.Join(",", rawMessage));
			mSerialPort.Write(rawMessage.ToArray(), 0, rawMessage.Count);
		}

    private void mMaybeOpenPort()
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
								mReadingState = EReadingState.WaitingFor_PacketID;
							else
								Logging.Log.LogError("Invalid start char: " + currChar);
						}
						break;
					case EReadingState.WaitingFor_PacketID:
						{
							mRxPacketId = currChar;
							mReadingState = EReadingState.WaitingFor_Length;

							//if (currChar == mCurrentTxPacketId)
							//	mReadingState = EReadingState.WaitingFor_Length;
							//else
							//{
							//	mReadingState = EReadingState.WaitingFor_StartChar;
							//	Logging.Log.LogError("Invalid RX Packet id: " + currChar);
							//}

							if (WriteDebugTxRxInfo)
							{
								Debug.WriteLine("RX Packet id=" + currChar);
							}
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

							//if (currChar == CheckSum(mBufferToCheck))
							//	mReadingState = EReadingState.WaitingFor_EndChar;
							//else
							//{
							//	mReadingState = EReadingState.WaitingFor_StartChar;
							//	Logging.Log.LogError("Invalid RX checksum: " + currChar);
							//}
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
			Debug.WriteLine("Adding Protocol Message to queue");
			mRxMessageBuffer.Add(new CProtocolMessage(mRxPacketId, (ECommands)mReceiveBuffer.ElementAt(0), mReceiveBuffer.Skip(1).ToList()));
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
