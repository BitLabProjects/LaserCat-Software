using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.Math
{
  public struct DblPoint2
  {
    public double x, y;
    public DblPoint2(double xx, double yy)
    {
      x = xx;
      y = yy;
    }

    public static DblPoint2 Zero { get { return new DblPoint2(0, 0); } }

    public double Length { get { return System.Math.Sqrt(x * x + y * y); } }
    public double Length2 { get { return x * x + y * y; } }

    public static DblPoint2 operator +(DblPoint2 c1, DblPoint2 c2)
    {
      return new DblPoint2(c1.x + c2.x, c1.y + c2.y);
    }

    public static DblPoint2 operator -(DblPoint2 c1, DblPoint2 c2)
    {
      return new DblPoint2(c1.x - c2.x, c1.y - c2.y);
    }

    public static DblPoint2 operator -(DblPoint2 c1)
    {
      return new DblPoint2(-c1.x, -c1.y);
    }

    public static Boolean operator ==(DblPoint2 c1, DblPoint2 c2)
    {
      return (c1.x == c2.x) && (c1.y == c2.y);
    }

    public static Boolean operator !=(DblPoint2 c1, DblPoint2 c2)
    {
      return !(c1 == c2);
    }

    public static DblPoint2 operator *(DblPoint2 c1, double s)
    {
      return new DblPoint2(c1.x * s, c1.y * s);
    }

    public static double operator *(DblPoint2 c1, DblPoint2 c2)
    {
      return c1.x * c2.y - c1.y * c2.x;
    }

    public static implicit operator IntPoint2(DblPoint2 point)
    {
      return new IntPoint2((int)point.x, (int)point.y);
    }

    public DblPoint2 Rotated90CCW { get { return new DblPoint2(-y, x); } }
    public DblPoint2 Normalized
    {
      get
      {
        var l = Length;
        if (l < 0.00001)
          return DblPoint2.Zero;
        else
          return new DblPoint2(x / l, y / l);
      }
    }

    public double Dot(DblPoint2 other)
    {
      return x * other.x + y * other.y;
    }

    //public double AngleTo(DblPoint2 other)
    //{
    //  return System.Math.Acos(Dot(other) / (Length + other.Length)
    //}

    public double CWAngleTo(DblPoint2 other)
    {
      //angle of 2 relative to 1= atan2(v2.y,v2.x) - atan2(v1.y,v1.x)
      var result = System.Math.Atan2(y, x) - System.Math.Atan2(other.y, other.x);

      //Riporta da 0 a 2pi
      if (result > 0)
        return result;
      else
        return result + 2 * System.Math.PI;
    }

    //public DblPoint2 BrownianMotion(double distance)
    //{
    //  var x = Rnd.DblPoint2(2, 2) - new DblPoint2(1, 1);
    //  this += x * distance;
    //  return this;
    //}

    public DblPoint2 Warp(double minX, double minY, double maxX, double maxY)
    {
      while (x >= maxX) x -= (maxX - minX);
      while (x < minX) x += (maxX - minX);
      while (y >= maxY) y -= (maxY - minY);
      while (y < minY) y += (maxY - minY);
      return this;
    }

    public override string ToString()
    {
      return string.Format("DblPoint2 x={0}, y={1}", x, y);
    }
  }
}
