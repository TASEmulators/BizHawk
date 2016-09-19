using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class UNIF_BMC_FK23C : MMC3Board_Base
	{
		private ByteBuffer exRegs = new ByteBuffer(8);

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_BMC-FK23C":
					break;
				default:
					return false;
			}

			exRegs[4] = 0xFF;
			exRegs[5] = 0xFF;
			exRegs[6] = 0xFF;
			exRegs[7] = 0xFF;
			exRegs[8] = 0xFF;

			BaseSetup();
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("exRegs", ref exRegs);
		}

		public override void WriteEXP(int addr, byte value)
		{
			base.WriteEXP(addr, value);
		}

		public override void WritePRG(int addr, byte value)
		{
			base.WritePRG(addr, value);
		}

		protected override int Get_PRGBank_8K(int addr)
		{
			return base.Get_PRGBank_8K(addr);
		}

		protected override int Get_CHRBank_1K(int addr)
		{
			return base.Get_CHRBank_1K(addr);
		}
	}
}
