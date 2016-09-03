using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Based on Nitnendulator src
	public sealed class UNIF_BMC_WS : NES.NESBoardBase
	{
		private byte _reg0;
		private byte _reg1;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
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

		public override void WriteWRAM(int addr, byte value)
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

			if ((_reg0 & 0x10) > 0)
			{
				SetMirrorType(EMirrorType.Horizontal);
			}
			else
			{
				SetMirrorType(EMirrorType.Vertical);
			}
		}

		public override byte ReadPRG(int addr)
		{
			if ((_reg0 & 0x08) > 0)
			{
				return ROM[((_reg0 & 0x07) * 0x4000) + (addr & 0x3FFF)];
			}
			else
			{
				return ROM[(((_reg0 & 0x6) >> 1) * 0x8000) + (addr & 0x7FFF)];
			}

		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VROM[((_reg1 & 0x07) * 0x2000) + (addr & 0x1FFF)];
			}

			return base.ReadPPU(addr);
		}
	}
}
