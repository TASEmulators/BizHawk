using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	public partial class GBHawk
	{
		private IMemoryDomains MemoryDomains;

		public void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>
			{
				new MemoryDomainDelegate(
					"WRAM",
					RAM.Length,
					MemoryDomain.Endian.Little,
					addr => PeekRAM(addr),
					(addr, value) => RAM[addr] = value,
					1),
				new MemoryDomainDelegate(
					"ROM",
					_rom.Length,
					MemoryDomain.Endian.Little,
					addr => _rom[addr],
					(addr, value) => _rom[addr] = value,
					1),
				new MemoryDomainDelegate(
					"VRAM",
					VRAM.Length,
					MemoryDomain.Endian.Little,
					addr => PeekVRAM(addr),
					(addr, value) => VRAM[addr] = value,
					1),
				new MemoryDomainDelegate(
					"OAM",
					OAM.Length,
					MemoryDomain.Endian.Little,
					addr => PeekOAM(addr),
					(addr, value) => OAM[addr] = value,
					1),
				new MemoryDomainDelegate(
					"HRAM",
					ZP_RAM.Length,
					MemoryDomain.Endian.Little,
					addr => PeekHRAM(addr),
					(addr, value) => ZP_RAM[addr] = value,
					1),
				new MemoryDomainDelegate(
					"System Bus",
					0X10000,
					MemoryDomain.Endian.Little,
					addr => PeekSystemBus(addr),
					(addr, value) => PokeSystemBus(addr, value),
					1),
			};

			if (cart_RAM != null)
			{
				var CartRam = new MemoryDomainDelegate(
					"CartRAM",
					cart_RAM.Length,
					MemoryDomain.Endian.Little,
					addr => PeekCART(addr),
					(addr, value) => cart_RAM[addr] = value,
					1);
				domains.Add(CartRam);
			}

			MemoryDomains = new MemoryDomainList(domains);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(MemoryDomains);
		}

		private byte PeekRAM(long addr)
		{
			ushort addr2 = (ushort)(addr & 0x7FFF);
			if (_settings.VBL_sync)
			{
				return RAM_vbls[addr2];
			}
			return RAM[addr2];
		}

		private byte PeekVRAM(long addr)
		{
			ushort addr2 = (ushort)(addr & 0x3FFF);
			if (_settings.VBL_sync)
			{
				return VRAM_vbls[addr2];
			}
			return VRAM[addr2];
		}

		private byte PeekHRAM(long addr)
		{
			ushort addr2 = (ushort)(addr & 0x7F);
			if (_settings.VBL_sync)
			{
				return ZP_RAM_vbls[addr2];
			}
			return ZP_RAM[addr2];
		}

		private byte PeekOAM(long addr)
		{
			if (addr < 0xA0)
			{
				if (_settings.VBL_sync)
				{
					return OAM_vbls[addr];
				}
				return OAM[addr];
			}
			return 0xFF;
		}

		private byte PeekCART(long addr)
		{
			if (cart_RAM != null)
			{
				if (addr < cart_RAM.Length)
				{
					if (_settings.VBL_sync)
					{
						return cart_RAM_vbls[addr];
					}
					return cart_RAM[addr];
				}

				return 0xFF;
			}

			return 0xFF;
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
