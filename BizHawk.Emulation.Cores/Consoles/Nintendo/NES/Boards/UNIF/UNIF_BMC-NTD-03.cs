using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class UNIF_BMC_NTD_03 : NES.NESBoardBase
	{
		private int latche;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_BMC-NTD-03":
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
			SetMirrorType(addr.Bit(10) ? EMirrorType.Horizontal : EMirrorType.Vertical);
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank = ((latche & 0x0300) >> 5) | (latche & 7);
				return VROM[(bank << 13) + addr];
			}

			return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			int prg = ((latche >> 10) & 0x1E);

			if (latche.Bit(7))
			{
				int bank = prg | ((latche >> 6) & 1);
				return ROM[(bank << 14) + (addr & 0x3FFF)];
			}
			else
			{
				int bank = prg >> 1;
				return ROM[( bank << 15) + addr];
			}
		}
	}
}
