using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using bitLab.ViewModel;
using bitLab.ViewModel.Console;

namespace bitLab.LaserCat.ViewModels
{
  public class CLaserCatVM: CBaseVM
  {
    public CConsoleVM ConsoleVM { get; set; }
    public CLaserCatVM()
    {
      ConsoleVM = new CConsoleVM(false);
      LoadTestGCode = new CDelegateCommand((obj) => { new Commands.CLoadTestGCodeFileCommand().Execute(); });
    }

    public CDelegateCommand LoadTestGCode { get; set; } 
  }
}
