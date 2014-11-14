using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.LaserCat.Grbl
{
  partial class Grbl //System
  {
    // Define system header files and standard libraries used by Grbl

    //TODO Implementate vuote, gestire
    //public _delay_ms(x) ;
    //public _delay_us(x) ;
    //public ISR(x) public public void x() 
    //public trunc(x) floor(x)
    //public lround(x) round(x)
    //inline double round(double number)
    //{
    //    return number < 0.0 ? ceil(number - 0.5) : floor(number + 0.5);
    //}

    // Define system executor bit map. Used internally by runtime protocol as runtime command flags, 
    // which notifies the main program to execute the specified runtime command asynchronously.
    // NOTE: The system executor uses an unsigned 8-bit volatile variable (8 flag limit.) The default
    // flags are always false, so the runtime protocol only needs to check for a non-zero value to 
    // know when there is a runtime command to execute.
    public byte EXEC_STATUS_REPORT = bit(0); // bitmask 00000001
    public byte EXEC_CYCLE_START = bit(1); // bitmask 00000010
    public byte EXEC_CYCLE_STOP = bit(2); // bitmask 00000100
    public byte EXEC_FEED_HOLD = bit(3); // bitmask 00001000
    public byte EXEC_RESET = bit(4); // bitmask 00010000
    public byte EXEC_ALARM = bit(5); // bitmask 00100000
    public byte EXEC_CRIT_EVENT = bit(6); // bitmask 01000000
    // public                  bit(7) // bitmask 10000000

    // Define system state bit map. The state variable primarily tracks the individual functions
    // of Grbl to manage each without overlapping. It is also used as a messaging flag for
    // critical events.
    public byte STATE_IDLE = 0; // Must be zero. No flags.
    public byte STATE_ALARM = bit(0); // In alarm state. Locks out all g-code processes. Allows settings access.
    public byte STATE_CHECK_MODE = bit(1); // G-code check mode. Locks out planner and motion only.
    public byte STATE_HOMING = bit(2); // Performing homing cycle
    public byte STATE_QUEUED = bit(3); // Indicates buffered blocks, awaiting cycle start.
    public byte STATE_CYCLE = bit(4); // Cycle is running
    public byte STATE_HOLD = bit(5); // Executing feed hold
    // public int STATE_JOG =     NutsAndBolts.bit(6); // Jogging mode is unique like homing.

    
    

  }

  partial class Grbl //CPU_MAP
  {
    // Define serial port pins and interrupt vectors.
    //public int SERIAL_RX =     USART_RX_vect; //USART_RX_vect
    //public int SERIAL_UDRE =   USART_UDRE_vect; //USART_UDRE_vect

    // Define step pulse output pins. NOTE: All step bit pins must be on the same port.
    public int STEP_DDR; //DDRD
    public int STEP_PORT; //PORTD
    public const int X_STEP_BIT = 2; // Uno Digital Pin 2
    public const int Y_STEP_BIT = 3; // Uno Digital Pin 3
    public const int Z_STEP_BIT = 4; // Uno Digital Pin 4
    public int STEP_MASK = ((1 << X_STEP_BIT) | (1 << Y_STEP_BIT) | (1 << Z_STEP_BIT)); // All step bits

    // Define step direction output pins. NOTE: All direction pins must be on the same port.
    public int DIRECTION_DDR; //DDRD
    public int DIRECTION_PORT; //PORTD
    public const int X_DIRECTION_BIT = 5; // Uno Digital Pin 5
    public const int Y_DIRECTION_BIT = 6; // Uno Digital Pin 6
    public const int Z_DIRECTION_BIT = 7; // Uno Digital Pin 7
    public int DIRECTION_MASK = ((1 << X_DIRECTION_BIT) | (1 << Y_DIRECTION_BIT) | (1 << Z_DIRECTION_BIT)); // All direction bits

    // Define stepper driver enable/disable output pin.
    public int STEPPERS_DISABLE_DDR; //DDRB
    public int STEPPERS_DISABLE_PORT; //PORTB
    public const int STEPPERS_DISABLE_BIT = 0; // Uno Digital Pin 8
    public int STEPPERS_DISABLE_MASK = (1 << STEPPERS_DISABLE_BIT);

    // Define homing/hard limit switch input pins and limit interrupt vectors. 
    // NOTE: All limit bit pins must be on the same port, but not on a port with other input pins (pinout).
    public int LIMIT_DDR; //DDRB
    public int LIMIT_PIN; //PINB
    public int LIMIT_PORT; //PORTB
    public const int X_LIMIT_BIT = 1; // Uno Digital Pin 9
    public const int Y_LIMIT_BIT = 2; // Uno Digital Pin 10
    public const bool VARIABLE_SPINDLE = false;
    // Z Limit pin and spindle enabled swapped to access hardware PWM on Pin 11.  
    public const int Z_LIMIT_BIT = VARIABLE_SPINDLE ? 4 : 3; // Uno Digital Pin 12 // Uno Digital Pin 11
    public int LIMIT_MASK = ((1 << X_LIMIT_BIT) | (1 << Y_LIMIT_BIT) | (1 << Z_LIMIT_BIT)); // All limit bits
    public int LIMIT_INT; //PCIE0 ; // Pin change interrupt enable pin
    //public int LIMIT_INT_vect =   LIMIT_INT_vect; //PCINT0_vect 
    public int LIMIT_PCMSK; //PCMSK0; // Pin change interrupt register

    // Define spindle enable and spindle direction output pins.
    public int SPINDLE_ENABLE_DDR; //DDRB
    public int SPINDLE_ENABLE_PORT; //PORTB
    // Z Limit pin and spindle enabled swapped to access hardware PWM on Pin 11.  
    public int SPINDLE_ENABLE_BIT = VARIABLE_SPINDLE ? 3 : 4; // Uno Digital Pin 11// Uno Digital Pin 12
    public int SPINDLE_DIRECTION_DDR; //DDRB
    public int SPINDLE_DIRECTION_PORT; //PORTB
    public int SPINDLE_DIRECTION_BIT = 5; // Uno Digital Pin 13 (NOTE: D13 can't be pulled-high input due to LED.)

    // Define flood and mist coolant enable output pins.
    // NOTE: Uno analog pins 4 and 5 are reserved for an i2c interface, and may be installed at
    // a later date if flash and memory space allows.
    public int COOLANT_FLOOD_DDR; //DDRC
    public int COOLANT_FLOOD_PORT; //PORTC
    public int COOLANT_FLOOD_BIT = 3; // Uno Analog Pin 3
    // Mist coolant disabled by default. See config.h to enable/disable.
    public int COOLANT_MIST_DDR;
    public int COOLANT_MIST_PORT;
    public int COOLANT_MIST_BIT = 4; // Uno Analog Pin 4

    // Define user-control pinouts (cycle start, reset, feed hold) input pins.
    // NOTE: All pinouts pins must be on the same port and not on a port with other input pins (limits).
    public int PINOUT_DDR; //DDRC
    public int PINOUT_PIN; //PINC
    public int PINOUT_PORT; //PORTC
    public const int PIN_RESET = 0; // Uno Analog Pin 0
    public const int PIN_FEED_HOLD = 1; // Uno Analog Pin 1
    public const int PIN_CYCLE_START = 2; // Uno Analog Pin 2
    public int PINOUT_INT; //PCIE1 ; // Pin change interrupt enable pin
    //public int PINOUT_INT_vect =  PINOUT_INT_vect; //PCINT1_vect
    public int PINOUT_PCMSK; //PCMSK1; // Pin change interrupt register
    public int PINOUT_MASK = ((1 << PIN_RESET) | (1 << PIN_FEED_HOLD) | (1 << PIN_CYCLE_START));

    // Define probe switch input pin.
    public int PROBE_DDR; //DDRC
    public int PROBE_PIN; //PINC
    public int PROBE_PORT; //PORTC
    public const int PROBE_BIT = 5; // Uno Analog Pin 5
    public int PROBE_MASK = (1 << PROBE_BIT);


    // Advanced Configuration Below You should not need to touch these variables
    public int TCCRA_REGISTER;
    public int TCCRB_REGISTER;
    public int OCR_REGISTER;

    public int COMB_BIT;
    public int WAVE0_REGISTER;
    public int WAVE1_REGISTER;
    public int WAVE2_REGISTER;
    public int WAVE3_REGISTER;

    // NOTE: On the 328p, these must be the same as the SPINDLE_ENABLE settings.
    public int SPINDLE_PWM_DDR;
    public int SPINDLE_PWM_PORT;
    public int SPINDLE_PWM_BIT; // Shared with SPINDLE_ENABLE.
  }

  public class NutsAndBolts
  {
    public const int N_AXIS = 3; // Number of axes
    public const int X_AXIS = 0; // Axis indexing value. Must start with 0 and be continuous.
    public const int Y_AXIS = 1;
    public const int Z_AXIS = 2;
    // public const int A_AXIS 3

    public const float MM_PER_INCH = (25.40f);
    public const float INCH_PER_MM = (0.0393701f);

    //TODO 
    public const int F_CPU = 48000000;
    public const int TICKS_PER_MICROSECOND = (F_CPU / 1000000);
  }

  public partial class Grbl //Report
  {
    // Define Grbl status codes.
    public const byte STATUS_OK = 0;
    public const byte STATUS_EXPECTED_COMMAND_LETTER = 1;
    public const byte STATUS_BAD_NUMBER_FORMAT = 2;
    public const byte STATUS_INVALID_STATEMENT = 3;
    public const byte STATUS_NEGATIVE_VALUE = 4;
    public const byte STATUS_SETTING_DISABLED = 5;
    public const byte STATUS_SETTING_STEP_PULSE_MIN = 6;
    public const byte STATUS_SETTING_READ_FAIL = 7;
    public const byte STATUS_IDLE_ERROR = 8;
    public const byte STATUS_ALARM_LOCK = 9;
    public const byte STATUS_SOFT_LIMIT_ERROR = 10;
    public const byte STATUS_OVERFLOW = 11;

    public const byte STATUS_GCODE_UNSUPPORTED_COMMAND = 20;
    public const byte STATUS_GCODE_MODAL_GROUP_VIOLATION = 21;
    public const byte STATUS_GCODE_UNDEFINED_FEED_RATE = 22;
    public const byte STATUS_GCODE_COMMAND_VALUE_NOT_INTEGER = 23;
    public const byte STATUS_GCODE_AXIS_COMMAND_CONFLICT = 24;
    public const byte STATUS_GCODE_WORD_REPEATED = 25;
    public const byte STATUS_GCODE_NO_AXIS_WORDS = 26;
    public const byte STATUS_GCODE_INVALID_LINE_NUMBER = 27;
    public const byte STATUS_GCODE_VALUE_WORD_MISSING = 28;
    public const byte STATUS_GCODE_UNSUPPORTED_COORD_SYS = 29;
    public const byte STATUS_GCODE_G53_INVALID_MOTION_MODE = 30;
    public const byte STATUS_GCODE_AXIS_WORDS_EXIST = 31;
    public const byte STATUS_GCODE_NO_AXIS_WORDS_IN_PLANE = 32;
    public const byte STATUS_GCODE_INVALID_TARGET = 33;
    public const byte STATUS_GCODE_ARC_RADIUS_ERROR = 34;
    public const byte STATUS_GCODE_NO_OFFSETS_IN_PLANE = 35;
    public const byte STATUS_GCODE_UNUSED_WORDS = 36;
    public const byte STATUS_GCODE_G43_DYNAMIC_AXIS_ERROR = 37;

    // Define Grbl alarm codes. Less than zero to distinguish alarm error from status error.
    public const int ALARM_LIMIT_ERROR = -1;
    public const int ALARM_ABORT_CYCLE = -2;
    public const int ALARM_PROBE_FAIL = -3;

    // Define Grbl feedback message codes.
    public const int MESSAGE_CRITICAL_EVENT = 1;
    public const int MESSAGE_ALARM_LOCK = 2;
    public const int MESSAGE_ALARM_UNLOCK = 3;
    public const int MESSAGE_ENABLED = 4;
    public const int MESSAGE_DISABLED = 5;
  }

  public partial class Grbl //Stepper
  {
    /*
    stepper.c - stepper motor driver: executes motion plans using stepper motors
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

    public static int SEGMENT_BUFFER_SIZE = 6;

    // Some useful constants.
    public float DT_SEGMENT = (1.0f / (ACCELERATION_TICKS_PER_SECOND * 60.0f)); // min/segment 
    public float REQ_MM_INCREMENT_SCALAR = 1.25f;
    public const byte RAMP_ACCEL = 0;
    public const byte RAMP_CRUISE = 1;
    public const byte RAMP_DECEL = 2;

    // Define Adaptive Multi-Axis Step-Smoothing(AMASS) levels and cutoff frequencies. The highest level
    // frequency bin starts at 0Hz and ends at its cutoff frequency. The next lower level frequency bin
    // starts at the next higher cutoff frequency, and so on. The cutoff frequencies for each level must
    // be considered carefully against how much it over-drives the stepper ISR, the accuracy of the 16-bit
    // timer, and the CPU overhead. Level 0 (no AMASS, normal operation) frequency bin starts at the 
    // Level 1 cutoff frequency and up to as fast as the CPU allows (over 30kHz in limited testing).
    // NOTE: AMASS cutoff frequency multiplied by ISR overdrive factor must not exceed maximum step frequency.
    // NOTE: Current settings are set to overdrive the ISR to no more than 16kHz, balancing CPU overhead
    // and timer accuracy.  Do not alter these settings unless you know what you are doing.
    public int MAX_AMASS_LEVEL = 3;
    // AMASS_LEVEL0: Normal operation. No AMASS. No upper cutoff frequency. Starts at LEVEL1 cutoff frequency.
    public int AMASS_LEVEL1 = (NutsAndBolts.F_CPU / 8000); // Over-drives ISR (x2). Defined as NutsAndBolts.F_CPU/(Cutoff frequency in Hz)
    public int AMASS_LEVEL2 = (NutsAndBolts.F_CPU / 4000); // Over-drives ISR (x4)
    public int AMASS_LEVEL3 = (NutsAndBolts.F_CPU / 2000); // Over-drives ISR (x8)

  }

  //public partial class //Main
  //{
  //  public system_t sys;
  //}

  public partial class Grbl //Settings
  {
    public const string GRBL_VERSION = "0.9g";
    public const string GRBL_VERSION_BUILD = "20140905";

    // Version of the EEPROM data. Will be used to migrate existing data from older versions of Grbl
    // when firmware is upgraded. Always stored in byte 0 of eeprom
    public const int SETTINGS_VERSION = 9;  // NOTE: Check settings_reset() when moving to next version.

    // Define bit flag masks for the boolean settings in settings.flag.
    public const int BITFLAG_REPORT_INCHES = 1 << 0;
    public const int BITFLAG_AUTO_START = 1 << 1;
    public const int BITFLAG_INVERT_ST_ENABLE = 1 << 2;
    public const int BITFLAG_HARD_LIMIT_ENABLE = 1 << 3;
    public const int BITFLAG_HOMING_ENABLE = 1 << 4;
    public const int BITFLAG_SOFT_LIMIT_ENABLE = 1 << 5;
    public const int BITFLAG_INVERT_LIMIT_PINS = 1 << 6;
    public const int BITFLAG_INVERT_PROBE_PIN = 1 << 7;

    // Define status reporting boolean enable bit flags in settings.status_report_mask
    public const int BITFLAG_RT_STATUS_MACHINE_POSITION = 1 << 0;
    public const int BITFLAG_RT_STATUS_WORK_POSITION = 1 << 1;
    public const int BITFLAG_RT_STATUS_PLANNER_BUFFER = 1 << 2;
    public const int BITFLAG_RT_STATUS_SERIAL_RX = 1 << 3;

    // Define EEPROM memory address location values for Grbl settings and parameters
    // NOTE: The Atmega328p has 1KB EEPROM. The upper half is reserved for parameters and
    // the startup script. The lower half contains the global settings and space for future 
    // developments.
    public const uint EEPROM_ADDR_GLOBAL = 1U;
    public const uint EEPROM_ADDR_PARAMETERS = 512U;
    public const uint EEPROM_ADDR_STARTUP_BLOCK = 768U;
    public const uint EEPROM_ADDR_BUILD_INFO = 942U;

    // Define EEPROM address indexing for coordinate parameters
    public const int N_COORDINATE_SYSTEM = 6;  // Number of supported work coordinate systems (from index 1)
    public const int SETTING_INDEX_NCOORD = N_COORDINATE_SYSTEM + 1; // Total number of system stored (from index 0)
    // NOTE: Work coordinate indices are (0=G54, 1=G55, ... , 6=G59)
    public const int SETTING_INDEX_G28 = N_COORDINATE_SYSTEM;    // Home position 1
    public const int SETTING_INDEX_G30 = N_COORDINATE_SYSTEM + 1;  // Home position 2
    // public const SETTING_INDEX_G92    N_COORDINATE_SYSTEM+2  // Coordinate offset (G92.2,G92.3 not supported)

    // Define Grbl axis settings numbering scheme. Starts at START_VAL, every INCREMENT, over N_SETTINGS.
    public const int AXIS_N_SETTINGS = 4;
    public const int AXIS_SETTINGS_START_VAL = 100; // NOTE: Reserving settings values >= 100 for axis settings. Up to 255.
    public const int AXIS_SETTINGS_INCREMENT = 10;  // Must be greater than the number of axis settings
  }

}
