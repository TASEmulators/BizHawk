using System;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Libretro
{
	unsafe partial class LibretroApi
	{
		bool Handle_SIG(eMessage msg)
		{
			switch (msg)
			{
				default:
					return false;
			
			} //switch(msg)

			Message(eMessage.Resume);
			return true;
		}
	}
}