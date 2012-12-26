using System;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO.MemoryMappedFiles;

namespace BizHawk.Emulation.Consoles.Nintendo.SNES
{
	public unsafe class LibsnesApi : IDisposable
	{
		//this wouldve been the ideal situation to learn protocol buffers, but since the number of messages here is so limited, it took less time to roll it by hand.
		//todo - could optimize a lot of the apis once we decide to commit to this. will we? then we wont be able to debug bsnes as well
		//        well, we could refactor it a lot and let the debuggable static dll version be the one that does annoying workarounds
		//todo - more intelligent use of buffers to avoid so many copies (especially framebuffer from bsnes? supply framebuffer to-be-used to libsnes? same for audiobuffer)
		//todo - refactor to use a smarter set of pipe reader and pipe writer classes
		//todo - combine messages / tracecallbacks into one system with a channel number enum additionally
		//todo - consider refactoring bsnes to allocate memory blocks through the interface, and set ours up to allocate from a large arena of shared memory.
		//        this is a lot of work, but it will be some decent speedups. who wouldve ever thought to make an emulator this way? I will, from now on...
		//todo - use a reader/writer ring buffer for communication instead of pipe
		//todo - when exe wrapper is fully baked, put it into mingw so we can just have libsneshawk.exe without a separate dll. it hardly needs any debugging presently, it should be easy to maintain.
		
		//space optimizations to deploy later (only if people complain about so many files)
		//todo - put executables in zipfiles and search for them there; dearchive to a .cache folder. check timestamps to know when to freshen. this is weird.....

		//speedups to deploy later:
		//todo - convey rom data faster than pipe blob (use shared memory) (WARNING: right now our general purpose shared memory is only 1MB. maybe wait until ring buffer IPC)
		//todo - collapse input messages to one IPC operation. right now theresl ike 30 of them
		//todo - collect all memory block names whenever a memory block is alloc/dealloced. that way we avoid the overhead when using them for gui stuff (gfx debugger, hex editor)

		string InstanceName;
		Process process;
		NamedPipeServerStream pipe;
		BinaryWriter bwPipe;
		BinaryReader brPipe;
		MemoryMappedFile mmf;
		MemoryMappedViewAccessor mmva;
		byte* mmvaPtr;

		public enum eMessage : int
		{
			eMessage_Complete,

			eMessage_snes_library_id,
			eMessage_snes_library_revision_major,
			eMessage_snes_library_revision_minor,

			eMessage_snes_init,
			eMessage_snes_power,
			eMessage_snes_reset,
			eMessage_snes_run,
			eMessage_snes_term,
			eMessage_snes_unload_cartridge,

			//snes_set_cartridge_basename, //not used

			eMessage_snes_load_cartridge_normal,
			eMessage_snes_load_cartridge_super_game_boy,

			//incoming from bsnes
			eMessage_snes_cb_video_refresh,
			eMessage_snes_cb_input_poll,
			eMessage_snes_cb_input_state,
			eMessage_snes_cb_input_notify,
			eMessage_snes_cb_audio_sample,
			eMessage_snes_cb_scanlineStart,
			eMessage_snes_cb_path_request,
			eMessage_snes_cb_trace_callback,

			eMessage_snes_get_region,

			eMessage_snes_get_memory_size,
			eMessage_snes_get_memory_data,
			eMessage_peek,
			eMessage_poke,

			eMessage_snes_serialize_size,

			eMessage_snes_serialize,
			eMessage_snes_unserialize,

			eMessage_snes_poll_message,
			eMessage_snes_dequeue_message,

			eMessage_snes_set_color_lut,

			eMessage_snes_enable_trace,
			eMessage_snes_enable_scanline,
			eMessage_snes_enable_audio,
			eMessage_snes_set_layer_enable,
			eMessage_snes_set_backdropColor,
			eMessage_snes_peek_logical_register,

			eMessage_snes_allocSharedMemory,
			eMessage_snes_freeSharedMemory,
			eMessage_GetMemoryIdName,
		};

