using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Diagnostics;

namespace bitLab.LaserCat.Grbl
{
	public class CLaserCatHardwarePIC : ILaserCatHardware
	{
		//SEND COMMANDS
		private const byte INIT_COMMAND = 1;
		private const byte RESET_COMMAND = 2;
		private const byte SETSETTINGS_COMMAND = 3;
		private const byte WAKEUP_COMMAND = 4;
		private const byte GOIDLE_COMMAND = 5;
		private const byte STOREBLOCK_COMMAND = 6;
		private const byte STORESEGMENT_COMMAND = 7;
		private const byte ASKPOSITION_COMMAND = 10;
		private const byte ASKHASMORESEGMENTBUFFER_COMMAND = 12;

		//RECEIVE COMMANDS
		private const byte OK_COMMAND = 8;
		private const byte ERROR_COMMAND = 9;
		private const byte OKPOSITION_COMMAND = 11;
		private const byte OKSEGMENTBUFFER_COMMAND = 13;

		private const int REPLY_TIMEOUT_MSEC = 0;

		private SerialPort mSerialPort;
		private String mPortName;

		private Queue<byte> mReceiveBuffer;
		List<byte> mBufferToCheck = new List<byte>();

		private const byte START_CHAR = 35;
		private const byte END_CHAR = 36;
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

		private byte mLastCommandReceived = 0;
		private byte mLastCommandSent = 0;
		private byte mCurrentTxPacketId = 255;
		private Int32 mPositionX;
		private Int32 mPositionY;
		private Int32 mPositionZ;
    private Int32 mSegmentBufferCount;
		private int mHasMoreSegmentBuffer;

		private AutoResetEvent CommandReceived;

		public CLaserCatHardwarePIC(string portName)
		{
			CommandReceived = new AutoResetEvent(false);
			mReadingState = EReadingState.WaitingFor_StartChar;
			mCharToRead = 0;
			mPortName = portName;
			mReceiveBuffer = new Queue<byte>();
			mSerialPort = new SerialPort(mPortName);
			mSerialPort.BaudRate = 115200;
			mSerialPort.Parity = Parity.None;
			mSerialPort.DataBits = 8;
			mSerialPort.StopBits = StopBits.One;
			mSerialPort.DataReceived += ReceiveFromPIC;
		}

		public void Init()
		{
      mSerialPort.Open();

			mLastCommandSent = INIT_COMMAND;
			byte[] data = { mLastCommandSent };
			SendToPIC(data);
		}

		public void Reset()
		{
			mLastCommandSent = RESET_COMMAND;
			byte[] data = { mLastCommandSent };
			SendToPIC(data);
		}

		public void SetSettings(LaserCatSettings settings)
		{
			mLastCommandSent = SETSETTINGS_COMMAND;
			byte[] data = { mLastCommandSent,
                      settings.pulse_microseconds,
                      settings.step_invert_mask,
                      settings.dir_invert_mask, 
                      settings.stepper_idle_lock_time,
                      settings.flags, 
                      settings.step_port_invert_mask,
                      settings.dir_port_invert_mask};
			SendToPIC(data);
		}

		public void WakeUp(bool setupAndEnableMotors)
		{
			mLastCommandSent = WAKEUP_COMMAND;
			byte[] data = { mLastCommandSent, Convert.ToByte(setupAndEnableMotors) };
			SendToPIC(data);
		}

		public void GoIdle(bool delayAndDisableSteppers)
		{
			mLastCommandSent = GOIDLE_COMMAND;
			byte[] data = { mLastCommandSent, Convert.ToByte(delayAndDisableSteppers) };
			SendToPIC(data);
		}

		public void StorePlannerBlock(byte blockIndex, st_block_t block)
		{
			mLastCommandSent = STOREBLOCK_COMMAND;

			List<byte> dataList = new List<byte>();

			dataList.Add(mLastCommandSent);
			dataList.Add(blockIndex);
			dataList.Add(block.direction_bits);

			int i, j;
			for (i = 0; i <= 2; i++)
			{
				for (j = 0; j <= 3; j++)
				{
					dataList.Add(mGetSubByteByIndex(Convert.ToInt32(block.steps[i]), j));
				}
			}

			for (j = 0; j <= 3; j++)
			{
				dataList.Add(mGetSubByteByIndex(Convert.ToInt32(block.step_event_count), j));
			}

			SendToPIC(dataList.ToArray());
		}

		public int AskHasMoreSegmentBuffer()
		{
			mLastCommandSent = ASKHASMORESEGMENTBUFFER_COMMAND;
			byte[] data = { mLastCommandSent };
			Debug.WriteLine("AskHasMoreSegmentBuffer");
			SendToPIC(data);
      Debug.WriteLine("AskHasMoreSegmentBuffer=" + mHasMoreSegmentBuffer);
			return mHasMoreSegmentBuffer;
		}

