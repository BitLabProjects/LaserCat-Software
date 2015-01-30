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
    private CCommunicationManager mComMan;

		public CLaserCatHardwarePIC(string portName)
		{
      mComMan = new CCommunicationManager(portName);
		}

		public void Init()
		{
      mComMan.mLastCommandSent = (byte)ECommands.INIT_COMMAND;
      byte[] data = { mComMan.mLastCommandSent };
			SendToPIC(data);
		}

		public void Reset()
		{
      mComMan.mLastCommandSent = (byte)ECommands.RESET_COMMAND;
      byte[] data = { mComMan.mLastCommandSent };
			SendToPIC(data);
		}

		public void SetSettings(LaserCatSettings settings)
		{
      mComMan.mLastCommandSent = (byte)ECommands.SETSETTINGS_COMMAND;
      byte[] data = { mComMan.mLastCommandSent,
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
      mComMan.mLastCommandSent = (byte)ECommands.WAKEUP_COMMAND;
      byte[] data = { mComMan.mLastCommandSent, Convert.ToByte(setupAndEnableMotors) };
			SendToPIC(data);
		}

		public void GoIdle(bool delayAndDisableSteppers)
		{
      mComMan.mLastCommandSent = (byte)ECommands.GOIDLE_COMMAND;
      byte[] data = { mComMan.mLastCommandSent, Convert.ToByte(delayAndDisableSteppers) };
			SendToPIC(data);
		}

		public void StorePlannerBlock(byte blockIndex, st_block_t block)
		{
      mComMan.mLastCommandSent = (byte)ECommands.STOREBLOCK_COMMAND;

			List<byte> dataList = new List<byte>();

      dataList.Add(mComMan.mLastCommandSent);
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
      mComMan.mLastCommandSent = (byte)ECommands.ASKHASMORESEGMENTBUFFER_COMMAND;
      byte[] data = { mComMan.mLastCommandSent };
			Debug.WriteLine("AskHasMoreSegmentBuffer");
			SendToPIC(data);
      Debug.WriteLine("AskHasMoreSegmentBuffer=" + mComMan.mHasMoreSegmentBuffer);
      return mComMan.mHasMoreSegmentBuffer;
		}

		public void StoreSegment(segment_t segment)
		{
      mComMan.mLastCommandSent = (byte)ECommands.STORESEGMENT_COMMAND;

			byte n_stepHI = mGetSubByteByIndex(Convert.ToInt32(segment.n_step), 1);
			byte n_stepLO = mGetSubByteByIndex(Convert.ToInt32(segment.n_step), 0);

      Debug.WriteLine("StoreSegment, cycles_per_tick={0}, stepIdx={1}, steps={2}", segment.cycles_per_tick, segment.st_block_index, segment.n_step);
			byte cycles_per_tickHI = mGetSubByteByIndex(Convert.ToInt32(segment.cycles_per_tick), 1);
			byte cycles_per_tickLO = mGetSubByteByIndex(Convert.ToInt32(segment.cycles_per_tick), 0);

      byte[] data = { mComMan.mLastCommandSent,
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
      return mComMan.mSegmentBufferCount;
    }

		public Int32[] AskPosition()
		{
      mComMan.mLastCommandSent = (byte)ECommands.ASKPOSITION_COMMAND;
      byte[] data = { mComMan.mLastCommandSent };
			Debug.WriteLine("AskPosition");
			SendToPIC(data);
      Int32[] position = { mComMan.mPositionX, mComMan.mPositionY, mComMan.mPositionZ };
			return position;
		}

		private void SendToPIC(byte[] data)
		{
      mComMan.SendToPIC(data);
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