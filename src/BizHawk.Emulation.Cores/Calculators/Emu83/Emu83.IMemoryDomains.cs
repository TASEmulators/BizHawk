using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Calculators.Emu83
{
	public partial class Emu83
	{
		private readonly List<MemoryDomain> _memoryDomains = new();
		private IMemoryDomains MemoryDomains { get; set; }

		private void CreateMemoryDomain(LibEmu83.MemoryArea_t which, string name)
		{
			IntPtr data = IntPtr.Zero;
			int length = 0;

			if (!LibEmu83.TI83_GetMemoryArea(Context, which, ref data, ref length))
			{
				throw new Exception($"{nameof(LibEmu83.TI83_GetMemoryArea)}() failed!");
			}

			_memoryDomains.Add(new MemoryDomainIntPtr(name, MemoryDomain.Endian.Little, data, length, true, 1));
		}

		private void InitMemoryDomains()
		{
			CreateMemoryDomain(LibEmu83.MemoryArea_t.MEM_ROM, "ROM");
			CreateMemoryDomain(LibEmu83.MemoryArea_t.MEM_RAM, "RAM");
			CreateMemoryDomain(LibEmu83.MemoryArea_t.MEM_VRAM, "VRAM");

			_memoryDomains.Add(new MemoryDomainDelegate("System Bus", 0x10000, MemoryDomain.Endian.Little,
				delegate(long addr)
				{
					if (addr < 0 || addr >= 0x10000)
					{
						throw new ArgumentOutOfRangeException();
					}

					return LibEmu83.TI83_ReadMemory(Context, (ushort)addr);
				},
				delegate(long addr, byte val)
				{
					if (addr < 0 || addr >= 0x10000)
					{
						throw new ArgumentOutOfRangeException();
					}

					LibEmu83.TI83_WriteMemory(Context, (ushort)addr, val);
				}, 1));

			MemoryDomains = new MemoryDomainList(_memoryDomains);
			_serviceProvider.Register(MemoryDomains);
		}
	}
}
