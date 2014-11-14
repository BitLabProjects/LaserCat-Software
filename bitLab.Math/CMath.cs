using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.Math
{
  public class CMath
  {
    private CMath() { }

    public static bool AreClose(double value1, double value2)
    {
      //in case they are Infinities (then epsilon check does not work)
      if (value1 == value2) return true;
      // This computes (|value1-value2| / (|value1| + |value2| + 10.0)) < DBL_EPSILON
      double eps = (System.Math.Abs(value1) + System.Math.Abs(value2) + 10.0) * 1e-6;
      double delta = value1 - value2;
      return (-eps < delta) && (eps > delta);
    }

    public static bool LessThanOrEqual(double value1, double value2)
    {
      return (value1 < value2) || AreClose(value1, value2);
    }
  }
}
