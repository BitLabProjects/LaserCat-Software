using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.LaserCat.Grbl
{
  public partial class GrblFirmware
  {
    private GCode mGCode;
    private ISerialPort mSerialPort;
    public GrblFirmware(GCode GCode, ISerialPort serialPort)
    {
      mGCode = GCode;
      mSerialPort = serialPort;
      mGCode.Initialize(this);
    }

    public void Execute()
    {
      // Initialize system upon power-up.
      serial_init();   // Setup serial baud rate and interrupts
      settings_init(); // Load grbl settings from EEPROM
      stepper_init();  // Configure stepper pins and interrupt timers
      system_init();   // Configure pinout pins and pin-change interrupt
  
      sys = new system_t(true); // Clear all system variables
      sys.abort = 1;   // Set abort to complete initialization
      //TODO sei(); // Enable interrupts

      // Check for power-up and set system alarm if homing is enabled to force homing cycle
      // by setting Grbl's alarm state. Alarm locks out all g-code commands, including the
      // startup scripts, but allows access to settings and internal commands. Only a homing
      // cycle '$H' or kill alarm locks '$X' will disable the alarm.
      // NOTE: The startup script will run after successful completion of the homing cycle, but
      // not after disabling the alarm locks. Prevents motion startup blocks from crashing into
      // things uncontrollably. Very bad.
      if (HOMING_INIT_LOCK)
      {
        if (bit_istrue(settings.flags, BITFLAG_HOMING_ENABLE)) { sys.state = STATE_ALARM; }
      }
  
      // Grbl initialization loop upon power-up or a system abort. For the latter, all processes
      // will return to this loop to be cleanly re-initialized.
      for(;;) {

        // TODO: Separate configure task that require interrupts to be disabled, especially upon
        // a system abort and ensuring any active interrupts are cleanly reset.
  
        // Reset Grbl primary systems.
        serial_reset_read_buffer(); // Clear serial read buffer
        mGCode.gc_init(); // Set g-code parser to default state
        spindle_init();
        coolant_init();
        //TODO limits_init(); 
        probe_init();
        plan_reset(); // Clear block buffer and planner variables
        st_reset(); // Clear stepper subsystem variables.

        // Sync cleared gcode and planner positions to current system position.
        plan_sync_position();
        mGCode.gc_sync_position();

        // Reset system variables.
        sys.abort = 0;
        sys.execute = 0;
        if (bit_istrue(settings.flags,BITFLAG_AUTO_START)) { sys.auto_start = 1; }
        else { sys.auto_start = 0; }
          
        // Start Grbl main loop. Processes program inputs and executes them.
        protocol_main_loop();
    
      }
    }
  }
}
