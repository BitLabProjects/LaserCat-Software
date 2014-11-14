using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.Math.Files
{
  public class PathStyle
  {
    public double Thickness;
  }

  public class SvgWriter
  {
    private string mFullFileName;

    public SvgWriter(string fullFileName)
    {
      mFullFileName = fullFileName;
      System.IO.File.WriteAllText(mFullFileName,
                          "<?xml version=\"1.0\" standalone=\"no\"?>\r\n" +
                          "<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 1.1//EN\" \"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd\">\r\n" +
                          "<svg width=\"12cm\" height=\"12cm\" viewBox=\"0 0 600 600\" xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\">\r\n");
    }

    public void WriteBeziers(CubicBezier[] beziers, Dictionary<CubicBezier, PathStyle> styles, int pointRadius, int lineWidth)
    {
      foreach (var bezier in beziers)
      {
        double actualLineWidth = lineWidth;
        if (styles.ContainsKey(bezier))
          actualLineWidth = styles[bezier].Thickness;
        System.IO.File.AppendAllText(mFullFileName, CreateSvgBezierPath(bezier, pointRadius, actualLineWidth));
      }
    }

    public void Complete()
    {
      System.IO.File.AppendAllText(mFullFileName, "</svg>");
    }

    #region "Helpers"
    private string CreateSvgPoint(DblPoint2 p1)
    {
      return CreateSvgPoint(p1, 5);
    }

    private string CreateSvgPoint(DblPoint2 p1, double r)
    {
      return string.Format(GetCI(), "  <circle cx=\"{0}\" cy=\"{1}\" r=\"{2}\"/>\r\n", p1.x, p1.y, r);
    }

    private string CreateSvgBezierPath(CubicBezier bezier, int pointRadius, double lineWidth)
    {
      string result = "";
      result += string.Format(GetCI(), "  <path d=\"M{0},{1} C{2},{3} {4},{5} {6},{7}\" fill=\"none\" stroke=\"red\" stroke-width=\"{8}\"  />\r\n",
                                       TraX(bezier.P0.x), TraY(bezier.P0.y), 
                                       TraX(bezier.C0.x), TraY(bezier.C0.y),
                                       TraX(bezier.C1.x), TraY(bezier.C1.y), 
                                       TraX(bezier.P1.x), TraY(bezier.P1.y),
                                       lineWidth);
      //result += "  <g fill=\"#8888FF\" >\r\n";
      //result += CreateSvgPoint(bezier.P0, pointRadius);
      //result += CreateSvgPoint(bezier.P1, pointRadius);
      //result += "  </g>\r\n";

      //result += "  <g fill=\"#FF8888\" >\r\n";
      //result += CreateSvgPoint(bezier.C0, pointRadius);
      //result += CreateSvgPoint(bezier.C1, pointRadius);
      //result += "  </g>\r\n";
      return result;
    }

    double scale = 10, center = 100;
    private double TraX(double x)
    {
      return (x - center) * scale + 300;
    }

    private double TraY(double y)
    {
      return (y - center) * scale + 300;
    }

    private CultureInfo GetCI()
    {
      return new CultureInfo("en-us");
    }
    #endregion
  }
}
