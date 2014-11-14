using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.Math
{
  public class CubicBezier
  {
    public DblPoint2 P0, P1, C0, C1;

    public static CubicBezier[] Interpolate(DblPoint2[] Points, DblPoint2 prePoint, DblPoint2 postPoint)
    {
      if (Points.Length < 2)
        throw new ArgumentException("Too few points to interpolate");

      var beziersNecessary = Points.Length - 1;
      var result = new CubicBezier[beziersNecessary];

      for (int i = 0; i < beziersNecessary; i++)
      {
        DblPoint2 Pa, Pb;
        if (i == 0)
          Pa = prePoint;
        else
          Pa = Points[i - 1];
        if (i == beziersNecessary - 1)
          Pb = postPoint;
        else
          Pb = Points[i + 2];
        result[i] = Interpolate(Pa, Points[i], Points[i + 1], Pb);
      }

      return result;
    }

    public static CubicBezier Interpolate(DblPoint2 Pa, DblPoint2 P0, DblPoint2 P1, DblPoint2 Pb)
    {
      var result = new CubicBezier();
      result.P0 = P0;
      result.P1 = P1;
      result.C0 = InterpolateControlPoint(Pa, P0, P1);
      result.C1 = InterpolateControlPoint(Pb, P1, P0);
      return result;
    }

    private static DblPoint2 InterpolateControlPoint(DblPoint2 Pa, DblPoint2 P0, DblPoint2 P1)
    {
      //<BlackMagic>
      var Ma0 = (Pa + P0) * 0.5;
      var M01 = (P0 + P1) * 0.5;
      var lenPaP0 = (Pa - P0).Length;
      var lenP0P1 = (P0 - P1).Length;
      var Q1 = Ma0 + (M01 - Ma0) * (lenPaP0 / (lenPaP0 + lenP0P1));
      return P0 + (M01 - Q1);
      //</BlackMagic>
    }
  }
}
