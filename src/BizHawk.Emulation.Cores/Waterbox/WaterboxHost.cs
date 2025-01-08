using System.IO;
using System.Runtime.InteropServices;

using BizHawk.Common;
using BizHawk.BizInvoke;
using BizHawk.Emulation.Common;

using static BizHawk.Emulation.Cores.Waterbox.WaterboxHostNative;

namespace BizHawk.Emulation.Cores.Waterbox
{
	public class WaterboxOptions
	{
		// string directory, string filename, ulong heapsize, ulong sealedheapsize, ulong invisibleheapsize
		/// <summary>
		/// path which the main executable and all associated libraries should be found
		/// </summary>
		public string Path { get; set; }

		/// <summary>
		/// filename of the main executable; expected to be in Path
		/// </summary>
		public string Filename { get; set; }

		/// <summary>
		/// how large the normal heap should be.  it services sbrk calls
		/// can be 0, but sbrk calls will crash.
		/// </summary>
		public uint SbrkHeapSizeKB { get; set; }

		/// <summary>
		/// how large the sealed heap should be.  it services special allocations that become readonly after init
		/// Must be > 0 and at least large enough to store argv and envp, and any alloc_sealed() calls
		/// </summary>
		public uint SealedHeapSizeKB { get; set; }

		/// <summary>
		/// how large the invisible heap should be.  it services special allocations which are not savestated
		/// Must be > 0 and at least large enough for the internal vtables, and any alloc_invisible() calls
		/// </summary>
		public uint InvisibleHeapSizeKB { get; set; }

		/// <summary>
		/// how large the "plain" heap should be.  it is savestated, and contains
		/// Must be > 0 and at least large enough for the internal pthread structure, and any alloc_plain() calls
		/// </summary>
		public uint PlainHeapSizeKB { get; set; }

		/// <summary>
		/// how large the mmap heap should be.  it is savestated.
		/// can be 0, but mmap calls will crash.
		/// </summary>
		public uint MmapHeapSizeKB { get; set; }

		/// <summary>
		/// Skips the check that the wbx file and other associated dlls match from state save to state load.
		/// DO NOT SET THIS TO TRUE.  A different executable most likely means different meanings for memory locations,
		/// and nothing will make sense.
		/// </summary>
		public bool SkipCoreConsistencyCheck { get; set; } = false;

		/// <summary>
		/// Skips the check that the initial memory state (after init, but before any running) matches from state save to state load.
		/// DO NOT SET THIS TO TRUE.  The initial memory state must be the same for the XORed memory contents in the savestate to make sense.
		/// </summary>
		public bool SkipMemoryConsistencyCheck { get; set; } = false;
	}

	public sealed class WaterboxHost : IMonitor, IImportResolver, IStatable, IDisposable, ICallbackAdjuster
	{
		private IntPtr _nativeHost;
		private int _enterCount;

		private static readonly WaterboxHostNative NativeImpl;
		static WaterboxHost()
		{
			NativeImpl = BizInvoker.GetInvoker<WaterboxHostNative>(
				new DynamicLibraryImportResolver(OSTailoredCode.IsUnixHost ? "libwaterboxhost.so" : "waterboxhost.dll", hasLimitedLifetime: false),
				CallingConventionAdapters.Native);
#if !DEBUG
			NativeImpl.wbx_set_always_evict_blocks(false);
#endif
		}

		private class ReadWriteWrapper : IDisposable
		{
			private GCHandle _handle;
			private readonly ISpanStream _backingSpanStream;
			private readonly Stream _backingStream;
			private readonly bool _disposeStreamAfterUse;

			public IntPtr WaterboxHandle => GCHandle.ToIntPtr(_handle);

			public ReadWriteWrapper(Stream backingStream, bool disposeStreamAfterUse = true)
			{
				_backingStream = backingStream;
				_disposeStreamAfterUse = disposeStreamAfterUse;
				_backingSpanStream = SpanStream.GetOrBuild(_backingStream);
				_handle = GCHandle.Alloc(this, GCHandleType.Weak);
			}

			public IntPtr Read(IntPtr data, UIntPtr size)
			{
				var count = checked((int) size);
				try
				{
					var n = _backingSpanStream.Read(Util.UnsafeSpanFromPointer(ptr: data, length: count));
					return Z.SS(n);
				}
				catch
				{
					return Z.SS(-1);
				}
			}

			public int Write(IntPtr data, UIntPtr size)
			{
				var count = checked((int) size);
				try
				{
					_backingSpanStream.Write(Util.UnsafeSpanFromPointer(ptr: data, length: count));
					return 0;
				}
				catch
				{
					return -1;
				}
			}

