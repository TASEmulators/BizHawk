using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo.Boards
{
	//generally mapper66

	//Doraemon
	//Dragon Power
	//Gumshoe
	//Thunder & Lightning
	//Super Mario Bros. + Duck Hunt

	//TODO - bus conflicts

	public class GxROM : NES.NESBoardBase
	{
		//configuraton
		int prg_mask, chr_mask;

		//state
		int prg, chr;

		public override bool Configure(NES.BootGodDB.Cart cart)
		{
			//configure
			switch (cart.board_type)
			{
				case "NES-GNROM":
				case "BANDAI-GNROM":
				case "HVC-GNROM":
				case "NES-MHROM":
					Assert(cart.chr_size == 8 || cart.chr_size == 16 || cart.chr_size == 32);
					BoardInfo.PRG_Size = (cart.board_type == "NES-MHROM" ? 64 : 128);
					BoardInfo.CHR_Size = cart.chr_size;
					break;

				default:
					return false;
			}

			prg_mask = (BoardInfo.PRG_Size/8/2) - 1;
			chr_mask = (BoardInfo.CHR_Size / 8) - 1;
			SetMirrorType(cart.pad_h, cart.pad_v);

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
			chr = ((value & 3) & chr_mask);
			prg = (((value>>4) & 3) & prg_mask);
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