using System.IO;

using BizHawk.Client.Common;
using BizHawk.Common.PathExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// dumps in the "nut" container format
	/// uncompressed video and audio
	/// </summary>
	[VideoWriter("nut", "NUT writer", "Writes a series of .nut files to disk, a container format which can be opened by ffmpeg.  All data is uncompressed.  Splits occur on resolution changes.  NOT RECOMMENDED FOR USE.")]
	public class NutWriter : IVideoWriter
	{
		/// <summary>
		/// dummy codec token class
		/// </summary>
		private class NutWriterToken : IDisposable
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
		public IDisposable AcquireVideoCodecToken(Config config)
		{
			return new NutWriterToken();
		}


		// avParams
		private int _fpsNum, _fpsDen, _width, _height, _sampleRate, _channels;

		private NutMuxer _current;
		private string _baseName;
		private int _segment;

		public void OpenFile(string baseName)
		{
			var (dir, fileNoExt, _) = baseName.SplitPathToDirFileAndExt();
			_baseName = Path.Combine(dir ?? string.Empty, fileNoExt);
			_segment = 0;

			StartSegment();
		}

		private void StartSegment()
		{
			var currentFile = File.Open($"{_baseName}_{_segment,4:D4}.nut", FileMode.Create, FileAccess.Write);
			_current = new NutMuxer(_width, _height, _fpsNum, _fpsDen, _sampleRate, _channels, currentFile);
		}

		private void EndSegment()
		{
			_current.Finish();
			_current = null;
		}

		public void CloseFile()
		{
			EndSegment();
		}

		public void AddFrame(IVideoProvider source)
		{
			if (source.BufferHeight != _height || source.BufferWidth != _width)
			{
				SetVideoParameters(source.BufferWidth, source.BufferHeight);
			}

			_current.WriteVideoFrame(source.GetVideoBuffer().AsSpan(0, _width * _height));
		}

		public void AddSamples(short[] samples)
		{
			_current.WriteAudioFrame(samples);
		}

		public void SetMovieParameters(int fpsNum, int fpsDen)
		{
			_fpsNum = fpsNum;
			_fpsDen = fpsDen;
			if (_current != null)
			{
				EndSegment();
				_segment++;
				StartSegment();
			}
		}

		public void SetVideoParameters(int width, int height)
		{
			_width = width;
			_height = height;
			if (_current != null)
			{
				EndSegment();
				_segment++;
				StartSegment();
			}
		}

		/// <exception cref="ArgumentOutOfRangeException"><paramref name="bits"/> is not <c>16</c></exception>
		public void SetAudioParameters(int sampleRate, int channels, int bits)
		{
			if (bits != 16)
			{
				throw new ArgumentOutOfRangeException(nameof(bits), "Audio depth must be 16 bit!");
			}

			_sampleRate = sampleRate;
			_channels = channels;
		}

		public void SetMetaData(string gameName, string authors, ulong lengthMs, ulong rerecords)
		{
			// could be implemented?
		}

		public void Dispose()
		{
			if (_current != null)
			{
				EndSegment();
			}

			_baseName = null;
		}

		public string DesiredExtension() => "nut";

		public void SetDefaultVideoCodecToken(Config config)
		{
			// ignored
		}

		public bool UsesAudio => true;

		public bool UsesVideo => true;
	}
}
