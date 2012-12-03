using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	public class NROM368 : NES.NESBoardBase
	{
		// not even one actual prototype of this pile of shit exists, and
		// there are already two incompatible implementations.  pathetic.
		bool small;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_NROM-368": // ??
					break;
				case "MAPPER000":
					if (Cart.prg_size == 48 || Cart.prg_size == 64)
						break;
					else
						return false;
				default:
					return false;
			}
			AssertPrg(48, 64);
			small = Cart.prg_size == 48;
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}

		public override byte ReadPRG(int addr)
		{
			if (small)
				return ROM[addr + 0x4000];
			else
				return ROM[addr];
		}

		public override byte ReadWRAM(int addr)
		{
			if (small)
				return ROM[addr + 0x2000];
			else
				return ROM[addr + 0xa000];
		}

		public override byte ReadEXP(int addr)
		{
			if (addr < 0x800)
				return NES.DB;
			if (small)
				return ROM[addr];
			else
				return ROM[addr + 0x8000];
		}
	}
}
