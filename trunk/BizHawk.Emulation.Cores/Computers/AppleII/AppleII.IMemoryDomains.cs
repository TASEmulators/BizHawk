using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	public partial class AppleII
	{
		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>();

			//_machine.Memory.Read
			var systemBusDomain = new MemoryDomain("System Bus", 0x10000, MemoryDomain.Endian.Little,
				(addr) =>
				{
					if (addr < 0 || addr >= 65536)
						throw new ArgumentOutOfRangeException();
					return (byte)_machine.Memory.Read((int)addr);
				},
				(addr, value) =>
				{
					if (addr < 0 || addr >= 65536)
						throw new ArgumentOutOfRangeException();
					_machine.Memory.Write((int)addr, value);
				});

			domains.Add(systemBusDomain);

			_memoryDomains = new MemoryDomainList(domains);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(_memoryDomains);
		}

		private IMemoryDomains _memoryDomains;
	}
}
