using System;
using System.Collections.Generic;

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

			mm.Add(new MemoryDomainIntPtr("IWRAM", l, s.iwram, 32 * 1024, true, 4));
			mm.Add(new MemoryDomainIntPtr("EWRAM", l, s.ewram, 256 * 1024, true, 4));
			mm.Add(new MemoryDomainIntPtr("BIOS", l, s.bios, 16 * 1024, false, 4));
			mm.Add(new MemoryDomainIntPtr("PALRAM", l, s.palram, 1024, false, 4));
			mm.Add(new MemoryDomainIntPtr("VRAM", l, s.vram, 96 * 1024, true, 4));
			mm.Add(new MemoryDomainIntPtr("OAM", l, s.oam, 1024, true, 4));

			mm.Add(new MemoryDomainIntPtr("ROM", l, s.rom, 32 * 1024 * 1024, true, 4));

			mm.Add(new MemoryDomainIntPtr("SRAM", l, s.sram, s.sram_size, true, 4));

			mm.Add(new MemoryDomainDelegate("System Bus", 0x10000000, l,
				delegate(long addr)
				{
					if (addr < 0 || addr >= 0x10000000)
					{
						throw new ArgumentOutOfRangeException();
					}

					return LibVBANext.SystemBusRead(Core, (int)addr);
				},
				delegate(long addr, byte val)
				{
					if (addr < 0 || addr >= 0x10000000)
					{
						throw new ArgumentOutOfRangeException();
					}

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
