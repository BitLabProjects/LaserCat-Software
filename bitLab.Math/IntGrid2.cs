using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.Math
{
  public class IntGrid2<CellType> where CellType : class, new()
  {
    private CellsCollection mCellsColl;
    private int mWidth, mHeight;

    public IntGrid2(int width, int height)
    {
      mWidth = width;
      mHeight = height;

      var cells = new CellType[width, height];
      for (int i = 0; i < mWidth; i++)
        for (int j = 0; j < mHeight; j++)
          cells[i, j] = new CellType();

      mCellsColl = new CellsCollection(cells);
    }

    public CellsCollection Cells { get { return mCellsColl; } }

    public IntPoint2 RandomPointOnBorder()
    {
      if (Rnd.Int(2) == 0)
        return new IntPoint2(0, Rnd.Int(mHeight));
      else
        return new IntPoint2(Rnd.Int(mWidth), 0);
    }

    public void Apply(Action<CellType> fun)
    {
      for (int i = 0; i < mWidth; i++)
        for (int j = 0; j < mHeight; j++)
          fun(mCellsColl[i, j]);
    }

    public bool NeighbourWhere(IntPoint2 point, Predicate<CellType> predicate, ref IntPoint2 result)
    {
      int w = mWidth, h = mHeight;
      IntPoint2[] pts = { new IntPoint2(point.x, point.y - 1).Warp(0, 0, w, h),
                          new IntPoint2(point.x, point.y + 1).Warp(0, 0, w, h),
                          new IntPoint2(point.x - 1, point.y).Warp(0, 0, w, h),
                          new IntPoint2(point.x + 1, point.y).Warp(0, 0, w, h),
                          new IntPoint2(point.x - 1, point.y - 1).Warp(0, 0, w, h),
                          new IntPoint2(point.x + 1, point.y - 1).Warp(0, 0, w, h),
                          new IntPoint2(point.x - 1, point.y + 1).Warp(0, 0, w, h),
                          new IntPoint2(point.x + 1, point.y + 1).Warp(0, 0, w, h) };

      var ptsTrue = (from p in pts
                     where predicate(Cells[p])
                     select p).ToList();

      if (ptsTrue.Count > 0)
      {
        result = ptsTrue[Rnd.Int(ptsTrue.Count)];
        return true;
      }
      else
        return false;
      //p = new IntPoint2(point.x, point.y - 1).Warp(0, 0, w, h);
      //if (predicate(Cells[p])) { result = p; return true; }
      //p = new IntPoint2(point.x, point.y + 1).Warp(0, 0, w, h);
      //if (predicate(Cells[p])) { result = p; return true; }
      //p = new IntPoint2(point.x - 1, point.y).Warp(0, 0, w, h);
      //if (predicate(Cells[p])) { result = p; return true; }
      //p = new IntPoint2(point.x + 1, point.y).Warp(0, 0, w, h);
      //if (predicate(Cells[p])) { result = p; return true; }

      //p = new IntPoint2(point.x - 1, point.y - 1).Warp(0, 0, w, h);
      //if (predicate(Cells[p])) { result = p; return true; }
      //p = new IntPoint2(point.x + 1, point.y - 1).Warp(0, 0, w, h);
      //if (predicate(Cells[p])) { result = p; return true; }
      //p = new IntPoint2(point.x - 1, point.y + 1).Warp(0, 0, w, h);
      //if (predicate(Cells[p])) { result = p; return true; }
      //p = new IntPoint2(point.x + 1, point.y + 1).Warp(0, 0, w, h);
      //if (predicate(Cells[p])) { result = p; return true; }

      //return false;
    }

    public class CellsCollection
    {
      private CellType[,] mCells;
      public CellsCollection(CellType[,] cells)
      {
        mCells = cells;
      }

      public CellType this[int x, int y]
      {
        get
        {
          return mCells[x, y];
        }
        set
        {
          mCells[x, y] = value;
        }
      }

      public CellType this[IntPoint2 p]
      {
        get
        {
          return mCells[p.x, p.y];
        }
        set
        {
          mCells[p.x, p.y] = value;
        }
      }
    }
  }

  
}
