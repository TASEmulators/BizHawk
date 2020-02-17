//TODO: this isn't "the libretro api" anymore
//it's more like a bridge
//I may need to rename stuff to make it sound more like a bridge
//(the bridge wraps libretro API and presents a very different interface)

using System;
using System.Runtime.InteropServices;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Libretro
{
	public unsafe partial class LibretroApi : IDisposable
	{
		InstanceDll instanceDll, instanceDllCore;
		string InstanceName;

		//YUCK
		public LibretroCore core;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate IntPtr DllInit(IntPtr dllModule);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void MessageApi(eMessage msg);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void BufferApi(BufId id, void* ptr, ulong size); //size_t

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void SetVariableApi(string key, string value);

		//it's NOT the original plan to make this public
		//however -- i need to merge the API and the core. theyre too closely related
		public CommStruct* comm;

		MessageApi Message;
		BufferApi _copyBuffer; //TODO: consider making private and wrapping
		BufferApi _setBuffer; //TODO: consider making private and wrapping
		SetVariableApi SetVariable;

		public LibretroApi(string dllPath, string corePath)
		{
			T GetTypedDelegate<T>(string proc) where T : Delegate => (T) Marshal.GetDelegateForFunctionPointer(instanceDll.GetProcAddrOrThrow(proc), typeof(T));

			InstanceName = "libretro_" + Guid.NewGuid();

			var pipeName = InstanceName;

			instanceDll = new InstanceDll(dllPath);
			instanceDllCore = new InstanceDll(corePath);

			var dllinit = GetTypedDelegate<DllInit>("DllInit");
			Message = GetTypedDelegate<MessageApi>("Message");
			_copyBuffer = GetTypedDelegate<BufferApi>("CopyBuffer");
			_setBuffer = GetTypedDelegate<BufferApi>("SetBuffer");
			SetVariable = GetTypedDelegate<SetVariableApi>("SetVariable");

			comm = (CommStruct*)dllinit(instanceDllCore.HModule).ToPointer();

			//TODO: (stash function pointers locally and thunk to IntPtr)
			//ALSO: this should be done by the core, I think, not the API. No smarts should be in here
			comm->env.retro_perf_callback.get_cpu_features = IntPtr.Zero;
			//retro_perf_callback.get_cpu_features = new LibRetro.retro_get_cpu_features_t(() => (ulong)(
			//		(ProcessorFeatureImports.IsProcessorFeaturePresent(ProcessorFeatureImports.ProcessorFeature.InstructionsXMMIAvailable) ? LibRetro.RETRO_SIMD.SSE : 0) |
			//		(ProcessorFeatureImports.IsProcessorFeaturePresent(ProcessorFeatureImports.ProcessorFeature.InstructionsXMMI64Available) ? LibRetro.RETRO_SIMD.SSE2 : 0) |
			//		(ProcessorFeatureImports.IsProcessorFeaturePresent(ProcessorFeatureImports.ProcessorFeature.InstructionsSSE3Available) ? LibRetro.RETRO_SIMD.SSE3 : 0) |
			//		(ProcessorFeatureImports.IsProcessorFeaturePresent(ProcessorFeatureImports.ProcessorFeature.InstructionsMMXAvailable) ? LibRetro.RETRO_SIMD.MMX : 0)
			//	));
			//retro_perf_callback.get_perf_counter = new LibRetro.retro_perf_get_counter_t(() => System.Diagnostics.Stopwatch.GetTimestamp());
			//retro_perf_callback.get_time_usec = new LibRetro.retro_perf_get_time_usec_t(() => DateTime.Now.Ticks / 10);
			//retro_perf_callback.perf_log = new LibRetro.retro_perf_log_t(() => { });
			//retro_perf_callback.perf_register = new LibRetro.retro_perf_register_t((ref LibRetro.retro_perf_counter counter) => { });
			//retro_perf_callback.perf_start = new LibRetro.retro_perf_start_t((ref LibRetro.retro_perf_counter counter) => { });
			//retro_perf_callback.perf_stop = new LibRetro.retro_perf_stop_t((ref LibRetro.retro_perf_counter counter) => { });
		}

		public void Dispose()
		{
			//TODO: better termination of course
			instanceDllCore.Dispose();
			instanceDll.Dispose();
		}

		public RetroDescription CalculateDescription()
		{
			var descr = new RetroDescription();
			descr.LibraryName = new string(comm->env.retro_system_info.library_name);
			descr.LibraryVersion = new string(comm->env.retro_system_info.library_version);
			descr.ValidExtensions = new string(comm->env.retro_system_info.valid_extensions);
			descr.NeedsRomAsPath = comm->env.retro_system_info.need_fullpath;
			descr.NeedsArchives = comm->env.retro_system_info.block_extract;
			descr.SupportsNoGame = comm->env.support_no_game;
			return descr;
		}

		/// <summary>
		/// Copy an ascii string into libretro. It keeps the copy.
		/// </summary>
		public void CopyAscii(BufId id, string str)
		{
			fixed (char* cp = str)
				_copyBuffer(id, cp, (ulong)str.Length + 1);
		}

		/// <summary>
		/// Copy a buffer into libretro. It keeps the copy.
		/// </summary>
		public void CopyBytes(BufId id, byte[] bytes)
		{
			fixed (byte* bp = bytes)
				_copyBuffer(id, bp, (ulong)bytes.Length);
		}

		/// <summary>
		/// Locks a buffer and sets it into libretro. You must pass a delegate to be executed while that buffer is locked.
		/// This is meant to be used for avoiding a memcpy for large roms (which the core is then just going to memcpy again on its own)
		/// The memcpy has to happen at some point (libretro semantics specify [not literally, the docs dont say] that the core should finish using the buffer before its init returns)
		/// but this limits it to once.
		/// Moreover, this keeps the c++ side from having to free strings when they're no longer used (and memory management is trickier there, so we try to avoid it)
		/// </summary>
		public void SetBytes(BufId id, byte[] bytes, Action andThen)
		{
			fixed (byte* bp = bytes)
			{
				_setBuffer(id, bp, (ulong)bytes.Length);
				andThen();
			}
		}

		/// <summary>
		/// see SetBytes
		/// </summary>
		public void SetAscii(BufId id, string str, Action andThen)
		{
			fixed (byte* cp = System.Text.Encoding.ASCII.GetBytes(str+"\0"))
			{
				_setBuffer(id, cp, (ulong)str.Length + 1);
				andThen();
			}
		}

		public unsafe struct CommStructEnv
		{
			public retro_system_info retro_system_info;
			public retro_system_av_info retro_system_av_info;

			public ulong retro_serialize_size_initial; //size_t :(
			public ulong retro_serialize_size; //size_t :(

			public uint retro_region;
			public uint retro_api_version;
			public retro_pixel_format pixel_format; //default is 0 -- RETRO_PIXEL_FORMAT_0RGB1555
			public int rotation_ccw;
			public bool support_no_game;
			public IntPtr core_get_proc_address; //this is.. a callback.. or something.. right?

			public retro_game_geometry retro_game_geometry;
			public bool retro_game_geometry_dirty; //c# can clear this when it's acknowledged (but I think we might handle it from here? not sure)

			public int variable_count;
			public char** variable_keys;
			public char** variable_comments;

			//c# sets these with thunked callbacks
			public retro_perf_callback retro_perf_callback;

			//various stashed stuff solely for c# convenience
			public ulong processor_features;

			public int fb_width, fb_height; //core sets these; c# picks up, and..
			public int* fb_bufptr; //..sets this for the core to spill its data nito
		}

		public struct CommStruct
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
			public uint id, addr, value, size;
			public uint port, device, index, slot;

			[MarshalAs(UnmanagedType.Struct)]
			public CommStructEnv env;

			//this should always be used in pairs
			public fixed ulong buf[(int)BufId.BufId_Num]; //actually a pointer, but can't marshal IntPtr, so dumb
			public fixed ulong buf_size[(int)BufId.BufId_Num]; //actually a size_t

			//utilities
			public bool GetBoolValue() => value != 0; // should this be here or by the other helpers? I dont know
		}

		public retro_system_av_info AVInfo => comm->env.retro_system_av_info;
	} //class
}
