using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Adapted from 
	public sealed class CamericaGoldenFive : NES.NESBoardBase
	{
		private ByteBuffer regs = new ByteBuffer(2);

		private int prg_bank_mask_16k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER104":
					break;
				default:
					return false;
			}

			prg_bank_mask_16k = Cart.prg_size / 16 - 1;

			regs[1] = 0xF;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("reg", ref regs);
		}

		public override void WritePRG(int addr, byte value)
		{
			if (addr < 0x4000) // 80000
			{
				if ((value & 8) > 0)
				{
					regs[0] = (byte)((value << 4 & 0x70) | (prg_bank_mask_16k & 0x0F));
					regs[1] = (byte)((value << 4 & 0x70) | 0x0F);
				}
			}
			else // C000
			{
				regs[0] = (byte)(prg_bank_mask_16k & 0x70 | (value & 0x0F));
			}
		}

		public override byte ReadPRG(int addr)
		{
			if (addr < 0x4000)
			{
				return ROM[((regs[0] & prg_bank_mask_16k) << 14) + (addr & 0x3FFF)];
			}
			else
			{
				return ROM[((regs[1] & prg_bank_mask_16k) << 14) + (addr & 0x3FFF)];
			}
		}
	}
}
