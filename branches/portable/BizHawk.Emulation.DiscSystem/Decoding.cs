using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace BizHawk.Emulation.DiscSystem
{
	public class FFMpeg
	{
		public static string FFMpegPath;

		public class AudioQueryResult
		{
			public bool IsAudio;
		}

		private static string[] Escape(IEnumerable<string> args)
		{
			return args.Select(s => s.Contains(" ") ? string.Format("\"{0}\"", s) : s).ToArray();
		}

		static readonly Regex rxHasAudio = new Regex(@"Stream \#(\d*[\.:]\d*)\: Audio", RegexOptions.Compiled);
		public AudioQueryResult QueryAudio(string path)
		{
			var ret = new AudioQueryResult();
			string stdout = Run("-i", path);
			ret.IsAudio = rxHasAudio.Matches(stdout).Count > 0;
			return ret;
		}

		/// <summary>
		/// queries whether this service is available. if ffmpeg is broken or missing, then you can handle it gracefully
		/// </summary>
		public bool QueryServiceAvailable()
		{
			try
			{
				string stdout = Run("-version");
				if (stdout.Contains("ffmpeg version")) return true;
			}
			catch
			{
			}
			return false;
		}

		public string Run(params string[] args)
		{
			args = Escape(args);
			StringBuilder sbCmdline = new StringBuilder();
			for (int i = 0; i < args.Length; i++)
			{
				sbCmdline.Append(args[i]);
				if (i != args.Length - 1) sbCmdline.Append(' ');
			}

			ProcessStartInfo oInfo = new ProcessStartInfo(FFMpegPath, sbCmdline.ToString())
				{
					UseShellExecute = false,
					CreateNoWindow = true,
					RedirectStandardOutput = true,
					RedirectStandardError = true
				};

			Process proc = Process.Start(oInfo);
			#if WINDOWS
			string result = proc.StandardOutput.ReadToEnd();
			result += proc.StandardError.ReadToEnd();
			#else
			string result = proc.StandardOutput.ReadToEnd();
			if(string.IsNullOrEmpty(result))
			{
				//Version goes to Standard output because it's not an error.
				//Info goes to error, because you don't specify output and it's required.
				result = proc.StandardError.ReadToEnd();
			}
			#endif
			proc.WaitForExit();

			return result;
		}

		public byte[] DecodeAudio(string path)
		{
			string tempfile = Path.GetTempFileName();
			try
			{
				string runResults = Run("-i", path, "-f", "wav", "-ar", "44100", "-ac", "2", "-acodec", "pcm_s16le", "-y", tempfile);
				byte[] ret = File.ReadAllBytes(tempfile);
				if (ret.Length == 0)
					throw new InvalidOperationException("Failure running ffmpeg for audio decode. here was its output:\r\n" + runResults);
				return ret;
			}
			finally
			{
				File.Delete(tempfile);
			}
		}
	}

	class AudioDecoder
	{
		[Serializable]
		public class AudioDecoder_Exception : Exception
		{
			public AudioDecoder_Exception(string message)
				: base(message)
			{
			}
		}

		public AudioDecoder()
		{
		}

		bool CheckForAudio(string path)
		{
			FFMpeg ffmpeg = new FFMpeg();
			var qa = ffmpeg.QueryAudio(path);
			return qa.IsAudio;
		}

		/// <summary>
		/// finds audio at a path similar to the provided path (i.e. finds Track01.mp3 for Track01.wav)
		/// </summary>
		string FindAudio(string audioPath)
		{
			string basePath = Path.GetFileNameWithoutExtension(audioPath);
			//look for potential candidates
			var di = new DirectoryInfo(Path.GetDirectoryName(audioPath));
			var fis = di.GetFiles();
			//first, look for the file type we actually asked for
			foreach (var fi in fis)
			{
				if (fi.FullName.ToUpper() == audioPath.ToUpper())
					if (CheckForAudio(fi.FullName))
						return fi.FullName;
			}
			//then look for any other type
			foreach (var fi in fis)
			{
				if (Path.GetFileNameWithoutExtension(fi.FullName).ToUpper() == basePath.ToUpper())
				{
					if (CheckForAudio(fi.FullName))
					{
						return fi.FullName;
					}
				}
			}
			return null;
		}

		public byte[] AcquireWaveData(string audioPath)
		{
			string path = FindAudio(audioPath);
			if (path == null)
			{
				throw new AudioDecoder_Exception("Could not find source audio for: " + Path.GetFileName(audioPath));
			}
			return new FFMpeg().DecodeAudio(path);
		}

	}
}