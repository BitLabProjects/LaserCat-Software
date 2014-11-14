using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.Math
{
  public class Polygon
  {
    private List<DblPoint2> mPoints;

    public Polygon()
    {
      mPoints = new List<DblPoint2>();
    }

    public List<DblPoint2> Points { get { return mPoints; } }

    public string ToString()
    {
      return mPoints.Aggregate<DblPoint2, string>(
           string.Empty,
           (agg, p) => agg + (agg == string.Empty ? "" : " ") + String.Format("{0},{1}", p.x, p.y));
    }
  }
}
