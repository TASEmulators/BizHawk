using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class VBANext
	{
		private IMemoryDomains _memoryDomains;

		private void InitMemoryDomains()
		{
			var mm = new List<MemoryDomain>();
			var s = new LibVBANext.MemoryAreas();
			var l = MemoryDomain.Endian.Little;
			LibVBANext.GetMemoryAreas(Core, s);
			mm.Add(MemoryDomain.FromIntPtr("IWRAM", 32 * 1024, l, s.iwram, true, 4));
			mm.Add(MemoryDomain.FromIntPtr("EWRAM", 256 * 1024, l, s.ewram, true, 4));
			mm.Add(MemoryDomain.FromIntPtr("BIOS", 16 * 1024, l, s.bios, false, 4));
			mm.Add(MemoryDomain.FromIntPtr("PALRAM", 1024, l, s.palram, false, 4));
			mm.Add(MemoryDomain.FromIntPtr("VRAM", 96 * 1024, l, s.vram, true, 4));
			mm.Add(MemoryDomain.FromIntPtr("OAM", 1024, l, s.oam, true, 4));
			mm.Add(MemoryDomain.FromIntPtr("ROM", 32 * 1024 * 1024, l, s.rom, true, 4));
			mm.Add(MemoryDomain.FromIntPtr("SRAM", s.sram_size, l, s.sram, true, 4));

			mm.Add(new MemoryDomainDelegate("System Bus", 0x10000000, l,
				delegate(long addr)
				{
					if (addr < 0 || addr >= 0x10000000)
						throw new ArgumentOutOfRangeException();
					return LibVBANext.SystemBusRead(Core, (int)addr);
				},
				delegate(long addr, byte val)
				{
					if (addr < 0 || addr >= 0x10000000)
						throw new ArgumentOutOfRangeException();
					LibVBANext.SystemBusWrite(Core, (int)addr, val);
				}, 4));
			// special combined ram memory domain
			{
				var ew = mm[1];
				var iw = mm[0];
				MemoryDomain cr = new MemoryDomainDelegate("Combined WRAM", (256 + 32) * 1024, MemoryDomain.Endian.Little,
					delegate(long addr)
					{
						if (addr < 0 || addr >= (256 + 32) * 1024)
							throw new IndexOutOfRangeException();
						if (addr >= 256 * 1024)
							return iw.PeekByte(addr & 32767);
						else
							return ew.PeekByte(addr);
					},
					delegate(long addr, byte val)
					{
						if (addr < 0 || addr >= (256 + 32) * 1024)
							throw new IndexOutOfRangeException();
						if (addr >= 256 * 1024)
							iw.PokeByte(addr & 32767, val);
						else
							ew.PokeByte(addr, val);
					}, 4);
				mm.Add(cr);
			}

			_memoryDomains = new MemoryDomainList(mm);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(_memoryDomains);
		}
	}
}
