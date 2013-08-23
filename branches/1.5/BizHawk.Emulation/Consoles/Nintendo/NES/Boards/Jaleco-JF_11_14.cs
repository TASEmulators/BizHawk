namespace BizHawk.Emulation.Consoles.Nintendo
{
	/*
	 * Life Span: October 1986 - April 1987
PCB Class: Jaleco-JF-11
		   Jaleco-JF-14
iNES Mapper 140

JF-11
PRG-ROM: 128kb
CHR-ROM: 32kb
Battery is not available
Uses vertical mirroring
No CIC present
Other chips used: Sunsoft-1
	 * 
	 * Games:
	 * Mississippi Satsujin Jiken (J)
	 * Bio Senshi Dan - Increaser Tono Tatakai [allegedly; but it does not work]
	 */

	class Jaleco_JF_11_14 : NES.NESBoardBase
	{
		int chr, prg;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "MAPPER140":
					break;
				case "JALECO-JF-14":
					break;
				case "JALECO-JF-11":
					break;
				default:
					return false;
			}
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}

		public override byte ReadPRG(int addr)
		{
			if (addr < 0x8000)
				return ROM[addr + (prg * 0x8000)];
			else
				return base.ReadPRG(addr);
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
				return VROM[(addr & 0x1FFF) + (chr * 0x2000)];
			else
				return base.ReadPPU(addr);
		}

		public override void WriteWRAM(int addr, byte value)
		{
			prg = (value >> 4) & 3;
			chr = (value & 15);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chr", ref chr);
			ser.Sync("prg", ref prg);
		}
	}
}
