using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public sealed partial class SMS
	{
		private MemoryDomainList MemoryDomains;

		void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>(3);
			var MainMemoryDomain = new MemoryDomainByteArray("Main RAM", MemoryDomain.Endian.Little, SystemRam, true, 1);
			var VRamDomain = new MemoryDomainByteArray("Video RAM", MemoryDomain.Endian.Little, Vdp.VRAM, true, 1);

			var ROMDomain = new MemoryDomainByteArray("ROM", MemoryDomain.Endian.Little, RomData, true, 1);

			var SystemBusDomain = new MemoryDomainDelegate("System Bus", 0x10000, MemoryDomain.Endian.Little,
				(addr) =>
				{
					if (addr < 0 || addr >= 65536)
					{
						throw new ArgumentOutOfRangeException();
					}

					return Cpu.ReadMemory((ushort)addr);
				},
				(addr, value) =>
				{
					if (addr < 0 || addr >= 65536)
					{
						throw new ArgumentOutOfRangeException();
					}

					Cpu.WriteMemory((ushort)addr, value);
				}, 1);

			domains.Add(MainMemoryDomain);
			domains.Add(VRamDomain);
			domains.Add(ROMDomain);
			domains.Add(SystemBusDomain);

			if (SaveRAM != null)
			{
				var SaveRamDomain = new MemoryDomainDelegate("Save RAM", SaveRAM.Length, MemoryDomain.Endian.Little,
					addr => SaveRAM[addr],
					(addr, value) => { SaveRAM[addr] = value; SaveRamModified = true; }, 1);
				domains.Add(SaveRamDomain);
			}

			if (ExtRam != null)
			{
				var ExtRamDomain = new MemoryDomainByteArray("Cart (Volatile) RAM", MemoryDomain.Endian.Little, ExtRam, true, 1);
				domains.Add(ExtRamDomain);
			}

			MemoryDomains = new MemoryDomainList(domains);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(MemoryDomains);
		}
	}
}
