namespace BizHawk.Emulation.Consoles.Nintendo
{
	// Time Diver Avenger (Unl)
	// MMC3 with slightly different write scheme
	// presumably the board contains an MMC3 clone with some unique edge case behavior; unknown
	public class Mapper250 : MMC3Board_Base
	{
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER250":
					break;
				default:
					return false;
			}
			BaseSetup();
			return true;
		}

		public override void WritePRG(int addr, byte value)
		{
			base.WritePRG(addr & 0x6000 | addr >> 10 & 1, (byte)(addr & 0xff));
		}
	}
}
