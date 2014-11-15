using System;
namespace bitLab.LaserCat.Grbl
{
  unsafe public partial class GrblFirmware
  {

    // Define global system variables
    public struct system_t
    {
      public byte abort;                 // System abort flag. Forces exit back to main loop for reset.
      public byte state;                 // Tracks the current state of Grbl.
      public byte execute;      // Global system runtime executor bitflag variable. See EXEC bitmasks.
      public byte homing_axis_lock;
      public int[] position;      // Real-time machine (aka home) position vector in steps. 
      // NOTE: This may need to be a volatile variable, if problems arise.                             
      public byte auto_start;            // Planner auto-start flag. Toggled off during feed hold. Defaulted by settings.
      public byte probe_state;   // Probing state value.  Used to coordinate the probing cycle with stepper ISR.
      public int[] probe_position; // Last probe position in machine coordinates and steps.

      public system_t(bool dummy)
      {
        abort = 0;
        state = 0;
        execute = 0;
        homing_axis_lock = 0;
        position = new int[NutsAndBolts.N_AXIS];
        auto_start = 0;
        probe_state = 0;
        probe_position = new int[NutsAndBolts.N_AXIS];
      }
    } ;

    public system_t sys = new system_t(true);

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

    private static st_block_t[] CreateStBlockArray(int count)
    {
      var array = new st_block_t[count];
      for (int i = 0; i < count; i++)
      {
        array[i] = new st_block_t(true);
      }
      return array;
    }
    public st_block_t[] st_block_buffer = CreateStBlockArray(SEGMENT_BUFFER_SIZE-1);

    //SB! Added event to notify planner blocks changes
    public event EventHandler StepperSegmentBufferChanged;
    private void RaiseStepperSegmentBufferChanged()
    {
      if (StepperSegmentBufferChanged != null)
        StepperSegmentBufferChanged(this, EventArgs.Empty);
    }

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
    } ;

    public segment_t[] segment_buffer = new segment_t[SEGMENT_BUFFER_SIZE];

    //SB! Returns the number of active blocks are in the segment buffer.
    public int stepper_get_segment_buffer_count()
    {
      if (segment_buffer_head >= segment_buffer_tail) { return segment_buffer_head - segment_buffer_tail; }
      return SEGMENT_BUFFER_SIZE - (segment_buffer_tail - segment_buffer_head);
    }

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

    // Step segment ring buffer indices
    byte segment_buffer_tail;
    byte segment_buffer_head;
    byte segment_next_head;

    // Step and direction port invert masks. 
    byte step_port_invert_mask;
    byte dir_port_invert_mask;

    // Used to avoid ISR nesting of the "Stepper Driver Interrupt". Should never occur though.
    bool busy;

    // Pointers for the step segment being prepped from the planner buffer. Accessed only by the
    // main program. Pointers may be planning segments or planner blocks ahead of what being executed.
    //plan_block_t* pl_block;     // Pointer to the planner block being prepped
    int pl_blockIdx = -1;

    // Segment preparation data struct. Contains all the necessary information to compute new segments
    // based on the current executing planner block.
    public struct st_prep_t
    {
      public byte st_block_index;  // Index of stepper common data block being prepped
      public byte flag_partial_block;  // Flag indicating the last block completed. Time to load a new one.

      public float steps_remaining;
      public float step_per_mm;           // Current planner block step/millimeter conversion scalar
      public float req_mm_increment;
      public float dt_remainder;

      public byte ramp_type;      // Current segment ramp state
      public float mm_complete;      // End of velocity profile from end of current planner block in (mm).
      // NOTE: This value must coincide with a step(no mantissa) when converted.
      public float current_speed;    // Current speed at the end of the segment buffer (mm/min)
      public float maximum_speed;    // Maximum speed of executing block. Not always nominal speed. (mm/min)
      public float exit_speed;       // Exit speed of executing block (mm/min)
      public float accelerate_until; // Acceleration ramp end measured from end of block (mm)
      public float decelerate_after; // Deceleration ramp start measured from end of block (mm)
    } ;
    st_prep_t prep;


