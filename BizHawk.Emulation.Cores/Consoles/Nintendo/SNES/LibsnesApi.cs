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
		/// <summary>
		/// This allows us to push our delegate callbacks to unmanaged code
		/// and not have it garbage collected
		/// </summary>
		List<object> objects = new List<object>();
		T Keep<T>(T obj)
		{
			objects.Add(obj);
			return obj;
		}

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
		//todo - collapse input messages to one IPC operation. right now theres like 30 of them
		//todo - collect all memory block names whenever a memory block is alloc/dealloced. that way we avoid the overhead when using them for gui stuff (gfx debugger, hex editor)


		InstanceDll instanceDll;
		string InstanceName;
		MemoryMappedFile mmf;
		MemoryMappedViewAccessor mmva;
		byte* mmvaPtr;
		bool bufio;

		byte[] StringToBytes(string str)
		{
			byte[] ret = new byte[str.Length];

			for (int i = 0; i < str.Length; i++)
				ret[i] = (byte)str[i];

			return ret;
		}

		[DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
		public static unsafe extern void* CopyMemory(void* dest, void* src, ulong count);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void DllInit(string ipcname);


		public LibsnesApi(string dllPath)
		{

			InstanceName = "libsneshawk_" + Guid.NewGuid().ToString();

			var pipeName = InstanceName;

			mmf = MemoryMappedFile.CreateNew(pipeName, 1024 * 1024);
			mmva = mmf.CreateViewAccessor();
			mmva.SafeMemoryMappedViewHandle.AcquirePointer(ref mmvaPtr);
            
			instanceDll = new InstanceDll(dllPath);
			DllInit dllinit = (DllInit)Marshal.GetDelegateForFunctionPointer(instanceDll.GetProcAddress("DllInit"), typeof(DllInit));
			dllinit(pipeName);
			InitCMDFunctions();
			InitSigFunctions();
			InitBrkFunctions();
			InitQueryFunctions();
		}
		~LibsnesApi()
		{
			// This is now needed because of Dispose being called after all the objects 
			// That we pass to the unmanaged code are garbage collected.
			// It will crash if we do this in Dispose()
			instanceDll.Dispose();
		}

		public void Dispose()
		{   
			mmva.Dispose();
			mmf.Dispose();
			foreach (var smb in DeallocatedMemoryBlocks.Values)
				smb.Dispose();
			DeallocatedMemoryBlocks.Clear();
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
	}

}
