using System;
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
  }
  public struct TMachineConnectionSettings
  {
    public string COMPort;
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
      core.initGrblState();

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
