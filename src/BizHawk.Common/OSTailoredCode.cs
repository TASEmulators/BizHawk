#nullable disable

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

#if EXE_PROJECT
namespace EXE_PROJECT // Use a different namespace so the executable can still use this class' members without an implicit dependency on the BizHawk.Common library, and without resorting to code duplication.
#else
namespace BizHawk.Common
#endif
{
	public static class OSTailoredCode
	{
		/// <remarks>macOS doesn't use <see cref="PlatformID.MacOSX">PlatformID.MacOSX</see></remarks>
		public static readonly DistinctOS CurrentOS = Environment.OSVersion.Platform == PlatformID.Unix
			? SimpleSubshell("uname", "-s", "Can't determine OS") == "Darwin" ? DistinctOS.macOS : DistinctOS.Linux
			: DistinctOS.Windows;

		public static readonly bool IsUnixHost = CurrentOS != DistinctOS.Windows;

		private static readonly Lazy<ILinkedLibManager> _LinkedLibManager = new Lazy<ILinkedLibManager>(() => CurrentOS switch
		{
			DistinctOS.Linux => (ILinkedLibManager) new UnixMonoLLManager(),
			DistinctOS.macOS => new UnixMonoLLManager(),
			DistinctOS.Windows => new WindowsLLManager(),
			_ => throw new ArgumentOutOfRangeException()
		});

		public static ILinkedLibManager LinkedLibManager => _LinkedLibManager.Value;

		/// <remarks>this interface's inheritors hide OS-specific implementation details</remarks>
		public interface ILinkedLibManager
		{
			int FreeByPtr(IntPtr hModule);

			IntPtr? GetProcAddrOrNull(IntPtr hModule, string procName);

			/// <exception cref="InvalidOperationException">could not find symbol</exception>
			IntPtr GetProcAddrOrThrow(IntPtr hModule, string procName);

			IntPtr? LoadOrNull(string dllToLoad);

			/// <exception cref="InvalidOperationException">could not find library</exception>
			IntPtr LoadOrThrow(string dllToLoad);
		}

		private class UnixMonoLLManager : ILinkedLibManager
		{
			[DllImport("libdl.so.2")]
			private static extern int dlclose(IntPtr handle);

			[DllImport("libdl.so.2")]
			private static extern IntPtr dlerror();

			[DllImport("libdl.so.2")]
			private static extern IntPtr dlopen(string fileName, int flags);

			[DllImport("libdl.so.2")]
			private static extern IntPtr dlsym(IntPtr handle, string symbol);

			public int FreeByPtr(IntPtr hModule) => dlclose(hModule);

			public IntPtr? GetProcAddrOrNull(IntPtr hModule, string procName)
			{
				var p = dlsym(hModule, procName);
				return p == IntPtr.Zero ? (IntPtr?) null : p;
			}

			public IntPtr GetProcAddrOrThrow(IntPtr hModule, string procName)
			{
				_ = dlerror(); // the Internet said to do this
				var p = GetProcAddrOrNull(hModule, procName);
				if (p != null) return p.Value;
				var errCharPtr = dlerror();
				throw new InvalidOperationException($"error in {nameof(dlsym)}{(errCharPtr == IntPtr.Zero ? string.Empty : $": {Marshal.PtrToStringAnsi(errCharPtr)}")}");
			}

			public IntPtr? LoadOrNull(string dllToLoad)
			{
				const int RTLD_NOW = 2;
				var p = dlopen(dllToLoad, RTLD_NOW);
				return p == IntPtr.Zero ? (IntPtr?) null : p;
			}

			public IntPtr LoadOrThrow(string dllToLoad) => LoadOrNull(dllToLoad) ?? throw new InvalidOperationException($"got null pointer from {nameof(dlopen)}, error: {Marshal.PtrToStringAnsi(dlerror())}");
		}

		private class WindowsLLManager : ILinkedLibManager
		{
			// comments reference extern functions removed from SevenZip.NativeMethods

			[DllImport("kernel32.dll")]
			private static extern bool FreeLibrary(IntPtr hModule); // return type was annotated MarshalAs(UnmanagedType.Bool)

			[DllImport("kernel32.dll")]
			private static extern uint GetLastError();

			[DllImport("kernel32.dll", SetLastError = true)] // had BestFitMapping = false, ThrowOnUnmappableChar = true
			private static extern IntPtr GetProcAddress(IntPtr hModule, string procName); // param procName was annotated `[MarshalAs(UnmanagedType.LPStr)]`

			[DllImport("kernel32.dll", SetLastError = true)] // had BestFitMapping = false, ThrowOnUnmappableChar = true
			private static extern IntPtr LoadLibrary(string dllToLoad); // param dllToLoad was annotated `[MarshalAs(UnmanagedType.LPStr)]`

			public int FreeByPtr(IntPtr hModule) => FreeLibrary(hModule) ? 0 : 1;

			public IntPtr? GetProcAddrOrNull(IntPtr hModule, string procName)
			{
				var p = GetProcAddress(hModule, procName);
				return p == IntPtr.Zero ? (IntPtr?) null : p;
			}

			public IntPtr GetProcAddrOrThrow(IntPtr hModule, string procName) => GetProcAddrOrNull(hModule, procName) ?? throw new InvalidOperationException($"got null pointer from {nameof(GetProcAddress)}, error code: {GetLastError()}");

			public IntPtr? LoadOrNull(string dllToLoad)
			{
				var p = LoadLibrary(dllToLoad);
				return p == IntPtr.Zero ? (IntPtr?) null : p;
			}

			public IntPtr LoadOrThrow(string dllToLoad) => LoadOrNull(dllToLoad) ?? throw new InvalidOperationException($"got null pointer from {nameof(LoadLibrary)}, error code: {GetLastError()}");
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
		/// <remarks>OS is implicit and needs to be checked at callsite. Returned <see cref="Process"/> has not been started.</remarks>
		public static Process ConstructSubshell(string cmd, string args, bool checkStdout = true, bool checkStderr = false) =>
			new Process {
				StartInfo = new ProcessStartInfo {
					Arguments = args,
					CreateNoWindow = true,
					FileName = cmd,
					RedirectStandardError = checkStderr,
					RedirectStandardInput = true,
					RedirectStandardOutput = checkStdout,
					UseShellExecute = false
				}
			};

		/// <param name="cmd">POSIX <c>$0</c></param>
		/// <param name="args">POSIX <c>$*</c> (space-delimited)</param>
		/// <param name="noOutputMsg">used in exception</param>
		/// <returns>first line of stdout</returns>
		/// <exception cref="Exception">stdout is empty</exception>
		/// <remarks>OS is implicit and needs to be checked at callsite</remarks>
		public static string SimpleSubshell(string cmd, string args, string noOutputMsg)
		{
			using var proc = ConstructSubshell(cmd, args);
			proc.Start();
			var stdout = proc.StandardOutput;
			if (stdout.EndOfStream) throw new Exception($"{noOutputMsg} ({cmd} wrote nothing to stdout)");
			return stdout.ReadLine();
		}
	}
}
