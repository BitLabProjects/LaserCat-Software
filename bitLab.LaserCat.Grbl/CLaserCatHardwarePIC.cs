using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Ports;

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

		//RECEIVE COMMANDS
		private const byte OK_COMMAND = 8;
		private const byte ERROR_COMMAND = 9;

		private const int REPLY_TIMEOUT_MSEC = 1000;

		private SerialPort mSerialPort;
		private String mPortName;

		private Queue<byte> mReceiveBuffer;

		private const byte START_CHAR = 35;
		private enum EReadingState { WaitingForStartChar, WaitingForReadLength, Reading };
		private EReadingState mReadingState;
		private int mCharToRead;

		private byte mLastCommandReceived = 0;
		private byte mLastCommandSent = 0;
		private AutoResetEvent CommandReceived;

		public event EventHandler CommandParsed;
		private void RaiseCommandParsed()
		{
			if (CommandParsed != null)
				CommandParsed(this, EventArgs.Empty);
		}

		public CLaserCatHardwarePIC(string portName)
		{
			CommandReceived = new AutoResetEvent(false);
			mReadingState = EReadingState.WaitingForStartChar;
			mCharToRead = 0;
			mPortName = portName;
			mReceiveBuffer = new Queue<byte>();
			mSerialPort = new SerialPort(mPortName);
			mSerialPort.BaudRate = 115200;
			mSerialPort.Parity = Parity.None;
			mSerialPort.DataBits = 8;
			mSerialPort.StopBits = StopBits.One;
			mSerialPort.DataReceived += ReceiveFromPIC;
			mSerialPort.Open();
		}

		public void Init()
		{
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
			//TODO
			mLastCommandSent = WAKEUP_COMMAND;
			byte[] data = { mLastCommandSent, Convert.ToByte(setupAndEnableMotors) };
			SendToPIC(data);
		}

		public void GoIdle(bool delayAndDisableSteppers)
		{
			//TODO
			mLastCommandSent = GOIDLE_COMMAND;
			byte[] data = { mLastCommandSent, Convert.ToByte(delayAndDisableSteppers) };
			SendToPIC(data);
		}

		public void StorePlannerBlock(byte blockIndex, st_block_t block)
		{
			//TODO
			mLastCommandSent = STOREBLOCK_COMMAND;

			List<byte> dataList = new List<byte>();

			dataList.Add(mLastCommandSent);
			dataList.Add(blockIndex);
			dataList.Add(block.direction_bits);

			int i,j;
			for (i = 0; i <= 2; i++)
			{
				for (j = 0; j <= 3;j++)
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

		public bool GetHasMoreSegmentBuffer()
		{
			//TODO
			return true;
		}

		public void StoreSegment(segment_t segment)
		{
			//TODO
			mLastCommandSent = SETSETTINGS_COMMAND;

			byte n_stepHI = (byte)((segment.n_step & (255<<8))>>8);
			byte n_stepLO = (byte)(segment.n_step & 255);

			byte cycles_per_tickHI = (byte)((segment.cycles_per_tick & (255 << 8)) >> 8);
			byte cycles_per_tickLO = (byte)(segment.cycles_per_tick & 255);

			byte[] data = { mLastCommandSent,
                      n_stepHI, n_stepLO,
                      segment.st_block_index,
                      cycles_per_tickHI, cycles_per_tickLO, 
                      segment.amass_level,
                      segment.prescaler};
			SendToPIC(data);
		}

		private void SendToPIC(byte[] data)
		{
			var dataToSend = FormatCommandForSend(data);
			mSerialPort.Write(dataToSend, 0, dataToSend.Length);
			CommandReceived.WaitOne(REPLY_TIMEOUT_MSEC);
		}

		private byte[] FormatCommandForSend(byte[] data)
		{
			List<byte> dataToSend = new List<byte>();
			dataToSend.Add(START_CHAR);
			dataToSend.Add(Convert.ToByte(data.Length));
			dataToSend.AddRange(data);
			return dataToSend.ToArray();
		}

		private void ReceiveFromPIC(object sender, SerialDataReceivedEventArgs e)
		{
			SerialPort sp = (SerialPort)sender;
			int bufferLength = sp.BytesToRead;
			byte[] buffer = new byte[bufferLength];
			sp.Read(buffer, 0, bufferLength);

			int i;
			for (i = 0; i < bufferLength; i++)
			{
				char currChar = Convert.ToChar(buffer[i]);

				switch (mReadingState)
				{
					case EReadingState.WaitingForStartChar:
						{
							if (currChar == START_CHAR) mReadingState = EReadingState.WaitingForReadLength;
						}
						break;
					case EReadingState.WaitingForReadLength:
						{
							mCharToRead = Convert.ToInt32(currChar);
							mReadingState = EReadingState.Reading;
						}
						break;
					case EReadingState.Reading:
						{
							mCharToRead--;
							mReceiveBuffer.Enqueue(buffer[i]);
							if (mCharToRead == 0)
							{
								mReadingState = EReadingState.WaitingForStartChar;
								ParseCommand();
								mReceiveBuffer = new Queue<byte>();
							}
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

			if (mLastCommandReceived == OK_COMMAND) message = mLastCommandSent + ":OK";
			if (mLastCommandReceived == ERROR_COMMAND) message = mLastCommandSent + ":ERRORE";

			Logging.Log.LogInfo(message);
			RaiseCommandParsed();
			CommandReceived.Set();
		}

		private byte mGetSubByteByIndex(int param, int index)
		{
			byte subByte = (byte)((param & (255 << 8*index)) >> 8*index);
			return subByte;
		}


	}
}
