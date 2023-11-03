#pragma warning disable IDE0240
#nullable enable
#pragma warning restore IDE0240

using System;
using System.Diagnostics;
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

		private static readonly Lazy<(WindowsVersion, Version?)?> _HostWindowsVersion = new(() =>
		{
			static string? GetRegValue(string key)
			{
				try
				{
					using var proc = ConstructSubshell("REG", $@"QUERY ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion"" /V {key}");
					proc.Start();
					return proc.StandardOutput.ReadToEnd().Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)[1].Split(new[] { "\t", "    " }, StringSplitOptions.RemoveEmptyEntries)[2];
				}
				catch (Exception)
				{
					// Education edition? Poor Group Policy setup? https://github.com/TASEmulators/BizHawk/issues/2972
					return null;
				}
			}
			if (CurrentOS != DistinctOS.Windows) return null;
			Version rawWinVer = new(GetRegValue("CurrentVersion") ?? "6.3");
			WindowsVersion winVer;
			if (rawWinVer >= new Version(6, 3))
			{
				// Win8.1, Win10, and Win11 all have CurrentVersion == "6.3"
				if ((GetRegValue("ProductName") ?? "Windows 10").Contains("Windows 10"))
				{
					// Win11 has ProductName == "Windows 10 Pro" MICROSOFT WHY https://stackoverflow.com/a/69922526 https://stackoverflow.com/a/70456554
//					Version win10PlusVer = new(FileVersionInfo.GetVersionInfo(@"C:\Windows\System32\kernel32.dll").FileVersion.SubstringBefore(' ')); // bonus why: this doesn't work because the file's metadata wasn't updated
					Version win10PlusVer = new(10, 0, int.TryParse(GetRegValue("CurrentBuild") ?? "19044", out var i) ? i : 19044); // still, leaving the Version wrapper here for when they inevitably do something stupid like decide to call Win11 11.0.x (or 10.1.x because they're incapable of doing anything sensible)
					return (win10PlusVer < new Version(10, 0, 22000) ? WindowsVersion._10 : WindowsVersion._11, win10PlusVer);
				}
				// ...else we're on 8.1. Can't be bothered writing code for KB installed check, not that I have a Win8.1 machine to test on anyway, so it gets a free pass (though I suspect `CurrentBuild` would work here too). --yoshi
				winVer = WindowsVersion._8_1;
			}
			else if (rawWinVer == new Version(6, 2)) winVer = WindowsVersion._8;
			else if (rawWinVer == new Version(6, 1)) winVer = WindowsVersion._7;
			// in reality, EmuHawk will not run on these OSes, but here they are for posterity
			else if (rawWinVer == new Version(6, 0)) winVer = WindowsVersion.Vista;
			else /*if (rawWinVer < new Version(6, 0))*/ winVer = WindowsVersion.XP;
			return (winVer, null);
		});

		private static readonly Lazy<bool> _isWSL = new(() => IsUnixHost && SimpleSubshell("uname", "-r", "missing uname?").Contains("microsoft", StringComparison.InvariantCultureIgnoreCase));

		public static (WindowsVersion Version, Version? Win10PlusVersion)? HostWindowsVersion => _HostWindowsVersion.Value;

		public static readonly bool IsUnixHost = CurrentOS != DistinctOS.Windows;

		public static bool IsWSL => _isWSL.Value;

		private static readonly Lazy<bool> _isWine = new(() =>
		{
			if (IsUnixHost)
			{
				return false;
			}

			var ntdll = LinkedLibManager.LoadOrZero("ntdll.dll");
			if (ntdll == IntPtr.Zero)
			{
				return false;
			}

			var isWine = LinkedLibManager.GetProcAddrOrZero(ntdll, "wine_get_version") != IntPtr.Zero;
			LinkedLibManager.FreeByPtr(ntdll);
			return isWine;
		});

		public static bool IsWine => _isWine.Value;

		private static readonly Lazy<ILinkedLibManager> _LinkedLibManager = new Lazy<ILinkedLibManager>(() => CurrentOS switch
		{
			DistinctOS.Linux => new UnixMonoLLManager(),
			DistinctOS.macOS => new UnixMonoLLManager(),
			DistinctOS.Windows => new WindowsLLManager(),
			_ => throw new InvalidOperationException()
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

			string GetErrorMessage();
		}

		private class UnixMonoLLManager : ILinkedLibManager
		{
			private const int RTLD_NOW = 2;

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

			public string GetErrorMessage()
			{
				var errCharPtr = dlerror();
				return errCharPtr == IntPtr.Zero ? "No error present" : Marshal.PtrToStringAnsi(errCharPtr)!;
			}
		}

		private class WindowsLLManager : ILinkedLibManager
		{
			// functions taken from LoaderApiImports
			// TODO: Should we apply the same EXE_PROJECT hack to that file?

			[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
			private static extern IntPtr LoadLibraryW(string lpLibFileName);

			[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			private static extern bool FreeLibrary(IntPtr hLibModule);

			[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
			private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

			public int FreeByPtr(IntPtr hModule) => FreeLibrary(hModule) ? 0 : 1;

			public IntPtr GetProcAddrOrZero(IntPtr hModule, string procName) => GetProcAddress(hModule, procName);

			public IntPtr GetProcAddrOrThrow(IntPtr hModule, string procName)
			{
				var ret = GetProcAddrOrZero(hModule, procName);
				return ret != IntPtr.Zero ? ret : throw new InvalidOperationException($"got null pointer from {nameof(GetProcAddress)}, {GetErrorMessage()}");
			}

			public IntPtr LoadOrZero(string dllToLoad) => LoadLibraryW(dllToLoad);

			public IntPtr LoadOrThrow(string dllToLoad)
			{
				var ret = LoadOrZero(dllToLoad);
				return ret != IntPtr.Zero ? ret : throw new InvalidOperationException($"got null pointer from {nameof(LoadLibraryW)}, {GetErrorMessage()}");
			}

			[DllImport("kernel32.dll", ExactSpelling = true)]
			private static extern uint GetLastError();

			[DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
			private static extern unsafe int FormatMessageW(int flags, IntPtr source, uint messageId, uint languageId, char* outMsg, int size, IntPtr args);

			public unsafe string GetErrorMessage()
			{
				var errCode = GetLastError();
				var buffer = stackalloc char[1024];
				const int FORMAT_MESSAGE_FROM_SYSTEM = 0x1000;
				var sz = FormatMessageW(FORMAT_MESSAGE_FROM_SYSTEM, IntPtr.Zero, errCode, 0, buffer, 1024, IntPtr.Zero);
				return $"error code: 0x{errCode:X8}, error message: {new string(buffer, 0, sz)}";
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
			_10,
			_11,
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
			return stdout.ReadLine()!;
		}
	}
}
