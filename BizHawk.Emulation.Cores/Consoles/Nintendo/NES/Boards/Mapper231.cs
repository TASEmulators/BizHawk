using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

// http://wiki.nesdev.com/w/index.php/INES_Mapper_231
namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper231 : NesBoardBase
	{
		public int prg_reg;
		public int prg_bank_mask_16k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER231":
					break;
				default:
					return false;
			}

			prg_bank_mask_16k = Cart.prg_size / 16 - 1;
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(prg_reg), ref prg_reg);
			base.SyncState(ser);
		}

		public override void WritePrg(int addr, byte value)
		{
			if (addr.Bit(7))
			{
				SetMirrorType(EMirrorType.Horizontal);
			}
			else
			{
				SetMirrorType(EMirrorType.Vertical);
			}

			int prg_reg_P = (addr >> 1) & 0xF;
			int prg_reg_L = (addr >> 5) & 1;
			prg_reg = (prg_reg_P<<1) | prg_reg_L;
			prg_reg &= prg_bank_mask_16k;
		}

		public override byte ReadPrg(int addr)
		{
			int bank = prg_reg;
			return Rom[(bank << 14) + addr - 0x4000];
		}
	}
}
