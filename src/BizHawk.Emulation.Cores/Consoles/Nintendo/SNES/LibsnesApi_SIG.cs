using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	unsafe partial class LibsnesApi
	{
		private bool Handle_SIG(eMessage msg)
		{
			using (_exe.EnterExit())
			{
				switch (msg)
				{
					default:
						return false;

					case eMessage.eMessage_SIG_video_refresh:
						{
							int width = _comm->width;
							int height = _comm->height;
							video_refresh?.Invoke((int*)_comm->ptr, width, height);
							break;
						}
					case eMessage.eMessage_SIG_input_poll:
						break;
					case eMessage.eMessage_SIG_input_state:
						{
							int port = _comm->port;
							int device = _comm->device;
							int index = _comm->index;
							int id = (int)_comm->id;
							if (input_state != null)
								_comm->value = (uint)input_state(port, device, index, id);
							break;
						}
					case eMessage.eMessage_SIG_input_notify:
						{
							input_notify?.Invoke(_comm->index);
							break;
						}
					case eMessage.eMessage_SIG_audio_flush:
						{
							uint nsamples = _comm->size;

							if (audio_sample != null)
							{
								var audiobuffer = (short*)_comm->ptr;
								for (uint i = 0; i < nsamples;)
								{
									var left = audiobuffer[i++];
									var right = audiobuffer[i++];
									audio_sample(left, right);
								}
							}

							break;
						}
					case eMessage.eMessage_SIG_path_request:
						{
							int slot = _comm->slot;
							string hint = _comm->GetAscii();
							string ret = hint;
							if (pathRequest != null)
								hint = pathRequest(slot, hint);
							CopyAscii(0, hint);
							break;
						}
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
