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
		/// </summary>
		public uint NormalHeapSizeKB { get; set; }

		/// <summary>
		/// how large the sealed heap should be.  it services special allocations that become readonly after init
		/// </summary>
		public uint SealedHeapSizeKB { get; set; }

		/// <summary>
		/// how large the invisible heap should be.  it services special allocations which are not savestated
		/// </summary>
		public uint InvisibleHeapSizeKB { get; set; }

		/// <summary>
		/// how large the special heap should be.  it is savestated, and contains ??
		/// </summary>
		public uint SpecialHeapSizeKB { get; set; }
	}


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

			/// <summary>
			/// for midipix-built PEs, pointer to the construtors to run during init
			/// </summary>
			public IntPtr CtorList { get; private set; }
			/// <summary>
			/// for midipix-build PEs, pointer to the destructors to run during fini
			/// </summary>
			public IntPtr DtorList { get; private set; }

			/*[UnmanagedFunctionPointer(CallingConvention.Winapi)]
			private delegate bool DllEntry(IntPtr instance, int reason, IntPtr reserved);
			[UnmanagedFunctionPointer(CallingConvention.Winapi)]
			private delegate void ExeEntry();*/
			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			private delegate void GlobalCtor();

			/*public bool RunDllEntry()
			{
				var entryThunk = (DllEntry)Marshal.GetDelegateForFunctionPointer(EntryPoint, typeof(DllEntry));
				return entryThunk(Z.US(Start), 1, IntPtr.Zero); // DLL_PROCESS_ATTACH
			}
			public void RunExeEntry()
			{
				var entryThunk = (ExeEntry)Marshal.GetDelegateForFunctionPointer(EntryPoint, typeof(ExeEntry));
				entryThunk();
			}*/
			public unsafe void RunGlobalCtors()
			{
				int did = 0;
				if (CtorList != IntPtr.Zero)
				{
					IntPtr* p = (IntPtr*)CtorList;
					IntPtr f;
					while ((f = *++p) != IntPtr.Zero) // skip 0th dummy pointer
					{
						var ctorThunk = (GlobalCtor)Marshal.GetDelegateForFunctionPointer(f, typeof(GlobalCtor));
						//Console.WriteLine(f);
						//System.Diagnostics.Debugger.Break();
						ctorThunk();
						did++;
					}
				}

				if (did > 0)
				{
					Console.WriteLine($"Did {did} global ctors for {ModuleName}");
				}
				else
				{
					Console.WriteLine($"Warn: no global ctors for {ModuleName}; possibly no C++?");
				}
			}

			public PeWrapper(string moduleName, byte[] fileData, ulong destAddress)
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
				Mount(destAddress);
			}

			/// <summary>
			/// set memory protections.
			/// </summary>
			private void ProtectMemory()
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
			private void Mount(ulong org)
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
				var n32 = 0;
				var n64 = 0;
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
									n32++;
									break;
								}

							case 10: // IMAGE_REL_BASED_DIR64
								{
									byte[] tmp = new byte[8];
									Marshal.Copy(Z.US(address), tmp, 0, 8);
									long val = BitConverter.ToInt64(tmp, 0);
									tmp = BitConverter.GetBytes(val + LoadOffset);
									Marshal.Copy(tmp, 0, Z.US(address), 8);
									n64++;
									break;
								}
						}
					}
				}
				if (IntPtr.Size == 8 && n32 > 0)
				{
					// check mcmodel, etc
					throw new InvalidOperationException("32 bit relocations found in 64 bit dll!  This will fail.");
				}
				Console.WriteLine($"Processed {n32} 32 bit and {n64} 64 bit relocations");

				ProtectMemory();

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

				var midipix = _pe.ImageSectionHeaders.Where(s => s.Name.SequenceEqual(Encoding.ASCII.GetBytes(".midipix")))
					.SingleOrDefault();
				if (midipix != null)
				{
					var dataOffset = midipix.PointerToRawData;
					CtorList = Z.SS(BitConverter.ToInt64(_fileData, (int)(dataOffset + 0x30)) + LoadOffset);
					DtorList = Z.SS(BitConverter.ToInt64(_fileData, (int)(dataOffset + 0x38)) + LoadOffset);
				}

				Console.WriteLine($"Mounted `{ModuleName}` @{Start:x16}");
				foreach (var s in _pe.ImageSectionHeaders.OrderBy(s => s.VirtualAddress))
				{
					var r = (s.Characteristics & (uint)Constants.SectionFlags.IMAGE_SCN_MEM_READ) != 0;
					var w = (s.Characteristics & (uint)Constants.SectionFlags.IMAGE_SCN_MEM_WRITE) != 0;
					var x = (s.Characteristics & (uint)Constants.SectionFlags.IMAGE_SCN_MEM_EXECUTE) != 0;
					Console.WriteLine("  @{0:x16} {1}{2}{3} `{4}` {5} bytes",
						Start + s.VirtualAddress,
						r ? "R" : " ",
						w ? "W" : " ",
						x ? "X" : " ",
						Encoding.ASCII.GetString(s.Name),
						s.VirtualSize);
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

				ProtectMemory();
			}
		}

		private class EndOfMainException : Exception
		{
		}

		/// <summary>
		/// serves as a standin for libpsxscl.so
		/// </summary>
		private class Psx
		{
			private static IntPtr BEXLAND;
			static Psx()
			{
				BEXLAND = Marshal.AllocHGlobal(16);
			}

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
					var ptr = imports.Resolve(e);
					if (ptr == IntPtr.Zero)
					{
						var s = string.Format("Trapped on unimplemented function {0}:{1}", moduleName, e);
						Action del = () =>
						{
							Console.WriteLine(s);
							System.Diagnostics.Debugger.Break(); // do not remove this until all unwindings are fixed
							throw new InvalidOperationException(s);
						};
						_traps.Add(del);
						ptr = Marshal.GetFunctionPointerForDelegate(del);
						//ptr = BEXLAND;
					}
					return ptr;
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

			private IntPtr _pthreadSelf;

			public void Init()
			{
				// as the inits are done in a defined order with a defined memory map,
				// we don't need to savestate _pthreadSelf
				_pthreadSelf = Z.US(_parent._specheap.Allocate(65536, 1));
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

			[BizExport(CallingConvention.Cdecl, EntryPoint = "n16")]
			public int IoCtl(int fd, ulong req)
			{
				return 0; // sure it worked, honest
			}

			public struct Iovec
			{
				public IntPtr Base;
				public ulong Length;
			}

			[BizExport(CallingConvention.Cdecl, EntryPoint = "n0")]
			public long Read(int fd, IntPtr buff, ulong count)
			{
				return 0;
			}

			[BizExport(CallingConvention.Cdecl, EntryPoint = "n1")]
			public long Write(int fd, IntPtr buff, ulong count)
			{
				return (long)count;
			}

			[BizExport(CallingConvention.Cdecl, EntryPoint = "n19")]
			public unsafe long Readv(int fd, Iovec* iov, int iovcnt)
			{
				return 0;
			}
			[BizExport(CallingConvention.Cdecl, EntryPoint = "n20")]
			public unsafe long Writev(int fd, Iovec* iov, int iovcnt)
			{
				long ret = 0;
				for (int i = 0; i < iovcnt; i++)
				{
					ret += (long)iov[i].Length;
				}
				return ret;
			}

			[BizExport(CallingConvention.Cdecl, EntryPoint = "n2")]
			public int Open(string path, int flags, int mode)
			{
				return -1;
			}
			[BizExport(CallingConvention.Cdecl, EntryPoint = "n3")]
			public int Close(int fd)
			{
				return 0;
			}

			[BizExport(CallingConvention.Cdecl, EntryPoint = "n4")]
			public int Stat(string path, IntPtr statbuf)
			{
				return -1;
			}

			[BizExport(CallingConvention.Cdecl, EntryPoint = "n5")]
			public int Fstat(int fd, IntPtr statbuf)
			{
				return -1;
			}

			//[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			//private delegate int LibcStartMain(IntPtr main, int argc, IntPtr argv);

			bool _firstTime = true;

			// aka __psx_init_frame (it's used elsewhere for thread setup)
			// in midipix, this just sets up a SEH frame and then calls musl's start_main
			[BizExport(CallingConvention.Cdecl, EntryPoint = "start_main")]
			public unsafe int StartMain(IntPtr main, int argc, IntPtr argv, IntPtr libc_start_main)
			{
				//var del = (LibcStartMain)Marshal.GetDelegateForFunctionPointer(libc_start_main, typeof(LibcStartMain));
				// this will init, and then call user main, and then call exit()
				//del(main, argc, argv);
				//int* foobar = stackalloc int[128];


				// if we return from this, psx will then halt, so break out
				//if (_firstTime)
				//{
					//_firstTime = false;
					throw new EndOfMainException();
				//}
				//else
				//{
				//	return 0;
				//}
			}

			[BizExport(CallingConvention.Cdecl, EntryPoint = "pthread_self")]
			public IntPtr PthreadSelf()
			{
				return _pthreadSelf;
			}

			/*[BizExport(CallingConvention.Cdecl, EntryPoint = "convert_thread")]
            public void ConvertThread()
            {

            }*/

			[BizExport(CallingConvention.Cdecl, EntryPoint = "n218")]
			public long SetTidAddress(IntPtr address)
			{
				return 8675309;
			}

			[StructLayout(LayoutKind.Sequential)]
			public class TimeSpec
			{
				public long Seconds;
				public long NanoSeconds;
			}

			[BizExport(CallingConvention.Cdecl, EntryPoint = "n228")]
			public int SysClockGetTime(int which, [In,Out] TimeSpec time)
			{
				time.Seconds = 1495889068;
				time.NanoSeconds = 0;
				return 0;
			}
		}

		/// <summary>
		/// libc.so functions that we want to redirect
		/// </summary>
		private class LibcPatch
		{
			private readonly PeRunner _parent;
			public LibcPatch(PeRunner parent)
			{
				_parent = parent;
			}


			private bool _didOnce = false;
			private readonly Dictionary<uint, IntPtr> _specificKeys = new Dictionary<uint, IntPtr>();
			private uint _nextSpecificKey = 401;

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			public delegate void PthreadCallback();

			// pthread stuff:
			// since we don't allow multiple threads (for now), this is all pretty simple
			/*
            // int pthread_key_create(pthread_key_t *key, void (*destructor)(void*));
            [BizExport(CallingConvention.Cdecl, EntryPoint = "pthread_key_create")]
            public int PthreadKeyCreate(ref uint key, PthreadCallback destructor)
            {
                key = _nextSpecificKey++;
                _specificKeys.Add(key, IntPtr.Zero);
                return 0;
            }
            // int pthread_key_delete(pthread_key_t key);
            [BizExport(CallingConvention.Cdecl, EntryPoint = "pthread_key_delete")]
            public int PthreadKeyDelete(uint key)
            {
                _specificKeys.Remove(key);
                return 0;
            }
            // int pthread_setspecific(pthread_key_t key, const void *value);
            [BizExport(CallingConvention.Cdecl, EntryPoint = "pthread_setspecific")]
            public int PthreadSetSpecific(uint key, IntPtr value)
            {
                _specificKeys[key] = value;
                return 0;
            }
            // void *pthread_getspecific(pthread_key_t key);
            [BizExport(CallingConvention.Cdecl, EntryPoint = "pthread_getspecific")]
            public IntPtr PthreadGetSpecific(uint key)
            {
                IntPtr ret;
                _specificKeys.TryGetValue(key, out ret);
                return ret;
            }

            // int pthread_once(pthread_once_t* once_control, void (*init_routine)(void));
            [BizExport(CallingConvention.Cdecl, EntryPoint = "pthread_once")]
            public int PthreadOnce(IntPtr control, PthreadCallback init)
            {
                if (!_didOnce)
                {
                    System.Diagnostics.Debugger.Break();
                    _didOnce = true;
                    init();
                }
                return 0;
            }

            // int pthread_mutex_init(pthread_mutex_t *mutex, const pthread_mutexattr_t* attr);
            [BizExport(CallingConvention.Cdecl, EntryPoint = "pthread_mutex_init")]
            public int PthreadMutexInit(IntPtr mutex, IntPtr attr) { return 0; }
            // int pthread_mutex_destroy(pthread_mutex_t* mutex);
            [BizExport(CallingConvention.Cdecl, EntryPoint = "pthread_mutex_destroy")]
            public int PthreadMutexDestroy(IntPtr mutex) { return 0; }

            // int pthread_mutex_lock(pthread_mutex_t* mutex);
            [BizExport(CallingConvention.Cdecl, EntryPoint = "pthread_mutex_lock")]
            public int PthreadMutexLock(IntPtr mutex) { return 0; }
            // int pthread_mutex_trylock(pthread_mutex_t* mutex);
            [BizExport(CallingConvention.Cdecl, EntryPoint = "pthread_mutex_trylock")]
            public int PthreadMutexTryLock(IntPtr mutex) { return 0; }
            // int pthread_mutex_unlock(pthread_mutex_t* mutex);
            [BizExport(CallingConvention.Cdecl, EntryPoint = "pthread_mutex_unlock")]
            public int PthreadMutexUnlock(IntPtr mutex) { return 0; }*/


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
		/// extra savestated heap
		/// </summary>
		private Heap _specheap;

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
		private LibcPatch _libcpatch;

		/// <summary>
		/// timestamp of creation acts as a sort of "object id" in the savestate
		/// </summary>
		private readonly long _createstamp = WaterboxUtils.Timestamp();

		private Heap CreateHeapHelper(uint sizeKB, string name, bool saveStated)
		{
			var heap = new Heap(_nextStart, sizeKB * 1024, name);
			heap.Memory.Activate();
			ComputeNextStart(sizeKB * 1024);
			AddMemoryBlock(heap.Memory);
			if (saveStated)
				_savestateComponents.Add(heap);
			_disposeList.Add(heap);
			return heap;
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void LibcEntryRoutineD(IntPtr appMain, IntPtr psxInit, int options);

		public PeRunner(PeRunnerOptions opt)
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
				todoModules.Enqueue(opt.Filename);

				while (todoModules.Count > 0)
				{
					var moduleName = todoModules.Dequeue();
					if (!_exports.ContainsKey(moduleName))
					{
						var module = new PeWrapper(moduleName, File.ReadAllBytes(Path.Combine(opt.Path, moduleName)), _nextStart);
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

				_libcpatch = new LibcPatch(this);
				_exports["libc.so"] = new PatchImportResolver(_exports["libc.so"], BizExvoker.GetExvoker(_libcpatch));

				ConnectAllImports();

				// load all heaps
				_heap = CreateHeapHelper(opt.NormalHeapSizeKB, "brk-heap", true);
				_sealedheap = CreateHeapHelper(opt.SealedHeapSizeKB, "sealed-heap", true);
				_invisibleheap = CreateHeapHelper(opt.InvisibleHeapSizeKB, "invisible-heap", false);
				_specheap = CreateHeapHelper(opt.SpecialHeapSizeKB, "special-heap", true);

				_syscalls.Init();

				//System.Diagnostics.Debugger.Break();

				// run unmanaged init code
				var libcEnter = _exports["libc.so"].SafeResolve("__libc_entry_routine");
				var psxInit = _exports["libpsxscl.so"].SafeResolve("__psx_init");

				var del = (LibcEntryRoutineD)Marshal.GetDelegateForFunctionPointer(libcEnter, typeof(LibcEntryRoutineD));
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
					Console.WriteLine("Restoring PeRunner state from a different core...");
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
				_specheap = null;
			}
		}
	}
}
