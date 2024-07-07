using System.Collections.Generic;
using System.Linq;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public partial class ChannelF
	{
		internal IMemoryDomains memoryDomains;
		private readonly Dictionary<string, MemoryDomainByteArray> _byteArrayDomains = new Dictionary<string, MemoryDomainByteArray>();
		private bool _memoryDomainsInit = false;

		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>
			{
				new MemoryDomainDelegate("System Bus", 0x10000, MemoryDomain.Endian.Big,
					(addr) =>
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

			memoryDomains = new MemoryDomainList(_byteArrayDomains.Values.Concat(domains).ToList());
			(ServiceProvider as BasicServiceProvider)?.Register<IMemoryDomains>(memoryDomains);

			_memoryDomainsInit = true;
		}

		private void SyncAllByteArrayDomains()
		{
			SyncByteArrayDomain("BIOS1", BIOS01);
			SyncByteArrayDomain("BIOS2", BIOS02);
			Cartridge.SyncByteArrayDomain(this);
			//SyncByteArrayDomain("ROM", Rom);
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
				var m = new MemoryDomainByteArray(name, MemoryDomain.Endian.Big, data, false, 1);
				_byteArrayDomains.Add(name, m);
			}
#pragma warning restore MEN014
		}
	}
}
