using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkNew
{
	public partial class GBHawkNew
	{
		private IMemoryDomains MemoryDomains;

		public void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>
			{
				new MemoryDomainDelegate(
					"WRAM",
					0x8000,
					MemoryDomain.Endian.Little,
					addr => LibGBHawk.GB_getram(GB_Pntr, (int)(addr & 0xFFFF)),
					(addr, value) => LibGBHawk.GB_setram(GB_Pntr, (int)(addr & 0xFFFF), value),
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
					0x4000,
					MemoryDomain.Endian.Little,
					addr => LibGBHawk.GB_getvram(GB_Pntr, (int)(addr & 0xFFFF)),
					(addr, value) => LibGBHawk.GB_setvram(GB_Pntr, (int)(addr & 0xFFFF), value),
					1),
				new MemoryDomainDelegate(
					"OAM",
					0xA0,
					MemoryDomain.Endian.Little,
					addr => LibGBHawk.GB_getoam(GB_Pntr, (int)(addr & 0xFFFF)),
					(addr, value) => LibGBHawk.GB_setoam(GB_Pntr, (int)(addr & 0xFFFF), value),
					1),
				new MemoryDomainDelegate(
					"HRAM",
					0x80,
					MemoryDomain.Endian.Little,
					addr => LibGBHawk.GB_gethram(GB_Pntr, (int)(addr & 0xFFFF)),
					(addr, value) => LibGBHawk.GB_sethram(GB_Pntr, (int)(addr & 0xFFFF), value),
					1),
				new MemoryDomainDelegate(
					"System Bus",
					0X10000,
					MemoryDomain.Endian.Little,
					addr => LibGBHawk.GB_getsysbus(GB_Pntr, (int)(addr & 0xFFFF)),
					(addr, value) => LibGBHawk.GB_setsysbus(GB_Pntr, (int)(addr & 0xFFFF), value),
					1),
			};
			/*
			if (cart_RAM != null)
			{
				var CartRam = new MemoryDomainByteArray("CartRAM", MemoryDomain.Endian.Little, cart_RAM, true, 1);
				domains.Add(CartRam);
			}
			*/

			MemoryDomains = new MemoryDomainList(domains);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(MemoryDomains);
		}
	}
}
