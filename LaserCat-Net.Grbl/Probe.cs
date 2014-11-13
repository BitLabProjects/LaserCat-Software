using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace grbl_pcnet
{
  public partial class Grbl
  {
    // Inverts the probe pin state depending on user settings.
    byte probe_invert_mask;

    // Values that define the probing state machine.  
    public const byte PROBE_OFF     = 0; // No probing. (Must be zero.)
    public const byte PROBE_ACTIVE  = 1; // Actively watching the input pin.

    // Probe pin initialization routine.
    void probe_init()
    {
      //TODO
      //PROBE_DDR &= ~(PROBE_MASK); // Configure as input pins
      //if (bit_istrue(settings.flags, BITFLAG_INVERT_PROBE_PIN))
      //{
      //  PROBE_PORT &= ~(PROBE_MASK); // Normal low operation. Requires external pull-down.
      //  probe_invert_mask = 0;
      //}
      //else
      //{
      //  PROBE_PORT |= PROBE_MASK;    // Enable internal pull-up resistors. Normal high operation.
      //  probe_invert_mask = PROBE_MASK;
      //}
    }


    // Returns the probe pin state. Triggered = true. Called by gcode parser and probe state monitor.
    byte probe_get_state() { return (byte)((PROBE_PIN & PROBE_MASK) ^ probe_invert_mask); }


    // Monitors probe pin state and records the system position when detected. Called by the
    // stepper ISR per ISR tick.
    // NOTE: This function must be extremely efficient as to not bog down the stepper ISR.
    void probe_state_monitor()
    {
      if (sys.probe_state == PROBE_ACTIVE)
      {
        if (probe_get_state() != 0)
        {
          sys.probe_state = PROBE_OFF;
          //memcpy(sys.probe_position, sys.position, sizeof(float) * N_AXIS);
          copyArray(sys.probe_position, sys.position);
          bit_true(ref sys.execute, EXEC_FEED_HOLD);
        }
      }
    }
  }
}
