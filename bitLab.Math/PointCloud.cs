using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.Math
{
  public interface IPointCloud
  {
    IEnumerable<DblPoint2> Points { get; }
    QuadTree QuadTree { get; }
  }

  public class PointCloud<TValue>: IPointCloud
    where TValue : class
  {
    private Dictionary<DblPoint2, TValue> mPointsToData;
    private QuadTree mQuadTree;

    public PointCloud(double width, double height)
    {
      mPointsToData = new Dictionary<DblPoint2, TValue>();
      mQuadTree = new QuadTree(new Rectangle(0, 0, width, height));
    }

    public IEnumerable<DblPoint2> Points
    {
      get { return mPointsToData.Keys; }
    }

    public QuadTree QuadTree
    {
      get { return mQuadTree; }
    }

    public void AddPoint(DblPoint2 point, TValue data)
    {
      if (!mPointsToData.ContainsKey(point))
      {
        mPointsToData[point] = data;
        mQuadTree.AddPoint(point);
      }
    }

    public TValue GetData(DblPoint2 point)
    {
      return mPointsToData[point];
    }

    public List<DblPoint2> InsideCircle(DblPoint2 center, double radius)
    {
      var resultFromQT = new List<DblPoint2>();
      mQuadTree.GetPointsInsideCircle(center, radius, resultFromQT);

      //Security check, enable only in case of invalid results
      //var resultFromBruteforce = new List<DblPoint2>();
      //foreach (var p in mPointsToData.Keys)
      //{
      //  if ((p - center).Length < radius)
      //    resultFromBruteforce.Add(p);
      //}
      //System.Diagnostics.Debug.Assert(Enumerable.SequenceEqual(resultFromQT, resultFromBruteforce));

      return resultFromQT;
    }

    public double GetDistange(DblPoint2 center)
    {
      return mQuadTree.GetDistance(center);
    }
  }
}
