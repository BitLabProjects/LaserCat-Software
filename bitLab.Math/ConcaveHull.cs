using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.Math
{
  public class ConcaveHull
  {
    private ConcaveHull() { }

    const int kNearestCount = 3;

    public static Polygon Create(IPointCloud pointCloud, double searchRange, int kNearestCount)
    {
      var result = new Polygon();

      var currPoint = pointCloud.QuadTree.GetLowestYPoint();
      result.Points.Add(currPoint);

      //Uno a sinistra così può calcolare l'angolo in modo uniforme
      var prevPoint = new DblPoint2(currPoint.x - 1, currPoint.y);

      while (true)
      {
        var nextPoint = GetNextPoint(pointCloud, result, prevPoint, currPoint, result.Points.Count < 3 ? result.Points : result.Points.Skip(1), 
                                     searchRange, kNearestCount);
        if (nextPoint == result.Points[0])
          break;

        result.Points.Add(nextPoint);

        prevPoint = currPoint;
        currPoint = nextPoint;
      }

      return result;
    }

    private static DblPoint2 GetNextPoint(IPointCloud pointCloud, Polygon partialResult, DblPoint2 prevPoint, DblPoint2 currPoint, IEnumerable<DblPoint2> excludedPoints,
                                          double searchRange, int kNearestCount)
    {
      while (true) 
      {
        var kNearest = new List<DblPoint2>();
        var actualkNearestCount = kNearestCount;
        while (true)
        {
          pointCloud.QuadTree.GetPointsInsideCircle(currPoint, 1000000.0, kNearest);

          kNearest.RemoveAll((x) => excludedPoints.Contains(x));

          if (kNearest.Count < actualkNearestCount && kNearest.Count != (pointCloud.Points.Count() - excludedPoints.Count()))
          {
            actualkNearestCount++;
            kNearest.Clear();
          }
          else if (kNearest.Count > 3)
          {
            kNearest = (from k in kNearest
                        orderby (k - currPoint).Length2 ascending
                        select k).Take(actualkNearestCount).ToList();
            break;
          }
          else
            break;
        }

        var kNearestOrderedByAngle = (from k in kNearest
                                      orderby (prevPoint - currPoint).CWAngleTo(k - currPoint) descending
                                      select k).ToList();

        //Rimuovi tutti quelli che intersecano il poligono corrente
        var kNearestOrderedByAngleNotIntersecting = (from k in kNearestOrderedByAngle
                                                     where !IntersectAnyPolygonEdgeExceptLast(partialResult, currPoint, k)
                                                     select k).ToList();

        if (kNearestOrderedByAngleNotIntersecting.Count > 0)
          return kNearestOrderedByAngleNotIntersecting.First();
      }
      
    }

    private static Boolean IntersectAnyPolygonEdgeExceptLast(Polygon polygon, DblPoint2 p1, DblPoint2 p2)
    {
      var s12 = new Segment(p1, p2);
      for (var i = 0; i < polygon.Points.Count - 1; i++)
      {
        var result = new Segment(polygon.Points[i], polygon.Points[i + 1]).Intersect(s12);
        if (result.IsIntersection)
          return true;
      }
      return false;
    }
  }
}
