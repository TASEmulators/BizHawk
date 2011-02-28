using System;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo.Boards
{
	public class NROM : NES.NESBoardBase
	{
		public override void Initialize(NES.RomInfo romInfo, NES nes)
		{
			base.Initialize(romInfo, nes);
			Debug.Assert(romInfo.PRG_Size < 3);
			mask = (RomInfo.PRG_Size << 14) - 1;
		}
		public override byte ReadPRG(int addr)
		{
			addr &= mask;
			return RomInfo.ROM[addr];
		}

		int mask;
	}
}