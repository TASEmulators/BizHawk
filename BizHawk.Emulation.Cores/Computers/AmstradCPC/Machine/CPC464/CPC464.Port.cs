using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// CPC464
    /// * Port *
    /// </summary>
    public partial class CPC464 : CPCBase
    {
        /// <summary>
        /// Reads a byte of data from a specified port address
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public override byte ReadPort(ushort port)
        {
            int result = 0xff;

            if (CRCT.ReadPort(port, ref result))
            {
                return (byte)result;
            }
            else if (GateArray.ReadPort(port, ref result))
            {
                return (byte)result;
            }
            else if (PPI.ReadPort(port, ref result))
            {
                return (byte)result;
            }

            return (byte)result;
        }

        /// <summary>
        /// Writes a byte of data to a specified port address
        /// </summary>
        /// <param name="port"></param>
        /// <param name="value"></param>
        public override void WritePort(ushort port, byte value)
        {
            if (CRCT.WritePort(port, (int)value))
            { }
            else if (GateArray.WritePort(port, (int)value))
            { }
            else if (PPI.WritePort(port, (int)value))
            { }
        }
    }
}
