using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.Logging
{
  public class HiResTimer
  {
    private bool isPerfCounterSupported = false;
    private Int64 frequency = 0;

    [System.Runtime.InteropServices.DllImport("Kernel32.dll")]
    public static extern bool QueryPerformanceCounter(out Int64 perfcount);

    [System.Runtime.InteropServices.DllImport("Kernel32.dll")]
    public static extern bool QueryPerformanceFrequency(out Int64 freq);

    public HiResTimer()
    {
      // Query the high-resolution timer only if it is supported.
      // A returned frequency of 1000 typically indicates that it is not
      // supported and is emulated by the OS using the same value that is
      // returned by Environment.TickCount.
      // A return value of 0 indicates that the performance counter is
      // not supported.
      bool returnVal = QueryPerformanceFrequency(out frequency);

      if (returnVal)
      {
        // The performance counter is supported.
        isPerfCounterSupported = true;
      }
      else
      {
        // The performance counter is not supported. Use
        // Environment.TickCount instead.
        frequency = 1000;
      }
    }

    public Int64 Frequency
    {
      get
      {
        return frequency;
      }
    }

    public Int64 Value
    {
      get
      {
        Int64 tickCount = 0;

        if (isPerfCounterSupported)
        {
          // Get the value here if the counter is supported.
          QueryPerformanceCounter(out tickCount);
          return tickCount;
        }
        else
        {
          // Otherwise, use Environment.TickCount.
          return (Int64)Environment.TickCount;
        }
      }
    }
  }
}
