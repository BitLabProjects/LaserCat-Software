using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.Math
{
  public class QuadTree
  {
    private const int PointCapacity = 10;
    private const double MinimunCellWidth = 10;

    private Rectangle mBounds;
    private QuadTree mTopLeft, mTopRight, mBottomLeft, mBottomRight;
    private List<DblPoint2> mPoints;
    private int mDepth;

    public QuadTree(Rectangle bounds): this(bounds, 0)
    {
    }

    public QuadTree(Rectangle bounds, int depth)
    {
      mDepth = depth;
      mTopLeft = mTopRight = mBottomLeft = mBottomRight = null;
      mBounds = bounds;
      mPoints = new List<DblPoint2>();
    }

    public DblPoint2 Center
    {
      get
      {
        return new DblPoint2(mBounds.X + mBounds.Width / 2.0,
                             mBounds.Y + mBounds.Height / 2.0);
      }
    }

    public void AddPoint(DblPoint2 p)
    {
      if (mTopLeft == null)
      {
        mPoints.Add(p);
        if (mPoints.Count == PointCapacity && mBounds.Width > MinimunCellWidth)
          Split();
      }
      else
      {
        var subQT = Classify(p);
        subQT.AddPoint(p);
      }
    }

    private QuadTree Classify(DblPoint2 p)
    {
      var deltaC = (p - this.Center);
      var isLeft = deltaC.x <= 0;
      var isBottom = deltaC.y <= 0;

      if (isLeft)
        if (isBottom)
          return mBottomLeft;
        else
          return mTopLeft;
      else
        if (isBottom)
          return mBottomRight;
        else
          return mTopRight;
    }

    private void Split()
    {
      var w2 = mBounds.Width  / 2.0;
      var h2 = mBounds.Height / 2.0;
      mBottomLeft  = new QuadTree(new Rectangle(mBounds.X     , mBounds.Y     , w2, h2), mDepth + 1);
      mBottomRight = new QuadTree(new Rectangle(mBounds.X + w2, mBounds.Y     , w2, h2), mDepth + 1);
      mTopLeft     = new QuadTree(new Rectangle(mBounds.X     , mBounds.Y + h2, w2, h2), mDepth + 1);
      mTopRight    = new QuadTree(new Rectangle(mBounds.X + w2, mBounds.Y + h2, w2, h2), mDepth + 1);
      
      //Riaggiungili tutti sui figli e poi svuota
      foreach (var p in mPoints)
        AddPoint(p);

      mPoints.Clear();
    }

#region "Query methods"
    public void GetPointsInsideCircle(DblPoint2 center, double radius, List<DblPoint2> result)
    {
      if (mTopLeft == null)
      {
        var radius2 = radius * radius;
        foreach (var p in mPoints)
          if ((p - center).Length2 < radius2)
            result.Add(p);
      }
      else
      {
        var deltaC = (center - this.Center);
        /*         |
         *         |--r>0--
         *         |
         * --------c--------
         *         |
         *         |
         *         |
         */
        //Compare with radius instead of with 0, to include points on the right quadrant that are near the border
        if (deltaC.x < radius && deltaC.y < radius)
          mBottomLeft.GetPointsInsideCircle(center, radius, result);
        if (deltaC.x > -radius && deltaC.y < radius)
          mBottomRight.GetPointsInsideCircle(center, radius, result);
        if (deltaC.x < radius && deltaC.y > -radius)
          mTopLeft.GetPointsInsideCircle(center, radius, result);
        if (deltaC.x > -radius && deltaC.y > -radius)
          mTopRight.GetPointsInsideCircle(center, radius, result);
      }
    }

    public double GetDistance(DblPoint2 center)
    {
      if (mTopLeft == null)
      {
        var result = double.MaxValue;
        foreach (var p in mPoints)
        {
          var d = (p - center).Length2;
          if (d < result)
            result = d;
        }
        return System.Math.Sqrt(result);
      }
      else
      {
        var resultTop = System.Math.Min(mTopLeft.GetDistance(center), mTopRight.GetDistance(center));
        var resultBottom = System.Math.Min(mBottomLeft.GetDistance(center), mBottomRight.GetDistance(center));
        return System.Math.Min(resultTop, resultBottom);
      }
    }

    public DblPoint2 GetLowestYPoint()
    {
      if (mTopLeft == null)
      {
        var lowestY = mPoints.First();
        for (var i = 1; i < mPoints.Count; i++)
        {
          if (mPoints[i].y < lowestY.y)
            lowestY = mPoints[i];
        }
        return lowestY;
      }
      else
      {
        var lowestBottomLeft = mBottomLeft.GetLowestYPoint();
        var lowestBottomRight = mBottomRight.GetLowestYPoint();
        if (lowestBottomLeft.y < lowestBottomRight.y)
          return lowestBottomLeft;
        else
          return lowestBottomRight;
      }
    }
#endregion
  }
}
