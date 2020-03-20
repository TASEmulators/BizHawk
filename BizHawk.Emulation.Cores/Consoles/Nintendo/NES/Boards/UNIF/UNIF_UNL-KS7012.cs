using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class UNIF_UNL_KS7012 : NesBoardBase
	{
		private int reg;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_UNL-KS7012":
					break;
				default:
					return false;
			}

			Wram = new byte[8192];
			reg = 0xFF;

			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(reg), ref reg);
			base.SyncState(ser);
		}

		public override void WritePrg(int addr, byte value)
		{
			addr += 0x8000;
			switch (addr)
			{
				case 0xE0A0:
					reg = 0; break;
				case 0xEE36:
					reg = 1; break;
			}
		}

		public override byte ReadPrg(int addr)
		{
			return Rom[((reg & 1) << 15) + addr];
		}
	}
}
