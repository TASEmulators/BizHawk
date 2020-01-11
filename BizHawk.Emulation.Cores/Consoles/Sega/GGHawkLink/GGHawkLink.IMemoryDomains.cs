using System;

using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.GGHawkLink
{
	public partial class GGHawkLink
	{
		private IMemoryDomains _memoryDomains;

		public void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>
			{
				new MemoryDomainDelegate(
					"Main RAM L",
					L.SystemRam.Length,
					MemoryDomain.Endian.Little,
					addr => L.SystemRam[addr],
					(addr, value) => L.SystemRam[addr] = value,
					1),
				new MemoryDomainDelegate(
					"Main RAM R",
					R.SystemRam.Length,
					MemoryDomain.Endian.Little,
					addr => R.SystemRam[addr],
					(addr, value) => R.SystemRam[addr] = value,
					1),
				new MemoryDomainDelegate(
					"System Bus L",
					0X10000,
					MemoryDomain.Endian.Little,
					addr => PeekSystemBusL(addr),
					(addr, value) => PokeSystemBusL(addr, value),
					1),
				new MemoryDomainDelegate(
					"System Bus R",
					0X10000,
					MemoryDomain.Endian.Little,
					addr => PeekSystemBusR(addr),
					(addr, value) => PokeSystemBusR(addr, value),
					1),
				new MemoryDomainDelegate(
					"ROM L",
					L.RomData.Length,
					MemoryDomain.Endian.Little,
					addr => L.RomData[addr],
					(addr, value) => L.RomData[addr] = value,
					1),
				new MemoryDomainDelegate(
					"ROM R",
					R.RomData.Length,
					MemoryDomain.Endian.Little,
					addr => R.RomData[addr],
					(addr, value) => R.RomData[addr] = value,
					1),
				new MemoryDomainDelegate(
					"VRAM L",
					L.Vdp.VRAM.Length,
					MemoryDomain.Endian.Little,
					addr => L.Vdp.VRAM[addr],
					(addr, value) => L.Vdp.VRAM[addr] = value,
					1),
				new MemoryDomainDelegate(
					"VRAM R",
					R.Vdp.VRAM.Length,
					MemoryDomain.Endian.Little,
					addr => R.Vdp.VRAM[addr],
					(addr, value) => R.Vdp.VRAM[addr] = value,
					1)
			};

			if (L.SaveRAM != null)
			{
				var cartRamL = new MemoryDomainByteArray("Cart RAM L", MemoryDomain.Endian.Little, L.SaveRAM, true, 1);
				domains.Add(cartRamL);
			}

			if (R.SaveRAM != null)
			{
				var cartRamR = new MemoryDomainByteArray("Cart RAM R", MemoryDomain.Endian.Little, R.SaveRAM, true, 1);
				domains.Add(cartRamR);
			}

			_memoryDomains = new MemoryDomainList(domains);
			((BasicServiceProvider) ServiceProvider).Register(_memoryDomains);
		}

		private byte PeekSystemBusL(long addr)
		{
			ushort addr2 = (ushort)(addr & 0xFFFF);
			return L.ReadMemory(addr2);
		}

		private byte PeekSystemBusR(long addr)
		{
			ushort addr2 = (ushort)(addr & 0xFFFF);
			return R.ReadMemory(addr2);
		}

		private void PokeSystemBusL(long addr, byte value)
		{
			ushort addr2 = (ushort)(addr & 0xFFFF);
			L.WriteMemory(addr2, value);
		}

		private void PokeSystemBusR(long addr, byte value)
		{
			ushort addr2 = (ushort)(addr & 0xFFFF);
			R.WriteMemory(addr2, value);
		}
	}
}
