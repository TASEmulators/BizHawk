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
		partial class PPU
		{
			void ppubus_write(int addr, byte value)
			{
				nes.board.WritePPU(addr, value);
			}

			byte ppubus_read(int addr)
			{
				return nes.board.ReadPPU(addr);
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
			bool PAL = false;
			bool SPRITELIMIT = true;
			const int MAXSPRITES = 8;

	
		}
	}
}
