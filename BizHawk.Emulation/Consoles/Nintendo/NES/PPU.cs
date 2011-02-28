//http://nesdev.parodius.com/bbs/viewtopic.php?p=4571&sid=db4c7e35316cc5d734606dd02f11dccb

using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using BizHawk.Emulation.CPUs.M6502;


namespace BizHawk.Emulation.Consoles.Nintendo
{
	partial class NES
	{
		public partial class PPU
		{
			//when the ppu issues a write it goes through here and into the game board
			void ppubus_write(int addr, byte value)
			{
				nes.board.WritePPU(addr, value);
			}

			//when the ppu issues a read it goes through here and into the game board
			byte ppubus_read(int addr)
			{
				return nes.board.ReadPPU(addr);
			}

			//boards may not respond to a read, in which case this will get called. please apply mirroring logic beforehand
			public byte ppu_defaultRead(int addr)
			{
				addr &= 0x7FF;
				return NTARAM[addr];
			}

			//boards may not respond to a write, in which case this will get called. please apply mirroring logic beforehand
			public void ppu_defaultWrite(int addr, byte value)
			{
				addr &= 0x7FF;
				NTARAM[addr] = value;
			}

			enum PPUPHASE {
				VBL, BG, OBJ
			};
			PPUPHASE ppuphase;

			NES nes;
			public PPU(NES nes)
			{
				this.nes = nes;
				Reset();
			}

			int ppudead; //measured in frames
			bool idleSynch;

			public void Reset()
			{
				regs_reset();
				ppudead = 2;
				idleSynch = true;
			}

			void TriggerNMI()
			{
				nes.cpu.NMI = true;
			}

			void runppu(int x)
			{
				//pputime+=x;
				//if(cputodo<200) return;

				//DON'T LIKE THIS....
				ppur.status.cycle = (ppur.status.cycle + x) %
				                       ppur.status.end_cycle;
				nes.RunCpu(x);
				//pputime -= cputodo<<2;
			}

			//hack
			public bool PAL = false;
			bool SPRITELIMIT = true;
			const int MAXSPRITES = 8;

	
		}
	}
}
