using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace bitLab.LaserCat.Converters
{
  class CEnumToStringConverter: IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (value == null)
        return Binding.DoNothing;

      if (!value.GetType().IsEnum)
        return Binding.DoNothing;

      var dict = new Dictionary<String, String>();
      foreach(var pair in (parameter as String).Split('|')) {
        var kvp = pair.Split('>');
        dict.Add(kvp[0], kvp[1]);
      }

      String result = null;
      if (dict.TryGetValue(Enum.GetName(value.GetType(), value), out result))
        return result;
      return Binding.DoNothing;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return Binding.DoNothing;
    }
  }
}
