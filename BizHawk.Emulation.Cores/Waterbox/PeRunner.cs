using BizHawk.Common;
using BizHawk.Common.BizInvoke;
using BizHawk.Emulation.Common;
using PeNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BizHawk.Emulation.Cores.Waterbox
{
	public class PeRunner : Swappable, IImportResolver, IBinaryStateable
	{
		/// <summary>
		/// manages one PE file within the the set of loaded PE files
		/// </summary>
		private class PeWrapper : IImportResolver, IBinaryStateable, IDisposable
		{
			public Dictionary<int, IntPtr> ExportsByOrdinal { get; } = new Dictionary<int, IntPtr>();
			/// <summary>
			/// ordinal only exports will not show up in this list!
			/// </summary>
			public Dictionary<string, IntPtr> ExportsByName { get; } = new Dictionary<string, IntPtr>();

			public Dictionary<string, Dictionary<string, IntPtr>> ImportsByModule { get; } = new Dictionary<string, Dictionary<string, IntPtr>>();

			public string ModuleName { get; }

			private readonly byte[] _fileData;
			private readonly PeFile _pe;
			private readonly byte[] _fileHash;

			public ulong Size { get; }
			public ulong Start { get; private set; }

			public long LoadOffset { get; private set; }

			public MemoryBlock Memory { get; private set; }

			public IntPtr EntryPoint { get; private set; }

			[UnmanagedFunctionPointer(CallingConvention.Winapi)]
			private delegate bool DllEntry(IntPtr instance, int reason, IntPtr reserved);
			[UnmanagedFunctionPointer(CallingConvention.Winapi)]
			private delegate void ExeEntry();

			public bool RunDllEntry()
			{
				var entryThunk = (DllEntry)Marshal.GetDelegateForFunctionPointer(EntryPoint, typeof(DllEntry));
				return entryThunk(Z.US(Start), 1, IntPtr.Zero); // DLL_PROCESS_ATTACH
			}
			public void RunExeEntry()
			{
				var entryThunk = (ExeEntry)Marshal.GetDelegateForFunctionPointer(EntryPoint, typeof(ExeEntry));
				entryThunk();
			}

			public PeWrapper(string moduleName, byte[] fileData)
			{
				ModuleName = moduleName;
				_fileData = fileData;
				_pe = new PeFile(fileData);
				Size = _pe.ImageNtHeaders.OptionalHeader.SizeOfImage;

				if (Size < _pe.ImageSectionHeaders.Max(s => (ulong)s.VirtualSize + s.VirtualAddress))
				{
					throw new InvalidOperationException("Image not Big Enough");
				}

				_fileHash = WaterboxUtils.Hash(fileData);
			}

			/// <summary>
			/// set memory protections, finishing the Mount process
			/// </summary>
			public void FinishMount()
			{
				Memory.Protect(Memory.Start, Memory.Size, MemoryBlock.Protection.R);

				foreach (var s in _pe.ImageSectionHeaders)
				{
					ulong start = Start + s.VirtualAddress;
					ulong length = s.VirtualSize;

					MemoryBlock.Protection prot;
					var r = (s.Characteristics & (uint)Constants.SectionFlags.IMAGE_SCN_MEM_READ) != 0;
					var w = (s.Characteristics & (uint)Constants.SectionFlags.IMAGE_SCN_MEM_WRITE) != 0;
					var x = (s.Characteristics & (uint)Constants.SectionFlags.IMAGE_SCN_MEM_EXECUTE) != 0;
					if (w && x)
					{
						throw new InvalidOperationException("Write and Execute not allowed");
					}

					prot = x ? MemoryBlock.Protection.RX : w ? MemoryBlock.Protection.RW : MemoryBlock.Protection.R;

					Memory.Protect(start, length, prot);
				}
			}

			/// <summary>
			/// load the PE into memory
			/// </summary>
			/// <param name="org">start address</param>
			public void Mount(ulong org)
			{
				Start = org;
				LoadOffset = (long)Start - (long)_pe.ImageNtHeaders.OptionalHeader.ImageBase;
				Memory = new MemoryBlock(Start, Size);
				Memory.Activate();
				Memory.Protect(Start, Size, MemoryBlock.Protection.RW);

				// copy headers
				Marshal.Copy(_fileData, 0, Z.US(Start), (int)_pe.ImageNtHeaders.OptionalHeader.SizeOfHeaders);

				// copy sections
				foreach (var s in _pe.ImageSectionHeaders)
				{
					ulong start = Start + s.VirtualAddress;
					ulong length = s.VirtualSize;
					ulong datalength = Math.Min(s.VirtualSize, s.SizeOfRawData);

					Marshal.Copy(_fileData, (int)s.PointerToRawData, Z.US(start), (int)datalength);
					WaterboxUtils.ZeroMemory(Z.US(start + datalength), (long)(length - datalength));
				}

				// apply relocations
				foreach (var rel in _pe.ImageRelocationDirectory)
				{
					foreach (var to in rel.TypeOffsets)
					{
						ulong address = Start + rel.VirtualAddress + to.Offset;

						switch (to.Type)
						{
							// there are many other types of relocation specified,
							// but the only that are used is 0 (does nothing), 3 (32 bit standard), 10 (64 bit standard)

							case 3: // IMAGE_REL_BASED_HIGHLOW
								{
									byte[] tmp = new byte[4];
									Marshal.Copy(Z.US(address), tmp, 0, 4);
									uint val = BitConverter.ToUInt32(tmp, 0);
									tmp = BitConverter.GetBytes((uint)(val + LoadOffset));
									Marshal.Copy(tmp, 0, Z.US(address), 4);
									break;
								}

							case 10: // IMAGE_REL_BASED_DIR64
								{
									byte[] tmp = new byte[8];
									Marshal.Copy(Z.US(address), tmp, 0, 8);
									long val = BitConverter.ToInt64(tmp, 0);
									tmp = BitConverter.GetBytes(val + LoadOffset);
									Marshal.Copy(tmp, 0, Z.US(address), 8);
									break;
								}
						}
					}
				}

				// publish exports
				EntryPoint = Z.US(Start + _pe.ImageNtHeaders.OptionalHeader.AddressOfEntryPoint);
				foreach (var export in _pe.ExportedFunctions)
				{
					if (export.Name != null)
						ExportsByName.Add(export.Name, Z.US(Start + export.Address));
					ExportsByOrdinal.Add(export.Ordinal, Z.US(Start + export.Address));
				}

				// collect information about imports
				// NB: Hints are not the same as Ordinals
				foreach (var import in _pe.ImportedFunctions)
				{
					Dictionary<string, IntPtr> module;
					if (!ImportsByModule.TryGetValue(import.DLL, out module))
					{
						module = new Dictionary<string, IntPtr>();
						ImportsByModule.Add(import.DLL, module);
					}
					module.Add(import.Name, Z.US(Start + import.Thunk));
				}
			}

			public IntPtr Resolve(string entryPoint)
			{
				IntPtr ret;
				ExportsByName.TryGetValue(entryPoint, out ret);
				return ret;
			}

			public void ConnectImports(string moduleName, IImportResolver module)
			{
				Dictionary<string, IntPtr> imports;
				if (ImportsByModule.TryGetValue(moduleName, out imports))
				{
					foreach (var kvp in imports)
					{
						var valueArray = new IntPtr[] { module.SafeResolve(kvp.Key) };
						Marshal.Copy(valueArray, 0, kvp.Value, 1);
					}
				}
			}

			private bool _disposed = false;

			public void Dispose()
			{
				if (!_disposed)
				{
					Memory.Dispose();
					Memory = null;
					_disposed = true;
				}
			}

			const ulong MAGIC = 0x420cccb1a2e17420;

			public void SaveStateBinary(BinaryWriter bw)
			{
				bw.Write(MAGIC);
				bw.Write(_fileHash);
				bw.Write(Start);

				foreach (var s in _pe.ImageSectionHeaders)
				{
					if ((s.Characteristics & (uint)Constants.SectionFlags.IMAGE_SCN_MEM_WRITE) == 0)
						continue;

					ulong start = Start + s.VirtualAddress;
					ulong length = s.VirtualSize;

					var ms = Memory.GetStream(start, length, false);
					bw.Write(length);
					ms.CopyTo(bw.BaseStream);
				}
			}

			public void LoadStateBinary(BinaryReader br)
			{
				if (br.ReadUInt64() != MAGIC)
					throw new InvalidOperationException("Magic not magic enough!");
				if (!br.ReadBytes(_fileHash.Length).SequenceEqual(_fileHash))
					throw new InvalidOperationException("Elf changed disguise!");
				if (br.ReadUInt64() != Start)
					throw new InvalidOperationException("Trickys elves moved on you!");

				Memory.Protect(Memory.Start, Memory.Size, MemoryBlock.Protection.RW);

				foreach (var s in _pe.ImageSectionHeaders)
				{
					if ((s.Characteristics & (uint)Constants.SectionFlags.IMAGE_SCN_MEM_WRITE) == 0)
						continue;

					ulong start = Start + s.VirtualAddress;
					ulong length = s.VirtualSize;

					if (br.ReadUInt64() != length)
						throw new InvalidOperationException("Unexpected section size for " + s.Name);

					var ms = Memory.GetStream(start, length, true);
					WaterboxUtils.CopySome(br.BaseStream, ms, (long)length);
				}

				FinishMount();
			}
		}

		private  class EndOfMainException : Exception
		{
		}

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

			private IntPtr CreateVtable(string moduleName, ICollection<string> entries)
			{
				var imports = _parent._exports[moduleName];
				var ret = Z.US(_parent._sealedheap.Allocate((ulong)(entries.Count * IntPtr.Size), 16));
				var pointers = entries.Select(e =>
				{
					var ptr = imports.Resolve(e);
					if (ptr == IntPtr.Zero)
					{
						var s = string.Format("Trapped on unimplemented function {0}:{1}", moduleName, e);
						Action del = () => { throw new InvalidOperationException(s); };
						_traps.Add(del);
						ptr = Marshal.GetFunctionPointerForDelegate(del);
					}
					return ptr;
				}).ToArray();
				Marshal.Copy(pointers, 0, ret, pointers.Length);
				return ret;
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

				context.SyscallVtable = CreateVtable("__syscalls", Enumerable.Range(0, 340).Select(i => "n" + i).ToList());
				context.LdsoVtable = CreateVtable("__syscalls", new[] // ldso
				{
					"dladdr", "dlinfo", "dlsym", "dlopen", "dlclose", "dlerror", "reset_tls"
				});
				context.PsxVtable = CreateVtable("__syscalls", new[] // psx
				{
					"start_main", "convert_thread", "unmapself", "log_output"
				});
				var extraTable = CreateVtable("__syscalls", new[]
				{
					"pthread_surrogate", "pthread_create", "do_global_ctors", "do_global_dtors"
				});
				{
					var tmp = new IntPtr[4];
					Marshal.Copy(extraTable, tmp, 0, 4);
					context.PthreadSurrogate = tmp[0];
					context.PthreadCreate = tmp[1];
					context.DoGlobalCtors = tmp[2];
					context.DoGlobalDtors = tmp[3];
				}

				return 0; // success
			}
		}

		/// <summary>
		/// special emulator-functions
		/// </summary>
		private class Emu
		{
			private readonly PeRunner _parent;
			public Emu(PeRunner parent)
			{
				_parent = parent;
			}

			[BizExport(CallingConvention.Cdecl, EntryPoint = "alloc_sealed")]
			public IntPtr AllocSealed(UIntPtr size)
			{
				return Z.US(_parent._sealedheap.Allocate((ulong)size, 16));
			}

			[BizExport(CallingConvention.Cdecl, EntryPoint = "alloc_invisible")]
			public IntPtr AllocInvisible(UIntPtr size)
			{
				return Z.US(_parent._invisibleheap.Allocate((ulong)size, 16));
			}

			[BizExport(CallingConvention.Cdecl, EntryPoint = "_debug_puts")]
			public void DebugPuts(IntPtr s)
			{
				Console.WriteLine("_debug_puts:" + Marshal.PtrToStringAnsi(s));
			}
		}

		/// <summary>
		/// syscall emulation layer, as well as a bit of other stuff
		/// </summary>
		private class Syscalls
		{
			private readonly PeRunner _parent;
			public Syscalls(PeRunner parent)
			{
				_parent = parent;
			}

			[BizExport(CallingConvention.Cdecl, EntryPoint = "log_output")]
			public void DebugPuts(IntPtr s)
			{
				Console.WriteLine("_psx_log_output:" + Marshal.PtrToStringAnsi(s));
			}

			[BizExport(CallingConvention.Cdecl, EntryPoint = "n12")]
			public UIntPtr Brk(UIntPtr _p)
			{
				// does MUSL use this?
				var heap = _parent._heap;

				var start = heap.Memory.Start;
				var end = start + heap.Used;
				var max = heap.Memory.End;

				var p = (ulong)_p;

				if (p < start || p > max)
				{
					// failure: return current break
					return Z.UU(end);
				}
				else if (p > end)
				{
					// increase size of heap
					heap.Allocate(p - end, 1);
					return Z.UU(p);
				}
				else if (p < end)
				{
					throw new InvalidOperationException("We don't support shrinking heaps");
				}
				else
				{
					// no change
					return Z.UU(end);
				}
			}

			// aka __psx_init_frame
			// in midipix, this just sets up SEH and does not do anything that start_main does in MUSL normally
			[BizExport(CallingConvention.Cdecl, EntryPoint = "start_main")]
			public int StartMain(IntPtr u0, int u1, IntPtr u2, IntPtr main)
			{
				// since we don't really need main, we can blow up here
				throw new EndOfMainException();
			}
		}

		// usual starting address for the executable
		private static readonly ulong CanonicalStart = 0x0000036f00000000;

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
		private Heap _heap;

		/// <summary>
		/// sealed heap (writable only during init)
		/// </summary>
		private Heap _sealedheap;

		/// <summary>
		/// invisible heap (not savestated, use with care)
		/// </summary>
		private Heap _invisibleheap;

		/// <summary>
		/// all loaded PE files
		/// </summary>
		private readonly List<PeWrapper> _modules = new List<PeWrapper>();

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
		private Emu _emu;
		private Syscalls _syscalls;

		public PeRunner(string directory, string filename, ulong heapsize, ulong sealedheapsize, ulong invisibleheapsize)
		{
			Initialize(_nextStart);
			Enter();
			try
			{
				// load any predefined exports
				_psx = new Psx(this);
				_exports.Add("libpsxscl.so", BizExvoker.GetExvoker(_psx));
				_emu = new Emu(this);
				_exports.Add("libemuhost.so", BizExvoker.GetExvoker(_emu));
				_syscalls = new Syscalls(this);
				_exports.Add("__syscalls", BizExvoker.GetExvoker(_syscalls));

				// load and connect all modules, starting with the executable
				var todoModules = new Queue<string>();
				todoModules.Enqueue(filename);

				while (todoModules.Count > 0)
				{
					var moduleName = todoModules.Dequeue();
					if (!_exports.ContainsKey(moduleName))
					{
						var module = new PeWrapper(moduleName, File.ReadAllBytes(Path.Combine(directory, moduleName)));
						module.Mount(_nextStart);
						ComputeNextStart(module.Size);
						AddMemoryBlock(module.Memory);
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

				foreach (var module in _modules)
				{
					foreach (var name in module.ImportsByModule.Keys)
					{
						module.ConnectImports(name, _exports[name]);
					}
				}
				foreach (var module in _modules)
				{
					module.FinishMount();
				}

				// load all heaps
				_heap = new Heap(_nextStart, heapsize, "brk-heap");
				_heap.Memory.Activate();
				ComputeNextStart(heapsize);
				AddMemoryBlock(_heap.Memory);
				_savestateComponents.Add(_heap);
				_disposeList.Add(_heap);

				_sealedheap = new Heap(_nextStart, sealedheapsize, "sealed-heap");
				_sealedheap.Memory.Activate();
				ComputeNextStart(sealedheapsize);
				AddMemoryBlock(_sealedheap.Memory);
				_savestateComponents.Add(_sealedheap);
				_disposeList.Add(_sealedheap);

				_invisibleheap = new Heap(_nextStart, invisibleheapsize, "invisible-heap");
				_invisibleheap.Memory.Activate();
				ComputeNextStart(invisibleheapsize);
				AddMemoryBlock(_invisibleheap.Memory);
				_savestateComponents.Add(_invisibleheap);
				_disposeList.Add(_invisibleheap);

				try
				{
					_modules[0].RunExeEntry();
					throw new InvalidOperationException("main() returned!");
				}
				catch (EndOfMainException)
				{ }
				foreach (var m in _modules.Skip(1))
				{
					if (!m.RunDllEntry())
						throw new InvalidOperationException("DllMain() returned false");
				}
			}
			catch
			{
				Dispose();
				throw;
			}
			finally
			{
				Exit();
			}
		}

		public IntPtr Resolve(string entryPoint)
		{
			// modules[0] is always the main module
			return _modules[0].Resolve(entryPoint);
		}

		public void Seal()
		{
			using (this.EnterExit())
			{
				_sealedheap.Seal();
			}
		}

		public void SaveStateBinary(BinaryWriter bw)
		{
			using (this.EnterExit())
			{
				bw.Write(_savestateComponents.Count);
				foreach (var c in _savestateComponents)
				{
					c.SaveStateBinary(bw);
				}
			}
		}

		public void LoadStateBinary(BinaryReader br)
		{
			if (br.ReadInt32() != _savestateComponents.Count)
				throw new InvalidOperationException("Internal savestate error");
			using (this.EnterExit())
			{
				foreach (var c in _savestateComponents)
				{
					c.LoadStateBinary(br);
				}
			}
		}

		private bool _disposed = false;

		public void Dispose()
		{
			if (!_disposed)
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
				_disposed = true;
			}
		}
	}
}
