using System;

using BizHawk.Common;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	unsafe partial class LibsnesApi
	{

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate string allocSharedMemory_t(string name, int size);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate int snesVideoRefresh_t(int w, int h, bool which);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate int snesAudioFlush_t(int nsamples);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void snesInputNotify_t(int index);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void snesFreeSharedMemory_t(string name);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate short snesInputState_t(int port, int device, int index, int id);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void snesTraceCallback_t(string trace);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate string snesPathRequest_t(int slot, string hint);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void SetAllocSharedMemory_t(allocSharedMemory_t f);
		SetAllocSharedMemory_t SetAllocSharedMemory;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void SetSnesVideoRefresh_t(snesVideoRefresh_t f);
		SetSnesVideoRefresh_t SetSnesVideoRefresh;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void SetSnesAudioFlush_t(snesAudioFlush_t f);
		SetSnesAudioFlush_t SetSnesAudioFlush;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void SetSnesInputNotify_t(snesInputNotify_t f);
		SetSnesInputNotify_t SetSnesInputNotify;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void SetSnesFreeSharedMemory_t(snesFreeSharedMemory_t f);
		SetSnesFreeSharedMemory_t SetSnesFreeSharedMemory;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void SetSnesInputState_t(snesInputState_t f);
		SetSnesInputState_t SetSnesInputState;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void SetSnesTraceCallback_t(snesTraceCallback_t f);
		SetSnesTraceCallback_t SetSnesTraceCallback;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void SetSnesPathRequest_t(snesPathRequest_t f);
		SetSnesPathRequest_t SetSnesPathRequest;

		string snesPathRequest(int slot, string hint)
		{
			string ret = hint;
			if (pathRequest != null)
				hint = pathRequest(slot, hint);
			return hint;
		}

		void snesTraceCallback(string trace)
		{
			traceCallback?.Invoke(trace);
		}
		short snesInputState(int port, int device, int index, int id)
		{
			short ret = 0;
			if (input_state != null)
				ret = (short)input_state(port, device, index, id);
			return ret;
		}
		void InitSigFunctions()
		{
			instanceDll.Retrieve(out SetAllocSharedMemory, "SetAllocSharedMemory");
			SetAllocSharedMemory(Keep<allocSharedMemory_t>(allocSharedMemory));

			instanceDll.Retrieve(out SetSnesVideoRefresh, "SetSnesVideoRefresh");
			SetSnesVideoRefresh(Keep<snesVideoRefresh_t>(snesVideoRefresh));

			instanceDll.Retrieve(out SetSnesAudioFlush, "SetSnesAudioFlush");
			SetSnesAudioFlush(Keep<snesAudioFlush_t>(snesAudioFlush));

			instanceDll.Retrieve(out SetSnesInputNotify, "SetSnesInputNotify");
			SetSnesInputNotify(Keep<snesInputNotify_t>(snesInputNotify));

			instanceDll.Retrieve(out SetSnesFreeSharedMemory, "SetSnesFreeSharedMemory");
			SetSnesFreeSharedMemory(Keep<snesFreeSharedMemory_t>(snesFreeSharedMemory));

			instanceDll.Retrieve(out SetSnesInputState, "SetSnesInputState");
			SetSnesInputState(Keep<snesInputState_t>(snesInputState));

			instanceDll.Retrieve(out SetSnesPathRequest, "SetSnesPathRequest");
			SetSnesPathRequest(Keep<snesPathRequest_t>(snesPathRequest));

			instanceDll.Retrieve(out SetSnesTraceCallback, "SetSnesTraceCallback");
			SetSnesTraceCallback(Keep<snesTraceCallback_t>(snesTraceCallback));
		}
		void snesFreeSharedMemory(string name)
		{
			var smb = SharedMemoryBlocks[name];
			DeallocatedMemoryBlocks[name] = smb;
			SharedMemoryBlocks.Remove(name);
		}
		void snesInputNotify(int index)
		{
			input_notify?.Invoke(index);
		}
		int snesAudioFlush(int nsamples)
		{
			if (audio_sample != null)
			{
				ushort* audiobuffer = ((ushort*)mmvaPtr);
				for (int i = 0; i < nsamples;)
				{
					ushort left = audiobuffer[i++];
					ushort right = audiobuffer[i++];
					audio_sample(left, right);
				}
			}

			return 0;
		}
		int snesVideoRefresh(int w, int h, bool get)
		{
			if (get)
				return 0;
			video_refresh?.Invoke((int*)mmvaPtr, w, h);
			return 0;
		}
		string allocSharedMemory(string name, int size)
		{

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

			return smb.BlockName;
		}
	}
}