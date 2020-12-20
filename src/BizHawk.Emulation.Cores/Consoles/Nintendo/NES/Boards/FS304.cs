namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class FS304 : NesBoardBase
	{
		// waixing?
		private int prg;
		private int prg_mask_32k;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "UNIF_UNL-FS304":
					AssertChr(0);
					AssertPrg(512, 1024, 2048, 4096);
					Cart.VramSize = 8;
					Cart.WramSize = 8;
					Cart.WramBattery = true;
					break;
				default:
					return false;
			}

			prg_mask_32k = Cart.PrgSize / 32 - 1;
			SetMirrorType(Cart.PadH, Cart.PadV);
			return true;
		}

		public override void WriteExp(int addr, byte value)
		{
			switch (addr & 0x1300)
			{
				case 0x1000:
					prg &= ~0x0e;
					prg |= value & 0x0e;
					break;
				case 0x1100:
					prg &= ~0x01;
					prg |= value >> 1 & 0x01;
					break;
				case 0x1200:
					prg &= ~0xf0;
					prg |= value << 4 & 0xf0;
					break;
			}
			prg &= prg_mask_32k;
		}

		public override byte ReadPrg(int addr)
		{
			return Rom[addr | prg << 15];
		}
	}
}