    /*    BLOCK VELOCITY PROFILE DEFINITION 
              __________________________
             /|                        |\     _________________         ^
            / |                        | \   /|               |\        |
           /  |                        |  \ / |               | \       s
          /   |                        |   |  |               |  \      p
         /    |                        |   |  |               |   \     e
        +-----+------------------------+---+--+---------------+----+    e
        |               BLOCK 1            ^      BLOCK 2          |    d
                                           |
                      time ----->      EXAMPLE: Block 2 entry speed is at max junction velocity
  
      The planner block buffer is planned assuming constant acceleration velocity profiles and are
      continuously joined at block junctions as shown above. However, the planner only actively computes
      the block entry speeds for an optimal velocity plan, but does not compute the block internal
      velocity profiles. These velocity profiles are computed ad-hoc as they are executed by the 
      stepper algorithm and consists of only 7 possible types of profiles: cruise-only, cruise-
      deceleration, acceleration-cruise, acceleration-only, deceleration-only, full-trapezoid, and 
      triangle(no cruise).

                                            maximum_speed (< nominal_speed) ->  + 
                        +--------+ <- maximum_speed (= nominal_speed)          /|\                                         
                       /          \                                           / | \                      
     current_speed -> +            \                                         /  |  + <- exit_speed
                      |             + <- exit_speed                         /   |  |                       
                      +-------------+                     current_speed -> +----+--+                   
                       time -->  ^  ^                                           ^  ^                       
                                 |  |                                           |  |                       
                    decelerate_after(in mm)                             decelerate_after(in mm)
                        ^           ^                                           ^  ^
                        |           |                                           |  |
                    accelerate_until(in mm)                             accelerate_until(in mm)
                    
      The step segment buffer computes the executing block velocity profile and tracks the critical
      parameters for the stepper algorithm to accurately trace the profile. These critical parameters 
      are shown and defined in the above illustration.
    */


    // Stepper state initialization. Cycle should only start if the st.cycle_start flag is
    // enabled. Startup init and limits call this function but shouldn't start the cycle.
    public void st_wake_up()
    {
      // Enable stepper drivers.
      if (bit_istrue(settings.flags, BITFLAG_INVERT_ST_ENABLE)) { STEPPERS_DISABLE_PORT |= (1 << STEPPERS_DISABLE_BIT); }
      else { STEPPERS_DISABLE_PORT &= ~(1 << STEPPERS_DISABLE_BIT); }

      if ((sys.state & (STATE_CYCLE | STATE_HOMING)) != 0)
      {
        // Initialize stepper output bits
        st.dir_outbits = dir_port_invert_mask;
        st.step_outbits = step_port_invert_mask;

        // Initialize step pulse timing from settings. Here to ensure updating after re-writing.
        if (STEP_PULSE_DELAY == 10)
        {
          // Set total step pulse time after direction pin set. Ad hoc computation from oscilloscope.
          st.step_pulse_time = (byte)(-(((settings.pulse_microseconds + STEP_PULSE_DELAY - 2) * NutsAndBolts.TICKS_PER_MICROSECOND) >> 3));
          // Set delay between direction pin write and step command.
          //TODO
          //OCR0A = -(((settings.pulse_microseconds)*NutsAndBolts.TICKS_PER_MICROSECOND) >> 3);
        }
        else // Normal operation
          // Set step pulse time. Ad hoc computation from oscilloscope. Uses two's complement.
          st.step_pulse_time = (byte)(-(((settings.pulse_microseconds - 2) * NutsAndBolts.TICKS_PER_MICROSECOND) >> 3));


        // Enable Stepper Driver Interrupt
        //TODO TIMSK1 |= (1<<OCIE1A);
      }
    }


