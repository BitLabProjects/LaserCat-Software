using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.LaserCat.Grbl
{
  public class CInMemorySerialPort : ISerialPort
  {
    private Queue<byte> mInputBuffer;
    private Queue<byte> mOutputBuffer;

    public CInMemorySerialPort()
    {
      mInputBuffer = new Queue<byte>();
      mOutputBuffer = new Queue<byte>();
    }

    public bool HasByte
    {
      get { return mInputBuffer.Count > 0; }
    }

    public byte ReadByte()
    {
      return mInputBuffer.Dequeue();
    }

    public void WriteByte(byte value)
    {
      mOutputBuffer.Enqueue(value);
    }

    public void AddLineToInputBuffer(String line)
    {
      foreach (char c in line)
        mInputBuffer.Enqueue(Convert.ToByte(c)); //TODO Use an appropriate encoding
    }
  }
}
