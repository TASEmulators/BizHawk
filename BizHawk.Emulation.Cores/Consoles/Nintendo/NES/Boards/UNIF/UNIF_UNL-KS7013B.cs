using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class UNIF_UNL_KS7013B : NesBoardBase
	{
		private byte reg;
		private int prg_mask_16k;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_UNL-KS7013B":
					break;
				default:
					return false;
			}

			prg_mask_16k = Cart.prg_size / 16 - 1;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(reg), ref reg);
			base.SyncState(ser);
		}

		public override void WriteWram(int addr, byte value)
		{
			reg = value;
		}

		public override void WritePrg(int addr, byte value)
		{
			SetMirrorType(value.Bit(0) ? EMirrorType.Horizontal : EMirrorType.Vertical);
		}

		public override byte ReadPrg(int addr)
		{
			if (addr < 0x4000)
			{
				return Rom[((reg & prg_mask_16k) << 14) + (addr & 0x3FFF)];
			}

			return Rom[(prg_mask_16k << 14) + (addr & 0x3FFF)];
		}
	}
}
