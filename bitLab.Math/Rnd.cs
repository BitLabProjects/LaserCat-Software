using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.Math
{
  public class Rnd
  {
    private Rnd() { }

    private static RandomNumberGenerator mRNG = RandomNumberGenerator.Create();
    private static byte[] mBytesBuffer = new byte[1024 * 16];
    private static int mBytesBufferIndex = mBytesBuffer.Length;

    private static void EnsureBuffer(int size)
    {
      if (mBytesBufferIndex + size >= mBytesBuffer.Length)
      {
        mRNG.GetBytes(mBytesBuffer);
        mBytesBufferIndex = 0;
      }
    }

    public static byte Byte()
    {
      EnsureBuffer(1);
      return mBytesBuffer[mBytesBufferIndex++];
    }

    public static int Int(int maxValue)
    {
      EnsureBuffer(4);
      var pos = mBytesBufferIndex;
      mBytesBufferIndex += 4;
      return System.Math.Abs(BitConverter.ToInt32(mBytesBuffer, pos)) % maxValue;
    }

    public static DblPoint2 DblPoint2(double sizeX, double sizeY)
    {
      return new DblPoint2(Int(Int32.MaxValue) * 1.0 / Int32.MaxValue * sizeX,
                           Int(Int32.MaxValue) * 1.0 / Int32.MaxValue * sizeY);
    }
  }
}
