using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.Math
{
  public struct ScaleTranslateTransform
  {
    private DblPoint3 mScale;
    private DblPoint3 mTranslate;

    public ScaleTranslateTransform(DblPoint3 scale, DblPoint3 translate)
    {
      mScale = scale;
      mTranslate = translate;
    }

    public DblPoint2 Apply(DblPoint2 input)
    {
      return new DblPoint2(input.x * mScale.x + mTranslate.x, 
                           input.y * mScale.y + mTranslate.y);
    }

    public DblPoint3 Apply(DblPoint3 input)
    {
      return new DblPoint3(input.x * mScale.x + mTranslate.x, 
                           input.y * mScale.y + mTranslate.y,
                           input.z * mScale.z + mTranslate.z);
    }
  }
}
