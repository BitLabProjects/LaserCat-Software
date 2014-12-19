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
    private ILaserCatHardware mHardware;
    public GrblCore(GrblFirmware grbl, GCode gcode, ILaserCatHardware hardware)
    {
      mGrbl = grbl;
      mGCode = gcode;
      mHardware = hardware;
      mIsConnected = false;
    }

    private enum EGrblState
    {
      Idle,
      GCodeLoaded
    }
    private EGrblState mState;
    private bool mIsConnected;

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
    public void handleMessage(TGrblMessage msg)
    {
      switch (msg.Message)
      {
        case EGrblMessage.LoadGCode: 
          loadGCode((List<string>)msg.Param0); break;
        case EGrblMessage.ConnectToMachine:
          connectToMachine((TMachineConnectionSettings)msg.Param0); break;
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

    private void connectToMachine(TMachineConnectionSettings settings)
    {
      if (!checkAllowedEntryState(new EGrblState[] { EGrblState.Idle, EGrblState.GCodeLoaded }))
        return;

      if (mIsConnected)
      {
        Log.LogInfo("Machine already connected");
        return;
      }

      Log.LogInfo("Connecting to machine on port {0}...", settings.COMPort);
      if (mHardware.Connect(settings.COMPort)) {
        mIsConnected = true;
        Log.LogInfo("Connected");
      }
      else 
        Log.LogInfo("Connection failed");
    }
    #endregion
  }
}
