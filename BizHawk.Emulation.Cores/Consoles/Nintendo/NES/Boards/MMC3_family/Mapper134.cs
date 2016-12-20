using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper134 : MMC3Board_Base
	{
		private byte reg;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER134":
					break;
				default:
					return false;
			}

			BaseSetup();
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("reg", ref reg);
		}

		public override void WriteWRAM(int addr, byte value)
		{
			if (addr == 1) // 0x6001
			{
				reg = value;
			}

			base.WriteWRAM(addr, value);
		}

		protected override int Get_PRGBank_8K(int addr)
		{
			return (base.Get_PRGBank_8K(addr) &0x1F) | ((reg & 0x2) << 4);
		}

		protected override int Get_CHRBank_1K(int addr)
		{
			return base.Get_CHRBank_1K(addr) | (reg <<3 & 0x100);
		}
	}
}
