namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper195 : MMC3Board_Base
	{
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER195":
					break;
				default:
					return false;
			}

			BaseSetup();
			return true;
		}

		public override byte ReadEXP(int addr)
		{
			if (addr < 0x1000)
			{
				return ROM[(2 << 0x1000) + (addr & 0xFFF)];
			}

			return base.ReadEXP(addr);
		}

		public override void WritePRG(int addr, byte value)
		{
			base.WritePRG(addr, value);
		}
	}
}
