using bitLab.LaserCat.Grbl;
using bitLab.LaserCat.Model;
using bitLab.ViewModel;
using bitLab.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace bitLab.LaserCat.ViewModels
{
  public class CCuttingPlaneVM : CBaseVM
  {
    public CCuttingPlaneVM()
    {
      mPlannedLines = new ObservableCollection<CLineVM>();
      mDrawnLines = new ObservableCollection<CLineVM>();
      Grbl.PlannerBlocksChanged += mGrbl_PlannerBlocksChanged;

      mCuttingPlaneSize = new DblPoint2(500, 500);
      CalculateTransform();
    }

    private void CalculateTransform()
    {
      //Fitting of the world square inside the CuttingPlaneSize
      double worldWidth = 20000;
      double worldHeight = 20000;

      double scale;
      if (mCuttingPlaneSize.x / mCuttingPlaneSize.y > worldWidth / worldHeight)
        //The first dimension to hit the border is height, because cuttingplane has a wider aspect than world
        scale = mCuttingPlaneSize.y / worldHeight;
      else
        scale = mCuttingPlaneSize.x / worldWidth;

      mWorldToCuttingPlaneTransform = new ScaleTranslateTransform(new DblPoint3(scale, -scale, 1),
                                                                  new DblPoint3(mCuttingPlaneSize.x / 2,
                                                                                mCuttingPlaneSize.y / 2,
                                                                                0));
    }
    public void Resize(double newWidth, double newHeight)
    {
      mCuttingPlaneSize = new DblPoint2(newWidth, newHeight);
      CalculateTransform();
      mSysPositionTransformed = mWorldToCuttingPlaneTransform.Apply(mSysPosition);
      foreach (var lineVM in mPlannedLines)
        lineVM.ApplyTransform(mWorldToCuttingPlaneTransform);
      foreach (var lineVM in mDrawnLines)
        lineVM.ApplyTransform(mWorldToCuttingPlaneTransform);
      Notify("CuttingPlaneSizeX");
      Notify("CuttingPlaneSizeY");
      Notify("CurrX");
      Notify("CurrY");
    }

    private GrblFirmware Grbl { get { return CLaserCat.Instance.GrblFirmware; } }

    private DblPoint2 mCuttingPlaneSize;
    private ScaleTranslateTransform mWorldToCuttingPlaneTransform;

    public double CuttingPlaneSizeX { get { return mCuttingPlaneSize.x; } }
    public double CuttingPlaneSizeY { get { return mCuttingPlaneSize.y; } }

    private ObservableCollection<CLineVM> mPlannedLines;
    public ObservableCollection<CLineVM> PlannedLines { get { return mPlannedLines; } }

    private ObservableCollection<CLineVM> mDrawnLines;
    public ObservableCollection<CLineVM> DrawnLines { get { return mDrawnLines; } }

    private DblPoint2 mSysPosition;
    private DblPoint2 mSysPositionTransformed;
    public double CurrX { get { return mSysPositionTransformed.x; } }
    public double CurrY { get { return mSysPositionTransformed.y; } }


    public void Update()
    {
      //SB! Save original coordinates and normalize them in the property getter so that we can dinamically size the 
      //drawing surface
      mSysPosition = new DblPoint2(Grbl.sys.position[0], Grbl.sys.position[1]);
      mSysPositionTransformed = mWorldToCuttingPlaneTransform.Apply(mSysPosition);
      Notify("CurrX");
      Notify("CurrY");
    }

    private void mGrbl_PlannerBlocksChanged(object sender, CPlannerBlocksChangedEventArgs e)
    {
      Dispatcher.Invoke(() => mGrbl_PlannerBlocksChangedDo(e));
    }

    private void mGrbl_PlannerBlocksChangedDo(CPlannerBlocksChangedEventArgs e)
    {
      switch (e.PlannerBlocksChangedState)
      {
        case EPlannerBlockChangedState.BlockAdded:
          AddLineToList(mPlannedLines, e.Target); 
          break;
        case EPlannerBlockChangedState.BlockRemoved:
          RemovePlannedLine(); 
          break;
      }
    }

    public void AddLineToList(ObservableCollection<CLineVM> list, DblPoint3 DstPoint)
    {
      if (list.Count > 0)
        PlannedLines.Add(new CLineVM(list.Last().P2, DstPoint, mWorldToCuttingPlaneTransform));
      else
        PlannedLines.Add(new CLineVM(new DblPoint3(0, 0, 0), DstPoint, mWorldToCuttingPlaneTransform));
    }

    public void RemovePlannedLine()
    {
      var lineToRemove = PlannedLines.First();
      PlannedLines.RemoveAt(0);
      mDrawnLines.Add(lineToRemove);
    }
  }
}
