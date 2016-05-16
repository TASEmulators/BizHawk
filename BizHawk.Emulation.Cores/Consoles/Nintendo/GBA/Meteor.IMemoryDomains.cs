using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class GBA 
	{
		private List<MemoryDomain> _domainList = new List<MemoryDomain>();
		private IMemoryDomains _memoryDomains;

		private void AddMemoryDomain(LibMeteor.MemoryArea which, int size, string name)
		{
			IntPtr data = LibMeteor.libmeteor_getmemoryarea(which);
			if (data == IntPtr.Zero)
				throw new Exception("libmeteor_getmemoryarea() returned NULL??");

			MemoryDomain md = MemoryDomain.FromIntPtr(name, size, MemoryDomain.Endian.Little, data);
			_domainList.Add(md);
		}

		private void SetUpMemoryDomains()
		{
			_domainList.Clear();
			// this must be first to coincide with "main memory"
			// note that ewram could also be considered main memory depending on which hairs you split
			AddMemoryDomain(LibMeteor.MemoryArea.iwram, 32 * 1024, "IWRAM");
			AddMemoryDomain(LibMeteor.MemoryArea.ewram, 256 * 1024, "EWRAM");
			AddMemoryDomain(LibMeteor.MemoryArea.bios, 16 * 1024, "BIOS");
			AddMemoryDomain(LibMeteor.MemoryArea.palram, 1024, "PALRAM");
			AddMemoryDomain(LibMeteor.MemoryArea.vram, 96 * 1024, "VRAM");
			AddMemoryDomain(LibMeteor.MemoryArea.oam, 1024, "OAM");
			// even if the rom is less than 32MB, the whole is still valid in meteor
			AddMemoryDomain(LibMeteor.MemoryArea.rom, 32 * 1024 * 1024, "ROM");
			// special domain for system bus
			{
				MemoryDomain sb = new MemoryDomainDelegate("System Bus", 1 << 28, MemoryDomain.Endian.Little,
					delegate(long addr)
					{
						if (addr < 0 || addr >= 0x10000000)
							throw new IndexOutOfRangeException();
						return LibMeteor.libmeteor_peekbus((uint)addr);
					},
					delegate(long addr, byte val)
					{
						if (addr < 0 || addr >= 0x10000000)
							throw new IndexOutOfRangeException();
						LibMeteor.libmeteor_writebus((uint)addr, val);
					}, 4);
				_domainList.Add(sb);
			}
			// special combined ram memory domain
			{
				var ew = _domainList[1];
				var iw = _domainList[0];
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
				_domainList.Add(cr);
			}

			_memoryDomains = new MemoryDomainList(_domainList);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(_memoryDomains);
		}
	}
}