			public void Dispose()
			{
				if (_disposeStreamAfterUse) _backingStream.Dispose();
				_handle.Free();
			}
		}

		private static IntPtr ReadCallback(IntPtr userdata, IntPtr data, UIntPtr size)
		{
			var handle = GCHandle.FromIntPtr(userdata);
			var reader = (ReadWriteWrapper)handle.Target;
			return reader.Read(data, size);
		}

		private static int WriteCallback(IntPtr userdata, IntPtr data, UIntPtr size)
		{
			var handle = GCHandle.FromIntPtr(userdata);
			var writer = (ReadWriteWrapper)handle.Target;
			return writer.Write(data, size);
		}

		// cache these delegates so they aren't GC'd
		private static readonly ReadCallback _readCallback = ReadCallback;
		private static readonly WriteCallback _writeCallback = WriteCallback;

		public WaterboxHost(WaterboxOptions opt)
		{
			var nativeOpts = new MemoryLayoutTemplate
			{
				sbrk_size = Z.UU(opt.SbrkHeapSizeKB * 1024),
				sealed_size = Z.UU(opt.SealedHeapSizeKB * 1024),
				invis_size = Z.UU(opt.InvisibleHeapSizeKB * 1024),
				plain_size = Z.UU(opt.PlainHeapSizeKB * 1024),
				mmap_size = Z.UU(opt.MmapHeapSizeKB * 1024),
			};

			var moduleName = opt.Filename;

			var path = Path.Combine(opt.Path, moduleName);
			var zstpath = path + ".zst";
			if (File.Exists(zstpath))
			{
				using var zstd = new Zstd();
				using var fs = new FileStream(zstpath, FileMode.Open, FileAccess.Read);
				using var reader = new ReadWriteWrapper(zstd.CreateZstdDecompressionStream(fs));
				NativeImpl.wbx_create_host(nativeOpts, opt.Filename, _readCallback, reader.WaterboxHandle, out var retobj);
				_nativeHost = retobj.GetDataOrThrow();
			}
			else
			{
				using var reader = new ReadWriteWrapper(new FileStream(path, FileMode.Open, FileAccess.Read));
				NativeImpl.wbx_create_host(nativeOpts, opt.Filename, _readCallback, reader.WaterboxHandle, out var retobj);
				_nativeHost = retobj.GetDataOrThrow();
			}
		}

		public IntPtr GetProcAddrOrZero(string entryPoint)
		{
			NativeImpl.wbx_get_proc_addr_raw(_nativeHost, entryPoint, out var retobj);
			return retobj.GetDataOrThrow();
		}

		public IntPtr GetProcAddrOrThrow(string entryPoint)
		{
			var addr = GetProcAddrOrZero(entryPoint);
			if (addr != IntPtr.Zero)
			{
				return addr;
			}
			else
			{
				throw new InvalidOperationException($"{entryPoint} was not exported from elf");
			}
		}

		public IntPtr GetCallbackProcAddr(IntPtr exitPoint, int slot)
		{
			NativeImpl.wbx_get_callback_addr(_nativeHost, exitPoint, slot, out var retobj);
			return retobj.GetDataOrThrow();
		}

		public IntPtr GetCallinProcAddr(IntPtr entryPoint)
		{
			NativeImpl.wbx_get_callin_addr(_nativeHost, entryPoint, out var retobj);
			return retobj.GetDataOrThrow();
		}

		public void Seal()
		{
			NativeImpl.wbx_seal(_nativeHost, out var retobj);
			retobj.GetDataOrThrow();
			Console.WriteLine("WaterboxHost Sealed!");
		}

		/// <summary>
		/// Adds a file that will appear to the waterbox core's libc.  the file will be read only.
		/// All savestates must have the same file list, so either leave it up forever or remove it during init!
		/// </summary>
		/// <param name="name">the filename that the unmanaged core will access the file by</param>
		public void AddReadonlyFile(byte[] data, string name)
		{
			using var reader = new ReadWriteWrapper(new MemoryStream(data, false));
			NativeImpl.wbx_mount_file(_nativeHost, name, _readCallback, reader.WaterboxHandle, false, out var retobj);
			retobj.GetDataOrThrow();
		}

		/// <summary>
		/// Remove a file previously added by AddReadonlyFile.  Frees the internal copy of the filedata, saving memory.
		/// All savestates must have the same file list, so either leave it up forever or remove it during init!
		/// </summary>
		public void RemoveReadonlyFile(string name)
		{
			NativeImpl.wbx_unmount_file(_nativeHost, name, null, IntPtr.Zero, out var retobj);
			retobj.GetDataOrThrow();
		}