		static bool DryRun(string exePath)
		{
			ProcessStartInfo oInfo = new ProcessStartInfo(exePath, "Bongizong");
			oInfo.WorkingDirectory = Path.GetDirectoryName(exePath);
			oInfo.UseShellExecute = false;
			oInfo.CreateNoWindow = true;
			oInfo.RedirectStandardOutput = true;
			oInfo.RedirectStandardError = true;

			Process proc = System.Diagnostics.Process.Start(oInfo);
			string result = proc.StandardError.ReadToEnd();
			proc.WaitForExit();

			//yongou chonganong nongo tong rongeadong
			//pongigong chong hongi nonge songe
			if (result == "Honga Wongkong" && proc.ExitCode == 0x16817)
				return true;

			return false;
		}

		static HashSet<string> okExes = new HashSet<string>();
		public LibsnesApi(string exePath)
		{
			//make sure we've checked this exe for OKness.. the dry run should keep us from freezing up or crashing weirdly if the external process isnt correct
			if (!okExes.Contains(exePath))
			{
				bool ok = DryRun(exePath);
				if (!ok)
					throw new InvalidOperationException(string.Format("Couldn't launch {0} to run SNES core. Not sure why this would have happened. Try redownloading BizHawk first.", Path.GetFileName(exePath)));
				okExes.Add(exePath);
			}

			InstanceName = "libsneshawk_" + Guid.NewGuid().ToString();

			//use this to get a debug console with libsnes output
			//InstanceName = "console-" + InstanceName;

			var pipeName = InstanceName;

			mmf = MemoryMappedFile.CreateNew(pipeName, 1024 * 1024);
			mmva = mmf.CreateViewAccessor();
			mmva.SafeMemoryMappedViewHandle.AcquirePointer(ref mmvaPtr);

			pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None, 1024 * 1024, 1024);

			process = new Process();
			process.StartInfo.WorkingDirectory = Path.GetDirectoryName(exePath);
			process.StartInfo.FileName = exePath;
			process.StartInfo.Arguments = pipeName;
			process.StartInfo.ErrorDialog = true;
			process.Start();

			//TODO - start a thread to wait for process to exit and gracefully handle errors? how about the pipe?

			pipe.WaitForConnection();
			bwPipe = new BinaryWriter(pipe);
			brPipe = new BinaryReader(pipe);
		}

		public void Dispose()
		{
			process.Kill();
			process.Dispose();
			process = null;
			pipe.Dispose();
			mmva.Dispose();
			mmf.Dispose();
		}

		void WritePipeString(string str)
		{
			WritePipeBlob(System.Text.Encoding.ASCII.GetBytes(str));
		}

		byte[] ReadPipeBlob()
		{
			int len = brPipe.ReadInt32();
			var ret = new byte[len];
			brPipe.Read(ret, 0, len);
			return ret;
		}

		void WritePipeBlob(byte[] blob)
		{
			bwPipe.Write(blob.Length);
			bwPipe.Write(blob);
		}

		public int MessageCounter;

		void WritePipeMessage(eMessage msg)
		{
			MessageCounter++;
			bwPipe.Write((int)msg);
		}

		eMessage ReadPipeMessage()
		{
			return (eMessage)brPipe.ReadInt32();
		}

		string ReadPipeString()
		{
			int len = brPipe.ReadInt32();
			var bytes = brPipe.ReadBytes(len);
			return System.Text.ASCIIEncoding.ASCII.GetString(bytes);
		}

		public string snes_library_id()
		{
			WritePipeMessage(eMessage.eMessage_snes_library_id);
			return ReadPipeString();
		}


		public uint snes_library_revision_major()
		{
			WritePipeMessage(eMessage.eMessage_snes_library_revision_major);
			return brPipe.ReadUInt32();
		}

		public uint snes_library_revision_minor()
		{
			WritePipeMessage(eMessage.eMessage_snes_library_revision_minor);
			return brPipe.ReadUInt32();
		}

