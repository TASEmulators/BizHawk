using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class UNIF_BMC_Super24in1SC03 : MMC3Board_Base
	{
		private ByteBuffer exRegs = new ByteBuffer(3);
		private readonly int[] masko8 = { 63, 31, 15, 1, 3, 0, 0, 0 };
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_BMC-Super24in1SC03":
					break;
				default:
					return false;
			}

			BaseSetup();

			exRegs[0] = 0x24;
			exRegs[1] = 159;
			exRegs[2] = 0;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("exRegs", ref exRegs);
		}

		public override void WriteEXP(int addr, byte value)
		{
			switch (addr)
			{
				case 0x1FF0:
					exRegs[0] = value; break;
				case 0x1FF1:
					exRegs[1] = value; break;
				case 0x1FF2:
					exRegs[2] = value; break;
			}

			base.WriteEXP(addr, value);
		}

		protected override int Get_CHRBank_1K(int addr)
		{
			if (!exRegs[0].Bit(5))
			{
				return base.Get_CHRBank_1K(addr) | (exRegs[1] << 3);
			}

			return base.Get_CHRBank_1K(addr);
		}

		protected override int Get_PRGBank_8K(int addr)
		{
			// TODO
			return base.Get_PRGBank_8K(addr);
		}
	}
}
