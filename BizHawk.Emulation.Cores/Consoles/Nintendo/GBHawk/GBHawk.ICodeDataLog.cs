using System;
using System.IO;
using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.Components.LR35902;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	public partial class GBHawk : ICodeDataLogger
	{
		private ICodeDataLog _cdl;

		public void SetCDL(ICodeDataLog cdl)
		{
			_cdl = cdl;
			if (cdl == null)
				this.cpu.CDLCallback = null;
			else this.cpu.CDLCallback = DoCDL;
		}

		public void NewCDL(ICodeDataLog cdl)
		{
			cdl["ROM"] = new byte[MemoryDomains["ROM"].Size];
			cdl["HRAM"] = new byte[MemoryDomains["Zero Page RAM"].Size];

			cdl["WRAM"] = new byte[MemoryDomains["Main RAM"].Size];

			if (MemoryDomains.Has("Cart RAM"))
			{
				cdl["CartRAM"] = new byte[MemoryDomains["Cart RAM"].Size];
			}

			cdl.SubType = "GB";
			cdl.SubVer = 0;
		}

		[FeatureNotImplemented]
		void ICodeDataLogger.DisassembleCDL(Stream s, ICodeDataLog cdl)
		{
		}

		public void DoCDL2(LR35902.eCDLog_Flags flags, string type, int cdladdr)
		{
			if (type == null) return;
			_cdl[type][cdladdr] |= (byte)flags;
		}

		public void DoCDL(ushort addr, LR35902.eCDLog_Flags flags)
		{
			MemoryCallbacks.CallReads(addr, "System Bus");
			addr_access = addr;
			
			if (ppu.DMA_start)
			{
				// some of gekkio's tests require these to be accessible during DMA
				if (addr < 0x8000)
				{
					if (ppu.DMA_addr < 0x80)
					{
						return;
					}
					else
					{
						mapper.MapCDL(addr, flags);
						return;
					}
				}
				else if ((addr >= 0xE000) && (addr < 0xF000))
				{
					DoCDL2(flags, "WRAM", addr - 0xE000);
				}
				else if ((addr >= 0xF000) && (addr < 0xFE00))
				{
					DoCDL2(flags, "WRAM", (RAM_Bank * 0x1000) + (addr - 0xF000));
				}
				else if ((addr >= 0xFE00) && (addr < 0xFEA0) && ppu.DMA_OAM_access)
				{
					return;
				}
				else if ((addr >= 0xFF00) && (addr < 0xFF80)) // The game GOAL! Requires Hardware Regs to be accessible
				{
					return;
				}
				else if ((addr >= 0xFF80))
				{
					DoCDL2(flags, "HRAM", addr - 0xFF80);
				}
				
			}
			
			if (addr < 0x900)
			{
				if (addr < 0x100)
				{
					// return Either BIOS ROM or Game ROM
					if ((GB_bios_register & 0x1) == 0)
					{
						return;
					}
					else
					{
						mapper.MapCDL(addr, flags);
						return;
					}
				}
				else if (addr >= 0x200)
				{
					// return Either BIOS ROM or Game ROM
					if (((GB_bios_register & 0x1) == 0) && is_GBC)
					{
						return;
					}
					else
					{
						mapper.MapCDL(addr, flags);
						return;
					}
				}
				else
				{
					mapper.MapCDL(addr, flags);
					return;
				}
			}
			else if (addr < 0x8000)
			{
				mapper.MapCDL(addr, flags);
				return;
			}
			else if (addr < 0xA000)
			{
				return;
			}
			else if (addr < 0xC000)
			{
				mapper.MapCDL(addr, flags);
				return;
			}
			else if (addr < 0xD000)
			{
				return;
			}
			else if (addr < 0xE000)
			{
				DoCDL2(flags, "WRAM", (RAM_Bank * 0x1000) + (addr - 0xD000));
			}
			else if (addr < 0xF000)
			{
				DoCDL2(flags, "WRAM", addr - 0xE000);
			}
			else if (addr < 0xFE00)
			{
				DoCDL2(flags, "WRAM", (RAM_Bank * 0x1000) + (addr - 0xF000));
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
				DoCDL2(flags, "HRAM", addr - 0xFF80);
			}
			else
			{
				return;
			}

		}

	
	}
}