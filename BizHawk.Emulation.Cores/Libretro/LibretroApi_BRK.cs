using System;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Libretro
{
	unsafe partial class LibretroApi
	{
		bool Handle_BRK(eMessage msg)
		{
			switch (msg)
			{
				case eMessage.BRK_InputState:
					comm->value = (uint)core.CB_InputState(comm->port, comm->device, comm->index, comm->id);
					break;

				default:
					return false;

			} //switch(msg)

			Message(eMessage.Resume);
			return true;
		}
	}
}