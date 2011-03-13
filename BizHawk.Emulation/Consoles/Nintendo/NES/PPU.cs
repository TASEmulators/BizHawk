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
			public void ppubus_write(int addr, byte value)
			{
				nes.board.WritePPU(addr, value);
			}

			//when the ppu issues a read it goes through here and into the game board
			public byte ppubus_read(int addr)
			{
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

			public void SaveStateBinary(BinaryWriter bw)
			{
				bw.Write(ppudead);
				bw.Write(idleSynch);
				bw.Write((bool)Reg2002_objoverflow);
				bw.Write((bool)Reg2002_objhit);
				bw.Write((bool)Reg2002_vblank_active);
				bw.Write(PPUGenLatch);
				bw.Write(reg_2000.Value);
				bw.Write(reg_2001.Value);
				bw.Write(reg_2003);
				Util.WriteByteBuffer(bw, OAM);
				Util.WriteByteBuffer(bw, PALRAM);
				bw.Write(vtoggle);
				bw.Write(VRAMBuffer);
				ppur.SaveStateBinary(bw);
				bw.Write(xbuf);
			}

			public void LoadStateBinary(BinaryReader br)
			{
				ppudead = br.ReadInt32();
				idleSynch = br.ReadBoolean();
				Reg2002_objoverflow = br.ReadBit();
				Reg2002_objhit = br.ReadBit();
				Reg2002_vblank_active = br.ReadBit();
				PPUGenLatch = br.ReadByte();
				reg_2000.Value = br.ReadByte();
				reg_2001.Value = br.ReadByte();
				reg_2003 = br.ReadByte();
				OAM = Util.ReadByteBuffer(br,false);
				PALRAM = Util.ReadByteBuffer(br, false);
				vtoggle = br.ReadBoolean();
				VRAMBuffer = br.ReadByte();
				ppur.LoadStateBinary(br);
				xbuf = br.ReadShorts(xbuf.Length);
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
