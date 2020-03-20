using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class UNIF_UNL_BB : NesBoardBase
	{
		private byte reg, chr;
		private int prg_mask_32k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_UNL-BB":
					break;
				default:
					return false;
			}

			reg = 0xFF;
			prg_mask_32k = Cart.prg_size / 32 - 1;
			SetMirrorType(Cart.pad_h, Cart.pad_v);

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(reg), ref reg);
			ser.Sync(nameof(chr), ref chr);
			base.SyncState(ser);
		}

		public override void WritePrg(int addr, byte value)
		{
			addr += 0x8000;
			if ((addr & 0x9000) == 0x8000)
			{
				reg = chr = value;
			}
			else
			{
				chr = (byte)(value & 1);
			}
		}

		public override byte ReadWram(int addr)
		{
			return Rom[((reg & 3) << 13) + addr];
		}

		public override byte ReadPrg(int addr)
		{
			return Rom[(prg_mask_32k << 15) + addr];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				return Vrom[((chr & 3) << 13) + addr];
			}

			return base.ReadPpu(addr);
		}
	}
}
