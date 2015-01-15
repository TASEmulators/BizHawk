using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public partial class NES
	{
		private MemoryDomainList _memoryDomains;

		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>();
			var RAM = new MemoryDomain("RAM", 0x800, MemoryDomain.Endian.Little,
				addr => ram[addr], (addr, value) => ram[addr] = value);
			var SystemBus = new MemoryDomain("System Bus", 0x10000, MemoryDomain.Endian.Little,
				addr => PeekMemory((ushort)addr), (addr, value) => ApplySystemBusPoke(addr, value));
			var PPUBus = new MemoryDomain("PPU Bus", 0x4000, MemoryDomain.Endian.Little,
				addr => ppu.ppubus_peek(addr), (addr, value) => ppu.ppubus_write(addr, value));
			var CIRAMdomain = new MemoryDomain("CIRAM (nametables)", 0x800, MemoryDomain.Endian.Little,
				addr => CIRAM[addr], (addr, value) => CIRAM[addr] = value);
			var OAMdoman = new MemoryDomain("OAM", 64 * 4, MemoryDomain.Endian.Unknown,
				addr => ppu.OAM[addr], (addr, value) => ppu.OAM[addr] = value);

			domains.Add(RAM);
			domains.Add(SystemBus);
			domains.Add(PPUBus);
			domains.Add(CIRAMdomain);
			domains.Add(OAMdoman);

			if (!(board is FDS) && board.SaveRam != null)
			{
				var BatteryRam = new MemoryDomain("Battery RAM", board.SaveRam.Length, MemoryDomain.Endian.Little,
					addr => board.SaveRam[addr], (addr, value) => board.SaveRam[addr] = value);
				domains.Add(BatteryRam);
			}

			var PRGROM = new MemoryDomain("PRG ROM", cart.prg_size * 1024, MemoryDomain.Endian.Little,
				addr => board.ROM[addr], (addr, value) => board.ROM[addr] = value);
			domains.Add(PRGROM);

			if (board.VROM != null)
			{
				var CHRROM = new MemoryDomain("CHR VROM", cart.chr_size * 1024, MemoryDomain.Endian.Little,
					addr => board.VROM[addr], (addr, value) => board.VROM[addr] = value);
				domains.Add(CHRROM);
			}

			if (board.VRAM != null)
			{
				var VRAM = new MemoryDomain("VRAM", board.VRAM.Length, MemoryDomain.Endian.Little,
					addr => board.VRAM[addr], (addr, value) => board.VRAM[addr] = value);
				domains.Add(VRAM);
			}

			if (board.WRAM != null)
			{
				var WRAM = new MemoryDomain("WRAM", board.WRAM.Length, MemoryDomain.Endian.Little,
					addr => board.WRAM[addr], (addr, value) => board.WRAM[addr] = value);
				domains.Add(WRAM);
			}

			// if there were more boards with special ram sets, we'd want to do something more general
			if (board is FDS)
			{
				domains.Add((board as FDS).GetDiskPeeker());
			}
			else if (board is ExROM)
			{
				domains.Add((board as ExROM).GetExRAM());
			}

			_memoryDomains = new MemoryDomainList(domains);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(_memoryDomains);
		}
	}
}