    // Stepper shutdown
    public void st_go_idle()
    {
      // Disable Stepper Driver Interrupt. Allow Stepper Port Reset Interrupt to finish, if active.
      //TODO TIMSK1 &= ~(1<<OCIE1A); // Disable Timer1 interrupt
      //TODO TCCR1B = (TCCR1B & ~((1<<CS12) | (1<<CS11))) | (1<<CS10); // Reset clock to no prescaling.
      busy = false;

      // Set stepper driver idle state, disabled or enabled, depending on settings and circumstances.
      bool pin_state = false; // Keep enabled.
      if (((settings.stepper_idle_lock_time != 0xff) || bit_istrue(sys.execute, EXEC_ALARM)) && sys.state != STATE_HOMING)
      {
        // Force stepper dwell to lock axes for a defined amount of time to ensure the axes come to a complete
        // stop and not drift from residual inertial forces at the end of the last movement.
        delay_ms(settings.stepper_idle_lock_time);
        pin_state = true; // Override. Disable steppers.
      }
      if (bit_istrue(settings.flags, BITFLAG_INVERT_ST_ENABLE)) { pin_state = !pin_state; } // Apply pin invert.
      if (pin_state) { STEPPERS_DISABLE_PORT |= (1 << STEPPERS_DISABLE_BIT); }
      else { STEPPERS_DISABLE_PORT &= ~(1 << STEPPERS_DISABLE_BIT); }
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
    // TODO: Replace direct updating of the int32 position counters in the ISR somehow. Perhaps use smaller
    // int8 variables and update position counters only when a segment completes. This can get complicated 
    // with probing and homing cycles that require true real-time positions.
    //TODO
    //ISR(TIMER1_COMPA_vect)
    public void TIMER1_COMPA_vect()
    {        
    // SPINDLE_ENABLE_PORT ^= 1<<SPINDLE_ENABLE_BIT; // Debug: Used to time ISR
      if (busy) { return; } // The busy-flag is used to avoid reentering this interrupt

      // Set the direction pins a couple of nanoseconds before we step the steppers
      DIRECTION_PORT = (DIRECTION_PORT & ~DIRECTION_MASK) | (st.dir_outbits & DIRECTION_MASK);

      // Then pulse the stepping pins
      if (STEP_PULSE_DELAY!=0)
        st.step_bits = (byte)((STEP_PORT & ~STEP_MASK) | st.step_outbits); // Store out_bits to prevent overwriting.
      else  // Normal operation
        STEP_PORT = (STEP_PORT & ~STEP_MASK) | st.step_outbits;

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
        if (segment_buffer_head != segment_buffer_tail) {
          // Initialize new step segment and load number of steps to execute
          st.exec_segmentIdx = segment_buffer_tail;

          if (!ADAPTIVE_MULTI_AXIS_STEP_SMOOTHING) {
            // With AMASS is disabled, set timer prescaler for segments with slow step frequencies (< 250Hz).
            //TODO TCCR1B = (TCCR1B & ~(0x07<<CS10)) | (segment_buffer[st.exec_segmentIdx].prescaler<<CS10);
          }

          // Initialize step segment timing per step and load number of steps to execute.
          //TODO OCR1A = segment_buffer[st.exec_segmentIdx].cycles_per_tick;
          st.step_count = segment_buffer[st.exec_segmentIdx].n_step; // NOTE: Can sometimes be zero when moving slow.
          // If the new segment starts a new planner block, initialize stepper variables and counters.
          // NOTE: When the segment data index changes, this indicates a new planner block.
          if ( st.exec_block_index != segment_buffer[st.exec_segmentIdx].st_block_index ) {
            st.exec_block_index = segment_buffer[st.exec_segmentIdx].st_block_index;
            //SB! no longer necessary, we already have st.exec_block_index
            //st.exec_block = &st_block_buffer[st.exec_block_index];

            // Initialize Bresenham line and distance counters
            st.counter_x = (st_block_buffer[st.exec_block_index].step_event_count >> 1);
            st.counter_y = st.counter_x;
            st.counter_z = st.counter_x;        
          }

          st.dir_outbits = (byte)(st_block_buffer[st.exec_block_index].direction_bits ^ dir_port_invert_mask); 

          if (ADAPTIVE_MULTI_AXIS_STEP_SMOOTHING) {
            // With AMASS enabled, adjust Bresenham axis increment counters according to AMASS level.
            st.steps[NutsAndBolts.X_AXIS] = st_block_buffer[st.exec_block_index].steps[NutsAndBolts.X_AXIS] >> segment_buffer[st.exec_segmentIdx].amass_level;
            st.steps[NutsAndBolts.Y_AXIS] = st_block_buffer[st.exec_block_index].steps[NutsAndBolts.Y_AXIS] >> segment_buffer[st.exec_segmentIdx].amass_level;
            st.steps[NutsAndBolts.Z_AXIS] = st_block_buffer[st.exec_block_index].steps[NutsAndBolts.Z_AXIS] >> segment_buffer[st.exec_segmentIdx].amass_level;
          }

        } else {
          // Segment buffer empty. Shutdown.
          st_go_idle();
          bit_true_atomic(ref sys.execute, EXEC_CYCLE_STOP); // Flag main program for cycle end
          return; // Nothing to do but exit.
        }  
      }


      // Check probing state.
      probe_state_monitor();

      // Reset step out bits.
      st.step_outbits = 0; 

      // Execute step displacement profile by Bresenham line algorithm
      if (ADAPTIVE_MULTI_AXIS_STEP_SMOOTHING)
        st.counter_x += st.steps[NutsAndBolts.X_AXIS];
      else
        st.counter_x += st_block_buffer[st.exec_block_index].steps[NutsAndBolts.X_AXIS];
      
      if (st.counter_x > st_block_buffer[st.exec_block_index].step_event_count) {
        st.step_outbits |= (1<<X_STEP_BIT);
        st.counter_x -= st_block_buffer[st.exec_block_index].step_event_count;
        if ((st_block_buffer[st.exec_block_index].direction_bits & (1<<X_DIRECTION_BIT))!=0) { sys.position[NutsAndBolts.X_AXIS]--; }
        else { sys.position[NutsAndBolts.X_AXIS]++; }
      }
      if (ADAPTIVE_MULTI_AXIS_STEP_SMOOTHING)
        st.counter_y += st.steps[NutsAndBolts.Y_AXIS];
      else
        st.counter_y += st_block_buffer[st.exec_block_index].steps[NutsAndBolts.Y_AXIS];
      
      if (st.counter_y > st_block_buffer[st.exec_block_index].step_event_count) {
        st.step_outbits |= (1<<Y_STEP_BIT);
        st.counter_y -= st_block_buffer[st.exec_block_index].step_event_count;
        if ((st_block_buffer[st.exec_block_index].direction_bits & (1<<Y_DIRECTION_BIT))!=0) { sys.position[NutsAndBolts.Y_AXIS]--; }
        else { sys.position[NutsAndBolts.Y_AXIS]++; }
      }
      if (ADAPTIVE_MULTI_AXIS_STEP_SMOOTHING)
        st.counter_z += st.steps[NutsAndBolts.Z_AXIS];
      else
        st.counter_z += st_block_buffer[st.exec_block_index].steps[NutsAndBolts.Z_AXIS];
      
      if (st.counter_z > st_block_buffer[st.exec_block_index].step_event_count) {
        st.step_outbits |= (1<<Z_STEP_BIT);
        st.counter_z -= st_block_buffer[st.exec_block_index].step_event_count;
        if ((st_block_buffer[st.exec_block_index].direction_bits & (1<<Z_DIRECTION_BIT))!=0) { sys.position[NutsAndBolts.Z_AXIS]--; }
        else { sys.position[NutsAndBolts.Z_AXIS]++; }
      }  

      // During a homing cycle, lock out and prevent desired axes from moving.
      if (sys.state == STATE_HOMING) { st.step_outbits &= sys.homing_axis_lock; }   

      st.step_count--; // Decrement step events count 
      if (st.step_count == 0) {
        // Segment is complete. Discard current segment and advance segment indexing.
        st.exec_segmentIdx = -1;
        if ( ++segment_buffer_tail == SEGMENT_BUFFER_SIZE) { segment_buffer_tail = 0; }
      }

      st.step_outbits ^= step_port_invert_mask;  // Apply step port invert mask    
      busy = false;
    // SPINDLE_ENABLE_PORT ^= 1<<SPINDLE_ENABLE_BIT; // Debug: Used to time ISR
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
    //TODO
    //ISR(TIMER0_OVF_vect)
    //{
    //  // Reset stepping pins (leave the direction pins)
    //  STEP_PORT = (STEP_PORT & ~STEP_MASK) | (step_port_invert_mask & STEP_MASK); 
    //  //TODO TCCR0B = 0; // Disable Timer0 to prevent re-entering this interrupt when it's not needed. 
    //}

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


    // Generates the step and direction port invert masks used in the Stepper Interrupt Driver.
    public void st_generate_step_dir_invert_masks()
    {
      byte idx;
      step_port_invert_mask = 0;
      dir_port_invert_mask = 0;
      for (idx = 0; idx < NutsAndBolts.N_AXIS; idx++)
      {
        if (bit_istrue(settings.step_invert_mask, bit(idx))) { step_port_invert_mask |= get_step_pin_mask(idx); }
        if (bit_istrue(settings.dir_invert_mask, bit(idx))) { dir_port_invert_mask |= get_direction_pin_mask(idx); }
      }
    }


    // Reset and clear stepper subsystem variables
    public void st_reset()
    {
      // Initialize stepper driver idle state.
      st_go_idle();

      // Initialize stepper algorithm variables.
      //TODO
      //memset(&st, 0, sizeof(st));
      prep = new st_prep_t();
      st = new stepper_t(true);
      st.exec_segmentIdx = -1;
      pl_blockIdx = -1;  // Planner block pointer used by segment buffer
      segment_buffer_tail = 0;
      segment_buffer_head = 0; // empty = tail
      segment_next_head = 1;
      busy = false;

      st_generate_step_dir_invert_masks();

      // Initialize step and direction port pins.
      STEP_PORT = (STEP_PORT & ~STEP_MASK) | step_port_invert_mask;
      DIRECTION_PORT = (DIRECTION_PORT & ~DIRECTION_MASK) | dir_port_invert_mask;

      RaiseStepperSegmentBufferChanged();
    }


    // Initialize and start the stepper motor subsystem
    public void stepper_init()
    {
      // Configure step and direction interface pins
      STEP_DDR |= STEP_MASK;
      STEPPERS_DISABLE_DDR |= 1 << STEPPERS_DISABLE_BIT;
      DIRECTION_DDR |= DIRECTION_MASK;

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

    // Called by planner_recalculate() when the executing block is updated by the new plan.
    public void st_update_plan_block_parameters()
    {
      if (pl_blockIdx != -1)
      { // Ignore if at start of a new block.
        prep.flag_partial_block = 1;
        block_buffer[pl_blockIdx].entry_speed_sqr = prep.current_speed * prep.current_speed; // Update entry speed.
        //pl_block = null; // Flag st_prep_segment() to load new velocity profile.
        pl_blockIdx = -1;
      }
    }


    /* Prepares step segment buffer. Continuously called from main program. 

       The segment buffer is an intermediary buffer interface between the execution of steps
       by the stepper algorithm and the velocity profiles generated by the planner. The stepper
       algorithm only executes steps within the segment buffer and is filled by the main program
       when steps are "checked-out" from the first block in the planner buffer. This keeps the
       step execution and planning optimization processes atomic and protected from each other.
       The number of steps "checked-out" from the planner buffer and the number of segments in
       the segment buffer is sized and computed such that no operation in the main program takes
       longer than the time it takes the stepper algorithm to empty it before refilling it. 
       Currently, the segment buffer conservatively holds roughly up to 40-50 msec of steps.
       NOTE: Computation units are in steps, millimeters, and minutes.
    */
    public void st_prep_buffer()
    {
      while (segment_buffer_tail != segment_next_head)
      { // Check if we need to fill the buffer.

        // Determine if we need to load a new planner block or if the block has been replanned. 
        if (pl_blockIdx == -1)
        {
          pl_blockIdx = plan_get_current_block(); // Query planner for a queued block
          if (pl_blockIdx == -1) { return; } // No planner blocks. Exit.

          // Check if the segment buffer completed the last planner block. If so, load the Bresenham
          // data for the block. If not, we are still mid-block and the velocity profile was updated. 
          if (prep.flag_partial_block != 0)
          {
            prep.flag_partial_block = 0; // Reset flag
          }
          else
          {
            // Increment stepper common data index to store new planner block data. 
            if (++prep.st_block_index == (SEGMENT_BUFFER_SIZE - 1)) { prep.st_block_index = 0; }

            // Prepare and copy Bresenham algorithm segment data from the new planner block, so that
            // when the segment buffer completes the planner block, it may be discarded when the 
            // segment buffer finishes the prepped block, but the stepper ISR is still executing it. 
            st_block_buffer[prep.st_block_index].direction_bits = block_buffer[pl_blockIdx].direction_bits;
            if (!ADAPTIVE_MULTI_AXIS_STEP_SMOOTHING)
            {
              st_block_buffer[prep.st_block_index].steps[NutsAndBolts.X_AXIS] = block_buffer[pl_blockIdx].steps[NutsAndBolts.X_AXIS];
              st_block_buffer[prep.st_block_index].steps[NutsAndBolts.Y_AXIS] = block_buffer[pl_blockIdx].steps[NutsAndBolts.Y_AXIS];
              st_block_buffer[prep.st_block_index].steps[NutsAndBolts.Z_AXIS] = block_buffer[pl_blockIdx].steps[NutsAndBolts.Z_AXIS];
              st_block_buffer[prep.st_block_index].step_event_count = block_buffer[pl_blockIdx].step_event_count;
            }
            else
            {
              // With AMASS enabled, simply bit-shift multiply all Bresenham data by the max AMASS 
              // level, such that we never divide beyond the original data anywhere in the algorithm.
              // If the original data is divided, we can lose a step from integer roundoff.
              st_block_buffer[prep.st_block_index].steps[NutsAndBolts.X_AXIS] = block_buffer[pl_blockIdx].steps[NutsAndBolts.X_AXIS] << MAX_AMASS_LEVEL;
              st_block_buffer[prep.st_block_index].steps[NutsAndBolts.Y_AXIS] = block_buffer[pl_blockIdx].steps[NutsAndBolts.Y_AXIS] << MAX_AMASS_LEVEL;
              st_block_buffer[prep.st_block_index].steps[NutsAndBolts.Z_AXIS] = block_buffer[pl_blockIdx].steps[NutsAndBolts.Z_AXIS] << MAX_AMASS_LEVEL;
              st_block_buffer[prep.st_block_index].step_event_count = block_buffer[pl_blockIdx].step_event_count << MAX_AMASS_LEVEL;
            }
            // Initialize segment buffer data for generating the segments.
            prep.steps_remaining = block_buffer[pl_blockIdx].step_event_count;
            prep.step_per_mm = prep.steps_remaining / block_buffer[pl_blockIdx].millimeters;
            prep.req_mm_increment = REQ_MM_INCREMENT_SCALAR / prep.step_per_mm;

            prep.dt_remainder = 0.0f; // Reset for new planner block

            if (sys.state == STATE_HOLD)
            {
              // Override planner block entry speed and enforce deceleration during feed hold.
              prep.current_speed = prep.exit_speed;
              block_buffer[pl_blockIdx].entry_speed_sqr = prep.exit_speed * prep.exit_speed;
            }
            else { prep.current_speed = (float)System.Math.Sqrt(block_buffer[pl_blockIdx].entry_speed_sqr); }
          }

          /* --------------------------------------------------------------------------------- 
             Compute the velocity profile of a new planner block based on its entry and exit
             speeds, or recompute the profile of a partially-completed planner block if the 
             planner has updated it. For a commanded forced-deceleration, such as from a feed 
             hold, override the planner velocities and decelerate to the target exit speed.
          */
          prep.mm_complete = 0.0f; // Default velocity profile complete at 0.0mm from end of block.
          float inv_2_accel = 0.5f / block_buffer[pl_blockIdx].acceleration;
          if (sys.state == STATE_HOLD)
          { // [Forced Deceleration to Zero Velocity]
            // Compute velocity profile parameters for a feed hold in-progress. This profile overrides
            // the planner block profile, enforcing a deceleration to zero speed.
            prep.ramp_type = RAMP_DECEL;
            // Compute decelerate distance relative to end of block.
            float decel_dist = block_buffer[pl_blockIdx].millimeters - inv_2_accel * block_buffer[pl_blockIdx].entry_speed_sqr;
            if (decel_dist < 0.0)
            {
              // Deceleration through entire planner block. End of feed hold is not in this block.
              prep.exit_speed = (float)System.Math.Sqrt(block_buffer[pl_blockIdx].entry_speed_sqr - 2 * block_buffer[pl_blockIdx].acceleration * block_buffer[pl_blockIdx].millimeters);
            }
            else
            {
              prep.mm_complete = decel_dist; // End of feed hold.
              prep.exit_speed = 0.0f;
            }
          }
          else
          { // [Normal Operation]
            // Compute or recompute velocity profile parameters of the prepped planner block.
            prep.ramp_type = RAMP_ACCEL; // Initialize as acceleration ramp.
            prep.accelerate_until = block_buffer[pl_blockIdx].millimeters;
            prep.exit_speed = plan_get_exec_block_exit_speed();
            float exit_speed_sqr = prep.exit_speed * prep.exit_speed;
            float intersect_distance =
                    0.5f * (block_buffer[pl_blockIdx].millimeters + inv_2_accel * (block_buffer[pl_blockIdx].entry_speed_sqr - exit_speed_sqr));
            if (intersect_distance > 0.0f)
            {
              if (intersect_distance < block_buffer[pl_blockIdx].millimeters)
              { // Either trapezoid or triangle types
                // NOTE: For acceleration-cruise and cruise-only types, following calculation will be 0.0.
                prep.decelerate_after = inv_2_accel * (block_buffer[pl_blockIdx].nominal_speed_sqr - exit_speed_sqr);
                if (prep.decelerate_after < intersect_distance)
                { // Trapezoid type
                  prep.maximum_speed = (float)System.Math.Sqrt(block_buffer[pl_blockIdx].nominal_speed_sqr);
                  if (block_buffer[pl_blockIdx].entry_speed_sqr == block_buffer[pl_blockIdx].nominal_speed_sqr)
                  {
                    // Cruise-deceleration or cruise-only type.
                    prep.ramp_type = RAMP_CRUISE;
                  }
                  else
                  {
                    // Full-trapezoid or acceleration-cruise types
                    prep.accelerate_until -= inv_2_accel * (block_buffer[pl_blockIdx].nominal_speed_sqr - block_buffer[pl_blockIdx].entry_speed_sqr);
                  }
                }
                else
                { // Triangle type
                  prep.accelerate_until = intersect_distance;
                  prep.decelerate_after = intersect_distance;
                  prep.maximum_speed = (float)System.Math.Sqrt(2.0 * block_buffer[pl_blockIdx].acceleration * intersect_distance + exit_speed_sqr);
                }
              }
              else
              { // Deceleration-only type
                prep.ramp_type = RAMP_DECEL;
                // prep.decelerate_after = block_buffer[pl_blockIdx].millimeters;
                prep.maximum_speed = prep.current_speed;
              }
            }
            else
            { // Acceleration-only type
              prep.accelerate_until = 0.0f;
              // prep.decelerate_after = 0.0;
              prep.maximum_speed = prep.exit_speed;
            }
          }
        }

        // Initialize new segment
        fixed (segment_t* prep_segment = &segment_buffer[segment_buffer_head])
        {


          // Set new segment to point to the current segment data block.
          prep_segment->st_block_index = prep.st_block_index;

          /*------------------------------------------------------------------------------------
              Compute the average velocity of this new segment by determining the total distance
            traveled over the segment time DT_SEGMENT. The following code first attempts to create 
            a full segment based on the current ramp conditions. If the segment time is incomplete 
            when terminating at a ramp state change, the code will continue to loop through the
            progressing ramp states to fill the remaining segment execution time. However, if 
            an incomplete segment terminates at the end of the velocity profile, the segment is 
            considered completed despite having a truncated execution time less than DT_SEGMENT.
              The velocity profile is always assumed to progress through the ramp sequence:
            acceleration ramp, cruising state, and deceleration ramp. Each ramp's travel distance
            may range from zero to the length of the block. Velocity profiles can end either at 
            the end of planner block (typical) or mid-block at the end of a forced deceleration, 
            such as from a feed hold.
          */
          float dt_max = DT_SEGMENT; // Maximum segment time
          float dt = 0.0f; // Initialize segment time
          float time_var = dt_max; // Time worker variable
          float mm_var; // mm-Distance worker variable
          float speed_var; // Speed worker variable   
          float mm_remaining = block_buffer[pl_blockIdx].millimeters; // New segment distance from end of block.
          float minimum_mm = mm_remaining - prep.req_mm_increment; // Guarantee at least one step.
          if (minimum_mm < 0.0) { minimum_mm = 0.0f; }

          do
          {
            switch (prep.ramp_type)
            {
              case RAMP_ACCEL:
                // NOTE: Acceleration ramp only computes during first do-while loop.
                speed_var = block_buffer[pl_blockIdx].acceleration * time_var;
                mm_remaining -= time_var * (prep.current_speed + 0.5f * speed_var);
                if (mm_remaining < prep.accelerate_until)
                { // End of acceleration ramp.
                  // Acceleration-cruise, acceleration-deceleration ramp junction, or end of block.
                  mm_remaining = prep.accelerate_until; // NOTE: 0.0 at EOB
                  time_var = 2.0f * (block_buffer[pl_blockIdx].millimeters - mm_remaining) / (prep.current_speed + prep.maximum_speed);
                  if (mm_remaining == prep.decelerate_after) { prep.ramp_type = RAMP_DECEL; }
                  else { prep.ramp_type = RAMP_CRUISE; }
                  prep.current_speed = prep.maximum_speed;
                }
                else
                { // Acceleration only. 
                  prep.current_speed += speed_var;
                }
                break;
              case RAMP_CRUISE:
                // NOTE: mm_var used to retain the last mm_remaining for incomplete segment time_var calculations.
                // NOTE: If maximum_speed*time_var value is too low, round-off can cause mm_var to not change. To 
                //   prevent this, simply enforce a minimum speed threshold in the planner.
                mm_var = mm_remaining - prep.maximum_speed * time_var;
                if (mm_var < prep.decelerate_after)
                { // End of cruise. 
                  // Cruise-deceleration junction or end of block.
                  time_var = (mm_remaining - prep.decelerate_after) / prep.maximum_speed;
                  mm_remaining = prep.decelerate_after; // NOTE: 0.0 at EOB
                  prep.ramp_type = RAMP_DECEL;
                }
                else
                { // Cruising only.         
                  mm_remaining = mm_var;
                }
                break;
              default: // case RAMP_DECEL:
                // NOTE: mm_var used as a misc worker variable to prevent errors when near zero speed.
                speed_var = block_buffer[pl_blockIdx].acceleration * time_var; // Used as delta speed (mm/min)
                if (prep.current_speed > speed_var)
                { // Check if at or below zero speed.
                  // Compute distance from end of segment to end of block.
                  mm_var = mm_remaining - time_var * (prep.current_speed - 0.5f * speed_var); // (mm)
                  if (mm_var > prep.mm_complete)
                  { // Deceleration only.
                    mm_remaining = mm_var;
                    prep.current_speed -= speed_var;
                    break; // Segment complete. Exit switch-case statement. Continue do-while loop.
                  }
                } // End of block or end of forced-deceleration.
                time_var = 2.0f * (mm_remaining - prep.mm_complete) / (prep.current_speed + prep.exit_speed);
                mm_remaining = prep.mm_complete;
                break;
            }
            dt += time_var; // Add computed ramp time to total segment time.
            if (dt < dt_max) { time_var = dt_max - dt; } // **Incomplete** At ramp junction.
            else
            {
              if (mm_remaining > minimum_mm)
              { // Check for very slow segments with zero steps.
                // Increase segment time to ensure at least one step in segment. Override and loop
                // through distance calculations until minimum_mm or mm_complete.
                dt_max += DT_SEGMENT;
                time_var = dt_max - dt;
              }
              else
              {
                break; // **Complete** Exit loop. Segment execution time maxed.
              }
            }
          } while (mm_remaining > prep.mm_complete); // **Complete** Exit loop. Profile complete.


          /* -----------------------------------------------------------------------------------
             Compute segment step rate, steps to execute, and apply necessary rate corrections.
             NOTE: Steps are computed by direct scalar conversion of the millimeter distance 
             remaining in the block, rather than incrementally tallying the steps executed per
             segment. This helps in removing floating point round-off issues of several additions. 
             However, since floats have only 7.2 significant digits, long moves with extremely 
             high step counts can exceed the precision of floats, which can lead to lost steps.
             Fortunately, this scenario is highly unlikely and unrealistic in CNC machines
             supported by Grbl (i.e. exceeding 10 meters axis travel at 200 step/mm).
          */
          float steps_remaining = prep.step_per_mm * mm_remaining; // Convert mm_remaining to steps
          short n_steps_remaining = (short)System.Math.Ceiling(steps_remaining); // Round-up current steps remaining
          short last_n_steps_remaining = (short)System.Math.Ceiling(prep.steps_remaining); // Round-up last steps remaining
          prep_segment->n_step = (short)(last_n_steps_remaining - n_steps_remaining); // Compute number of steps to execute.

          // Bail if we are at the end of a feed hold and don't have a step to execute.
          if (prep_segment->n_step == 0)
          {
            if (sys.state == STATE_HOLD)
            {

              // Less than one step to decelerate to zero speed, but already very close. AMASS 
              // requires full steps to execute. So, just bail.
              prep.current_speed = 0.0f;
              prep.dt_remainder = 0.0f;
              prep.steps_remaining = n_steps_remaining;
              block_buffer[pl_blockIdx].millimeters = prep.steps_remaining / prep.step_per_mm; // Update with full steps.
              plan_cycle_reinitialize();
              sys.state = STATE_QUEUED;
              return; // Segment not generated, but current step data still retained.
            }
          }

          // Compute segment step rate. Since steps are integers and mm distances traveled are not,
          // the end of every segment can have a partial step of varying magnitudes that are not 
          // executed, because the stepper ISR requires whole steps due to the AMASS algorithm. To
          // compensate, we track the time to execute the previous segment's partial step and simply
          // apply it with the partial step distance to the current segment, so that it minutely
          // adjusts the whole segment rate to keep step output exact. These rate adjustments are 
          // typically very small and do not adversely effect performance, but ensures that Grbl
          // outputs the exact acceleration and velocity profiles as computed by the planner.
          dt += prep.dt_remainder; // Apply previous segment partial step execute time
          float inv_rate = dt / (last_n_steps_remaining - steps_remaining); // Compute adjusted step rate inverse
          prep.dt_remainder = (n_steps_remaining - steps_remaining) * inv_rate; // Update segment partial step time

          // Compute CPU cycles per step for the prepped segment.
          uint cycles = (uint)System.Math.Ceiling((NutsAndBolts.TICKS_PER_MICROSECOND * 1000000.0 * 60) * inv_rate); // (cycles/step)    

          if (ADAPTIVE_MULTI_AXIS_STEP_SMOOTHING)
          {
            // Compute step timing and multi-axis smoothing level.
            // NOTE: AMASS overdrives the timer with each level, so only one prescalar is required.
            if (cycles < AMASS_LEVEL1) { prep_segment->amass_level = 0; }
            else
            {
              if (cycles < AMASS_LEVEL2) { prep_segment->amass_level = 1; }
              else if (cycles < AMASS_LEVEL3) { prep_segment->amass_level = 2; }
              else { prep_segment->amass_level = 3; }
              cycles >>= prep_segment->amass_level;
              prep_segment->n_step <<= prep_segment->amass_level;
            }
            if (cycles < (1 << 16)) { prep_segment->cycles_per_tick = (ushort)cycles; } // < 65536 (4.1ms @ 16MHz)
            else { prep_segment->cycles_per_tick = 0xffff; } // Just set the slowest speed possible.
          }
          else
          {
            // Compute step timing and timer prescalar for normal step generation.
            if (cycles < (1 << 16))
            { // < 65536  (4.1ms @ 16MHz)
              prep_segment->prescaler = 1; // prescaler: 0
              prep_segment->cycles_per_tick = (ushort)cycles;
            }
            else if (cycles < (1UL << 19))
            { // < 524288 (32.8ms@16MHz)
              prep_segment->prescaler = 2; // prescaler: 8
              prep_segment->cycles_per_tick = (ushort)(cycles >> 3);
            }
            else
            {
              prep_segment->prescaler = 3; // prescaler: 64
              if (cycles < (1UL << 22))
              { // < 4194304 (262ms@16MHz)
                prep_segment->cycles_per_tick = (ushort)(cycles >> 6);
              }
              else
              { // Just set the slowest speed possible. (Around 4 step/sec.)
                prep_segment->cycles_per_tick = 0xffff;
              }
            }
          }


          // Segment complete! Increment segment buffer indices.
          segment_buffer_head = segment_next_head;
          if (++segment_next_head == SEGMENT_BUFFER_SIZE) { segment_next_head = 0; }
          RaiseStepperSegmentBufferChanged();

          // Setup initial conditions for next segment.
          if (mm_remaining > prep.mm_complete)
          {
            // Normal operation. Block incomplete. Distance remaining in block to be executed.
            block_buffer[pl_blockIdx].millimeters = mm_remaining;
            prep.steps_remaining = steps_remaining;
          }
          else
          {
            // End of planner block or forced-termination. No more distance to be executed.
            if (mm_remaining > 0.0)
            { // At end of forced-termination.
              // Reset prep parameters for resuming and then bail.
              // NOTE: Currently only feed holds qualify for this scenario. May change with overrides.       
              prep.current_speed = 0.0f;
              prep.dt_remainder = 0.0f;
              prep.steps_remaining = (float)System.Math.Ceiling(steps_remaining);
              block_buffer[pl_blockIdx].millimeters = prep.steps_remaining / prep.step_per_mm; // Update with full steps.
              plan_cycle_reinitialize();
              sys.state = STATE_QUEUED; // End cycle.        

              return; // Bail!
              // TODO: Try to move QUEUED setting into cycle re-initialize.

            }
            else
            { // End of planner block
              // The planner block is complete. All steps are set to be executed in the segment buffer.
              //pl_block = null;
              pl_blockIdx = -1;
              plan_discard_current_block();
            }
          }
        }
      }
    }


    // Called by runtime status reporting to fetch the current speed being executed. This value
    // however is not exactly the current speed, but the speed computed in the last step segment
    // in the segment buffer. It will always be behind by up to the number of segment blocks (-1)
    // divided by the ACCELERATION TICKS PER SECOND in seconds. 
    //#ifdef REPORT_REALTIME_RATE
    float st_get_realtime_rate()
    {
      if ((sys.state & (STATE_CYCLE | STATE_HOMING | STATE_HOLD)) != 0)
      {
        return prep.current_speed;
      }
      return 0.0f;
    }
    //#endif
  }
}