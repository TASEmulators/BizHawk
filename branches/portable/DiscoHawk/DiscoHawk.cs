using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Collections.Generic;
using System.IO;

//cue format preferences notes

//pcejin -
//does not like session commands
//it can handle binpercue
//it seems not to be able to handle binpertrack, or maybe i am doing something wrong (still havent ruled it out)

namespace BizHawk
{
	class DiscoHawk
	{

		public static string GetExeDirectoryAbsolute()
		{
			var uri = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
			string module = uri.LocalPath + System.Web.HttpUtility.UrlDecode(uri.Fragment);
			return Path.GetDirectoryName(module);
		}

		static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			//load missing assemblies by trying to find them in the dll directory
			string dllname = new AssemblyName(args.Name).Name + ".dll";
			string directory = System.IO.Path.Combine(GetExeDirectoryAbsolute(), "dll");
			string fname = Path.Combine(directory, dllname);
			if (!File.Exists(fname)) return null;
			//it is important that we use LoadFile here and not load from a byte array; otherwise mixed (managed/unamanged) assemblies can't load
			return Assembly.LoadFile(fname);
		}

		//declared here instead of a more usual place to avoid dependencies on the more usual place
#if WINDOWS
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool SetDllDirectory(string lpPathName);
#endif

		[STAThread]
		static void Main(string[] args)
		{
#if WINDOWS
			// this will look in subdirectory "dll" to load pinvoked stuff
			SetDllDirectory(System.IO.Path.Combine(GetExeDirectoryAbsolute(), "dll"));

			//in case assembly resolution fails, such as if we moved them into the dll subdiretory, this event handler can reroute to them
			AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
#endif

			SubMain(args);
		}

		static void SubMain(string[] args)
		{
			var ffmpegPath = Path.Combine(GetExeDirectoryAbsolute(), "ffmpeg.exe");
			if(!File.Exists(ffmpegPath))
				ffmpegPath = Path.Combine(Path.Combine(GetExeDirectoryAbsolute(), "dll"), "ffmpeg.exe");
			DiscSystem.FFMpeg.FFMpegPath = ffmpegPath;
			AudioExtractor.FFmpegPath = ffmpegPath;
			new DiscoHawk().Run(args);
		}

		void Run(string[] args)
		{
			bool gui = true;
			foreach (var arg in args)
			{
				if (arg.ToUpper() == "COMMAND") gui = false;
			}

			if (gui)
			{
				var dialog = new MainDiscoForm();
				dialog.ShowDialog();
				return;
			}

		}
	}

}