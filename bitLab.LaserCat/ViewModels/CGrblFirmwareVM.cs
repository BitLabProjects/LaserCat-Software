using bitLab.LaserCat.Grbl;
using bitLab.LaserCat.Model;
using bitLab.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace bitLab.LaserCat.ViewModels
{
  public class CGrblFirmwareVM : CBaseVM
  {
		public CCuttingPlaneVM CuttingPlaneVM { get; set; }


    DispatcherTimer mStatusPollingTimer;
    public CGrblFirmwareVM()
    {
			CuttingPlaneVM = new CCuttingPlaneVM();
      mStatusPollingTimer = new DispatcherTimer();
      mStatusPollingTimer.Interval = TimeSpan.FromMilliseconds(100);
      mStatusPollingTimer.Tick += mStatusPollingTimer_Tick;
      mStatusPollingTimer.Start();
      Grbl.PlannerBlocksChanged += mGrbl_PlannerBlocksChanged;
    }

    private void mStatusPollingTimer_Tick(object sender, EventArgs e)
    {
      Notify("CurrentPositionString");
      Notify("StepperSpeed");
      Notify("StepperSegmentBufferCount");
      Notify("StepperSegmentBufferMaxSize");
			CuttingPlaneVM.Update();
    }

    private void mGrbl_PlannerBlocksChanged(object sender, EventArgs e)
    {
      Notify("PlannerBlockCount");
      Notify("PlannerBlockMaxSize");
    }

    private GrblFirmware Grbl { get { return CLaserCat.Instance.GrblFirmware; } }
    private ILaserCatHardware LaserCatHardwareSimulator { get { return CLaserCat.Instance.LaserCatHardwareSimulator; } }

    public string CurrentPositionString
    {
      get
      {
        return String.Format("X:{0:0.000}, Y:{1:0.000}, Z:{2:0.000}", Grbl.sys.position[0], Grbl.sys.position[1], Grbl.sys.position[2]);
      }
    }
    public int PlannerBlockCount { get { return Grbl.plan_get_block_buffer_count(); } }
    public int PlannerBlockMaxSize { get { return GrblFirmware.BLOCK_BUFFER_SIZE; } }
    public int StepperSegmentBufferCount { get { return LaserCatHardwareSimulator.GetSegmentBufferCount(); } }
    public int StepperSegmentBufferMaxSize { get { return GrblFirmware.SEGMENT_BUFFER_SIZE; } }
    public float StepperSpeed { get { return Grbl.st_get_realtime_rate(); } }
  }
}
