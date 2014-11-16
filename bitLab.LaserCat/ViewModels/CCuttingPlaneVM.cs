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

namespace bitLab.LaserCat.ViewModels
{
	public class CCuttingPlaneVM : CBaseVM
	{
		public CCuttingPlaneVM()
		{
			mPlannedLines = new List<Line>();
			mDrawnLines = new List<Line>();

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

		private List<Line> mPlannedLines;
		public List<Line> PlannedLines
		{
			get { return mPlannedLines; }
			set { SetAndNotify(ref mPlannedLines, value); }
		}

		private List<Line> mDrawnLines;
		public List<Line> DrawnLines
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


		public void AddPlannedLine(int x, int y)
		{
			Line newLine = new Line();
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

			mPlannedLines.Add(newLine);
		}

		public void AddDrawnLine(int x, int y)
		{
			Line newLine = new Line();
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

			mDrawnLines.Add(newLine);
		}
	}
}
