using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	/// <summary>
	/// Imports of functions in dlfcn.h
	/// For most POSIX systems, these come from libc
	/// Linux is a partial exception, as they weren't in libc until glibc 2.34
	/// (For reference, Debian 10, the current Debian oldoldstable, is on glibc 2.28)
	/// </summary>
	public static class PosixDlfcnImports
	{
		public const int RTLD_NOW = 2;

		[DllImport("libc")]
		public static extern IntPtr dlopen(string fileName, int flags);

		[DllImport("libc")]
		public static extern int dlclose(IntPtr handle);

		[DllImport("libc")]
		public static extern IntPtr dlsym(IntPtr handle, string symbol);

		[DllImport("libc")]
		public static extern IntPtr dlerror();
	}
}
