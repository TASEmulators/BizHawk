using System;
using System.Runtime.InteropServices;
using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Waterbox
{
	partial class Syscalls
	{
		internal const long MAP_FAILED = -1;

		internal const ulong MAP_SHARED = 0x01;
		internal const ulong MAP_PRIVATE = 0x02;
		internal const ulong MAP_SHARED_VALIDATE = 0x03;
		internal const ulong MAP_TYPE = 0x0f;
		internal const ulong MAP_FIXED = 0x10;
		internal const ulong MAP_ANON = 0x20;
		internal const ulong MAP_ANONYMOUS = MAP_ANON;
		internal const ulong MAP_NORESERVE = 0x4000;
		internal const ulong MAP_GROWSDOWN = 0x0100;
		internal const ulong MAP_DENYWRITE = 0x0800;
		internal const ulong MAP_EXECUTABLE = 0x1000;
		internal const ulong MAP_LOCKED = 0x2000;
		internal const ulong MAP_POPULATE = 0x8000;
		internal const ulong MAP_NONBLOCK = 0x10000;
		internal const ulong MAP_STACK = 0x20000;
		internal const ulong MAP_HUGETLB = 0x40000;
		internal const ulong MAP_SYNC = 0x80000;
		internal const ulong MAP_FIXED_NOREPLACE = 0x100000;
		internal const ulong MAP_FILE = 0;

		internal const ulong MAP_HUGE_SHIFT = 26;
		internal const ulong MAP_HUGE_MASK = 0x3f;
		internal const ulong MAP_HUGE_64KB = (16 << 26);
		internal const ulong MAP_HUGE_512KB = (19 << 26);
		internal const ulong MAP_HUGE_1MB = (20 << 26);
		internal const ulong MAP_HUGE_2MB = (21 << 26);
		internal const ulong MAP_HUGE_8MB = (23 << 26);
		internal const ulong MAP_HUGE_16MB = (24 << 26);
		internal const ulong MAP_HUGE_32MB = (25 << 26);
		internal const ulong MAP_HUGE_256MB = (28 << 26);
		internal const ulong MAP_HUGE_512MB = (29 << 26);
		internal const ulong MAP_HUGE_1GB = (30 << 26);
		internal const ulong MAP_HUGE_2GB = (31 << 26);
		internal const ulong MAP_HUGE_16GB = (34U << 26);

		internal const ulong PROT_NONE = 0;
		internal const ulong PROT_READ = 1;
		internal const ulong PROT_WRITE = 2;
		internal const ulong PROT_EXEC = 4;
		internal const ulong PROT_GROWSDOWN = 0x01000000;
		internal const ulong PROT_GROWSUP = 0x02000000;

		internal const ulong MS_ASYNC = 1;
		internal const ulong MS_INVALIDATE = 2;
		internal const ulong MS_SYNC = 4;

		internal const ulong MCL_CURRENT = 1;
		internal const ulong MCL_FUTURE = 2;
		internal const ulong MCL_ONFAULT = 4;

		internal const ulong POSIX_MADV_NORMAL = 0;
		internal const ulong POSIX_MADV_RANDOM = 1;
		internal const ulong POSIX_MADV_SEQUENTIAL = 2;
		internal const ulong POSIX_MADV_WILLNEED = 3;
		internal const ulong POSIX_MADV_DONTNEED = 4;

		internal const ulong MADV_NORMAL = 0;
		internal const ulong MADV_RANDOM = 1;
		internal const ulong MADV_SEQUENTIAL = 2;
		internal const ulong MADV_WILLNEED = 3;
		internal const ulong MADV_DONTNEED = 4;
		internal const ulong MADV_FREE = 8;
		internal const ulong MADV_REMOVE = 9;
		internal const ulong MADV_DONTFORK = 10;
		internal const ulong MADV_DOFORK = 11;
		internal const ulong MADV_MERGEABLE = 12;
		internal const ulong MADV_UNMERGEABLE = 13;
		internal const ulong MADV_HUGEPAGE = 14;
		internal const ulong MADV_NOHUGEPAGE = 15;
		internal const ulong MADV_DONTDUMP = 16;
		internal const ulong MADV_DODUMP = 17;
		internal const ulong MADV_WIPEONFORK = 18;
		internal const ulong MADV_KEEPONFORK = 19;
		internal const ulong MADV_COLD = 20;
		internal const ulong MADV_PAGEOUT = 21;
		internal const ulong MADV_HWPOISON = 100;
		internal const ulong MADV_SOFT_OFFLINE = 101;

		internal const ulong MREMAP_MAYMOVE = 1;
		internal const ulong MREMAP_FIXED = 2;

		internal const ulong MLOCK_ONFAULT = 0x01;

		internal const ulong MFD_CLOEXEC = 0x0001U;
		internal const ulong MFD_ALLOW_SEALING = 0x0002U;
		internal const ulong MFD_HUGETLB = 0x0004U;

		[BizExport(CallingConvention.Cdecl, EntryPoint = "__wsyscalltab[9]")]
		public IntPtr MMap(IntPtr address, UIntPtr size, ulong prot, ulong flags, int fd, IntPtr offs)
		{
			if (address != IntPtr.Zero)
			{
				// waterbox cores generally don't know about hardcoded addresses
				// we could support this, so long as the address is in our heap's range
				return Z.SS(MAP_FAILED);
			}
			MemoryBlock.Protection mprot;
			switch (prot)
			{
				case PROT_NONE:
					mprot = MemoryBlock.Protection.None;
					break;
				default:
				case PROT_WRITE | PROT_EXEC: // W^X
				case PROT_READ | PROT_WRITE | PROT_EXEC: // W^X
				case PROT_EXEC: // exec only????
				case PROT_WRITE:
					return Z.SS(MAP_FAILED); // write only????
				case PROT_READ | PROT_WRITE:
					mprot = MemoryBlock.Protection.RW;
					break;
				case PROT_READ:
					mprot = MemoryBlock.Protection.R;
					break;
				case PROT_READ | PROT_EXEC:
					mprot = MemoryBlock.Protection.RX;
					break;
			}
			if ((flags & MAP_ANONYMOUS) == 0)
			{
				// anonymous + private is easy
				// anonymous by itself is hard
				// nothing needs either right now
				return Z.SS(MAP_FAILED);
			}
			if ((flags & 0xf00) != 0)
			{
				// various unsupported flags
				return Z.SS(MAP_FAILED);
			}

			var ret = _parent._mmapheap.Map((ulong)size, mprot);
			return ret == 0 ? Z.SS(MAP_FAILED) : Z.US(ret);
		}
		[BizExport(CallingConvention.Cdecl, EntryPoint = "__wsyscalltab[25]")]
		public IntPtr MRemap(UIntPtr oldAddress, UIntPtr oldSize,
			UIntPtr newSize, ulong flags)
		{
			if ((flags & MREMAP_FIXED) != 0)
			{
				// just like mmap.
				// waterbox cores generally don't know about hardcoded addresses
				// we could support this, so long as the address is in our heap's range	
				return Z.SS(MAP_FAILED);
			}
			var ret = _parent._mmapheap.Remap((ulong)oldAddress, (ulong)oldSize, (ulong)newSize,
				(flags & MREMAP_MAYMOVE) != 0);
			return ret == 0 ? Z.SS(MAP_FAILED) : Z.US(ret);
		}
		[BizExport(CallingConvention.Cdecl, EntryPoint = "__wsyscalltab[11]")]
		public long MUnmap(UIntPtr address, UIntPtr size)
		{
			return _parent._mmapheap.Unmap((ulong)address, (ulong)size) ? 0 : MAP_FAILED;
		}

		[BizExport(CallingConvention.Cdecl, EntryPoint = "__wsyscalltab[10]")]
		public long MProtect(UIntPtr address, UIntPtr size, ulong prot)
		{
			MemoryBlock.Protection mprot;
			switch (prot)
			{
				case PROT_NONE:
					mprot = MemoryBlock.Protection.None;
					break;
				default:
				case PROT_WRITE | PROT_EXEC: // W^X
				case PROT_READ | PROT_WRITE | PROT_EXEC: // W^X
				case PROT_EXEC: // exec only????
				case PROT_WRITE:
					return MAP_FAILED; // write only????
				case PROT_READ | PROT_WRITE:
					mprot = MemoryBlock.Protection.RW;
					break;
				case PROT_READ:
					mprot = MemoryBlock.Protection.R;
					break;
				case PROT_READ | PROT_EXEC:
					mprot = MemoryBlock.Protection.RX;
					break;
			}
			return _parent._mmapheap.Protect((ulong)address, (ulong)size, mprot) ? 0 : MAP_FAILED;
		}
	}
}