		public void StoreSegment(segment_t segment)
		{
			mLastCommandSent = STORESEGMENT_COMMAND;

			byte n_stepHI = mGetSubByteByIndex(Convert.ToInt32(segment.n_step), 1);
			byte n_stepLO = mGetSubByteByIndex(Convert.ToInt32(segment.n_step), 0);

      Debug.WriteLine("StoreSegment, cycles_per_tick={0}, stepIdx={1}, steps={2}", segment.cycles_per_tick, segment.st_block_index, segment.n_step);
			byte cycles_per_tickHI = mGetSubByteByIndex(Convert.ToInt32(segment.cycles_per_tick), 1);
			byte cycles_per_tickLO = mGetSubByteByIndex(Convert.ToInt32(segment.cycles_per_tick), 0);

			byte[] data = { mLastCommandSent,
                      n_stepLO, n_stepHI,
                      segment.st_block_index,
                      cycles_per_tickLO, cycles_per_tickHI, 
                      segment.amass_level,
                      segment.prescaler};
			Debug.WriteLine("StoreSegment");
			SendToPIC(data);
		}

    public int GetSegmentBufferCount()
    {
      return mSegmentBufferCount;
    }

		public Int32[] AskPosition()
		{
			mLastCommandSent = ASKPOSITION_COMMAND;
			byte[] data = { mLastCommandSent };
			Debug.WriteLine("AskPosition");
			SendToPIC(data);
			Int32[] position = { mPositionX, mPositionY, mPositionZ };
			return position;
		}

		private void SendToPIC(byte[] data)
		{
			var dataToSend = FormatCommandForSend(data);
      Debug.WriteLine("SendToPic, Data=" + string.Join(",", dataToSend));
			mSerialPort.Write(dataToSend, 0, dataToSend.Length);

      Debug.WriteLine("Waiting event");
			CommandReceived.WaitOne();
      Debug.WriteLine("Waiting completed");
		}

    private const bool WriteDebugTxRxInfo = false;

		private byte[] FormatCommandForSend(byte[] data)
		{
			if (mCurrentTxPacketId == 255) mCurrentTxPacketId = 0;
			else mCurrentTxPacketId++;

      if (WriteDebugTxRxInfo) {
        Debug.WriteLine("TX Packet id=" + mCurrentTxPacketId);
        Debug.WriteLine("TX Data=" + string.Join(",", data));
      }

			List<byte> dataToCheck = new List<byte>();
			dataToCheck.Add(mCurrentTxPacketId);
			dataToCheck.Add(Convert.ToByte(data.Length));
			dataToCheck.AddRange(data);
      var checksum = CheckSum(dataToCheck);

			List<byte> dataToSend = new List<byte>();
			dataToSend.Add(START_CHAR);
			dataToSend.Add(mCurrentTxPacketId);
			dataToSend.Add(Convert.ToByte(data.Length));
			dataToSend.AddRange(data);
      dataToSend.Add(checksum);
      dataToSend.Add(END_CHAR);

			return dataToSend.ToArray();
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

              if (WriteDebugTxRxInfo) {
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

      if (WriteDebugTxRxInfo) {
        Debug.WriteLine("RX Data=" + string.Join(",", mReceiveBuffer));
      }

			if (mLastCommandSent == ASKPOSITION_COMMAND)
			{
				if (mLastCommandReceived == OKPOSITION_COMMAND)
				{
					message = mLastCommandSent + ":OK";
          int index = 1;
          //SB!Extracted and corrected, << has higher precedence than +, () needed
          mPositionX = QueueReadInt32(mReceiveBuffer, ref index);
          mPositionY = QueueReadInt32(mReceiveBuffer, ref index);
          mPositionZ = QueueReadInt32(mReceiveBuffer, ref index);
				}
			}

			if (mLastCommandSent == ASKHASMORESEGMENTBUFFER_COMMAND && mLastCommandReceived == OKSEGMENTBUFFER_COMMAND)
			{
				message = mLastCommandSent + ":OK";
				mHasMoreSegmentBuffer = mReceiveBuffer.ElementAt(1);
			}

			if (mLastCommandReceived == OK_COMMAND) message = mLastCommandSent + ":OK";
			if (mLastCommandReceived == ERROR_COMMAND) message = mLastCommandSent + ":ERRORE";

			Logging.Log.LogInfo(message);
      lock (this)
      {
        Debug.WriteLine("Setting event");
			  CommandReceived.Set();
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

		private byte mGetSubByteByIndex(int param, int index)
		{
			byte subByte = (byte)((param & (255 << 8 * index)) >> 8 * index);
			return subByte;
		}

    public bool Connect(string COMPort)
    {
			return true;
    }
  }
}



/*

AskHasMoreSegmentBuffer
TX Packet id=124
TX Data=12
RX Packet id=124
RX Data=13,1
StoreSegment
TX Packet id=125
TX Data=7,88,0,1,42,20,3,0
RX Packet id=125
RX Data=8
AskHasMoreSegmentBuffer
TX Packet id=126
TX Data=12
RX Packet id=126
RX Data=8
RX Packet id=126
StoreSegment
TX Packet id=127
TX Data=7,96,0,1,149,20,3,0
RX Data=13,1
AskHasMoreSegmentBuffer
TX Packet id=128


*/