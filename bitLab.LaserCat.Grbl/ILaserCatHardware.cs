using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.LaserCat.Grbl
{
  // Stores the planner block Bresenham algorithm execution data for the segments in the segment 
  // buffer. Normally, this buffer is partially in-use, but, for the worst case scenario, it will
  // never exceed the number of accessible stepper buffer segments (SEGMENT_BUFFER_SIZE-1).
  // NOTE: This data is copied from the prepped planner blocks so that the planner blocks may be
  // discarded when entirely consumed and completed by the segment buffer. Also, AMASS alters this
  // data for its own use. 
  public struct st_block_t
  {
    public byte direction_bits;
    public uint[] steps;
    public uint step_event_count;

    public st_block_t(bool dummy)
    {
      direction_bits = 0;
      steps = new uint[NutsAndBolts.N_AXIS];
      step_event_count = 0;
    }
  };

  // Primary stepper segment ring buffer. Contains small, short line segments for the stepper 
  // algorithm to execute, which are "checked-out" incrementally from the first block in the
  // planner buffer. Once "checked-out", the steps in the segments buffer cannot be modified by 
  // the planner, where the remaining planner block steps still can.
  public struct segment_t
  {
    public short n_step;          // Number of step events to be executed for this segment
    public byte st_block_index;   // Stepper block data index. Uses this information to execute this segment.
    public ushort cycles_per_tick; // Step distance traveled per ISR tick, aka step rate.  
    public byte amass_level;    // Indicates AMASS level for the ISR to execute this segment  
    public byte prescaler;      // Without AMASS, a prescaler is required to adjust for slow timing.
  };

  public struct LaserCatSettings
  {
    public byte pulse_microseconds;
    public byte step_invert_mask;
    public byte dir_invert_mask; 
    public byte stepper_idle_lock_time; // If max value 255, steppers do not disable.
    public byte flags; // Contains default boolean settings

    //Previously these were locals of stepper.cs
    public byte step_port_invert_mask;
    public byte dir_port_invert_mask;
  }

  //SB!Implementation guidelines:
  //1) sys.position is written only by the stepper interrupt, read by GCode, Planner and Probe to sync the position
  //Solution: Add a method to read the position from the hardware
  //2) bit_true_atomic(ref sys.execute, EXEC_CYCLE_STOP)
  //Solution: TODO analysis
  //3) Call to GoIdle, don't know the value for delayAndDisableSteppers
  //Solution: TODO analysis, maybe we don't need to disable them at all? can disable the interrupts be sufficient?
  //4) probe_state_monitor()
  //Solution: Ignore until Probing is implemented
  //5) if (sys.state == STATE_HOMING) ...
  //Solution: Ignore until Limits is implemented

  public interface ILaserCatHardware
  {
    bool Connect(string COMPort);
    void Reset();
    void SetSettings(LaserCatSettings settings);
    void WakeUp(bool setupAndEnableMotors);
    void GoIdle(bool delayAndDisableSteppers);
    void StorePlannerBlock(byte blockIndex, st_block_t block);
    int AskHasMoreSegmentBuffer();
    //void StoreSegment(segment_t segment);
    byte StoreSegment(segment_t segment);
		Int32[] AskPosition();
    Int32 GetSegmentBufferCount();
		void SetSpeed(int speedValue , int timerPeriod);
  }
}
