using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.ColecoVision
{
	public partial class ColecoVision : IMemoryDomains
	{
		public MemoryDomainList MemoryDomains
		{
			get { return memoryDomains; }
		}

		private MemoryDomainList memoryDomains;

		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>(3);
			var MainMemoryDomain = new MemoryDomain("Main RAM", Ram.Length, MemoryDomain.Endian.Little,
				addr => Ram[addr],
				(addr, value) => Ram[addr] = value);
			var VRamDomain = new MemoryDomain("Video RAM", VDP.VRAM.Length, MemoryDomain.Endian.Little,
				addr => VDP.VRAM[addr],
				(addr, value) => VDP.VRAM[addr] = value);
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
			domains.Add(SystemBusDomain);
			memoryDomains = new MemoryDomainList(domains);
		}
	}
}
