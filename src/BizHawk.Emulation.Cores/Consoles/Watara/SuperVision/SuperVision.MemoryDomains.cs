using BizHawk.Emulation.Common;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Emulation.Cores.Consoles.SuperVision
{
	public partial class SuperVision
	{
		private IMemoryDomains _memoryDomains;
		private readonly Dictionary<string, MemoryDomainByteArray> _byteArrayDomains = [ ];
		private bool _memoryDomainsInit;

		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>
			{
				new MemoryDomainDelegate("System Bus", 0x10000, MemoryDomain.Endian.Big,
					addr =>
					{
						if (addr is < 0 or > 0xFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
						return ReadMemory((ushort)addr);
					},
					(addr, value) =>
					{
						if (addr is < 0 or > 0xFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
						WriteMemory((ushort)addr, value);
					}, 1)
			};

			SyncAllByteArrayDomains();

			_memoryDomains = new MemoryDomainList(_byteArrayDomains.Values.Concat(domains).ToList()) { MainMemory = domains[0] };
			((BasicServiceProvider) ServiceProvider).Register(_memoryDomains);

			_memoryDomainsInit = true;
		}


		private void SyncAllByteArrayDomains()
		{
			_cartridge.SyncByteArrayDomain(this);
			SyncByteArrayDomain("VRAM", VRAM);
		}

		public void SyncByteArrayDomain(string name, byte[] data)
		{
#pragma warning disable MEN014 // see ZXHawk copy
			if (_memoryDomainsInit || _byteArrayDomains.ContainsKey(name))
			{
				var m = _byteArrayDomains[name];
				m.Data = data;
			}
			else
			{
				var m = new MemoryDomainByteArray(name, MemoryDomain.Endian.Big, data, true, 1);
				_byteArrayDomains.Add(name, m);
			}
#pragma warning restore MEN014
		}
	}
}
