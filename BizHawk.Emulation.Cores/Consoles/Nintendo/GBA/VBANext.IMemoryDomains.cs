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
			mm.Add(MemoryDomain.FromIntPtr("IWRAM", 32 * 1024, l, s.iwram));
			mm.Add(MemoryDomain.FromIntPtr("EWRAM", 256 * 1024, l, s.ewram));
			mm.Add(MemoryDomain.FromIntPtr("BIOS", 16 * 1024, l, s.bios, false));
			mm.Add(MemoryDomain.FromIntPtr("PALRAM", 1024, l, s.palram, false));
			mm.Add(MemoryDomain.FromIntPtr("VRAM", 96 * 1024, l, s.vram));
			mm.Add(MemoryDomain.FromIntPtr("OAM", 1024, l, s.oam));
			mm.Add(MemoryDomain.FromIntPtr("ROM", 32 * 1024 * 1024, l, s.rom));

			mm.Add(new MemoryDomain("System Bus", 0x10000000, l,
				delegate(int addr)
				{
					if (addr < 0 || addr >= 0x10000000)
						throw new ArgumentOutOfRangeException();
					return LibVBANext.SystemBusRead(Core, addr);
				},
				delegate(int addr, byte val)
				{
					if (addr < 0 || addr >= 0x10000000)
						throw new ArgumentOutOfRangeException();
					LibVBANext.SystemBusWrite(Core, addr, val);
				}));
			// special combined ram memory domain
			{
				var ew = mm[1];
				var iw = mm[0];
				MemoryDomain cr = new MemoryDomain("Combined WRAM", (256 + 32) * 1024, MemoryDomain.Endian.Little,
					delegate(int addr)
					{
						if (addr < 0 || addr >= (256 + 32) * 1024)
							throw new IndexOutOfRangeException();
						if (addr >= 256 * 1024)
							return iw.PeekByte(addr & 32767);
						else
							return ew.PeekByte(addr);
					},
					delegate(int addr, byte val)
					{
						if (addr < 0 || addr >= (256 + 32) * 1024)
							throw new IndexOutOfRangeException();
						if (addr >= 256 * 1024)
							iw.PokeByte(addr & 32767, val);
						else
							ew.PokeByte(addr, val);
					});
				mm.Add(cr);
			}

			_memoryDomains = new MemoryDomainList(mm, 0);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(_memoryDomains);
		}
	}
}
