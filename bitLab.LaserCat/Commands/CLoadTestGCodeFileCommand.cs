using bitLab.LaserCat.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.LaserCat.Commands
{
  class CLoadTestGCodeFileCommand
  {
    public void Execute()
    {
      string TestGCodeFile = Path.Combine(Environment.CurrentDirectory, @"..\..\Data\TestGCode.nc");
      CLaserCat.Instance.LoadGCode(TestGCodeFile);
    }
  }
}
