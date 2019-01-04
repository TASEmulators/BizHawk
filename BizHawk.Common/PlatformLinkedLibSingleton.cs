using System;
using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	public sealed class PlatformLinkedLibSingleton
	{
		public static readonly bool RunningOnUnix = Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX;

		private static readonly Lazy<PlatformLinkedLibManager> lazy = new Lazy<PlatformLinkedLibManager>(() => RunningOnUnix
			? (PlatformLinkedLibManager) new UnixMonoLinkedLibManager()
			: (PlatformLinkedLibManager) new Win32LinkedLibManager());

		public static PlatformLinkedLibManager LinkedLibManager { get { return lazy.Value; } }

		private PlatformLinkedLibSingleton() {}

		public interface PlatformLinkedLibManager
		{
			IntPtr LoadPlatformSpecific(string dllToLoad);
			IntPtr GetProcAddr(IntPtr hModule, string procName);
			int FreePlatformSpecific(IntPtr hModule);
		}

		public class UnixMonoLinkedLibManager : PlatformLinkedLibManager
		{
			// This class is copied from a tutorial, so don't git blame and then email me expecting insight.
			const int RTLD_NOW = 2;
			[DllImport("libdl.so.2")]
			private static extern IntPtr dlopen(String fileName, int flags);
			[DllImport("libdl.so.2")]
			private static extern IntPtr dlerror();
			[DllImport("libdl.so.2")]
			private static extern IntPtr dlsym(IntPtr handle, String symbol);
			[DllImport("libdl.so.2")]
			private static extern int dlclose(IntPtr handle);
			public IntPtr LoadPlatformSpecific(string dllToLoad)
			{
				return dlopen(dllToLoad, RTLD_NOW);
			}
			public IntPtr GetProcAddr(IntPtr hModule, string procName)
			{
				dlerror();
				var res = dlsym(hModule, procName);
				var errPtr = dlerror();
				if (errPtr != IntPtr.Zero) throw new InvalidOperationException($"error in dlsym: {Marshal.PtrToStringAnsi(errPtr)}");
				return res;
			}
			public int FreePlatformSpecific(IntPtr hModule)
			{
				return dlclose(hModule);
			}
		}

		public class Win32LinkedLibManager : PlatformLinkedLibManager
		{
			[DllImport("kernel32.dll")]
			private static extern UInt32 GetLastError();
			// was annotated `[DllImport("kernel32.dll", BestFitMapping = false, ThrowOnUnmappableChar = true)]` in SevenZip.NativeMethods
			// param dllToLoad was annotated `[MarshalAs(UnmanagedType.LPStr)]` in SevenZip.NativeMethods
			[DllImport("kernel32.dll")]
			private static extern IntPtr LoadLibrary(string dllToLoad);
			// was annotated `[DllImport("kernel32.dll", BestFitMapping = false, ThrowOnUnmappableChar = true)]` in SevenZip.NativeMethods
			// param procName was annotated `[MarshalAs(UnmanagedType.LPStr)]` in SevenZip.NativeMethods
			[DllImport("kernel32.dll")]
			private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
			// was annotated `[return: MarshalAs(UnmanagedType.Bool)]` in SevenZip.NativeMethods
			[DllImport("kernel32.dll")]
			private static extern bool FreeLibrary(IntPtr hModule);
			public IntPtr LoadPlatformSpecific(string dllToLoad)
			{
				var p = LoadLibrary(dllToLoad);
				if (p == IntPtr.Zero) throw new InvalidOperationException($"got null pointer, error code {GetLastError()}");
				return p;
			}
			public IntPtr GetProcAddr(IntPtr hModule, string procName)
			{
				return GetProcAddress(hModule, procName);
			}
			public int FreePlatformSpecific(IntPtr hModule)
			{
				return FreeLibrary(hModule) ? 1 : 0;
			}
		}
	}
}