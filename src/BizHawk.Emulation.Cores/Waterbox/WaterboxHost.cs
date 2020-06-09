using BizHawk.Common;
using BizHawk.BizInvoke;
using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

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

	public class WaterboxHost : Swappable, IImportResolver, IBinaryStateable
	{
		/// <summary>
		/// usual starting point for the executable
		/// </summary>
		public const ulong CanonicalStart = 0x0000036f00000000;

		/// <summary>
		/// the next place where we can put a module or heap
		/// </summary>
		private ulong _nextStart = CanonicalStart;

		/// <summary>
		/// increment _nextStart after adding a module
		/// </summary>
		private void ComputeNextStart(ulong size)
		{
			_nextStart += size;
			// align to 1MB, then increment 16MB
			_nextStart = ((_nextStart - 1) | 0xfffff) + 0x1000001;
		}

		/// <summary>
		/// standard malloc() heap
		/// </summary>
		internal Heap _heap;

		/// <summary>
		/// sealed heap (writable only during init)
		/// </summary>
		internal Heap _sealedheap;

		/// <summary>
		/// invisible heap (not savestated, use with care)
		/// </summary>
		internal Heap _invisibleheap;

		/// <summary>
		/// extra savestated heap
		/// </summary>
		internal Heap _plainheap;

		/// <summary>
		/// memory map emulation
		/// </summary>
		internal MapHeap _mmapheap;

		/// <summary>
		/// the loaded elf file
		/// </summary>
		private ElfLoader _module;

		/// <summary>
		/// all loaded heaps
		/// </summary>
		private readonly List<Heap> _heaps = new List<Heap>();

		/// <summary>
		/// anything at all that needs to be disposed on finish
		/// </summary>
		private readonly List<IDisposable> _disposeList = new List<IDisposable>();

		/// <summary>
		/// anything at all that needs its state saved and loaded
		/// </summary>
		private readonly List<IBinaryStateable> _savestateComponents = new List<IBinaryStateable>();

		private readonly EmuLibc _emu;
		private readonly Syscalls _syscalls;

		/// <summary>
		/// the set of functions made available for the elf module
		/// </summary>
		private readonly IImportResolver _imports;

		/// <summary>
		/// timestamp of creation acts as a sort of "object id" in the savestate
		/// </summary>
		private readonly long _createstamp = WaterboxUtils.Timestamp();

		private Heap CreateHeapHelper(uint sizeKB, string name, bool saveStated)
		{
			if (sizeKB != 0)
			{
				var heap = new Heap(_nextStart, sizeKB * 1024, name);
				heap.Memory.Activate();
				ComputeNextStart(sizeKB * 1024);
				AddMemoryBlock(heap.Memory, name);
				if (saveStated)
					_savestateComponents.Add(heap);
				_disposeList.Add(heap);
				_heaps.Add(heap);
				return heap;
			}
			else
			{
				return null;
			}
		}

		public WaterboxHost(WaterboxOptions opt)
		{
			_nextStart = opt.StartAddress;
			Initialize(_nextStart);
			using (this.EnterExit())
			{
				_emu = new EmuLibc(this);
				_syscalls = new Syscalls(this);

				_imports = new PatchImportResolver(
					NotImplementedSyscalls.Instance,
					BizExvoker.GetExvoker(_emu, CallingConventionAdapters.Waterbox),
					BizExvoker.GetExvoker(_syscalls, CallingConventionAdapters.Waterbox)
				);

				if (true)
				{
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

					_module = new ElfLoader(moduleName, data, _nextStart, opt.SkipCoreConsistencyCheck, opt.SkipMemoryConsistencyCheck);

					ComputeNextStart(_module.Memory.Size);
					AddMemoryBlock(_module.Memory, moduleName);
					_savestateComponents.Add(_module);
					_disposeList.Add(_module);
				}

				ConnectAllImports();

				// load all heaps
				_heap = CreateHeapHelper(opt.SbrkHeapSizeKB, "brk-heap", true);
				_sealedheap = CreateHeapHelper(opt.SealedHeapSizeKB, "sealed-heap", true);
				_invisibleheap = CreateHeapHelper(opt.InvisibleHeapSizeKB, "invisible-heap", false);
				_plainheap = CreateHeapHelper(opt.PlainHeapSizeKB, "plain-heap", true);

				if (opt.MmapHeapSizeKB != 0)
				{
					_mmapheap = new MapHeap(_nextStart, opt.MmapHeapSizeKB * 1024, "mmap-heap");
					_mmapheap.Memory.Activate();
					ComputeNextStart(opt.MmapHeapSizeKB * 1024);
					AddMemoryBlock(_mmapheap.Memory, "mmap-heap");
					_savestateComponents.Add(_mmapheap);
					_disposeList.Add(_mmapheap);
				}

				// TODO: This debugger stuff doesn't work on nix?
				System.Diagnostics.Debug.WriteLine($"About to enter unmanaged code for {opt.Filename}");
				if (OSTailoredCode.IsUnixHost)
				{
					if (System.Diagnostics.Debugger.IsAttached)
						System.Diagnostics.Debugger.Break();
				}
				else
				{
					if (!System.Diagnostics.Debugger.IsAttached && Win32Imports.IsDebuggerPresent())
					{
						// this means that GDB or another unconventional debugger is attached.
						// if that's the case, and it's observing this core, it probably wants a break
						System.Diagnostics.Debugger.Break();
					}
				}
				_module.RunNativeInit();
			}
		}

		public IntPtr GetProcAddrOrZero(string entryPoint)
		{
			var addr = _module.GetProcAddrOrZero(entryPoint);
			if (addr != IntPtr.Zero)
			{
				var exclude = _imports.GetProcAddrOrZero(entryPoint);
				if (exclude != IntPtr.Zero)
				{
					// don't reexport anything that's part of waterbox internals
					return IntPtr.Zero;
				}	
			}
			return addr;
		}

		public IntPtr GetProcAddrOrThrow(string entryPoint)
		{
			var addr = _module.GetProcAddrOrZero(entryPoint);
			if (addr != IntPtr.Zero)
			{
				var exclude = _imports.GetProcAddrOrZero(entryPoint);
				if (exclude != IntPtr.Zero)
				{
					// don't reexport anything that's part of waterbox internals
					throw new InvalidOperationException($"Tried to resolve {entryPoint}, but it should not be exported");
				}
				else
				{
					return addr;
				}
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
				// if libco is used, the jmp_buf for the main cothread can have stack stuff in it.
				// this isn't a problem, since we only savestate when the core is not running, and
				// the next time it's run, that buf will be overridden again.
				// but it breaks xor state verification, so when we seal, nuke it.

				// this could be the responsibility of something else other than the PeRunner; I am not sure yet...

				// TODO: MAKE SURE THIS STILL WORKS
				IntPtr co_clean;
				if ((co_clean = _module.GetProcAddrOrZero("co_clean")) != IntPtr.Zero)
				{
					Console.WriteLine("Calling co_clean().");
					CallingConventionAdapters.Waterbox.GetDelegateForFunctionPointer<Action>(co_clean)();
				}

				_sealedheap.Seal();
				foreach (var h in _heaps)
				{
					if (h != _invisibleheap && h != _sealedheap) // TODO: if we have more non-savestated heaps, refine this hack
						h.Memory.Seal();
				}
				_module.SealImportsAndTakeXorSnapshot();
				_mmapheap?.Memory.Seal();
			}
			Console.WriteLine("WaterboxHost Sealed!");
		}

		private void ConnectAllImports()
		{
			_module.ConnectSyscalls(_imports);
		}

		/// <summary>
		/// Adds a file that will appear to the waterbox core's libc.  the file will be read only.
		/// All savestates must have the same file list, so either leave it up forever or remove it during init!
		/// </summary>
		/// <param name="name">the filename that the unmanaged core will access the file by</param>
		public void AddReadonlyFile(byte[] data, string name)
		{
			_syscalls.AddReadonlyFile((byte[])data.Clone(), name);
		}

		/// <summary>
		/// Remove a file previously added by AddReadonlyFile.  Frees the internal copy of the filedata, saving memory.
		/// All savestates must have the same file list, so either leave it up forever or remove it during init!
		/// </summary>
		public void RemoveReadonlyFile(string name)
		{
			_syscalls.RemoveReadonlyFile(name);
		}

		/// <summary>
		/// Add a transient file that will appear to the waterbox core's libc.  The file will be readable
		/// and writable.  Any attempt to save state while the file is loaded will fail.
		/// </summary>
		public void AddTransientFile(byte[] data, string name)
		{
			_syscalls.AddTransientFile(data, name); // don't need to clone data, as it's used at init only
		}

		/// <summary>
		/// Remove a file previously added by AddTransientFile
		/// </summary>
		/// <returns>The state of the file when it was removed</returns>
		public byte[] RemoveTransientFile(string name)
		{
			return _syscalls.RemoveTransientFile(name);
		}

		/// <summary>
		/// Can be set by the frontend and will be called if the core attempts to open a missing file.
		/// The callee may add additional files to the waterbox during the callback and return `true` to indicate
		/// that the right file was added and the scan should be rerun.  The callee may return `false` to indicate
		/// that the file should be reported as missing.  Do not call other things during this callback.
		/// Can be called at any time by the core, so you may want to remove your callback entirely after init
		/// if it was for firmware only.
		/// </summary>
		public Func<string, bool> MissingFileCallback
		{
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
	}
}
