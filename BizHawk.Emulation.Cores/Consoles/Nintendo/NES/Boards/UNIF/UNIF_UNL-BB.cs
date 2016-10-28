using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class UNIF_UNL_BB : NES.NESBoardBase
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
			ser.Sync("reg", ref reg);
			ser.Sync("chr", ref chr);
			base.SyncState(ser);
		}

		public override void WritePRG(int addr, byte value)
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

		public override byte ReadWRAM(int addr)
		{
			return ROM[((reg & 3) << 13) + addr];
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[(prg_mask_32k << 15) + addr];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VROM[((chr & 3) << 13) + addr];
			}

			return base.ReadPPU(addr);
		}
	}
}
