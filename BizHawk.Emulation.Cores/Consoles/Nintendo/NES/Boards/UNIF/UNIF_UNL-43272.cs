using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class UNIF_UNL_43272 : NES.NESBoardBase
	{
		private int latche;

		public override bool Configure(NES.EDetectionOrigin origin)
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
			ser.Sync("latche", ref latche);
			base.SyncState(ser);
		}

		public override void WritePRG(int addr, byte value)
		{
			latche = addr & 65535;
		}

		public override byte ReadPRG(int addr)
		{
			int bank = (latche & 0x38) >> 3;
			return ROM[(bank << 15) + addr];
		}
	}
}
