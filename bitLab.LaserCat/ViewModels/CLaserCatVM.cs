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
				string TestGCodeFile = Path.Combine(Environment.CurrentDirectory, @"..\..\Data\bitLabLogoSmall.nc");
				CLaserCat.Instance.LoadGCode(TestGCodeFile);
			});
			GrblStart = new CDelegateCommand((obj) => { CLaserCat.Instance.GrblStart(); });
			Connect = new CDelegateCommand((obj) => { CLaserCat.Instance.Connect(); });
			Play = new CDelegateCommand((obj) => { CLaserCat.Instance.Play(); });
			WakeUp = new CDelegateCommand((obj) => { CLaserCat.Instance.WakeUp(); });
      ManualStepMotor1Forward = new CDelegateCommand((obj) => { CLaserCat.Instance.ManualStep(0,1); });
			ManualStepMotor1Backward = new CDelegateCommand((obj) => { CLaserCat.Instance.ManualStep(0,2); });
			ManualStepMotor2Forward = new CDelegateCommand((obj) => { CLaserCat.Instance.ManualStep(1,1); });
			ManualStepMotor2Backward = new CDelegateCommand((obj) => { CLaserCat.Instance.ManualStep(1,2); });
		}

		public CDelegateCommand LoadTestGCode { get; set; }
		public CDelegateCommand LoadBitLabLogo { get; set; }
		public CDelegateCommand GrblStart { get; set; }
		public CDelegateCommand Connect { get; set; }
		public CDelegateCommand Play { get; set; }
		public CDelegateCommand WakeUp { get; set; }
    public CDelegateCommand ManualStepMotor1Forward { get; set; }
    public CDelegateCommand ManualStepMotor1Backward { get; set; }
    public CDelegateCommand ManualStepMotor2Forward { get; set; }
    public CDelegateCommand ManualStepMotor2Backward { get; set; }

    private Double mMotorSpeed = 128;
		public Double MotorSpeed
		{
			get { return mMotorSpeed; }
			set
			{
				if (SetAndNotify(ref mMotorSpeed, value))
					CLaserCat.Instance.SetSpeed(mMotorSpeed, mTimerPeriod);
			}
		}

    private Double mTimerPeriod = 65280;
		public Double TimerPeriod
		{
			get { return mTimerPeriod; }
			set
			{
				if (SetAndNotify(ref mTimerPeriod, value))
					CLaserCat.Instance.SetSpeed(mMotorSpeed, mTimerPeriod);
			}
		}

	}
}
