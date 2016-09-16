namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper219 : MMC3Board_Base
	{
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER219":
					break;
				default:
					return false;
			}

			BaseSetup();
			return true;
		}
	}
}
