using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	// mmc3 multi, PAL, "Super Mario Bros. / Tetris / Nintendo World Cup"
	public class Mapper037 : MMC3Board_Base
	{
		int exreg;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER037":
				case "PAL-ZZ":
					break;
				default:
					return false;
			}
			AssertPrg(256);
			AssertChr(256);
			BaseSetup();
			//mmc3.MMC3Type = ??
			exreg = 0;
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("exreg", ref exreg);
			base.SyncState(ser);
		}

		public override void WriteWRAM(int addr, byte value)
		{
			if (!mmc3.wram_enable || mmc3.wram_write_protect)
				return;
			exreg = value & 7;
			mmc3.Sync(); // unneeded?
		}

		protected override int Get_CHRBank_1K(int addr)
		{
			return base.Get_CHRBank_1K(addr) | (exreg << 5 & 0x80);
		}

		protected override int Get_PRGBank_8K(int addr)
		{
			return (exreg << 2 & 0x10) | ((exreg & 3) == 3 ? 8 : 0) | (base.Get_PRGBank_8K(addr) & (exreg << 1 | 7));
		}
	}
}
