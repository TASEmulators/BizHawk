using BizHawk.Common;
using BizHawk.Emulation.Cores.Components.I8048;

namespace BizHawk.Emulation.Cores.Consoles.O2Hawk
{
	// XROM mapper, 3KB ROM and 1KB data accessible through port 0
	public class MapperXROM : MapperBase
	{
		public int ROM_mask;
		
		public override void Initialize()
		{
			// XROM has data instructions from 0x400-0xFFF
			ROM_mask = 0xFFF;
		}

		public override byte ReadMemory(ushort addr)
		{
			return Core._rom[(addr + 0x400) & ROM_mask];
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
