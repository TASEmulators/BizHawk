using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class UNIF_UNL_AC08 : NesBoardBase
	{
		private int reg;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_UNL-AC08":
					break;
				default:
					return false;
			}

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(reg), ref reg);
			base.SyncState(ser);
		}

		public override void WriteExp(int addr, byte value)
		{
			if (addr == 0x25)
			{
				SetMirrorType(value.Bit(3) ? EMirrorType.Horizontal : EMirrorType.Vertical);
			}

			base.WriteExp(addr, value);
		}

		public override void WritePrg(int addr, byte value)
		{
			if (addr == 1)
			{
				reg = (value >> 1) & 0x0F;
			}
			else
			{
				reg = value & 0x0F;
			}
		}

		public override byte ReadWram(int addr)
		{
			return Rom[(reg << 13) + addr];
		}

		public override byte ReadPrg(int addr)
		{
			return Rom[0x20000 + addr];
		}
	}
}
