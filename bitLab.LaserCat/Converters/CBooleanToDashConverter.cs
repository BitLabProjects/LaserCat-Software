using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace bitLab.LaserCat.Converters
{
  public class CBooleanToDashConverter: IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if ((bool)value)
      {
        var dc = new DoubleCollection();
        dc.Add(1);
        dc.Add(3);
        return dc;
      }
      else
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return Binding.DoNothing;
    }
  }
}
