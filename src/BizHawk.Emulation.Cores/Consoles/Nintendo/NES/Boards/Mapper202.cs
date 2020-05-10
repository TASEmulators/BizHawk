using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// 150-in-1
	// http://wiki.nesdev.com/w/index.php/INES_Mapper_202
	internal sealed class Mapper202 : NesBoardBase
	{
		private int _reg;
		private bool _isprg32KMode;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER202":
					break;
				default:
					return false;
			}

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("reg", ref _reg);
			ser.Sync("isPrg32kMode", ref _isprg32KMode);
		}

		public override void WritePrg(int addr, byte value)
		{
			_reg = (addr >> 1) & 7;
			_isprg32KMode = addr.Bit(0) && addr.Bit(3);

			SetMirrorType(addr.Bit(0) ? EMirrorType.Horizontal : EMirrorType.Vertical);
		}

		public override byte ReadPrg(int addr)
		{
			if (_isprg32KMode)
			{
				return Rom[((_reg >> 1) * 0x8000) + (addr & 0x7FFF)];
			}
			
			return Rom[(_reg * 0x4000) + (addr & 0x3FFF)];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				return Vrom[(_reg * 0x2000) + (addr & 0x1FFF)];
			}

			return base.ReadPpu(addr);
		}
	}
}
