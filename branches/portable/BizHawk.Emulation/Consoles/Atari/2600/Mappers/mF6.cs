using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Atari._2600
{
	class mF6 : MapperBase 
	{
		int toggle = 0;

		public override byte ReadMemory(ushort addr)
		{
			Address(addr);
			if (addr < 0x1000) return base.ReadMemory(addr);
			return core.rom[toggle * 4 * 1024 + (addr & 0xFFF)];
		}
		public override void WriteMemory(ushort addr, byte value)
		{
			Address(addr);
			if (addr < 0x1000) base.WriteMemory(addr, value);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("toggle", ref toggle);
		}

		void Address(ushort addr)
		{
			if (addr == 0x1FF6) toggle = 0;
			if (addr == 0x1FF7) toggle = 1;
			if (addr == 0x1FF8) toggle = 2;
			if (addr == 0x1FF9) toggle = 3;
		}
	}
}
