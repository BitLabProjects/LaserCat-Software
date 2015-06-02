using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.LaserCat.Grbl
{
  unsafe partial class GrblFirmware
  {
    /*
      settings.c - eeprom configuration handling 
      Part of Grbl v0.9

      Copyright (c) 2012-2014 Sungeun K. Jeon  

      Grbl is free software: you can redistribute it and/or modify
      it under the terms of the GNU General Public License as published by
      the Free Software Foundation, either version 3 of the License, or
      (at your option) any later version.

      Grbl is distributed in the hope that it will be useful,
      but WITHOUT ANY WARRANTY; without even the implied warranty of
      MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
      GNU General Public License for more details.

      You should have received a copy of the GNU General Public License
      along with Grbl.  If not, see <http://www.gnu.org/licenses/>.
    */
    /* 
      This file is based on work from Grbl v0.8, distributed under the 
      terms of the MIT-license. See COPYING for more details.  
        Copyright (c) 2009-2011 Simen Svale Skogsrud
        Copyright (c) 2011-2012 Sungeun K. Jeon
    */

    //#include "system.h"
    //#include "settings.h"
    //#include "eeprom.h"
    //#include "protocol.h"
    //#include "report.h"
    //#include "limits.h"
    //#include "stepper.h"

    // Global persistent settings (Stored from byte EEPROM_ADDR_GLOBAL onwards)
    //SB!Items marked with //x are used only by ILaserCatHardware and sent on initialization
    //Items marked with //xx are used by the interface and the rest of Grbl (flags)
    public struct settings_t
    {
      // Axis settings
      public float[] steps_per_mm;
      public float[] max_rate;
      public float[] acceleration;
      public float[] max_travel;

      // Remaining Grbl settings
      public byte pulse_microseconds; //x
      public byte step_invert_mask; //x
      public byte dir_invert_mask; //x
      public byte stepper_idle_lock_time;  //x // If max value 255, steppers do not disable.
      public byte status_report_mask; // Mask to indicate desired report data.
      public float junction_deviation;
      public float arc_tolerance;

      public byte flags;  //xx // Contains default boolean settings

      public byte homing_dir_mask;
      public float homing_feed_rate;
      public float homing_seek_rate;
      public ushort homing_debounce_delay;
      public float homing_pulloff;

      public settings_t(bool dummy)
      {
        pulse_microseconds = 0;
        step_invert_mask = 0;
        dir_invert_mask = 0;
        stepper_idle_lock_time = 0; // If max value 255, steppers do not disable.
        status_report_mask = 0; // Mask to indicate desired report data.
        junction_deviation = 0;
        arc_tolerance = 0;

        flags = 0;  // Contains default boolean settings

        homing_dir_mask = 0;
        homing_feed_rate = 0;
        homing_seek_rate = 0;
        homing_debounce_delay = 0;
        homing_pulloff = 0;
        steps_per_mm = new float[NutsAndBolts.N_AXIS];
        max_rate = new float[NutsAndBolts.N_AXIS];
        acceleration = new float[NutsAndBolts.N_AXIS];
        max_travel = new float[NutsAndBolts.N_AXIS];
      }
    };

    public settings_t settings = new settings_t(true);

    // Method to store coord data parameters into EEPROM
    public void settings_write_coord_data(byte coord_select, float[] coord_data)
    {
      //TODO
      //uint addr = coord_select * (sizeof(float) * NutsAndBolts.N_AXIS + 1) + EEPROM_ADDR_PARAMETERS;
      //memcpy_to_eeprom_with_checksum(addr, (byte*)coord_data, sizeof(float) * NutsAndBolts.N_AXIS);
    }


    // Method to restore EEPROM-saved Grbl global settings back to defaults. 
    public void settings_restore_global_settings()
    {
      settings.pulse_microseconds = GrblFirmware.DEFAULT_STEP_PULSE_MICROSECONDS;
      settings.stepper_idle_lock_time = GrblFirmware.DEFAULT_STEPPER_IDLE_LOCK_TIME;
      settings.step_invert_mask = GrblFirmware.DEFAULT_STEPPING_INVERT_MASK;
      settings.dir_invert_mask = GrblFirmware.DEFAULT_DIRECTION_INVERT_MASK;
      settings.status_report_mask = GrblFirmware.DEFAULT_STATUS_REPORT_MASK;
      settings.junction_deviation = GrblFirmware.DEFAULT_JUNCTION_DEVIATION;
      settings.arc_tolerance = GrblFirmware.DEFAULT_ARC_TOLERANCE;
      settings.homing_dir_mask = GrblFirmware.DEFAULT_HOMING_DIR_MASK;
      settings.homing_feed_rate = GrblFirmware.DEFAULT_HOMING_FEED_RATE;
      settings.homing_seek_rate = GrblFirmware.DEFAULT_HOMING_SEEK_RATE;
      settings.homing_debounce_delay = GrblFirmware.DEFAULT_HOMING_DEBOUNCE_DELAY;
      settings.homing_pulloff = GrblFirmware.DEFAULT_HOMING_PULLOFF;

      settings.flags = 0;
      if (GrblFirmware.DEFAULT_REPORT_INCHES) { settings.flags |= BITFLAG_REPORT_INCHES; }
      if (GrblFirmware.DEFAULT_AUTO_START) { settings.flags |= BITFLAG_AUTO_START; }
      if (GrblFirmware.DEFAULT_INVERT_ST_ENABLE) { settings.flags |= BITFLAG_INVERT_ST_ENABLE; }
      if (GrblFirmware.DEFAULT_INVERT_LIMIT_PINS) { settings.flags |= BITFLAG_INVERT_LIMIT_PINS; }
      if (GrblFirmware.DEFAULT_SOFT_LIMIT_ENABLE) { settings.flags |= BITFLAG_SOFT_LIMIT_ENABLE; }
      if (GrblFirmware.DEFAULT_HARD_LIMIT_ENABLE) { settings.flags |= BITFLAG_HARD_LIMIT_ENABLE; }
      if (GrblFirmware.DEFAULT_HOMING_ENABLE) { settings.flags |= BITFLAG_HOMING_ENABLE; }

      settings.steps_per_mm[NutsAndBolts.X_AXIS] = GrblFirmware.DEFAULT_X_STEPS_PER_MM;
      settings.steps_per_mm[NutsAndBolts.Y_AXIS] = GrblFirmware.DEFAULT_Y_STEPS_PER_MM;
      settings.steps_per_mm[NutsAndBolts.Z_AXIS] = GrblFirmware.DEFAULT_Z_STEPS_PER_MM;
      settings.max_rate[NutsAndBolts.X_AXIS] = GrblFirmware.DEFAULT_X_MAX_RATE;
      settings.max_rate[NutsAndBolts.Y_AXIS] = GrblFirmware.DEFAULT_Y_MAX_RATE;
      settings.max_rate[NutsAndBolts.Z_AXIS] = GrblFirmware.DEFAULT_Z_MAX_RATE;
      settings.acceleration[NutsAndBolts.X_AXIS] = GrblFirmware.DEFAULT_X_ACCELERATION;
      settings.acceleration[NutsAndBolts.Y_AXIS] = GrblFirmware.DEFAULT_Y_ACCELERATION;
      settings.acceleration[NutsAndBolts.Z_AXIS] = GrblFirmware.DEFAULT_Z_ACCELERATION;
      settings.max_travel[NutsAndBolts.X_AXIS] = (-GrblFirmware.DEFAULT_X_MAX_TRAVEL);
      settings.max_travel[NutsAndBolts.Y_AXIS] = (-GrblFirmware.DEFAULT_Y_MAX_TRAVEL);
      settings.max_travel[NutsAndBolts.Z_AXIS] = (-GrblFirmware.DEFAULT_Z_MAX_TRAVEL);
    }

    // Read selected coordinate data from EEPROM. Updates pointed coord_data value.
    public bool settings_read_coord_data(byte coord_select, float[] coord_data)
    {
      //@_TODO 
      //uint addr = coord_select * (sizeof(float) * NutsAndBolts.N_AXIS + 1) + EEPROM_ADDR_PARAMETERS;
      //if (!(memcpy_from_eeprom_with_checksum((char*)coord_data, addr, sizeof(float)*Consts.NutsAndBolts.N_AXIS))) {
      //  // Reset with default zero vector
      //  clear_vector_float(coord_data); 
      //  settings_write_coord_data(coord_select,coord_data);
      //  return(false);
      //}
      //return(true);
      for (int i = 0; i < coord_data.Length; i++)
        coord_data[i] = 0.0f;
      return false;
    }

    // Initialize the config subsystem
    public void settings_init()
    {
      settings_restore_global_settings();
    }


    // Returns step pin mask according to Grbl internal axis indexing.
    public byte get_step_pin_mask(byte axis_idx)
    {
      if (axis_idx == NutsAndBolts.X_AXIS) { return ((1 << X_STEP_BIT)); }
      if (axis_idx == NutsAndBolts.Y_AXIS) { return ((1 << Y_STEP_BIT)); }
      return ((1 << Z_STEP_BIT));
    }


    // Returns direction pin mask according to Grbl internal axis indexing.
    public byte get_direction_pin_mask(byte axis_idx)
    {
      if (axis_idx == NutsAndBolts.X_AXIS) { return ((1 << X_DIRECTION_BIT)); }
      if (axis_idx == NutsAndBolts.Y_AXIS) { return ((1 << Y_DIRECTION_BIT)); }
      return ((1 << Z_DIRECTION_BIT));
    }


    // Returns limit pin mask according to Grbl internal axis indexing.
    public byte get_limit_pin_mask(byte axis_idx)
    {
      if (axis_idx == NutsAndBolts.X_AXIS) { return ((1 << X_LIMIT_BIT)); }
      if (axis_idx == NutsAndBolts.Y_AXIS) { return ((1 << Y_LIMIT_BIT)); }
      return ((1 << Z_LIMIT_BIT));
    }

  }
}
