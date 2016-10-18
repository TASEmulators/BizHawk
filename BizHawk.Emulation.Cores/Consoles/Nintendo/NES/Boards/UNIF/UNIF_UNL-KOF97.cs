using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// adapted from Nestopia src
	public sealed class UNIF_UNL_KOF97 : MMC3Board_Base
	{
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
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
		{
			return (byte)
			(
				(data >> 1 & 0x01) |
				(data >> 4 & 0x02) |
				(data << 2 & 0x04) |
				(data >> 0 & 0xD8) |
				(data << 3 & 0x20)
			);
		}

		public override void WritePRG(int addr, byte value)
		{
			if (addr < 0x1000) // 0x8000 - 0x8FFF
			{
				base.WritePRG(addr & 1, Unscramble(value));
			}

			else if (addr == 0x1000) // 9000 = 8001
			{
				base.WritePRG(1, Unscramble(value));
			}

			else if (addr == 0x2000) // A000 = 8000)
			{
				base.WritePRG(0, Unscramble(value));
			}

			else if (addr == 0x5000) // D000 = C001
			{
				base.WritePRG(0x4001, Unscramble(value));
			}

			else if (addr >= 0x6000 && addr < 0x7000) // 0xE0000 - 0xEFFF
			{
				base.WritePRG(addr & 1, Unscramble(value));
			}

			else if (addr == 0x7000) // F000
			{
				base.WritePRG(0x6001, Unscramble(value));
			}

			else
			{
				base.WritePRG(addr, value);
			}
		}
	}
}
