using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// TODO
	public sealed class Mapper223 : MMC3Board_Base
	{
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER223":
					break;
				default:
					return false;
			}

			BaseSetup();
			return true;
		}
	}
}
