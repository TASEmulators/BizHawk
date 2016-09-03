using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class UNIF_BMC_190in1 : NES.NESBoardBase
	{
		private int _reg;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_BMC-190in1":
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

		}

		public override void WritePRG(int addr, byte value)
		{
			_reg = (addr >> 2) & 7;
			SetMirrorType(addr.Bit(0) ? EMirrorType.Horizontal : EMirrorType.Vertical);
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VROM[(_reg * 0x2000) + (addr & 0x1FFF)];
			}

			return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[(_reg * 0x4000) + (addr & 0x3FFF)];
		}
	}
}
