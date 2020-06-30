using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	public partial class VectrexHawk
	{
		private IMemoryDomains MemoryDomains;

		public void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>
			{
				new MemoryDomainDelegate(
					"Main RAM",
					RAM.Length,
					MemoryDomain.Endian.Little,
					addr => RAM[addr],
					(addr, value) => RAM[addr] = value,
					1),
				new MemoryDomainDelegate(
					"System Bus",
					0X10000,
					MemoryDomain.Endian.Little,
					addr => PeekSystemBus(addr),
					(addr, value) => PokeSystemBus(addr, value),
					1),
				new MemoryDomainDelegate(
					"ROM",
					_rom.Length,
					MemoryDomain.Endian.Little,
					addr => _rom[addr],
					(addr, value) => _rom[addr] = value,
					1),
			};

			MemoryDomains = new MemoryDomainList(domains);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(MemoryDomains);
		}

		private byte PeekSystemBus(long addr)
		{
			ushort addr2 = (ushort)(addr & 0xFFFF);
			return PeekMemory(addr2);
		}

		private void PokeSystemBus(long addr, byte value)
		{
			ushort addr2 = (ushort)(addr & 0xFFFF);
			WriteMemory(addr2, value);
		}
	}
}
