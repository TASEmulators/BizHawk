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
		/// system page size
		/// </summary>
		public static int PageSize { get; private set;} 

		/// <summary>
		/// bitshift corresponding to PageSize
		/// </summary>
		private static readonly int PageShift;
		/// <summary>
		/// bitmask corresponding to PageSize
		/// </summary>
		private static readonly ulong PageMask;

		static MemoryBlock()
		{
			int p = PageSize = Environment.SystemPageSize;
			while (p != 1)
			{
				p >>= 1;
				PageShift++;
			}
			PageMask = (ulong)(PageSize - 1);
		}
		
		/// <summary>
		/// true if addr is aligned
		/// </summary>
		private static bool Aligned(ulong addr)
		{
			return (addr & PageMask) == 0;
		}

		/// <summary>
		/// align address down to previous page boundary
		/// </summary>
		private static ulong AlignDown(ulong addr)
		{
			return addr & ~PageMask;
		}

		/// <summary>
		/// align address up to next page boundary
		/// </summary>
		private static ulong AlignUp(ulong addr)
		{
			return ((addr - 1) | PageMask) + 1;
		}

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

		/// <summary>
		/// handle returned by CreateFileMapping
		/// </summary>
		private IntPtr _handle;

		/// <summary>
		/// true if this is currently swapped in
		/// </summary>
		public bool Active { get; private set; }

		/// <summary>
		/// stores last set memory protection value for each page
		/// </summary>
		private readonly Protection[] _pageData;

		/// <summary>
		/// get a page index within the block
		/// </summary>
		private int GetPage(ulong addr)
		{
			if (addr < Start || addr >= End)
				throw new ArgumentOutOfRangeException();

			return (int)((addr - Start) >> PageShift);
		}

		/// <summary>
		/// get a start address for a page index within the block
		/// </summary>
		private ulong GetStartAddr(int page)
		{
			return ((ulong)page << PageShift) + Start;
		}

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
			if (!Aligned(start))
				throw new ArgumentOutOfRangeException();
			if (size == 0)
				throw new ArgumentOutOfRangeException();
			size = AlignUp(size);

			_handle = Kernel32.CreateFileMapping(Kernel32.INVALID_HANDLE_VALUE, IntPtr.Zero,
				Kernel32.FileMapProtection.PageExecuteReadWrite | Kernel32.FileMapProtection.SectionCommit, (uint)(size >> 32), (uint)size, null);

			if (_handle == IntPtr.Zero)
				throw new InvalidOperationException("CreateFileMapping() returned NULL");
			Start = start;
			End = start + size;
			Size = size;
			_pageData = new Protection[GetPage(End - 1) + 1];
		}

		/// <summary>
		/// activate the memory block, swapping it in at the specified address
		/// </summary>
		public void Activate()
		{
			if (Active)
				throw new InvalidOperationException("Already active");
			if (Kernel32.MapViewOfFileEx(_handle, Kernel32.FileMapAccessType.Read | Kernel32.FileMapAccessType.Write | Kernel32.FileMapAccessType.Execute,
				0, 0, Z.UU(Size), Z.US(Start)) != Z.US(Start))
			{
				throw new InvalidOperationException("MapViewOfFileEx() returned NULL");
			}
			ProtectAll();
			Active = true;
		}

		/// <summary>
		/// deactivate the memory block, removing it from RAM but leaving it immediately available to swap back in
		/// </summary>
		public void Deactivate()
		{
			if (!Active)
				throw new InvalidOperationException("Not active");
			if (!Kernel32.UnmapViewOfFile(Z.US(Start)))
				throw new InvalidOperationException("UnmapViewOfFile() returned NULL");
			Active = false;
		}

		/// <summary>
		/// Memory protection constant
		/// </summary>
		public enum Protection : byte
		{
			None, R, RW, RX
		}

		/// <summary>
		/// Get a stream that can be used to read or write from part of the block.  Does not check for or change Protect()!
		/// </summary>
		public Stream GetStream(ulong start, ulong length, bool writer)
		{
			if (start < Start)
				throw new ArgumentOutOfRangeException("start");

			if (start + length > End)
				throw new ArgumentOutOfRangeException("length");

			return new MemoryViewStream(!writer, writer, (long)start, (long)length, this);
		}

		private static Kernel32.MemoryProtection GetKernelMemoryProtectionValue(Protection prot)
		{
			Kernel32.MemoryProtection p;
			switch (prot)
			{
				case Protection.None: p = Kernel32.MemoryProtection.NOACCESS; break;
				case Protection.R: p = Kernel32.MemoryProtection.READONLY; break;
				case Protection.RW: p = Kernel32.MemoryProtection.READWRITE; break;
				case Protection.RX: p = Kernel32.MemoryProtection.EXECUTE_READ; break;
				default: throw new ArgumentOutOfRangeException("prot");
			}
			return p;
		}

		/// <summary>
		/// restore all recorded protections
		/// </summary>
		private void ProtectAll()
		{
			int ps = 0;
			for (int i = 0; i < _pageData.Length; i++)
			{
				if (i == _pageData.Length - 1 || _pageData[i] != _pageData[i + 1])
				{
					var p = GetKernelMemoryProtectionValue(_pageData[i]);
					ulong zstart = GetStartAddr(ps);
					ulong zend = GetStartAddr(i + 1);
					Kernel32.MemoryProtection old;
					if (!Kernel32.VirtualProtect(Z.UU(zstart), Z.UU(zend - zstart), p, out old))
						throw new InvalidOperationException("VirtualProtect() returned FALSE!");
					ps = i + 1;
				}
			}
		}

		/// <summary>
		/// set r/w/x protection on a portion of memory.  rounded to encompassing pages
		/// </summary>
		public void Protect(ulong start, ulong length, Protection prot)
		{
			if (length == 0)
				return;
			int pstart = GetPage(start);
			int pend = GetPage(start + length - 1);

			var p = GetKernelMemoryProtectionValue(prot);
			for (int i = pstart; i <= pend; i++)
				_pageData[i] = prot; // also store the value for later use

			if (Active) // it's legal to Protect() if we're not active; the information is just saved for the next activation
			{
				// TODO: if using another OS's memory protection calls, they must give the same non-aligned behavior
				// as VirtualProtect, or this must be changed
				Kernel32.MemoryProtection old;
				if (!Kernel32.VirtualProtect(Z.UU(start), Z.UU(length), p, out old))
					throw new InvalidOperationException("VirtualProtect() returned FALSE!");
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (_handle != IntPtr.Zero)
			{
				if (Active)
					Deactivate();
				Kernel32.CloseHandle(_handle);
				_handle = IntPtr.Zero;
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

			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern IntPtr CreateFileMapping(
				IntPtr hFile,
				IntPtr lpFileMappingAttributes,
				FileMapProtection flProtect,
				uint dwMaximumSizeHigh,
				uint dwMaximumSizeLow,
				string lpName);

			[Flags]
			public enum FileMapProtection : uint
			{
				PageReadonly = 0x02,
				PageReadWrite = 0x04,
				PageWriteCopy = 0x08,
				PageExecuteRead = 0x20,
				PageExecuteReadWrite = 0x40,
				SectionCommit = 0x8000000,
				SectionImage = 0x1000000,
				SectionNoCache = 0x10000000,
				SectionReserve = 0x4000000,
			}

			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern bool CloseHandle(IntPtr hObject);

			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

			[DllImport("kernel32.dll")]
			public static extern IntPtr MapViewOfFileEx(IntPtr hFileMappingObject,
			   FileMapAccessType dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow,
			   UIntPtr dwNumberOfBytesToMap, IntPtr lpBaseAddress);

			[Flags]
			public enum FileMapAccessType : uint
			{
				Copy = 0x01,
				Write = 0x02,
				Read = 0x04,
				AllAccess = 0x08,
				Execute = 0x20,
			}

			public static readonly IntPtr INVALID_HANDLE_VALUE = Z.US(0xffffffffffffffff);
		}
	}
}
