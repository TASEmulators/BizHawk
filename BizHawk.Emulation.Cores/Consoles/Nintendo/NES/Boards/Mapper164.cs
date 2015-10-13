using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper164 : NES.NESBoardBase 
	{
		// http://wiki.nesdev.com/w/index.php/INES_Mapper_164

		private int _prgHigh;
		private int _prgLow;

		private int prg_bank_mask_32k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER164":
					break;
				default:
					return false;
			}

			_prgLow = 0xFF;
			prg_bank_mask_32k = Cart.prg_size / 32 - 1;
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}

		public override void WriteEXP(int addr, byte value)
		{
			addr = (addr + 0x4000) & 0x7300;
			switch (addr)
			{
				case 0x5000:
					_prgLow = value;
					break;
				case 0x5100:
					_prgHigh = value;
					break;
			}
		}

		public override byte ReadPRG(int addr)
		{
			int bank = (_prgHigh << 4) | (_prgLow & 0xF);
			bank &= prg_bank_mask_32k;
			return ROM[(bank * 0x8000) + (addr & 0x7FFF)];
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prgHigh", ref _prgHigh);
			ser.Sync("prgLow", ref _prgLow);
		}

		public override void NESSoftReset()
		{
			_prgHigh = 0xFF;
			base.NESSoftReset();
		}
	}
}
