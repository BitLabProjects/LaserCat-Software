using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.Math
{
  public class CColors
  {
    private CColors()
    {
    }

    public static readonly CColor Black = new CColor(  0,   0,   0);
    public static readonly CColor White = new CColor(255, 255, 255);
    public static readonly CColor Red   = new CColor(255,   0,   0);
    public static readonly CColor SteelBlue     = new CColor(160, 180, 240);
    public static readonly CColor DarkSteelBlue = new CColor( 65, 105, 225);
    public static readonly CColor Orange        = new CColor(255, 127,  39);
    public static readonly CColor Salmon        = new CColor(255, 155,  90);
  }
}
