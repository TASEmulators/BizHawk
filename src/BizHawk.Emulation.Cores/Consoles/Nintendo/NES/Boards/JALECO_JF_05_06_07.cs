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
	internal sealed class JALECO_JF_05_06_07 : NesBoardBase
	{
		private int prg_byte_mask;
		private int chr;
		private int chr_mask_8k;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER087":
					AssertPrg(8, 16, 32);
					AssertChr(8, 16, 32);
					AssertVram(0);
					Cart.WramSize = 0;
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
			prg_byte_mask = Cart.PrgSize * 1024 - 1;
			chr_mask_8k = Cart.ChrSize / 8 - 1;
			SetMirrorType(Cart.PadH, Cart.PadV);

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(chr), ref chr);
		}

		public override void WriteWram(int addr, byte value)
		{
			// 2 bits, but flipped
			chr = value << 1 & 2 | value >> 1 & 1;
			chr &= chr_mask_8k;
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
				return Vrom[addr | chr << 13];
			return base.ReadPpu(addr);
		}

		public override byte ReadPrg(int addr)
		{
			return Rom[addr & prg_byte_mask];
		}
	}
}
