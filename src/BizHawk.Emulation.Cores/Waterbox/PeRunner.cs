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

			[BizExport(CallingConvention.Cdecl, EntryPoint = "alloc_plain")]
			public IntPtr AllocPlain(UIntPtr size)
			{
				return Z.US(_parent._plainheap.Allocate((ulong)size, 16));
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
		private class Syscalls : IBinaryStateable
		{
			public interface IFileObject : IBinaryStateable
			{
				bool Open(FileAccess access);
				bool Close();
				Stream Stream { get; }
				string Name { get; }
			}

			private class SpecialFile : IFileObject
			{
				// stdin, stdout, stderr
				public string Name { get; }
				public Stream Stream { get; }
				public bool Close() => false;
				public bool Open(FileAccess access) => false;

				public void SaveStateBinary(BinaryWriter writer) { }
				public void LoadStateBinary(BinaryReader reader) { }

				public SpecialFile(Stream stream, string name)
				{
					Stream = stream;
					Name = name;
				}
			}

			private class ReadonlyFirmware : IFileObject
			{
				private readonly byte[] _data;
				private readonly byte[] _hash;

				public string Name { get; }
				public Stream Stream { get; private set; }
				public bool Close()
				{
					if (Stream == null)
						return false;
					Stream = null;
					return true;
				}

				public bool Open(FileAccess access)
				{
					if (Stream != null || access != FileAccess.Read)
						return false;
					Stream = new MemoryStream(_data, false);
					return true;
				}

				public void LoadStateBinary(BinaryReader br)
				{
					if (!br.ReadBytes(_hash.Length).SequenceEqual(_hash))
						throw new InvalidOperationException("Savestate internal firmware mismatch");
					var pos = br.ReadInt64();
					if (pos == -1)
					{
						Stream = null;
					}
					else
					{
						if (Stream == null)
							Open(FileAccess.Read);
						Stream.Position = pos;
					}
				}

				public void SaveStateBinary(BinaryWriter bw)
				{
					bw.Write(_hash);
					bw.Write(Stream != null ? Stream.Position : -1);
				}

				public ReadonlyFirmware(byte[] data, string name)
				{
					_data = data;
					_hash = WaterboxUtils.Hash(data);
					Name = name;
				}
			}

			private class TransientFile : IFileObject
			{
				private bool _inUse = false;
				public string Name { get; }
				public Stream Stream { get; }
				public bool Close()
				{
					if (_inUse)
					{
						_inUse = false;
						return true;
					}
					else
					{
						return false;
					}
				}

				public bool Open(FileAccess access)
				{
					if (_inUse)
					{
						return false;
					}
					else
					{
						// TODO: if access != RW, the resultant handle lets you do those all anyway
						_inUse = true;
						Stream.Position = 0;
						return true;
					}
				}

				public void LoadStateBinary(BinaryReader br)
				{
					throw new InvalidOperationException("Internal savestate error!");
				}

				public void SaveStateBinary(BinaryWriter bw)
				{
					throw new InvalidOperationException("Transient files cannot be savestated!");
				}

				public TransientFile(byte[] data, string name)
				{
					Stream = new MemoryStream();
					Name = name;
					if (data != null)
					{
						Stream.Write(data, 0, data.Length);
						Stream.Position = 0;
					}
				}

				public byte[] GetContents()
				{
					if (_inUse)
						throw new InvalidOperationException();
					return ((MemoryStream)Stream).ToArray();
				}
			}

			private readonly List<IFileObject> _openFiles = new List<IFileObject>();
			private readonly Dictionary<string, IFileObject> _availableFiles = new Dictionary<string, IFileObject>();

			private readonly PeRunner _parent;
			public Syscalls(PeRunner parent)
			{
				_parent = parent;
				var stdin = new SpecialFile(Stream.Null, "___stdin");
				var stdout = new SpecialFile(Console.OpenStandardOutput(), "___stdout");
				var stderr = new SpecialFile(Console.OpenStandardError(), "___stderr");

				_openFiles = new List<IFileObject>
				{
					stdin,
					stdout,
					stderr
				};
				_availableFiles = new Dictionary<string, IFileObject>
				{
					[stdin.Name] = stdin,
					[stdout.Name] = stdout,
					[stderr.Name] = stderr
				};
			}

			private IntPtr _pthreadSelf;

			public void Init()
			{
				// as the inits are done in a defined order with a defined memory map,
				// we don't need to savestate _pthreadSelf, only its contents
				_pthreadSelf = Z.US(_parent._plainheap.Allocate(512, 1));
			}

			private Stream StreamForFd(int fd)
			{
				if (fd >= 0 && fd < _openFiles.Count)
					return _openFiles[fd].Stream;
				else
					return null;
			}

			[BizExport(CallingConvention.Cdecl, EntryPoint = "log_output")]
			public void DebugPuts(IntPtr s)
			{
				Console.WriteLine("_psx_log_output:" + Marshal.PtrToStringAnsi(s));
			}

			[BizExport(CallingConvention.Cdecl, EntryPoint = "n12")]
			public UIntPtr Brk(UIntPtr _p)
			{
				var heap = _parent._heap;

				var start = heap.Memory.Start;
				var end = start + heap.Used;
				var max = heap.Memory.EndExclusive;

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
				var s = StreamForFd(fd);
				if (s == null || !s.CanRead)
					return -1;
				var tmp = new byte[count];
				var ret = s.Read(tmp, 0, (int)count);
				Marshal.Copy(tmp, 0, buff, ret);
				return ret;
			}

			[BizExport(CallingConvention.Cdecl, EntryPoint = "n1")]
			public long Write(int fd, IntPtr buff, ulong count)
			{
				var s = StreamForFd(fd);
				if (s == null || !s.CanWrite)
					return -1;
				var tmp = new byte[count];
				Marshal.Copy(buff, tmp, 0, (int)count);
				s.Write(tmp, 0, tmp.Length);
				return (long)count;
			}

			[BizExport(CallingConvention.Cdecl, EntryPoint = "n19")]
			public unsafe long Readv(int fd, Iovec* iov, int iovcnt)
			{
				long ret = 0;
				for (int i = 0; i < iovcnt; i++)
				{
					var len = Read(fd, iov[i].Base, iov[i].Length);
					if (len < 0)
						return len;
					ret += len;
					if (len != (long)iov[i].Length)
						break;
				}
				return ret;
			}
			[BizExport(CallingConvention.Cdecl, EntryPoint = "n20")]
			public unsafe long Writev(int fd, Iovec* iov, int iovcnt)
			{
				long ret = 0;
				for (int i = 0; i < iovcnt; i++)
				{
					if (iov[i].Base != IntPtr.Zero)
						ret += Write(fd, iov[i].Base, iov[i].Length);
				}
				return ret;
			}

			[BizExport(CallingConvention.Cdecl, EntryPoint = "n2")]
			public int Open(string path, int flags, int mode)
			{
				if (!_availableFiles.TryGetValue(path, out var o))
					return -1;
				if (_openFiles.Contains(o))
					return -1;
				FileAccess access;
				switch (flags & 3)
				{
					case 0:
						access = FileAccess.Read;
						break;
					case 1:
						access = FileAccess.Write;
						break;
					case 2:
						access = FileAccess.ReadWrite;
						break;
					default:
						return -1;
				}
				if (!o.Open(access))
					return -1;
				int fd;
				for (fd = 0; fd < _openFiles.Count; fd++)
					if (_openFiles[fd] == null)
						break;
				if (fd == _openFiles.Count)
					_openFiles.Add(o);
				else
					_openFiles[fd] = o;
				return fd;
			}
			[BizExport(CallingConvention.Cdecl, EntryPoint = "n3")]
			public int Close(int fd)
			{
				if (fd < 0 || fd >= _openFiles.Count)
					return -1;
				var o = _openFiles[fd];
				if (o == null || !o.Close())
					return -1;
				_openFiles[fd] = null;
				return 0;
			}
			[BizExport(CallingConvention.Cdecl, EntryPoint = "n8")]
			public long Seek(int fd, long offset, int type)
			{
				var s = StreamForFd(fd);
				if (s == null || !s.CanSeek)
					return -1;
				SeekOrigin o;
				switch (type)
				{
					case 0:
						o = SeekOrigin.Begin;
						break;
					case 1:
						o = SeekOrigin.Current;
						break;
					case 2:
						o = SeekOrigin.End;
						break;
					default:
						return -1;
				}
				return s.Seek(offset, o);
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

			//bool _firstTime = true;

			// aka __psx_init_frame (it's used elsewhere for thread setup)
			// in midipix, this just sets up a SEH frame and then calls musl's start_main
			[BizExport(CallingConvention.Cdecl, EntryPoint = "start_main")]
			public unsafe int StartMain(IntPtr main, int argc, IntPtr argv, IntPtr libc_start_main)
			{
				//var del = (LibcStartMain)CallingConventionAdapters.Waterbox.GetDelegateForFunctionPointer(libc_start_main, typeof(LibcStartMain));
				// this will init, and then call user main, and then call exit()
				//del(main, argc, argv);
				//int* foobar = stackalloc int[128];


				// if we return from this, psx will then halt, so break out
				//if (_firstTime)
				//{
				//_firstTime = false;
				throw new InvalidOperationException("This shouldn't be called");
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
			public int SysClockGetTime(int which, [In, Out] TimeSpec time)
			{
				time.Seconds = 1495889068;
				time.NanoSeconds = 0;
				return 0;
			}

			[BizExport(CallingConvention.Cdecl, EntryPoint = "n9")]
			public IntPtr MMap(IntPtr address, UIntPtr size, int prot, int flags, int fd, IntPtr offs)
			{
				if (address != IntPtr.Zero)
					return Z.SS(-1);
				MemoryBlockBase.Protection mprot;
				switch (prot)
				{
					case 0: mprot = MemoryBlockBase.Protection.None; break;
					default:
					case 6: // W^X
					case 7: // W^X
					case 4: // exec only????
					case 2: return Z.SS(-1); // write only????
					case 3: mprot = MemoryBlockBase.Protection.RW; break;
					case 1: mprot = MemoryBlockBase.Protection.R; break;
					case 5: mprot = MemoryBlockBase.Protection.RX; break;
				}
				if ((flags & 0x20) == 0)
				{
					// MAP_ANONYMOUS is required
					return Z.SS(-1);
				}
				if ((flags & 0xf00) != 0)
				{
					// various unsupported flags
					return Z.SS(-1);
				}

				var ret = _parent._mmapheap.Map((ulong)size, mprot);
				return ret == 0 ? Z.SS(-1) : Z.US(ret);
			}
			[BizExport(CallingConvention.Cdecl, EntryPoint = "n25")]
			public IntPtr MRemap(UIntPtr oldAddress, UIntPtr oldSize,
				UIntPtr newSize, int flags)
			{
				if ((flags & 2) != 0)
				{
					// don't support MREMAP_FIXED
					return Z.SS(-1);
				}
				var ret = _parent._mmapheap.Remap((ulong)oldAddress, (ulong)oldSize, (ulong)newSize,
					(flags & 1) != 0);
				return ret == 0 ? Z.SS(-1) : Z.US(ret);
			}
			[BizExport(CallingConvention.Cdecl, EntryPoint = "n11")]
			public int MUnmap(UIntPtr address, UIntPtr size)
			{
				return _parent._mmapheap.Unmap((ulong)address, (ulong)size) ? 0 : -1;
			}

			[BizExport(CallingConvention.Cdecl, EntryPoint = "n10")]
			public int MProtect(UIntPtr address, UIntPtr size, int prot)
			{
				MemoryBlockBase.Protection mprot;
				switch (prot)
				{
					case 0: mprot = MemoryBlockBase.Protection.None; break;
					default:
					case 6: // W^X
					case 7: // W^X
					case 4: // exec only????
					case 2: return -1; // write only????
					case 3: mprot = MemoryBlockBase.Protection.RW; break;
					case 1: mprot = MemoryBlockBase.Protection.R; break;
					case 5: mprot = MemoryBlockBase.Protection.RX; break;
				}
				return _parent._mmapheap.Protect((ulong)address, (ulong)size, mprot) ? 0 : -1;
			}

			public void SaveStateBinary(BinaryWriter bw)
			{
				bw.Write(_availableFiles.Count);
				foreach (var f in _availableFiles.Values.OrderBy(f => f.Name))
				{
					bw.Write(f.Name);
					f.SaveStateBinary(bw);
				}
				bw.Write(_openFiles.Count);
				foreach (var f in _openFiles)
				{
					bw.Write(f != null);
					if (f != null)
						bw.Write(f.Name);
				}
			}

			public void LoadStateBinary(BinaryReader br)
			{
				if (_availableFiles.Count != br.ReadInt32())
					throw new InvalidOperationException("Internal savestate error:  Filelist change");
				foreach (var f in _availableFiles.Values.OrderBy(f => f.Name))
				{
					if (br.ReadString() != f.Name)
						throw new InvalidOperationException("Internal savestate error:  Filelist change");
					f.LoadStateBinary(br);
				}
				var c = br.ReadInt32();
				_openFiles.Clear();
				for (int i = 0; i < c; i++)
				{
					_openFiles.Add(br.ReadBoolean() ? _availableFiles[br.ReadString()] : null);
				}
			}


			private T RemoveFileInternal<T>(string name)
				where T : IFileObject
			{
				if (!_availableFiles.TryGetValue(name, out var o))
					throw new InvalidOperationException("File was never registered!");
				if (o.GetType() != typeof(T))
					throw new InvalidOperationException("Object was not a the right kind of file");
				if (_openFiles.Contains(o))
					throw new InvalidOperationException("Core never closed the file!");
				_availableFiles.Remove(name);
				return (T)o;
			}

			public void AddReadonlyFile(byte[] data, string name)
			{
				_availableFiles.Add(name, new ReadonlyFirmware(data, name));
			}

			public void RemoveReadonlyFile(string name)
			{
				RemoveFileInternal<ReadonlyFirmware>(name);
			}

			public void AddTransientFile(byte[] data, string name)
			{
				_availableFiles.Add(name, new TransientFile(data, name));
			}
			public byte[] RemoveTransientFile(string name)
			{
				return RemoveFileInternal<TransientFile>(name).GetContents();
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
		private Heap _plainheap;

		/// <summary>
		/// memory map emulation
		/// </summary>
		private MapHeap _mmapheap;

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
		private Emu _emu;
		private Syscalls _syscalls;
		private LibcPatch _libcpatch;

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
				_emu = new Emu(this);
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

				_libcpatch = new LibcPatch(this);
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

				_syscalls.Init();

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
