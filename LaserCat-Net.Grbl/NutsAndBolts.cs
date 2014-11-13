using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace grbl_pcnet
{
  unsafe public partial class Grbl
  {
    //// Useful macros
    //public const int clear_vector(a) memset(a, 0, sizeof(a))
    //public const int clear_vector_float(a) memset(a, 0.0, sizeof(float)*N_AXIS)
    //// public const int clear_vector_long(a) memset(a, 0.0, sizeof(long)*N_AXIS)
    //public const int max(a,b) (((a) > (b)) ? (a) : (b))
    //public const int min(a,b) (((a) < (b)) ? (a) : (b))

    // Bit field and masking macros
    public static byte bit(int n) { return (byte)(1 << n); }
    //public const int bit_true_atomic(x,mask) {uint8_t sreg = SREG; cli(); (x) |= (mask); SREG = sreg; }
    //public const int bit_false_atomic(x,mask) {uint8_t sreg = SREG; cli(); (x) &= ~(mask); SREG = sreg; }
    public void bit_false_atomic(ref byte x, byte mask) { bit_false(ref x, mask); }
    public void bit_true_atomic(ref byte x, byte mask) { bit_true(ref x, mask); }
    //public const int bit_toggle_atomic(x,mask) {uint8_t sreg = SREG; cli(); (x) ^= (mask); SREG = sreg; }
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

    // Read a floating point value from a string. Line points to the input buffer, char_counter 
    // is the indexer pointing to the current character of the line, while float_ptr is 
    // a pointer to the result variable. Returns true when it succeeds
    byte read_float(char[] line, ref byte char_counter, ref float float_ptr)
    {
      return 0;
      //TODO
    }

    // Delays variable-defined milliseconds. Compiler compatibility fix for _delay_ms().
    void delay_ms(ushort ms)
    {
    }

    // Delays variable-defined microseconds. Compiler compatibility fix for _delay_us().
    void delay_us(uint us)
    {
    }

    // Computes hypotenuse, avoiding avr-gcc's bloated version and the extra error checking.
    float hypot_f(float x, float y)
    {
      return 0;
      //TODO
    }

    void copyArray(float[] dst, float[] src)
    {
      for (int i = 0; i < src.Length; i++)
        dst[i] = src[i];
    }
    void copyArray(int[] dst, int[] src)
    {
      for (int i = 0; i < src.Length; i++)
        dst[i] = src[i];
    }
    void clear_vector(float[] dst)
    {
      for (int i = 0; i < dst.Length; i++)
        dst[i] = 0.0f;
    }
  }
}
