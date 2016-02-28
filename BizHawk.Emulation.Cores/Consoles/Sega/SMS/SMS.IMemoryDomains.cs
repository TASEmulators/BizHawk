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
			var MainMemoryDomain = new MemoryDomain("Main RAM", SystemRam.Length, MemoryDomain.Endian.Little,
				addr => SystemRam[addr],
				(addr, value) => SystemRam[addr] = value);
			var VRamDomain = new MemoryDomain("Video RAM", Vdp.VRAM.Length, MemoryDomain.Endian.Little,
				addr => Vdp.VRAM[addr],
				(addr, value) => Vdp.VRAM[addr] = value);

			var ROMDomain = new MemoryDomain("ROM", RomData.Length, MemoryDomain.Endian.Little,
				addr => RomData[addr],
				(addr, value) => RomData[addr] = value);

			var SystemBusDomain = new MemoryDomain("System Bus", 0x10000, MemoryDomain.Endian.Little,
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
				});

			domains.Add(MainMemoryDomain);
			domains.Add(VRamDomain);
			domains.Add(ROMDomain);
			domains.Add(SystemBusDomain);

			if (SaveRAM != null)
			{
				var SaveRamDomain = new MemoryDomain("Save RAM", SaveRAM.Length, MemoryDomain.Endian.Little,
					addr => SaveRAM[addr],
					(addr, value) => { SaveRAM[addr] = value; SaveRamModified = true; });
				domains.Add(SaveRamDomain);
			}

			if (ExtRam != null)
			{
				var ExtRamDomain = new MemoryDomain("Cart (Volatile) RAM", ExtRam.Length, MemoryDomain.Endian.Little,
					addr => ExtRam[addr],
					(addr, value) => { ExtRam[addr] = value; });
				domains.Add(ExtRamDomain);
			}

			MemoryDomains = new MemoryDomainList(domains);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(MemoryDomains);
		}
	}
}
