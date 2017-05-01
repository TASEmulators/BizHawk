using System;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Libretro
{
	unsafe partial class LibretroApi
	{
		bool Handle_SIG(eMessage msg)
		{
			//I know, ive done this two completely different ways
			//both ways are sloppy glue, anyway
			//I havent decided on the final architecture yet

			switch (msg)
			{
				case eMessage.SIG_InputState:
					comm->value = (uint)core.CB_InputState(comm->port, comm->device, comm->index, comm->id);
					break;

				case eMessage.SIG_VideoUpdate:
					core.SIG_VideoUpdate();
					break;

				default:
					return false;
			
			} //switch(msg)

			Message(eMessage.Resume);
			return true;
		}
	}
}