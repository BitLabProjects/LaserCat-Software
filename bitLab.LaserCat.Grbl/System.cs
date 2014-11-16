using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public void system_init() 
    {
      PINOUT_DDR &= ~(PINOUT_MASK); // Configure as input pins
      PINOUT_PORT |= PINOUT_MASK;   // Enable internal pull-up resistors. Normal high operation.
      PINOUT_PCMSK |= PINOUT_MASK;  // Enable specific pins of the Pin Change Interrupt
      //TODO PCICR |= (1 << PINOUT_INT);   // Enable Pin Change Interrupt
    }


    // Pin change interrupt for pin-out commands, i.e. cycle start, feed hold, and reset. Sets
    // only the runtime command execute variable to have the main program execute these when 
    // its ready. This works exactly like the character-based runtime commands when picked off
    // directly from the incoming serial data stream.
    //ISR(PINOUT_INT_vect)
    //TODO 
    public void PINOUT_INT_vect()
    {
      // Enter only if any pinout pin is actively low.
      if (((PINOUT_PIN & PINOUT_MASK) ^ PINOUT_MASK) != 0)
      {
        if (bit_isfalse(PINOUT_PIN, bit(PIN_RESET)))
        {
          mc_reset();
        }
        else if (bit_isfalse(PINOUT_PIN, bit(PIN_FEED_HOLD)))
        {
          bit_true(ref sys.execute, EXEC_FEED_HOLD);
        }
        else if (bit_isfalse(PINOUT_PIN, bit(PIN_CYCLE_START)))
        {
          bit_true(ref sys.execute, EXEC_CYCLE_START);
        } 
      }
    }


    // Executes user startup script, if stored.
    public void system_execute_startup(char[] line) 
    {
      byte n;
      for (n=0; n < N_STARTUP_LINE; n++) {
        if (!(settings_read_startup_line(n, line))) {
          report_status_message(STATUS_SETTING_READ_FAIL);
        } else {
          if (line[0] != 0) {
            printPgmString(new String(line)); // Echo startup line to indicate execution.
            report_status_message(mGCode.gc_execute_line(line));
          }
        } 
      }  
    }


    // Directs and executes one line of formatted input from protocol_process. While mostly
    // incoming streaming g-code blocks, this also executes Grbl internal commands, such as 
    // settings, initiating the homing cycle, and toggling switch states. This differs from
    // the runtime command module by being susceptible to when Grbl is ready to execute the 
    // next line during a cycle, so for switches like block delete, the switch only effects
    // the lines that are processed afterward, not necessarily real-time during a cycle, 
    // since there are motions already stored in the buffer. However, this 'lag' should not
    // be an issue, since these commands are not typically used during a cycle.
    public byte system_execute_line(char[] line) 
    {   
      byte char_counter = 1; 
      byte helper_var = 0; // Helper variable
      float parameter = 0.0f, value = 0.0f;
      switch(line[char_counter]) {
        case '\0' : report_grbl_help(); break;
        case '$' : // Prints Grbl settings
          if ( line[++char_counter] != 0 ) { return(STATUS_INVALID_STATEMENT); }
          if ((sys.state & (STATE_CYCLE | STATE_HOLD)) != 0) { return (STATUS_IDLE_ERROR); } // Block during cycle. Takes too long to print.
          else { report_grbl_settings(); }
          break;
        case 'G' : // Prints gcode parser state
          if ( line[++char_counter] != 0 ) { return(STATUS_INVALID_STATEMENT); }
          else { report_gcode_modes(); }
          break;   
        case 'C' : // Set check g-code mode [IDLE/CHECK]
          if ( line[++char_counter] != 0 ) { return(STATUS_INVALID_STATEMENT); }
          // Perform reset when toggling off. Check g-code mode should only work if Grbl
          // is idle and ready, regardless of alarm locks. This is mainly to keep things
          // simple and consistent.
          if ( sys.state == STATE_CHECK_MODE ) { 
            mc_reset(); 
            report_feedback_message(MESSAGE_DISABLED);
          } else {
            if (sys.state != 0) { return(STATUS_IDLE_ERROR); } // Requires no alarm mode.
            sys.state = STATE_CHECK_MODE;
            report_feedback_message(MESSAGE_ENABLED);
          }
          break; 
        case 'X' : // Disable alarm lock [ALARM]
          if ( line[++char_counter] != 0 ) { return(STATUS_INVALID_STATEMENT); }
          if (sys.state == STATE_ALARM) { 
            report_feedback_message(MESSAGE_ALARM_UNLOCK);
            sys.state = STATE_IDLE;
            // Don't run startup script. Prevents stored moves in startup from causing accidents.
          } // Otherwise, no effect.
          break;               
    //  case 'J' : break;  // Jogging methods
        // TODO: Here jogging can be placed for execution as a seperate subprogram. It does not need to be 
        // susceptible to other runtime commands except for e-stop. The jogging function is intended to
        // be a basic toggle on/off with controlled acceleration and deceleration to prevent skipped 
        // steps. The user would supply the desired feedrate, axis to move, and direction. Toggle on would
        // start motion and toggle off would initiate a deceleration to stop. One could 'feather' the
        // motion by repeatedly toggling to slow the motion to the desired location. Location data would 
        // need to be updated real-time and supplied to the user through status queries.
        //   More controlled exact motions can be taken care of by inputting G0 or G1 commands, which are 
        // handled by the planner. It would be possible for the jog subprogram to insert blocks into the
        // block buffer without having the planner plan them. It would need to manage de/ac-celerations 
        // on its own carefully. This approach could be effective and possibly size/memory efficient.        
        default : 
          // Block any system command that requires the state as IDLE/ALARM. (i.e. EEPROM, homing)
          if ( !(sys.state == STATE_IDLE || sys.state == STATE_ALARM) ) { return(STATUS_IDLE_ERROR); }
          bool performDefault = false;
          switch( line[char_counter] ) {
            case '#' : // Print Grbl NGC parameters
              if ( line[++char_counter] != 0 ) { return(STATUS_INVALID_STATEMENT); }
              else { report_ngc_parameters(); }
              break;          
            case 'H' : // Perform homing cycle [IDLE/ALARM]
              if (bit_istrue(settings.flags,BITFLAG_HOMING_ENABLE)) { 
                // Only perform homing if Grbl is idle or lost.
                mc_homing_cycle(); 
                if (sys.abort == 0) { system_execute_startup(line); } // Execute startup scripts after successful homing.
              } else { return(STATUS_SETTING_DISABLED); }
              break;
            case 'I' : // Print or store build info. [IDLE/ALARM]
              if ( line[++char_counter] == 0 ) { 
                settings_read_build_info(line);
                report_build_info(line);
              } else { // Store startup line [IDLE/ALARM]
                if(line[char_counter++] != '=') { return(STATUS_INVALID_STATEMENT); }
                helper_var = char_counter; // Set helper variable as counter to start of user info line.
                do {
                  line[char_counter-helper_var] = line[char_counter];
                } while (line[char_counter++] != 0);
                settings_store_build_info(line);
              }
              break;                 
            case 'N' : // Startup lines. [IDLE/ALARM]
              if ( line[++char_counter] == 0 ) { // Print startup lines
                for (helper_var=0; helper_var < N_STARTUP_LINE; helper_var++) {
                  if (!(settings_read_startup_line(helper_var, line))) {
                    report_status_message(STATUS_SETTING_READ_FAIL);
                  } else {
                    report_startup_line(helper_var,line);
                  }
                }
                break;
              } else { // Store startup line [IDLE Only] Prevents motion during ALARM.
                if (sys.state != STATE_IDLE) { return(STATUS_IDLE_ERROR); } // Store only when idle.
                helper_var = 1;  // Set helper_var to flag storing method. 
                // No break. Continues into default: to read remaining command characters.
                performDefault = true;
                break;
              }
            default :  // Storing setting methods [IDLE/ALARM]
              performDefault = true;
              break;
          }

          if (performDefault)
          {
            if (!read_float(line, ref char_counter, ref parameter)) { return (STATUS_BAD_NUMBER_FORMAT); }
            if (line[char_counter++] != '=') { return (STATUS_INVALID_STATEMENT); }
            if (helper_var != 0)
            { // Store startup line
              // Prepare sending gcode block to gcode parser by shifting all characters
              helper_var = char_counter; // Set helper variable as counter to start of gcode block
              do
              {
                line[char_counter - helper_var] = line[char_counter];
              } while (line[char_counter++] != 0);
              // Execute gcode block to ensure block is valid.
              helper_var = mGCode.gc_execute_line(line); // Set helper_var to returned status code.
              if (helper_var != 0) { return (helper_var); }
              else
              {
                helper_var = (byte)trunc(parameter); // Set helper_var to int value of parameter
                settings_store_startup_line(helper_var, line);
              }
            }
            else
            { // Store global setting.
              if (!read_float(line, ref char_counter, ref value)) { return (STATUS_BAD_NUMBER_FORMAT); }
              if (line[char_counter] != 0) { return (STATUS_INVALID_STATEMENT); }
              return (settings_store_global_setting((byte)parameter, value));
            }
          }

          break;
      }
      return(STATUS_OK); // If '$' command makes it to here, then everything's ok.
    }

  }
}
