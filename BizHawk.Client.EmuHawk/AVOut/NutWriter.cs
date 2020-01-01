using System;
using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// dumps in the "nut" container format
	/// uncompressed video and audio
	/// </summary>
	[VideoWriter("nut", "NUT writer", "Writes a series of .nut files to disk, a container format which can be opened by ffmpeg.  All data is uncompressed.  Splits occur on resolution changes.  NOT RECCOMENDED FOR USE.")]
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

		public void SetFrame(int frame) { }

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
		private int fpsnum, fpsden, width, height, sampleRate, channels;

		private NutMuxer _current = null;
		private string _baseName;
		private int _segment;

		public void OpenFile(string baseName)
		{
			_baseName = Path.Combine(
				Path.GetDirectoryName(baseName),
				Path.GetFileNameWithoutExtension(baseName));
			_segment = 0;

			startsegment();
		}

		private void startsegment()
		{
			var currentfile = File.Open($"{_baseName}_{_segment,4:D4}.nut", FileMode.Create, FileAccess.Write);
			_current = new NutMuxer(width, height, fpsnum, fpsden, sampleRate, channels, currentfile);
		}

		private void endsegment()
		{
			_current.Finish();
			_current = null;
		}

		public void CloseFile()
		{
			endsegment();
		}

		public void AddFrame(IVideoProvider source)
		{
			if (source.BufferHeight != height || source.BufferWidth != width)
			{
				SetVideoParameters(source.BufferWidth, source.BufferHeight);
			}

			_current.WriteVideoFrame(source.GetVideoBuffer());
		}

		public void AddSamples(short[] samples)
		{
			_current.WriteAudioFrame(samples);
		}

		public void SetMovieParameters(int fpsnum, int fpsden)
		{
			this.fpsnum = fpsnum;
			this.fpsden = fpsden;
			if (_current != null)
			{
				endsegment();
				_segment++;
				startsegment();
			}
		}

		public void SetVideoParameters(int width, int height)
		{
			this.width = width;
			this.height = height;
			if (_current != null)
			{
				endsegment();
				_segment++;
				startsegment();
			}
		}

		/// <exception cref="ArgumentOutOfRangeException"><paramref name="bits"/> is not <c>16</c></exception>
		public void SetAudioParameters(int sampleRate, int channels, int bits)
		{
			if (bits != 16)
			{
				throw new ArgumentOutOfRangeException(nameof(bits), "Audio depth must be 16 bit!");
			}

			this.sampleRate = sampleRate;
			this.channels = channels;
		}

		public void SetMetaData(string gameName, string authors, ulong lengthMS, ulong rerecords)
		{
			// could be implemented?
		}

		public void Dispose()
		{
			if (_current != null)
			{
				endsegment();
			}

			_baseName = null;
		}

		public string DesiredExtension()
		{
			return "nut";
		}

		public void SetDefaultVideoCodecToken()
		{
			// ignored
		}

		public bool UsesAudio => true;

		public bool UsesVideo => true;
	}
}
