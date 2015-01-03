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

    private enum EGrblCoreState
    {
      Idle,
      GCodeLoaded
    }
    private EGrblCoreState mState;
    private bool mIsConnected;

    public void initGrblState()
    {
      changeState(EGrblCoreState.Idle);
      Log.LogInfo("Grbl initialized");
    }

    #region Utilities
    private void changeState(EGrblCoreState newState)
    {
      mState = newState;
      Log.LogInfo("State changed to " + newState.ToString());
    }

    private bool checkAllowedEntryState(EGrblCoreState[] allowedStates)
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
        case EGrblMessage.Play:
          play(); break;
      }
    }
    #endregion

    #region Message handlers
    private void loadGCode(List<string> GCodeLines)
    {
      if (!checkAllowedEntryState(new EGrblCoreState[] { EGrblCoreState.Idle, EGrblCoreState.GCodeLoaded }))
        return;

      Log.LogInfo("Resetting planner and parsing GCode...");
      mGrbl.plan_reset();
      int idxLine = 1;
      foreach (var line in GCodeLines)
      {
        var result = mGCode.gc_execute_line(line);
        if (result != GrblFirmware.STATUS_OK)
        {
          Log.LogError("GCode parse error: ");
          Log.LogError(" - Line {0}: {1} ", idxLine, line);
          Log.LogError(" - Error: {0} ", mGrbl.getStatusMessage(result));
          changeState(EGrblCoreState.Idle);
          return;
        }
        idxLine++;
      }

      Log.LogInfo("Parsing GCode completed:");
      Log.LogInfo("- Parsed {0} GCode lines", GCodeLines.Count);
      Log.LogInfo("- Planned {0} segments", mGrbl.plan_get_block_buffer_count());
      changeState(EGrblCoreState.GCodeLoaded);
    }

    private void connectToMachine(TMachineConnectionSettings settings)
    {
      if (!checkAllowedEntryState(new EGrblCoreState[] { EGrblCoreState.Idle, EGrblCoreState.GCodeLoaded }))
        return;

      if (mIsConnected)
      {
        Log.LogError("Machine already connected");
        return;
      }

      Log.LogInfo("Connecting to machine on port {0}...", settings.COMPort);
      mIsConnected = mHardware.Connect(settings.COMPort);
      if (mIsConnected)
        Log.LogInfo("Connected");
      else
        Log.LogError("Connection failed");

      Log.LogInfo("Sending initial settings...");
      mHardware.Init();
      mGrbl.st_reset();
      Log.LogInfo("Done");
    }

    private void play()
    {
      Log.LogInfo("--- Play - {0} ---", DateTime.Now.ToShortTimeString());

      Log.LogInfo("Filling stepper buffer...");
      mGrbl.st_prep_buffer();
      Log.LogInfo("Done");

      Log.LogInfo("Issuing play command...");
      mHardware.WakeUp(true);
      Log.LogInfo("Done");

      Log.LogInfo("Streaming stepper data...");
      while (mGrbl.plan_get_block_buffer_count() > 0) {
				System.Threading.Thread.Sleep(100);
        mGrbl.st_prep_buffer();
        var newPos = mHardware.AskPosition();
        for (int i = 0; i < newPos.Length; i++)
          mGrbl.sys.position[i] = newPos[i];
      }
      Log.LogInfo("Done");
    }

    #endregion
  }
}
