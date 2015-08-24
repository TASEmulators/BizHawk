using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// 9999999-in-1 [p2]
	// http://wiki.nesdev.com/w/index.php/INES_Mapper_213
	public class Mapper213 : NES.NESBoardBase
	{
		private int _reg;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER213":
					break;
				default:
					return false;
			}

			SetMirrorType(Cart.pad_h, Cart.pad_v);

			_reg = 65535;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("_reg", ref _reg);
		}

		public override void WritePRG(int addr, byte value)
		{
			addr += 0x8000;

			_reg = addr;
			SetMirrorType(addr.Bit(3) ? EMirrorType.Vertical : EMirrorType.Horizontal);
		}

		public override byte ReadPRG(int addr)
		{
			int bank = (_reg >> 1) & 3;
			return ROM[(bank * 0x8000) + (addr & 0x7FFF)];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank = (_reg >> 3) & 7;
				return VROM[(bank * 0x2000) + (addr & 0x1FFF)];
			}

			return base.ReadPPU(addr);
		}
	}
}
