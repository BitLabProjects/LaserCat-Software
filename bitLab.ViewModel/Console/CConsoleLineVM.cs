using bitLab.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.ViewModel.Console
{
  public class CConsoleLineVM
  {
    private string mText;
    private CColor mColor;

    internal CConsoleLineVM(string text, CColor color)
    {
      mText = text;
      mColor = color;
    }

    public string Text { get { return mText; } }
    public CColor Color { get { return mColor; } }
  }
}
