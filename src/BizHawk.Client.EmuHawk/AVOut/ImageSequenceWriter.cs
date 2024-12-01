using System.IO;
using System.Drawing.Imaging;

using BizHawk.Bizware.Graphics;
using BizHawk.Client.Common;
using BizHawk.Common.PathExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Writes a sequence of 24bpp PNG or JPG files
	/// </summary>
	[VideoWriter("imagesequence", "Image sequence writer", "Writes a sequence of 24bpp PNG or JPG files (default compression quality)")]
	public class ImageSequenceWriter : IDisposable, IVideoWriter
	{
		private string _baseName;
		private int _frame;

		public void SetVideoCodecToken(IDisposable token)
		{
		}

		public void SetDefaultVideoCodecToken(Config config)
		{
		}

		public bool UsesAudio => false;

		public bool UsesVideo => true;

		public void OpenFile(string baseName)
		{
			_baseName = baseName;
		}

		public void CloseFile()
		{
		}

		public void SetFrame(int frame)
		{
			// eh? this gets ditched somehow
		}

		public void AddFrame(IVideoProvider source)
		{
			var (dir, fileNoExt, ext) = _baseName.SplitPathToDirFileAndExt();
			var name = Path.Combine(dir!, $"{fileNoExt}_{_frame}{ext}");
			BitmapBuffer bb = new BitmapBuffer(source.BufferWidth, source.BufferHeight, source.GetVideoBuffer());
			using var bmp = bb.ToSysdrawingBitmap();
			if (ext.ToUpperInvariant() == ".PNG")
			{
				bmp.Save(name, ImageFormat.Png);
			}
			else if (ext.ToUpperInvariant() == ".JPG")
			{
				bmp.Save(name, ImageFormat.Jpeg);
			}

			_frame++;
		}

		public void AddSamples(short[] samples)
		{
		}

		private class CodecToken : IDisposable
		{
			public void Dispose()
			{
			}
		}

		public IDisposable AcquireVideoCodecToken(Config config)
		{
			return new CodecToken();
		}

		public void SetMovieParameters(int fpsNum, int fpsDen)
		{
		}

		public void SetVideoParameters(int width, int height)
		{
		}

		public void SetAudioParameters(int sampleRate, int channels, int bits)
		{
		}

		public void SetMetaData(string gameName, string authors, ulong lengthMs, ulong rerecords)
		{
		}

		public string DesiredExtension() => "png";

		public void Dispose()
		{
		}
	}
}
