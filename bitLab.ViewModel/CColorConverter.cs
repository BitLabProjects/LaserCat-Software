using bitLab.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace bitLab.ViewModel
{
  public class CColorConverter
  {
    private CColorConverter()
    {
    }

    public static Brush CColorToBrush(CColor color)
    {
      return new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));
    }
  }
}