		public void snes_init()
		{
			WritePipeMessage(eMessage.eMessage_snes_init);
			WaitForCompletion();
		}
		public void snes_power() { WritePipeMessage(eMessage.eMessage_snes_power); }
		public void snes_reset() { WritePipeMessage(eMessage.eMessage_snes_reset); }
		public void snes_run()
		{
			WritePipeMessage(eMessage.eMessage_snes_run);
			WaitForCompletion();
		}
		public void snes_term() { WritePipeMessage(eMessage.eMessage_snes_term); }
		public void snes_unload_cartridge() { WritePipeMessage(eMessage.eMessage_snes_unload_cartridge); }

		public bool snes_load_cartridge_super_game_boy(string rom_xml, byte[] rom_data, uint rom_size, string dmg_xml, byte[] dmg_data, uint dmg_size)
		{
			WritePipeMessage(eMessage.eMessage_snes_load_cartridge_super_game_boy);
			WritePipeString(rom_xml ?? "");
			WritePipeBlob(rom_data);
			WritePipeString(rom_xml ?? "");
			WritePipeBlob(dmg_data);
			//not a very obvious order.. because we do tons of work immediately after the last param goes down and need to answer messages
			WaitForCompletion();
			bool ret = brPipe.ReadBoolean();
			return ret;
		}

		public bool snes_load_cartridge_normal(string rom_xml, byte[] rom_data)
		{
			WritePipeMessage(eMessage.eMessage_snes_load_cartridge_normal);
			WritePipeString(rom_xml ?? "");
			WritePipeBlob(rom_data);
			//not a very obvious order.. because we do tons of work immediately after the last param goes down and need to answer messages
			WaitForCompletion();
			bool ret = brPipe.ReadBoolean();
			return ret;
		}

		public SNES_REGION snes_get_region()
		{
			WritePipeMessage(eMessage.eMessage_snes_get_region);
			return (SNES_REGION)brPipe.ReadByte();
		}

		public int snes_get_memory_size(SNES_MEMORY id)
		{
			WritePipeMessage(eMessage.eMessage_snes_get_memory_size);
			bwPipe.Write((int)id);
			return brPipe.ReadInt32();
		}

		string MemoryNameForId(SNES_MEMORY id)
		{
			WritePipeMessage(eMessage.eMessage_GetMemoryIdName);
			bwPipe.Write((uint)id);
			return ReadPipeString();
		}

		public byte* snes_get_memory_data(SNES_MEMORY id)
		{
			string name = MemoryNameForId(id);
			var smb = SharedMemoryBlocks[name];
			return (byte*)smb.Ptr;
		}

		public byte peek(SNES_MEMORY id, uint addr)
		{
			WritePipeMessage(eMessage.eMessage_peek);
			bwPipe.Write((uint)id);
			bwPipe.Write(addr);
			return brPipe.ReadByte();
		}
		public void poke(SNES_MEMORY id, uint addr, byte val)
		{
			WritePipeMessage(eMessage.eMessage_poke);
			bwPipe.Write((uint)id);
			bwPipe.Write(addr);
			bwPipe.Write(val);
		}

		public int snes_serialize_size()
		{
			WritePipeMessage(eMessage.eMessage_snes_serialize_size);
			return brPipe.ReadInt32();
		}

