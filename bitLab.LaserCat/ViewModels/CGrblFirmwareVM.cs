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
    DispatcherTimer mStatusPollingTimer;
    public CGrblFirmwareVM()
    {
      mStatusPollingTimer = new DispatcherTimer();
      mStatusPollingTimer.Interval = TimeSpan.FromSeconds(1);
      mStatusPollingTimer.Tick += mStatusPollingTimer_Tick;
      mStatusPollingTimer.Start();
      Grbl.PlannerBlocksChanged += mGrbl_PlannerBlocksChanged;
      Grbl.StepperSegmentBufferChanged += GrblFirmware_StepperSegmentBufferChanged;
    }

    private void mStatusPollingTimer_Tick(object sender, EventArgs e)
    {
      Notify("CurrentPositionString");
    }

    private void GrblFirmware_StepperSegmentBufferChanged(object sender, EventArgs e)
    {
      Notify("StepperSegmentBufferCount");
      Notify("StepperSegmentBufferMaxSize");
    }

    private void mGrbl_PlannerBlocksChanged(object sender, EventArgs e)
    {
      Notify("PlannerBlockCount");
      Notify("PlannerBlockMaxSize");
    }

    private GrblFirmware Grbl { get { return CLaserCat.Instance.GrblFirmware; } }

    public string CurrentPositionString
    {
      get
      {
        return String.Format("X:{0:0.000}, Y:{1:0.000}, Z:{2:0.000}", Grbl.sys.position[0], Grbl.sys.position[1], Grbl.sys.position[2]);
      }
    }
    public int PlannerBlockCount { get { return Grbl.plan_get_block_buffer_count(); } }
    public int PlannerBlockMaxSize { get { return GrblFirmware.BLOCK_BUFFER_SIZE; } }
    public int StepperSegmentBufferCount { get { return Grbl.stepper_get_segment_buffer_count(); } }
    public int StepperSegmentBufferMaxSize { get { return GrblFirmware.SEGMENT_BUFFER_SIZE; } }
  }
}
