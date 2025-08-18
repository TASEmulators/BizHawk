﻿using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper164 : NesBoardBase
	{
		// http://wiki.nesdev.com/w/index.php/INES_Mapper_164

		private int _prgHigh;
		private int _prgLow;

		private int prg_bank_mask_32k;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER164":
					break;
				default:
					return false;
			}

			_prgLow = 0xFF;
			prg_bank_mask_32k = Cart.PrgSize / 32 - 1;
			SetMirrorType(Cart.PadH, Cart.PadV);
			return true;
		}

		public override void WriteExp(int addr, byte value)
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

		public override byte ReadPrg(int addr)
		{
			int bank = (_prgHigh << 4) | (_prgLow & 0xF);
			bank &= prg_bank_mask_32k;
			return Rom[(bank * 0x8000) + (addr & 0x7FFF)];
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prgHigh", ref _prgHigh);
			ser.Sync("prgLow", ref _prgLow);
		}

		public override void NesSoftReset()
		{
			_prgHigh = 0xFF;
			base.NesSoftReset();
		}
	}
}
