using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace bitLab.LaserCat.Grbl
{
	public class CLaserCatHardwarePIC : ILaserCatHardware
	{
		private SerialPort mSerialPort;
		private String mPortName;

		private Queue<byte> mReceiveBuffer;

		private const char START_CHAR = '#';
		private enum EReadingState { WaitingForStartChar, WaitingForReadLength, Reading };
		private EReadingState mReadingState;
		private int mCharToRead;

		public event EventHandler CommandParsed;
		private void RaiseCommandParsed()
		{
			if (CommandParsed != null)
				CommandParsed(this, EventArgs.Empty);
		}

		public CLaserCatHardwarePIC(string portName)
		{
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
			//TODO
			SendToPIC("Init");
		}

		public void Reset()
		{
			//TODO
			SendToPIC("Reset");
		}

		public void SetSettings(LaserCatSettings settings)
		{
			//TODO
			SendToPIC(String.Format("Settings,{0},{1}",settings.dir_invert_mask,settings.pulse_microseconds));
		}

		public void WakeUp(bool setupAndEnableMotors)
		{
			//TODO
			SendToPIC("WakeUp," + Convert.ToString(setupAndEnableMotors));
		}

		public void GoIdle(bool delayAndDisableSteppers)
		{
			//TODO
			SendToPIC("GoIdle," + Convert.ToString(delayAndDisableSteppers));
		}

		public void StorePlannerBlock(byte blockIndex, st_block_t block)
		{
			//TODO
			SendToPIC(string.Format("StoreBlock,{0},{1},{2},{3}",
															blockIndex,
															Convert.ToString(block.direction_bits),
															Convert.ToString(block.step_event_count),
															Convert.ToString(block.steps)));
		}

		public bool GetHasMoreSegmentBuffer()
		{
			//TODO
			return true;
		}

		public void StoreSegment(segment_t segment)
		{
			//TODO
			SendToPIC(string.Format("StoreSegment,{0},{1},{2},{3},{4}",
															Convert.ToString(segment.amass_level),
															Convert.ToString(segment.cycles_per_tick),
															Convert.ToString(segment.n_step),
															Convert.ToString(segment.prescaler),
															Convert.ToString(segment.st_block_index)));
		}

		private void SendToPIC(String data)
		{
			mSerialPort.Write(FormatCommandForSend(data));
		}

		private String FormatCommandForSend(String data)
		{
			String formattedData = Convert.ToString(START_CHAR);
			int dataLength = data.Length;
			formattedData += Convert.ToChar(dataLength);
			formattedData += data;
			return formattedData;
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
							if (mCharToRead == 0)
							{
								mReadingState = EReadingState.WaitingForStartChar;
								ParseCommand();
								mReceiveBuffer = new Queue<byte>();
							}
							else
							{
								mReceiveBuffer.Enqueue(buffer[i]);
								mCharToRead--;
							}
						}
						break;
				}
			}
		}

		private void ParseCommand()
		{
			//TODO
			String message = System.Text.Encoding.Default.GetString(mReceiveBuffer.ToArray());

			if (message.StartsWith("Init")) message = "INIZIALIZZAZIONE";
			if (message.StartsWith("Reset")) message = "RESET";
			if (message.StartsWith("Settings")) message = "IMPOSTAZIONE";
			if (message.StartsWith("WakeUp")) message = "SVEGLIA";
			if (message.StartsWith("GoIdle")) message = "RIPOSA";
			if (message.StartsWith("StoreBlock")) message = "BLOCCO RICEVUTO";
			if (message.StartsWith("StoreSegment")) message = "SEGMENTO RICEVUTO";

			Logging.Log.LogInfo(message);
			RaiseCommandParsed();
		}

	}
}
