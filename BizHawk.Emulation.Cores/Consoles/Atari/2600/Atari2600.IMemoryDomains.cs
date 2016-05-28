using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public partial class Atari2600
	{
		internal IMemoryDomains MemoryDomains;

		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>
			{
				new MemoryDomainByteArray(
					"Main RAM",
					MemoryDomain.Endian.Little,
					Ram, true, 1),
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
					addr => M6532.ReadMemory((ushort)addr, true),
					(addr, value) => M6532.WriteMemory((ushort)addr, value), 1),
				new MemoryDomainDelegate(
					"System Bus",
					65536,
					MemoryDomain.Endian.Little,
					addr => _mapper.PeekMemory((ushort) addr),
					(addr, value) => _mapper.PokeMemory((ushort) addr, value), 1) 
			};

			if (_mapper is mDPC) // TODO: also mDPCPlus
			{
				domains.Add(new MemoryDomainByteArray(
					"DPC",
					MemoryDomain.Endian.Little,(_mapper as mDPC).DspData, true, 1));
			}

			if (_mapper.HasCartRam)
			{
				domains.Add(new MemoryDomainDelegate(
					"Cart Ram",
					_mapper.CartRam.Len,
					MemoryDomain.Endian.Little,
					addr => _mapper.CartRam[(int)addr],
					(addr, value) => _mapper.CartRam[(int)addr] = value, 1));
			}

			MemoryDomains = new MemoryDomainList(domains);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(MemoryDomains);
		}
	}
}
