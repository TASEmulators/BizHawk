using System;
using System.Collections;
using System.Linq.Expressions;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// CPCHawk: Core Class
    /// * Misc Utilities *
    /// </summary>
    public partial class AmstradCPC
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
                case MachineType.CPC464:
                    m = "(Amstrad) CPC 464 (64K)";
                    break;
                case MachineType.CPC6128:
                    m = "(Amstrad) CPC 6464 (128K)";
                    break;
            }

            return m;
        }

		public static string GetMemberName<T, TValue>(Expression<Func<T, TValue>> memberAccess)
		{
			return ((MemberExpression)memberAccess.Body).Member.Name;
		}
	}
}
