using System;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	unsafe partial class LibsnesApi
	{
		bool Handle_BRK(eMessage msg)
		{
			switch (msg)
			{
				default:
					return false;

				case eMessage.eMessage_BRK_hook_exec:
					{
						ExecHook(comm->addr);
						break;
					}
				case eMessage.eMessage_BRK_hook_read:
					{
						ReadHook(comm->addr);
						break;
					}
				case eMessage.eMessage_BRK_hook_write:
					{
						WriteHook(comm->addr, (byte)comm->value);
						break;
					}

				//not supported yet
				case eMessage.eMessage_BRK_hook_nmi:
					break;
				case eMessage.eMessage_BRK_hook_irq:
					break;

				case eMessage.eMessage_BRK_scanlineStart:
					if (scanlineStart != null)
						scanlineStart(comm->scanline);
					break;

			} //switch(msg)

			Message(eMessage.eMessage_Resume);
			return true;
		}
	}
}