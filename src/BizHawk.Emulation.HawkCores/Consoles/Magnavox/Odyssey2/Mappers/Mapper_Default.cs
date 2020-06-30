using BizHawk.Common;
using BizHawk.Emulation.Cores.Components.I8048;

namespace BizHawk.Emulation.Cores.Consoles.O2Hawk
{
	// Default mapper with no bank switching
	public class MapperDefault : MapperBase
	{
		public int ROM_mask;
		
		public override void Initialize()
		{
			// roms come in 3 sizes, but the last size is a degenerate case
			if (Core._rom.Length == 0x3000)
			{
				ROM_mask = 0x3FFF;
			}
			else
			{
				ROM_mask = Core._rom.Length - 1;
			}
		}

		public override byte ReadMemory(ushort addr)
		{
			return Core._rom[addr & ROM_mask];
		}

		public override void MapCDL(ushort addr, I8048.eCDLogMemFlags flags)
		{
			SetCDLROM(flags, addr);
		}

		public override void WriteMemory(ushort addr, byte value)
		{
			// no mapping hardware available
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(ROM_mask), ref ROM_mask);
		}
	}
}
