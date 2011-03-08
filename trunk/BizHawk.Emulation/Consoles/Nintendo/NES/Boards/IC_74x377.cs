using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
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

		public override bool Configure()
		{
			switch (Cart.board_type)
			{
				case "COLORDREAMS-74*377":
					AssertPrg(32,64,128); AssertChr(16,32,64,128); AssertVram(0); AssertWram(0);
					break;

				default:
					return false;
			}
			
			prg_mask = (Cart.prg_size/8/2)-1;
			chr_mask = (Cart.chr_size / 8 - 1);

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