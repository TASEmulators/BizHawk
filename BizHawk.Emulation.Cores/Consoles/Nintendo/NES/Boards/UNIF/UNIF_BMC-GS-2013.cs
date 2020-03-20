using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Tetris Family 12-in-1 (GS-2013) [U][!]
	// This cart is 2 ROMs in 1
	// Pretty much the UNIF_BMC-GS_2004 board, with more Rom tacked on
	public class UNIF_BMC_GS_2013 : NesBoardBase
	{
		private int _reg = 0xFF;
		private bool _isRom2 = true;

		private int _prgMaskRom1 = 7;
		private int _prgMaskRom2 = 1;

		private int _wramPage = 0x3E000;
		private int _rom2Offset = 0x40000;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_BMC-GS-2013":
					break;
				default:
					return false;
			}

			SetMirrorType(EMirrorType.Vertical);

			return true;
		}

		public override void NesSoftReset()
		{
			_reg = 0xFF;
			_isRom2 = true;
			base.NesSoftReset();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("reg", ref _reg);
			ser.Sync(nameof(_isRom2), ref _isRom2);

		}

		public override void WritePrg(int addr, byte value)
		{
			_isRom2 = value.Bit(3);
			_reg = value;
		}

		public override byte ReadWram(int addr)
		{
			return Rom[_wramPage + (addr & 0x1FFF)];
		}

		public override byte ReadPrg(int addr)
		{
			int bank = _reg & (_isRom2 ? _prgMaskRom2 : _prgMaskRom1);
			return Rom[(bank * 0x8000) + (addr & 0x7FFF) + (_isRom2 ? _rom2Offset : 0)];
		}
	}
}
