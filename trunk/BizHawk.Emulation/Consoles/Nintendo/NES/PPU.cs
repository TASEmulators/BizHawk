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
            public MemoryDomain.FreezeData[] ppubus_freeze = new MemoryDomain.FreezeData[16384];

            //when the ppu issues a write it goes through here and into the game board
			public void ppubus_write(int addr, byte value)
			{
				nes.board.WritePPU(addr, value);
			}

			//when the ppu issues a read it goes through here and into the game board
			public byte ppubus_read(int addr)
			{
				//apply freeze
                if (ppubus_freeze[addr].IsFrozen)
                    return ppubus_freeze[addr].value;
                else
                    return nes.board.ReadPPU(addr);
			}

			//debug tools peek into the ppu through this
			public byte ppubus_peek(int addr)
			{
				return nes.board.PeekPPU(addr);
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

			//state
			int ppudead; //measured in frames
			bool idleSynch;

			public void SyncState(Serializer ser)
			{
				ser.Sync("ppudead", ref ppudead);
				ser.Sync("idleSynch", ref idleSynch);
				ser.Sync("Reg2002_objoverflow", ref Reg2002_objoverflow);
				ser.Sync("Reg2002_objhit", ref Reg2002_objhit);
				ser.Sync("Reg2002_vblank_active", ref Reg2002_vblank_active);
				ser.Sync("PPUGenLatch", ref PPUGenLatch);
				ser.Sync("reg_2003", ref reg_2003);
				ser.Sync("OAM", ref OAM, false);
				ser.Sync("PALRAM", ref PALRAM, false);
				ser.Sync("vtoggle", ref vtoggle);
				ser.Sync("VRAMBuffer", ref VRAMBuffer);
				ppur.SyncState(ser);

				if(ser.IsText)
					ser.Sync("xbuf", ref xbuf, false);

				byte temp;

				temp = reg_2000.Value; ser.Sync("reg_2000.Value", ref temp); reg_2000.Value = temp;
				temp = reg_2001.Value; ser.Sync("reg_2001.Value", ref temp); reg_2001.Value = temp;
			}

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

				//DON'T LIKE THIS....
				ppur.status.cycle += x;
				if (ppur.status.cycle > ppur.status.end_cycle)
					ppur.status.cycle -= ppur.status.end_cycle;
				
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
