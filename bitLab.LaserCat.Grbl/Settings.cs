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
    public struct settings_t
    {
      // Axis settings
      public float[] steps_per_mm;
      public float[] max_rate;
      public float[] acceleration;
      public float[] max_travel;

      // Remaining Grbl settings
      public byte pulse_microseconds;
      public byte step_invert_mask;
      public byte dir_invert_mask;
      public byte stepper_idle_lock_time; // If max value 255, steppers do not disable.
      public byte status_report_mask; // Mask to indicate desired report data.
      public float junction_deviation;
      public float arc_tolerance;

      public byte flags;  // Contains default boolean settings

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


    // Method to store startup lines into EEPROM
    public void settings_store_startup_line(byte n, char[] line)
    {
      //TODO
      //uint addr = n * (LINE_BUFFER_SIZE + 1) + EEPROM_ADDR_STARTUP_BLOCK;
      //memcpy_to_eeprom_with_checksum(addr, (byte*)line, LINE_BUFFER_SIZE);
    }


    // Method to store build info into EEPROM
    public void settings_store_build_info(char[] line)
    {
      memcpy_to_eeprom_with_checksum(EEPROM_ADDR_BUILD_INFO, line, LINE_BUFFER_SIZE);
    }


    // Method to store coord data parameters into EEPROM
    public void settings_write_coord_data(byte coord_select, float[] coord_data)
    {
      //TODO
      //uint addr = coord_select * (sizeof(float) * NutsAndBolts.N_AXIS + 1) + EEPROM_ADDR_PARAMETERS;
      //memcpy_to_eeprom_with_checksum(addr, (byte*)coord_data, sizeof(float) * NutsAndBolts.N_AXIS);
    }


    // Method to store Grbl global settings struct and version number into EEPROM
    public void write_global_settings()
    {
      eeprom_put_char(0, SETTINGS_VERSION);
      //TODO
      //memcpy_to_eeprom_with_checksum(EEPROM_ADDR_GLOBAL, (byte*)&settings, (uint)sizeof(settings_t));
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

      write_global_settings();
    }


    // Helper function to clear the EEPROM space containing parameter data.
    public void settings_clear_parameters()
    {
      byte idx;
      float[] coord_data = new float[3];
      //memset(&coord_data, 0, sizeof(coord_data));
      //for (idx=0; idx < SETTING_INDEX_NCOORD; idx++) { settings_write_coord_data(idx, coord_data); }
      //TODO
    }


    // Helper function to clear the EEPROM space containing the startup lines.
    public void settings_clear_startup_lines()
    {
      if (N_STARTUP_LINE > 0)
        eeprom_put_char(EEPROM_ADDR_STARTUP_BLOCK, 0);
      if (N_STARTUP_LINE > 1)
        eeprom_put_char(EEPROM_ADDR_STARTUP_BLOCK + (LINE_BUFFER_SIZE + 1), 0);
    }


    // Helper function to clear the EEPROM space containing the user build info string.
    public void settings_clear_build_info() { eeprom_put_char(EEPROM_ADDR_BUILD_INFO, 0); }


    // Reads startup line from EEPROM. Updated pointed line string data.
    public bool settings_read_startup_line(byte n, char[] line)
    {
      //TODO
      return false;
      //uint addr = n * (LINE_BUFFER_SIZE + 1) + EEPROM_ADDR_STARTUP_BLOCK;
      //if (!(memcpy_from_eeprom_with_checksum(line, addr, LINE_BUFFER_SIZE)))
      //{
      //  // Reset line with default value
      //  line[0] = 0; // Empty line
      //  settings_store_startup_line(n, line);
      //  return (false);
      //}
      //return (true);
    }


    // Reads startup line from EEPROM. Updated pointed line string data.
    public bool settings_read_build_info(char[] line)
    {
      if (!(memcpy_from_eeprom_with_checksum(line, EEPROM_ADDR_BUILD_INFO, LINE_BUFFER_SIZE)))
      {
        // Reset line with default value
        line[0] = '\0'; // Empty line
        settings_store_build_info(line);
        return (false);
      }
      return (true);
    }


    // Read selected coordinate data from EEPROM. Updates pointed coord_data value.
    public bool settings_read_coord_data(byte coord_select, float[] coord_data)
    {
      //TODO 
      //uint addr = coord_select * (sizeof(float) * NutsAndBolts.N_AXIS + 1) + EEPROM_ADDR_PARAMETERS;
      //if (!(memcpy_from_eeprom_with_checksum((char*)coord_data, addr, sizeof(float)*Consts.NutsAndBolts.N_AXIS))) {
      //  // Reset with default zero vector
      //  clear_vector_float(coord_data); 
      //  settings_write_coord_data(coord_select,coord_data);
      //  return(false);
      //}
      //return(true);
      return false;
    }


    // Reads Grbl global settings struct from EEPROM.
    public bool read_global_settings()
    {
      // Check version-byte of eeprom
      byte version = eeprom_get_char(0);
      //if (version == SETTINGS_VERSION) {
      //  // Read settings-record and check checksum
      //  if (!(memcpy_from_eeprom_with_checksum((char*)&settings, EEPROM_ADDR_GLOBAL, sizeof(settings_t)))) {
      //    return(false);
      //  }
      //} else {
      //  return(false); 
      //}
      //return(true);
      //TODO 
      return false;
    }


    // A helper method to set settings from command line
    public byte settings_store_global_setting(byte parameter, float value)
    {
      if (value < 0.0) { return (STATUS_NEGATIVE_VALUE); }
      if (parameter >= AXIS_SETTINGS_START_VAL)
      {
        // Store axis configuration. Axis numbering sequence set by AXIS_SETTING defines.
        // NOTE: Ensure the setting index corresponds to the report.c settings printout.
        parameter -= AXIS_SETTINGS_START_VAL;
        byte set_idx = 0;
        while (set_idx < AXIS_N_SETTINGS)
        {
          if (parameter < NutsAndBolts.N_AXIS)
          {
            // Valid axis setting found.
            switch (set_idx)
            {
              case 0: settings.steps_per_mm[parameter] = value; break;
              case 1: settings.max_rate[parameter] = value; break;
              case 2: settings.acceleration[parameter] = value * 60 * 60; break; // Convert to mm/min^2 for grbl internal use.
              case 3: settings.max_travel[parameter] = -value; break;  // Store as negative for grbl internal use.
            }
            break; // Exit while-loop after setting has been configured and proceed to the EEPROM write call.
          }
          else
          {
            set_idx++;
            // If axis index greater than Consts.NutsAndBolts.N_AXIS or setting index greater than number of axis settings, error out.
            if ((parameter < AXIS_SETTINGS_INCREMENT) || (set_idx == AXIS_N_SETTINGS)) { return (STATUS_INVALID_STATEMENT); }
            parameter -= AXIS_SETTINGS_INCREMENT;
          }
        }
      }
      else
      {
        // Store non-axis Grbl settings
        byte int_value = (byte)trunc(value);
        switch (parameter)
        {
          case 0:
            if (int_value < 3) { return (STATUS_SETTING_STEP_PULSE_MIN); }
            settings.pulse_microseconds = int_value; break;
          case 1: settings.stepper_idle_lock_time = int_value; break;
          case 2:
            settings.step_invert_mask = int_value;
            st_generate_step_dir_invert_masks(); // Regenerate step and direction port invert masks.
            break;
          case 3:
            settings.dir_invert_mask = int_value;
            st_generate_step_dir_invert_masks(); // Regenerate step and direction port invert masks.
            break;
          case 4: // Reset to ensure change. Immediate re-init may cause problems.
            if (int_value != 0) { settings.flags |= BITFLAG_INVERT_ST_ENABLE; }
            else { settings.flags &= unchecked((byte)(~BITFLAG_INVERT_ST_ENABLE)); }
            break;
          case 5: // Reset to ensure change. Immediate re-init may cause problems.
            if (int_value != 0) { settings.flags |= BITFLAG_INVERT_LIMIT_PINS; }
            else { settings.flags &= unchecked((byte)(~BITFLAG_INVERT_LIMIT_PINS)); }
            break;
          case 6: // Reset to ensure change. Immediate re-init may cause problems.
            if (int_value != 0) { settings.flags |= BITFLAG_INVERT_PROBE_PIN; }
            else { settings.flags &= unchecked((byte)(~BITFLAG_INVERT_PROBE_PIN)); }
            break;
          case 10: settings.status_report_mask = int_value; break;
          case 11: settings.junction_deviation = value; break;
          case 12: settings.arc_tolerance = value; break;
          case 13:
            if (int_value != 0) { settings.flags |= BITFLAG_REPORT_INCHES; }
            else { settings.flags &= unchecked((byte)(~BITFLAG_REPORT_INCHES)); }
            break;
          case 14: // Reset to ensure change. Immediate re-init may cause problems.
            if (int_value != 0) { settings.flags |= BITFLAG_AUTO_START; }
            else { settings.flags &= unchecked((byte)(~BITFLAG_AUTO_START)); }
            break;
          case 20:
            if (int_value != 0)
            {
              if (bit_isfalse(settings.flags, BITFLAG_HOMING_ENABLE)) { return (STATUS_SOFT_LIMIT_ERROR); }
              settings.flags |= BITFLAG_SOFT_LIMIT_ENABLE;
            }
            else { settings.flags &= unchecked((byte)(~BITFLAG_SOFT_LIMIT_ENABLE)); }
            break;
          case 21:
            if (int_value != 0) { settings.flags |= BITFLAG_HARD_LIMIT_ENABLE; }
            else { settings.flags &= unchecked((byte)(~BITFLAG_HARD_LIMIT_ENABLE)); }
            limits_init(); // Re-init to immediately change. NOTE: Nice to have but could be problematic later.
            break;
          case 22:
            if (int_value != 0) { settings.flags |= BITFLAG_HOMING_ENABLE; }
            else
            {
              settings.flags &= unchecked((byte)(~BITFLAG_HOMING_ENABLE));
              settings.flags &= unchecked((byte)(~BITFLAG_SOFT_LIMIT_ENABLE)); // Force disable soft-limits.
            }
            break;
          case 23: settings.homing_dir_mask = int_value; break;
          case 24: settings.homing_feed_rate = value; break;
          case 25: settings.homing_seek_rate = value; break;
          case 26: settings.homing_debounce_delay = int_value; break;
          case 27: settings.homing_pulloff = value; break;
          default:
            return (STATUS_INVALID_STATEMENT);
        }
      }
      write_global_settings();
      return (STATUS_OK);
    }


    // Initialize the config subsystem
    public void settings_init()
    {
      if (!read_global_settings())
      {
        report_status_message(STATUS_SETTING_READ_FAIL);

        settings_restore_global_settings();

        // Force clear startup lines and build info user data. Parameters should be ok.
        // TODO: For next version, remove these clears. Only here because line buffer increased.
        settings_clear_startup_lines();
        settings_clear_build_info();

        report_grbl_settings();
      }

      // Check all parameter data into a dummy variable. If error, reset to zero, otherwise do nothing.
      float[] coord_data = new float[NutsAndBolts.N_AXIS];
      byte i;
      for (i = 0; i <= SETTING_INDEX_NCOORD; i++)
      {
        if (!settings_read_coord_data(i, coord_data))
        {
          report_status_message(STATUS_SETTING_READ_FAIL);
        }
      }
      // NOTE: Startup lines are checked and executed by protocol_main_loop at the end of initialization.
      // TODO: Build info should be checked here, but will wait until v1.0 to address this. Ok for now.
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
