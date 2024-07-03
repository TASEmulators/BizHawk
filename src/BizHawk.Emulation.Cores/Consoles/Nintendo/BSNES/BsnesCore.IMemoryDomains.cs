using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.BSNES
{
	public partial class BsnesCore
	{
		private MemoryDomainList _memoryDomains;

		private void SetMemoryDomains()
		{
			List<MemoryDomain> mm = new();
			foreach (int i in Enum.GetValues(typeof(BsnesApi.SNES_MEMORY)))
			{
				var data = Api.core.snes_get_memory_region(i, out var size, out var wordSize);
				if (i == (int)BsnesApi.SNES_MEMORY.OAM)
				{
					mm.Add(new MemoryDomainDelegate("OAM", size, MemoryDomain.Endian.Little,
						address => Api.core.snes_read_oam((ushort)address),
						(address, value) => Api.core.snes_write_oam((ushort)address, value), wordSize));
					continue;
				}
				if (data == IntPtr.Zero) continue;
				if (i == (int) BsnesApi.SNES_MEMORY.CARTRAM)
				{
					_saveRam = data;
					_saveRamSize = size;
				}
				mm.Add(new MemoryDomainIntPtrMonitor(Enum.GetName(typeof(BsnesApi.SNES_MEMORY), i)!.Replace('_', ' '), MemoryDomain.Endian.Little, data, size, true, wordSize, Api));
			}

			mm.Add(new MemoryDomainDelegate(
				"System Bus",
				0x1000000,
				MemoryDomain.Endian.Little,
				address => Api.core.snes_bus_read((uint) address),
				(address, value) => Api.core.snes_bus_write((uint) address, value), wordSize: 4));

			if (_isSGB)
			{
				foreach (int i in Enum.GetValues(typeof(BsnesApi.SGB_MEMORY)))
				{
					var data = Api.core.snes_get_sgb_memory_region(i, out var size);
					if (data == IntPtr.Zero || size == 0) continue;
					mm.Add(new MemoryDomainIntPtrMonitor("SGB " + Enum.GetName(typeof(BsnesApi.SGB_MEMORY), i), MemoryDomain.Endian.Little, data, size, true, 1, Api));
				}

				mm.Add(new MemoryDomainDelegate(
					"SGB System Bus",
					0x10000,
					MemoryDomain.Endian.Little,
					address => Api.core.snes_sgb_bus_read((ushort) address),
					(address, value) => Api.core.snes_sgb_bus_write((ushort) address, value), wordSize: 1));

				_saveRam = IntPtr.Zero;
				_saveRamSize = Api.core.snes_sgb_battery_size();
			}

			mm.Add(Api.exe.GetPagesDomain());

			_memoryDomains = new(mm);
			((BasicServiceProvider) ServiceProvider).Register<IMemoryDomains>(_memoryDomains);

			_memoryDomains.MainMemory = _memoryDomains[_isSGB ? "SGB WRAM" : "WRAM"];
			_memoryDomains.SystemBus = _memoryDomains[_isSGB ? "SGB System Bus" : "System Bus"];
		}
	}
}
