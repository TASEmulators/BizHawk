using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Common;
using BizHawk.Emulation.Cores.Waterbox;
using BizHawk.BizInvoke;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public abstract unsafe class CoreImpl
	{
		[BizImport(CallingConvention.Cdecl, Compatibility = true)]
		public abstract IntPtr DllInit();
		[BizImport(CallingConvention.Cdecl, Compatibility = true)]
		public abstract void Message(LibsnesApi.eMessage msg);
		[BizImport(CallingConvention.Cdecl, Compatibility = true)]
		public abstract void CopyBuffer(int id, void* ptr, int size);
		[BizImport(CallingConvention.Cdecl, Compatibility = true)]
		public abstract void SetBuffer(int id, void* ptr, int size);
		[BizImport(CallingConvention.Cdecl)]
		public abstract void PostLoadState();
	}

	public unsafe partial class LibsnesApi : IDisposable, IMonitor, IStatable
	{
		static LibsnesApi()
		{
			if (sizeof(CommStruct) != 368)
			{
				throw new InvalidOperationException("sizeof(comm)");
			}
		}

		private WaterboxHost _exe;
		private CoreImpl _core;
		private bool _disposed;
		private CommStruct* _comm;
		private readonly Dictionary<string, IntPtr> _sharedMemoryBlocks = new Dictionary<string, IntPtr>();
		private bool _sealed = false;

		public void Enter()
		{
			_exe.Enter();
		}

		public void Exit()
		{
			_exe.Exit();
		}

		private readonly List<string> _readonlyFiles = new List<string>();

		public void AddReadonlyFile(byte[] data, string name)
		{
			_exe.AddReadonlyFile(data, name);
			_readonlyFiles.Add(name);
		}

		public LibsnesApi(string dllPath, CoreComm comm, IEnumerable<Delegate> allCallbacks)
		{
			_exe = new WaterboxHost(new WaterboxOptions
			{
				Filename = "libsnes.wbx",
				Path = dllPath,
				SbrkHeapSizeKB = 4 * 1024,
				InvisibleHeapSizeKB = 8 * 1024,
				MmapHeapSizeKB = 32 * 1024, // TODO: see if we can safely make libco stacks smaller
				PlainHeapSizeKB = 32 * 1024, // TODO: This can be smaller, probably; needs to be as big as largest ROM + 2MB, or less
				SealedHeapSizeKB = 80 * 1024,
				SkipCoreConsistencyCheck = comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			});
			using (_exe.EnterExit())
			{
				// Marshal checks that function pointers passed to GetDelegateForFunctionPointer are
				// _currently_ valid when created, even though they don't need to be valid until
				// the delegate is later invoked.  so GetInvoker needs to be acquired within a lock.
				_core = BizInvoker.GetInvoker<CoreImpl>(_exe, _exe, CallingConventionAdapters.MakeWaterbox(allCallbacks, _exe));
				_comm = (CommStruct*)_core.DllInit().ToPointer();
			}
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;
				_exe.Dispose();
				_exe = null;
				_core = null;
				_comm = null;
			}
		}

		/// <summary>
		/// Copy an ascii string into libretro. It keeps the copy.
		/// </summary>
		public void CopyAscii(int id, string str)
		{
			fixed (byte* cp = System.Text.Encoding.ASCII.GetBytes(str + "\0"))
			{
				_core.CopyBuffer(id, cp, str.Length + 1);
			}
		}

		/// <summary>
		/// Copy a buffer into libretro. It keeps the copy.
		/// </summary>
		public void CopyBytes(int id, byte[] bytes)
		{
			fixed (byte* bp = bytes)
			{
				_core.CopyBuffer(id, bp, bytes.Length);
			}
		}

		/// <summary>
		/// Locks a buffer and sets it into libretro. You must pass a delegate to be executed while that buffer is locked.
		/// This is meant to be used for avoiding a memcpy for large roms (which the core is then just going to memcpy again on its own)
		/// The memcpy has to happen at some point (libretro semantics specify [not literally, the docs don't say] that the core should finish using the buffer before its init returns)
		/// but this limits it to once.
		/// Moreover, this keeps the c++ side from having to free strings when they're no longer used (and memory management is trickier there, so we try to avoid it)
		/// </summary>
		public void SetBytes(int id, byte[] bytes, Action andThen)
		{
			if (_sealed)
				throw new InvalidOperationException("Init period is over");
			fixed (byte* bp = bytes)
			{
				_core.SetBuffer(id, bp, bytes.Length);
				andThen();
			}
		}

		/// <summary>
		/// see SetBytes
		/// </summary>
		public void SetAscii(int id, string str, Action andThen)
		{
			if (_sealed)
				throw new InvalidOperationException("Init period is over");
			fixed (byte* cp = System.Text.Encoding.ASCII.GetBytes(str + "\0"))
			{
				_core.SetBuffer(id, cp, str.Length + 1);
				andThen();
			}
		}

		public Action<uint> ReadHook, ExecHook;
		public Action<uint, byte> WriteHook;

		public Action<uint> ReadHook_SMP, ExecHook_SMP;
		public Action<uint, byte> WriteHook_SMP;

		public enum eCDLog_AddrType
		{
			CARTROM, CARTRAM, WRAM, APURAM,
			SGB_CARTROM, SGB_CARTRAM, SGB_WRAM, SGB_HRAM,
			NUM
		}

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
		}

		private snes_video_refresh_t video_refresh;
		private snes_input_poll_t input_poll;
		private snes_input_state_t input_state;
		private snes_input_notify_t input_notify;
		private snes_audio_sample_t audio_sample;
		private snes_scanlineStart_t scanlineStart;
		private snes_path_request_t pathRequest;
		private snes_trace_t traceCallback;

		public void QUERY_set_video_refresh(snes_video_refresh_t video_refresh) { this.video_refresh = video_refresh; }
		// not used??
		public void QUERY_set_input_poll(snes_input_poll_t input_poll) { this.input_poll = input_poll; }
		public void QUERY_set_input_state(snes_input_state_t input_state) { this.input_state = input_state; }
		public void QUERY_set_input_notify(snes_input_notify_t input_notify) { this.input_notify = input_notify; }
		public void QUERY_set_path_request(snes_path_request_t pathRequest) { this.pathRequest = pathRequest; }

		public delegate void snes_video_refresh_t(int* data, int width, int height);
		public delegate void snes_input_poll_t();
		public delegate short snes_input_state_t(int port, int device, int index, int id);
		public delegate void snes_input_notify_t(int index);
		public delegate void snes_audio_sample_t(short left, short right);
		public delegate void snes_scanlineStart_t(int line);
		public delegate string snes_path_request_t(int slot, string hint);
		public delegate void snes_trace_t(uint which, string msg);


		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct CPURegs
		{
			public uint pc;
			public ushort a, x, y, s, d, vector; //7x
			public byte p, db, nothing, nothing2;
			public ushort v, h;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct LayerEnables
		{
			private byte _BG1_Prio0, _BG1_Prio1;
			private byte _BG2_Prio0, _BG2_Prio1;
			private byte _BG3_Prio0, _BG3_Prio1;
			private byte _BG4_Prio0, _BG4_Prio1;
			private byte _Obj_Prio0, _Obj_Prio1, _Obj_Prio2, _Obj_Prio3;

			public bool BG1_Prio0
			{
				get => _BG1_Prio0 != 0;
				set => _BG1_Prio0 = (byte)(value ? 1 : 0);
			}
			public bool BG1_Prio1
			{
				get => _BG1_Prio1 != 0;
				set => _BG1_Prio1 = (byte)(value ? 1 : 0);
			}
			public bool BG2_Prio0
			{
				get => _BG2_Prio0 != 0;
				set => _BG2_Prio0 = (byte)(value ? 1 : 0);
			}
			public bool BG2_Prio1
			{
				get => _BG2_Prio1 != 0;
				set => _BG2_Prio1 = (byte)(value ? 1 : 0);
			}
			public bool BG3_Prio0
			{
				get => _BG3_Prio0 != 0;
				set => _BG3_Prio0 = (byte)(value ? 1 : 0);
			}
			public bool BG3_Prio1
			{
				get => _BG3_Prio1 != 0;
				set => _BG3_Prio1 = (byte)(value ? 1 : 0);
			}
			public bool BG4_Prio0
			{
				get => _BG4_Prio0 != 0;
				set => _BG4_Prio0 = (byte)(value ? 1 : 0);
			}
			public bool BG4_Prio1
			{
				get => _BG4_Prio1 != 0;
				set => _BG4_Prio1 = (byte)(value ? 1 : 0);
			}

			public bool Obj_Prio0
			{
				get => _Obj_Prio0 != 0;
				set => _Obj_Prio0 = (byte)(value ? 1 : 0);
			}
			public bool Obj_Prio1
			{
				get => _Obj_Prio1 != 0;
				set => _Obj_Prio1 = (byte)(value ? 1 : 0);
			}
			public bool Obj_Prio2
			{
				get => _Obj_Prio2 != 0;
				set => _Obj_Prio2 = (byte)(value ? 1 : 0);
			}
			public bool Obj_Prio3
			{
				get => _Obj_Prio3 != 0;
				set => _Obj_Prio3 = (byte)(value ? 1 : 0);
			}
		}

		[StructLayout(LayoutKind.Explicit)]
		private struct CommStruct
		{
			[FieldOffset(0)]
			//the cmd being executed
			public readonly eMessage cmd;
			[FieldOffset(4)]
			//the status of the core
			public readonly eStatus status;
			[FieldOffset(8)]
			//the SIG or BRK that the core is halted in
			public readonly eMessage reason;

			//flexible in/out parameters
			//these are all "overloaded" a little so it isn't clear what's used for what in for any particular message..
			//but I think it will beat having to have some kind of extremely verbose custom layouts for every message
			[FieldOffset(16)]
			public sbyte* str;
			[FieldOffset(24)]
			public void* ptr;
			[FieldOffset(32)]
			public uint id;
			[FieldOffset(36)]
			public uint addr;
			[FieldOffset(40)]
			public uint value;
			[FieldOffset(44)]
			public uint size;
			[FieldOffset(48)]
			public int port;
			[FieldOffset(52)]
			public int device;
			[FieldOffset(56)]
			public int index;
			[FieldOffset(60)]
			public int slot;
			[FieldOffset(64)]
			public int width;
			[FieldOffset(68)]
			public int height;
			[FieldOffset(72)]
			public int scanline;
			[FieldOffset(76)]
			public fixed int inports[2];

			[FieldOffset(88)]
			//this should always be used in pairs
			public fixed long buf[3]; //ACTUALLY A POINTER but can't marshal it :(
			[FieldOffset(112)]
			public fixed int buf_size[3];

			[FieldOffset(128)]
			//bleck. this is a long so that it can be a 32/64bit pointer
			public fixed long cdl_ptr[16];
			[FieldOffset(256)]
			public fixed int cdl_size[16];

			[FieldOffset(320)]
			public CPURegs cpuregs;
			[FieldOffset(344)]
			public LayerEnables layerEnables;

			[FieldOffset(356)]
			//static configuration-type information which can be grabbed off the core at any time without even needing a QUERY command
			public SNES_REGION region;
			[FieldOffset(360)]
			public SNES_MAPPER mapper;

			[FieldOffset(364)] private uint BLANK0;


			//utilities
			//TODO: make internal, wrap on the API instead of the comm
			public string GetAscii() => _getAscii(str);
			public bool GetBool() { return value != 0; }

			private string _getAscii(sbyte* ptr)
			{
				int len = 0;
				sbyte* junko = ptr;
				while (junko[len] != 0) len++;

				return new string(str, 0, len, System.Text.Encoding.ASCII);
			}
		}

		public SNES_REGION Region
		{
			get
			{
				using (_exe.EnterExit())
				{
					return _comm->region;
				}
			}
		}
		public SNES_MAPPER Mapper
		{
			get
			{
				using (_exe.EnterExit())
				{
					return _comm->mapper;
				}
			}
		}

		public void SetLayerEnables(ref LayerEnables enables)
		{
			using (_exe.EnterExit())
			{
				_comm->layerEnables = enables;
				QUERY_set_layer_enable();
			}
		}

		public void SetInputPortBeforeInit(int port, SNES_INPUT_PORT type)
		{
			using (_exe.EnterExit())
			{
				_comm->inports[port] = (int)type;
			}
		}

		public void Seal()
		{
			/* Cothreads can very easily acquire "pointer poison"; because their stack and even registers
			 * are part of state, any poisoned pointer that's used even temporarily might be persisted longer
			 * than needed.  Most of the libsnes core cothreads handle internal matters only and aren't very
			 * vulnerable to pointer poison, but the main boss cothread is used heavily during init, when
			 * many syscalls happen and many kinds of poison can end up on the stack.  so here, we call
			 * _core.DllInit() again, which recreates that cothread, zeroing out all of the memory first,
			 * as well as zeroing out the comm struct. */
			_core.DllInit();
			_exe.Seal();
			_sealed = true;
			foreach (var s in _readonlyFiles)
			{
				_exe.RemoveReadonlyFile(s);
			}
			_readonlyFiles.Clear();
		}

		public bool AvoidRewind => false;

		public void SaveStateBinary(BinaryWriter writer)
		{
			_exe.SaveStateBinary(writer);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			_exe.LoadStateBinary(reader);
			_core.PostLoadState();
		}

		public MemoryDomain GetPagesDomain()
		{
			return _exe.GetPagesDomain();
		}
	}
}
