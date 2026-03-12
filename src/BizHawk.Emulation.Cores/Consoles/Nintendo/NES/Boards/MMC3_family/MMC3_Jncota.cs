using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class MMC_Jncota : MMC3Board_Base
	{
		// unsure if this is represented in any other mapper (it doesn't appear to be.)
		// the games have a register at 0x5000 that swaps out 512K banks that the MMC3 references.
		// unclear if it is readable or has mirrors etc

		public byte prg_reg;

		public override bool Configure(EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.BoardType)
			{
				case "MMC3_Jncota":
					break;

				default:
					return false;
			}

			prg_reg = 0;
			BaseSetup();

			return true;
		}

		protected override int Get_PRGBank_8K(int addr)
		{
			int bank_8k = addr >> 13;
			bank_8k = mmc3.prg_regs_8k[bank_8k];
			bank_8k |= prg_reg;
			return bank_8k;
		}

		public override void WriteExp(int addr, byte value)
		{
			if (addr == 0x1000) { prg_reg = (byte)(value != 0 ? 0x40 : 0); }
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(prg_reg), ref prg_reg);

			base.SyncState(ser);
		}
	}
}