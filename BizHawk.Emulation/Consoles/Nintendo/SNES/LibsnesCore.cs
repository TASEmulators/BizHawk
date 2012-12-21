//TODO - add serializer, add interlace field variable to serializer

//http://wiki.superfamicom.org/snes/show/Backgrounds

//TODO 
//libsnes needs to be modified to support multiple instances - THIS IS NECESSARY - or else loading one game and then another breaks things
// edit - this is a lot of work
//wrap dll code around some kind of library-accessing interface so that it doesnt malfunction if the dll is unavailable

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
			eMessage_snes_peek_logical_register
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

			if (result == "Honga Wongkong" && proc.ExitCode == 0x16817)
				return true;

			return false;
		}

		static bool Initialized;
		static string exePath;

		void StaticInit()
		{
			if (Initialized) return;

			string thisDir = System.Reflection.Assembly.GetExecutingAssembly().GetDirectory();
			exePath = Path.Combine(thisDir, "libsnes_pwrap.exe");
			if (!File.Exists(exePath))
				exePath = Path.Combine(Path.Combine(thisDir, "dll"), "libsnes_pwrap.exe");

			if (!File.Exists(exePath))
				throw new InvalidOperationException("Can't find libsnes_pwrap.exe to run libsneshawk core");

			if (!DryRun(exePath))
			{
				throw new InvalidOperationException("Can't launch libsnes_pwrap.exe to run libsneshawk core. Missing a libsneshawk.dll?");
			}

			Initialized = true;
		}

		public LibsnesApi()
		{
			StaticInit();

			var pipeName = "libsnespwrap_" + Guid.NewGuid().ToString();

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

		public string snes_library_id()
		{
			WritePipeMessage(eMessage.eMessage_snes_library_id);
			return brPipe.ReadStringAsciiZ();
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

		public void snes_init() { WritePipeMessage(eMessage.eMessage_snes_init); }
		public void snes_power() { WritePipeMessage(eMessage.eMessage_snes_power); }
		public void snes_reset() { WritePipeMessage(eMessage.eMessage_snes_reset); }
		public void snes_run()
		{
			WritePipeMessage(eMessage.eMessage_snes_run);
			WaitForCompletion();
		}
		public void snes_term() { WritePipeMessage(eMessage.eMessage_snes_term); }
		public void snes_unload_cartridge() { WritePipeMessage(eMessage.eMessage_snes_unload_cartridge); }


		public bool snes_load_cartridge_normal(string rom_xml, byte[] rom_data)
		{
			WritePipeMessage(eMessage.eMessage_snes_load_cartridge_normal);
			WritePipeString(rom_xml ?? "");
			WritePipeBlob(rom_data);
			WaitForCompletion();
			return brPipe.ReadBoolean();
		}

		public LibsnesDll.SNES_REGION snes_get_region()
		{
			WritePipeMessage(eMessage.eMessage_snes_get_region);
			return (LibsnesDll.SNES_REGION)brPipe.ReadByte();
		}

		public int snes_get_memory_size(LibsnesDll.SNES_MEMORY id)
		{
			WritePipeMessage(eMessage.eMessage_snes_get_memory_size);
			bwPipe.Write((int)id);
			return brPipe.ReadInt32();
		}
		public byte[] snes_get_memory_data(LibsnesDll.SNES_MEMORY id)
		{
			int size = snes_get_memory_size(id);
			WritePipeMessage(eMessage.eMessage_snes_get_memory_data);
			bwPipe.Write((int)id);
			bwPipe.Write(0);
			var ret = new byte[size];
			WaitForCompletion();
			Marshal.Copy(new IntPtr(mmvaPtr), ret, 0, size);
			return ret;
		}

		public byte peek(LibsnesDll.SNES_MEMORY id, uint addr)
		{
			WritePipeMessage(eMessage.eMessage_peek);
			bwPipe.Write((uint)id);
			bwPipe.Write(addr);
			return brPipe.ReadByte();
		}
		public void poke(LibsnesDll.SNES_MEMORY id, uint addr, byte val)
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
			if(ret)
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
			return brPipe.ReadStringAsciiZ();
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

		public int snes_peek_logical_register(LibsnesDll.SNES_REG reg)
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
							bwPipe.Write(0); //dummy synchronization
							brPipe.ReadInt32();  //dummy synchronization
							if (audio_sample != null)
							{
								ushort* audiobuffer = ((ushort*)mmvaPtr);
								for (int i = 0; i < nsamples; i++)
								{
									ushort left = audiobuffer[i++];
									ushort right = audiobuffer[i++];
									audio_sample(left, right);
								}
							}
							break;
						}
					case eMessage.eMessage_snes_cb_scanlineStart:
						{
							int line = brPipe.ReadInt32();
							if (scanlineStart != null)
								scanlineStart(line);
							break;
						}
					case eMessage.eMessage_snes_cb_path_request:
						{
							int slot = brPipe.ReadInt32();
							string hint = brPipe.ReadStringAsciiZ();
							string ret = hint;
							if(pathRequest != null)
								hint = pathRequest(slot, hint);
							WritePipeString(hint);
							break;
						}
					case eMessage.eMessage_snes_cb_trace_callback:
						{
							var trace = brPipe.ReadStringAsciiZ();
							if (traceCallback != null)
								traceCallback(trace);
							break;
						}
				}
			}
		}


		LibsnesDll.snes_video_refresh_t video_refresh;
		LibsnesDll.snes_input_poll_t input_poll;
		LibsnesDll.snes_input_state_t input_state;
		LibsnesDll.snes_input_notify_t input_notify;
		LibsnesDll.snes_audio_sample_t audio_sample;
		LibsnesDll.snes_scanlineStart_t scanlineStart;
		LibsnesDll.snes_path_request_t pathRequest;
		LibsnesDll.snes_trace_t traceCallback;

		public void snes_set_video_refresh(LibsnesDll.snes_video_refresh_t video_refresh) { this.video_refresh = video_refresh; }
		public void snes_set_input_poll(LibsnesDll.snes_input_poll_t input_poll) { this.input_poll = input_poll; }
		public void snes_set_input_state(LibsnesDll.snes_input_state_t input_state) { this.input_state = input_state; }
		public void snes_set_input_notify(LibsnesDll.snes_input_notify_t input_notify) { this.input_notify = input_notify; }
		public void snes_set_audio_sample(LibsnesDll.snes_audio_sample_t audio_sample)
		{
			this.audio_sample = audio_sample;
			WritePipeMessage(eMessage.eMessage_snes_enable_audio);
			bwPipe.Write(audio_sample != null);
		}
		public void snes_set_path_request(LibsnesDll.snes_path_request_t pathRequest) { this.pathRequest = pathRequest; }
		public void snes_set_scanlineStart(LibsnesDll.snes_scanlineStart_t scanlineStart)
		{ 
			this.scanlineStart = scanlineStart;
			WritePipeMessage(eMessage.eMessage_snes_enable_scanline);
			bwPipe.Write(scanlineStart != null);
		}
		public void snes_set_trace_callback(LibsnesDll.snes_trace_t callback)
		{ 
			this.traceCallback = callback;
			WritePipeMessage(eMessage.eMessage_snes_enable_trace);
			bwPipe.Write(callback != null);
		}
	}

	public unsafe static class LibsnesDll
	{
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern string snes_library_id();
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int snes_library_revision_major();
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int snes_library_revision_minor();

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_init();
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_power();
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_reset();
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_run();
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_term();
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_unload_cartridge();

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_cartridge_basename(string basename);

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		[return: MarshalAs(UnmanagedType.U1)]
		public static extern bool snes_load_cartridge_normal(
			[MarshalAs(UnmanagedType.LPStr)]
			string rom_xml,
			[MarshalAs(UnmanagedType.LPArray)]
			byte[] rom_data,
			uint rom_size);

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		[return: MarshalAs(UnmanagedType.U1)]
		public static extern bool snes_load_cartridge_super_game_boy(
			[MarshalAs(UnmanagedType.LPStr)]
			string rom_xml,
			[MarshalAs(UnmanagedType.LPArray)]
			byte[] rom_data,
			uint rom_size,
			[MarshalAs(UnmanagedType.LPStr)]
			string dmg_xml,
			[MarshalAs(UnmanagedType.LPArray)]
			byte[] dmg_data,
			uint dmg_size);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void snes_video_refresh_t(int* data, int width, int height);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void snes_input_poll_t();
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate ushort snes_input_state_t(int port, int device, int index, int id);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void snes_input_notify_t(int index);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void snes_audio_sample_t(ushort left, ushort right);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void snes_scanlineStart_t(int line);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate string snes_path_request_t(int slot, string hint);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void snes_trace_t(string msg);

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_video_refresh(snes_video_refresh_t video_refresh);
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_input_poll(snes_input_poll_t input_poll);
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_input_state(snes_input_state_t input_state);
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_input_notify(snes_input_notify_t input_notify);
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_audio_sample(snes_audio_sample_t audio_sample);
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_scanlineStart(snes_scanlineStart_t scanlineStart);
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_path_request(snes_path_request_t pathRequest);

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		[return: MarshalAs(UnmanagedType.U1)]
		public static extern bool snes_check_cartridge(
			[MarshalAs(UnmanagedType.LPArray)] byte[] rom_data,
			int rom_size);

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		[return: MarshalAs(UnmanagedType.U1)]
		public static extern SNES_REGION snes_get_region();

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int snes_get_memory_size(SNES_MEMORY id);
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr snes_get_memory_data(SNES_MEMORY id);

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern byte bus_read(uint addr);
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void bus_write(uint addr, byte val);

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int snes_serialize_size();

		[return: MarshalAs(UnmanagedType.U1)]
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool snes_serialize(IntPtr data, int size);

		[return: MarshalAs(UnmanagedType.U1)]
		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool snes_unserialize(IntPtr data, int size);

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int snes_poll_message();

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_dequeue_message(IntPtr strBuffer);

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_trace_callback(snes_trace_t callback);

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_color_lut(IntPtr colors);

		public static bool HasMessage { get { return snes_poll_message() != -1; } }

		public static string DequeueMessage()
		{
			int len = snes_poll_message();
			sbyte* temp = stackalloc sbyte[len + 1];
			temp[len] = 0;
			snes_dequeue_message(new IntPtr(temp));
			return new string(temp);
		}

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_layer_enable(int layer, int priority,
			[MarshalAs(UnmanagedType.U1)]
			bool enable
			);

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void snes_set_backdropColor(int backdropColor);

		[DllImport("libsneshawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int snes_peek_logical_register(SNES_REG reg);

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

	public class ScanlineHookManager
	{
		public void Register(object tag, Action<int> callback)
		{
			var rr = new RegistrationRecord();
			rr.tag = tag;
			rr.callback = callback;

			Unregister(tag);
			records.Add(rr);
			OnHooksChanged();
		}

		public int HookCount { get { return records.Count; } }

		public virtual void OnHooksChanged() { }

		public void Unregister(object tag)
		{
			records.RemoveAll((r) => r.tag == tag);
		}

		public void HandleScanline(int scanline)
		{
			foreach (var rr in records) rr.callback(scanline);
		}

		List<RegistrationRecord> records = new List<RegistrationRecord>();

		class RegistrationRecord
		{
			public object tag;
			public int scanline;
			public Action<int> callback;
		}
	}

	public unsafe class LibsnesCore : IEmulator, IVideoProvider
	{
		public bool IsSGB { get; private set; }

		/// <summary>disable all external callbacks.  the front end should not even know the core is frame advancing</summary>
		bool nocallbacks = false;

		bool disposed = false;
		public void Dispose()
		{
			if (disposed) return;
			disposed = true;

			disposedSaveRam = ReadSaveRam();

			api.snes_unload_cartridge();
			api.snes_term();

			resampler.Dispose();
			api.Dispose();
		}
		//save the save memory before disposing the core, so we can pull from it in the future after the core is terminated
		//that will be necessary to get it saving to disk
		byte[] disposedSaveRam;

		//we can only have one active snes core at a time, due to libsnes being so static.
		//so we'll track the current one here and detach the previous one whenever a new one is booted up.
		static LibsnesCore CurrLibsnesCore;

		public class MyScanlineHookManager : ScanlineHookManager
		{
			public MyScanlineHookManager(LibsnesCore core)
			{
				this.core = core;
			}
			LibsnesCore core;

			public override void OnHooksChanged()
			{
				core.OnScanlineHooksChanged();
			}
		}
		public MyScanlineHookManager ScanlineHookManager;
		void OnScanlineHooksChanged()
		{
			if (disposed) return;
			if (ScanlineHookManager.HookCount == 0) api.snes_set_scanlineStart(null);
			else api.snes_set_scanlineStart(scanlineStart_cb);
		}

		void snes_scanlineStart(int line)
		{
			ScanlineHookManager.HandleScanline(line);
		}

		string snes_path_request(int slot, string hint)
		{
			//every rom requests this byuu homemade rom
			if (hint == "msu1.rom") return "";

			//build romfilename
			string test = Path.Combine(CoreComm.SNES_FirmwaresPath ?? "", hint);

			//does it exist?
			if (!File.Exists(test))
			{
				System.Windows.Forms.MessageBox.Show("The SNES core is referencing a firmware file which could not be found. Please make sure it's in your configured SNES firmwares folder. The referenced filename is: " + hint);
				return "";
			}

			Console.WriteLine("Served libsnes request for firmware \"{0}\" with \"{1}\"", hint, test);

			//return the path we built
			return test;
		}

		void snes_trace(string msg)
		{
			CoreComm.Tracer.Put(msg);
		}

		public SnesColors.ColorType CurrPalette { get; private set; }

		public void SetPalette(SnesColors.ColorType pal)
		{
			CurrPalette = pal;
			int[] tmp = SnesColors.GetLUT(pal);
			fixed (int* p = &tmp[0])
				api.snes_set_color_lut((IntPtr)p);
		}

		public LibsnesApi api;

		public LibsnesCore(CoreComm comm)
		{
			CoreComm = comm;
			api = new LibsnesApi();
			api.snes_init();
		}

		public void Load(GameInfo game, byte[] romData, byte[] sgbRomData, bool DeterministicEmulation)
		{
			//attach this core as the current
			if (CurrLibsnesCore != null)
				CurrLibsnesCore.Dispose();
			CurrLibsnesCore = this;

			ScanlineHookManager = new MyScanlineHookManager(this);

			api.snes_init();

			vidcb = new LibsnesDll.snes_video_refresh_t(snes_video_refresh);
			api.snes_set_video_refresh(vidcb);

			pollcb = new LibsnesDll.snes_input_poll_t(snes_input_poll);
			api.snes_set_input_poll(pollcb);

			inputcb = new LibsnesDll.snes_input_state_t(snes_input_state);
			api.snes_set_input_state(inputcb);

			notifycb = new LibsnesDll.snes_input_notify_t(snes_input_notify);
			api.snes_set_input_notify(notifycb);

			soundcb = new LibsnesDll.snes_audio_sample_t(snes_audio_sample);
			api.snes_set_audio_sample(soundcb);

			pathRequest_cb = new LibsnesDll.snes_path_request_t(snes_path_request);
			api.snes_set_path_request(pathRequest_cb);

			scanlineStart_cb = new LibsnesDll.snes_scanlineStart_t(snes_scanlineStart);

			tracecb = new LibsnesDll.snes_trace_t(snes_trace);

			// set default palette. Should be overridden by frontend probably
			SetPalette(SnesColors.ColorType.BizHawk);


			// start up audio resampler
			InitAudio();

			//strip header
			if ((romData.Length & 0x7FFF) == 512)
			{
				var newData = new byte[romData.Length - 512];
				Array.Copy(romData, 512, newData, 0, newData.Length);
				romData = newData;
			}

			if (game["SGB"])
			{
				IsSGB = true;
				SystemId = "SNES";
				if (!LibsnesDll.snes_load_cartridge_super_game_boy(null, sgbRomData, (uint)sgbRomData.Length, null, romData, (uint)romData.Length))
					throw new Exception("snes_load_cartridge_super_game_boy() failed");
			}
			else
			{
				SystemId = "SNES";
				if (!api.snes_load_cartridge_normal(null, romData))
					throw new Exception("snes_load_cartridge_normal() failed");
			}

			if (api.snes_get_region() == LibsnesDll.SNES_REGION.NTSC)
			{
				//similar to what aviout reports from snes9x and seems logical from bsnes first principles. bsnes uses that numerator (ntsc master clockrate) for sure.
				CoreComm.VsyncNum = 21477272;
				CoreComm.VsyncDen = 4 * 341 * 262;
			}
			else
			{
				CoreComm.VsyncNum = 50;
				CoreComm.VsyncDen = 1;
			}

			CoreComm.CpuTraceAvailable = true;

			api.snes_power();

			SetupMemoryDomains(romData);

			this.DeterministicEmulation = DeterministicEmulation;
			if (DeterministicEmulation) // save frame-0 savestate now
			{
				MemoryStream ms = new MemoryStream();
				BinaryWriter bw = new BinaryWriter(ms);
				bw.Write(CoreSaveState());
				bw.Write(true); // framezero, so no controller follows and don't frameadvance on load
				// hack: write fake dummy controller info
				bw.Write(new byte[536]);
				bw.Close();
				savestatebuff = ms.ToArray();
			}
		}

		//must keep references to these so that they wont get garbage collected
		LibsnesDll.snes_video_refresh_t vidcb;
		LibsnesDll.snes_input_poll_t pollcb;
		LibsnesDll.snes_input_state_t inputcb;
		LibsnesDll.snes_input_notify_t notifycb;
		LibsnesDll.snes_audio_sample_t soundcb;
		LibsnesDll.snes_scanlineStart_t scanlineStart_cb;
		LibsnesDll.snes_path_request_t pathRequest_cb;
		LibsnesDll.snes_trace_t tracecb;

		ushort snes_input_state(int port, int device, int index, int id)
		{
			if (!nocallbacks && CoreComm.InputCallback != null) CoreComm.InputCallback();
			//Console.WriteLine("{0} {1} {2} {3}", port, device, index, id);

			string key = "P" + (1 + port) + " ";
			if ((LibsnesDll.SNES_DEVICE)device == LibsnesDll.SNES_DEVICE.JOYPAD)
			{
				switch ((LibsnesDll.SNES_DEVICE_ID)id)
				{
					case LibsnesDll.SNES_DEVICE_ID.JOYPAD_A: key += "A"; break;
					case LibsnesDll.SNES_DEVICE_ID.JOYPAD_B: key += "B"; break;
					case LibsnesDll.SNES_DEVICE_ID.JOYPAD_X: key += "X"; break;
					case LibsnesDll.SNES_DEVICE_ID.JOYPAD_Y: key += "Y"; break;
					case LibsnesDll.SNES_DEVICE_ID.JOYPAD_UP: key += "Up"; break;
					case LibsnesDll.SNES_DEVICE_ID.JOYPAD_DOWN: key += "Down"; break;
					case LibsnesDll.SNES_DEVICE_ID.JOYPAD_LEFT: key += "Left"; break;
					case LibsnesDll.SNES_DEVICE_ID.JOYPAD_RIGHT: key += "Right"; break;
					case LibsnesDll.SNES_DEVICE_ID.JOYPAD_L: key += "L"; break;
					case LibsnesDll.SNES_DEVICE_ID.JOYPAD_R: key += "R"; break;
					case LibsnesDll.SNES_DEVICE_ID.JOYPAD_SELECT: key += "Select"; break;
					case LibsnesDll.SNES_DEVICE_ID.JOYPAD_START: key += "Start"; break;
					default: return 0;
				}

				return (ushort)(Controller[key] ? 1 : 0);
			}

			return 0;

		}

		void snes_input_poll()
		{
			// this doesn't actually correspond to anything in the underlying bsnes;
			// it gets called once per frame with video_refresh() and has nothing to do with anything
		}

		void snes_input_notify(int index)
		{
			IsLagFrame = false;
		}

		int field = 0;
		void snes_video_refresh(int* data, int width, int height)
		{
			vidWidth = width;
			vidHeight = height;

			//if we are in high-res mode, we get double width. so, lets double the height here to keep it square.
			//TODO - does interlacing have something to do with the correct way to handle this? need an example that turns it on.
			int yskip = 1;
			if (width == 512)
			{
				vidHeight *= 2;
				yskip = 2;
			}

			int srcPitch = 1024;
			int srcStart = 0;

			//for interlaced mode, we're gonna alternate fields. you know, like we're supposed to
			bool interlaced = (height == 478 || height == 448);
			if (interlaced)
			{
				srcPitch = 1024;
				if (field == 1)
					srcStart = 512; //start on second field
				//really only half as high as the video output
				vidHeight /= 2;
				height /= 2;
				//alternate fields
				field ^= 1;
			}

			int size = vidWidth * vidHeight;
			if (vidBuffer.Length != size)
				vidBuffer = new int[size];


			for (int y = 0; y < height; y++)
				for (int x = 0; x < width; x++)
				{
					int si = y * srcPitch + x + srcStart;
					int di = y * vidWidth * yskip + x;
					int rgb = data[si];
					vidBuffer[di] = rgb;
				}

			//alternate scanlines
			if (width == 512)
				for (int y = 0; y < height; y++)
					for (int x = 0; x < width; x++)
					{
						int si = y * 1024 + x;
						int di = y * vidWidth * yskip + x + 512;
						int rgb = data[si];
						vidBuffer[di] = rgb;
					}
		}

		public void FrameAdvance(bool render, bool rendersound)
		{
			api.MessageCounter = 0;

			// for deterministic emulation, save the state we're going to use before frame advance
			// don't do this during nocallbacks though, since it's already been done
			if (!nocallbacks && DeterministicEmulation)
			{
				MemoryStream ms = new MemoryStream();
				BinaryWriter bw = new BinaryWriter(ms);
				bw.Write(CoreSaveState());
				bw.Write(false); // not framezero
				SnesSaveController ssc = new SnesSaveController();
				ssc.CopyFrom(Controller);
				ssc.Serialize(bw);
				bw.Close();
				savestatebuff = ms.ToArray();
			}

			if (!nocallbacks && CoreComm.Tracer.Enabled)
				api.snes_set_trace_callback(tracecb);
			else
				api.snes_set_trace_callback(null);

			// speedup when sound rendering is not needed
			if (!rendersound)
				api.snes_set_audio_sample(null);
			else
				api.snes_set_audio_sample(soundcb);

			bool resetSignal = Controller["Reset"];
			if (resetSignal) api.snes_reset();

			bool powerSignal = Controller["Power"];
			if (powerSignal) api.snes_power();

			//too many messages
			api.snes_set_layer_enable(0, 0, CoreComm.SNES_ShowBG1_0);
			api.snes_set_layer_enable(0, 1, CoreComm.SNES_ShowBG1_1);
			api.snes_set_layer_enable(1, 0, CoreComm.SNES_ShowBG2_0);
			api.snes_set_layer_enable(1, 1, CoreComm.SNES_ShowBG2_1);
			api.snes_set_layer_enable(2, 0, CoreComm.SNES_ShowBG3_0);
			api.snes_set_layer_enable(2, 1, CoreComm.SNES_ShowBG3_1);
			api.snes_set_layer_enable(3, 0, CoreComm.SNES_ShowBG4_0);
			api.snes_set_layer_enable(3, 1, CoreComm.SNES_ShowBG4_1);
			api.snes_set_layer_enable(4, 0, CoreComm.SNES_ShowOBJ_0);
			api.snes_set_layer_enable(4, 1, CoreComm.SNES_ShowOBJ_1);
			api.snes_set_layer_enable(4, 2, CoreComm.SNES_ShowOBJ_2);
			api.snes_set_layer_enable(4, 3, CoreComm.SNES_ShowOBJ_3);

			// if the input poll callback is called, it will set this to false
			IsLagFrame = true;

			//apparently this is one frame?
			timeFrameCounter++;
			api.snes_run();

			while (api.HasMessage)
				Console.WriteLine(api.DequeueMessage());

			if (IsLagFrame)
				LagCount++;

			//diagnostics for IPC traffic
			//Console.WriteLine(api.MessageCounter);
		}

		public DisplayType DisplayType
		{
			get
			{
				if (api.snes_get_region() == LibsnesDll.SNES_REGION.NTSC)
					return BizHawk.DisplayType.NTSC;
				else
					return BizHawk.DisplayType.PAL;
			}
		}

		//video provider
		int IVideoProvider.BackgroundColor { get { return 0; } }
		int[] IVideoProvider.GetVideoBuffer() { return vidBuffer; }
		int IVideoProvider.VirtualWidth { get { return vidWidth; } }
		int IVideoProvider.BufferWidth { get { return vidWidth; } }
		int IVideoProvider.BufferHeight { get { return vidHeight; } }

		int[] vidBuffer = new int[256 * 224];
		int vidWidth = 256, vidHeight = 224;

		public IVideoProvider VideoProvider { get { return this; } }

		public ControllerDefinition ControllerDefinition { get { return SNESController; } }
		IController controller;
		public IController Controller
		{
			get { return controller; }
			set { controller = value; }
		}

		public static readonly ControllerDefinition SNESController =
			new ControllerDefinition
			{
				Name = "SNES Controller",
				BoolButtons = {
					"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Select", "P1 Start", "P1 B", "P1 A", "P1 X", "P1 Y", "P1 L", "P1 R", "Reset", "Power",
					"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Select", "P2 Start", "P2 B", "P2 A", "P2 X", "P2 Y", "P2 L", "P2 R",
					"P3 Up", "P3 Down", "P3 Left", "P3 Right", "P3 Select", "P3 Start", "P3 B", "P3 A", "P3 X", "P3 Y", "P3 L", "P3 R",
					"P4 Up", "P4 Down", "P4 Left", "P4 Right", "P4 Select", "P4 Start", "P4 B", "P4 A", "P4 X", "P4 Y", "P4 L", "P4 R",
				}
			};

		int timeFrameCounter;
		public int Frame { get { return timeFrameCounter; } set { timeFrameCounter = value; } }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; private set; }
		public string SystemId { get; private set; }

		public bool DeterministicEmulation
		{
			get;
			private set;
		}


		public bool SaveRamModified
		{
			set { }
			get
			{
				return LibsnesDll.snes_get_memory_size(LibsnesDll.SNES_MEMORY.CARTRIDGE_RAM) != 0;
			}
		}

		public byte[] ReadSaveRam()
		{
			if (disposedSaveRam != null) return disposedSaveRam;
			return snes_get_memory_data_read(LibsnesDll.SNES_MEMORY.CARTRIDGE_RAM);
		}

		public static byte[] snes_get_memory_data_read(LibsnesDll.SNES_MEMORY id)
		{
			var size = (int)LibsnesDll.snes_get_memory_size(id);
			if (size == 0) return new byte[0];
			var data = LibsnesDll.snes_get_memory_data(id);
			var ret = new byte[size];
			Marshal.Copy(data, ret, 0, size);
			return ret;
		}

		public void StoreSaveRam(byte[] data)
		{
			var size = (int)LibsnesDll.snes_get_memory_size(LibsnesDll.SNES_MEMORY.CARTRIDGE_RAM);
			if (size == 0) return;
			var emudata = LibsnesDll.snes_get_memory_data(LibsnesDll.SNES_MEMORY.CARTRIDGE_RAM);
			Marshal.Copy(data, 0, emudata, size);
		}

		public void ClearSaveRam()
		{
			byte[] cleardata = new byte[(int)LibsnesDll.snes_get_memory_size(LibsnesDll.SNES_MEMORY.CARTRIDGE_RAM)];
			StoreSaveRam(cleardata);
		}

		public void ResetFrameCounter()
		{
			timeFrameCounter = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		#region savestates

		/// <summary>
		/// can freeze a copy of a controller input set and serialize\deserialize it
		/// </summary>
		class SnesSaveController : IController
		{
			// this is all rather general, so perhaps should be moved out of LibsnesCore

			ControllerDefinition def;

			public SnesSaveController()
			{
				this.def = null;
			}

			public SnesSaveController(ControllerDefinition def)
			{
				this.def = def;
			}

			WorkingDictionary<string, float> buttons = new WorkingDictionary<string,float>();

			/// <summary>
			/// invalid until CopyFrom has been called
			/// </summary>
			public ControllerDefinition Type
			{
				get { return def; }
			}

			public void Serialize(BinaryWriter b)
			{
				b.Write(buttons.Keys.Count);
				foreach (var k in buttons.Keys)
				{
					b.Write(k);
					b.Write(buttons[k]);
				}
			}

			/// <summary>
			/// no checking to see if the deserialized controls match any definition
			/// </summary>
			/// <param name="b"></param>
			public void DeSerialize(BinaryReader b)
			{
				buttons.Clear();
				int numbuttons = b.ReadInt32();
				for (int i = 0; i < numbuttons; i++)
				{
					string k = b.ReadString();
					float v = b.ReadSingle();
					buttons.Add(k, v);
				}
			}
			
			/// <summary>
			/// this controller's definition changes to that of source
			/// </summary>
			/// <param name="source"></param>
			public void CopyFrom(IController source)
			{
				this.def = source.Type;
				buttons.Clear();
				foreach (var k in def.BoolButtons)
					buttons.Add(k, source.IsPressed(k) ? 1.0f : 0);
				foreach (var k in def.FloatControls)
				{
					if (buttons.Keys.Contains(k))
						throw new Exception("name collision between bool and float lists!");
					buttons.Add(k, source.GetFloat(k));
				}
			}

			public bool this[string button]
			{
				get { return buttons[button] != 0; }
			}

			public bool IsPressed(string button)
			{
				return buttons[button] != 0;
			}

			public float GetFloat(string name)
			{
				return buttons[name];
			}

			public void UpdateControls(int frame)
			{
				throw new NotImplementedException();
			}
		}


		public void SaveStateText(TextWriter writer)
		{
			var temp = SaveStateBinary();
			temp.SaveAsHex(writer);
			// write extra copy of stuff we don't use
			writer.WriteLine("Frame {0}", Frame);
		}
		public void LoadStateText(TextReader reader)
		{
			string hex = reader.ReadLine();
			byte[] state = new byte[hex.Length / 2];
			state.ReadFromHex(hex);
			LoadStateBinary(new BinaryReader(new MemoryStream(state)));
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			if (!DeterministicEmulation)
				writer.Write(CoreSaveState());
			else
				writer.Write(savestatebuff);

			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);

			writer.Flush();
		}
		public void LoadStateBinary(BinaryReader reader)
		{
			int size = api.snes_serialize_size();
			byte[] buf = reader.ReadBytes(size);
			CoreLoadState(buf);

			if (DeterministicEmulation) // deserialize controller and fast-foward now
			{
				// reconstruct savestatebuff at the same time to avoid a costly core serialize
				MemoryStream ms = new MemoryStream();
				BinaryWriter bw = new BinaryWriter(ms);
				bw.Write(buf);
				bool framezero = reader.ReadBoolean();
				bw.Write(framezero);
				if (!framezero)
				{
					SnesSaveController ssc = new SnesSaveController(ControllerDefinition);
					ssc.DeSerialize(reader);
					IController tmp = this.Controller;
					this.Controller = ssc;
					nocallbacks = true;
					FrameAdvance(false, false);
					nocallbacks = false;
					this.Controller = tmp;
					ssc.Serialize(bw);
				}
				else // hack: dummy controller info
				{
					bw.Write(reader.ReadBytes(536));
				}
				bw.Close();
				savestatebuff = ms.ToArray();
			}

			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
		}
		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		/// <summary>
		/// handle the unmanaged part of loadstating
		/// </summary>
		void CoreLoadState(byte[] data)
		{
			int size = api.snes_serialize_size();
			if (data.Length != size)
				throw new Exception("Libsnes internal savestate size mismatch!");
			api.snes_init();
			fixed (byte* pbuf = &data[0])
				api.snes_unserialize(new IntPtr(pbuf), size);
		}
		/// <summary>
		/// handle the unmanaged part of savestating
		/// </summary>
		byte[] CoreSaveState()
		{
			int size = api.snes_serialize_size();
			byte[] buf = new byte[size];
			fixed (byte* pbuf = &buf[0])
				api.snes_serialize(new IntPtr(pbuf), size);
			return buf;
		}

		/// <summary>
		/// most recent internal savestate, for deterministic mode ONLY
		/// </summary>
		byte[] savestatebuff;

		#endregion

		public CoreComm CoreComm { get; private set; }

		// ----- Client Debugging API stuff -----
		unsafe MemoryDomain MakeMemoryDomain(string name, LibsnesDll.SNES_MEMORY id, Endian endian)
		{
			int size = api.snes_get_memory_size(id);
			int mask = size - 1;
			MemoryDomain md;

			////have to bitmask these somehow because it's unmanaged memory and we would hate to clobber things or make them nondeterministic
			//if (Util.IsPowerOfTwo(size))
			//{
			//  //can &mask for speed
			//  md = new MemoryDomain(name, size, endian,
			//     (addr) => blockptr[addr & mask],
			//     (addr, value) => blockptr[addr & mask] = value);
			//}
			//else
			//{
			//  //have to use % (only OAM needs this, it seems)
			//  //(OAM is actually two differently sized banks of memory which arent truly considered adjacent. maybe a better way to visualize it would be with an empty bus and adjacent banks)
			//  md = new MemoryDomain(name, size, endian,
			//     (addr) => blockptr[addr % size],
			//     (addr, value) => blockptr[addr % size] = value);
			//}

			//EXTERNAL PROCESS CONVERSION: MUST MAKE THIS SAFE
			//speed it up later
			if (Util.IsPowerOfTwo(size))
			{
				//can &mask for speed
				md = new MemoryDomain(name, size, endian,
					 (addr) => api.peek(id, (uint)(addr & mask)),
					 (addr, value) => api.poke(id, (uint)(addr & mask), value));
			}
			else
			{
				//have to use % (only OAM needs this, it seems)
				//(OAM is actually two differently sized banks of memory which arent truly considered adjacent. maybe a better way to visualize it would be with an empty bus and adjacent banks)
				md = new MemoryDomain(name, size, endian,
				 (addr) => api.peek(id, (uint)(addr % size)),
					 (addr, value) => api.poke(id, (uint)(addr % size), value));
			}

			MemoryDomains.Add(md);

			return md;


		}

		void SetupMemoryDomains(byte[] romData)
		{
			MemoryDomains = new List<MemoryDomain>();

			var romDomain = new MemoryDomain("CARTROM", romData.Length, Endian.Little,
				(addr) => romData[addr],
				(addr, value) => romData[addr] = value);

			MainMemory = MakeMemoryDomain("WRAM", LibsnesDll.SNES_MEMORY.WRAM, Endian.Little);
			MemoryDomains.Add(romDomain);
			MakeMemoryDomain("CARTRAM", LibsnesDll.SNES_MEMORY.CARTRIDGE_RAM, Endian.Little);
			MakeMemoryDomain("VRAM", LibsnesDll.SNES_MEMORY.VRAM, Endian.Little);
			MakeMemoryDomain("OAM", LibsnesDll.SNES_MEMORY.OAM, Endian.Little);
			MakeMemoryDomain("CGRAM", LibsnesDll.SNES_MEMORY.CGRAM, Endian.Little);
			MakeMemoryDomain("APURAM", LibsnesDll.SNES_MEMORY.APURAM, Endian.Little);

			if (!DeterministicEmulation)
				MemoryDomains.Add(new MemoryDomain("BUS", 0x1000000, Endian.Little,
					(addr) => api.peek(LibsnesDll.SNES_MEMORY.SYSBUS, (uint)addr),
					(addr, val) => api.poke(LibsnesDll.SNES_MEMORY.SYSBUS, (uint)addr, val)));
		}
		public IList<MemoryDomain> MemoryDomains { get; private set; }
		public MemoryDomain MainMemory { get; private set; }

		#region audio stuff

		Sound.Utilities.SpeexResampler resampler;

		void InitAudio()
		{
			resampler = new Sound.Utilities.SpeexResampler(6, 64081, 88200, 32041, 44100);
		}

		void snes_audio_sample(ushort left, ushort right)
		{
			resampler.EnqueueSample((short)left, (short)right);
		}

		public ISoundProvider SoundProvider { get { return null; } }
		public ISyncSoundProvider SyncSoundProvider { get { return resampler; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }

		#endregion audio stuff
	}
}
