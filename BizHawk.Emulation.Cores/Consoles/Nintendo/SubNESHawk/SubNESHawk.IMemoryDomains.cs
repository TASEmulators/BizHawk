using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SubNESHawk
{
	public partial class SubNESHawk
	{
		private MemoryDomainList _memoryDomains;
		private bool _memoryDomainsSetup = false;

		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>();
			var RAM = new MemoryDomainByteArray("RAM", MemoryDomain.Endian.Little, subnes.ram, true, 1);
			var SystemBus = new MemoryDomainDelegate("System Bus", 0x10000, MemoryDomain.Endian.Little,
				addr => subnes.PeekMemory((ushort)addr), (addr, value) => subnes.ApplySystemBusPoke((int)addr, value), 1);
			var PPUBus = new MemoryDomainDelegate("PPU Bus", 0x4000, MemoryDomain.Endian.Little,
				addr => subnes.ppu.ppubus_peek((int)addr), (addr, value) => subnes.ppu.ppubus_write((int)addr, value), 1);
			var CIRAMdomain = new MemoryDomainByteArray("CIRAM (nametables)", MemoryDomain.Endian.Little, subnes.CIRAM, true, 1);
			var OAMdoman = new MemoryDomainByteArray("OAM", MemoryDomain.Endian.Unknown, subnes.ppu.OAM, true, 1);

			domains.Add(RAM);
			domains.Add(SystemBus);
			domains.Add(PPUBus);
			domains.Add(CIRAMdomain);
			domains.Add(OAMdoman);

			if (!(subnes.Board is NES.FDS) && subnes.Board.SaveRam != null)
			{
				var BatteryRam = new MemoryDomainByteArray("Battery RAM", MemoryDomain.Endian.Little, subnes.Board.SaveRam, true, 1);
				domains.Add(BatteryRam);
			}

			if (subnes.Board.ROM != null)
			{
				var PRGROM = new MemoryDomainByteArray("PRG ROM", MemoryDomain.Endian.Little, subnes.Board.ROM, true, 1);
				domains.Add(PRGROM);
			}

			if (subnes.Board.VROM != null)
			{
				var CHRROM = new MemoryDomainByteArray("CHR VROM", MemoryDomain.Endian.Little, subnes.Board.VROM, true, 1);
				domains.Add(CHRROM);
			}

			if (subnes.Board.VRAM != null)
			{
				var VRAM = new MemoryDomainByteArray("VRAM", MemoryDomain.Endian.Little, subnes.Board.VRAM, true, 1);
				domains.Add(VRAM);
			}

			if (subnes.Board.WRAM != null)
			{
				var WRAM = new MemoryDomainByteArray("WRAM", MemoryDomain.Endian.Little, subnes.Board.WRAM, true, 1);
				domains.Add(WRAM);
			}

			// if there were more boards with special ram sets, we'd want to do something more general
			if (subnes.Board is NES.FDS)
			{
				domains.Add((subnes.Board as NES.FDS).GetDiskPeeker());
			}
			else if (subnes.Board is NES.ExROM)
			{
				domains.Add((subnes.Board as NES.ExROM).GetExRAM());
			}

			if (!_memoryDomainsSetup)
			{
				_memoryDomains = new MemoryDomainList(domains);
				(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(_memoryDomains);
				_memoryDomainsSetup = true;
			}
			else
			{
				var src = new MemoryDomainList(domains);
				_memoryDomains.MergeList(src);
			}
		}
	}
}