using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// this is an internal testing thing, not really for using

	public class GameGenie : NES.NESBoardBase
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

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "CAMERICA-GAMEGENIE":
					break;
				case "UNIF_CAMERICA-GAMEGENIE":
					break;
				default:
					return false;
			}
			AssertChr(0); AssertPrg(4);
			Cart.wram_size = 0;
			Cart.vram_size = 0;

			SetMirroring(0, 0, 0, 0);

			return true;
		}

		public override byte ReadPRG(int addr)
		{
			if (addr < 0x4000)
				return NES.DB;
			else
				return ROM[addr & 0xfff];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr >= 0x2000)
				return base.ReadPPU(addr);
			else
				return PatternTables[addr & 0xff];
		}

		public override void WritePRG(int addr, byte value)
		{
			NES.LogLine("{0:x4}<={1:x2}", addr + 0x8000, value);
		}
	}
}
