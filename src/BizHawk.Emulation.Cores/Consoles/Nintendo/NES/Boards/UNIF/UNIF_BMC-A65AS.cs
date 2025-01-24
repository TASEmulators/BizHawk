﻿using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class UNIF_BMC_A65AS : NesBoardBase
	{
		private int _prgReg;
		private bool _isPrg32kMode;

		private int prgMask16k;
		private int prgMask32k;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "UNIF_BMC-A65AS":
					break;
				default:
					return false;
			}

			prgMask16k = Cart.PrgSize / 16 - 1;
			prgMask32k = Cart.PrgSize / 32 - 1;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prgReg", ref _prgReg);
			ser.Sync("isPrg32kMode", ref _isPrg32kMode);
		}

		public override void WritePrg(int addr, byte value)
		{
			_isPrg32kMode = value.Bit(6);
			_prgReg = value;

			// From FCEUX:
			// actually, there is two cart in one... First have extra mirroring
			// mode (one screen) and 32K bankswitching, second one have only
			// 16 bankswitching mode and normal mirroring... But there is no any
			// correlations between modes and they can be used in one mapper code.
			if (value.Bit(7))
			{
				 SetMirrorType(value.Bit(5) ? EMirrorType.OneScreenB : EMirrorType.OneScreenA);
			}
			else
			{
				SetMirrorType(value.Bit(3) ? EMirrorType.Horizontal : EMirrorType.Vertical);
			}
		}

		public override byte ReadPrg(int addr)
		{
			if (_isPrg32kMode)
			{
				int bank = (_prgReg >> 1) & 0xF;
				bank &= prgMask32k;
				return Rom[(bank * 0x8000) + (addr & 0x7FFF)];
			}
			else
			{
				
				if (addr < 0x4000)
				{
					int bank = (_prgReg & 0x30) >> 1 | _prgReg & 7;
					bank &= prgMask16k;
					return Rom[(bank * 0x4000) + (addr & 0x3FFF)];
				}
				else
				{
					int bank = (_prgReg & 0x30) >> 1 | 7;
					bank &= prgMask16k;
					return Rom[(bank * 0x4000) + (addr & 0x3FFF)];
				}
			}
		}
	}
}
