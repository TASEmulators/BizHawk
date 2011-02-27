using System;

namespace BizHawk.Emulation.Consoles.Nintendo.Boards
{
	public class NROM : NES.NESBoardBase
	{
		public override byte ReadPRG(int addr)
		{
			return RomInfo.ROM[addr];
		}
	}
}