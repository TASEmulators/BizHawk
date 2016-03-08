using System;
using System.Collections.Generic;
using System.IO;

using BizHawk.Common.IOExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Writes a sequence of 24bpp PNG or JPG files
	/// </summary>
	[VideoWriter("imagesequence", "Image sequence writer", "Writes a sequence of 24bpp PNG or JPG files (default compression quality)")]
	public class ImageSequenceWriter : IDisposable, IVideoWriter
	{
		string BaseName;
		int Frame;

		public void SetVideoCodecToken(IDisposable token)
		{
		}

		public void SetDefaultVideoCodecToken()
		{
		}

		public bool UsesAudio { get { return false; } }
		public bool UsesVideo { get { return true; } }

		public void OpenFile(string baseName)
		{
			BaseName = baseName;
		}

		public void CloseFile()
		{
		}

		public void SetFrame(int frame)
		{
			//eh? this gets ditched somehow
		}

		public void AddFrame(IVideoProvider source)
		{
			string ext = Path.GetExtension(BaseName);
			string name = Path.GetFileNameWithoutExtension(BaseName) + "_" + Frame.ToString();
			name += ext;
			name = Path.Combine(Path.GetDirectoryName(BaseName), name);
			BizHawk.Bizware.BizwareGL.BitmapBuffer bb = new Bizware.BizwareGL.BitmapBuffer(source.BufferWidth, source.BufferHeight, source.GetVideoBuffer());
			using (var bmp = bb.ToSysdrawingBitmap())
			{
				if (ext.ToUpper() == ".PNG")
					bmp.Save(name, System.Drawing.Imaging.ImageFormat.Png);
				else if (ext.ToUpper() == ".JPG")
					bmp.Save(name, System.Drawing.Imaging.ImageFormat.Jpeg);
			}
			Frame++;
		}

		public void AddSamples(short[] samples)
		{
		}

		class CodecToken : IDisposable
		{
			public void Dispose() { }
		}

		public IDisposable AcquireVideoCodecToken(System.Windows.Forms.IWin32Window hwnd)
		{
			return new CodecToken();
		}

		public void SetMovieParameters(int fpsnum, int fpsden)
		{
		}

		public void SetVideoParameters(int width, int height)
		{
		}

		public void SetAudioParameters(int sampleRate, int channels, int bits)
		{
		}

		public void SetMetaData(string gameName, string authors, ulong lengthMS, ulong rerecords)
		{
		}

		public string DesiredExtension()
		{
			return "png";
		}

		public void Dispose()
		{
		}
	}
}
