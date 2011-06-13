using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	//generally mapper3

	//Solomon's Key
	//Arkanoid
	//Arkista's Ring
	//Bump 'n' Jump
	//Cybernoid

	public class CNROM : NES.NESBoardBase
	{
		//configuration
		int prg_mask,chr_mask;
		bool bus_conflict;

		//state
		int chr;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "NES-CNROM": //adventure island
				case "HVC-CNROM":
					AssertPrg(16, 32); AssertChr(8,16,32);
					break;

				default:
					return false;

			}
			prg_mask = (Cart.prg_size / 16) - 1;
			chr_mask = (Cart.chr_size / 8) - 1;
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			bus_conflict = true;

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

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chr",ref chr);
		}

	}
}