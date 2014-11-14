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

    private CLaserCat()
    {
      mGCodeLines = new List<String>();
    }

    public void LoadGCode(string fullFileName)
    {
      mCurrentGCodeFile = fullFileName;
      if (!File.Exists(fullFileName))
      {
        Logging.Log.LogError(String.Format("The file '{0}' does not exist", fullFileName));
        return;
      }

      mGCodeLines.AddRange(File.ReadAllLines(mCurrentGCodeFile));
      Logging.Log.LogInfo(String.Format("Loaded GCode file '{0}'", fullFileName));
      Logging.Log.LogInfo("Statistics");
      Logging.Log.LogInfo(String.Format(" Lines: {0}", mGCodeLines.Count));
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
