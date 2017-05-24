using System;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	unsafe partial class LibsnesApi
	{
		bool Handle_SIG(eMessage msg)
		{
			switch (msg)
			{
				default:
					return false;

				case eMessage.eMessage_SIG_video_refresh:
					{
						int width = comm->width;
						int height = comm->height;
						if (video_refresh != null)
						{
							video_refresh((int*)comm->ptr, width, height);
						}
						break;
					}
				case eMessage.eMessage_SIG_input_poll:
					break;
				case eMessage.eMessage_SIG_input_state:
					{
						int port = comm->port;
						int device = comm->device;
						int index = comm->index;
						int id = (int)comm->id;
						if (input_state != null)
							comm->value = (uint)input_state(port, device, index, id);
						break;
					}
				case eMessage.eMessage_SIG_input_notify:
					{
						if (input_notify != null)
							input_notify(comm->index);
						break;
					}
				case eMessage.eMessage_SIG_audio_flush:
					{
						uint nsamples = comm->size;

						if (audio_sample != null)
						{
							ushort* audiobuffer = ((ushort*)comm->ptr);
							for (uint i = 0; i < nsamples; )
							{
								ushort left = audiobuffer[i++];
								ushort right = audiobuffer[i++];
								audio_sample(left, right);
							}
						}

						break;
					}
				case eMessage.eMessage_SIG_path_request:
					{
						int slot = comm->slot;
						string hint = comm->GetAscii();
						string ret = hint;
						if (pathRequest != null)
						  hint = pathRequest(slot, hint);
						CopyAscii(0, hint);
						break;
					}
				case eMessage.eMessage_SIG_trace_callback:
					{
						if (traceCallback != null)
							traceCallback(comm->value, comm->GetAscii());
						break;
					}
				case eMessage.eMessage_SIG_allocSharedMemory:
					{
						var name = comm->GetAscii();
						var size = comm->size;

						if (SharedMemoryBlocks.ContainsKey(name))
						{
							throw new InvalidOperationException("Re-defined a shared memory block. Check bsnes init/shutdown code. Block name: " + name);
						}

						//try reusing existing block; dispose it if it exists and if the size doesnt match
						SharedMemoryBlock smb = null;
						if (DeallocatedMemoryBlocks.ContainsKey(name))
						{
							smb = DeallocatedMemoryBlocks[name];
							DeallocatedMemoryBlocks.Remove(name);
							if (smb.Size != size)
							{
								smb.Dispose();
								smb = null;
							}
						}

						//allocate a new block if we have to
						if (smb == null)
						{
							smb = new SharedMemoryBlock();
							smb.Name = name;
							smb.Size = (int)size;
							smb.BlockName = InstanceName + smb.Name;
							smb.Allocate();
						}

						comm->ptr = smb.Ptr;
						SharedMemoryBlocks[smb.Name] = smb;
						CopyAscii(0, smb.BlockName);
						break;
					}
				case eMessage.eMessage_SIG_freeSharedMemory:
					{
						foreach (var block in SharedMemoryBlocks.Values)
						{
							if (block.Ptr == comm->ptr)
							{
								DeallocatedMemoryBlocks[block.Name] = block;
								SharedMemoryBlocks.Remove(block.Name);
								break;
							}
						}
						break;
					}
			} //switch(msg)

			Message(eMessage.eMessage_Resume);
			return true;
		}
	}
}