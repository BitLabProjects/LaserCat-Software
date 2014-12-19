using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using bitLab.Logging;
using System.Collections.Concurrent;

namespace bitLab.LaserCat.Grbl
{
	public partial class GrblFirmware
	{
		public const int LINE_BUFFER_SIZE = 80;

		char[] line = new char[LINE_BUFFER_SIZE]; // Line to be executed. Zero-terminated.

		// Directs and executes one line of formatted input from protocol_process. While mostly
		// incoming streaming g-code blocks, this also directs and executes Grbl internal commands,
		// such as settings, initiating the homing cycle, and toggling switch states.
		public void protocol_execute_line(string line)
		{
			//protocol_execute_runtime(); // Runtime command check point.
			//if (sys.abort != 0) { return; } // Bail to calling function upon system abort  

			if (string.IsNullOrWhiteSpace(line))
			{
				// Empty or comment line. Send status message for syncing purposes.
				report_status_message(STATUS_OK);
			}
			else
			{
				// Parse and execute g-code block!
				report_status_message(mGCode.gc_execute_line(line));
			}
		}

    public enum EGrblMessage
    {
      LoadGCode
    }
    private struct TGrblMessage
    {
      public EGrblMessage Message;
      public object Param0;
    }
    private BlockingCollection<TGrblMessage> mMessageQueue;

    public void SendMessage(EGrblMessage message, object param0)
    {
      mMessageQueue.Add(new TGrblMessage() { Message = message, Param0 = param0 });
    }

    public void protocol_main_loop()
    {
      mMessageQueue = new BlockingCollection<TGrblMessage>();
      initGrblState();

      try
      {
        while (true)
          handleMessage(mMessageQueue.Take());
      }
      catch (InvalidOperationException)
      {
      }
    }

    private void handleMessage(TGrblMessage msg)
    {
      switch (msg.Message)
      {
        case EGrblMessage.LoadGCode:
          loadGCode(msg.Param0 as List<string>);
          break;
      }
    }

    private enum EGrblState
    {
      Idle,
      GCodeLoaded
    }
    private EGrblState mState;

    private void initGrblState()
    {
      changeState(EGrblState.Idle);
      Log.LogInfo("Grbl initialized");
    }
    private void changeState(EGrblState newState)
    {
      mState = newState;
      Log.LogInfo("State changed to " + newState.ToString());
    }

    private bool checkAllowedEntryState(EGrblState[] allowedStates)
    {
      if (allowedStates.Contains(mState))
        return true;
      Log.LogInfo("Operation not allowed in this state");
      return false;
    }

    private void loadGCode(List<string> GCodeLines)
    {
      if (!checkAllowedEntryState(new EGrblState[] { EGrblState.Idle, EGrblState.GCodeLoaded }))
        return;

      Log.LogInfo("Resetting planner and parsing GCode...");
      plan_reset();
      foreach(var line in GCodeLines)
        mGCode.gc_execute_line(line);
      Log.LogInfo("Parsing GCode completed:");
      Log.LogInfo(string.Format("- Parsed {0} GCode lines", GCodeLines.Count));
      Log.LogInfo(string.Format("- Planned {0} segments", plan_get_block_buffer_count()));
      changeState(EGrblState.GCodeLoaded);
    }

