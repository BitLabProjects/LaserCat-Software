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
      mComMan.Send(ECommands.INIT_COMMAND);
		}

		public void Reset()
		{
      mComMan.Send(ECommands.RESET_COMMAND);
		}

		public void SetSettings(LaserCatSettings settings)
		{
      var data = new List<byte>() { settings.pulse_microseconds,
                      settings.step_invert_mask,
                      settings.dir_invert_mask, 
                      settings.stepper_idle_lock_time,
                      settings.flags, 
                      settings.step_port_invert_mask,
                      settings.dir_port_invert_mask};
      mComMan.Send(ECommands.SETSETTINGS_COMMAND, data);
		}

		public void WakeUp(bool setupAndEnableMotors)
		{
      var data = new List<byte>() {  Convert.ToByte(setupAndEnableMotors) };
      mComMan.Send(ECommands.WAKEUP_COMMAND, data);
		}

		public void GoIdle(bool delayAndDisableSteppers)
		{
      var data = new List<byte>() { Convert.ToByte(delayAndDisableSteppers) };
      mComMan.Send(ECommands.GOIDLE_COMMAND, data);
		}

		public void StorePlannerBlock(byte blockIndex, st_block_t block)
		{
			List<byte> dataList = new List<byte>();

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

      mComMan.Send(ECommands.STOREBLOCK_COMMAND, dataList);
		}

		public int AskHasMoreSegmentBuffer()
		{
			Debug.WriteLine("AskHasMoreSegmentBuffer");
      mComMan.Send(ECommands.ASKHASMORESEGMENTBUFFER_COMMAND);
      Debug.WriteLine("AskHasMoreSegmentBuffer=" + mComMan.mHasMoreSegmentBuffer);
      return mComMan.mHasMoreSegmentBuffer;
		}

		public void StoreSegment(segment_t segment)
		{
			byte n_stepHI = mGetSubByteByIndex(Convert.ToInt32(segment.n_step), 1);
			byte n_stepLO = mGetSubByteByIndex(Convert.ToInt32(segment.n_step), 0);

      Debug.WriteLine("StoreSegment, cycles_per_tick={0}, stepIdx={1}, steps={2}", segment.cycles_per_tick, segment.st_block_index, segment.n_step);
			byte cycles_per_tickHI = mGetSubByteByIndex(Convert.ToInt32(segment.cycles_per_tick), 1);
			byte cycles_per_tickLO = mGetSubByteByIndex(Convert.ToInt32(segment.cycles_per_tick), 0);

      var data = new List<byte>() { n_stepLO, n_stepHI,
                      segment.st_block_index,
                      cycles_per_tickLO, cycles_per_tickHI, 
                      segment.amass_level,
                      segment.prescaler};
			Debug.WriteLine("StoreSegment");
      mComMan.Send(ECommands.STORESEGMENT_COMMAND, data);
		}

    public int GetSegmentBufferCount()
    {
      return mComMan.mSegmentBufferCount;
    }

		public Int32[] AskPosition()
		{
			Debug.WriteLine("AskPosition");
      mComMan.Send(ECommands.ASKPOSITION_COMMAND);
      Int32[] position = { mComMan.mPositionX, mComMan.mPositionY, mComMan.mPositionZ };
			return position;
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