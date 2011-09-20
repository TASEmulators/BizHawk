using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	/*
	 * Life Span: April 1986 - July 1986
PCB Class: SUNSOFT-1
iNES Mapper 184
PRG-ROM: 32KB
PRG-RAM: None
CHR-ROM: 16KB
CHR-RAM: None
Battery is not available
Uses vertical mirroring
No CIC present
Other chips used: Sunsoft-1
	 * 
	 * Games:
	 * Atlantis no Nazo
	 * The Wing of Madoola
	 */

	class Sunsoft1 : NES.NESBoardBase
	{
		int chr_mask;
		int left_piece = 0;
		int right_piece = 3;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "SUNSOFT-1":
					break;
				default:
					return false;
			}
			chr_mask = (Cart.chr_size / 4) - 1;
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}

		public override byte ReadPPU(int addr)
		{

			if (addr < 0x1000)
			{
				return VROM[(addr & 0xFFF) + (left_piece * 0x1000)];
			}
			else if (addr < 0x2000)
			{
				return VROM[(addr & 0xFFF) + (right_piece * 0x1000)];
			}

			return base.ReadPPU(addr);
		}

		public override void WriteWRAM(int addr, byte value)
		{
			left_piece = value & 7 & chr_mask;
			right_piece = (value >> 4) & 7 & chr_mask;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("left_piece", ref left_piece);
			ser.Sync("right_piece", ref right_piece);
		}
	}
}
