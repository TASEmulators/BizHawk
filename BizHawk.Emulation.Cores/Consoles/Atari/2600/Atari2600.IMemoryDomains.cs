using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public partial class Atari2600
	{
		internal IMemoryDomains MemoryDomains;
		private readonly Dictionary<string, MemoryDomainByteArray> _byteArrayDomains = new Dictionary<string, MemoryDomainByteArray>();
		private bool _memoryDomainsInit;

		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>
			{
				new MemoryDomainDelegate(
					"TIA",
					16,
					MemoryDomain.Endian.Little,
					addr => _tia.ReadMemory((ushort)addr, true),
					(addr, value) => this._tia.WriteMemory((ushort)addr, value, true), 1),
				new MemoryDomainDelegate(
					"PIA",
					1024,
					MemoryDomain.Endian.Little,
					addr => _m6532.ReadMemory((ushort)addr, true),
					(addr, value) => _m6532.WriteMemory((ushort)addr, value), 1),
				new MemoryDomainDelegate(
					"System Bus",
					65536,
					MemoryDomain.Endian.Little,
					addr => _mapper.PeekMemory((ushort) addr),
					(addr, value) => _mapper.PokeMemory((ushort) addr, value), 1) 
			};

			if (_mapper.HasCartRam)
			{
				domains.Add(new MemoryDomainDelegate(
					"Cart Ram",
					_mapper.CartRam.Length,
					MemoryDomain.Endian.Little,
					addr => _mapper.CartRam[(int)addr],
					(addr, value) => _mapper.CartRam[(int)addr] = value, 1));
			}

			SyncAllByteArrayDomains();

			MemoryDomains = new MemoryDomainList(_byteArrayDomains.Values.Concat(domains).ToList());
			((BasicServiceProvider)ServiceProvider).Register(MemoryDomains);

			_memoryDomainsInit = true;
		}

		private void SyncAllByteArrayDomains()
		{
			SyncByteArrayDomain("Main RAM", _ram);

			if (_mapper is mDPC dpc)
			{
				SyncByteArrayDomain("DPC", dpc.DspData);
			}
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
