using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.LaserCat.Grbl
{
  public interface ISerialPort
  {
    bool HasByte { get; }
    byte ReadByte();
    void WriteByte(Byte value);
  }
}
