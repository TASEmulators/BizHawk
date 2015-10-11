using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Tetris Fily 6-in-1 (GS-2004) (U) [!]
	public class UNIF_BMC_GS_2004 : NES.NESBoardBase
	{
		private int _reg = 0xFF;

		private int _prgMask32k;
		private int _wramOffset;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_BMC-GS-2004":
					break;
				default:
					return false;
			}

			
			_prgMask32k = (Cart.prg_size - 8) / 32 - 1;

			// Last 8k of Prg goes into 6000-7FFF 
			_wramOffset = ((Cart.prg_size - 8) / 32) * 0x8000;

			SetMirrorType(EMirrorType.Vertical);

			return true;
		}

		public override void NESSoftReset()
		{
			_reg = 0xFF;
			base.NESSoftReset();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("reg", ref _reg);

		}

		public override void WritePRG(int addr, byte value)
		{
			_reg = value;
		}

		public override byte ReadWRAM(int addr)
		{
			return ROM[_wramOffset + (addr & 0x1FFF)];
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[((_reg & _prgMask32k) * 0x8000) + (addr & 0x7FFF)];
		}
	}
}
