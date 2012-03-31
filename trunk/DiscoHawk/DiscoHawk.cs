using System;
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
			string p = Path.GetDirectoryName(Assembly.GetEntryAssembly().GetName().CodeBase);
			if (p.Substring(0, 6) == "file:\\")
				p = p.Remove(0, 6);
			string z = p;
			return p;
		}

		[STAThread]
		static void Main(string[] args)
		{
            var ffmpegPath = Path.Combine(GetExeDirectoryAbsolute(), "ffmpeg.exe");
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