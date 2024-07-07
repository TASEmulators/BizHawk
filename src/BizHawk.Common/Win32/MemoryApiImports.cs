using System.Runtime.InteropServices;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable UnusedMember.Global

namespace BizHawk.Common
{
	public static class MemoryApiImports
	{
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

		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		public static extern UIntPtr VirtualAlloc(UIntPtr lpAddress, UIntPtr dwSize,
			AllocationType flAllocationType, MemoryProtection flProtect);

		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool VirtualProtect(
			UIntPtr lpAddress,
			UIntPtr dwSize,
			MemoryProtection flNewProtect,
			out MemoryProtection lpflOldProtect);

		[Flags]
		public enum FreeType : uint
		{
			Decommit = 0x4000,
			Release = 0x8000,
		}

		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool VirtualFree(UIntPtr lpAddress, UIntPtr dwSize, FreeType dwFreeType);
	}
}
