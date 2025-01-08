using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public partial class ChannelF
	{
		private IMemoryDomains _memoryDomains;
		private readonly Dictionary<string, MemoryDomainByteArray> _byteArrayDomains = [ ];
		private bool _memoryDomainsInit;

		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>
			{
				new MemoryDomainDelegate("Scratchpad", 64, MemoryDomain.Endian.Big,
					addr =>
					{
						if (addr is < 0 or > 63) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
						return _cpu.Regs[addr];
					},
					(addr, value) =>
					{
						if (addr is < 0 or > 63) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
						_cpu.Regs[addr] = value;
					}, 1),
				new MemoryDomainDelegate("System Bus", 0x10000, MemoryDomain.Endian.Big,
					addr =>
					{
						if (addr is < 0 or > 0xFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
						return ReadBus((ushort)addr);
					},
					(addr, value) =>
					{
						if (addr is < 0 or > 0xFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
						WriteBus((ushort)addr, value);
					}, 1)
			};

			SyncAllByteArrayDomains();

			_memoryDomains = new MemoryDomainList(_byteArrayDomains.Values.Concat(domains).ToList()) { MainMemory = domains[0] };
			((BasicServiceProvider)ServiceProvider).Register(_memoryDomains);

			_memoryDomainsInit = true;
		}

		private void SyncAllByteArrayDomains()
		{
			SyncByteArrayDomain("BIOS1", _bios01);
			SyncByteArrayDomain("BIOS2", _bios02);
			_cartridge.SyncByteArrayDomain(this);
			SyncByteArrayDomain("VRAM", _vram);
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
