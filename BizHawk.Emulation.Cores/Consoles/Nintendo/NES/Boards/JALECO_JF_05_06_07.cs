using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	/*
	PRG-ROM - 32kb/16kb
	CHR-ROM - 16kb
	Mirroring - Vertical
	ines Mapper 87
	
	Example Games:
	--------------------------
	City Connection (J) - JF_05
	Ninja Jajamaru Kun - JF_06
	Argus (J) - JF_07
	*/
	public sealed class JALECO_JF_05_06_07 : NES.NESBoardBase
	{
		int prg_byte_mask;
		int chr;
		int chr_mask_8k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER087":
					AssertPrg(8, 16, 32);
					AssertChr(8, 16, 32);
					AssertVram(0);
					Cart.wram_size = 0;
					break;
				case "JALECO-JF-05":
				case "JALECO-JF-06":
				case "TAITO-74*139/74":
				case "JALECO-JF-07":
				case "JALECO-JF-08":
				case "JALECO-JF-09": // untested
				case "KONAMI-74*139/74":
				case "JALECO-JF-10":
					AssertPrg(16, 32); AssertChr(16, 32); AssertVram(0); AssertWram(0);
					break;
				default:
					return false;
			}
			prg_byte_mask = Cart.prg_size * 1024 - 1;
			chr_mask_8k = Cart.chr_size / 8 - 1;
			SetMirrorType(Cart.pad_h, Cart.pad_v);

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chr", ref chr);
		}

		public override void WriteWRAM(int addr, byte value)
		{
			// 2 bits, but flipped
			chr = value << 1 & 2 | value >> 1 & 1;
			chr &= chr_mask_8k;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
				return VROM[addr | chr << 13];
			else
				return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[addr & prg_byte_mask];
		}
	}
}
