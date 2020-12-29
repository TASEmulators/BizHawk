using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Collections.Generic;
using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.DiscSystem;

using OSTC = EXE_PROJECT.OSTailoredCode;

namespace BizHawk.Client.DiscoHawk
{
	internal static class Program
	{
		static Program()
		{
			if (OSTC.IsUnixHost)
			{
				// for Unix, skip everything else and just wire up the event handler
				AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
				return;
			}

			// http://www.codeproject.com/Articles/310675/AppDomain-AssemblyResolve-Event-Tips
			// this will look in subdirectory "dll" to load pinvoked stuff
			string dllDir = Path.Combine(GetExeDirectoryAbsolute(), "dll");
			SetDllDirectory(dllDir);

			// in case assembly resolution fails, such as if we moved them into the dll subdirectory, this event handler can reroute to them
			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

			// but before we even try doing that, whack the MOTW from everything in that directory (that's a dll)
			// otherwise, some people will have crashes at boot-up due to .net security disliking MOTW.
			// some people are getting MOTW through a combination of browser used to download BizHawk, and program used to dearchive it
			static void RemoveMOTW(string path) => DeleteFileW($"{path}:Zone.Identifier");
			var todo = new Queue<DirectoryInfo>(new[] { new DirectoryInfo(dllDir) });
			while (todo.Count != 0)
			{
				var di = todo.Dequeue();
				foreach (var diSub in di.GetDirectories()) todo.Enqueue(diSub);
				foreach (var fi in di.GetFiles("*.dll")) RemoveMOTW(fi.FullName);
				foreach (var fi in di.GetFiles("*.exe")) RemoveMOTW(fi.FullName);
			}
		}

		[STAThread]
		private static void Main(string[] args)
		{
			SubMain(args);
		}

		// NoInlining should keep this code from getting jammed into Main() which would create dependencies on types which haven't been setup by the resolver yet... or something like that
		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd, uint msg, ChangeWindowMessageFilterExAction action, ref CHANGEFILTERSTRUCT changeInfo);

		private static void SubMain(string[] args)
		{
			if (!OSTC.IsUnixHost)
			{
				// MICROSOFT BROKE DRAG AND DROP IN WINDOWS 7. IT DOESN'T WORK ANYMORE
				// WELL, OBVIOUSLY IT DOES SOMETIMES. I DON'T REMEMBER THE DETAILS OR WHY WE HAD TO DO THIS SHIT
				// BUT THE FUNCTION WE NEED DOESN'T EXIST UNTIL WINDOWS 7, CONVENIENTLY
				// SO CHECK FOR IT
				IntPtr lib = OSTC.LinkedLibManager.LoadOrThrow("user32.dll");
				IntPtr proc = OSTC.LinkedLibManager.GetProcAddrOrZero(lib, "ChangeWindowMessageFilterEx");
				if (proc != IntPtr.Zero)
				{
					ChangeWindowMessageFilter(WM_DROPFILES, ChangeWindowMessageFilterFlags.Add);
					ChangeWindowMessageFilter(WM_COPYDATA, ChangeWindowMessageFilterFlags.Add);
					ChangeWindowMessageFilter(0x0049, ChangeWindowMessageFilterFlags.Add);
				}
				OSTC.LinkedLibManager.FreeByPtr(lib);
			}

			var ffmpegPath = Path.Combine(GetExeDirectoryAbsolute(), "ffmpeg.exe");
			if (!File.Exists(ffmpegPath))
				ffmpegPath = Path.Combine(Path.Combine(GetExeDirectoryAbsolute(), "dll"), "ffmpeg.exe");
			FFmpegService.FFmpegPath = ffmpegPath;
			AudioExtractor.FFmpegPath = ffmpegPath;

			if (args.Length == 0)
			{
				using var dialog = new MainDiscoForm();
				dialog.ShowDialog();
			}
			else
			{
				DiscoHawkLogic.RunWithArgs(
					args,
					results =>
					{
						using var cr = new ComparisonResults { textBox1 = { Text = results } };
						cr.ShowDialog();
					});
			}
		}


		public static string GetExeDirectoryAbsolute()
		{
			var uri = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
			string module = uri.LocalPath + System.Web.HttpUtility.UrlDecode(uri.Fragment);
			return Path.GetDirectoryName(module);
		}

		private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			lock (AppDomain.CurrentDomain)
			{
				var asms = AppDomain.CurrentDomain.GetAssemblies();
				foreach (var asm in asms)
					if (asm.FullName == args.Name)
						return asm;

				//load missing assemblies by trying to find them in the dll directory
				string dllName = $"{new AssemblyName(args.Name).Name}.dll";
				string directory = Path.Combine(GetExeDirectoryAbsolute(), "dll");
				string fname = Path.Combine(directory, dllName);
				return File.Exists(fname) ? Assembly.LoadFile(fname) : null;

				// it is important that we use LoadFile here and not load from a byte array; otherwise mixed (managed/unmanaged) assemblies can't load
			}
		}

		//declared here instead of a more usual place to avoid dependencies on the more usual place
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool SetDllDirectory(string lpPathName);

		[DllImport("kernel32.dll", EntryPoint = "DeleteFileW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
		private static extern bool DeleteFileW([MarshalAs(UnmanagedType.LPWStr)]string lpFileName);

		private const uint WM_DROPFILES = 0x0233;
		private const uint WM_COPYDATA = 0x004A;
		[DllImport("user32")]
		public static extern bool ChangeWindowMessageFilter(uint msg, ChangeWindowMessageFilterFlags flags);
		public enum ChangeWindowMessageFilterFlags : uint
		{
			Add = 1, Remove = 2
		}
		public enum MessageFilterInfo : uint
		{
			None = 0, AlreadyAllowed = 1, AlreadyDisAllowed = 2, AllowedHigher = 3
		}

		public enum ChangeWindowMessageFilterExAction : uint
		{
			Reset = 0, Allow = 1, DisAllow = 2
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct CHANGEFILTERSTRUCT
		{
			public uint size;
			public MessageFilterInfo info;
		}
	}
}
