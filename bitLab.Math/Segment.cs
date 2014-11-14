using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.Math
{
  public class Segment
  {
    public readonly DblPoint2 P1, P2;

    public Segment(DblPoint2 p1, DblPoint2 p2)
    {
      this.P1 = p1;
      this.P2 = p2;
    }

    public SegmentsIntersectionResult Intersect(Segment other)
    {
      var result = new SegmentsIntersectionResult();
      result.IsIntersection = false;

      var S = P2 - P1;
      var otherS = other.P2 - other.P1;
      //t = (other.P1 − P1) × otherS / (S × otherS)
      //u = (other.P1 − P1) × S / (S × otherS)

      var otherP1ToP1 = (other.P1 - P1);
      var den = S * otherS;

      var t = otherP1ToP1 * otherS;
      var u = otherP1ToP1 * S;

      //Actually it's more complex than this, if only one is 0 it means parallel, otherwise coincident
      if (t == 0 || u == 0 || den == 0)
        return result;

      t /= den;
      u /= den;

      if (t < 0 || t > 1 || u < 0 || u > 1)
        return result;

      result.IsIntersection = true;
      result.Point = P1 + S * t;

      var otherLeftNormal = otherS.Rotated90CCW.Normalized;
      var projOnNormal = (-otherP1ToP1).Dot(otherLeftNormal);
      if (projOnNormal > 0)
        result.Normal = otherLeftNormal;
      else
        result.Normal = -otherLeftNormal;

      return result;
    }
  }

  public struct SegmentsIntersectionResult
  {
    public bool IsIntersection;
    public DblPoint2 Point;
    public DblPoint2 Normal;
  }
}
