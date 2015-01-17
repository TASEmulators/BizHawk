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

			if (!(Board is FDS) && Board.SaveRam != null)
			{
				var BatteryRam = new MemoryDomain("Battery RAM", Board.SaveRam.Length, MemoryDomain.Endian.Little,
					addr => Board.SaveRam[addr], (addr, value) => Board.SaveRam[addr] = value);
				domains.Add(BatteryRam);
			}

			var PRGROM = new MemoryDomain("PRG ROM", cart.prg_size * 1024, MemoryDomain.Endian.Little,
				addr => Board.ROM[addr], (addr, value) => Board.ROM[addr] = value);
			domains.Add(PRGROM);

			if (Board.VROM != null)
			{
				var CHRROM = new MemoryDomain("CHR VROM", cart.chr_size * 1024, MemoryDomain.Endian.Little,
					addr => Board.VROM[addr], (addr, value) => Board.VROM[addr] = value);
				domains.Add(CHRROM);
			}

			if (Board.VRAM != null)
			{
				var VRAM = new MemoryDomain("VRAM", Board.VRAM.Length, MemoryDomain.Endian.Little,
					addr => Board.VRAM[addr], (addr, value) => Board.VRAM[addr] = value);
				domains.Add(VRAM);
			}

			if (Board.WRAM != null)
			{
				var WRAM = new MemoryDomain("WRAM", Board.WRAM.Length, MemoryDomain.Endian.Little,
					addr => Board.WRAM[addr], (addr, value) => Board.WRAM[addr] = value);
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

			_memoryDomains = new MemoryDomainList(domains);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(_memoryDomains);
		}
	}
}
