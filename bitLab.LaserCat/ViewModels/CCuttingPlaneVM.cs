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

			mMaxX = 500;
			mMaxY = 500;
			mCenterX = mMaxX / 2;
			mCenterY = mMaxY / 2;
			mScale = mMaxY / 20000.0;
		}

		private GrblFirmware Grbl { get { return CLaserCat.Instance.GrblFirmware; } }

		private int mMaxX;
		private int mMaxY;
		private int mCenterX;
		private int mCenterY;
		private double mScale;

		public int MaxX { get { return mMaxX; } }
		public int MaxY { get { return mMaxY; } }

		private ObservableCollection<CLineVM> mPlannedLines;
		public ObservableCollection<CLineVM> PlannedLines
		{
			get { return mPlannedLines; }
			set { SetAndNotify(ref mPlannedLines, value); }
		}

		private ObservableCollection<CLineVM> mDrawnLines;
		public ObservableCollection<CLineVM> DrawnLines
		{
			get { return mDrawnLines; }
			set { SetAndNotify(ref mPlannedLines, value); }
		}

		private int mCurrX;
		public int CurrX
		{
			get { return mCurrX; }
			set { SetAndNotify(ref mCurrX, value); }
		}

		private int mCurrY;
		public int CurrY
		{
			get { return mCurrY; }
			set { SetAndNotify(ref mCurrY, value); }
		}


		public void Update()
		{
			CurrX = (int)(Grbl.sys.position[0] * mScale) + mCenterX;
			CurrY = (int)(-Grbl.sys.position[1] * mScale) + mCenterY;
			//CurrX = CurrX + 1;
			//CurrY = CurrY + 1;

		}

		private double GetNormalizedX(double x)
		{
			return x * mScale + mCenterX;
		}

		private double GetNormalizedY(double y)
		{
			return -y * mScale + mCenterY;
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
					var line = Grbl.plan_lines.Last();
					AddPlannedLine(GetNormalizedX(line.end[0]), GetNormalizedY(line.end[1])); break;
				case EPlannerBlockChangedState.BlockRemoved:
					RemovePlannedLine(); break;
			}
		}

		public void AddPlannedLine(double x, double y)
		{
			CLineVM newLine = new CLineVM();
			if (mPlannedLines.Count > 0)
			{
				newLine.X1 = mPlannedLines.Last().X2;
				newLine.Y1 = mPlannedLines.Last().Y2;
			}
			else
			{
				newLine.X1 = newLine.X2 = newLine.Y1 = newLine.Y2 = 0;
			}

			newLine.X2 = x;
			newLine.Y2 = y;
			//ssl
			PlannedLines.Add(newLine);
		}

		public void RemovePlannedLine()
		{
			AddDrawnLine(PlannedLines.First().X2, PlannedLines.First().Y2);
			PlannedLines.Remove(PlannedLines.First());
		}


		public void AddDrawnLine(double x, double y)
		{
			CLineVM newLine = new CLineVM();
			if (mDrawnLines.Count > 0)
			{
				newLine.X1 = mDrawnLines.Last().X2;
				newLine.Y1 = mDrawnLines.Last().Y2;
			}
			else
			{
				newLine.X1 = newLine.X2 = newLine.Y1 = newLine.Y2 = 0;
			}

			newLine.X2 = x;
			newLine.Y2 = y;

			DrawnLines.Add(newLine);
		}
	}
}
