using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using BizHawk.Common;
using BizHawk.Emulation.Cores.Waterbox;
using BizHawk.BizInvoke;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public abstract unsafe class BsnesCoreImpl
	{
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_set_audio_enabled(bool enabled);
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_set_video_enabled(bool enabled);
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_set_layer_enables(BsnesApi.LayerEnables layerEnables);
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_set_trace_enabled(bool enabled);

		[BizImport(CallingConvention.Cdecl)]
		public abstract BsnesApi.SNES_REGION snes_get_region();
		[BizImport(CallingConvention.Cdecl)]
		public abstract BsnesApi.SNES_MAPPER snes_get_mapper();
		[BizImport(CallingConvention.Cdecl)]
		public abstract void* snes_get_memory_region(int id, out int size, out int wordSize);
		[BizImport(CallingConvention.Cdecl)]
		public abstract byte snes_bus_read(uint address);
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_bus_write(uint address, byte value);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_set_callbacks(IntPtr[] snesCallbacks);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_init(BsnesApi.ENTROPY entropy, BsnesApi.BSNES_INPUT_DEVICE left,
			BsnesApi.BSNES_INPUT_DEVICE right, ushort mergedBools);// bool hotfixes, bool fastPPU);
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_power();
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_term();
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_reset();
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_run();

		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_serialize(byte[] serializedData, int serializedSize);
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_unserialize(byte[] serializedData, int serializedSize);
		[BizImport(CallingConvention.Cdecl)]
		public abstract int snes_serialized_size();

		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_load_cartridge_normal(string baseRomPath, byte[] romData, int romSize);
		// note this might fail currently if ever called
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_load_cartridge_super_gameboy(string baseRomPath, byte[] romData, int romSize, byte[] sgbRomData, int sgbRomSize);
	}

	public unsafe partial class BsnesApi : IDisposable, IMonitor, IStatable
	{
		internal WaterboxHost exe;
		internal BsnesCoreImpl core;
		private readonly ICallingConventionAdapter _adapter;
		private bool _disposed;

		public void Enter()
		{
			exe.Enter();
		}

		public void Exit()
		{
			exe.Exit();
		}

		private readonly List<string> _readonlyFiles = new List<string>();

		public void AddReadonlyFile(byte[] data, string name)
		{
			exe.AddReadonlyFile(data, name);
			_readonlyFiles.Add(name);
		}

		public void SetCallbacks(SnesCallbacks callbacks)
		{
			FieldInfo[] fieldInfos = typeof(SnesCallbacks).GetFields();
			IntPtr[] functionPointerArray = new IntPtr[fieldInfos.Length];
			for (int i = 0; i < fieldInfos.Length; i++)
			{
				functionPointerArray[i] = _adapter.GetFunctionPointerForDelegate((Delegate) fieldInfos[i].GetValue(callbacks));
			}
			core.snes_set_callbacks(functionPointerArray);
		}

		public BsnesApi(string dllPath, CoreComm comm, IEnumerable<Delegate> allCallbacks)
		{
			exe = new WaterboxHost(new WaterboxOptions
			{
				Filename = "bsnes.wbx",
				Path = dllPath,
				SbrkHeapSizeKB = 16 * 1024, // TODO: this can probably be optimized slightly
				InvisibleHeapSizeKB = 4,
				MmapHeapSizeKB = 128 * 1024, // TODO: check whether this can be smaller; it needs to be 80+ at least rn
				PlainHeapSizeKB = 0,
				SealedHeapSizeKB = 0, // this might actually need to be larger than 0, but doesn't crash for me
				SkipCoreConsistencyCheck = comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			});
			using (exe.EnterExit())
			{
				// Marshal checks that function pointers passed to GetDelegateForFunctionPointer are
				// _currently_ valid when created, even though they don't need to be valid until
				// the delegate is later invoked.  so GetInvoker needs to be acquired within a lock.
				_adapter = CallingConventionAdapters.MakeWaterbox(allCallbacks, exe);
				this.core = BizInvoker.GetInvoker<BsnesCoreImpl>(exe, exe, _adapter);
			}
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;
				exe.Dispose();
				exe = null;
				core = null;
				// serializedSize = 0;
			}
		}

		public delegate void snes_video_frame_t(ushort* data, int width, int height, int pitch);
		public delegate void snes_input_poll_t();
		public delegate short snes_input_state_t(int port, int index, int id);
		public delegate void snes_no_lag_t();
		public delegate void snes_audio_sample_t(short left, short right);
		public delegate string snes_path_request_t(int slot, string hint);
		public delegate void snes_trace_t(string disassembly, string register_info);


		// I cannot use a struct here because marshalling is retarded for bool (4 bytes). I honestly cannot
		[StructLayout(LayoutKind.Sequential)]
		public class LayerEnables
		{
			public bool BG1_Prio0, BG1_Prio1;
			public bool BG2_Prio0, BG2_Prio1;
			public bool BG3_Prio0, BG3_Prio1;
			public bool BG4_Prio0, BG4_Prio1;
			public bool Obj_Prio0, Obj_Prio1, Obj_Prio2, Obj_Prio3;
		}

		[StructLayout(LayoutKind.Sequential)]
		public class SnesCallbacks
		{
			public snes_input_poll_t inputPollCb;
			public snes_input_state_t inputStateCb;
			public snes_no_lag_t noLagCb;
			public snes_video_frame_t videoFrameCb;
			public snes_audio_sample_t audioSampleCb;
			public snes_path_request_t pathRequestCb;
			public snes_trace_t snesTraceCb;
		}

		public void Seal()
		{
			// TODO: check correctness of this method
			exe.Seal();
			foreach (var s in _readonlyFiles)
			{
				exe.RemoveReadonlyFile(s);
			}
			_readonlyFiles.Clear();
		}

		// TODO: confirm that the serializedSize is CONSTANT for any given game,
		// else this might be problematic
		// private int serializedSize;// = 284275;

		public void SaveStateBinary(BinaryWriter writer)
		{
			// if (serializedSize == 0)
				// serializedSize = _core.snes_serialized_size();
			// TODO: do some profiling and testing to check whether this is actually better than _exe.SaveStateBinary(writer);
			// re-adding bsnes's own serialization will need to be done once it's confirmed to be deterministic, aka after libco update

			// byte[] serializedData = new byte[serializedSize];
			exe.SaveStateBinary(writer);
			// _core.snes_serialize(serializedData, serializedSize);
			// writer.Write(serializedData);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			// byte[] serializedData = reader.ReadBytes(serializedSize);
			exe.LoadStateBinary(reader);
			// _core.snes_unserialize(serializedData, serializedSize);
		}
	}
}
