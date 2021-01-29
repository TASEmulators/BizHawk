using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using BizHawk.Common.StringExtensions;

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

		private static readonly Lazy<(WindowsVersion, int?)?> _HostWindowsVersion = new Lazy<(WindowsVersion, int?)?>(() =>
		{
			static string GetRegValue(string key)
			{
				using var proc = ConstructSubshell("REG", $@"QUERY ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion"" /V {key}");
				proc.Start();
				return proc.StandardOutput.ReadToEnd().Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)[1].Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)[2];
			}
			if (CurrentOS != DistinctOS.Windows) return null;
			var rawWinVer = float.Parse(GetRegValue("CurrentVersion"), NumberFormatInfo.InvariantInfo); // contains '.' even when system-wide decimal separator is ','
			WindowsVersion winVer; // sorry if this elif chain is confusing, I couldn't be bothered writing and testing float equality --yoshi
			if (rawWinVer < 6.0f) winVer = WindowsVersion.XP;
			else if (rawWinVer < 6.1f) winVer = WindowsVersion.Vista;
			else if (rawWinVer < 6.2f) winVer = WindowsVersion._7;
			else if (rawWinVer < 6.3f) winVer = WindowsVersion._8;
			else
			{
				// 8.1 and 10 are both version 6.3
				if (GetRegValue("ProductName").Contains("Windows 10"))
				{
					return (WindowsVersion._10, int.Parse(GetRegValue("ReleaseId")));
				}
				// ...else we're on 8.1. Can't be bothered writing code for KB installed check, not that I have a Win8.1 machine to test on anyway, so it gets a free pass --yoshi
				winVer = WindowsVersion._8_1;
			}
			return (winVer, null);
		});

		private static readonly Lazy<bool> _isWSL = new(() => IsUnixHost && SimpleSubshell("uname", "-r", "missing uname?").Contains("microsoft", StringComparison.InvariantCultureIgnoreCase));

		public static (WindowsVersion Version, int? Win10Release)? HostWindowsVersion => _HostWindowsVersion.Value;

		public static readonly bool IsUnixHost = CurrentOS != DistinctOS.Windows;

		public static bool IsWSL => _isWSL.Value;

		private static readonly Lazy<ILinkedLibManager> _LinkedLibManager = new Lazy<ILinkedLibManager>(() => CurrentOS switch
		{
			DistinctOS.Linux => new UnixMonoLLManager(),
			DistinctOS.macOS => new UnixMonoLLManager(),
			DistinctOS.Windows => new WindowsLLManager(),
			_ => throw new ArgumentOutOfRangeException()
		});

		public static ILinkedLibManager LinkedLibManager => _LinkedLibManager.Value;

		/// <remarks>this interface's inheritors hide OS-specific implementation details</remarks>
		public interface ILinkedLibManager
		{
			int FreeByPtr(IntPtr hModule);

			IntPtr GetProcAddrOrZero(IntPtr hModule, string procName);

			/// <exception cref="InvalidOperationException">could not find symbol</exception>
			IntPtr GetProcAddrOrThrow(IntPtr hModule, string procName);

			IntPtr LoadOrZero(string dllToLoad);

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

			public IntPtr GetProcAddrOrZero(IntPtr hModule, string procName) => dlsym(hModule, procName);

			public IntPtr GetProcAddrOrThrow(IntPtr hModule, string procName)
			{
				_ = dlerror(); // the Internet said to do this
				var p = GetProcAddrOrZero(hModule, procName);
				if (p != IntPtr.Zero) return p;
				var errCharPtr = dlerror();
				throw new InvalidOperationException($"error in {nameof(dlsym)}{(errCharPtr == IntPtr.Zero ? string.Empty : $": {Marshal.PtrToStringAnsi(errCharPtr)}")}");
			}

			public IntPtr LoadOrZero(string dllToLoad) => dlopen(dllToLoad, RTLD_NOW);

			public IntPtr LoadOrThrow(string dllToLoad)
			{
				var ret = LoadOrZero(dllToLoad);
				return ret != IntPtr.Zero ? ret : throw new InvalidOperationException($"got null pointer from {nameof(dlopen)}, error: {Marshal.PtrToStringAnsi(dlerror())}");
			}

			private const int RTLD_NOW = 2;
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

			public IntPtr GetProcAddrOrZero(IntPtr hModule, string procName) => GetProcAddress(hModule, procName);

			public IntPtr GetProcAddrOrThrow(IntPtr hModule, string procName)
			{
				var ret = GetProcAddrOrZero(hModule, procName);
				return ret != IntPtr.Zero ? ret : throw new InvalidOperationException($"got null pointer from {nameof(GetProcAddress)}, error code: {GetLastError()}");
			}

			public IntPtr LoadOrZero(string dllToLoad) => LoadLibrary(dllToLoad);

			public IntPtr LoadOrThrow(string dllToLoad)
			{
				var ret = LoadOrZero(dllToLoad);
				return ret != IntPtr.Zero ? ret : throw new InvalidOperationException($"got null pointer from {nameof(LoadLibrary)}, error code: {GetLastError()}");
			}
		}

		public enum DistinctOS : byte
		{
			Linux,
			macOS,
			Windows
		}

		public enum WindowsVersion
		{
			XP,
			Vista,
			_7,
			_8,
			_8_1,
			_10
		}

		/// <param name="cmd">POSIX <c>$0</c></param>
		/// <param name="args">POSIX <c>$*</c> (space-delimited)</param>
		/// <param name="checkStdout">stdout is discarded if false</param>
		/// <param name="checkStderr">stderr is discarded if false</param>
		/// <remarks>OS is implicit and needs to be checked at callsite. Returned <see cref="Process"/> has not been started.</remarks>
		public static Process ConstructSubshell(string cmd, string args, bool checkStdout = true, bool checkStderr = false)
			=> new Process {
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
