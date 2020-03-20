using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class UNIF_UNL_43272 : NesBoardBase
	{
		private int latche;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_UNL-43272":
					break;
				default:
					return false;
			}

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(latche), ref latche);
			base.SyncState(ser);
		}

		public override void WritePrg(int addr, byte value)
		{
			latche = addr & 65535;
		}

		public override byte ReadPrg(int addr)
		{
			int bank = (latche & 0x38) >> 3;
			return Rom[(bank << 15) + addr];
		}
	}
}