		/* 
			GRBL PRIMARY LOOP:
		*/
		public void protocol_main_loop_old()
		{
			// ------------------------------------------------------------
			// Complete initialization procedures upon a power-up or reset.
			// ------------------------------------------------------------

			// Print welcome message   
			report_init_message();

			/* TODO // Check for and report alarm state after a reset, error, or an initial power up.
			if (sys.state == STATE_ALARM)
			{
				report_feedback_message(MESSAGE_ALARM_LOCK);
			}
			else
			{
				// All systems go!
				sys.setState(STATE_IDLE); // Set system to ready. Clear all state flags.
				system_execute_startup(line); // Execute startup script.
			}*/

			// ---------------------------------------------------------------------------------  
			// Primary loop! Upon a system abort, this exits back to main() to reset the system. 
			// ---------------------------------------------------------------------------------  

      readSerialReset();
			for (; ; )
			{

				// Process one line of incoming serial data, as the data becomes available. Performs an
				// initial filtering by removing spaces and comments and capitalizing all letters.

				// NOTE: While comment, spaces, and block delete(if supported) handling should technically 
				// be done in the g-code parser, doing it here helps compress the incoming data into Grbl's
				// line buffer, which is limited in size. The g-code standard actually states a line can't
				// exceed 256 characters, but the Arduino Uno does not have the memory space for this.
				// With a better processor, it would be very easy to pull this initial parsing out as a 
				// seperate task to be shared by the g-code parser and Grbl's system commands.
        //if (readGCodeLineOrNull())
        //{
        //  protocol_execute_line(line); // Line is complete. Execute it!
        //}				

				// If there are no more characters in the serial read buffer to be processed and executed,
				// this indicates that g-code streaming has either filled the planner buffer or has 
				// completed. In either case, auto-cycle start, if enabled, any queued moves.
				//protocol_auto_cycle_start();

				if (mLastTime == 0)
					mLastTime = mTimer.Value;

				if (mTimer.Value - mLastTime > mTimer.Frequency)
				{
					var position = mLaserCatHardware.AskPosition();
					sys.position[0] = position[0];
					sys.position[1] = position[1];
					sys.position[2] = position[2];
					mLastTime = mTimer.Value;
					protocol_execute_runtime();  // Runtime command check point.
				}

				if (sys.abort != 0) { return; } // Bail to main() program loop to reset system.
			}
		}

    bool iscomment = false;
    byte char_counter = 0;
    private void readSerialReset()
    {
      iscomment = false;
      char_counter = 0;
    }
    private string readGCodeLineOrNull()
    {
      while (mSerialPort.HasByte)
      {
        byte c = mSerialPort.ReadByte();
        if ((c == '\n') || (c == '\r'))
        { // End of line reached
          line[char_counter] = '\0'; // Set string termination character.
          iscomment = false;
          char_counter = 0;
          return new string(line);
        }
        else
        {
          if (iscomment)
          {
            // Throw away all comment characters
            if (c == ')')
            {
              // End of comment. Resume line.
              iscomment = false;
            }
          }
          else
          {
            if (c <= ' ')
            {
              // Throw away whitepace and control characters  
            }
            else if (c == '/')
            {
              // Block delete NOT SUPPORTED. Ignore character.
              // NOTE: If supported, would simply need to check the system if block delete is enabled.
            }
            else if (c == '(')
            {
              // Enable comments flag and ignore all characters until ')' or EOL.
              // NOTE: This doesn't follow the NIST definition exactly, but is good enough for now.
              // In the future, we could simply remove the items within the comments, but retain the
              // comment control characters, so that the g-code parser can error-check it.
              iscomment = true;
              // } else if (c == ';') {
              // Comment character to EOL NOT SUPPORTED. LinuxCNC definition. Not NIST.

              // TODO: Install '%' feature 
              // } else if (c == '%') {
              // Program start-end percent sign NOT SUPPORTED.
              // NOTE: This maybe installed to tell Grbl when a program is running vs manual input,
              // where, during a program, the system auto-cycle start will continue to execute 
              // everything until the next '%' sign. This will help fix resuming issues with certain
              // functions that empty the planner buffer to execute its task on-time.

            }
            else if (char_counter >= (LINE_BUFFER_SIZE - 1))
            {
              // Detect line buffer overflow. Report error and reset line buffer.
              report_status_message(STATUS_OVERFLOW);
              iscomment = false;
              char_counter = 0;
            }
            else if (c >= 'a' && c <= 'z')
            { // Upcase lowercase
              line[char_counter++] = Convert.ToChar(c - Convert.ToInt32('a') + Convert.ToInt32('A'));
            }
            else
            {
              line[char_counter++] = Convert.ToChar(c);
            }
          }
        }
      }
      return null;
    }

		HiResTimer mTimer = new HiResTimer();
		Int64 mLastTime;

