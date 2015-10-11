using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Tetris Family 12-in-1 (GS-2013) [U][!]
	// This cart is 2 ROMs in 1
	// Pretty much the UNIF_BMC-GS_2004 board, with more Rom tacked on
	public class UNIF_BMC_GS_2013 : NES.NESBoardBase
	{
		private int _reg = 0xFF;
		private bool _isRom2 = true;

		private int _prgMaskRom1 = 7;
		private int _prgMaskRom2 = 1;

		private int _wramPage = 0x3E000;
		private int _rom2Offset = 0x40000;

		public override bool Configure(NES.EDetectionOrigin origin)
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

		public override void NESSoftReset()
		{
			_reg = 0xFF;
			_isRom2 = true;
			base.NESSoftReset();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("reg", ref _reg);
			ser.Sync("_isRom2", ref _isRom2);

		}

		public override void WritePRG(int addr, byte value)
		{
			_isRom2 = value.Bit(3);
			_reg = value;
		}

		public override byte ReadWRAM(int addr)
		{
			return ROM[_wramPage + (addr & 0x1FFF)];
		}

		public override byte ReadPRG(int addr)
		{
			int bank = _reg & (_isRom2 ? _prgMaskRom2 : _prgMaskRom1);
			return ROM[(bank * 0x8000) + (addr & 0x7FFF) + (_isRom2 ? _rom2Offset : 0)];
		}
	}
}
