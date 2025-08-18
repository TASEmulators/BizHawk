﻿namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	/*
	Example Games:
	--------------------------
	Quinty (J)
	Namcot Mahjong 3
	Dragon Spirit - Aratanaru Densetsu

	This is the same as Mapper206, with the following exception:
	CHR support is increased to 128KB by connecting PPU's A12 line to the CHR ROM's A16 line.
	For example, mask the CHR ROM 1K bank output from the mapper by $3F, and then OR it with $40 if the PPU address was >= $1000.
	Consequently, CHR is split into two halves. $0xxx can only have CHR from the first 64K, $1xxx can only have CHR from the second 64K.
	*/

	internal sealed class Mapper088 : Namcot108Board_Base
	{
		//configuration
		private int chr_bank_mask_1k;

		public override bool Configure(EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.BoardType)
			{
				case "NAMCOT-3443":
				case "NAMCOT-3433":
				case "MAPPER088":
					break;
				default:
					return false;
			}

			BaseSetup();
			SetMirrorType(Cart.PadH, Cart.PadV);

			chr_bank_mask_1k = Cart.ChrSize - 1;

			return true;
		}

		private int RewireCHR(int addr)
		{
			int bank_1k = mapper.Get_CHRBank_1K(addr);
			bank_1k &= 0x3F;
			if (addr >= 0x1000)
				bank_1k |= 0x40;
			bank_1k &= chr_bank_mask_1k;
			int ofs = addr & ((1 << 10) - 1);
			addr = (bank_1k << 10) + ofs;
			return addr;
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000) return Vrom[RewireCHR(addr)];
			else return base.ReadPpu(addr);
		}
		public override void WritePpu(int addr, byte value)
		{
			if (addr >= 0x2000) base.WritePpu(addr, value);
		}
	}
}
