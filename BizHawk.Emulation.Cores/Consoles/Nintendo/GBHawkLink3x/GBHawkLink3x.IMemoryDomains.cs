using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink3x
{
	public partial class GBHawkLink3x
	{
		private IMemoryDomains MemoryDomains;

		public void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>
			{
				new MemoryDomainDelegate(
					"Main RAM L",
					L.RAM.Length,
					MemoryDomain.Endian.Little,
					addr => L.RAM[addr],
					(addr, value) => L.RAM[addr] = value,
					1),
				new MemoryDomainDelegate(
					"Main RAM C",
					C.RAM.Length,
					MemoryDomain.Endian.Little,
					addr => C.RAM[addr],
					(addr, value) => C.RAM[addr] = value,
					1),
				new MemoryDomainDelegate(
					"Main RAM R",
					R.RAM.Length,
					MemoryDomain.Endian.Little,
					addr => R.RAM[addr],
					(addr, value) => R.RAM[addr] = value,
					1),
				new MemoryDomainDelegate(
					"Zero Page RAM L",
					L.ZP_RAM.Length,
					MemoryDomain.Endian.Little,
					addr => L.ZP_RAM[addr],
					(addr, value) => L.ZP_RAM[addr] = value,
					1),
				new MemoryDomainDelegate(
					"Zero Page RAM C",
					C.ZP_RAM.Length,
					MemoryDomain.Endian.Little,
					addr => C.ZP_RAM[addr],
					(addr, value) => C.ZP_RAM[addr] = value,
					1),
				new MemoryDomainDelegate(
					"Zero Page RAM R",
					R.ZP_RAM.Length,
					MemoryDomain.Endian.Little,
					addr => R.ZP_RAM[addr],
					(addr, value) => R.ZP_RAM[addr] = value,
					1),
				new MemoryDomainDelegate(
					"System Bus L",
					0X10000,
					MemoryDomain.Endian.Little,
					PeekSystemBusL,
					PokeSystemBusL,
					1),
				new MemoryDomainDelegate(
					"System Bus C",
					0X10000,
					MemoryDomain.Endian.Little,
					PeekSystemBusC,
					PokeSystemBusC,
					1),
				new MemoryDomainDelegate(
					"System Bus R",
					0X10000,
					MemoryDomain.Endian.Little,
					PeekSystemBusR,
					PokeSystemBusR,
					1),
				new MemoryDomainDelegate(
					"ROM L",
					L._rom.Length,
					MemoryDomain.Endian.Little,
					addr => L._rom[addr],
					(addr, value) => L._rom[addr] = value,
					1),
				new MemoryDomainDelegate(
					"ROM C",
					C._rom.Length,
					MemoryDomain.Endian.Little,
					addr => C._rom[addr],
					(addr, value) => C._rom[addr] = value,
					1),
				new MemoryDomainDelegate(
					"ROM R",
					R._rom.Length,
					MemoryDomain.Endian.Little,
					addr => R._rom[addr],
					(addr, value) => R._rom[addr] = value,
					1),
				new MemoryDomainDelegate(
					"VRAM L",
					L.VRAM.Length,
					MemoryDomain.Endian.Little,
					addr => L.VRAM[addr],
					(addr, value) => L.VRAM[addr] = value,
					1),
				new MemoryDomainDelegate(
					"VRAM C",
					C.VRAM.Length,
					MemoryDomain.Endian.Little,
					addr => C.VRAM[addr],
					(addr, value) => C.VRAM[addr] = value,
					1),
				new MemoryDomainDelegate(
					"VRAM R",
					R.VRAM.Length,
					MemoryDomain.Endian.Little,
					addr => R.VRAM[addr],
					(addr, value) => R.VRAM[addr] = value,
					1)
			};

			if (L.cart_RAM != null)
			{
				var cartRamL = new MemoryDomainByteArray("Cart RAM L", MemoryDomain.Endian.Little, L.cart_RAM, true, 1);
				domains.Add(cartRamL);
			}

			if (C.cart_RAM != null)
			{
				var cartRamC = new MemoryDomainByteArray("Cart RAM C", MemoryDomain.Endian.Little, C.cart_RAM, true, 1);
				domains.Add(cartRamC);
			}

			if (R.cart_RAM != null)
			{
				var cartRamR = new MemoryDomainByteArray("Cart RAM R", MemoryDomain.Endian.Little, R.cart_RAM, true, 1);
				domains.Add(cartRamR);
			}

			MemoryDomains = new MemoryDomainList(domains);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(MemoryDomains);
		}

		private byte PeekSystemBusL(long addr)
		{
			ushort addr2 = (ushort)(addr & 0xFFFF);
			return L.PeekMemory(addr2);
		}

		private byte PeekSystemBusC(long addr)
		{
			ushort addr2 = (ushort)(addr & 0xFFFF);
			return C.PeekMemory(addr2);
		}

		private byte PeekSystemBusR(long addr)
		{
			ushort addr2 = (ushort)(addr & 0xFFFF);
			return R.PeekMemory(addr2);
		}

		private void PokeSystemBusL(long addr, byte value)
		{
			ushort addr2 = (ushort)(addr & 0xFFFF);
			L.WriteMemory(addr2, value);
		}

		private void PokeSystemBusC(long addr, byte value)
		{
			ushort addr2 = (ushort)(addr & 0xFFFF);
			C.WriteMemory(addr2, value);
		}

		private void PokeSystemBusR(long addr, byte value)
		{
			ushort addr2 = (ushort)(addr & 0xFFFF);
			R.WriteMemory(addr2, value);
		}
	}
}
