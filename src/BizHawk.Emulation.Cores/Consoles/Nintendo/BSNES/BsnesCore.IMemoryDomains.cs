using System;
using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public partial class BsnesCore
	{
		private IMemoryDomains _memoryDomains;

		private unsafe void SetMemoryDomains()
		{
			List<MemoryDomain> mm = new();
			foreach (int i in Enum.GetValues(typeof(BsnesApi.SNES_MEMORY)))
			{
				void* data = Api.core.snes_get_memory_region(i, out int size, out int wordSize);
				if (data == null) continue;
				if (Enum.GetName(typeof(BsnesApi.SNES_MEMORY), i) == BsnesApi.SNES_MEMORY.CARTRIDGE_RAM.ToString())
				{
					_saveRam = (byte*) data;
					_saveRamSize = size;
				}
				mm.Add(new MemoryDomainIntPtr(Enum.GetName(typeof(BsnesApi.SNES_MEMORY), i), MemoryDomain.Endian.Little, (IntPtr) data, size, true, wordSize));
			}

			mm.Add(new MemoryDomainDelegate(
				"System Bus",
				0x1000000,
				MemoryDomain.Endian.Little,
				address => Api.core.snes_bus_read((uint) address),
				(address, value) => Api.core.snes_bus_write((uint) address, value), wordSize: 4));
			mm.Add(Api.exe.GetPagesDomain());

			_memoryDomains = new MemoryDomainList(mm);
			((BasicServiceProvider) ServiceProvider).Register(_memoryDomains);
		}
	}
}
