using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper197 : MMC3Board_Base
	{
		public override bool Configure(EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.BoardType)
			{
				case "MAPPER197":
					break;
				default:
					return false;
			}
			int num_prg_banks = Cart.PrgSize / 8;
			prg_mask = num_prg_banks - 1;

			int num_chr_banks = (Cart.ChrSize);
			chr_mask = num_chr_banks - 1;

			mmc3 = new Mapper197_MMC3(this, num_prg_banks);
			SetMirrorType(EMirrorType.Vertical);
			return true;
		}
	}

	internal sealed class Mapper197_MMC3 : MMC3
	{
		//This board has 512k CHR ROM, so the ByteBuffer in the base class deosn't suffice.
		public int[] chr_regs_1k_512 = new int[8];

		public Mapper197_MMC3(NesBoardBase board, int num_prg_banks) : base(board, num_prg_banks)
		{
			
		}

		public override void Sync()
		{
			base.Sync();
			int chr_left = regs[0] << 1;
			int chr_right_upper = regs[2] << 1;
			int chr_right_lower = regs[3] << 1;

			for (var i = 0; i < 4; i++)
			{
				chr_regs_1k_512[i] = chr_left | i;
			}

			for (var i = 0; i < 2; i++)
			{
				chr_regs_1k_512[4 | i] = chr_right_upper | i;
				chr_regs_1k_512[6 | i] = chr_right_lower | i;
			}

		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(chr_regs_1k_512), ref chr_regs_1k_512, false);
		}

		public override int Get_CHRBank_1K(int addr)
		{
			int bank_1k = addr >> 10;
			bank_1k = chr_regs_1k_512[bank_1k];
			return bank_1k;
		}
	}
}
