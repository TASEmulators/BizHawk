using BizHawk.Common;


namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper125 : NES.NESBoardBase
	{
		private byte reg;
		private int prg_bank_mask_8k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{

			switch (Cart.board_type)
			{
				case "MAPPER125":
				case "UNIF_UNL-LH32":
					break;
				default:
					return false;
			}

			SetMirrorType(Cart.pad_h, Cart.pad_v);

			prg_bank_mask_8k = Cart.prg_size / 8 - 1;
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("reg", ref reg);
			base.SyncState(ser);
		}

		public override void WriteWRAM(int addr, byte value)
		{
			if (addr == 0)
			{
				reg = value;
			}
		}

		public override void WritePRG(int addr, byte value)
		{
			if ((addr >= 0x4000) && (addr < 0x6000))
				WRAM[addr - 0x4000] = value;
			else
				base.WritePRG(addr, value);
		}

		public override byte ReadPRG(int addr)
		{
			int bank = 0;
			if (addr < 0x2000) { bank = prg_bank_mask_8k - 3; }
			else if (addr < 0x4000) { bank = prg_bank_mask_8k - 2; }
			// for some reason WRAM is mapped to here.
			else if (addr < 0x6000)
			{
				return WRAM[addr - 0x4000];
			}
			else { bank = prg_bank_mask_8k; }

			bank &= prg_bank_mask_8k;
			return ROM[(bank << 13) + (addr & 0x1FFF)];
		}

		public override byte ReadWRAM(int addr)
		{
			return ROM[((reg & prg_bank_mask_8k) << 13) + addr];
		}
	}
}
