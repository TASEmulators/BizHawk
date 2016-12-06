using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class UNIF_UNL_KS7013B : NES.NESBoardBase
	{
		private byte reg;
		private int prg_mask_16k;

		public override bool Configure(NES.EDetectionOrigin origin)
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
			ser.Sync("reg", ref reg);
			base.SyncState(ser);
		}

		public override void WriteWRAM(int addr, byte value)
		{
			reg = value;
		}

		public override void WritePRG(int addr, byte value)
		{
			SetMirrorType(value.Bit(0) ? EMirrorType.Horizontal : EMirrorType.Vertical);
		}

		public override byte ReadPRG(int addr)
		{
			if (addr < 0x4000)
			{
				return ROM[((reg & prg_mask_16k) << 14) + (addr & 0x3FFF)];
			}

			return ROM[(prg_mask_16k << 14) + (addr & 0x3FFF)];
		}
	}
}
