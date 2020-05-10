using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink4x
{
	public partial class GBHawkLink4x
	{
		private IMemoryDomains MemoryDomains;

		public void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>
			{
				new MemoryDomainDelegate(
					"Main RAM A",
					A.RAM.Length,
					MemoryDomain.Endian.Little,
					addr => A.RAM[addr],
					(addr, value) => A.RAM[addr] = value,
					1),
				new MemoryDomainDelegate(
					"Main RAM B",
					B.RAM.Length,
					MemoryDomain.Endian.Little,
					addr => B.RAM[addr],
					(addr, value) => B.RAM[addr] = value,
					1),
				new MemoryDomainDelegate(
					"Main RAM C",
					C.RAM.Length,
					MemoryDomain.Endian.Little,
					addr => C.RAM[addr],
					(addr, value) => C.RAM[addr] = value,
					1),
				new MemoryDomainDelegate(
					"Main RAM D",
					D.RAM.Length,
					MemoryDomain.Endian.Little,
					addr => D.RAM[addr],
					(addr, value) => D.RAM[addr] = value,
					1),
				new MemoryDomainDelegate(
					"Zero Page RAM A",
					A.ZP_RAM.Length,
					MemoryDomain.Endian.Little,
					addr => A.ZP_RAM[addr],
					(addr, value) => A.ZP_RAM[addr] = value,
					1),
				new MemoryDomainDelegate(
					"Zero Page RAM B",
					B.ZP_RAM.Length,
					MemoryDomain.Endian.Little,
					addr => B.ZP_RAM[addr],
					(addr, value) => B.ZP_RAM[addr] = value,
					1),
				new MemoryDomainDelegate(
					"Zero Page RAM C",
					C.ZP_RAM.Length,
					MemoryDomain.Endian.Little,
					addr => C.ZP_RAM[addr],
					(addr, value) => C.ZP_RAM[addr] = value,
					1),
				new MemoryDomainDelegate(
					"Zero Page RAM D",
					D.ZP_RAM.Length,
					MemoryDomain.Endian.Little,
					addr => D.ZP_RAM[addr],
					(addr, value) => D.ZP_RAM[addr] = value,
					1),
				new MemoryDomainDelegate(
					"System Bus A",
					0X10000,
					MemoryDomain.Endian.Little,
					PeekSystemBusA,
					PokeSystemBusA,
					1),
				new MemoryDomainDelegate(
					"System Bus B",
					0X10000,
					MemoryDomain.Endian.Little,
					PeekSystemBusB,
					PokeSystemBusB,
					1),
				new MemoryDomainDelegate(
					"System Bus C",
					0X10000,
					MemoryDomain.Endian.Little,
					PeekSystemBusC,
					PokeSystemBusC,
					1),
				new MemoryDomainDelegate(
					"System Bus D",
					0X10000,
					MemoryDomain.Endian.Little,
					PeekSystemBusD,
					PokeSystemBusD,
					1),
				new MemoryDomainDelegate(
					"ROM A",
					A._rom.Length,
					MemoryDomain.Endian.Little,
					addr => A._rom[addr],
					(addr, value) => A._rom[addr] = value,
					1),
				new MemoryDomainDelegate(
					"ROM B",
					B._rom.Length,
					MemoryDomain.Endian.Little,
					addr => B._rom[addr],
					(addr, value) => B._rom[addr] = value,
					1),
				new MemoryDomainDelegate(
					"ROM C",
					C._rom.Length,
					MemoryDomain.Endian.Little,
					addr => C._rom[addr],
					(addr, value) => C._rom[addr] = value,
					1),
				new MemoryDomainDelegate(
					"ROM D",
					D._rom.Length,
					MemoryDomain.Endian.Little,
					addr => D._rom[addr],
					(addr, value) => D._rom[addr] = value,
					1),
				new MemoryDomainDelegate(
					"VRAM A",
					A.VRAM.Length,
					MemoryDomain.Endian.Little,
					addr => A.VRAM[addr],
					(addr, value) => A.VRAM[addr] = value,
					1),
				new MemoryDomainDelegate(
					"VRAM B",
					B.VRAM.Length,
					MemoryDomain.Endian.Little,
					addr => B.VRAM[addr],
					(addr, value) => B.VRAM[addr] = value,
					1),
				new MemoryDomainDelegate(
					"VRAM C",
					C.VRAM.Length,
					MemoryDomain.Endian.Little,
					addr => C.VRAM[addr],
					(addr, value) => C.VRAM[addr] = value,
					1),
				new MemoryDomainDelegate(
					"VRAM D",
					D.VRAM.Length,
					MemoryDomain.Endian.Little,
					addr => D.VRAM[addr],
					(addr, value) => D.VRAM[addr] = value,
					1)
			};

			if (A.cart_RAM != null)
			{
				var cartRamA = new MemoryDomainByteArray("Cart RAM L", MemoryDomain.Endian.Little, A.cart_RAM, true, 1);
				domains.Add(cartRamA);
			}

			if (B.cart_RAM != null)
			{
				var cartRamB = new MemoryDomainByteArray("Cart RAM B", MemoryDomain.Endian.Little, B.cart_RAM, true, 1);
				domains.Add(cartRamB);
			}

			if (C.cart_RAM != null)
			{
				var cartRamC = new MemoryDomainByteArray("Cart RAM C", MemoryDomain.Endian.Little, C.cart_RAM, true, 1);
				domains.Add(cartRamC);
			}

			if (D.cart_RAM != null)
			{
				var cartRamD = new MemoryDomainByteArray("Cart RAM D", MemoryDomain.Endian.Little, D.cart_RAM, true, 1);
				domains.Add(cartRamD);
			}

			MemoryDomains = new MemoryDomainList(domains);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(MemoryDomains);
		}

		private byte PeekSystemBusA(long addr)
		{
			ushort addr2 = (ushort)(addr & 0xFFFF);
			return A.PeekMemory(addr2);
		}

		private byte PeekSystemBusB(long addr)
		{
			ushort addr2 = (ushort)(addr & 0xFFFF);
			return B.PeekMemory(addr2);
		}

		private byte PeekSystemBusC(long addr)
		{
			ushort addr2 = (ushort)(addr & 0xFFFF);
			return C.PeekMemory(addr2);
		}

		private byte PeekSystemBusD(long addr)
		{
			ushort addr2 = (ushort)(addr & 0xFFFF);
			return D.PeekMemory(addr2);
		}

		private void PokeSystemBusA(long addr, byte value)
		{
			ushort addr2 = (ushort)(addr & 0xFFFF);
			A.WriteMemory(addr2, value);
		}

		private void PokeSystemBusB(long addr, byte value)
		{
			ushort addr2 = (ushort)(addr & 0xFFFF);
			B.WriteMemory(addr2, value);
		}

		private void PokeSystemBusC(long addr, byte value)
		{
			ushort addr2 = (ushort)(addr & 0xFFFF);
			C.WriteMemory(addr2, value);
		}

		private void PokeSystemBusD(long addr, byte value)
		{
			ushort addr2 = (ushort)(addr & 0xFFFF);
			D.WriteMemory(addr2, value);
		}
	}
}
