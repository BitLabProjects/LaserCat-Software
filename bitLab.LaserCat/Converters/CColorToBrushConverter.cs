using bitLab.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace bitLab.LaserCat.Converters
{
  class CColorToBrushConverter: IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      var color = value as CColor;
      if (color == null)
        return Binding.DoNothing;
      return new SolidColorBrush(Color.FromArgb(255, color.R, color.G, color.B));
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return Binding.DoNothing;
    }
  }
}
