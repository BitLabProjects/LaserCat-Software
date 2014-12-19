using bitLab.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.LaserCat.Grbl
{
  unsafe public partial class GrblFirmware
  {
    private string printLineBuffer;
    public void printPgmString(string s)
    {
      const string newLineString = "\r\n";
      if (s.Contains(newLineString))
      {
        var lines = s.Split(new string[] { newLineString }, StringSplitOptions.None);
        for (var i = 0; i < lines.Length; i++)
        {
          //Se è l'ultimo split ed è vuoto significa che s finisce con \r\n, quindi invia il buffer
          if (i == lines.Length - 1 && string.IsNullOrEmpty(lines[i]))
            break;
          printLineBuffer += lines[i];
          Log.LogInfo("Grbl: " + printLineBuffer);
          printLineBuffer = string.Empty;
        }
      }
      else
      {
        printLineBuffer += s;
      }
    }

    // Convert float to string by immediately converting to a long integer, which contains
    // more digits than a float. Number of decimal places, which are tracked by a counter,
    // may be set by the user. The integer is then efficiently converted to a string.
    // NOTE: AVR '%' and '/' integer operations are very efficient. Bitshifting speed-up 
    // techniques are actually just slightly slower. Found this out the hard way.
    string printFloat(float n, int decimal_places)
    {
      string result = "";
      if (n < 0)
      {
        result += "-";
        n = -n;
      }

      int decimals = decimal_places;
      while (decimals >= 2)
      { // Quickly convert values expected to be E0 to E-4.
        n *= 100;
        decimals -= 2;
      }
      if (decimals != 0) { n *= 10; }
      n += 0.5f; // Add rounding factor. Ensures carryover through entire value.

      // Generate digits backwards and store in string.
      char[] buf = new char[10];
      byte i = 0;
      uint a = (uint)n;
      buf[decimal_places] = '.'; // Place decimal point, even if decimal places are zero.
      while (a > 0)
      {
        if (i == decimal_places) { i++; } // Skip decimal point location
        buf[i++] = (a % 10).ToString()[0]; // Get digit
        a /= 10;
      }
      while (i < decimal_places)
      {
        buf[i++] = '0'; // Fill in zeros to decimal point for (n < 1)
      }
      if (i == decimal_places)
      { // Fill in leading zero, if needed.
        i++;
        buf[i++] = '0';
      }

      // Print the generated string.
      for (; i > 0; i--)
        result += (buf[i - 1]);
      return result;
    }

    string printFloat_SettingValue(float n) { return printFloat(n, N_DECIMAL_SETTINGVALUE); }

    public void serial_write(char data)
    {
      //TODO


      //printf("%c", (char)data);
      //return;

      //// Calculate next head
      //uint8_t next_head = serial_tx_buffer_head + 1;
      //if (next_head == TX_BUFFER_SIZE) { next_head = 0; }

      //// Wait until there is space in the buffer
      //while (next_head == serial_tx_buffer_tail)
      //{
      //  // TODO: Restructure st_prep_buffer() calls to be executed here during a long print.    
      //  if (sys.execute & EXEC_RESET) { return; } // Only check for abort to avoid an endless loop.
      //}

      //// Store data and advance head
      //serial_tx_buffer[serial_tx_buffer_head] = data;
      //serial_tx_buffer_head = next_head;

      // Enable Data Register Empty Interrupt to make sure tx-streaming is running
      //TODO UCSR0B |=  (1 << UDRIE0); 
    }

    string print_uint8_base2(byte n)
    {
      string result = "";

	    for (int i = 0; i < 8; i++) {
		    result += ((n & 1) == 1) ? "1" : "0";
		    n >>= 1;
	    }

      return result;
    }

  }
}
