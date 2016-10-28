using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class UNIF_UNL_AC08 : NES.NESBoardBase
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
			ser.Sync("reg", ref reg);
			base.SyncState(ser);
		}

		public override void WriteEXP(int addr, byte value)
		{
			if (addr == 0x25)
			{
				SetMirrorType(value.Bit(3) ? EMirrorType.Horizontal : EMirrorType.Vertical);
			}

			base.WriteEXP(addr, value);
		}

		public override void WritePRG(int addr, byte value)
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

		public override byte ReadWRAM(int addr)
		{
			return ROM[(reg << 13) + addr];
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[0x20000 + addr];
		}
	}
}
