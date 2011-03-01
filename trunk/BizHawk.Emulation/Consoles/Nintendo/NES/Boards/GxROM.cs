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

	//should this be called GNROM? there is no other Gx anything AFAIK..

	public class GxROM : NES.NESBoardBase
	{
		//configuraton
		int prg_mask, chr_mask;

		//state
		int prg, chr;

		public override void Initialize(NES.RomInfo romInfo, NES nes)
		{
			base.Initialize(romInfo, nes);
			Debug.Assert(romInfo.PRG_Size == 2 || romInfo.PRG_Size == 4 || romInfo.PRG_Size == 8);
			//romInfo.CHR_Size == 8 || romInfo.CHR_Size == 16
			Debug.Assert(romInfo.CHR_Size == 2 || romInfo.CHR_Size == 4, "This is unverified behaviour. Please check it (maybe you are playing thunder&lightning; do you have to play far into that game to see missing CHR?");

			prg_mask = (romInfo.PRG_Size/2) - 1;
			chr_mask = romInfo.CHR_Size - 1;
		}
		public override byte ReadPRG(int addr)
		{
			return RomInfo.ROM[addr + (prg<<15)];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return RomInfo.VROM[addr + (chr << 13)];
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