using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Tetris Fily 6-in-1 (GS-2004) (U) [!]
	internal sealed class UNIF_BMC_GS_2004 : NesBoardBase
	{
		private int _reg = 0xFF;

		private int _prgMask32k;
		private int _wramOffset;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "UNIF_BMC-GS-2004":
					break;
				default:
					return false;
			}

			
			_prgMask32k = (Cart.PrgSize - 8) / 32 - 1;

			// Last 8k of Prg goes into 6000-7FFF 
			_wramOffset = ((Cart.PrgSize - 8) / 32) * 0x8000;

			SetMirrorType(EMirrorType.Vertical);

			return true;
		}

		public override void NesSoftReset()
		{
			_reg = 0xFF;
			base.NesSoftReset();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("reg", ref _reg);

		}

		public override void WritePrg(int addr, byte value)
		{
			_reg = value;
		}

		public override byte ReadWram(int addr)
		{
			return Rom[_wramOffset + (addr & 0x1FFF)];
		}

		public override byte ReadPrg(int addr)
		{
			return Rom[((_reg & _prgMask32k) * 0x8000) + (addr & 0x7FFF)];
		}
	}
}
