using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.LaserCat.Grbl
{
  unsafe public partial class GrblFirmware
  {
    //// Useful macros
    //public const int clear_vector(a) memset(a, 0, sizeof(a))
    //public const int clear_vector_float(a) memset(a, 0.0, sizeof(float)*NutsAndBolts.N_AXIS)
    //// public const int clear_vector_long(a) memset(a, 0.0, sizeof(long)*NutsAndBolts.N_AXIS)
    //public const int max(a,b) (((a) > (b)) ? (a) : (b))
    //public const int min(a,b) (((a) < (b)) ? (a) : (b))

    // Bit field and masking macros
    public static byte bit(int n) { return (byte)(1 << n); }
    //public const int bit_true_atomic(x,mask) {byte sreg = SREG; cli(); (x) |= (mask); SREG = sreg; }
    //public const int bit_false_atomic(x,mask) {byte sreg = SREG; cli(); (x) &= ~(mask); SREG = sreg; }
    public void bit_false_atomic(ref byte x, byte mask) { bit_false(ref x, mask); }
    public void bit_true_atomic(ref byte x, byte mask) { bit_true(ref x, mask); }
    //public const int bit_toggle_atomic(x,mask) {byte sreg = SREG; cli(); (x) ^= (mask); SREG = sreg; }
    //public const int bit_true_atomic(x,mask) bit_true(x,mask)
    //public const int bit_false_atomic(x,mask) bit_false(x,mask)
    //public const int bit_toggle_atomic(x,mask) (x) ^= (mask)
    public void bit_true(ref byte x, byte mask) { x |= mask; }
    public void bit_false(ref ushort x, ushort mask) { x &= (ushort)~mask; }
    public void bit_false(ref byte x, byte mask) { x &= (byte)~mask; }
    public bool bit_istrue(int x, int mask) { return (x & mask) != 0; }
    public bool bit_isfalse(int x, int mask) { return (x & mask) == 0; }

    public int trunc(float x)
    {
      return (int)Math.Truncate(x);
    }

    private const int MAX_INT_DIGITS = 8;

    // Read a floating point value from a string. Line points to the input buffer, char_counter 
    // is the indexer pointing to the current character of the line, while float_ptr is 
    // a pointer to the result variable. Returns true when it succeeds
    public bool read_float(char[] line, ref byte char_counter, ref float float_ptr)
    {
      //char *ptr = line + *char_counter;
      byte c;
    
      // Grab first character and increment pointer. No spaces assumed in line.
      c = Convert.ToByte(line[char_counter++]);
  
      // Capture initial positive/minus character
      bool isnegative = false;
      if (c == '-') {
        isnegative = true;
        c = Convert.ToByte(line[char_counter++]);
      } else if (c == '+') {
        c = Convert.ToByte(line[char_counter++]);
      }
  
      // Extract number into fast integer. Track decimal in terms of exponent value.
      uint intval = 0;
      sbyte exp = 0;
      byte ndigit = 0;
      bool isdecimal = false;
      while(true) {
        c -= Convert.ToByte('0');
        if (c <= 9) {
          ndigit++;
          if (ndigit <= MAX_INT_DIGITS) {
            if (isdecimal) { exp--; }
            intval = (((intval << 2) + intval) << 1) + c; // intval*10 + c
          } else {
            if (!(isdecimal)) { exp++; }  // Drop overflow digits
          }
        } else if (c == (('.'-'0') & 0xff)  &&  !(isdecimal)) {
          isdecimal = true;
        } else {
          break;
        }
        c = Convert.ToByte(line[char_counter++]);
      }
  
      // Return if no digits have been read.
      if (ndigit == 0) { return false; };
  
      // Convert integer into floating point.
      float fval;
      fval = (float)intval;
  
      // Apply decimal. Should perform no more than two floating point multiplications for the
      // expected range of E0 to E-4.
      if (fval != 0) {
        while (exp <= -2) {
          fval *= 0.01f; 
          exp += 2;
        }
        if (exp < 0) { 
          fval *= 0.1f; 
        } else if (exp > 0) {
          do {
            fval *= 10.0f;
          } while (--exp > 0);
        } 
      }

      // Assign floating point value with correct sign.    
      if (isnegative) {
        float_ptr = -fval;
      } else {
        float_ptr = fval;
      }

      //SB!Increment is implicit because we're incrementing char_counter at each character read in the function, just go back one
      //*char_counter = ptr - line - 1; // Set char_counter to next statement
      char_counter--;
  
      return true;
    }

    // Delays variable-defined milliseconds. Compiler compatibility fix for _delay_ms().
    public void delay_ms(ushort ms)
    {
    }

    // Delays variable-defined microseconds. Compiler compatibility fix for _delay_us().
    public void delay_us(uint us)
    {
    }

    // Computes hypotenuse, avoiding avr-gcc's bloated version and the extra error checking.
    public float hypot_f(float x, float y)
    {
      return 0;
      //TODO
    }

    public void copyArray(float[] dst, float[] src)
    {
      for (int i = 0; i < src.Length; i++)
        dst[i] = src[i];
    }
    public void copyArray(int[] dst, int[] src)
    {
      for (int i = 0; i < src.Length; i++)
        dst[i] = src[i];
    }
    public void clear_vector(float[] dst)
    {
      for (int i = 0; i < dst.Length; i++)
        dst[i] = 0.0f;
    }
  }
}
