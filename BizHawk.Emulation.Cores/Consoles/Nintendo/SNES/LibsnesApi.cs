using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO.MemoryMappedFiles;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public unsafe partial class LibsnesApi : IDisposable
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
		//todo - collect all memory block names whenever a memory block is alloc/dealloced. that way we avoid the overhead when using them for gui stuff (gfx debugger, hex editor)


		InstanceDll instanceDll;
		string InstanceName;

		[DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
		public static unsafe extern void* CopyMemory(void* dest, void* src, ulong count);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate CommStruct* DllInit();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void MessageApi(eMessage msg);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void BufferApi(int id, void* ptr, int size);

		CommStruct* comm;
		MessageApi Message;
		BufferApi CopyBuffer; //TODO: consider making private and wrapping
		BufferApi SetBuffer; //TODO: consider making private and wrapping

		public LibsnesApi(string dllPath)
		{
			InstanceName = "libsneshawk_" + Guid.NewGuid().ToString();

			var pipeName = InstanceName;

			instanceDll = new InstanceDll(dllPath);
			var dllinit = (DllInit)Marshal.GetDelegateForFunctionPointer(instanceDll.GetProcAddress("DllInit"), typeof(DllInit));
			Message = (MessageApi)Marshal.GetDelegateForFunctionPointer(instanceDll.GetProcAddress("Message"), typeof(MessageApi));
			CopyBuffer = (BufferApi)Marshal.GetDelegateForFunctionPointer(instanceDll.GetProcAddress("CopyBuffer"), typeof(BufferApi));
			SetBuffer = (BufferApi)Marshal.GetDelegateForFunctionPointer(instanceDll.GetProcAddress("SetBuffer"), typeof(BufferApi));

			comm = dllinit();
		}

		public void Dispose()
		{
			instanceDll.Dispose();

			foreach (var smb in DeallocatedMemoryBlocks.Values) smb.Dispose();
			foreach (var smb in SharedMemoryBlocks.Values) smb.Dispose();
			SharedMemoryBlocks.Clear();
			DeallocatedMemoryBlocks.Clear();
		}

		public void CopyString(string str)
		{
			fixed (char* cp = str)
				CopyBuffer(0, cp, str.Length + 1);
		}

		public void CopyBytes(byte[] bytes)
		{
			fixed (byte* bp = bytes)
				CopyBuffer(0, bp, bytes.Length);
		}

		public void SetAscii(string str)
		{
			fixed (char* cp = str)
				SetBuffer(0, cp, str.Length + 1);
		}

		public void SetBytes(byte[] bytes)
		{
			fixed (byte* bp = bytes)
				SetBuffer(0, bp, bytes.Length);
		}
		public void SetBytes2(byte[] bytes)
		{
			fixed (byte* bp = bytes)
				SetBuffer(1, bp, bytes.Length);
		}

		public Action<uint> ReadHook, ExecHook;
		public Action<uint, byte> WriteHook;

		public enum eCDLog_AddrType
		{
			CARTROM, CARTRAM, WRAM, APURAM,
			NUM
		};

		public enum eCDLog_Flags
		{
			ExecFirst = 0x01,
			ExecOperand = 0x02,
			CPUData = 0x04,
			DMAData = 0x08, //not supported yet
			BRR = 0x80,
		};

		Dictionary<string, SharedMemoryBlock> SharedMemoryBlocks = new Dictionary<string, SharedMemoryBlock>();
		Dictionary<string, SharedMemoryBlock> DeallocatedMemoryBlocks = new Dictionary<string, SharedMemoryBlock>();

		snes_video_refresh_t video_refresh;
		snes_input_poll_t input_poll;
		snes_input_state_t input_state;
		snes_input_notify_t input_notify;
		snes_audio_sample_t audio_sample;
		snes_scanlineStart_t scanlineStart;
		snes_path_request_t pathRequest;
		snes_trace_t traceCallback;

		public void QUERY_set_video_refresh(snes_video_refresh_t video_refresh) { this.video_refresh = video_refresh; }
		public void QUERY_set_input_poll(snes_input_poll_t input_poll) { this.input_poll = input_poll; }
		public void QUERY_set_input_state(snes_input_state_t input_state) { this.input_state = input_state; }
		public void QUERY_set_input_notify(snes_input_notify_t input_notify) { this.input_notify = input_notify; }
		public void QUERY_set_path_request(snes_path_request_t pathRequest) { this.pathRequest = pathRequest; }

		public delegate void snes_video_refresh_t(int* data, int width, int height);
		public delegate void snes_input_poll_t();
		public delegate ushort snes_input_state_t(int port, int device, int index, int id);
		public delegate void snes_input_notify_t(int index);
		public delegate void snes_audio_sample_t(ushort left, ushort right);
		public delegate void snes_scanlineStart_t(int line);
		public delegate string snes_path_request_t(int slot, string hint);
		public delegate void snes_trace_t(string msg);


		[StructLayout(LayoutKind.Sequential)]
		public struct CPURegs
		{
			public uint pc;
			public ushort a, x, y, z, s, d, vector; //7x
			public byte p, nothing;
			public uint aa, rd;
			public byte sp, dp, db, mdr;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct LayerEnables
		{
			byte _BG1_Prio0, _BG1_Prio1;
			byte _BG2_Prio0, _BG2_Prio1;
			byte _BG3_Prio0, _BG3_Prio1;
			byte _BG4_Prio0, _BG4_Prio1;
			byte _Obj_Prio0, _Obj_Prio1, _Obj_Prio2, _Obj_Prio3;

			public bool BG1_Prio0 { get { return _BG1_Prio0 != 0; } set { _BG1_Prio0 = (byte)(value ? 1 : 0); } }
			public bool BG1_Prio1 { get { return _BG1_Prio1 != 0; } set { _BG1_Prio1 = (byte)(value ? 1 : 0); } }
			public bool BG2_Prio0 { get { return _BG2_Prio0 != 0; } set { _BG2_Prio0 = (byte)(value ? 1 : 0); } }
			public bool BG2_Prio1 { get { return _BG2_Prio1 != 0; } set { _BG2_Prio1 = (byte)(value ? 1 : 0); } }
			public bool BG3_Prio0 { get { return _BG3_Prio0 != 0; } set { _BG3_Prio0 = (byte)(value ? 1 : 0); } }
			public bool BG3_Prio1 { get { return _BG3_Prio1 != 0; } set { _BG3_Prio1 = (byte)(value ? 1 : 0); } }
			public bool BG4_Prio0 { get { return _BG4_Prio0 != 0; } set { _BG4_Prio0 = (byte)(value ? 1 : 0); } }
			public bool BG4_Prio1 { get { return _BG4_Prio1 != 0; } set { _BG4_Prio1 = (byte)(value ? 1 : 0); } }
			
			public bool Obj_Prio0 { get { return _Obj_Prio0 != 0; } set { _Obj_Prio0 = (byte)(value ? 1 : 0); } }
			public bool Obj_Prio1 { get { return _Obj_Prio1 != 0; } set { _Obj_Prio1 = (byte)(value ? 1 : 0); } }
			public bool Obj_Prio2 { get { return _Obj_Prio2 != 0; } set { _Obj_Prio2 = (byte)(value ? 1 : 0); } }
			public bool Obj_Prio3 { get { return _Obj_Prio3 != 0; } set { _Obj_Prio3 = (byte)(value ? 1 : 0); } }
		}

		[StructLayout(LayoutKind.Sequential)]
		struct CommStruct
		{
			//the cmd being executed
			public eMessage cmd;

			//the status of the core
			public eStatus status;

			//the SIG or BRK that the core is halted in
			public eMessage reason;

			//flexible in/out parameters
			//these are all "overloaded" a little so it isn't clear what's used for what in for any particular message..
			//but I think it will beat having to have some kind of extremely verbose custom layouts for every message
			public sbyte* str;
			public void* ptr;
			public uint id, addr, value, size;
			public int port, device, index, slot;
			public int width, height;
			public int scanline;
			public fixed int inports[2];

			//this should always be used in pairs
			public void* buf0, buf1;
			public int buf_size0, buf_size1;

			//bleck. this is a long so that it can be a 32/64bit pointer
			public fixed long cdl_ptr[4];
			public fixed int cdl_size[4];

			public CPURegs cpuregs;
			public LayerEnables layerEnables;

			//static configuration-type information which can be grabbed off the core at any time without even needing a QUERY command
			public SNES_REGION region;
			public SNES_MAPPER mapper;

			//utilities
			//TODO: make internal, wrap on the API instead of the comm
			public unsafe string GetAscii() { return _getAscii(str); }
			public bool GetBool() { return value != 0; }

			private unsafe string _getAscii(sbyte* ptr) {
				int len = 0;
				sbyte* junko = (sbyte*)ptr;
				while(junko[len] != 0) len++;

				return new string((sbyte*)str, 0, len, System.Text.Encoding.ASCII);
			}
		}

		public SNES_REGION Region { get { return comm->region; } }
		public SNES_MAPPER Mapper { get { return comm->mapper; } }
		public void SetLayerEnables(ref LayerEnables enables)
		{
			comm->layerEnables = enables;
			QUERY_set_layer_enable();
		}

		public void SetInputPortBeforeInit(int port, SNES_INPUT_PORT type)
		{
			comm->inports[port] = (int)type;
		}
	}

}
