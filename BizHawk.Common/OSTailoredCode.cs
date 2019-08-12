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

		private static bool currentIsMacOS() => SimpleSubshell("uname", "-s", "Can't determine OS") == "Darwin";

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

		/// <param name="cmd">POSIX <c>$0</c></param>
		/// <param name="args">POSIX <c>$*</c> (space-delimited)</param>
		/// <param name="checkStdout">stdout is discarded if false</param>
		/// <param name="checkStderr">stderr is discarded if false</param>
		/// <remarks>OS is implicit and needs to be checked at callsite, returned <see cref="Process"/> has not been started</remarks>
		public static Process ConstructSubshell(string cmd, string args, bool checkStdout = true, bool checkStderr = false) =>
			new Process {
				StartInfo = new ProcessStartInfo {
					Arguments = args,
					CreateNoWindow = true,
					FileName = cmd,
					RedirectStandardError = checkStderr,
					RedirectStandardOutput = checkStdout,
					UseShellExecute = false
				}
			};

		/// <param name="cmd">POSIX <c>$0</c></param>
		/// <param name="args">POSIX <c>$*</c> (space-delimited)</param>
		/// <param name="noOutputMsg">used in exception</param>
		/// <returns>first line of stdout</returns>
		/// <exception cref="Exception">thrown if stdout is empty</exception>
		/// <remarks>OS is implicit and needs to be checked at callsite</remarks>
		public static string SimpleSubshell(string cmd, string args, string noOutputMsg)
		{
			using (var proc = ConstructSubshell(cmd, args))
			{
				proc.Start();
				var stdout = proc.StandardOutput;
				if (stdout.EndOfStream) throw new Exception($"{noOutputMsg} ({cmd} wrote nothing to stdout)");
				return stdout.ReadLine();
			}
		}
	}
}