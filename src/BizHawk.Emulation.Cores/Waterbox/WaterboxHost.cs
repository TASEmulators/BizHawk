using BizHawk.Common;
using BizHawk.BizInvoke;
using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using static BizHawk.Emulation.Cores.Waterbox.WaterboxHost.WaterboxHostNative;

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
		/// start address in memory
		/// </summary>
		public ulong StartAddress { get; set; } = WaterboxHost.CanonicalStart;

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

	public unsafe class WaterboxHost : IMonitor, IImportResolver, IBinaryStateable
	{

		/// <summary>
		/// usual starting point for the executable
		/// </summary>
		public const ulong CanonicalStart = 0x0000036f00000000;

		private IntPtr _nativeHost;
		private IntPtr _activatedNativeHost;

		private static readonly WaterboxHostNative NativeImpl;
		static WaterboxHost()
		{
			NativeImpl = BizInvoker.GetInvoker<WaterboxHostNative>(
				new DynamicLibraryImportResolver("waterboxhost.dll", eternal: true),
				CallingConventionAdapters.Native);
		}

		public WaterboxHost(WaterboxOptions opt)
		{
			var nativeOpts = new MemoryLayoutTemplate
			{
				start = Z.UU(opt.StartAddress),
				elf_size = Z.UU(64 * 1024 * 1024),
				sbrk_size = Z.UU(opt.SbrkHeapSizeKB * 1024),
				sealed_size = Z.UU(opt.SealedHeapSizeKB * 1024),
				invis_size = Z.UU(opt.InvisibleHeapSizeKB * 1024),
				plain_size = Z.UU(opt.PlainHeapSizeKB * 1024),
				mmap_size = Z.UU(opt.MmapHeapSizeKB * 1024),
			};

			var moduleName = opt.Filename;

			var path = Path.Combine(opt.Path, moduleName);
			var gzpath = path + ".gz";
			byte[] data;
			if (File.Exists(gzpath))
			{
				using var fs = new FileStream(gzpath, FileMode.Open, FileAccess.Read);
				data = Util.DecompressGzipFile(fs);
			}
			else
			{
				data = File.ReadAllBytes(path);
			}

			var retobj = new ReturnData();
			NativeImpl.wbx_create_host(nativeOpts, opt.Filename, CReader.FromStream(new MemoryStream(data, false)), retobj);
			_nativeHost = retobj.GetDataOrThrow();
		}

		public IntPtr GetProcAddrOrZero(string entryPoint)
		{
			using (this.EnterExit())
			{
				var retobj = new ReturnData();
				NativeImpl.wbx_get_proc_addr(_activatedNativeHost, entryPoint, retobj);
				return retobj.GetDataOrThrow();
			}
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

		public void Seal()
		{
			using (this.EnterExit())
			{
				var retobj = new ReturnData();
				NativeImpl.wbx_seal(_activatedNativeHost, retobj);
				retobj.GetDataOrThrow();
			}
			Console.WriteLine("WaterboxHost Sealed!");
		}

		/// <summary>
		/// Adds a file that will appear to the waterbox core's libc.  the file will be read only.
		/// All savestates must have the same file list, so either leave it up forever or remove it during init!
		/// </summary>
		/// <param name="name">the filename that the unmanaged core will access the file by</param>
		public void AddReadonlyFile(byte[] data, string name)
		{
			using (this.EnterExit())
			{
				var retobj = new ReturnData();
				NativeImpl.wbx_mount_file(_activatedNativeHost, name, CReader.FromStream(new MemoryStream(data, false)), false, retobj);
				retobj.GetDataOrThrow();
			}
		}

		/// <summary>
		/// Remove a file previously added by AddReadonlyFile.  Frees the internal copy of the filedata, saving memory.
		/// All savestates must have the same file list, so either leave it up forever or remove it during init!
		/// </summary>
		public void RemoveReadonlyFile(string name)
		{
			using (this.EnterExit())
			{
				var retobj = new ReturnData();
				NativeImpl.wbx_unmount_file(_activatedNativeHost, name, null, retobj);
				retobj.GetDataOrThrow();
			}
		}

		/// <summary>
		/// Add a transient file that will appear to the waterbox core's libc.  The file will be readable
		/// and writable.  Any attempt to save state while the file is loaded will fail.
		/// </summary>
		public void AddTransientFile(byte[] data, string name)
		{
			using (this.EnterExit())
			{
				var retobj = new ReturnData();
				NativeImpl.wbx_mount_file(_activatedNativeHost, name, CReader.FromStream(new MemoryStream(data, false)), true, retobj);
				retobj.GetDataOrThrow();
			}
		}

		/// <summary>
		/// Remove a file previously added by AddTransientFile
		/// </summary>
		/// <returns>The state of the file when it was removed</returns>
		public byte[] RemoveTransientFile(string name)
		{
			using (this.EnterExit())
			{
				var retobj = new ReturnData();
				var ms = new MemoryStream();
				NativeImpl.wbx_unmount_file(_activatedNativeHost, name, CWriter.FromStream(ms), retobj);
				retobj.GetDataOrThrow();
				return ms.ToArray();
			}
		}

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

		private const ulong MAGIC = 0x736b776162727477;
		private const ulong WATERBOXSTATEVERSION = 2;

		public void SaveStateBinary(BinaryWriter bw)
		{
			bw.Write(MAGIC);
			bw.Write(WATERBOXSTATEVERSION);
			bw.Write(_createstamp);
			bw.Write(_savestateComponents.Count);
			using (this.EnterExit())
			{
				foreach (var c in _savestateComponents)
				{
					c.SaveStateBinary(bw);
				}
			}
		}

		public void LoadStateBinary(BinaryReader br)
		{
			if (br.ReadUInt64() != MAGIC)
				throw new InvalidOperationException("Internal savestate error");
			if (br.ReadUInt64() != WATERBOXSTATEVERSION)
				throw new InvalidOperationException("Waterbox savestate version mismatch");
			var differentCore = br.ReadInt64() != _createstamp; // true if a different core instance created the state
			if (br.ReadInt32() != _savestateComponents.Count)
				throw new InvalidOperationException("Internal savestate error");
			using (this.EnterExit())
			{
				foreach (var c in _savestateComponents)
				{
					c.LoadStateBinary(br);
				}
				if (differentCore)
				{
					// if a different runtime instance than this one saved the state,
					// Exvoker imports need to be reconnected
					Console.WriteLine($"Restoring {nameof(WaterboxHost)} state from a different core...");
					ConnectAllImports();
				}
			}
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing)
			{
				foreach (var d in _disposeList)
					d.Dispose();
				_disposeList.Clear();
				PurgeMemoryBlocks();
				_module = null;
				_heap = null;
				_sealedheap = null;
				_invisibleheap = null;
				_plainheap = null;
				_mmapheap = null;
			}
		}

		public abstract class WaterboxHostNative
		{
			[StructLayout(LayoutKind.Sequential)]
			public class ReturnData
			{
				[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
				public byte[] ErrorMessage = new byte[1024];
				public IntPtr Data;

				public IntPtr GetDataOrThrow()
				{
					if (ErrorMessage[0] != 0)
					{
						fixed(byte* p = ErrorMessage)
							throw new InvalidOperationException(Mershul.PtrToStringUtf8((IntPtr)p));
					}
					return Data;
				}
			}

			[StructLayout(LayoutKind.Sequential)]
			public class MemoryLayoutTemplate
			{
				/// Absolute pointer to the start of the mapped space
				public UIntPtr start;
				/// Memory space for the elf executable.  The elf must be non-relocatable and
				/// all loaded segments must fit within [start..start + elf_size]
				public UIntPtr elf_size;
				/// Memory space to serve brk(2)
				public UIntPtr sbrk_size;
				/// Memory space to serve alloc_sealed(3)
				public UIntPtr sealed_size;
				/// Memory space to serve alloc_invisible(3)
				public UIntPtr invis_size;
				/// Memory space to serve alloc_plain(3)
				public UIntPtr plain_size;
				/// Memory space to serve mmap(2) and friends.
				/// Calls without MAP_FIXED or MREMAP_FIXED will be placed in this area.
				/// TODO: Are we allowing fixed calls to happen anywhere in the block?
				public UIntPtr mmap_size;
			}
			public delegate IntPtr StreamCallback(IntPtr userdata, IntPtr /*byte**/ data, UIntPtr size);
			public delegate UIntPtr /*MissingFileResult*/ FileCallback(IntPtr userdata, UIntPtr /*string*/ name);
			[StructLayout(LayoutKind.Sequential)]
			public class CWriter
			{
				/// will be passed to callback
				public IntPtr userdata;
				/// write bytes.  Return number of bytes written on success, or < 0 on failure.
				/// Permitted to write less than the provided number of bytes.
				public StreamCallback callback;
				public static CWriter FromStream(Stream stream)
				{
					return new CWriter
					{
						// TODO: spans
						callback = (_unused, data, size) =>
						{
							try
							{
								var count = (int)size;
								var buff = new byte[count];
								Marshal.Copy(data, buff, 0, count);
								stream.Write(buff, 0, count);
								return Z.SS(count);
							}
							catch
							{
								return Z.SS(-1);
							}
						}
					};
				}
			}
			[StructLayout(LayoutKind.Sequential)]
			public class CReader
			{
				/// will be passed to callback
				public UIntPtr userdata;
				/// Read bytes into the buffer.  Return number of bytes read on success, or < 0 on failure.
				/// permitted to read less than the provided buffer size, but must always read at least 1
				/// byte if EOF is not reached.  If EOF is reached, should return 0.
				public StreamCallback callback;
				public static CReader FromStream(Stream stream)
				{
					return new CReader
					{
						// TODO: spans
						callback = (_unused, data, size) =>
						{
							try
							{
								var count = (int)size;
								var buff = new byte[count];
								var n = stream.Read(buff, 0, count);
								Marshal.Copy(buff, 0, data, count);
								return Z.SS(n);
							}
							catch
							{
								return Z.SS(-1);
							}
						}
					};
				}
			}
			[StructLayout(LayoutKind.Sequential)]
			public class MissingFileCallback
			{
				public UIntPtr userdata;
				public FileCallback callback;
			}
			[StructLayout(LayoutKind.Sequential)]
			public class MissingFileResult : CReader
			{
				public bool writable;
			}

			[BizImport(CallingConvention.Cdecl, Compatibility = true)]
			public abstract void wbx_create_host(MemoryLayoutTemplate layout, string moduleName, CReader wbx, ReturnData /*WaterboxHost*/ ret);
			/// Tear down a host environment.  May not be called while the environment is active.
			[BizImport(CallingConvention.Cdecl, Compatibility = true)]
			public abstract void wbx_destroy_host(IntPtr /*WaterboxHost*/ obj, [Out]ReturnData /*void*/ ret);
			/// Activate a host environment.  This swaps it into memory and makes it available for use.
			/// Pointers to inside the environment are only valid while active.  Uses a mutex internally
			/// so as to not stomp over other host environments in the same 4GiB slice.
			/// Returns a pointer to the activated object, used to do most other functions.
			[BizImport(CallingConvention.Cdecl, Compatibility = true)]
			public abstract void wbx_activate_host(IntPtr /*WaterboxHost*/ obj, ReturnData /*ActivatedWaterboxHost*/ ret);
			/// Deactivates a host environment, and releases the mutex.
			[BizImport(CallingConvention.Cdecl, Compatibility = true)]
			public abstract void wbx_deactivate_host(IntPtr /*ActivatedWaterboxHost*/ obj, ReturnData /*void*/ ret);
			/// Returns the address of an exported function from the guest executable.  This pointer is only valid
			/// while the host is active.  A missing proc is not an error and simply returns 0.
			[BizImport(CallingConvention.Cdecl, Compatibility = true)]
			public abstract void wbx_get_proc_addr(IntPtr /*ActivatedWaterboxHost*/ obj, string name, ReturnData /*UIntPtr*/ ret);
			/// Calls the seal operation, which is a one time action that prepares the host to save states.
			[BizImport(CallingConvention.Cdecl, Compatibility = true)]
			public abstract void wbx_seal(IntPtr /*ActivatedWaterboxHost*/ obj, ReturnData /*void*/ ret);
			/// Mounts a file in the environment.  All data will be immediately consumed from the reader, which will not be used after this call.
			/// To prevent nondeterminism, adding and removing files is very limited WRT savestates.  If a file is writable, it must never exist
			/// when save_state is called, and can only be used for transient operations.  If a file is readable, it can appear in savestates,
			/// but it must exist in every savestate and the exact sequence of add_file calls must be consistent from savestate to savestate.
			[BizImport(CallingConvention.Cdecl, Compatibility = true)]
			public abstract void wbx_mount_file(IntPtr /*ActivatedWaterboxHost*/ obj, string name, CReader reader, bool writable, ReturnData /*void*/ ret);
			/// Remove a file previously added.  Writer is optional; if provided, the contents of the file at time of removal will be dumped to it.
			/// It is an error to remove a file which is currently open in the guest.
			[BizImport(CallingConvention.Cdecl, Compatibility = true)]
			public abstract void wbx_unmount_file(IntPtr /*ActivatedWaterboxHost*/ obj, string name, CWriter writer, [Out]ReturnData /*void*/ ret);
			/// Set (or clear, with None) a callback to be called whenever the guest tries to load a nonexistant file.
			/// The callback will be provided with the name of the requested load, and can either return null to signal the waterbox
			/// to return ENOENT to the guest, or a struct to immediately load that file.  You may not call any wbx methods
			/// in the callback.  If the MissingFileResult is provided, it will be consumed immediately and will have the same effect
			/// as wbx_mount_file().  You may free resources associated with the MissingFileResult whenever control next returns to your code.
			[BizImport(CallingConvention.Cdecl, Compatibility = true)]
			public abstract void wbx_set_missing_file_callback(IntPtr /*ActivatedWaterboxHost*/ obj, MissingFileCallback mfc_o);
			/// Save state.  Must not be called before seal.  Must not be called with any writable files mounted.
			/// Must always be called with the same sequence and contents of readonly files.
			[BizImport(CallingConvention.Cdecl, Compatibility = true)]
			public abstract void wbx_save_state(IntPtr /*ActivatedWaterboxHost*/ obj, CWriter writer, [Out]ReturnData /*void*/ ret);
			/// Load state.  Must not be called before seal.  Must not be called with any writable files mounted.
			/// Must always be called with the same sequence and contents of readonly files that were in the save state.
			/// Must be called with the same wbx executable and memory layout as in the savestate.
			/// Errors generally poison the environment; sorry!
			[BizImport(CallingConvention.Cdecl, Compatibility = true)]
			public abstract void wbx_load_state(IntPtr /*ActivatedWaterboxHost*/ obj, CReader reader, [Out]ReturnData /*void*/ ret);
		}
	}
}
