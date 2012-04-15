using System;
using System.Collections.Generic;
using System.IO;
using BizHawk.Emulation.CPUs.M6502;
using BizHawk.Emulation.Consoles.Atari;

namespace BizHawk
{
	partial class Atari2600
	{
		class mF8 : MapperBase
		{
			int toggle = 0;

			public override byte ReadMemory(ushort addr)
			{
				Address(addr);
				if (addr < 0x1000) return base.ReadMemory(addr);
				return core.rom[toggle*4*1024 + (addr&0xFFF)];
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
				if (addr == 0x1FF8) toggle = 0;
				else if (addr == 0x1FF9) toggle = 1;
			}
		}
	}
}