using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// this is an internal testing thing, not really for using

	internal sealed class GameGenie : NesBoardBase
	{
		static byte[] PatternTables = new byte[256];

		static GameGenie()
		{
			for (int addr = 0; addr < 256; addr++)
			{
				byte d = 0;
				if (addr.Bit(2))
				{
					if (addr.Bit(4)) d |= 16;
					if (addr.Bit(5)) d |= 1;
				}
				else
				{
					if (addr.Bit(6)) d |= 16;
					if (addr.Bit(7)) d |= 1;
				}
				d |= (byte)(d << 1);
				d |= (byte)(d << 2);
				PatternTables[addr] = d;
			}
		}

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "CAMERICA-GAMEGENIE":
					break;
				case "UNIF_CAMERICA-GAMEGENIE":
					break;
				default:
					return false;
			}
			AssertChr(0); AssertPrg(4);
			Cart.WramSize = 0;
			Cart.VramSize = 0;

			SetMirroring(0, 0, 0, 0);

			return true;
		}

		public override byte ReadPrg(int addr)
		{
			if (addr < 0x4000)
				return NES.DB;
			else
				return Rom[addr & 0xfff];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr >= 0x2000)
				return base.ReadPpu(addr);
			else
				return PatternTables[addr & 0xff];
		}

		public override void WritePrg(int addr, byte value)
		{
			NES.LogLine("{0:x4}<={1:x2}", addr + 0x8000, value);
		}
	}
}
