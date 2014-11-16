using bitLab.LaserCat.Grbl;
using bitLab.LaserCat.Model;
using bitLab.ViewModel;
using bitLab.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.LaserCat.ViewModels
{
	public class CLineVM : CBaseVM
	{

		private double mX1;
		public double X1
		{
			get { return mX1; }
			set
			{
				SetAndNotify(ref mX1, value);				
			}
		}

		private double mX2;
		public double X2
		{
			get { return mX2; }
			set
			{
				SetAndNotify(ref mX2, value);
			}
		}

		private double mY1;
		public double Y1
		{
			get { return mY1; }
			set
			{
				SetAndNotify(ref mY1, value);
			}
		}

		private double mY2;
		public double Y2
		{
			get { return mY2; }
			set
			{
				SetAndNotify(ref mY2, value);
			}
		}

	}
}
