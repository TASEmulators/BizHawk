using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Utilities
    /// </summary>
    public partial class ZXSpectrum
    {
        /// <summary>
        /// Helper method that returns a single INT32 from a BitArray
        /// </summary>
        /// <param name="bitarray"></param>
        /// <returns></returns>
        public static int GetIntFromBitArray(BitArray bitArray)
        {
            if (bitArray.Length > 32)
                throw new ArgumentException("Argument length shall be at most 32 bits.");

            int[] array = new int[1];
            bitArray.CopyTo(array, 0);
            return array[0];
        }

        /// <summary>
        /// POKEs a memory bus address
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        public void PokeMemory(ushort addr, byte value)
        {
            _machine.WriteBus(addr, value);
        }
    }
}