		// Executes run-time commands, when required. This is called from various check points in the main
		// program, primarily where there may be a while loop waiting for a buffer to clear space or any
		// point where the execution time from the last check point may be more than a fraction of a second.
		// This is a way to execute runtime commands asynchronously (aka multitasking) with grbl's g-code
		// parsing and planning functions. This function also serves as an interface for the interrupts to 
		// set the system runtime flags, where only the main program handles them, removing the need to
		// define more computationally-expensive volatile variables. This also provides a controlled way to 
		// execute certain tasks without having two or more instances of the same task, such as the planner
		// recalculating the buffer upon a feedhold or override.
		// NOTE: The sys.execute variable flags are set by any process, step or serial interrupts, pinouts,
		// limit switches, or the main program.
		public void protocol_execute_runtime()
		{
      //SB!Runtime commands disabled
      //return;

			byte rt_exec = sys.execute; // Copy to avoid calling volatile multiple times
			if (rt_exec != 0)
			{ // Enter only if any bit flag is true

        // Execute a feed hold with deceleration, only during cycle.
				if ((rt_exec & EXEC_FEED_HOLD) != 0)
				{
					// !!! During a cycle, the segment buffer has just been reloaded and full. So the math involved
					// with the feed hold should be fine for most, if not all, operational scenarios.
					if (sys.state == STATE_CYCLE)
					{
						sys.setState(STATE_HOLD);
						st_update_plan_block_parameters();
						st_prep_buffer();
						sys.auto_start = 0; // Disable planner auto start upon feed hold.
					}
					bit_false_atomic(ref sys.execute, EXEC_FEED_HOLD);
				}

				// Execute a cycle start by starting the stepper interrupt begin executing the blocks in queue.
				if ((rt_exec & EXEC_CYCLE_START) != 0)
				{
					if (sys.state == STATE_QUEUED)
					{
						sys.setState(STATE_CYCLE);
						st_prep_buffer(); // Initialize step segment buffer before beginning cycle.
						st_wake_up();
						if (bit_istrue(settings.flags, BITFLAG_AUTO_START))
						{
							sys.auto_start = 1; // Re-enable auto start after feed hold.
						}
						else
						{
							sys.auto_start = 0; // Reset auto start per settings.
						}
					}
					bit_false_atomic(ref sys.execute, EXEC_CYCLE_START);
				}

				// Reinitializes the cycle plan and stepper system after a feed hold for a resume. Called by 
				// runtime command execution in the main program, ensuring that the planner re-plans safely.
				// NOTE: Bresenham algorithm variables are still maintained through both the planner and stepper
				// cycle reinitializations. The stepper path should continue exactly as if nothing has happened.   
				// NOTE: EXEC_CYCLE_STOP is set by the stepper subsystem when a cycle or feed hold completes.
				if ((rt_exec & EXEC_CYCLE_STOP) != 0)
				{
					if (plan_get_current_block() != -1) { sys.setState(STATE_QUEUED); }
					else { sys.setState(STATE_IDLE); }
					bit_false_atomic(ref sys.execute, EXEC_CYCLE_STOP);
				}

			}

			// Overrides flag byte (sys.override) and execution should be installed here, since they 
			// are runtime and require a direct and controlled interface to the main stepper program.

			// Reload step segment buffer
			if ((sys.state & (STATE_CYCLE | STATE_HOLD | STATE_HOMING)) != 0) { st_prep_buffer(); }
		}


		// Block until all buffered steps are executed or in a cycle state. Works with feed hold
		// during a synchronize call, if it should happen. Also, waits for clean cycle end.
		public void protocol_buffer_synchronize()
		{
			// If system is queued, ensure cycle resumes if the auto start flag is present.
			protocol_auto_cycle_start();
			// Check and set auto start to resume cycle after synchronize and caller completes.
			if (sys.state == STATE_CYCLE) { sys.auto_start = 1; }
			while (plan_get_current_block() != -1 || (sys.state == STATE_CYCLE))
			{
				protocol_execute_runtime();   // Check and execute run-time commands
				if (sys.abort != 0) { return; } // Check for system abort
			}
		}


		// Auto-cycle start has two purposes: 1. Resumes a plan_synchronize() call from a function that
		// requires the planner buffer to empty (spindle enable, dwell, etc.) 2. As a user setting that 
		// automatically begins the cycle when a user enters a valid motion command manually. This is 
		// intended as a beginners feature to help new users to understand g-code. It can be disabled
		// as a beginner tool, but (1.) still operates. If disabled, the operation of cycle start is
		// manually issuing a cycle start command whenever the user is ready and there is a valid motion 
		// command in the planner queue.
		// NOTE: This function is called from the main loop and mc_line() only and executes when one of
		// two conditions exist respectively: There are no more blocks sent (i.e. streaming is finished, 
		// single commands), or the planner buffer is full and ready to go.
		public void protocol_auto_cycle_start() { if (sys.auto_start != 0) { bit_true_atomic(ref sys.execute, EXEC_CYCLE_START); } }

	}
}
