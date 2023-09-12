using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public partial class NES
	{
		public MemoryDomainList _memoryDomains;
		private bool _memoryDomainsSetup = false;

		private void SetupMemoryDomains()
		{
			List<MemoryDomain> domains = new();
			MemoryDomainByteArray RAM = new("RAM", MemoryDomain.Endian.Little, ram, true, 1);

			// System bus gets it's own class in order to send compare values to cheats
			MemoryDomainDelegateSysBusNES SystemBus = new("System Bus", 0x10000, MemoryDomain.Endian.Little,
				addr => PeekMemory((ushort)addr), (addr, value) => ApplySystemBusPoke((int)addr, value), 1,
				(addr, value, compare, comparetype) => ApplyCompareCheat(addr, value, compare, comparetype));

			MemoryDomainDelegate PPUBus = new("PPU Bus", 0x4000, MemoryDomain.Endian.Little,
				addr => ppu.ppubus_peek((int)addr), (addr, value) => ppu.ppubus_write((int)addr, value), 1);
			MemoryDomainByteArray CIRAMdomain = new("CIRAM (nametables)", MemoryDomain.Endian.Little, CIRAM, true, 1);
			MemoryDomainByteArray OAMdoman = new("OAM", MemoryDomain.Endian.Unknown, ppu.OAM, true, 1);

			domains.Add(RAM);
			domains.Add(SystemBus);
			domains.Add(PPUBus);
			domains.Add(CIRAMdomain);
			domains.Add(OAMdoman);

			if (Board is not FDS && Board.SaveRam != null)
			{
				MemoryDomainByteArray BatteryRam = new("Battery RAM", MemoryDomain.Endian.Little, Board.SaveRam, true, 1);
				domains.Add(BatteryRam);
			}

			if (Board.Rom != null)
			{
				MemoryDomainByteArray PRGROM = new("PRG ROM", MemoryDomain.Endian.Little, Board.Rom, true, 1);
				domains.Add(PRGROM);
			}

			if (Board.Vrom != null)
			{
				MemoryDomainByteArray CHRROM = new("CHR VROM", MemoryDomain.Endian.Little, Board.Vrom, true, 1);
				domains.Add(CHRROM);
			}

			if (Board.Vram != null)
			{
				MemoryDomainByteArray VRAM = new("VRAM", MemoryDomain.Endian.Little, Board.Vram, true, 1);
				domains.Add(VRAM);
			}

			if (Board.Wram != null)
			{
				MemoryDomainByteArray WRAM = new("WRAM", MemoryDomain.Endian.Little, Board.Wram, true, 1);
				domains.Add(WRAM);
			}

			// if there were more boards with special ram sets, we'd want to do something more general
			if (Board is FDS)
			{
				domains.Add((Board as FDS).GetDiskPeeker());
			}
			else if (Board is ExROM)
			{
				domains.Add((Board as ExROM).GetExRAM());
			}

			if (!_memoryDomainsSetup)
			{
				_memoryDomains = new MemoryDomainList(domains);
				(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(_memoryDomains);
				_memoryDomainsSetup = true;
			}
			else
			{
				MemoryDomainList src = new(domains);
				_memoryDomains.MergeList(src);
			}
		}
	}
}
