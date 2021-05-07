using System;
using System.Runtime.InteropServices;
using static BizHawk.BizInvoke.MemoryBlock;

namespace BizHawk.BizInvoke
{
	internal sealed unsafe class MemoryBlockWindowsPal : IMemoryBlockPal
	{
		public ulong Start { get; }
		private readonly ulong _size;
		private bool _disposed;

		public MemoryBlockWindowsPal(ulong size)
		{
			var ptr = (ulong)Kernel32.VirtualAlloc(
				UIntPtr.Zero, Z.UU(size), Kernel32.AllocationType.MEM_RESERVE | Kernel32.AllocationType.MEM_COMMIT, Kernel32.MemoryProtection.NOACCESS);
			if (ptr == 0)
				throw new InvalidOperationException($"{nameof(Kernel32.VirtualAlloc)}() returned NULL");
			Start = ptr;
			_size = size;
		}

		public void Protect(ulong start, ulong size, Protection prot)
		{
			if (!Kernel32.VirtualProtect(Z.UU(start), Z.UU(size), GetKernelMemoryProtectionValue(prot), out var old))
				throw new InvalidOperationException($"{nameof(Kernel32.VirtualProtect)}() returned FALSE!");
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
				default: throw new ArgumentOutOfRangeException(nameof(prot));
			}
			return p;
		}

		public void Dispose()
		{
			if (_disposed)
				return;
			Kernel32.VirtualFree(Z.UU(Start), UIntPtr.Zero, Kernel32.FreeType.Release);
			_disposed = true;
			GC.SuppressFinalize(this);
		}

		~MemoryBlockWindowsPal()
		{
			Dispose();
		}

		private static class Kernel32
		{
			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern UIntPtr VirtualAlloc(UIntPtr lpAddress, UIntPtr dwSize,
				AllocationType flAllocationType, MemoryProtection flProtect);

			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern bool VirtualProtect(
				UIntPtr lpAddress,
				UIntPtr dwSize,
				MemoryProtection flNewProtect,
				out MemoryProtection lpflOldProtect);

			[Flags]
			public enum AllocationType : uint
			{
				MEM_COMMIT = 0x00001000,
				MEM_RESERVE = 0x00002000,
				MEM_RESET = 0x00080000,
				MEM_RESET_UNDO = 0x1000000,
				MEM_LARGE_PAGES = 0x20000000,
				MEM_PHYSICAL = 0x00400000,
				MEM_TOP_DOWN = 0x00100000,
				MEM_WRITE_WATCH = 0x00200000
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
			public static extern IntPtr MapViewOfFileEx(
				IntPtr hFileMappingObject,
				FileMapAccessType dwDesiredAccess,
				uint dwFileOffsetHigh,
				uint dwFileOffsetLow,
				UIntPtr dwNumberOfBytesToMap,
				IntPtr lpBaseAddress);

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

			[StructLayout(LayoutKind.Sequential)]
			public struct MEMORY_BASIC_INFORMATION
			{
				public IntPtr BaseAddress;
				public IntPtr AllocationBase;
				public MemoryProtection AllocationProtect;
				public UIntPtr RegionSize;
				public StateEnum State;
				public MemoryProtection Protect;
				public TypeEnum Type;
			}
			public enum StateEnum : uint
			{
				MEM_COMMIT = 0x1000,
				MEM_FREE = 0x10000,
				MEM_RESERVE = 0x2000
			}

			public enum TypeEnum : uint
			{
				MEM_IMAGE = 0x1000000,
				MEM_MAPPED = 0x40000,
				MEM_PRIVATE = 0x20000
			}

			[DllImport("kernel32.dll")]
			public static extern UIntPtr VirtualQuery(UIntPtr lpAddress, MEMORY_BASIC_INFORMATION* lpBuffer, UIntPtr dwLength);

			[Flags]
			public enum FreeType
			{
				Decommit = 0x4000,
				Release = 0x8000,
			}

			[DllImport("kernel32.dll")]
			public static extern bool VirtualFree(UIntPtr lpAddress, UIntPtr dwSize, FreeType dwFreeType);
		}
	}
}
