using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.O2Hawk
{
	public partial class O2Hawk
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
					"CPU RAM",
					64,
					MemoryDomain.Endian.Little,
					addr => (byte)cpu.Regs[addr],
					(addr, value) => cpu.Regs[addr] = value,
					1),
				new MemoryDomainDelegate(
					"System Bus",
					0X1000,
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
				new MemoryDomainDelegate(
					"PPU",
					256,
					MemoryDomain.Endian.Little,
					addr => ppu.ReadReg((int)addr),
					(addr, value) => ppu.WriteReg((int)addr, value),
					1)
			};

			if (cart_RAM != null)
			{
				var CartRam = new MemoryDomainByteArray("Cart RAM", MemoryDomain.Endian.Little, cart_RAM, true, 1);
				domains.Add(CartRam);
			}

			MemoryDomains = new MemoryDomainList(domains);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(MemoryDomains);
		}

		private byte PeekSystemBus(long addr)
		{
			ushort addr2 = (ushort)(addr & 0xFFF);
			return PeekMemory(addr2);
		}

		private void PokeSystemBus(long addr, byte value)
		{
			ushort addr2 = (ushort)(addr & 0xFFF);
			WriteMemory(addr2, value);
		}
	}
}
