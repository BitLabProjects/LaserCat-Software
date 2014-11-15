using bitLab.LaserCat.Grbl;
using bitLab.LaserCat.Model;
using bitLab.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.LaserCat.ViewModels
{
  public class CGrblFirmwareVM: CBaseVM
  {
    public CGrblFirmwareVM()
    {
      CLaserCat.Instance.GrblFirmware.PlannerBlocksChanged += mGrbl_PlannerBlocksChanged;
      CLaserCat.Instance.GrblFirmware.StepperSegmentBufferChanged += GrblFirmware_StepperSegmentBufferChanged;
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

    public int PlannerBlockCount { get { return CLaserCat.Instance.GrblFirmware.plan_get_block_buffer_count(); } }
    public int PlannerBlockMaxSize { get { return GrblFirmware.BLOCK_BUFFER_SIZE; } }
    public int StepperSegmentBufferCount { get { return CLaserCat.Instance.GrblFirmware.stepper_get_segment_buffer_count(); } }
    public int StepperSegmentBufferMaxSize { get { return GrblFirmware.SEGMENT_BUFFER_SIZE; } }
  }
}
