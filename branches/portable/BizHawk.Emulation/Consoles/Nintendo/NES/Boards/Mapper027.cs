using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	// World Hero (Unl)
	// 8k banked prgrom, 1k banked chrrom, scanline counter
	// tried to copy behavior from fceux, but it doesn't work
	public class Mapper027 : NES.NESBoardBase
	{
		// state
		int[] chr = new int[8];
		int[] prg = new int[4];
		int prglatch;
		int irqlatch;
		int irqstat;
		int irqcount;

		// config
		int chr_mask;
		int prg_mask;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER027":
					break;
				default:
					return false;
			}
			AssertPrg(128);
			AssertChr(512);
			AssertVram(0);
			AssertWram(0);
			prg_mask = Cart.prg_size / 8 - 1;
			chr_mask = Cart.chr_size / 1 - 1;
			prg[3] = prg_mask;
			return true;
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[addr & 0x1fff | prg[addr >> 13] << 13];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
				return VROM[addr & 0x3ff | chr[addr >> 10] << 10];
			else
				return base.ReadPPU(addr);
		}

		public override void WritePRG(int addr, byte value)
		{
			addr &= 0xf00f;
			//if (true)
			//	Console.WriteLine("{0:x4}:{1:x2}", addr + 0x8000, value);
			if (addr >= 0x3000 && addr <= 0x6003)
			{
				int regnum = (addr >> 12) + 1 & 3;
				regnum = regnum << 1 | (addr & 2) >> 1;
				if ((addr & 1) != 0)
				{
					chr[regnum] &= 0x00f;
					chr[regnum] |= value << 4 & chr_mask;
				}
				else
				{
					chr[regnum] &= 0x1f0;
					chr[regnum] |= value & 0xf & chr_mask;
				}
				return;
			}

			switch (addr)
			{
				case 0x0000:
					prg[prglatch] = value & prg_mask;
					break;
				case 0x1000:
					switch (value & 3)
					{
						case 0: SetMirrorType(EMirrorType.Vertical); break;
						case 1: SetMirrorType(EMirrorType.Horizontal); break;
						case 2: SetMirrorType(EMirrorType.OneScreenA); break;
						case 3: SetMirrorType(EMirrorType.OneScreenB); break;
					}
					prglatch = value & 2; // in fceux, this runs because of a lack of case break.  bug?
					break;
				case 0x1002:
					prglatch = value & 2;
					break;
				case 0x2000:
					prg[1] = value & prg_mask;
					break;
				case 0x7000:
					irqlatch &= 0xf0;
					irqlatch |= value & 0x0f;
					break;
				case 0x7001:
					irqlatch &= 0x0f;
					irqlatch |= value << 4 & 0xf0;
					break;
				case 0x7002:
					irqstat = value & 3;
					if ((irqstat & 2) != 0)
						irqcount = irqlatch - 1;
					break;
				case 0x7003:
					irqstat = irqstat << 1 & 2 | irqstat & 1;
					IRQSignal = false;
					break;
			}


		}

		// irq timing is entirely a guess; this bit improvised from ExROM
		public override void ClockPPU()
		{
			if (NES.ppu.ppur.status.cycle != 336)
				return;
			if (!NES.ppu.reg_2001.PPUON)
				return;

			int sl = NES.ppu.ppur.status.sl + 1;

			if (sl >= 241)
				return;
			hblanktrigger();
		}

		void hblanktrigger()
		{
			if ((irqstat & 2) != 0)
			{
				if (irqcount == 255)
				{
					IRQSignal = true;
					irqcount = irqlatch + 1;
					Console.WriteLine("Raise");
				}
				else
					irqcount++;
			}
		}


	}
}
