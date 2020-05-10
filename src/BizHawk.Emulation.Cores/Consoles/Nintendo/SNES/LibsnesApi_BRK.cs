using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	unsafe partial class LibsnesApi
	{
		bool Handle_BRK(eMessage msg)
		{
			using (_exe.EnterExit())
			{
				switch (msg)
				{
					default:
						return false;

					case eMessage.eMessage_BRK_hook_exec:
						ExecHook(_comm->addr);
						break;
					case eMessage.eMessage_BRK_hook_read:
						ReadHook(_comm->addr);
						break;
					case eMessage.eMessage_BRK_hook_write:
						WriteHook(_comm->addr, (byte)_comm->value);
						break;

					case eMessage.eMessage_BRK_hook_exec_smp:
						ExecHook_SMP(_comm->addr);
						break;
					case eMessage.eMessage_BRK_hook_read_smp:
						ReadHook_SMP(_comm->addr);
						break;
					case eMessage.eMessage_BRK_hook_write_smp:
						WriteHook_SMP(_comm->addr, (byte)_comm->value);
						break;

					//not supported yet
					case eMessage.eMessage_BRK_hook_nmi:
						break;
					case eMessage.eMessage_BRK_hook_irq:
						break;

					case eMessage.eMessage_BRK_scanlineStart:
						scanlineStart?.Invoke(_comm->scanline);
						break;

				} //switch(msg)

				_core.Message(eMessage.eMessage_Resume);
				return true;
			}
		}
	}
}
