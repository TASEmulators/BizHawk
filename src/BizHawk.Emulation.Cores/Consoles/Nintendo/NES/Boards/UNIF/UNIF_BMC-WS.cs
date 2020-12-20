using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Based on Nitnendulator src
	internal sealed class UNIF_BMC_WS : NesBoardBase
	{
		private byte _reg0;
		private byte _reg1;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "UNIF_BMC-WS":
					break;
				default:
					return false;
			}

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("reg0", ref _reg0);
			ser.Sync("reg1", ref _reg1);
		}

		public override void WriteWram(int addr, byte value)
		{
			if ((_reg0 & 0x20) > 0)
			{
				return;
			}

			switch (addr & 1)
			{
				case 0:
					_reg0 = value;
					break;
				case 1:
					_reg1 = value;
					break;
			}

			SetMirrorType((_reg0 & 0x10) > 0 ? EMirrorType.Horizontal : EMirrorType.Vertical);
		}

		public override byte ReadPrg(int addr)
		{
			if ((_reg0 & 0x08) > 0)
			{
				return Rom[((_reg0 & 0x07) * 0x4000) + (addr & 0x3FFF)];
			}

			return Rom[(((_reg0 & 0x6) >> 1) * 0x8000) + (addr & 0x7FFF)];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				return Vrom[((_reg1 & 0x07) * 0x2000) + (addr & 0x1FFF)];
			}

			return base.ReadPpu(addr);
		}
	}
}
