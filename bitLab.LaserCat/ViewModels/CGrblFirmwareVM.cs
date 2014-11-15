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
    }

    private void mGrbl_PlannerBlocksChanged(object sender, EventArgs e)
    {
      Notify("");
    }

    public int PlannerBlockCount { get { return CLaserCat.Instance.GrblFirmware.plan_get_block_buffer_count(); } }
    public int PlannerBlockMaxSize { get { return GrblFirmware.BLOCK_BUFFER_SIZE; } }
  }
}
