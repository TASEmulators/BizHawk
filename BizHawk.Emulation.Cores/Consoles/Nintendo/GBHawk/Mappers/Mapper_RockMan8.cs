using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using System;

using BizHawk.Emulation.Common.Components.LR35902;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	// RockMan 8, just some simple bankswitching 
	public class MapperRM8 : MapperBase
	{
		public int ROM_bank;
		public int ROM_mask;

		public override void Initialize()
		{
			ROM_bank = 1;
			ROM_mask = Core._rom.Length / 0x4000 - 1;

			// some games have sizes that result in a degenerate ROM, account for it here
			if (ROM_mask > 4) { ROM_mask |= 3; }
	}

		public override byte ReadMemory(ushort addr)
		{
			if (addr < 0x4000)
			{
				// lowest bank is fixed
				return Core._rom[addr];
			
			}
			else if (addr < 0x8000)
			{
				return Core._rom[(addr - 0x4000) + ROM_bank * 0x4000];
			}
			else
			{
				return 0xFF;
			}
		}

		public override void MapCDL(ushort addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x4000)
			{
				// lowest bank is fixed
				SetCDLROM(flags, addr);
			
			}
			else if (addr < 0x8000)
			{
				SetCDLROM(flags, (addr - 0x4000) + ROM_bank * 0x4000);
			}
			else
			{
				return;
			}
		}

		public override byte PeekMemory(ushort addr)
		{
			return ReadMemory(addr);
		}

		public override void WriteMemory(ushort addr, byte value)
		{
			if ((addr >= 0x2000) && (addr < 0x4000))
			{
				value &= 0x1F;

				if (value == 0) { value = 1; }

				// in hhugboy they just subtract 8, but to me looks like bits 4 and 5 are just swapped (and bit 4 is unused?)
				ROM_bank = ((value & 0xF) | ((value & 0x10) >> 1)) & ROM_mask;
			}
		}

		public override void PokeMemory(ushort addr, byte value)
		{
			WriteMemory(addr, value);
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("ROM_Bank", ref ROM_bank);
			ser.Sync("ROM_Mask", ref ROM_mask);
		}
	}
}
