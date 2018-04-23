using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public unsafe partial class LibsnesApi : IDisposable
	{
		InstanceDll instanceDll;
		string InstanceName;

		[DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
		public static unsafe extern void* CopyMemory(void* dest, void* src, ulong count);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate IntPtr DllInit();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void MessageApi(eMessage msg);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void BufferApi(int id, void* ptr, int size);

		CommStruct* comm;
		MessageApi Message;
		BufferApi _copyBuffer; //TODO: consider making private and wrapping
		BufferApi _setBuffer; //TODO: consider making private and wrapping

		public LibsnesApi(string dllPath)
		{
			InstanceName = "libsneshawk_" + Guid.NewGuid().ToString();
			instanceDll = new InstanceDll(dllPath);
			var dllinit = (DllInit)Marshal.GetDelegateForFunctionPointer(instanceDll.GetProcAddress("DllInit"), typeof(DllInit));
			Message = (MessageApi)Marshal.GetDelegateForFunctionPointer(instanceDll.GetProcAddress("Message"), typeof(MessageApi));
			_copyBuffer = (BufferApi)Marshal.GetDelegateForFunctionPointer(instanceDll.GetProcAddress("CopyBuffer"), typeof(BufferApi));
			_setBuffer = (BufferApi)Marshal.GetDelegateForFunctionPointer(instanceDll.GetProcAddress("SetBuffer"), typeof(BufferApi));

			comm = (CommStruct*)dllinit().ToPointer();
		}

		public void Dispose()
		{
			instanceDll.Dispose();

			foreach (var smb in DeallocatedMemoryBlocks.Values) smb.Dispose();
			foreach (var smb in SharedMemoryBlocks.Values) smb.Dispose();
			SharedMemoryBlocks.Clear();
			DeallocatedMemoryBlocks.Clear();
		}

		/// <summary>
		/// Copy an ascii string into libretro. It keeps the copy.
		/// </summary>
		public void CopyAscii(int id, string str)
		{
			fixed (byte* cp = System.Text.Encoding.ASCII.GetBytes(str+"\0"))
				_copyBuffer(id, cp, str.Length + 1);
		}

		/// <summary>
		/// Copy a buffer into libretro. It keeps the copy.
		/// </summary>
		public void CopyBytes(int id, byte[] bytes)
		{
			fixed (byte* bp = bytes)
				_copyBuffer(id, bp, bytes.Length);
		}

		/// <summary>
		/// Locks a buffer and sets it into libretro. You must pass a delegate to be executed while that buffer is locked.
		/// This is meant to be used for avoiding a memcpy for large roms (which the core is then just going to memcpy again on its own)
		/// The memcpy has to happen at some point (libretro semantics specify [not literally, the docs dont say] that the core should finish using the buffer before its init returns)
		/// but this limits it to once.
		/// Moreover, this keeps the c++ side from having to free strings when they're no longer used (and memory management is trickier there, so we try to avoid it)
		/// </summary>
		public void SetBytes(int id, byte[] bytes, Action andThen)
		{
			fixed (byte* bp = bytes)
			{
				_setBuffer(id, bp, bytes.Length);
				andThen();
			}
		}

		/// <summary>
		/// see SetBytes
		/// </summary>
		public void SetAscii(int id, string str, Action andThen)
		{
			fixed (byte* cp = System.Text.Encoding.ASCII.GetBytes(str+"\0"))
			{
				_setBuffer(id, cp, str.Length + 1);
				andThen();
			}
		}

		public Action<uint> ReadHook, ExecHook;
		public Action<uint, byte> WriteHook;

		public enum eCDLog_AddrType
		{
			CARTROM, CARTRAM, WRAM, APURAM,
			SGB_CARTROM, SGB_CARTRAM, SGB_WRAM, SGB_HRAM,
			NUM
		};

		public enum eTRACE : uint
		{
			CPU = 0,
			SMP = 1,
			GB = 2
		}

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
		public delegate short snes_input_state_t(int port, int device, int index, int id);
		public delegate void snes_input_notify_t(int index);
		public delegate void snes_audio_sample_t(ushort left, ushort right);
		public delegate void snes_scanlineStart_t(int line);
		public delegate string snes_path_request_t(int slot, string hint);
		public delegate void snes_trace_t(uint which, string msg);


		public struct CPURegs
		{
			public uint pc;
			public ushort a, x, y, z, s, d, vector; //7x
			public byte p, nothing;
			public uint aa, rd;
			public byte sp, dp, db, mdr;
			public ushort vcounter, hcounter;
		}

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
			public fixed uint buf[3]; //ACTUALLY A POINTER but can't marshal it :(
			public fixed int buf_size[3];

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
