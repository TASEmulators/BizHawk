using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	public static class MmanImports
	{
		[Flags]
		public enum MemoryProtection : int
		{
			None = 0x0,
			Read = 0x1,
			Write = 0x2,
			Execute = 0x4
		}

		[DllImport("libc")]
		public static extern IntPtr mmap(IntPtr addr, UIntPtr length, MemoryProtection prot, int flags, int fd, IntPtr offset);

		[DllImport("libc")]
		public static extern int mprotect(IntPtr addr, UIntPtr len, MemoryProtection prot);

		[DllImport("libc")]
		public static extern int munmap(IntPtr addr, UIntPtr length);
	}
}
