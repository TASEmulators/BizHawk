using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper012 : MMC3Board_Base
	{
		public override bool Configure(EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.BoardType)
			{
				case "MAPPER012": 
					break;
				default:
					return false;
			}

			BaseSetup();
			mmc3.MMC3Type = MMC3.EMMC3Type.MMC3A;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(block0), ref block0);
			ser.Sync(nameof(block1), ref block1);
		}

		private int block0, block1;

		public override void WriteExp(int addr, byte value)
		{
			base.WriteExp(addr, value);
			block0 = value & 1;
			block1 = (value >> 4) & 1;
		}

		protected override int Get_CHRBank_1K(int addr)
		{
			int bank_1k = base.Get_CHRBank_1K(addr);
			if (addr < 0x1000)
				bank_1k += (block0 << 8);
			else bank_1k += (block1 << 8);
			return bank_1k;
		}
	}
}