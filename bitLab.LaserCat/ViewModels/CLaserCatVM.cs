using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using bitLab.ViewModel;
using bitLab.ViewModel.Console;
using System.IO;
using bitLab.LaserCat.Model;

namespace bitLab.LaserCat.ViewModels
{
	public class CLaserCatVM : CBaseVM
  {
    public CConsoleVM ConsoleVM { get; set; }
    public CGrblFirmwareVM GrblFirmwareVM { get; set; }


    public CLaserCatVM()
    {
      ConsoleVM = new CConsoleVM(false);
      GrblFirmwareVM = new CGrblFirmwareVM();
			LoadTestGCode = new CDelegateCommand((obj) =>
			{
        string TestGCodeFile = Path.Combine(Environment.CurrentDirectory, @"..\..\Data\TestGCodeSupported.nc");
        CLaserCat.Instance.LoadGCode(TestGCodeFile);
      });
      LoadBitLabLogo = new CDelegateCommand((obj) =>
      {
        string TestGCodeFile = Path.Combine(Environment.CurrentDirectory, @"..\..\Data\bitLabLogo.nc");
        CLaserCat.Instance.LoadGCode(TestGCodeFile);
      });
      GrblStart = new CDelegateCommand((obj) => { CLaserCat.Instance.GrblStart(); });
      Connect = new CDelegateCommand((obj) => { CLaserCat.Instance.Connect(); });
      Play = new CDelegateCommand((obj) => { CLaserCat.Instance.Play(); });
      WakeUp = new CDelegateCommand((obj) => { CLaserCat.Instance.WakeUp(); });
    }

    public CDelegateCommand LoadTestGCode { get; set; }
    public CDelegateCommand LoadBitLabLogo { get; set; }
    public CDelegateCommand GrblStart { get; set; }
    public CDelegateCommand Connect { get; set; }
    public CDelegateCommand Play { get; set; }
    public CDelegateCommand WakeUp { get; set; } 
  }
}
