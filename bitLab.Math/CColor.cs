using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.Math
{
  public class CColor
  {
    public byte R, G, B;

    public CColor()
    {
      R = G = B = 0;
    }

    public CColor(byte r, byte g, byte b)
    {
      R = r;
      G = g;
      B = b;
    }

    public byte GetValue()
    {
      return System.Math.Max(System.Math.Max(R, G), B);
    }

    public CColor SetValue(byte value)
    {
      var increment = value - GetValue();
      return AddAll(increment);
    }

    public CColor GetOpposite()
    {
      return new CColor((byte)(255 - R), 
                        (byte)(255 - G), 
                        (byte)(255 - B));
    }

    public static CColor operator ^(CColor c1, CColor c2)
    {
      return new CColor((byte)(c1.R ^ c2.R),
                        (byte)(c1.G ^ c2.G),
                        (byte)(c1.B ^ c2.B));
    }

    public CColor AddAll(int value)
    {
      return new CColor((byte)System.Math.Max(0, System.Math.Min(255, R + value)),
                        (byte)System.Math.Max(0, System.Math.Min(255, G + value)),
                        (byte)System.Math.Max(0, System.Math.Min(255, B + value)));
    }
  }
}
