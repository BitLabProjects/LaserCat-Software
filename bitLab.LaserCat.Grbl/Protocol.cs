﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using bitLab.Logging;
using System.Collections.Concurrent;

namespace bitLab.LaserCat.Grbl
{
  public enum EGrblMessage
  {
    LoadGCode,
    ConnectToMachine,
    Play,
    WakeUp,
    SetSpeed,
		ManualStep
  }
  public struct TMachineConnectionSettings
  {
    public string COMPort;
  }

	public struct TMotorSpeedSettings
	{
		public int SpeedValue;
		public int TimerPeriod;
	}

	public struct TManualStepSettings
	{
		public byte IdxMotor;
		public byte MotorDirection;
	}

  internal struct TGrblMessage
  {
    public EGrblMessage Message;
    public object Param0;
  }

	public partial class GrblFirmware
	{
    private BlockingCollection<TGrblMessage> mMessageQueue;

    public void SendMessage(EGrblMessage message, object param0)
    {
      mMessageQueue.Add(new TGrblMessage() { Message = message, Param0 = param0 });
    }

    public void protocol_main_loop()
    {
      mMessageQueue = new BlockingCollection<TGrblMessage>();
      var core = new GrblCore(this, mGCode, mLaserCatHardware);
      Log.LogInfo("Grbl initialized");

      try
      {
        while (true)
          core.handleMessage(mMessageQueue.Take());
      }
      catch (InvalidOperationException)
      {
      }
    }
	}
}
