using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public class UNIF_DREAMTECH01 : NesBoardBase
	{
		// Korean Igo (Unl) [U][!]
		private int reg;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_DREAMTECH01":
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
			if (addr == 0x1020)
			{
				reg = value & 0x07;
			}
		}

		public override byte ReadPrg(int addr)
		{
			int bank = addr < 0x4000 ? reg : 8;
			return Rom[(bank << 14) + (addr & 0x3FFF)];
		}
	}
}
