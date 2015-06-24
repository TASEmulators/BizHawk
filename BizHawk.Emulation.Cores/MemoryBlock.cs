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
				Kernel32.VirtualFree(Start, UIntPtr.Zero, Kernel32.FreeType.RELEASE);
				Start = UIntPtr.Zero;
			}
		}

		~MemoryBlock()
		{
			Dispose(false);
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
			/*
			[Flags]
			public enum Protection
			{
				PAGE_NOACCESS = 0x01,
				PAGE_READONLY = 0x02,
				PAGE_READWRITE = 0x04,
				PAGE_WRITECOPY = 0x08,
				PAGE_EXECUTE = 0x10,
				PAGE_EXECUTE_READ = 0x20,
				PAGE_EXECUTE_READWRITE = 0x40,
				PAGE_EXECUTE_WRITECOPY = 0x80,
				PAGE_GUARD = 0x100,
				PAGE_NOCACHE = 0x200,
				PAGE_WRITECOMBINE = 0x400
			}*/

		}
	}
}
