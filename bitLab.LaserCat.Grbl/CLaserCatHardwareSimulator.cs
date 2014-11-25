using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.LaserCat.Grbl
{
  public class CLaserCatHardwareSimulator : ILaserCatHardware
  {
    // Define step pulse output pins. NOTE: All step bit pins must be on the same port.
    public int STEP_DDR; //DDRD
    public int STEP_PORT; //PORTD

    // Define step direction output pins. NOTE: All direction pins must be on the same port.
    public int DIRECTION_DDR; //DDRD
    public int DIRECTION_PORT; //PORTD

    // Define stepper driver enable/disable output pin.
    private int STEPPERS_DISABLE_DDR; //DDRB
    private int STEPPERS_DISABLE_PORT; //PORTB
    private const int STEPPERS_DISABLE_BIT = 0; // Uno Digital Pin 8
    private int STEPPERS_DISABLE_MASK = (1 << STEPPERS_DISABLE_BIT);

    // Define global system variables
    public struct system_t
    {
      public int[] position;             // Real-time machine (aka home) position vector in steps. 

      public system_t(bool dummy)
      {
        position = new int[NutsAndBolts.N_AXIS];
      }
    };

    public system_t sys = new system_t(true);

    // Stepper ISR data struct. Contains the running data for the main stepper ISR.
    public struct stepper_t
    {
      // Used by the bresenham line algorithm
      public uint counter_x,        // Counter variables for the bresenham line tracer
                  counter_y,
                  counter_z;

      public byte step_bits;  // Stores out_bits output to complete the step pulse delay   
      public byte execute_step;     // Flags step execution for each interrupt.
      public byte step_pulse_time;  // Step pulse reset time after step rise
      public byte step_outbits;         // The next stepping-bits to be output
      public byte dir_outbits;
      public uint[] steps;
      public short step_count;       // Steps remaining in line segment motion  
      public byte exec_block_index; // Tracks the current st_block index. Change indicates new block.
      //SB! Removed pointers and used indexed access instead
      //public st_block_t* exec_block;   // Pointer to the block data for the segment being executed
      //public segment_t* exec_segment;  // Pointer to the segment being executed
      public int exec_segmentIdx;

      public stepper_t(bool dummy)
      {
        counter_x = counter_y = counter_z = 0;
        step_bits = 0;
        execute_step = 0;
        step_pulse_time = 0;
        step_outbits = 0;
        dir_outbits = 0;
        steps = new uint[NutsAndBolts.N_AXIS];
        step_count = 0;
        exec_block_index = 0;
        //exec_block = null;
        //exec_segment = null;
        exec_segmentIdx = -1;
      }
    } ;
    stepper_t st;

    // Used to avoid ISR nesting of the "Stepper Driver Interrupt". Should never occur though.
    bool busy;

    private LaserCatSettings settings;

    private static st_block_t[] CreateStBlockArray(int count)
    {
      var array = new st_block_t[count];
      for (int i = 0; i < count; i++)
      {
        array[i] = new st_block_t(true);
      }
      return array;
    }
    public st_block_t[] st_block_buffer = CreateStBlockArray(GrblFirmware.SEGMENT_BUFFER_SIZE - 1);

    //SB! Added event to notify planner blocks changes
    public event EventHandler StepperSegmentBufferChanged;
    private void RaiseStepperSegmentBufferChanged()
    {
      if (StepperSegmentBufferChanged != null)
        StepperSegmentBufferChanged(this, EventArgs.Empty);
    }

    public segment_t[] segment_buffer = new segment_t[GrblFirmware.SEGMENT_BUFFER_SIZE];

    //SB! Returns the number of active blocks are in the segment buffer.
    public int stepper_get_segment_buffer_count()
    {
      return segment_buffer_count;
    }
    private void recalcSegmentBufferCount()
    {
      if (segment_buffer_head >= segment_buffer_tail) 
        segment_buffer_count = segment_buffer_head - segment_buffer_tail;
      else
        segment_buffer_count = GrblFirmware.SEGMENT_BUFFER_SIZE - (segment_buffer_tail - segment_buffer_head - 1);
    }

    // Step segment ring buffer indices
    byte segment_buffer_tail;
    byte segment_buffer_head;
    byte segment_next_head;
    int segment_buffer_count;

    public CLaserCatHardwareSimulator()
    {
    }

    public void StorePlannerBlock(byte blockIndex, st_block_t block)
    {
      lock (this)
      {
        st_block_buffer[blockIndex] = block;
      }
    }

    public void StoreSegment(segment_t segment)
    {
      lock (this)
      {
        // Segment complete! Increment segment buffer indices.
        segment_buffer[segment_buffer_head] = segment;
        segment_buffer_head = segment_next_head;
        if (++segment_next_head == GrblFirmware.SEGMENT_BUFFER_SIZE) { segment_next_head = 0; }
        recalcSegmentBufferCount();
        RaiseStepperSegmentBufferChanged();
      }
    }

    public void SetSettings(LaserCatSettings settings)
    {
      this.settings = settings;
    }

    public void WakeUp(bool setupAndEnableMotors)
    {
      // Enable stepper drivers.
      if (NutsAndBolts.bit_istrue(settings.flags, GrblFirmware.BITFLAG_INVERT_ST_ENABLE)) { STEPPERS_DISABLE_PORT |= (1 << STEPPERS_DISABLE_BIT); }
      else { STEPPERS_DISABLE_PORT &= ~(1 << STEPPERS_DISABLE_BIT); }

      if (setupAndEnableMotors)
      {
        // Initialize stepper output bits
        st.dir_outbits = settings.dir_port_invert_mask;
        st.step_outbits = settings.step_port_invert_mask;

        // Initialize step pulse timing from settings. Here to ensure updating after re-writing.
        if (GrblFirmware.STEP_PULSE_DELAY == 10)
        {
          // Set total step pulse time after direction pin set. Ad hoc computation from oscilloscope.
          st.step_pulse_time = (byte)(-(((settings.pulse_microseconds + GrblFirmware.STEP_PULSE_DELAY - 2) * NutsAndBolts.TICKS_PER_MICROSECOND) >> 3));
          // Set delay between direction pin write and step command.
          //TODO
          //OCR0A = -(((settings.pulse_microseconds)*NutsAndBolts.TICKS_PER_MICROSECOND) >> 3);
        }
        else // Normal operation
          // Set step pulse time. Ad hoc computation from oscilloscope. Uses two's complement.
          st.step_pulse_time = (byte)(-(((settings.pulse_microseconds - 2) * NutsAndBolts.TICKS_PER_MICROSECOND) >> 3));


        // Enable Stepper Driver Interrupt
        //SB!
        //TODO TIMSK1 |= (1<<OCIE1A);
        EnableMotors();
      }
    }

    public void Init()
    {
      // Configure step and direction interface pins
      STEP_DDR |= GrblFirmware.STEP_MASK;
      STEPPERS_DISABLE_DDR |= 1 << STEPPERS_DISABLE_BIT;
      DIRECTION_DDR |= GrblFirmware.DIRECTION_MASK;

      //TODO 
      /*
      // Configure Timer 1: Stepper Driver Interrupt
      TCCR1B &= ~(1<<WGM13); // waveform generation = 0100 = CTC
      TCCR1B |=  (1<<WGM12);
      TCCR1A &= ~((1<<WGM11) | (1<<WGM10)); 
      TCCR1A &= ~((1<<COM1A1) | (1<<COM1A0) | (1<<COM1B1) | (1<<COM1B0)); // Disconnect OC1 output
      // TCCR1B = (TCCR1B & ~((1<<CS12) | (1<<CS11))) | (1<<CS10); // Set in st_go_idle().
      // TIMSK1 &= ~(1<<OCIE1A);  // Set in st_go_idle().
  
      // Configure Timer 0: Stepper Port Reset Interrupt
      TIMSK0 &= ~((1<<OCIE0B) | (1<<OCIE0A) | (1<<TOIE0)); // Disconnect OC0 outputs and OVF interrupt.
      TCCR0A = 0; // Normal operation
      TCCR0B = 0; // Disable Timer0 until needed
      TIMSK0 |= (1<<TOIE0); // Enable Timer0 overflow interrupt
      #ifdef STEP_PULSE_DELAY
        TIMSK0 |= (1<<OCIE0A); // Enable Timer0 Compare Match A interrupt
      #endif
      */
    }

    public void GoIdle(bool delayAndDisableSteppers)
    {
      // Disable Stepper Driver Interrupt. Allow Stepper Port Reset Interrupt to finish, if active.
      //SB!
      DisableMotors();
      //TODO
      //TIMSK1 &= ~(1<<OCIE1A); // Disable Timer1 interrupt
      //TCCR1B = (TCCR1B & ~((1<<CS12) | (1<<CS11))) | (1<<CS10); // Reset clock to no prescaling.
      busy = false;

      // Set stepper driver idle state, disabled or enabled, depending on settings and circumstances.
      bool pin_state = false; // Keep enabled.
      if (delayAndDisableSteppers)
      {
        // Force stepper dwell to lock axes for a defined amount of time to ensure the axes come to a complete
        // stop and not drift from residual inertial forces at the end of the last movement.
        NutsAndBolts.delay_ms(settings.stepper_idle_lock_time);
        pin_state = true; // Override. Disable steppers.
      }
      if (NutsAndBolts.bit_istrue(settings.flags, GrblFirmware.BITFLAG_INVERT_ST_ENABLE)) { pin_state = !pin_state; } // Apply pin invert.
      if (pin_state) { STEPPERS_DISABLE_PORT |= (1 << STEPPERS_DISABLE_BIT); }
      else { STEPPERS_DISABLE_PORT &= ~(1 << STEPPERS_DISABLE_BIT); }
    }

    public void Reset()
    {
      Debug.Assert(mMotorsTask == null);
      st = new stepper_t(true);
      st.exec_segmentIdx = -1;
      busy = false;
      // Initialize step and direction port pins.
      STEP_PORT = (STEP_PORT & ~GrblFirmware.STEP_MASK) | settings.step_port_invert_mask;
      DIRECTION_PORT = (DIRECTION_PORT & ~GrblFirmware.DIRECTION_MASK) | settings.dir_port_invert_mask;

      segment_buffer_tail = 0;
      segment_buffer_head = 0; // empty = tail
      segment_next_head = 1;
      recalcSegmentBufferCount();
      RaiseStepperSegmentBufferChanged();
    }







    /* "The Stepper Driver Interrupt" - This timer interrupt is the workhorse of Grbl. Grbl employs
       the venerable Bresenham line algorithm to manage and exactly synchronize multi-axis moves.
       Unlike the popular DDA algorithm, the Bresenham algorithm is not susceptible to numerical
       round-off errors and only requires fast integer counters, meaning low computational overhead
       and maximizing the Arduino's capabilities. However, the downside of the Bresenham algorithm
       is, for certain multi-axis motions, the non-dominant axes may suffer from un-smooth step 
       pulse trains, or aliasing, which can lead to strange audible noises or shaking. This is 
       particularly noticeable or may cause motion issues at low step frequencies (0-5kHz), but 
       is usually not a physical problem at higher frequencies, although audible.
         To improve Bresenham multi-axis performance, Grbl uses what we call an Adaptive Multi-Axis
       Step Smoothing (AMASS) algorithm, which does what the name implies. At lower step frequencies,
       AMASS artificially increases the Bresenham resolution without effecting the algorithm's 
       innate exactness. AMASS adapts its resolution levels automatically depending on the step
       frequency to be executed, meaning that for even lower step frequencies the step smoothing 
       level increases. Algorithmically, AMASS is acheived by a simple bit-shifting of the Bresenham
       step count for each AMASS level. For example, for a Level 1 step smoothing, we bit shift 
       the Bresenham step event count, effectively multiplying it by 2, while the axis step counts 
       remain the same, and then double the stepper ISR frequency. In effect, we are allowing the
       non-dominant Bresenham axes step in the intermediate ISR tick, while the dominant axis is 
       stepping every two ISR ticks, rather than every ISR tick in the traditional sense. At AMASS
       Level 2, we simply bit-shift again, so the non-dominant Bresenham axes can step within any 
       of the four ISR ticks, the dominant axis steps every four ISR ticks, and quadruple the 
       stepper ISR frequency. And so on. This, in effect, virtually eliminates multi-axis aliasing 
       issues with the Bresenham algorithm and does not significantly alter Grbl's performance, but 
       in fact, more efficiently utilizes unused CPU cycles overall throughout all configurations.
         AMASS retains the Bresenham algorithm exactness by requiring that it always executes a full
       Bresenham step, regardless of AMASS Level. Meaning that for an AMASS Level 2, all four 
       intermediate steps must be completed such that baseline Bresenham (Level 0) count is always 
       retained. Similarly, AMASS Level 3 means all eight intermediate steps must be executed. 
       Although the AMASS Levels are in reality arbitrary, where the baseline Bresenham counts can
       be multiplied by any integer value, multiplication by powers of two are simply used to ease 
       CPU overhead with bitshift integer operations. 
         This interrupt is simple and dumb by design. All the computational heavy-lifting, as in
       determining accelerations, is performed elsewhere. This interrupt pops pre-computed segments,
       defined as constant velocity over n number of steps, from the step segment buffer and then 
       executes them by pulsing the stepper pins appropriately via the Bresenham algorithm. This 
       ISR is supported by The Stepper Port Reset Interrupt which it uses to reset the stepper port
       after each pulse. The bresenham line tracer algorithm controls all stepper outputs
       simultaneously with these two interrupts.
   
       NOTE: This interrupt must be as efficient as possible and complete before the next ISR tick, 
       which for Grbl must be less than 33.3usec (@30kHz ISR rate). Oscilloscope measured time in 
       ISR is 5usec typical and 25usec maximum, well below requirement.
       NOTE: This ISR expects at least one step to be executed per segment.
    */
    private Task mMotorsTask;
    private bool mQuitMotorsTask;
    private void EnableMotors()
    {
      if (mMotorsTask != null)
        throw new InvalidOperationException("Motors already started");
      mQuitMotorsTask = false;
      mMotorsTask = Task.Factory.StartNew(MotorsTaskMain);
    }

    private void DisableMotors()
    {
      if (mMotorsTask == null)
        return;
      mQuitMotorsTask = true;
      mMotorsTask.Wait();
      mMotorsTask = null;
    }

    private void MotorsTaskMain()
    {
      while (!mQuitMotorsTask)
      {
        lock (this)
        {
          ExecuteMotors();
        }
      }
    }

    int mLastTime;
    private void ExecuteMotors()
    {
      if (mLastTime == 0)
        mLastTime = Environment.TickCount;
      else
      {
        if (Environment.TickCount - mLastTime > 1)
        {
          mLastTime = Environment.TickCount;
          for (var i = 0; i < 500; i++)
            //SB! TIMER1_COMPA_vect returns true when a segment has been finished
            //When that happens, quit executing the ISR, otherwise we might finish the segment buffer within the 100
            //cycles and the main loop would not have a chance to refill it, which leads to a halt.
            //Normally in Grbl this interrupt executes every 33.3us, and the main loop much more often.
            if (TIMER1_COMPA_vect()) 
              break;
        }
      }
    }

    // TODO: Replace direct updating of the int32 position counters in the ISR somehow. Perhaps use smaller
    // int8 variables and update position counters only when a segment completes. This can get complicated 
    // with probing and homing cycles that require true real-time positions.
    //ISR(TIMER1_COMPA_vect)
    public bool TIMER1_COMPA_vect()
    {
      // SPINDLE_ENABLE_PORT ^= 1<<SPINDLE_ENABLE_BIT; // Debug: Used to time ISR
      if (busy) { return false; } // The busy-flag is used to avoid reentering this interrupt

      // Set the direction pins a couple of nanoseconds before we step the steppers
      DIRECTION_PORT = (DIRECTION_PORT & ~GrblFirmware.DIRECTION_MASK) | (st.dir_outbits & GrblFirmware.DIRECTION_MASK);

      // Then pulse the stepping pins
      if (GrblFirmware.STEP_PULSE_DELAY != 0)
        st.step_bits = (byte)((STEP_PORT & ~GrblFirmware.STEP_MASK) | st.step_outbits); // Store out_bits to prevent overwriting.
      else  // Normal operation
        STEP_PORT = (STEP_PORT & ~GrblFirmware.STEP_MASK) | st.step_outbits;

      // Enable step pulse reset timer so that The Stepper Port Reset Interrupt can reset the signal after
      // exactly settings.pulse_microseconds microseconds, independent of the main Timer1 prescaler.
      //TODO TCNT0 = st.step_pulse_time; // Reload Timer0 counter
      //TODO TCCR0B = (1<<CS01); // Begin Timer0. Full speed, 1/8 prescaler

      busy = true;
      //TODO sei(); // Re-enable interrupts to allow Stepper Port Reset Interrupt to fire on-time. 
      // NOTE: The remaining code in this ISR will finish before returning to main program.

      // If there is no step segment, attempt to pop one from the stepper buffer
      if (st.exec_segmentIdx == -1)
      {
        // Anything in the buffer? If so, load and initialize next step segment.
        if (segment_buffer_head != segment_buffer_tail)
        {
          // Initialize new step segment and load number of steps to execute
          st.exec_segmentIdx = segment_buffer_tail;

          if (!GrblFirmware.ADAPTIVE_MULTI_AXIS_STEP_SMOOTHING)
          {
            // With AMASS is disabled, set timer prescaler for segments with slow step frequencies (< 250Hz).
            //TODO TCCR1B = (TCCR1B & ~(0x07<<CS10)) | (segment_buffer[st.exec_segmentIdx].prescaler<<CS10);
          }

          // Initialize step segment timing per step and load number of steps to execute.
          //TODO OCR1A = segment_buffer[st.exec_segmentIdx].cycles_per_tick;
          st.step_count = segment_buffer[st.exec_segmentIdx].n_step; // NOTE: Can sometimes be zero when moving slow.
          // If the new segment starts a new planner block, initialize stepper variables and counters.
          // NOTE: When the segment data index changes, this indicates a new planner block.
          if (st.exec_block_index != segment_buffer[st.exec_segmentIdx].st_block_index)
          {
            st.exec_block_index = segment_buffer[st.exec_segmentIdx].st_block_index;
            //SB! no longer necessary, we already have st.exec_block_index
            //st.exec_block = &st_block_buffer[st.exec_block_index];

            // Initialize Bresenham line and distance counters
            st.counter_x = (st_block_buffer[st.exec_block_index].step_event_count >> 1);
            st.counter_y = st.counter_x;
            st.counter_z = st.counter_x;
          }

          st.dir_outbits = (byte)(st_block_buffer[st.exec_block_index].direction_bits ^ settings.dir_port_invert_mask);

          if (GrblFirmware.ADAPTIVE_MULTI_AXIS_STEP_SMOOTHING)
          {
            // With AMASS enabled, adjust Bresenham axis increment counters according to AMASS level.
            st.steps[NutsAndBolts.X_AXIS] = st_block_buffer[st.exec_block_index].steps[NutsAndBolts.X_AXIS] >> segment_buffer[st.exec_segmentIdx].amass_level;
            st.steps[NutsAndBolts.Y_AXIS] = st_block_buffer[st.exec_block_index].steps[NutsAndBolts.Y_AXIS] >> segment_buffer[st.exec_segmentIdx].amass_level;
            st.steps[NutsAndBolts.Z_AXIS] = st_block_buffer[st.exec_block_index].steps[NutsAndBolts.Z_AXIS] >> segment_buffer[st.exec_segmentIdx].amass_level;
          }

        }
        else
        {
          //TODO
          //// Segment buffer empty. Shutdown.
          //st_go_idle();
          //bit_true_atomic(ref sys.execute, EXEC_CYCLE_STOP); // Flag main program for cycle end
          var delayAndDisableSteppersTODO = false;
          GoIdle(delayAndDisableSteppersTODO);
          return false; // Nothing to do but exit.
        }
      }


      // Check probing state.
      //TODO probe_state_monitor();

      // Reset step out bits.
      st.step_outbits = 0;

      // Execute step displacement profile by Bresenham line algorithm
      if (GrblFirmware.ADAPTIVE_MULTI_AXIS_STEP_SMOOTHING)
        st.counter_x += st.steps[NutsAndBolts.X_AXIS];
      else
        st.counter_x += st_block_buffer[st.exec_block_index].steps[NutsAndBolts.X_AXIS];

      if (st.counter_x > st_block_buffer[st.exec_block_index].step_event_count)
      {
        st.step_outbits |= (1 << GrblFirmware.X_STEP_BIT);
        st.counter_x -= st_block_buffer[st.exec_block_index].step_event_count;
        if ((st_block_buffer[st.exec_block_index].direction_bits & (1 << GrblFirmware.X_DIRECTION_BIT)) != 0) { sys.position[NutsAndBolts.X_AXIS]--; }
        else { sys.position[NutsAndBolts.X_AXIS]++; }
      }
      if (GrblFirmware.ADAPTIVE_MULTI_AXIS_STEP_SMOOTHING)
        st.counter_y += st.steps[NutsAndBolts.Y_AXIS];
      else
        st.counter_y += st_block_buffer[st.exec_block_index].steps[NutsAndBolts.Y_AXIS];

      if (st.counter_y > st_block_buffer[st.exec_block_index].step_event_count)
      {
        st.step_outbits |= (1 << GrblFirmware.Y_STEP_BIT);
        st.counter_y -= st_block_buffer[st.exec_block_index].step_event_count;
        if ((st_block_buffer[st.exec_block_index].direction_bits & (1 << GrblFirmware.Y_DIRECTION_BIT)) != 0) { sys.position[NutsAndBolts.Y_AXIS]--; }
        else { sys.position[NutsAndBolts.Y_AXIS]++; }
      }
      if (GrblFirmware.ADAPTIVE_MULTI_AXIS_STEP_SMOOTHING)
        st.counter_z += st.steps[NutsAndBolts.Z_AXIS];
      else
        st.counter_z += st_block_buffer[st.exec_block_index].steps[NutsAndBolts.Z_AXIS];

      if (st.counter_z > st_block_buffer[st.exec_block_index].step_event_count)
      {
        st.step_outbits |= (1 << GrblFirmware.Z_STEP_BIT);
        st.counter_z -= st_block_buffer[st.exec_block_index].step_event_count;
        if ((st_block_buffer[st.exec_block_index].direction_bits & (1 << GrblFirmware.Z_DIRECTION_BIT)) != 0) { sys.position[NutsAndBolts.Z_AXIS]--; }
        else { sys.position[NutsAndBolts.Z_AXIS]++; }
      }

      // During a homing cycle, lock out and prevent desired axes from moving.
      //TODO if (sys.state == STATE_HOMING) { st.step_outbits &= sys.homing_axis_lock; }

      st.step_count--; // Decrement step events count 
      var hasFinishedASegment = false;
      if (st.step_count == 0)
      {
        // Segment is complete. Discard current segment and advance segment indexing.
        st.exec_segmentIdx = -1;
        if (++segment_buffer_tail == GrblFirmware.SEGMENT_BUFFER_SIZE) { segment_buffer_tail = 0; }
        hasFinishedASegment = true;
        recalcSegmentBufferCount();
        RaiseStepperSegmentBufferChanged();
      }

      st.step_outbits ^= settings.step_port_invert_mask;  // Apply step port invert mask    
      busy = false;
      // SPINDLE_ENABLE_PORT ^= 1<<SPINDLE_ENABLE_BIT; // Debug: Used to time ISR
      return hasFinishedASegment;
    }


    /* The Stepper Port Reset Interrupt: Timer0 OVF interrupt handles the falling edge of the step
       pulse. This should always trigger before the next Timer1 COMPA interrupt and independently
       finish, if Timer1 is disabled after completing a move.
       NOTE: Interrupt collisions between the serial and stepper interrupts can cause delays by
       a few microseconds, if they execute right before one another. Not a big deal, but can
       cause issues at high step rates if another high frequency asynchronous interrupt is 
       added to Grbl.
    */
    // This interrupt is enabled by ISR_TIMER1_COMPAREA when it sets the motor port bits to execute
    // a step. This ISR resets the motor port after a short period (settings.pulse_microseconds) 
    // completing one step cycle.
    //ISR(TIMER0_OVF_vect)
    void TIMER0_OVF_vect()
    {
      // Reset stepping pins (leave the direction pins)
      STEP_PORT = (STEP_PORT & ~GrblFirmware.STEP_MASK) | (settings.step_port_invert_mask & GrblFirmware.STEP_MASK);
      //TODO TCCR0B = 0; // Disable Timer0 to prevent re-entering this interrupt when it's not needed. 
    }

    //TODO
    //#ifdef STEP_PULSE_DELAY
    //  // This interrupt is used only when STEP_PULSE_DELAY is enabled. Here, the step pulse is
    //  // initiated after the STEP_PULSE_DELAY time period has elapsed. The ISR TIMER2_OVF interrupt
    //  // will then trigger after the appropriate settings.pulse_microseconds, as in normal operation.
    //  // The new timing between direction, step pulse, and step complete events are setup in the
    //  // st_wake_up() routine.
    //  ISR(TIMER0_COMPA_vect) 
    //  { 
    //    STEP_PORT = st.step_bits; // Begin step pulse.
    //  }
    //#endif


    public bool GetHasMoreSegmentBuffer()
    {
      return segment_buffer_tail != segment_next_head;
    }

  }
}
