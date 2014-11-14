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
    private GrblFirmware mGrbl;
    private Task mGrblTask;

    private CLaserCat()
    {
      mGCodeLines = new List<String>();
      mGrbl = new GrblFirmware(new GCode());
    }

    public void LoadGCode(string fullFileName)
    {
      mCurrentGCodeFile = fullFileName;
      if (!File.Exists(fullFileName))
      {
        Log.LogError(String.Format("The file '{0}' does not exist", fullFileName));
        return;
      }

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
