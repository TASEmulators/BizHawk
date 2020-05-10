using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class UNIF_BMC_190in1 : NesBoardBase
	{
		private int _reg;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
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

		public override void WritePrg(int addr, byte value)
		{
			_reg = (addr >> 2) & 7;
			SetMirrorType(addr.Bit(0) ? EMirrorType.Horizontal : EMirrorType.Vertical);
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				return Vrom[(_reg * 0x2000) + (addr & 0x1FFF)];
			}

			return base.ReadPpu(addr);
		}

		public override byte ReadPrg(int addr)
		{
			return Rom[(_reg * 0x4000) + (addr & 0x3FFF)];
		}
	}
}
