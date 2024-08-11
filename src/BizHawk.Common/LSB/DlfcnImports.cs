using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	/// <summary>
	/// Imports of functions in dlfcn.h
	/// For Linux, these come from libdl historically
	/// (Although since glibc 2.34 these are just in libc, with libdl just calling into libc)
	/// </summary>
	public static class LinuxDlfcnImports
	{
		public const int RTLD_NOW = 2;

		[DllImport("libdl.so.2")]
		public static extern IntPtr dlopen(string fileName, int flags);

		[DllImport("libdl.so.2")]
		public static extern int dlclose(IntPtr handle);

		[DllImport("libdl.so.2")]
		public static extern IntPtr dlsym(IntPtr handle, string symbol);

		[DllImport("libdl.so.2")]
		public static extern IntPtr dlerror();
	}
}
