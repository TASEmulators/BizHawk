using System;
using System.Collections;
using System.Linq.Expressions;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// ZXHawk: Core Class
    /// * Misc Utilities *
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


        public string GetMachineType()
        {
            string m = "";
            switch (SyncSettings.MachineType)
            {
                case MachineType.ZXSpectrum16:
                    m = "(Sinclair) ZX Spectrum 16K";
                    break;
                case MachineType.ZXSpectrum48:
                    m = "(Sinclair) ZX Spectrum 48K";
                    break;
                case MachineType.ZXSpectrum128:
                    m = "(Sinclair) ZX Spectrum 128K";
                    break;
                case MachineType.ZXSpectrum128Plus2:
                    m = "(Amstrad) ZX Spectrum 128K +2";
                    break;
                case MachineType.ZXSpectrum128Plus2a:
                    m = "(Amstrad) ZX Spectrum 128K +2a";
                    break;
                case MachineType.ZXSpectrum128Plus3:
                    m = "(Amstrad) ZX Spectrum 128K +3";
                    break;
            }

            return m;
        }

        public byte[] GetSZXSnapshot()
        {
            return SZX.ExportSZX(_machine);
            //return System.Text.Encoding.Default.GetString(data);
        }

        public static string GetMemberName<T, TValue>(Expression<Func<T, TValue>> memberAccess)
        {
            return ((MemberExpression)memberAccess.Body).Member.Name;
        }
    }
}
