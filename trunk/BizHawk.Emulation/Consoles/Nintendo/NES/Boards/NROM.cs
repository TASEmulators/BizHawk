using System;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo.Boards
{
	public class NROM : NES.NESBoardBase
	{
		public virtual void Initialize(NES.RomInfo romInfo, NES nes)
		{
			base.Initialize(romInfo, nes);
			Debug.Assert(romInfo.PRG_Size < 3);
		}
		public override byte ReadPRG(int addr)
		{
			addr &= (RomInfo.PRG_Size << 14) - 1;
			return RomInfo.ROM[addr];
		}
	}
}