using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper188 : NesBoardBase
	{
		// config
		private int prg_16k_mask;
		// state
		private int prg;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER188":
					AssertVram(8);
					AssertChr(0);
					AssertPrg(128, 256);
					Cart.WramSize = 0;
					break;
				default:
					return false;
			}

			SetMirrorType(Cart.PadH, Cart.PadV);
			prg_16k_mask = Cart.PrgSize / 16 - 1;
			return true;
		}

		public override void WritePrg(int addr, byte value)
		{
			prg = value;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(prg), ref prg);
			base.SyncState(ser);
		}

		public override byte ReadPrg(int addr)
		{
			int bank = prg;
			if (addr >= 0x4000)
				bank = 15;
			bank ^= 8; // bad dumps?
			bank &= prg_16k_mask;
			return Rom[addr & 0x3fff | bank << 14];
		}

		public override byte ReadWram(int addr)
		{
			return 3;
		}
	}
}
