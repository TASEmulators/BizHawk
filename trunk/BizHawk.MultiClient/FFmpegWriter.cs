using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;

namespace BizHawk.MultiClient
{
	/// <summary>
	/// uses tcp sockets to launch an external ffmpeg process and encode
	/// </summary>
	class FFmpegWriter : WavWriterV, IVideoWriter
	{
		/// <summary>
		/// handle to external ffmpeg process
		/// </summary>
		Process ffmpeg;

		/// <summary>
		/// the commandline actually sent to ffmpeg; for informative purposes
		/// </summary>
		string commandline;

		/// <summary>
		/// current file segment (for multires)
		/// </summary>
		int segment;

		/// <summary>
		/// base filename before segment number is attached
		/// </summary>
		string baseName;

		/// <summary>
		/// recent lines in ffmpeg's stderr, for informative purposes
		/// </summary>
		Queue<string> stderr;

		/// <summary>
		/// number of lines of stderr to buffer
		/// </summary>
		const int consolebuffer = 5;

		public new void OpenFile(string baseName)
		{
			string s = System.IO.Path.GetFileNameWithoutExtension(baseName);

			base.OpenFile(s + ".wav");

			this.baseName = s;

			segment = 0;
			OpenFileSegment();
		}
		
		/// <summary>
		/// starts an ffmpeg process and sets up associated sockets
		/// </summary>
		void OpenFileSegment()
		{
			ffmpeg = new Process();
			ffmpeg.StartInfo.FileName = "ffmpeg";

			string filename = String.Format("{0}_{1,4:D4}", baseName, segment);

			ffmpeg.StartInfo.Arguments = String.Format
			(
				"-y -f rawvideo -pix_fmt bgra -s {0}x{1} -r {2}/{3} -i - -vcodec libx264rgb -crf 0 \"{4}.mkv\"",
				width,
				height,
				fpsnum,
				fpsden,
				filename
			);

			ffmpeg.StartInfo.CreateNoWindow = true;

			// ffmpeg sends informative display to stderr, and nothing to stdout
			ffmpeg.StartInfo.RedirectStandardError = true;
			ffmpeg.StartInfo.RedirectStandardInput = true;
			ffmpeg.StartInfo.UseShellExecute = false;

			commandline = "ffmpeg " + ffmpeg.StartInfo.Arguments;

			ffmpeg.ErrorDataReceived += new DataReceivedEventHandler(StderrHandler);

			stderr = new Queue<string>(consolebuffer);

			ffmpeg.Start();
			ffmpeg.BeginErrorReadLine();
		}

		/// <summary>
		/// saves stderr lines from ffmpeg in a short queue
		/// </summary>
		/// <param name="p"></param>
		/// <param name="line"></param>
		void StderrHandler(object p, DataReceivedEventArgs line)
		{
			if (!String.IsNullOrEmpty(line.Data))
			{
				if (stderr.Count == consolebuffer)
					stderr.Dequeue();
				stderr.Enqueue(line.Data + "\n");
			}
		}

		/// <summary>
		/// finishes an ffmpeg process
		/// </summary>
		void CloseFileSegment()
		{
			ffmpeg.StandardInput.Close();

			// how long should we wait here?
			ffmpeg.WaitForExit(20000);
			ffmpeg = null;
			stderr = null;
			commandline = null;
		}


		public new void CloseFile()
		{
			CloseFileSegment();
			baseName = null;
			base.CloseFile();
		}

		/// <summary>
		/// returns a string containing the commandline sent to ffmpeg and recent console (stderr) output
		/// </summary>
		/// <returns></returns>
		string ffmpeg_geterror()
		{
			if (ffmpeg.StartInfo.RedirectStandardError)
			{
				ffmpeg.CancelErrorRead();
			}
			StringBuilder s = new StringBuilder();
			s.Append(commandline);
			s.Append('\n');
			while (stderr.Count > 0)
			{
				var foo = stderr.Dequeue();
				System.Windows.Forms.MessageBox.Show(foo);
				s.Append(foo);
			}
			return s.ToString();
		}


		public new void AddFrame(IVideoProvider source)
		{
			if (source.BufferWidth != width || source.BufferHeight != height)
				SetVideoParameters(source.BufferWidth, source.BufferHeight);

			if (ffmpeg.HasExited)
				throw new Exception("unexpected ffmpeg death:\n" + ffmpeg_geterror());
			var a = source.GetVideoBuffer();
			var b = new byte[a.Length * sizeof (int)];
			Buffer.BlockCopy(a, 0, b, 0, b.Length);
			// have to do binary write!
			ffmpeg.StandardInput.BaseStream.Write(b, 0, b.Length);
		}


		/// <summary>
		/// codec token for FFmpegWriter
		/// </summary>
		class FFmpegWriterToken : IDisposable
		{
			public void Dispose()
			{
			}
		}

		public new IDisposable AcquireVideoCodecToken(IntPtr hwnd)
		{
			return new FFmpegWriterToken();
		}
	
		public new void SetVideoCodecToken(IDisposable token)
		{
			// nyi
		}

		/// <summary>
		/// video params
		/// </summary>
		int fpsnum, fpsden, width, height;

		public new void SetMovieParameters(int fpsnum, int fpsden)
		{
			this.fpsnum = fpsnum;
			this.fpsden = fpsden;
		}

		public new void SetVideoParameters(int width, int height)
		{
			this.width = width;
			this.height = height;

			/* ffmpeg theoretically supports variable resolution videos, but there's no way to
			 * signal that metadata in a raw pipe.  so if we're currently in a segment,
			 * start a new one */
			if (ffmpeg != null)
			{
				CloseFileSegment();
				segment++;
				OpenFileSegment();
			}
		}


		public new void SetMetaData(string gameName, string authors, ulong lengthMS, ulong rerecords)
		{
			// can be implemented with ffmpeg "-metadata" parameter???
			// nyi
		}

		public new void Dispose()
		{
			base.Dispose();
		}
	}
}
