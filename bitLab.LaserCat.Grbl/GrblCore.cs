using bitLab.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.LaserCat.Grbl
{
  internal class GrblCore
  {
    private GrblFirmware mGrbl;
    private GCode mGCode;
    public GrblCore(GrblFirmware grbl, GCode gcode)
    {
      mGrbl = grbl;
      mGCode = gcode;
    }

    private enum EGrblState
    {
      Idle,
      GCodeLoaded
    }
    private EGrblState mState;

    public void initGrblState()
    {
      changeState(EGrblState.Idle);
      Log.LogInfo("Grbl initialized");
    }

    #region Utilities
    private void changeState(EGrblState newState)
    {
      mState = newState;
      Log.LogInfo("State changed to " + newState.ToString());
    }

    private bool checkAllowedEntryState(EGrblState[] allowedStates)
    {
      if (allowedStates.Contains(mState))
        return true;
      Log.LogInfo("Operation not allowed in this state");
      return false;
    }
    #endregion

    #region Message dispatch
    public void handleMessage(GrblFirmware.TGrblMessage msg)
    {
      switch (msg.Message)
      {
        case GrblFirmware.EGrblMessage.LoadGCode:
          loadGCode(msg.Param0 as List<string>);
          break;
      }
    }
    #endregion

    #region Message handlers
    private void loadGCode(List<string> GCodeLines)
    {
      if (!checkAllowedEntryState(new EGrblState[] { EGrblState.Idle, EGrblState.GCodeLoaded }))
        return;

      Log.LogInfo("Resetting planner and parsing GCode...");
      mGrbl.plan_reset();
      int idxLine = 1;
      foreach (var line in GCodeLines)
      {
        var result = mGCode.gc_execute_line(line);
        if (result != GrblFirmware.STATUS_OK)
        {
          Log.LogInfo("GCode parse error: ");
          Log.LogInfo(" - Line {0}: {1} ", idxLine, line);
          Log.LogInfo(" - Error: {0} ", mGrbl.getStatusMessage(result));
          changeState(EGrblState.Idle);
          return;
        }
        idxLine++;
      }

      Log.LogInfo("Parsing GCode completed:");
      Log.LogInfo("- Parsed {0} GCode lines", GCodeLines.Count);
      Log.LogInfo("- Planned {0} segments", mGrbl.plan_get_block_buffer_count());
      changeState(EGrblState.GCodeLoaded);
    }
    #endregion
  }
}
