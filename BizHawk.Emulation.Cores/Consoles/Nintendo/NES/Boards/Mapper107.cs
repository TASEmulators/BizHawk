using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper107 : NesBoardBase
	{
		//configuration
		int prg_bank_mask_32k, chr_bank_mask_8k;

		//state
		int prg_bank_32k, chr_bank_8k;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg_bank_32k), ref prg_bank_32k);
			ser.Sync(nameof(chr_bank_8k), ref chr_bank_8k);
		}

		public override bool Configure(EDetectionOrigin origin)
		{
			//configure
			switch (Cart.BoardType)
			{
				case "MAPPER107":
					AssertPrg(128); AssertChr(64); AssertWram(8); AssertVram(0); AssertBattery(false);
					break;
				default:
					return false;
			}

			prg_bank_mask_32k = (Cart.PrgSize / 32) - 1;
			chr_bank_mask_8k = (Cart.ChrSize / 8) - 1;

			SetMirrorType(Cart.PadH, Cart.PadV);

			return true;
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				int ofs = addr & ((1 << 13) - 1);
				addr = (chr_bank_8k << 13) | ofs;
				return Vrom[addr];
			}
			else return base.ReadPpu(addr);
		}

		public override byte ReadPrg(int addr)
		{
			int ofs = addr & ((1 << 15) - 1);
			addr = (prg_bank_32k << 15) | ofs;
			return Rom[addr];
		}

		public override void WritePrg(int addr, byte value)
		{
			chr_bank_8k = value;
			prg_bank_32k = value >> 1;
			chr_bank_8k &= chr_bank_mask_8k;
			prg_bank_32k &= prg_bank_mask_32k;
		}

	}
}
