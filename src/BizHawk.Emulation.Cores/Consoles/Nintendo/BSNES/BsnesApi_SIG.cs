using System;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	unsafe partial class BsnesApi
	{
		private bool Handle_SIG(eMessage msg)
		{
			using (_exe.EnterExit())
			{
				switch (msg)
				{
					default:
						return false;

					case eMessage.eMessage_SIG_trace_callback:
						{
							traceCallback?.Invoke(_comm->value, _comm->GetAscii());
							break;
						}
					case eMessage.eMessage_SIG_allocSharedMemory:
						{
							// NB: shared memory blocks are allocated on the unmanaged side
							var name = _comm->GetAscii();
							var size = _comm->size;
							var ptr = _comm->ptr;

							if (_sharedMemoryBlocks.ContainsKey(name))
								throw new InvalidOperationException("Re-defined a shared memory block. Check bsnes init/shutdown code. Block name: " + name);

							_sharedMemoryBlocks.Add(name, (IntPtr)ptr);
							break;
						}
					case eMessage.eMessage_SIG_freeSharedMemory:
						throw new InvalidOperationException("Unexpected call:  SIG_freeSharedMemory");
				} //switch(msg)

				_core.Message(eMessage.eMessage_Resume);
				return true;
			}
		}
	}
}
