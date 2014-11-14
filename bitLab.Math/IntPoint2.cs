using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.Math
{
  public struct IntPoint2
  {
    public int x, y;
    public IntPoint2(int xx, int yy)
    {
      x = xx;
      y = yy;
    }

    public IntPoint2 BrownianMotion()
    {
      var randByte = Rnd.Byte();
      switch (randByte % 8)
      {
        case 0:
        case 1:
        case 2:
          x--;
          break;
        case 5:
        case 6:
        case 7:
          x++;
          break;
      }
      switch (randByte % 8)
      {
        case 0:
        case 3:
        case 5:
          y--;
          break;
        case 2:
        case 4:
        case 7:
          y++;
          break;
      }
      return this;
    }

    public IntPoint2 Warp(int minX, int minY, int maxX, int maxY)
    {
      while (x >= maxX) x -= (maxX - minX);
      while (x < minX) x += (maxX - minX);
      while (y >= maxY) y -= (maxY - minY);
      while (y < minY) y += (maxY - minY);
      return this;
    }

    public static implicit operator DblPoint2(IntPoint2 point)
    {
      return new DblPoint2(point.x, point.y);
    }
  }
}
