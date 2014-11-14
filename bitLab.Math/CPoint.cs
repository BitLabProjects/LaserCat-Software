using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.Math
{
  public class CPoint
  {
    public double X, Y;

    public CPoint()
    {
      X = 0.0;
      Y = 0.0;
    }

    public CPoint(double x, double y)
    {
      X = x;
      Y = y;
    }

    public double DistanceTo(double xTo, double yTo)
    {
      return System.Math.Sqrt(System.Math.Pow(X - xTo, 2) + System.Math.Pow(Y - yTo, 2));
    }

    public double Length
    {
      get
      {
        return System.Math.Sqrt(Length2);
      }
    }

    public double Length2
    {
      get
      {
        return System.Math.Pow(X, 2) + System.Math.Pow(Y, 2);
      }
    }

    public CPoint Perpendicular()
    {
      return new CPoint(-Y, X);
    }

    public double Dot(CPoint other)
    {
      return (X * other.X) + (Y * other.Y);
    }

    public static CPoint operator -(CPoint p1, CPoint p2)
    {
      return new CPoint(p1.X - p2.X, p1.Y - p2.Y);
    }

    public static CPoint operator +(CPoint p1, CPoint p2)
    {
      return new CPoint(p1.X + p2.X, p1.Y + p2.Y);
    }

    public static CPoint operator *(CPoint p1, double t)
    {
      return new CPoint(p1.X * t, p1.Y * t);
    }

    public CPoint Normalized()
    {
      var len = Length;
      if (CMath.AreClose(len, 0.0))
        return new CPoint(0.0, 0.0);
      else
        return new CPoint(X / len, Y / len);
    }

    public CPoint Rotated(double angle)
    {
      return new CPoint(System.Math.Cos(angle) * X - System.Math.Sin(angle) * Y,
                        System.Math.Sin(angle) * X + System.Math.Cos(angle) * Y);
    }
  }
}
