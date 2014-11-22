using bitLab.LaserCat.Grbl;
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
    private string mCurrentGCodeFile;
    private List<string> mGCodeLines;
    private int mCurrentGCodeLineIndex;
    private CInMemorySerialPort mSerialPort;
    private CLaserCatHardwareSimulator mLaserCatHardwareSimulator;
    private GrblFirmware mGrbl;
    private Task mGrblTask;

    private CLaserCat()
    {
      mGCodeLines = new List<String>();
      mSerialPort = new CInMemorySerialPort();
      mLaserCatHardwareSimulator = new CLaserCatHardwareSimulator();
      mGrbl = new GrblFirmware(new GCode(), mSerialPort, mLaserCatHardwareSimulator);
      mCurrentGCodeLineIndex = -1;
    }

    public GrblFirmware GrblFirmware { get { return mGrbl; } }
    public CLaserCatHardwareSimulator LaserCatHardwareSimulator { get { return mLaserCatHardwareSimulator; } }

    private bool CheckGrblIsStarted()
    {
      if (mGrblTask != null)
        return true;
      Log.LogError("Could not complete operation: Grbl is not started");
      return false;
    }

    private bool IsGCodeCompleted()
    {
      return mCurrentGCodeLineIndex + 1 >= mGCodeLines.Count;
    }

    public void LoadGCode(string fullFileName)
    {
      mCurrentGCodeFile = fullFileName;
      if (!File.Exists(fullFileName))
      {
        Log.LogError(String.Format("The file '{0}' does not exist", fullFileName));
        return;
      }

      mCurrentGCodeLineIndex = -1;
      mGCodeLines.Clear();
      mGCodeLines.AddRange(File.ReadAllLines(mCurrentGCodeFile));
      Log.LogInfo(String.Format("Loaded GCode file '{0}'", fullFileName));
      Log.LogInfo("Statistics");
      Log.LogInfo(String.Format(" Lines: {0}", mGCodeLines.Count));
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

    public void SendGCodeLine()
    {
      if (!CheckGrblIsStarted()) return;

      if (IsGCodeCompleted())
      {
        Log.LogError("Last GCode line reached");
        return;
      }

      mCurrentGCodeLineIndex += 1;
      Log.LogInfo(String.Format("Adding GCode line '{0}'", mGCodeLines[mCurrentGCodeLineIndex]));
      mSerialPort.AddLineToInputBuffer(mGCodeLines[mCurrentGCodeLineIndex] + '\n');
    }

    public void SendAllGCode()
    {
      if (!CheckGrblIsStarted()) return;

      while (!IsGCodeCompleted()) { SendGCodeLine(); }

      Log.LogInfo("GCode completed");
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
