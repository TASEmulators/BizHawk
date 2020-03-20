using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
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

	internal sealed class Jaleco_JF_11_14 : NesBoardBase
	{
		int chr, prg;

		public override bool Configure(EDetectionOrigin origin)
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

		public override byte ReadPrg(int addr)
		{
			if (addr < 0x8000)
				return Rom[addr + (prg * 0x8000)];
			else
				return base.ReadPrg(addr);
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
				return Vrom[(addr & 0x1FFF) + (chr * 0x2000)];
			else
				return base.ReadPpu(addr);
		}

		public override void WriteWram(int addr, byte value)
		{
			prg = (value >> 4) & 3;
			chr = (value & 15);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(chr), ref chr);
			ser.Sync(nameof(prg), ref prg);
		}
	}
}