		[DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
		public static unsafe extern void* CopyMemory(void* dest, void* src, ulong count);



		public bool snes_serialize(IntPtr data, int size)
		{
			WritePipeMessage(eMessage.eMessage_snes_serialize);
			bwPipe.Write(size);
			bwPipe.Write(0); //mapped memory location to serialize to
			WaitForCompletion(); //serialize/unserialize can cause traces to get called (because serialize can cause execution?)
			bool ret = brPipe.ReadBoolean();
			if (ret)
			{
				CopyMemory(data.ToPointer(), mmvaPtr, (ulong)size);
			}
			return ret;
		}

		public bool snes_unserialize(IntPtr data, int size)
		{
			WritePipeMessage(eMessage.eMessage_snes_unserialize);
			CopyMemory(mmvaPtr, data.ToPointer(), (ulong)size);
			bwPipe.Write(size);
			bwPipe.Write(0); //mapped memory location to serialize from
			WaitForCompletion(); //serialize/unserialize can cause traces to get called (because serialize can cause execution?)
			bool ret = brPipe.ReadBoolean();
			return ret;
		}

		int snes_poll_message()
		{
			WritePipeMessage(eMessage.eMessage_snes_poll_message);
			return brPipe.ReadInt32();
		}

		public bool HasMessage { get { return snes_poll_message() != -1; } }


		public string DequeueMessage()
		{
			WritePipeMessage(eMessage.eMessage_snes_dequeue_message);
			return ReadPipeString();
		}

		public void snes_set_color_lut(IntPtr colors)
		{
			int len = 4 * 16 * 32768;
			byte[] buf = new byte[len];
			Marshal.Copy(colors, buf, 0, len);

			WritePipeMessage(eMessage.eMessage_snes_set_color_lut);
			WritePipeBlob(buf);
		}

		public void snes_set_layer_enable(int layer, int priority, bool enable)
		{
			WritePipeMessage(eMessage.eMessage_snes_set_layer_enable);
			bwPipe.Write(layer);
			bwPipe.Write(priority);
			bwPipe.Write(enable);
		}

		public void snes_set_backdropColor(int backdropColor)
		{
			WritePipeMessage(eMessage.eMessage_snes_set_backdropColor);
			bwPipe.Write(backdropColor);
		}

		public int snes_peek_logical_register(SNES_REG reg)
		{
			WritePipeMessage(eMessage.eMessage_snes_peek_logical_register);
			bwPipe.Write((int)reg);
			return brPipe.ReadInt32();
		}

		void WaitForCompletion()
		{
			for (; ; )
			{
				var msg = ReadPipeMessage();
				MessageCounter++;
				//Console.WriteLine(msg);
				switch (msg)
				{
					case eMessage.eMessage_Complete:
						return;

					case eMessage.eMessage_snes_cb_video_refresh:
						{
							int width = brPipe.ReadInt32();
							int height = brPipe.ReadInt32();
							bwPipe.Write(0); //offset in mapped memory buffer
							brPipe.ReadBoolean(); //dummy synchronization
							if (video_refresh != null)
							{
								video_refresh((int*)mmvaPtr, width, height);
							}
							break;
						}
					case eMessage.eMessage_snes_cb_input_poll:
						break;
					case eMessage.eMessage_snes_cb_input_state:
						{
							int port = brPipe.ReadInt32();
							int device = brPipe.ReadInt32();
							int index = brPipe.ReadInt32();
							int id = brPipe.ReadInt32();
							ushort ret = 0;
							if (input_state != null)
								ret = input_state(port, device, index, id);
							bwPipe.Write(ret);
							break;
						}
					case eMessage.eMessage_snes_cb_input_notify:
						{
							int index = brPipe.ReadInt32();
							if (input_notify != null)
								input_notify(index);
							break;
						}
					case eMessage.eMessage_snes_cb_audio_sample:
						{
							int nsamples = brPipe.ReadInt32();
							bwPipe.Write(0); //location to store audio buffer in
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
							brPipe.ReadInt32();  //dummy synchronization
							break;
						}
					case eMessage.eMessage_snes_cb_scanlineStart:
						{
							int line = brPipe.ReadInt32();
							if (scanlineStart != null)
								scanlineStart(line);

							//we have to notify the unmanaged process that we're done peeking thruogh its memory and whatnot so it can proceed with emulation
							WritePipeMessage(eMessage.eMessage_Complete);
							break;
						}
					case eMessage.eMessage_snes_cb_path_request:
						{
							int slot = brPipe.ReadInt32();
							string hint = ReadPipeString();
							string ret = hint;
							if (pathRequest != null)
								hint = pathRequest(slot, hint);
							WritePipeString(hint);
							break;
						}
					case eMessage.eMessage_snes_cb_trace_callback:
						{
							var trace = ReadPipeString();
							if (traceCallback != null)
								traceCallback(trace);
							break;
						}
					case eMessage.eMessage_snes_allocSharedMemory:
						{
							var smb = new SharedMemoryBlock();
							smb.Name = ReadPipeString();
							smb.Size = brPipe.ReadInt32();
							smb.BlockName = InstanceName + smb.Name;
							smb.Allocate();
							if (SharedMemoryBlocks.ContainsKey(smb.Name))
							{
								throw new InvalidOperationException("Re-defined a shared memory block. Check bsnes init/shutdown code. Block name: " + smb.Name);
							}

							SharedMemoryBlocks[smb.Name] = smb;
							WritePipeString(smb.BlockName);
							break;
						}
					case eMessage.eMessage_snes_freeSharedMemory:
						{
							string name = ReadPipeString();
							var smb = SharedMemoryBlocks[name];
							smb.Dispose();
							SharedMemoryBlocks.Remove(name);
							break;
						}
				}
			}
		}

		class SharedMemoryBlock : IDisposable
		{
			public string Name;
			public string BlockName;
			public int Size;
			public MemoryMappedFile mmf;
			public MemoryMappedViewAccessor mmva;
			public byte* Ptr;

			public void Allocate()
			{
				mmf = MemoryMappedFile.CreateNew(BlockName, Size);
				mmva = mmf.CreateViewAccessor();
				mmva.SafeMemoryMappedViewHandle.AcquirePointer(ref Ptr);
			}

			public void Dispose()
			{
				if (mmf == null) return;
				mmva.Dispose();
				mmf.Dispose();
				mmf = null;
			}
		}

		Dictionary<string, SharedMemoryBlock> SharedMemoryBlocks = new Dictionary<string, SharedMemoryBlock>();

		snes_video_refresh_t video_refresh;
		snes_input_poll_t input_poll;
		snes_input_state_t input_state;
		snes_input_notify_t input_notify;
		snes_audio_sample_t audio_sample;
		snes_scanlineStart_t scanlineStart;
		snes_path_request_t pathRequest;
		snes_trace_t traceCallback;

		public void snes_set_video_refresh(snes_video_refresh_t video_refresh) { this.video_refresh = video_refresh; }
		public void snes_set_input_poll(snes_input_poll_t input_poll) { this.input_poll = input_poll; }
		public void snes_set_input_state(snes_input_state_t input_state) { this.input_state = input_state; }
		public void snes_set_input_notify(snes_input_notify_t input_notify) { this.input_notify = input_notify; }
		public void snes_set_audio_sample(snes_audio_sample_t audio_sample)
		{
			this.audio_sample = audio_sample;
			WritePipeMessage(eMessage.eMessage_snes_enable_audio);
			bwPipe.Write(audio_sample != null);
		}
		public void snes_set_path_request(snes_path_request_t pathRequest) { this.pathRequest = pathRequest; }
		public void snes_set_scanlineStart(snes_scanlineStart_t scanlineStart)
		{
			this.scanlineStart = scanlineStart;
			WritePipeMessage(eMessage.eMessage_snes_enable_scanline);
			bwPipe.Write(scanlineStart != null);
		}
		public void snes_set_trace_callback(snes_trace_t callback)
		{
			this.traceCallback = callback;
			WritePipeMessage(eMessage.eMessage_snes_enable_trace);
			bwPipe.Write(callback != null);
		}

		public delegate void snes_video_refresh_t(int* data, int width, int height);
		public delegate void snes_input_poll_t();
		public delegate ushort snes_input_state_t(int port, int device, int index, int id);
		public delegate void snes_input_notify_t(int index);
		public delegate void snes_audio_sample_t(ushort left, ushort right);
		public delegate void snes_scanlineStart_t(int line);
		public delegate string snes_path_request_t(int slot, string hint);
		public delegate void snes_trace_t(string msg);

		public enum SNES_REG : int
		{
			//$2105
			BG_MODE = 0,
			BG3_PRIORITY = 1,
			BG1_TILESIZE = 2,
			BG2_TILESIZE = 3,
			BG3_TILESIZE = 4,
			BG4_TILESIZE = 5,
			//$2107
			BG1_SCADDR = 10,
			BG1_SCSIZE = 11,
			//$2108
			BG2_SCADDR = 12,
			BG2_SCSIZE = 13,
			//$2109
			BG3_SCADDR = 14,
			BG3_SCSIZE = 15,
			//$210A
			BG4_SCADDR = 16,
			BG4_SCSIZE = 17,
			//$210B
			BG1_TDADDR = 20,
			BG2_TDADDR = 21,
			//$210C
			BG3_TDADDR = 22,
			BG4_TDADDR = 23,
			//$2133 SETINI
			SETINI_MODE7_EXTBG = 30,
			SETINI_HIRES = 31,
			SETINI_OVERSCAN = 32,
			SETINI_OBJ_INTERLACE = 33,
			SETINI_SCREEN_INTERLACE = 34,
			//$2130 CGWSEL
			CGWSEL_COLORMASK = 40,
			CGWSEL_COLORSUBMASK = 41,
			CGWSEL_ADDSUBMODE = 42,
			CGWSEL_DIRECTCOLOR = 43,
			//$2101 OBSEL
			OBSEL_NAMEBASE = 50,
			OBSEL_NAMESEL = 51,
			OBSEL_SIZE = 52,
			//$2131 CGADSUB
			CGADSUB_MODE = 60,
			CGADSUB_HALF = 61,
			CGADSUB_BG4 = 62,
			CGADSUB_BG3 = 63,
			CGADSUB_BG2 = 64,
			CGADSUB_BG1 = 65,
			CGADSUB_OBJ = 66,
			CGADSUB_BACKDROP = 67,
			//$212C TM
			TM_BG1 = 70,
			TM_BG2 = 71,
			TM_BG3 = 72,
			TM_BG4 = 73,
			TM_OBJ = 74,
			//$212D TM
			TS_BG1 = 80,
			TS_BG2 = 81,
			TS_BG3 = 82,
			TS_BG4 = 83,
			TS_OBJ = 84,
			//Mode7 regs
			M7SEL_REPEAT = 90,
			M7SEL_HFLIP = 91,
			M7SEL_VFLIP = 92,
			M7A = 93,
			M7B = 94,
			M7C = 95,
			M7D = 96,
			M7X = 97,
			M7Y = 98,
			//BG scroll regs
			BG1HOFS = 100,
			BG1VOFS = 101,
			BG2HOFS = 102,
			BG2VOFS = 103,
			BG3HOFS = 104,
			BG3VOFS = 105,
			BG4HOFS = 106,
			BG4VOFS = 107,
			M7HOFS = 108,
			M7VOFS = 109,
		}

		public enum SNES_MEMORY : uint
		{
			CARTRIDGE_RAM = 0,
			CARTRIDGE_RTC = 1,
			BSX_RAM = 2,
			BSX_PRAM = 3,
			SUFAMI_TURBO_A_RAM = 4,
			SUFAMI_TURBO_B_RAM = 5,
			GAME_BOY_RAM = 6,
			GAME_BOY_RTC = 7,

			WRAM = 100,
			APURAM = 101,
			VRAM = 102,
			OAM = 103,
			CGRAM = 104,

			SYSBUS = 200,
			LOGICAL_REGS = 201
		}

		public enum SNES_REGION : byte
		{
			NTSC = 0,
			PAL = 1,
		}

		public enum SNES_DEVICE : uint
		{
			NONE = 0,
			JOYPAD = 1,
			MULTITAP = 2,
			MOUSE = 3,
			SUPER_SCOPE = 4,
			JUSTIFIER = 5,
			JUSTIFIERS = 6,
			SERIAL_CABLE = 7
		}

		public enum SNES_DEVICE_ID : uint
		{
			JOYPAD_B = 0,
			JOYPAD_Y = 1,
			JOYPAD_SELECT = 2,
			JOYPAD_START = 3,
			JOYPAD_UP = 4,
			JOYPAD_DOWN = 5,
			JOYPAD_LEFT = 6,
			JOYPAD_RIGHT = 7,
			JOYPAD_A = 8,
			JOYPAD_X = 9,
			JOYPAD_L = 10,
			JOYPAD_R = 11
		}
	}

}
