using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.LaserCat.Grbl
{
  public class CLaserCatHardwareSimulator: ILaserCatHardware
  {

    public CLaserCatHardwareSimulator()
    {
    }

    public void StorePlannerBlock(byte blockIndex, st_block_t block)
    {
    }

    public void StoreSegment(byte segmentIndex, segment_t segment)
    {
    }

    public void SetSettings(LaserCatSettings settings)
    {
    }

    public void WakeUp(bool setupAndEnableMotors)
    {
    }

    public void Init()
    {
    }

    public void GoIdle(bool delayAndDisableSteppers)
    {
    }

    public void Reset()
    {
    }
  }
}
