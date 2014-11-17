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
    private DblPoint2 mP1, mP2;
    private DblPoint2 mP1Transformed, mP2Transformed;

    public CLineVM(DblPoint2 P1, DblPoint2 P2, ScaleTranslateTransform transform)
    {
      mP1 = P1;
      mP2 = P2;
      ApplyTransform(transform);
    }

    public void ApplyTransform(ScaleTranslateTransform transform)
    {
      mP1Transformed = transform.Apply(mP1);
      mP2Transformed = transform.Apply(mP2);
      Notify("");
    }

    public DblPoint2 P1 { get { return mP1; } }
    public DblPoint2 P2 { get { return mP2; } }
		public double X1 { get { return mP1Transformed.x; } }
    public double Y1 { get { return mP1Transformed.y; } }
    public double X2 { get { return mP2Transformed.x; } }
    public double Y2 { get { return mP2Transformed.y; } }
	}
}
