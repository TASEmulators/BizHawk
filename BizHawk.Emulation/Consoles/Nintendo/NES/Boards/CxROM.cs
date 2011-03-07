using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo.Boards
{
	//generally mapper3

	//Solomon's Key
	//Arkanoid
	//Arkista's Ring
	//Bump 'n' Jump
	//Cybernoid

	public class CxROM : NES.NESBoardBase
	{
		//configuration
		int prg_mask,chr_mask;
		bool bus_conflict;

		//state
		int chr;

		public override bool Configure(NES.BootGodDB.Cart cart)
		{
			//configure
			switch (cart.board_type)
			{
				case "NES-CNROM":
				case "HVC-CNROM":
					Assert(cart.prg_size == 16 || cart.prg_size == 32);
					Assert(cart.chr_size == 16 || cart.chr_size == 32);
					BoardInfo.PRG_Size = cart.prg_size;
					BoardInfo.CHR_Size = cart.chr_size;
					break;

				default:
					return false;

			}
			prg_mask = (BoardInfo.PRG_Size / 16) - 1;
			chr_mask = (BoardInfo.CHR_Size / 8) - 1;
			SetMirrorType(cart.pad_h, cart.pad_v);
			bus_conflict = true;

			//validate
			Assert(cart.prg_size == BoardInfo.PRG_Size);
			Assert(cart.chr_size == BoardInfo.CHR_Size);

			return true;
		}
		
		public override void WritePRG(int addr, byte value)
		{
			if (bus_conflict) value = HandleNormalPRGConflict(addr,value);
			chr = value&chr_mask;
			//Console.WriteLine("at {0}, set chr={1}", NES.ppu.ppur.status.sl, chr);
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VROM[addr + (chr<<13)];
			}
			else return base.ReadPPU(addr);
		}

		public override void SaveStateBinary(BinaryWriter bw)
		{
			base.SaveStateBinary(bw);
			bw.Write(chr);
		}

		public override void LoadStateBinary(BinaryReader br)
		{
			base.LoadStateBinary(br);
			chr = br.ReadInt32();
		}
	}
}