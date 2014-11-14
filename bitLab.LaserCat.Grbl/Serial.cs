using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.LaserCat.Grbl
{
  partial class GrblFirmware
  {

    public const int RX_BUFFER_SIZE = 128;
    public const int TX_BUFFER_SIZE = 64;

    public const int SERIAL_NO_DATA = 0xff;

    public const int RX_BUFFER_FULL = 96; // XOFF high watermark
    public const int RX_BUFFER_LOW = 64; // XON low watermark
    public const int SEND_XOFF = 1;
    public const int SEND_XON = 2;
    public const int XOFF_SENT = 3;
    public const int XON_SENT = 4;
    public const int XOFF_CHAR = 0x13;
    public const int XON_CHAR = 0x11;

    byte[] serial_rx_buffer = new byte[RX_BUFFER_SIZE];
    byte serial_rx_buffer_head = 0;
    volatile byte serial_rx_buffer_tail = 0;

    byte[] serial_tx_buffer = new byte[TX_BUFFER_SIZE];
    byte serial_tx_buffer_head = 0;
    volatile byte serial_tx_buffer_tail = 0;


    //#ifdef ENABLE_XONXOFF
    byte flow_ctrl = XON_SENT; // Flow control state variable
    //#endif
  

    // Returns the number of bytes used in the RX serial buffer.
    public byte serial_get_rx_buffer_count()
    {
      byte rtail = serial_rx_buffer_tail; // Copy to limit multiple calls to volatile
      if (serial_rx_buffer_head >= rtail) { return(byte)(serial_rx_buffer_head-rtail); }
      return (byte)(RX_BUFFER_SIZE - (rtail-serial_rx_buffer_head));
    }


    // Returns the number of bytes used in the TX serial buffer.
    // NOTE: Not used except for debugging and ensuring no TX bottlenecks.
    public byte serial_get_tx_buffer_count()
    {
      byte ttail = serial_tx_buffer_tail; // Copy to limit multiple calls to volatile
      if (serial_tx_buffer_head >= ttail) { return(byte)(serial_tx_buffer_head-ttail); }
      return (byte)(TX_BUFFER_SIZE - (ttail-serial_tx_buffer_head));
    }


    public void serial_init()
    {
	    //TODO
      //// Set baud rate
      //#if BAUD_RATE < 57600
      //  uint16_t UBRR0_value = ((NutsAndBolts.F_CPU / (8L * BAUD_RATE)) - 1)/2 ;
      //  UCSR0A &= ~(1 << U2X0); // baud doubler off  - Only needed on Uno XXX
      //#else
      //  uint16_t UBRR0_value = ((NutsAndBolts.F_CPU / (4L * BAUD_RATE)) - 1)/2;
      //  UCSR0A |= (1 << U2X0);  // baud doubler on for high baud rates, i.e. 115200
      //#endif
      //UBRR0H = UBRR0_value >> 8;
      //UBRR0L = UBRR0_value;
      //          
      //// enable rx and tx
      //UCSR0B |= 1<<RXEN0;
      //UCSR0B |= 1<<TXEN0;
	
      //// enable interrupt on complete reception of a byte
      //UCSR0B |= 1<<RXCIE0;
	     // 
      //// defaults to 8-bit, no parity, 1 stop bit
    }


    // Writes one byte to the TX serial buffer. Called by main program.
    // TODO: Check if we can speed this up for writing strings, rather than single bytes.

    public void serial_write(byte data) {
      //TODO
      //// Calculate next head
      //byte next_head = serial_tx_buffer_head + 1;
      //if (next_head == TX_BUFFER_SIZE) { next_head = 0; }

      //// Wait until there is space in the buffer
      //while (next_head == serial_tx_buffer_tail) { 
      //  // TODO: Restructure st_prep_buffer() calls to be executed here during a long print.    
      //  if (sys.execute & EXEC_RESET) { return; } // Only check for abort to avoid an endless loop.
      //}

      //// Store data and advance head
      //serial_tx_buffer[serial_tx_buffer_head] = data;
      //serial_tx_buffer_head = next_head;
  
      //// Enable Data Register Empty Interrupt to make sure tx-streaming is running
      ////TODO UCSR0B |=  (1 << UDRIE0); 
    }


    // Data Register Empty Interrupt handler
    //ISR(SERIAL_UDRE)
    public void SERIAL_UDRE()
    {
      //TODO
      //byte tail = serial_tx_buffer_tail; // Temporary serial_tx_buffer_tail (to optimize for volatile)
  
      //#ifdef ENABLE_XONXOFF
      //  if (flow_ctrl == SEND_XOFF) { 
      //    UDR0 = XOFF_CHAR; 
      //    flow_ctrl = XOFF_SENT; 
      //  } else if (flow_ctrl == SEND_XON) { 
      //    UDR0 = XON_CHAR; 
      //    flow_ctrl = XON_SENT; 
      //  } else
      //#endif
      //{ 
      //  // Send a byte from the buffer	
      //  //TODO UDR0 = serial_tx_buffer[tail];
  
      //  // Update tail position
      //  tail++;
      //  if (tail == TX_BUFFER_SIZE) { tail = 0; }
  
      //  serial_tx_buffer_tail = tail;
      //}
  
      // Turn off Data Register Empty Interrupt to stop tx-streaming if this concludes the transfer
      //TODO if (tail == serial_tx_buffer_head) { UCSR0B &= ~(1 << UDRIE0); }
    }


    // Fetches the first byte in the serial read buffer. Called by main program.
    public byte serial_read()
    {
      byte tail = serial_rx_buffer_tail; // Temporary serial_rx_buffer_tail (to optimize for volatile)
      if (serial_rx_buffer_head == tail) {
        return SERIAL_NO_DATA;
      } else {
        byte data = serial_rx_buffer[tail];
    
        tail++;
        if (tail == RX_BUFFER_SIZE) { tail = 0; }
        serial_rx_buffer_tail = tail;

        //#ifdef ENABLE_XONXOFF
        //TODO
          //if ((serial_get_rx_buffer_count() < RX_BUFFER_LOW) && flow_ctrl == XOFF_SENT) { 
          //  flow_ctrl = SEND_XON;
          //  UCSR0B |=  (1 << UDRIE0); // Force TX
          //}
        //#endif
    
        return data;
      }
    }


    //ISR(SERIAL_RX)
    public void SERIAL_RX()
    {
      char data = ' ';//TODO = UDR0;
      byte next_head;
  
      // Pick off runtime command characters directly from the serial stream. These characters are
      // not passed into the buffer, but these set system state flag bits for runtime execution.
      switch (data) {
        case CMD_STATUS_REPORT: bit_true_atomic(ref sys.execute, EXEC_STATUS_REPORT); break; // Set as true
        case CMD_CYCLE_START:   bit_true_atomic(ref sys.execute, EXEC_CYCLE_START); break; // Set as true
        case CMD_FEED_HOLD:     bit_true_atomic(ref sys.execute, EXEC_FEED_HOLD); break; // Set as true
        case CMD_RESET:         mc_reset(); break; // Call motion control reset routine.
        default: // Write character to buffer    
          next_head = (byte)(serial_rx_buffer_head + 1);
          if (next_head == RX_BUFFER_SIZE) { next_head = 0; }
    
          // Write data to buffer unless it is full.
          if (next_head != serial_rx_buffer_tail) {
            serial_rx_buffer[serial_rx_buffer_head] = Convert.ToByte(data);
            serial_rx_buffer_head = next_head;    
        
            //#ifdef ENABLE_XONXOFF
              //TODO
              //if ((serial_get_rx_buffer_count() >= RX_BUFFER_FULL) && flow_ctrl == XON_SENT) {
              //  flow_ctrl = SEND_XOFF;
              //  UCSR0B |=  (1 << UDRIE0); // Force TX
              //} 
            //#endif
        
          }
          //TODO: else alarm on overflow?
          break;
      }
    }


    public void serial_reset_read_buffer() 
    {
      serial_rx_buffer_tail = serial_rx_buffer_head;

      //#ifdef ENABLE_XONXOFF
        flow_ctrl = XON_SENT;
      //#endif
    }

  }
}
