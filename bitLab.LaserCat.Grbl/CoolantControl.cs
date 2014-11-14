using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.LaserCat.Grbl
{
  partial class Grbl
  {

    public void coolant_init()
    {
      //TODO
      //COOLANT_FLOOD_DDR |= (1 << COOLANT_FLOOD_BIT);
      //#ifdef ENABLE_M7
      //  COOLANT_MIST_DDR |= (1 << COOLANT_MIST_BIT);
      //#endif
      //coolant_stop();
    }


    public void coolant_stop()
    {
      //TODO
      //COOLANT_FLOOD_PORT &= ~(1 << COOLANT_FLOOD_BIT);
      //#ifdef ENABLE_M7
      //  COOLANT_MIST_PORT &= ~(1 << COOLANT_MIST_BIT);
      //#endif
    }


    public void coolant_run(byte mode)
    {
      //TODO
      //if (sys.state == STATE_CHECK_MODE) { return; }

      //protocol_auto_cycle_start();   //temp fix for M8 lockup
      //protocol_buffer_synchronize(); // Ensure coolant turns on when specified in program.
      //if (mode == COOLANT_FLOOD_ENABLE) {
      //  COOLANT_FLOOD_PORT |= (1 << COOLANT_FLOOD_BIT);

      //#ifdef ENABLE_M7  
      //  } else if (mode == COOLANT_MIST_ENABLE) {
      //    COOLANT_MIST_PORT |= (1 << COOLANT_MIST_BIT);
      //#endif

      //} else {
      //  coolant_stop();
      //}
    }
  }
}
