using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores
{
	public sealed class MemoryBlock : IDisposable
	{
		public UIntPtr Start { get; private set; }
		public long Size { get; private set; }
		public UIntPtr End { get; private set; }
		public int PageSize { get { return Environment.SystemPageSize; } }

		public MemoryBlock(long size)
			: this(UIntPtr.Zero, size)
		{
		}

		public MemoryBlock(UIntPtr start, long size)
		{
#if !MONO
			Start = Kernel32.VirtualAlloc(start, checked((UIntPtr)size),
				Kernel32.AllocationType.RESERVE | Kernel32.AllocationType.COMMIT,
				Kernel32.MemoryProtection.NOACCESS);
			if (Start == UIntPtr.Zero)
			{
				throw new InvalidOperationException("VirtualAlloc() returned NULL");
			}
			if (start != UIntPtr.Zero)
				End = (UIntPtr)((long)start + size);
			else
				End = (UIntPtr)((long)Start + size);
			Size = (long)End - (long)Start;
#else
			Start = LibC.mmap(start, checked((UIntPtr)size), 0, LibC.MapType.MAP_ANONYMOUS, -1, IntPtr.Zero);
			if (Start == UIntPtr.Zero)
			{
				throw new InvalidOperationException("mmap() returned NULL");
			}
			End = (UIntPtr)((long)Start + size);
			Size = (long)End - (long)Start;
#endif
		}

		public enum Protection
		{
			R, RW, RX, None
		}

		public void Set(UIntPtr start, long length, Protection prot)
		{
			if ((ulong)start < (ulong)Start)
				throw new ArgumentOutOfRangeException("start");

			if ((ulong)start + (ulong)length > (ulong)End)
				throw new ArgumentOutOfRangeException("length");

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
			if (!Kernel32.VirtualProtect(start, (UIntPtr)length, p, out old))
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
			if (Start != UIntPtr.Zero)
			{
#if !MONO
				Kernel32.VirtualFree(Start, UIntPtr.Zero, Kernel32.FreeType.RELEASE);
#else
				LibC.munmap(Start, (UIntPtr)Size);
#endif
				Start = UIntPtr.Zero;
			}
		}

		~MemoryBlock()
		{
			Dispose(false);
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
