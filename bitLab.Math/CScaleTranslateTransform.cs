using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.Math
{
  public struct ScaleTranslateTransform
  {
    private DblPoint2 mScale;
    private DblPoint2 mTranslate;

    public ScaleTranslateTransform(DblPoint2 scale, DblPoint2 translate)
    {
      mScale = scale;
      mTranslate = translate;
    }

    public DblPoint2 Apply(DblPoint2 input)
    {
      return new DblPoint2(input.x * mScale.x + mTranslate.x, input.y * mScale.y + mTranslate.y);
    }
  }
}
