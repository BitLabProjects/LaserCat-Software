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
        for (var i = 0; i < lines.Length; i++) {
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
    public void printString(char[] s)
    {
      //while (*s)
      //  serial_write(*s++);
      foreach (char c in s)
        serial_write(c);
    }

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



  }
}
