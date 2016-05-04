using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.ColecoVision
{
	public partial class ColecoVision
	{
		private MemoryDomainList memoryDomains;

		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>(3);
			var MainMemoryDomain = new MemoryDomainByteArray("Main RAM", MemoryDomain.Endian.Little, Ram, true, 1);
			var VRamDomain = new MemoryDomainByteArray("Video RAM", MemoryDomain.Endian.Little, VDP.VRAM, true, 1);
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
			domains.Add(SystemBusDomain);
			memoryDomains = new MemoryDomainList(domains);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(memoryDomains);
		}
	}
}
