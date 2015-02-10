using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.LaserCat.Grbl
{
  internal static class CHelpers
  {
    public static int ListReadInt32(List<byte> list, ref int index)
    {
      var i = index;
      index += 4;
      return ((int)list.ElementAt(i)) +
             ((int)list.ElementAt(i + 1) << 8) +
             ((int)list.ElementAt(i + 2) << 16) +
             ((int)list.ElementAt(i + 3) << 24);
    }
  }
}
