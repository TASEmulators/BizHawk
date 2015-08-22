using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// 150-in-1
	// http://wiki.nesdev.com/w/index.php/INES_Mapper_202
	public class Mapper202 : NES.NESBoardBase
	{
		private int _reg;
		private bool _isprg32KMode;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
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

		public override void WritePRG(int addr, byte value)
		{
			_reg = (addr >> 1) & 7;
			_isprg32KMode = addr.Bit(0) && addr.Bit(3);

			SetMirrorType(addr.Bit(0) ? EMirrorType.Horizontal : EMirrorType.Vertical);
		}

		public override byte ReadPRG(int addr)
		{
			if (_isprg32KMode)
			{
				return ROM[((_reg >> 1) * 0x8000) + (addr & 0x7FFF)];
			}
			
			return ROM[(_reg * 0x4000) + (addr & 0x3FFF)];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VROM[(_reg * 0x2000) + (addr & 0x1FFF)];
			}

			return base.ReadPPU(addr);
		}
	}
}
