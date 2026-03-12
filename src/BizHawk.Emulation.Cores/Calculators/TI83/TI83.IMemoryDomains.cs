using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Calculators.TI83
{
	public partial class TI83
	{
		private readonly Dictionary<string, MemoryDomainByteArray> _byteArrayDomains = new Dictionary<string, MemoryDomainByteArray>();
		private IMemoryDomains _memoryDomains;
		private bool _memoryDomainsInit;

		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>();

			var systemBusDomain = new MemoryDomainDelegate("System Bus", 0x10000, MemoryDomain.Endian.Little,
				(addr) =>
				{
					if (addr is < 0 or > 0xFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
					return ReadMemory((ushort)addr);
				},
				(addr, value) =>
				{
					if (addr is < 0 or > 0xFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
					WriteMemory((ushort)addr, value);
				}, 1);

			domains.Add(systemBusDomain);

			SyncAllByteArrayDomains();

			_memoryDomains = new MemoryDomainList(_byteArrayDomains.Values.Concat(domains).ToList());
			((BasicServiceProvider) ServiceProvider).Register(_memoryDomains);

			_memoryDomainsInit = true;
		}

		private void SyncAllByteArrayDomains()
		{
			SyncByteArrayDomain("Main RAM", _ram);
		}

		private void SyncByteArrayDomain(string name, byte[] data)
		{
			if (_memoryDomainsInit)
			{
				var m = _byteArrayDomains[name];
				m.Data = data;
			}
			else
			{
				var m = new MemoryDomainByteArray(name, MemoryDomain.Endian.Little, data, true, 1);
				_byteArrayDomains.Add(name, m);
			}
		}
	}
}
