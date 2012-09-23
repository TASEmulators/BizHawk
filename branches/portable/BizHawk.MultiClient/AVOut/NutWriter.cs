using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.MultiClient
{
	/// <summary>
	/// dumps in the "nut" container format
	/// uncompressed video and audio
	/// </summary>
	class NutWriter : IVideoWriter
	{
		/// <summary>
		/// dummy codec token class
		/// </summary>
		class NutWriterToken : IDisposable
		{
			public void Dispose()
			{
			}
		}

		public void SetVideoCodecToken(IDisposable token)
		{
			// ignored
		}
		public IDisposable AcquireVideoCodecToken(System.Windows.Forms.IWin32Window hwnd)
		{
			return new NutWriterToken();
		}

		/// <summary>
		/// avparams
		/// </summary>
		int fpsnum, fpsden, width, height, sampleRate, channels;

		NutMuxer current = null;
		string baseName;
		int segment;

		public void OpenFile(string baseName)
		{
			this.baseName = System.IO.Path.GetFileNameWithoutExtension(baseName);
			segment = 0;


			startsegment();
		}

		void startsegment()
		{
			var currentfile = System.IO.File.Open(String.Format("{0}_{1,4:D4}.nut", baseName, segment), System.IO.FileMode.Create, System.IO.FileAccess.Write);
			current = new NutMuxer(width, height, fpsnum, fpsden, sampleRate, channels, currentfile);
		}

		void endsegment()
		{
			current.Finish();
			current = null;
		}


		public void CloseFile()
		{
			endsegment();
		}

		public void AddFrame(IVideoProvider source)
		{
			if (source.BufferHeight != height || source.BufferWidth != width)
				SetVideoParameters(source.BufferWidth, source.BufferHeight);
			var a = source.GetVideoBuffer();
			var b = new byte[a.Length * sizeof(int)];
			Buffer.BlockCopy(a, 0, b, 0, b.Length);
			current.writevideoframe(b);
		}

		public void AddSamples(short[] samples)
		{
			current.writeaudioframe(samples);
		}


		public void SetMovieParameters(int fpsnum, int fpsden)
		{
			this.fpsnum = fpsnum;
			this.fpsden = fpsden;
			if (current != null)
			{
				endsegment();
				segment++;
				startsegment();
			}
		}

		public void SetVideoParameters(int width, int height)
		{
			this.width = width;
			this.height = height;
			if (current != null)
			{
				endsegment();
				segment++;
				startsegment();
			}
		}

		public void SetAudioParameters(int sampleRate, int channels, int bits)
		{
			if (bits != 16)
				throw new ArgumentOutOfRangeException("audio depth must be 16 bit!");
			this.sampleRate = sampleRate;
			this.channels = channels;
		}

		public void SetMetaData(string gameName, string authors, ulong lengthMS, ulong rerecords)
		{
			// could be implemented?
		}

		public void Dispose()
		{
			if (current != null)
				endsegment();
			baseName = null;
		}

		public override string ToString()
		{
			return "NUT writer";
		}

		public string WriterDescription()
		{
			return "Writes a series of .nut files to disk, a container format which can be opened by ffmpeg.  All data is uncompressed.  Splits occur on resolution changes.  NOT RECCOMENDED FOR USE.";
		}

		public string DesiredExtension()
		{
			return "nut";
		}


		public void SetDefaultVideoCodecToken()
		{
			// ignored
		}

		public string ShortName()
		{
			return "nut";
		}
	}
}
