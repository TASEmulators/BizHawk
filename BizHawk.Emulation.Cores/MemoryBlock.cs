using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace BizHawk.Emulation.Cores
{
	public static class Z
	{
		public static IntPtr US(ulong l)
		{
			if (IntPtr.Size == 8)
				return (IntPtr)(long)l;
			else
				return (IntPtr)(int)l;
		}

		public static UIntPtr UU(ulong l)
		{
			if (UIntPtr.Size == 8)
				return (UIntPtr)l;
			else
				return (UIntPtr)(uint)l;
		}

		public static IntPtr SS(long l)
		{
			if (IntPtr.Size == 8)
				return (IntPtr)l;
			else
				return (IntPtr)(int)l;
		}

		public static UIntPtr SU(long l)
		{
			if (UIntPtr.Size == 8)
				return (UIntPtr)(ulong)l;
			else
				return (UIntPtr)(uint)l;
		}
	}

	public sealed class MemoryBlock : IDisposable
	{
		/// <summary>
		/// starting address of the memory block
		/// </summary>
		public ulong Start { get; private set; }
		/// <summary>
		/// total size of the memory block
		/// </summary>
		public ulong Size { get; private set; }
		/// <summary>
		/// ending address of the memory block; equal to start + size
		/// </summary>
		public ulong End { get; private set; }
		public int PageSize { get { return Environment.SystemPageSize; } }

		/// <summary>
		/// allocate size bytes at any address
		/// </summary>
		/// <param name="size"></param>
		public MemoryBlock(ulong size)
			: this(0, size)
		{
		}

		/// <summary>
		/// allocate size bytes starting at a particular address
		/// </summary>
		/// <param name="start"></param>
		/// <param name="size"></param>
		public MemoryBlock(ulong start, ulong size)
		{
#if !MONO
			Start = (ulong)Kernel32.VirtualAlloc(Z.UU(start), Z.UU(size),
				Kernel32.AllocationType.RESERVE | Kernel32.AllocationType.COMMIT,
				Kernel32.MemoryProtection.NOACCESS);
			if (Start == 0)
			{
				throw new InvalidOperationException("VirtualAlloc() returned NULL");
			}
			if (start != 0)
				End = start + size;
			else
				End = Start + size;
			Size = End - Start;
#else
			Start = (ulong)LibC.mmap(ZC.U(start), ZC.U(size), 0, LibC.MapType.MAP_ANONYMOUS, -1, IntPtr.Zero);
			if (Start == 0)
			{
				throw new InvalidOperationException("mmap() returned NULL");
			}
			End = Start + size;
			Size = End - Start;
#endif
		}

		public enum Protection
		{
			R, RW, RX, None
		}

		public Stream GetStream(ulong start, ulong length, bool writer)
		{
			if (start < Start)
				throw new ArgumentOutOfRangeException("start");

			if (start + length > End)
				throw new ArgumentOutOfRangeException("length");

			return new MemoryViewStream(!writer, writer, (long)start, (long)length, this);
		}

		public void Set(ulong start, ulong length, Protection prot)
		{
			if (start < Start)
				throw new ArgumentOutOfRangeException("start");

			if (start + length > End)
				throw new ArgumentOutOfRangeException("length");

			if (length == 0)
				return;

#if !MONO
			Kernel32.MemoryProtection p;
			switch (prot)
			{
				case Protection.None: p = Kernel32.MemoryProtection.NOACCESS; break;
				case Protection.R: p = Kernel32.MemoryProtection.READONLY; break;
				case Protection.RW: p = Kernel32.MemoryProtection.READWRITE; break;
				case Protection.RX: p = Kernel32.MemoryProtection.EXECUTE_READ; break;
				default: throw new ArgumentOutOfRangeException("prot");
			}
			Kernel32.MemoryProtection old;
			if (!Kernel32.VirtualProtect(Z.UU(start), Z.UU(length), p, out old))
				throw new InvalidOperationException("VirtualProtect() returned FALSE!");
#else
			LibC.ProtType p;
			switch (prot)
			{
				case Protection.None: p = 0; break;
				case Protection.R: p = LibC.ProtType.PROT_READ; break;
				case Protection.RW: p = LibC.ProtType.PROT_READ | LibC.ProtType.PROT_WRITE; break;
				case Protection.RX: p = LibC.ProtType.PROT_READ | LibC.ProtType.PROT_EXEC; break;
				default: throw new ArgumentOutOfRangeException("prot");
			}
			ulong end = (ulong)start + (ulong)length;
			ulong newstart = (ulong)start & ~((ulong)Environment.SystemPageSize - 1);
			if (LibC.mprotect((UIntPtr)newstart, (UIntPtr)(end - newstart), p) != 0)
				throw new InvalidOperationException("mprotect() returned -1!");
#endif
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (Start != 0)
			{
#if !MONO
				Kernel32.VirtualFree(Z.UU(Start), UIntPtr.Zero, Kernel32.FreeType.RELEASE);
#else
				LibC.munmap(ZC.U(Start), (UIntPtr)Size);
#endif
				Start = 0;
			}
		}

		~MemoryBlock()
		{
			Dispose(false);
		}

		private class MemoryViewStream : Stream
		{
			public MemoryViewStream(bool readable, bool writable, long ptr, long length, MemoryBlock owner)
			{
				_readable = readable;
				_writable = writable;
				_ptr = ptr;
				_length = length;
				_owner = owner;
				_pos = 0;
			}

			private void EnsureNotDisposed()
			{
				if (_owner.Start == 0)
					throw new ObjectDisposedException("MemoryBlock");
			}

			private MemoryBlock _owner;

			private bool _readable;
			private bool _writable;

			private long _length;
			private long _pos;
			private long _ptr;

			public override bool CanRead { get { return _readable; } }
			public override bool CanSeek { get { return true; } }
			public override bool CanWrite { get { return _writable; } }
			public override void Flush() { }
			public override long Length { get { return _length; } }

			public override long Position
			{ 
				get { return _pos; } set 
				{
					if (value < 0 || value > _length)
						throw new ArgumentOutOfRangeException();
					_pos = value;
				} 
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				if (!_readable)
					throw new InvalidOperationException();
				if (count < 0 || count > buffer.Length)
					throw new ArgumentOutOfRangeException();
				EnsureNotDisposed();
				count = (int)Math.Min(count, _length - _pos);
				Marshal.Copy(Z.SS(_ptr + _pos), buffer, 0, count);
				_pos += count;
				return count;
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				long newpos;
				switch (origin)
				{
					default:
					case SeekOrigin.Begin:
						newpos = 0;
						break;
					case SeekOrigin.Current:
						newpos = _pos + offset;
						break;
					case SeekOrigin.End:
						newpos = _length + offset;
						break;
				}
				Position = newpos;
				return newpos;
			}

			public override void SetLength(long value)
			{
				throw new InvalidOperationException();
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				if (!_writable)
					throw new InvalidOperationException();
				if (count < 0 || count > buffer.Length)
					throw new ArgumentOutOfRangeException();
				EnsureNotDisposed();
				count = (int)Math.Min(count, _length - _pos);
				Marshal.Copy(buffer, 0, Z.SS(_ptr + _pos), count);
				_pos += count;
			}
		}


#if !MONO
		private static class Kernel32
		{
			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern UIntPtr VirtualAlloc(UIntPtr lpAddress, UIntPtr dwSize,
			   AllocationType flAllocationType, MemoryProtection flProtect);

			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern bool VirtualFree(UIntPtr lpAddress, UIntPtr dwSize,
			   FreeType dwFreeType);

			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern bool VirtualProtect(UIntPtr lpAddress, UIntPtr dwSize,
			   MemoryProtection flNewProtect, out MemoryProtection lpflOldProtect);

			public enum FreeType : uint
			{
				DECOMMIT = 0x4000,
				RELEASE = 0x8000
			}

			[Flags]
			public enum AllocationType : uint
			{
				COMMIT = 0x1000,
				RESERVE = 0x2000,
				RESET = 0x80000,
				RESET_UNDO = 0x1000000,
				LARGE_PAGES = 0x20000000,
				PHYSICAL = 0x400000,
				TOP_DOWN = 0x100000,
				WRITE_WATCH = 0x200000
			}

			[Flags]
			public enum MemoryProtection : uint
			{
				EXECUTE = 0x10,
				EXECUTE_READ = 0x20,
				EXECUTE_READWRITE = 0x40,
				EXECUTE_WRITECOPY = 0x80,
				NOACCESS = 0x01,
				READONLY = 0x02,
				READWRITE = 0x04,
				WRITECOPY = 0x08,
				GUARD_Modifierflag = 0x100,
				NOCACHE_Modifierflag = 0x200,
				WRITECOMBINE_Modifierflag = 0x400
			}
		}
#else
		private static class LibC
		{
			[DllImport("libc.so")]
			public static extern UIntPtr mmap(UIntPtr addr, UIntPtr length, ProtType prot, MapType flags, int fd, IntPtr offset);
			[DllImport("libc.so")]
			public static extern int munmap(UIntPtr addr, UIntPtr length);
			[DllImport("libc.so")]
			public static extern int mprotect(UIntPtr addr, UIntPtr length, ProtType prot);

			[Flags]
			public enum ProtType : int
			{
				PROT_EXEC = 1,
				PROT_WRITE = 2,
				PROT_READ = 4
			}

			[Flags]
			public enum MapType : int
			{
				MAP_ANONYMOUS = 2
			}
		}
#endif
	}
}
