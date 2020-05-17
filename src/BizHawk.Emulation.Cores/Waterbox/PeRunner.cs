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
	public class PeRunnerOptions
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
		public ulong StartAddress { get; set; } = PeRunner.CanonicalStart;

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

	public class PeRunner : Swappable, IImportResolver, IBinaryStateable
	{
		/// <summary>
		/// serves as a standin for libpsxscl.so
		/// </summary>
		private class Psx
		{
			private readonly PeRunner _parent;
			private readonly List<Delegate> _traps = new List<Delegate>();

			public Psx(PeRunner parent)
			{
				_parent = parent;
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct PsxContext
			{
				public int Size;
				public int Options;
				public IntPtr SyscallVtable;
				public IntPtr LdsoVtable;
				public IntPtr PsxVtable;
				public uint SysIdx;
				public uint LibcIdx;
				public IntPtr PthreadSurrogate;
				public IntPtr PthreadCreate;
				public IntPtr DoGlobalCtors;
				public IntPtr DoGlobalDtors;
			}

			private void PopulateVtable(string moduleName, ICollection<string> entries, IntPtr table)
			{
				var imports = _parent._exports[moduleName];
				var pointers = entries.Select(e =>
				{
					var ptr = imports.GetProcAddrOrZero(e);
					if (ptr != IntPtr.Zero) return ptr;
					var s = $"Trapped on unimplemented function {moduleName}:{e}";
					Action del = () =>
					{
						Console.WriteLine(s);
						throw new InvalidOperationException(s);
					};
					_traps.Add(del);
					return CallingConventionAdapters.Waterbox.GetFunctionPointerForDelegate(del);
				}).ToArray();
				Marshal.Copy(pointers, 0, table, pointers.Length);
			}

			/// <summary>
			/// called by the PeRunner to reset pointers after a loadsave
			/// </summary>
			public void ReloadVtables()
			{
				_traps.Clear();

				PopulateVtable("__syscalls", Enumerable.Range(0, 340).Select(i => "n" + i).ToList(), _syscallVtable);
				PopulateVtable("__syscalls", new[] // ldso
				{
					"dladdr", "dlinfo", "dlsym", "dlopen", "dlclose", "dlerror", "reset_tls"
				}, _ldsoVtable);
				PopulateVtable("__syscalls", new[] // psx
				{
					"start_main", "convert_thread", "unmapself", "log_output", "pthread_self"
				}, _psxVtable);
				/*unsafe
                {
                    var ptr = (IntPtr*)_psxVtable;
                    Console.WriteLine("AWESOMES: " + ptr[0]);
                }*/
			}

			private IntPtr _syscallVtable;
			private IntPtr _ldsoVtable;
			private IntPtr _psxVtable;

			private IntPtr AllocVtable(int count)
			{
				return Z.US(_parent._invisibleheap.Allocate((ulong)(count * IntPtr.Size), 16));
			}

			[BizExport(CallingConvention.Cdecl, EntryPoint = "__psx_init")]
			public int PsxInit(ref int argc, ref IntPtr argv, ref IntPtr envp, [In, Out]ref PsxContext context)
			{
				{
					// argc = 1, argv = ["foobar, NULL], envp = [NULL]
					argc = 1;
					var argArea = _parent._sealedheap.Allocate(32, 16);
					argv = Z.US(argArea);
					envp = Z.US(argArea + (uint)IntPtr.Size * 2);
					Marshal.WriteIntPtr(Z.US(argArea), Z.US(argArea + 24));
					Marshal.WriteInt64(Z.US(argArea + 24), 0x7261626f6f66);
				}

				context.SyscallVtable = _syscallVtable = AllocVtable(340);
				context.LdsoVtable = _ldsoVtable = AllocVtable(7);
				context.PsxVtable = _psxVtable = AllocVtable(5);
				// ctx comes from the native stack, where it could have any garbage in uninited fields
				context.SysIdx = 0;
				context.LibcIdx = 0;
				context.DoGlobalCtors = IntPtr.Zero;
				context.DoGlobalDtors = IntPtr.Zero;

				ReloadVtables();

				// TODO: we can't set these pointers 4 and preserve across session
				// until we find out where they get saved to and add a way to reset them
				/*var extraTable = CreateVtable("__syscalls", new[]
				{
					"pthread_surrogate", "pthread_create", "do_global_ctors", "do_global_dtors"
				});
				var tmp = new IntPtr[4];
				Marshal.Copy(extraTable, tmp, 0, 4);
				context.PthreadSurrogate = tmp[0];
				context.PthreadCreate = tmp[1];
				context.DoGlobalCtors = tmp[2];
				context.DoGlobalDtors = tmp[3];*/

				return 0; // success
			}
		}

		/// <summary>
		/// usual starting point for the executable
		/// </summary>
		public const ulong CanonicalStart = 0x0000036f00000000;

		public const ulong AlternateStart = 0x0000036e00000000;

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
		/// all loaded PE files
		/// </summary>
		private readonly List<PeWrapper> _modules = new List<PeWrapper>();

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

		/// <summary>
		/// all of the exports, including real PeWrapper ones and fake ones
		/// </summary>
		private readonly Dictionary<string, IImportResolver> _exports = new Dictionary<string, IImportResolver>();

		private Psx _psx;
		private EmuLibc _emu;
		private Syscalls _syscalls;

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

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void LibcEntryRoutineD(IntPtr appMain, IntPtr psxInit, int options);

		public PeRunner(PeRunnerOptions opt)
		{
			_nextStart = opt.StartAddress;
			Initialize(_nextStart);
			using (this.EnterExit())
			{
				// load any predefined exports
				_psx = new Psx(this);
				_exports.Add("libpsxscl.so", BizExvoker.GetExvoker(_psx, CallingConventionAdapters.Waterbox));
				_emu = new EmuLibc(this);
				_exports.Add("libemuhost.so", BizExvoker.GetExvoker(_emu, CallingConventionAdapters.Waterbox));
				_syscalls = new Syscalls(this);
				_exports.Add("__syscalls", BizExvoker.GetExvoker(_syscalls, CallingConventionAdapters.Waterbox));

				// load and connect all modules, starting with the executable
				var todoModules = new Queue<string>();
				todoModules.Enqueue(opt.Filename);

				while (todoModules.Count > 0)
				{
					var moduleName = todoModules.Dequeue();
					if (!_exports.ContainsKey(moduleName))
					{
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

						var module = new PeWrapper(moduleName, data, _nextStart, opt.SkipCoreConsistencyCheck, opt.SkipMemoryConsistencyCheck);
						ComputeNextStart(module.Size);
						AddMemoryBlock(module.Memory, moduleName);
						_savestateComponents.Add(module);
						_disposeList.Add(module);

						_exports.Add(moduleName, module);
						_modules.Add(module);
						foreach (var name in module.ImportsByModule.Keys)
						{
							todoModules.Enqueue(name);
						}
					}
				}

				_exports["libc.so"] = new PatchImportResolver(_exports["libc.so"], BizExvoker.GetExvoker(_libcpatch, CallingConventionAdapters.Waterbox));

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

				Console.WriteLine("About to enter unmanaged code");
				if (!OSTailoredCode.IsUnixHost && !System.Diagnostics.Debugger.IsAttached && Win32Imports.IsDebuggerPresent())
				{
					// this means that GDB or another unconventional debugger is attached.
					// if that's the case, and it's observing this core, it probably wants a break
					System.Diagnostics.Debugger.Break();
				}

				// run unmanaged init code
				var libcEnter = _exports["libc.so"].GetProcAddrOrThrow("__libc_entry_routine");
				var psxInit = _exports["libpsxscl.so"].GetProcAddrOrThrow("__psx_init");

				var del = (LibcEntryRoutineD)CallingConventionAdapters.Waterbox.GetDelegateForFunctionPointer(libcEnter, typeof(LibcEntryRoutineD));
				// the current mmglue code doesn't use the main pointer at all, and this just returns
				del(IntPtr.Zero, psxInit, 0);

				foreach (var m in _modules)
				{
					m.RunGlobalCtors();
				}

				/*try
				{
					_modules[0].RunExeEntry();
					//throw new InvalidOperationException("main() returned!");
				}
				catch //(EndOfMainException)
				{
				}
				_modules[0].RunGlobalCtors();
				foreach (var m in _modules.Skip(1))
				{
					if (!m.RunDllEntry())
						throw new InvalidOperationException("DllMain() returned false");
					m.RunGlobalCtors();
				}*/
			}
		}

		public IntPtr GetProcAddrOrZero(string entryPoint) => _modules[0].GetProcAddrOrZero(entryPoint); // _modules[0] is always the main module

		public IntPtr GetProcAddrOrThrow(string entryPoint) => _modules[0].GetProcAddrOrThrow(entryPoint);

		public void Seal()
		{
			using (this.EnterExit())
			{
				// if libco is used, the jmp_buf for the main cothread can have stack stuff in it.
				// this isn't a problem, since we only savestate when the core is not running, and
				// the next time it's run, that buf will be overridden again.
				// but it breaks xor state verification, so when we seal, nuke it.

				// this could be the responsibility of something else other than the PeRunner; I am not sure yet...
				if (_exports.TryGetValue("libco.so", out var libco))
				{
					Console.WriteLine("Calling co_clean()...");
					CallingConventionAdapters.Waterbox.GetDelegateForFunctionPointer<Action>(libco.GetProcAddrOrThrow("co_clean"))();
				}

				_sealedheap.Seal();
				foreach (var h in _heaps)
				{
					if (h != _invisibleheap && h != _sealedheap) // TODO: if we have more non-savestated heaps, refine this hack
						h.Memory.SaveXorSnapshot();
				}
				foreach (var pe in _modules)
				{
					pe.SealImportsAndTakeXorSnapshot();
				}

				_mmapheap?.Memory.SaveXorSnapshot();
			}
		}

		private void ConnectAllImports()
		{
			foreach (var module in _modules)
			{
				foreach (var name in module.ImportsByModule.Keys)
				{
					module.ConnectImports(name, _exports[name]);
				}
			}
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

		public void SaveStateBinary(BinaryWriter bw)
		{
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
					Console.WriteLine($"Restoring {nameof(PeRunner)} state from a different core...");
					ConnectAllImports();
					_psx.ReloadVtables();
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
				_modules.Clear();
				_exports.Clear();
				_heap = null;
				_sealedheap = null;
				_invisibleheap = null;
				_plainheap = null;
				_mmapheap = null;
			}
		}
	}
}
