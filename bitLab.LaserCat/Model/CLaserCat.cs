﻿using bitLab.LaserCat.Grbl;
using bitLab.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.LaserCat.Model
{
  class CLaserCat
  {
    private CLaserCatHardwareSimulator mLaserCatHardwareSimulator;
		private CLaserCatHardwarePIC mLaserCatHardwarePIC;
    private GrblFirmware mGrbl;
    private Task mGrblTask;

    private CLaserCat()
    {
      mLaserCatHardwareSimulator = new CLaserCatHardwareSimulator();
	    mLaserCatHardwarePIC = new CLaserCatHardwarePIC();
      //mGrbl = new GrblFirmware(new GCode(), mLaserCatHardwarePIC);
      mGrbl = new GrblFirmware(new GCode(), new GrblPlanner(), mLaserCatHardwarePIC);
    }

    public GrblFirmware GrblFirmware { get { return mGrbl; } }
    public ILaserCatHardware LaserCatHardwareSimulator { get { return mLaserCatHardwareSimulator; } }

    private bool CheckGrblIsStarted()
    {
      if (mGrblTask != null)
        return true;
      Log.LogError("Could not complete operation: Grbl is not started");
      return false;
    }

    public void GrblStart()
    {
      bool alreadyStarted = false;
      lock (this)
      {
        if (mGrblTask != null)
          alreadyStarted = true;
        else
          mGrblTask = new Task(() => { mGrbl.Execute(); });
      }
      if (alreadyStarted)
      {
        Log.LogError("Grbl already started");
        return;
      }

      mGrblTask.Start();
      Log.LogInfo("Grbl started");
    }

    public void LoadGCode(string fullFileName)
    {
      if (!CheckGrblIsStarted()) return;

      if (!File.Exists(fullFileName))
      {
        Log.LogError(String.Format("The file '{0}' does not exist", fullFileName));
        return;
      }

      var GCodeLines = File.ReadAllLines(fullFileName).ToList();
      mGrbl.SendMessage(Grbl.EGrblMessage.LoadGCode, GCodeLines);
    }

    public void Connect()
    {
      if (!CheckGrblIsStarted()) return;

      mGrbl.SendMessage(EGrblMessage.ConnectToMachine, new TMachineConnectionSettings() { COMPort = "COM8" });
    }

    public void Play()
    {
			if (!CheckGrblIsStarted()) return;
      mGrbl.SendMessage(EGrblMessage.Play, null);
    }

		public void WakeUp()
		{
			if (!CheckGrblIsStarted()) return;
			mGrbl.SendMessage(EGrblMessage.WakeUp, null);
		}

		public void SetSpeed(double speedValue, double timerPeriod)
		{
			if (!CheckGrblIsStarted()) return;
			mGrbl.SendMessage(EGrblMessage.SetSpeed, new TMotorSpeedSettings() { SpeedValue = (int)speedValue,
																																					 TimerPeriod = (int)timerPeriod });
		}

		public void ManualStep(byte idxMotor, byte motorDirection) {
      if (!CheckGrblIsStarted()) return;
			mGrbl.SendMessage(EGrblMessage.ManualStep, new TManualStepSettings(){ IdxMotor = idxMotor,
																																						MotorDirection = motorDirection });
		}

    #region Singleton
    private static CLaserCat mInstance;
    public static CLaserCat Instance { get { return mInstance; } }
    public static void Create()
    {
      mInstance = new CLaserCat();
    }
    #endregion
  }
}