		/// <summary>
		/// Add a transient file that will appear to the waterbox core's libc.  The file will be readable
		/// and writable.  Any attempt to save state while the file is loaded will fail.
		/// </summary>
		public void AddTransientFile(byte[] data, string name)
		{
			using var reader = new ReadWriteWrapper(new MemoryStream(data, false));
			NativeImpl.wbx_mount_file(_nativeHost, name, _readCallback, reader.WaterboxHandle, true, out var retobj);
			retobj.GetDataOrThrow();
		}

		/// <summary>
		/// Remove a file previously added by AddTransientFile
		/// </summary>
		/// <returns>The state of the file when it was removed</returns>
		public byte[] RemoveTransientFile(string name)
		{
			var ms = new MemoryStream();
			using var writer = new ReadWriteWrapper(ms);
			NativeImpl.wbx_unmount_file(_nativeHost, name, _writeCallback, writer.WaterboxHandle, out var retobj);
			retobj.GetDataOrThrow();
			return ms.ToArray();
		}

#if false
		public class MissingFileResult
		{
			public byte[] data;
			public bool writable;
		}

		/// <summary>
		/// Can be set by the frontend and will be called if the core attempts to open a missing file.
		/// The callee returns a result object, either null to indicate that the file should be reported as missing,
		/// or data and writable status for a file to be just in time mounted.
		/// Do not call anything on the waterbox things during this callback.
		/// Can be called at any time by the core, so you may want to remove your callback entirely after init
		/// if it was for firmware only.
		/// writable == false is equivalent to AddReadonlyFile, writable == true is equivalent to AddTransientFile
		/// </summary>
		public Func<string, MissingFileResult> MissingFileCallback
		{
			set
			{
				// TODO
				using (this.EnterExit())
				{
					var mfc_o = value == null ? null : new WaterboxHostNative.MissingFileCallback
					{
						callback = (_unused, name) =>
						{
							var res = value(name);
						}
					};

					NativeImpl.wbx_set_missing_file_callback(_activatedNativeHost, value == null
						? null
						: )
				}
			}
			get => _syscalls.MissingFileCallback;
			set => _syscalls.MissingFileCallback = value;
		}
#endif

		public MemoryDomain GetPagesDomain()
		{
			return new WaterboxPagesDomain(this);
		}

		private class WaterboxPagesDomain : MemoryDomain
		{
			private readonly WaterboxHost _host;

			public WaterboxPagesDomain(WaterboxHost host)
			{
				_host = host;

				NativeImpl.wbx_get_page_len(_host._nativeHost, out var retobj);

				Name = "Waterbox PageData";
				Size = (long)retobj.GetDataOrThrow();
				WordSize = 1;
				EndianType = Endian.Unknown;
				Writable = false;
			}

			public override byte PeekByte(long addr)
			{
				NativeImpl.wbx_get_page_data(_host._nativeHost, Z.SU(addr), out var retobj);
				return (byte)retobj.GetDataOrThrow();
			}

			public override void PokeByte(long addr, byte val)
			{
				throw new InvalidOperationException();
			}
		}

		public bool AvoidRewind => false;

		public void SaveStateBinary(BinaryWriter bw)
		{
			using var writer = new ReadWriteWrapper(bw.BaseStream, false);
			NativeImpl.wbx_save_state(_nativeHost, _writeCallback, writer.WaterboxHandle, out var retobj);
			retobj.GetDataOrThrow();
		}

		public void LoadStateBinary(BinaryReader br)
		{
			using var reader = new ReadWriteWrapper(br.BaseStream, false);
			NativeImpl.wbx_load_state(_nativeHost, _readCallback, reader.WaterboxHandle, out var retobj);
			retobj.GetDataOrThrow();
		}

		public void Enter()
		{
			if (_enterCount == 0)
			{
				NativeImpl.wbx_activate_host(_nativeHost, out var retobj);
				retobj.GetDataOrThrow();
			}
			_enterCount++;
		}

		public void Exit()
		{
			switch (_enterCount)
			{
				case <= 0:
					throw new InvalidOperationException();
				case 1:
					NativeImpl.wbx_deactivate_host(_nativeHost, out var retobj);
					retobj.GetDataOrThrow();
					break;
			}

			_enterCount--;
		}

		public void Dispose()
		{
			if (_nativeHost != IntPtr.Zero)
			{
				if (_enterCount != 0)
				{
					NativeImpl.wbx_deactivate_host(_nativeHost, out _);
					Console.Error.WriteLine("Warn: Disposed of WaterboxHost which was active");
				}
				NativeImpl.wbx_destroy_host(_nativeHost, out _);
				_enterCount = 0;
				_nativeHost = IntPtr.Zero;
				GC.SuppressFinalize(this);
			}
		}

		~WaterboxHost()
		{
			Dispose();
		}
	}
}
