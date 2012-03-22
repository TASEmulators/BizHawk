using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	//AKA mapper 67
	//this may be confusing due to general chaos with the early sunsoft mappers. see docs/sunsoft.txt
	class Sunsoft3 : NES.NESBoardBase
	{
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//TBD
			return false;
		}
	}
}
