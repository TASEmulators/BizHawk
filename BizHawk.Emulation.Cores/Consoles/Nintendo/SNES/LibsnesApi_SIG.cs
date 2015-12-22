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
						int width = brPipe.ReadInt32();
						int height = brPipe.ReadInt32();
						bwPipe.Write(0); //offset in mapped memory buffer
						bwPipe.Flush();
						brPipe.ReadBoolean(); //dummy synchronization
						if (video_refresh != null)
						{
							video_refresh((int*)mmvaPtr, width, height);
						}
						break;
					}
				case eMessage.eMessage_SIG_input_poll:
					break;
				case eMessage.eMessage_SIG_input_state:
					{
						int port = brPipe.ReadInt32();
						int device = brPipe.ReadInt32();
						int index = brPipe.ReadInt32();
						int id = brPipe.ReadInt32();
						ushort ret = 0;
						if (input_state != null)
							ret = input_state(port, device, index, id);
						bwPipe.Write(ret);
						bwPipe.Flush();
						break;
					}
				case eMessage.eMessage_SIG_input_notify:
					{
						int index = brPipe.ReadInt32();
						if (input_notify != null)
							input_notify(index);
						break;
					}
				case eMessage.eMessage_SIG_audio_flush:
					{
						int nsamples = brPipe.ReadInt32();
						bwPipe.Write(0); //location to store audio buffer in
						bwPipe.Flush();
						brPipe.ReadInt32(); //dummy synchronization

						if (audio_sample != null)
						{
							ushort* audiobuffer = ((ushort*)mmvaPtr);
							for (int i = 0; i < nsamples; )
							{
								ushort left = audiobuffer[i++];
								ushort right = audiobuffer[i++];
								audio_sample(left, right);
							}
						}

						bwPipe.Write(0); //dummy synchronization
						bwPipe.Flush();
						brPipe.ReadInt32();  //dummy synchronization
						break;
					}
				case eMessage.eMessage_SIG_path_request:
					{
						int slot = brPipe.ReadInt32();
						string hint = ReadPipeString();
						string ret = hint;
						if (pathRequest != null)
							hint = pathRequest(slot, hint);
						WritePipeString(hint);
						break;
					}
				case eMessage.eMessage_SIG_trace_callback:
					{
						var trace = ReadPipeString();
						if (traceCallback != null)
							traceCallback(trace);
						break;
					}
				case eMessage.eMessage_SIG_allocSharedMemory:
					{
						var name = ReadPipeString();
						var size = brPipe.ReadInt32();

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
							smb.Size = size;
							smb.BlockName = InstanceName + smb.Name;
							smb.Allocate();
						}

						SharedMemoryBlocks[smb.Name] = smb;
						WritePipeString(smb.BlockName);
						break;
					}
				case eMessage.eMessage_SIG_freeSharedMemory:
					{
						string name = ReadPipeString();
						var smb = SharedMemoryBlocks[name];
						DeallocatedMemoryBlocks[name] = smb;
						SharedMemoryBlocks.Remove(name);
						break;
					}
			} //switch(msg)
			
			return true;
		}
	}
}