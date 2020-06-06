using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Waterbox
{
	/// <summary>
	/// syscall emulation layer
	/// </summary>
	internal partial class Syscalls : IBinaryStateable
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

		private readonly WaterboxHost _parent;
		public Syscalls(WaterboxHost parent)
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

		private Stream StreamForFd(int fd)
		{
			if (fd >= 0 && fd < _openFiles.Count)
				return _openFiles[fd].Stream;
			else
				return null;
		}

		[BizExport(CallingConvention.Cdecl, EntryPoint = "__wsyscalltab[12]")]
		public UIntPtr Brk(UIntPtr _p)
		{
			var heap = _parent._heap;

			var start = heap.Memory.Start;
			var currentEnd = start + heap.Used;
			var maxEnd = heap.Memory.EndExclusive;

			var p = (ulong)_p;

			if (p < start || p > maxEnd)
			{
				// failure: return current break
				return Z.UU(currentEnd);
			}
			else if (p > currentEnd)
			{
				// increase size of heap
				heap.Allocate(p - currentEnd, 1);
				return Z.UU(p);
			}
			else if (p < currentEnd)
			{
				throw new InvalidOperationException("We don't support shrinking heaps");
			}
			else
			{
				// no change
				return Z.UU(currentEnd);
			}
		}

		[BizExport(CallingConvention.Cdecl, EntryPoint = "__wsyscalltab[16]")]
		public int IoCtl(int fd, ulong req)
		{
			return 0; // sure it worked, honest
		}

		public struct Iovec
		{
			public IntPtr Base;
			public ulong Length;
		}

		[BizExport(CallingConvention.Cdecl, EntryPoint = "__wsyscalltab[0]")]
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

		[BizExport(CallingConvention.Cdecl, EntryPoint = "__wsyscalltab[1]")]
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

		[BizExport(CallingConvention.Cdecl, EntryPoint = "__wsyscalltab[19]")]
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
		[BizExport(CallingConvention.Cdecl, EntryPoint = "__wsyscalltab[20]")]
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

		[BizExport(CallingConvention.Cdecl, EntryPoint = "__wsyscalltab[2]")]
		public long Open(string path, int flags, int mode)
		{
			if (!_availableFiles.TryGetValue(path, out var o))
				return -ENOENT;
			if (_openFiles.Contains(o))
				return -EACCES;
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
					return -EINVAL;
			}
			if (!o.Open(access))
				return -EACCES;
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
		[BizExport(CallingConvention.Cdecl, EntryPoint = "__wsyscalltab[3]")]
		public long Close(int fd)
		{
			if (fd < 0 || fd >= _openFiles.Count)
				return -EBADF;
			var o = _openFiles[fd];
			if (o == null)
				return -EBADF;
			if (!o.Close())
				return -EIO;
			_openFiles[fd] = null;
			return 0;
		}
		[BizExport(CallingConvention.Cdecl, EntryPoint = "__wsyscalltab[8]")]
		public long Seek(int fd, long offset, int type)
		{
			var s = StreamForFd(fd);
			if (s == null)
				return -EBADF;
			if (!s.CanSeek)
				return -ESPIPE;
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
					return -EINVAL;
			}
			try
			{
				return s.Seek(offset, o);
			}
			catch (IOException)
			{
				// usually means bad offset, since we already filtered out on CanSeek
				return -EINVAL;
			}
		}
		[BizExport(CallingConvention.Cdecl, EntryPoint = "__wsyscalltab[77]")]
		public long FTruncate(int fd, long length)
		{
			var s = StreamForFd(fd);
			if (s == null)
				return -EBADF;
			if (!s.CanWrite)
				return -ESPIPE;
			if (!s.CanSeek)
				return -ESPIPE;
			s.SetLength(length);
			return 0;
		}

		// TODO: Remove this entirely once everything is compiled against the new libc
		[BizExport(CallingConvention.Cdecl, EntryPoint = "__wsyscalltab[205]")]
		public long SetThreadArea(IntPtr uinfo)
		{
			return 38; // ENOSYS
		}

		[BizExport(CallingConvention.Cdecl, EntryPoint = "__wsyscalltab[218]")]
		public long SetTidAddress(IntPtr address)
		{
			// pretend we succeeded
			return 8675309; // arbitrary thread id
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct TimeSpec
		{
			public long Seconds;
			public long NanoSeconds;
		}

		[BizExport(CallingConvention.Cdecl, EntryPoint = "__wsyscalltab[228]")]
		public unsafe int SysClockGetTime(int which, TimeSpec* time)
		{
			time->Seconds = 1495889068;
			time->NanoSeconds = 0;
			return 0;
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
}
