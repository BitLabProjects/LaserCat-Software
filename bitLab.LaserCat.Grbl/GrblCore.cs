using bitLab.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.LaserCat.Grbl
{
  internal struct GrblCoreState
  {
    public bool IsGCodeLoaded;
    public bool IsConnected;

    public void Reset()
    {
      IsGCodeLoaded = false;
      IsConnected = false;
    }
  }

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
      mState = new GrblCoreState();
      mState.Reset();
		}

    private GrblCoreState mState;

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
				case EGrblMessage.WakeUp:
					wakeUp(); break;
				case EGrblMessage.SetSpeed:
					setSpeed((TMotorSpeedSettings)msg.Param0); break;
			}
		}
		#endregion

		#region Message handlers
		private void loadGCode(List<string> GCodeLines)
		{
			Log.LogInfo("Resetting planner and parsing GCode...");
			mGrbl.Planner.plan_reset();
			int idxLine = 1;
			foreach (var line in GCodeLines)
			{
				var result = mGCode.gc_execute_line(line);
				if (result != GrblFirmware.STATUS_OK)
				{
					Log.LogError("GCode parse error: ");
					Log.LogError(" - Line {0}: {1} ", idxLine, line);
					Log.LogError(" - Error: {0} ", mGrbl.getStatusMessage(result));
					return;
				}
				idxLine++;
			}

			Log.LogInfo("Parsing GCode completed:");
			Log.LogInfo("- Parsed {0} GCode lines", GCodeLines.Count);
			Log.LogInfo("- Planned {0} segments", mGrbl.Planner.plan_get_block_buffer_count());
      mState.IsGCodeLoaded = true;
		}

		private void connectToMachine(TMachineConnectionSettings settings)
		{
			if (mState.IsConnected)
			{
				Log.LogError("Machine already connected");
				return;
			}

			Log.LogInfo("Connecting to machine on port {0}...", settings.COMPort);
      mState.IsConnected = mHardware.Connect(settings.COMPort);
      if (!mState.IsConnected)
      {
				Log.LogError("Connection failed");
        return;
      }

			Log.LogInfo("Connected");

			Log.LogInfo("Resetting machine...");
			mHardware.Reset();

			Log.LogInfo("Sending initial settings...");
			mGrbl.st_reset();
			Log.LogInfo("Done");
		}

		private void play()
		{
      if (!mCheckIsConnected())
        return;
      if (!mCheckIsGCodeLoaded())
        return;

			Log.LogInfo("--- Play - {0} ---", DateTime.Now.ToShortTimeString());

			Log.LogInfo("Filling stepper buffer...");
			mGrbl.st_prep_buffer();
			Log.LogInfo("Done");

      mSendWakeUpCommand();

			Log.LogInfo("Streaming stepper data...");
			while (mGrbl.Planner.plan_get_block_buffer_count() > 0)
			{
				if (!mGrbl.st_prep_buffer())
					System.Threading.Thread.Sleep(100);

				var newPos = mHardware.AskPosition();
				for (int i = 0; i < newPos.Length; i++)
					mGrbl.sys.position[i] = newPos[i];
			}
			Log.LogInfo("Done");
		}

		private void wakeUp()
		{
      if (!mCheckIsConnected())
        return;

			Log.LogInfo("--- wakeup - {0} ---", DateTime.Now.ToShortTimeString());
      mSendWakeUpCommand();
		}

    private void mSendWakeUpCommand()
    {
      Log.LogInfo("Issuing wakeUp command...");
      mHardware.WakeUp(true);
      Log.LogInfo("Done");
    }

		private void setSpeed(TMotorSpeedSettings motorSpeedSettings)
		{
      if (!mCheckIsConnected())
        return;

			Log.LogInfo("Speed: {0}, Period: {1}", motorSpeedSettings.SpeedValue, motorSpeedSettings.TimerPeriod);
			mHardware.SetSpeed(motorSpeedSettings.SpeedValue, motorSpeedSettings.TimerPeriod);
		}

		#endregion

    #region Utilities
    private bool mCheckIsConnected()
    {
      if (!mState.IsConnected)
      {
        Log.LogError("Not connected");
        return false;
      }
      return true;
    }
    private bool mCheckIsGCodeLoaded()
    {
      if (!mState.IsGCodeLoaded)
      {
        Log.LogError("No GCode loaded");
        return false;
      }
      return true;
    }
    #endregion
  }
}

/*
Period 65280
Max speed 41

Period 65500
Max speed 157
 * */
