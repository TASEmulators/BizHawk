using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

//put in a different namespace for EXE so we can have an instance of this type (by linking to this file rather than copying it) built-in to the exe
//so the exe doesnt implicitly depend on the dll
#if EXE_PROJECT
namespace EXE_PROJECT
#else
namespace BizHawk.Common
#endif
{
	public sealed class OSTailoredCode
	{
		/// <remarks>macOS doesn't use PlatformID.MacOSX</remarks>
		public static readonly DistinctOS CurrentOS = Environment.OSVersion.Platform == PlatformID.Unix
			? currentIsMacOS() ? DistinctOS.macOS : DistinctOS.Linux
			: DistinctOS.Windows;

		private static readonly Lazy<ILinkedLibManager> lazy = new Lazy<ILinkedLibManager>(() =>
		{
			switch (CurrentOS)
			{
				case DistinctOS.Linux:
				case DistinctOS.macOS:
					return new UnixMonoLLManager();
				case DistinctOS.Windows:
					return new WindowsLLManager();
				default:
					throw new ArgumentOutOfRangeException();
			}
		});

		public static ILinkedLibManager LinkedLibManager => lazy.Value;

		private static bool currentIsMacOS()
		{
			var proc = new Process {
				StartInfo = new ProcessStartInfo {
					Arguments = "-s",
					CreateNoWindow = true,
					FileName = "uname",
					RedirectStandardOutput = true,
					UseShellExecute = false
				}
			};
			proc.Start();
			if (proc.StandardOutput.EndOfStream) throw new Exception("Can't determine OS (uname wrote nothing to stdout)!");
			return proc.StandardOutput.ReadLine() == "Darwin";
		}

		private OSTailoredCode() {}

		public interface ILinkedLibManager
		{
			IntPtr LoadPlatformSpecific(string dllToLoad);
			IntPtr GetProcAddr(IntPtr hModule, string procName);
			int FreePlatformSpecific(IntPtr hModule);
		}

		/// <remarks>This class is copied from a tutorial, so don't git blame and then email me expecting insight.</remarks>
		private class UnixMonoLLManager : ILinkedLibManager
		{
			private const int RTLD_NOW = 2;
			[DllImport("libdl.so.2")]
			private static extern IntPtr dlopen(string fileName, int flags);
			[DllImport("libdl.so.2")]
			private static extern IntPtr dlerror();
			[DllImport("libdl.so.2")]
			private static extern IntPtr dlsym(IntPtr handle, string symbol);
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

		private class WindowsLLManager : ILinkedLibManager
		{
			[DllImport("kernel32.dll")]
			private static extern uint GetLastError();
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

		public enum DistinctOS : byte
		{
			Linux,
			macOS,
			Windows
		}
	}
}