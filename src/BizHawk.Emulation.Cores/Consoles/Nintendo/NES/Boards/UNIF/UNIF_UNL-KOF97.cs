﻿namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// adapted from Nestopia src
	internal sealed class UNIF_UNL_KOF97 : MMC3Board_Base
	{
		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "UNIF_UNL-KOF97":
					break;
				default:
					return false;
			}

			BaseSetup();
			return true;
		}

		private byte Unscramble(byte data)
			=> unchecked((byte) (((data >> 1) & 0x01)
				| ((data >> 4) & 0x02)
				| ((data << 2) & 0x04)
				| (data & 0xD8)
				| ((data << 3) & 0x20)));

		public override void WritePrg(int addr, byte value)
		{
			value = Unscramble(value);

			if (addr == 0x1000) // 9000 = 8001
			{
				base.WritePrg(1, value);
			}
			else if (addr == 0x5000) // D000 = C001
			{
				base.WritePrg(0x4001, value);
			}
			else if (addr == 0x7000) // F000 = E001
			{
				base.WritePrg(0x6001, value);
			}
			else
			{
				base.WritePrg(addr, value);
			}
		}
	}
}
