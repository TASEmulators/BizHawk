﻿using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper162 : NesBoardBase
	{
		private byte[] reg = new byte[8];
		private int prg_bank_mask_32k;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER162":
					break;
				case "UNIF_UNL-FS304":
					AssertChr(0);
					AssertPrg(512, 1024, 2048, 4096);
					Cart.VramSize = 8;
					Cart.WramSize = 8;
					Cart.WramBattery = true;
					break;
				default:
					return false;
			}

			prg_bank_mask_32k = Cart.PrgSize / 32 - 1;

			reg[0] = 3;
			reg[3] = 7;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("regs", ref reg, false);
			base.SyncState(ser);
		}

		public override void WriteExp(int addr, byte value)
		{
			if (addr >= 0x1000)
			{
				reg[(addr >> 8) & 3] = value;
			}
			else
			{
				base.WriteExp(addr, value);
			}
		}

		public override byte ReadPrg(int addr)
		{
			int bank = 0;
			switch (reg[3] & 7)
			{
				case 0:
				case 2:
					bank = (reg[0] & 0xc) | (reg[1] & 2) | ((reg[2] & 0xf) << 4);
					break;
				case 1:
				case 3:
					bank = (reg[0] & 0xc) | (reg[2] & 0xf) << 4;
					break;
				case 4:
				case 6:
					bank = (reg[0] & 0xe) | ((reg[1] >> 1) & 1) | ((reg[2] & 0xf) << 4);
					break;
				case 5:
				case 7:
					bank = (reg[0] & 0xf) | ((reg[2] & 0xf) << 4);
					break;
			}

			return Rom[((bank & prg_bank_mask_32k) << 15) + addr];
		}
	}
}
