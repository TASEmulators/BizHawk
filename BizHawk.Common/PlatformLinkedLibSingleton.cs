using System;
using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	public sealed class PlatformLinkedLibSingleton
	{
		private static readonly Lazy<PlatformLinkedLibManager> lazy = new Lazy<PlatformLinkedLibManager>(() =>
			(Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
				? (PlatformLinkedLibManager) new UnixMonoLinkedLibManager()
				: (PlatformLinkedLibManager) new Win32LinkedLibManager());

		public static PlatformLinkedLibManager LinkedLibManager { get { return lazy.Value; } }

		private PlatformLinkedLibSingleton() {}

		public interface PlatformLinkedLibManager
		{
			IntPtr LoadPlatformSpecific(string dllToLoad);
			IntPtr GetProcAddr(IntPtr hModule, string procName);
			void FreePlatformSpecific(IntPtr hModule);
		}

		public class UnixMonoLinkedLibManager : PlatformLinkedLibManager
		{
			// This class is copied from a tutorial, so don't git blame and then email me expecting insight.
			const int RTLD_NOW = 2;
			[DllImport("libdl.so")]
			private static extern IntPtr dlopen(String fileName, int flags);
			[DllImport("libdl.so")]
			private static extern IntPtr dlerror();
			[DllImport("libdl.so")]
			private static extern IntPtr dlsym(IntPtr handle, String symbol);
			[DllImport("libdl.so")]
			private static extern int dlclose(IntPtr handle);
			public IntPtr LoadPlatformSpecific(string dllToLoad)
			{
				return dlopen(dllToLoad + ".so", RTLD_NOW);
			}
			public IntPtr GetProcAddr(IntPtr hModule, string procName)
			{
				dlerror();
				var res = dlsym(hModule, procName);
				var errPtr = dlerror();
				if (errPtr != IntPtr.Zero) throw new Exception("dlsym: " + Marshal.PtrToStringAnsi(errPtr));
				return res;
			}
			public void FreePlatformSpecific(IntPtr hModule)
			{
				dlclose(hModule);
			}
		}

		public class Win32LinkedLibManager : PlatformLinkedLibManager
		{
			[DllImport("kernel32.dll")]
			private static extern IntPtr LoadLibrary(string dllToLoad);
			[DllImport("kernel32.dll")]
			private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
			[DllImport("kernel32.dll")]
			private static extern void FreeLibrary(IntPtr hModule);
			public IntPtr LoadPlatformSpecific(string dllToLoad)
			{
				return LoadLibrary(dllToLoad);
			}
			public IntPtr GetProcAddr(IntPtr hModule, string procName)
			{
				return GetProcAddress(hModule, procName);
			}
			public void FreePlatformSpecific(IntPtr hModule)
			{
				FreeLibrary(hModule);
			}
		}
	}
}