using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Calculators
{
	public partial class TI83
	{
		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>
			{
				MemoryDomain.FromByteArray("Main RAM", MemoryDomain.Endian.Little, ram)
			};

			var systemBusDomain = new MemoryDomain("System Bus", 0x10000, MemoryDomain.Endian.Little,
				(addr) =>
				{
					if (addr < 0 || addr >= 65536)
						throw new ArgumentOutOfRangeException();
					return cpu.ReadMemory((ushort)addr);
				},
				(addr, value) =>
				{
					if (addr < 0 || addr >= 65536)
						throw new ArgumentOutOfRangeException();
					cpu.WriteMemory((ushort)addr, value);
				});

			domains.Add(systemBusDomain);

			_memoryDomains = new MemoryDomainList(domains);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(_memoryDomains);
		}

		private IMemoryDomains _memoryDomains;
	}
}
