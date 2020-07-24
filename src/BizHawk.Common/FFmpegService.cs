using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace BizHawk.Common
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
			return args.Select(s => s.Contains(" ") ? $"\"{s}\"" : s).ToArray();
		}

		//note: accepts . or : in the stream stream/substream separator in the stream ID format, since that changed at some point in FFMPEG history
		//if someone has a better idea how to make the determination of whether an audio stream is available, I'm all ears
		private static readonly Regex rxHasAudio = new Regex(@"Stream \#(\d*(\.|\:)\d*)\: Audio", RegexOptions.Compiled);
		public AudioQueryResult QueryAudio(string path)
		{
			var ret = new AudioQueryResult();
			string stdout = Run("-i", path).Text;
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
				string stdout = Run("-version").Text;
				if (stdout.Contains("ffmpeg version")) return true;
			}
			catch
			{
			}
			return false;
		}

		public struct RunResults
		{
			public string Text;
			public int ExitCode;
		}

		public RunResults Run(params string[] args)
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
			string result = proc.StandardOutput.ReadToEnd();
			result += proc.StandardError.ReadToEnd();
			proc.WaitForExit();

			return new RunResults
			{
				ExitCode = proc.ExitCode,
				Text = result
			};
		}

		/// <exception cref="InvalidOperationException">FFmpeg exited with non-zero exit code or produced no output</exception>
		public byte[] DecodeAudio(string path)
		{
			string tempfile = Path.GetTempFileName();
			try
			{
				var runResults = Run("-i", path, "-xerror", "-f", "wav", "-ar", "44100", "-ac", "2", "-acodec", "pcm_s16le", "-y", tempfile);
				if(runResults.ExitCode != 0)
					throw new InvalidOperationException($"Failure running ffmpeg for audio decode. here was its output:\r\n{runResults.Text}");
				byte[] ret = File.ReadAllBytes(tempfile);
				if (ret.Length == 0)
					throw new InvalidOperationException($"Failure running ffmpeg for audio decode. here was its output:\r\n{runResults.Text}");
				return ret;
			}
			finally
			{
				File.Delete(tempfile);
			}
		}
	}

}