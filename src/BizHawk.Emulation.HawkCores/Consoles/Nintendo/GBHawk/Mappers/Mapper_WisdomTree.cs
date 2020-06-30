using BizHawk.Common;
using BizHawk.Emulation.Cores.Components.LR35902;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	// Wisdom tree mapper (32K bank switching)
	public class MapperWT : MapperBase
	{
		public int ROM_bank;
		public int ROM_mask;

		public override void Reset()
		{
			ROM_bank = 0;
			ROM_mask = Core._rom.Length / 0x8000 - 1;

			// some games have sizes that result in a degenerate ROM, account for it here
			if (ROM_mask > 4) { ROM_mask |= 3; }
			if (ROM_mask > 0x100) { ROM_mask |= 0xFF; }
		}

		public override byte ReadMemoryLow(ushort addr)
		{
			return Core._rom[ROM_bank * 0x8000 + addr];
		}

		public override byte ReadMemoryHigh(ushort addr)
		{
			return 0xFF;
		}

		public override void MapCDL(ushort addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x8000)
			{
				SetCDLROM(flags, ROM_bank * 0x8000 + addr);
			}
			else
			{
				return;
			}
		}

		public override byte PeekMemoryLow(ushort addr)
		{
			return ReadMemoryLow(addr);
		}

		public override byte PeekMemoryHigh(ushort addr)
		{
			return ReadMemoryHigh(addr);
		}

		public override void WriteMemory(ushort addr, byte value)
		{
			if (addr < 0x4000)
			{
				ROM_bank = ((addr << 1) & 0x1ff) >> 1;
				ROM_bank &= ROM_mask;
			}
		}

		public override void PokeMemory(ushort addr, byte value)
		{
			WriteMemory(addr, value);
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(ROM_bank), ref ROM_bank);
			ser.Sync(nameof(ROM_mask), ref ROM_mask);
		}
	}
}
