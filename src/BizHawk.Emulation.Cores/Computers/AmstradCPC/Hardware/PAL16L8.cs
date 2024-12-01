using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	/// <summary>
	/// Programmable Array Logic (PAL) device
	/// The C6128's second page of 64KB of RAM is controlled by this chip
	/// </summary>
	public class PAL16L8 : IPortIODevice
	{
		private readonly CPCBase _machine;

		/// <summary>
		/// PAL MMR Register
		/// This register exists only in CPCs with 128K RAM (like the CPC 6128, or CPCs with Standard Memory Expansions)
		/// Note: In the CPC 6128, the register is a separate PAL that assists the Gate Array chip
		/// 
		/// Bit	Value	Function
		/// 7	1	    MMR register enable
		/// 6	1		MMR register enable
		/// 5	b	    64K bank number(0..7); always 0 on an unexpanded CPC6128, 0-7 on Standard Memory Expansions
		/// 4	b
		/// 3	b
		/// 2	x       RAM Config(0..7)
		/// 1	x       ""  
		/// 0	x       ""
		/// 
		/// The 3bit RAM Config value is used to access the second 64K of the total 128K RAM that is built into the CPC 6128 or the additional 64K-512K of standard memory expansions. 
		/// These contain up to eight 64K ram banks, which are selected with bit 3-5. A standard CPC 6128 only contains bank 0. Normally the register is set to 0, so that only the 
		/// first 64K RAM are used (identical to the CPC 464 and 664 models). The register can be used to select between the following eight predefined configurations only:
		/// 
		/// -Address-   0       1       2       3       4       5       6       7
		/// 0000-3FFF   RAM_0   RAM_0   RAM_4   RAM_0   RAM_0   RAM_0   RAM_0   RAM_0
		/// 4000-7FFF   RAM_1   RAM_1   RAM_5   RAM_3   RAM_4   RAM_5   RAM_6   RAM_7
		/// 8000-BFFF   RAM_2   RAM_2   RAM_6   RAM_2   RAM_2   RAM_2   RAM_2   RAM_2
		/// C000-FFFF   RAM_3   RAM_7   RAM_7   RAM_7   RAM_3   RAM_3   RAM_3   RAM_3
		/// 
		/// The Video RAM is always located in the first 64K, VRAM is in no way affected by this register
		/// </summary>
		public byte MMR => _MMR;
		private byte _MMR;		

		public PAL16L8(CPCBase machine)
		{
			_machine = machine;
		}

		public bool ReadPort(ushort port, ref int result)
		{
			// this is write-only
			return false;
		}

		public bool WritePort(ushort port, int result)
		{
			if (result.Bit(7) && result.Bit(6))
			{
				_MMR = (byte)result;
				return true;
			}
			return false;
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("PAL");
			ser.Sync(nameof(MMR), ref _MMR);
			ser.EndSection();
		}
	}
}
