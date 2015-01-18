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
				new MemoryDomain(
					"Main RAM",
					128,
					MemoryDomain.Endian.Little,
					addr => Ram[addr],
					(addr, value) => Ram[addr] = value),
				new MemoryDomain(
					"TIA",
					16,
					MemoryDomain.Endian.Little,
					addr => _tia.ReadMemory((ushort)addr, true),
					(addr, value) => this._tia.WriteMemory((ushort)addr, value)),
				new MemoryDomain(
					"PIA",
					1024,
					MemoryDomain.Endian.Little,
					addr => M6532.ReadMemory((ushort)addr, true),
					(addr, value) => M6532.WriteMemory((ushort)addr, value)),
				new MemoryDomain(
					"System Bus",
					65536,
					MemoryDomain.Endian.Little,
					addr => _mapper.PeekMemory((ushort) addr),
					(addr, value) => _mapper.PokeMemory((ushort) addr, value)) 
			};

			if (_mapper is mDPC) // TODO: also mDPCPlus
			{
				domains.Add(new MemoryDomain(
					"DPC",
					2048,
					MemoryDomain.Endian.Little,
					addr => (_mapper as mDPC).DspData[addr],
					(addr, value) => (_mapper as mDPC).DspData[addr] = value));
			}

			if (_mapper.HasCartRam)
			{
				domains.Add(new MemoryDomain(
					"Cart Ram",
					_mapper.CartRam.Len,
					MemoryDomain.Endian.Little,
					addr => _mapper.CartRam[(int)addr],
					(addr, value) => _mapper.CartRam[(int)addr] = value));
			}

			MemoryDomains = new MemoryDomainList(domains);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(MemoryDomains);
		}
	}
}
