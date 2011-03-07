using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo.Boards
{
	//mapper 11

	//Crystal Mines
	//Metal Fighter

	public class Discrete_74x377 : NES.NESBoardBase
	{
		//configuration
		int prg_mask, chr_mask;
		bool bus_conflict = true;

		//state
		int prg, chr;

		public override bool Configure(NES.BootGodDB.Cart cart)
		{
			switch (cart.board_type)
			{
				case "COLORDREAMS-74*377":
					Assert(cart.prg_size == 32 || cart.prg_size == 64 || cart.prg_size == 128);
					Assert(cart.chr_size == 16 || cart.chr_size == 32 || cart.chr_size == 64 || cart.chr_size == 128);
					BoardInfo.PRG_Size = cart.prg_size;
					BoardInfo.CHR_Size = cart.chr_size;
					break;

				default:
					return false;
			}
			
			prg_mask = (BoardInfo.PRG_Size/8/2)-1;
			chr_mask = (BoardInfo.CHR_Size / 8 - 1);

			//validate
			Assert(cart.prg_size == BoardInfo.PRG_Size);
			Assert(cart.chr_size == BoardInfo.CHR_Size);

			return true;
		}
		public override byte ReadPRG(int addr)
		{
			return ROM[addr + (prg<<15)];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VROM[addr + (chr << 13)];
			}
			else return base.ReadPPU(addr);
		}

		public override void WritePRG(int addr, byte value)
		{
			if (bus_conflict)
			{
				byte old_value = value;
				value &= ReadPRG(addr);
				Debug.Assert(old_value == value, "Found a test case of Discrete_74x377 bus conflict. please report.");
			}

			prg = (value & 3) & prg_mask;
			chr = (value >> 4) & chr_mask;
		}

		public override void SaveStateBinary(BinaryWriter bw)
		{
			base.SaveStateBinary(bw);
			bw.Write(chr);
			bw.Write(prg);
		}

		public override void LoadStateBinary(BinaryReader br)
		{
			base.LoadStateBinary(br);
			chr = br.ReadInt32();
			prg = br.ReadInt32();
		}
	}
}