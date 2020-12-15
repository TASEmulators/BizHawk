using BizHawk.Common;

// https://wiki.nesdev.com/w/index.php/INES_Mapper_203
namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper203 : NesBoardBase
	{
		private int prg_reg_16k, chr_reg_8k;
		private int prg_bank_mask_16k;
		private int chr_bank_mask_8k;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER203":
					break;
				default:
					return false;
			}

			prg_bank_mask_16k = Cart.PrgSize / 16 - 1;
			chr_bank_mask_8k = Cart.ChrSize / 8 - 1;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(prg_reg_16k), ref prg_reg_16k);
			ser.Sync(nameof(chr_reg_8k), ref chr_reg_8k);
			base.SyncState(ser);
		}

		public override void WritePrg(int addr, byte value)
		{
			prg_reg_16k = (value >> 2) & prg_bank_mask_16k;
			chr_reg_8k = (value & 0x03) & chr_bank_mask_8k;
		}

		public override byte ReadPrg(int addr)
		{
			return Rom[(prg_reg_16k * 0x4000) + (addr & 0x3FFF)];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				return Vrom[(chr_reg_8k * 0x2000) + addr];
			}
			return base.ReadPpu(addr);
		}
	}
}
