using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class UNIF_BMC_810544_C_A1 : NesBoardBase
	{
		private int latche;
		private int prg_mask_32k, prg_mask_16k;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_BMC-810544-C-A1":
					break;
				default:
					return false;
			}

			prg_mask_32k = Cart.prg_size / 32 - 1;
			prg_mask_16k = Cart.prg_size / 16 - 1;

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
			SetMirrorType(addr.Bit(3) ? EMirrorType.Vertical : EMirrorType.Horizontal);
		}

		public override byte ReadPrg(int addr)
		{
			int bank = latche >> 7;
			if (latche.Bit(6))
			{
				return Rom[((bank & prg_mask_32k) << 15) + addr];
			}

			int bank16 = (bank << 1) | ((latche >> 5) & 1);
			bank16 &= prg_mask_16k;
			return Rom[(bank16 << 14) + (addr & 0x3FFF)];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				return Vrom[((latche & 0x0F) << 13) + addr];
			}

			return base.ReadPpu(addr);
		}
	}
}
