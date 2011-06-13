using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	//mapper 011

	//Crystal Mines
	//Metal Fighter

	public class Discrete_74x377 : NES.NESBoardBase
	{
		//configuration
		int prg_mask, chr_mask;
		bool bus_conflict = true;

		//state
		int prg, chr;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "Discrete_74x377-FLEX":
					break;
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

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chr", ref chr);
			ser.Sync("prg", ref prg);
		}

	}
}