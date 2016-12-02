using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper162 : NES.NESBoardBase
	{
		private ByteBuffer reg = new ByteBuffer(8);
		private int prg_bank_mask_32k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER162":
				case "UNIF_UNL-FS304":
					break;
				default:
					return false;
			}

			prg_bank_mask_32k = Cart.prg_size / 32 - 1;

			reg[0] = 3;
			reg[3] = 7;

			return true;
		}

		public override void Dispose()
		{
			reg.Dispose();
			base.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("regs", ref reg);
			base.SyncState(ser);
		}

		public override void WriteEXP(int addr, byte value)
		{
			if (addr >= 0x1000)
			{
				reg[(addr >> 8) & 3] = value;
			}
			else
			{
				base.WriteEXP(addr, value);
			}
		}

		public override byte ReadPRG(int addr)
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

			return ROM[((bank & prg_bank_mask_32k) << 15) + addr];
		}
	}
}
