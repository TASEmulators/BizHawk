using System.IO;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.LR35902;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink4x
{
	public partial class GBHawkLink4x : ICodeDataLogger
	{
		private ICodeDataLog _cdl;

		public void SetCDL(ICodeDataLog cdl)
		{
			_cdl = cdl;
			if (cdl == null)
				this.A.cpu.CDLCallback = null;
			else this.A.cpu.CDLCallback = CDLCpuCallback;
		}

		public void NewCDL(ICodeDataLog cdl)
		{
			cdl["ROM"] = new byte[MemoryDomains["ROM A"]!.Size];
			cdl["HRAM"] = new byte[MemoryDomains["Zero Page RAM A"]!.Size];

			cdl["WRAM"] = new byte[MemoryDomains["Main RAM A"]!.Size];

			var found = MemoryDomains["Cart RAM A"];
			if (found is not null) cdl["CartRAM"] = new byte[found.Size];

			cdl.SubType = "GB";
			cdl.SubVer = 0;
		}

		[FeatureNotImplemented]
		public void DisassembleCDL(Stream s, ICodeDataLog cdl)
			=> throw new NotImplementedException();

		public void SetCDL(LR35902.eCDLogMemFlags flags, string type, int cdladdr)
		{
			if (type == null) return;
			byte val = (byte)flags;
			_cdl[type][cdladdr] |= (byte)flags;
		}

		private void CDLCpuCallback(ushort addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x8000)
			{
				//don't record writes to the ROM, it's just noisy
				//NOTE: in principle a mapper could mount a useful resource here, but I doubt it)
				if ((flags & LR35902.eCDLogMemFlags.Write) != 0) return;
			}
			
			if (A.ppu.DMA_bus_control)
			{
				// some of gekkio's tests require these to be accessible during DMA
				if (addr < 0x8000)
				{
					if (A.ppu.DMA_addr < 0x80)
					{
						return;
					}
					else
					{
						A.mapper.MapCDL(addr, flags);
						return;
					}
				}
				else if ((addr >= 0xE000) && (addr < 0xF000))
				{
					SetCDL(flags, "WRAM", addr - 0xE000);
				}
				else if ((addr >= 0xF000) && (addr < 0xFE00))
				{
					SetCDL(flags, "WRAM", (A.RAM_Bank * 0x1000) + (addr - 0xF000));
				}
				else if ((addr >= 0xFE00) && (addr < 0xFEA0) && A.ppu.DMA_OAM_access)
				{
					return;
				}
				else if ((addr >= 0xFF00) && (addr < 0xFF80)) // The game GOAL! Requires Hardware Regs to be accessible
				{
					return;
				}
				else if ((addr >= 0xFF80))
				{
					SetCDL(flags, "HRAM", addr - 0xFF80);
				}
				
			}
			
			if (addr < 0x900)
			{
				if (addr < 0x100)
				{
					// return Either BIOS ROM or Game ROM
					if ((A.GB_bios_register & 0x1) == 0)
					{
						return;
					}
					else
					{
						A.mapper.MapCDL(addr, flags);
						return;
					}
				}
				else if (addr >= 0x200)
				{
					// return Either BIOS ROM or Game ROM
					if (((A.GB_bios_register & 0x1) == 0) && A.is_GBC)
					{
						return;
					}
					else
					{
						A.mapper.MapCDL(addr, flags);
						return;
					}
				}
				else
				{
					A.mapper.MapCDL(addr, flags);
					return;
				}
			}
			else if (addr < 0x8000)
			{
				A.mapper.MapCDL(addr, flags);
				return;
			}
			else if (addr < 0xA000)
			{
				return;
			}
			else if (addr < 0xC000)
			{
				A.mapper.MapCDL(addr, flags);
				return;
			}
			else if (addr < 0xD000)
			{
				return;
			}
			else if (addr < 0xE000)
			{
				SetCDL(flags, "WRAM", (A.RAM_Bank * 0x1000) + (addr - 0xD000));
			}
			else if (addr < 0xF000)
			{
				SetCDL(flags, "WRAM", addr - 0xE000);
			}
			else if (addr < 0xFE00)
			{
				SetCDL(flags, "WRAM", (A.RAM_Bank * 0x1000) + (addr - 0xF000));
			}
			else if (addr < 0xFEA0)
			{
				return;
			}
			else if (addr < 0xFF00)
			{
				return;
			}
			else if (addr < 0xFF80)
			{
				return;
			}
			else if (addr < 0xFFFF)
			{
				SetCDL(flags, "HRAM", addr - 0xFF80);
			}
			else
			{
				return;
			}

		}

	
	}
}