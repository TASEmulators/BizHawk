﻿using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed partial class NES
	{
		public MemoryDomainList _memoryDomains;
		private bool _memoryDomainsSetup = false;

		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>();
			var RAM = new MemoryDomainByteArray("RAM", MemoryDomain.Endian.Little, ram, true, 1);

			// System bus gets it's own class in order to send compare values to cheats
			var SystemBus = new MemoryDomainDelegateSysBusNES("System Bus", 0x10000, MemoryDomain.Endian.Little,
				addr => PeekMemory((ushort)addr), (addr, value) => ApplySystemBusPoke((int)addr, value), 1,
				(addr, value, compare, comparetype) => ApplyCompareCheat(addr, value, compare, comparetype));

			var PPUBus = new MemoryDomainDelegate("PPU Bus", 0x4000, MemoryDomain.Endian.Little,
				addr => ppu.ppubus_peek((int)addr), (addr, value) => ppu.ppubus_write((int)addr, value), 1);
			var CIRAMdomain = new MemoryDomainByteArray("CIRAM (nametables)", MemoryDomain.Endian.Little, CIRAM, true, 1);
			var OAMdoman = new MemoryDomainByteArray("OAM", MemoryDomain.Endian.Unknown, ppu.OAM, true, 1);

			domains.Add(RAM);
			domains.Add(SystemBus);
			domains.Add(PPUBus);
			domains.Add(CIRAMdomain);
			domains.Add(new MemoryDomainByteArray("PALRAM", MemoryDomain.Endian.Little, ppu.PALRAM, writable: true, wordSize: 1));
			domains.Add(OAMdoman);

			if (Board is not FDS && Board.SaveRam != null)
			{
				var BatteryRam = new MemoryDomainByteArray("Battery RAM", MemoryDomain.Endian.Little, Board.SaveRam, true, 1);
				domains.Add(BatteryRam);
			}

			if (Board.Rom != null)
			{
				var PRGROM = new MemoryDomainByteArray("PRG ROM", MemoryDomain.Endian.Little, Board.Rom, true, 1);
				domains.Add(PRGROM);
			}

			if (Board.Vrom != null)
			{
				var CHRROM = new MemoryDomainByteArray("CHR VROM", MemoryDomain.Endian.Little, Board.Vrom, true, 1);
				domains.Add(CHRROM);
			}

			if (Board.Vram != null)
			{
				var VRAM = new MemoryDomainByteArray("VRAM", MemoryDomain.Endian.Little, Board.Vram, true, 1);
				domains.Add(VRAM);
			}

			if (Board.Wram != null)
			{
				var WRAM = new MemoryDomainByteArray("WRAM", MemoryDomain.Endian.Little, Board.Wram, true, 1);
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
				var src = new MemoryDomainList(domains);
				_memoryDomains.MergeList(src);
			}
		}
	}
}
