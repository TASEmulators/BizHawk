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

			var mainRamDomain = new MemoryDomainDelegate("Main Ram", 0xC000, MemoryDomain.Endian.Little,
				addr =>
				{
					if (addr is < 0 or > 0xBFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
					return (byte)_machine.Memory.Peek((int)addr);
				},
				(addr, value) =>
				{
					if (addr is < 0 or > 0xBFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
					_machine.Memory.Write((int)addr, value);
				}, 1);

			domains.Add(mainRamDomain);

			var systemBusDomain = new MemoryDomainDelegate("System Bus", 0x10000, MemoryDomain.Endian.Little,
				addr =>
				{
					if (addr is < 0 or > 0xFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
					return (byte)_machine.Memory.Peek((int)addr);
				},
				(addr, value) =>
				{
					if (addr is < 0 or > 0xFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
					_machine.Memory.Write((int)addr, value);
				}, 1);

			domains.Add(systemBusDomain);

			_memoryDomains = new MemoryDomainList(domains);
			((BasicServiceProvider) ServiceProvider).Register(_memoryDomains);
		}

		private IMemoryDomains _memoryDomains;
	}
}
