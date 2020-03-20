using BizHawk.Common;

// https://wiki.nesdev.com/w/index.php/INES_Mapper_201
namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper201 : NesBoardBase
	{
		public int reg;
		public int prg_bank_mask_32k;
		public int chr_bank_mask_8k;
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER201":
					break;
				default:
					return false;
			}
			SetMirrorType(EMirrorType.Vertical);
			prg_bank_mask_32k = Cart.prg_size / 32 - 1;
			chr_bank_mask_8k = Cart.chr_size / 8 - 1;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(reg), ref reg);
			base.SyncState(ser);
		}

		public override void WritePrg(int addr, byte value)
		{
			if ((addr & 0x08) > 0)
			{
				reg = addr & 0x03;
			}
			else
			{
				reg = 0;
			}
		}

		public override byte ReadPrg(int addr)
		{
			return Rom[((reg & prg_bank_mask_32k) * 0x8000) + addr];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				return Vrom[((reg & chr_bank_mask_8k) * 0x2000) + addr];
			}
			return base.ReadPpu(addr);
		}
	}
}
