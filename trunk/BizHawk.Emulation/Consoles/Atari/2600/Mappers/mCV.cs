using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Atari._2600
{
	class mCV: MapperBase
	{
		ByteBuffer aux_ram = new ByteBuffer(1024);
		
		public override byte ReadMemory(ushort addr)
		{
			if (addr < 0x1000)
				return base.ReadMemory(addr);
			else if (addr < 0x1400)
				return aux_ram[(addr & 0x3FF)];
			else if (addr >= 0x1800 && addr < 0x2000)
				return core.rom[(addr & 0x7FF)];
			else return base.ReadMemory(addr);
		}
		public override void WriteMemory(ushort addr, byte value)
		{
			if (addr < 0x1000)
				base.WriteMemory(addr, value);
			else if (addr >= 0x1400 && addr < 0x1800)
				aux_ram[(addr & 0x3FF)] = value;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("aux_ram", ref aux_ram);
		}
	}
}
