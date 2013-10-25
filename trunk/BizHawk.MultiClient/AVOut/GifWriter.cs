using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;

namespace BizHawk.MultiClient.AVOut
{
	public class GifWriter : IVideoWriter
	{
		public class GifToken : IDisposable
		{
			private int _frameskip, _framedelay;

			/// <summary>
			/// how many frames to skip for each frame deposited
			/// </summary>
			public int frameskip
			{
				get { return _frameskip; }
				private set
				{
					if (value < 0)
						_frameskip = 0;
					else if (value > 999)
						_frameskip = 999;
					else
						_frameskip = value;
				}
			}

			/// <summary>
			/// how long to delay between each gif frame (units of 10ms, -1 = auto)
			/// </summary>
			public int framedelay
			{
				get { return _framedelay; }
				private set
				{
					if (value < -1)
						_framedelay = -1;
					else if (value > 100)
						_framedelay = 100;
					else
						_framedelay = value;
				}
			}

			public void Dispose() { }

			public GifToken(int frameskip, int framedelay)
			{
				this.frameskip = frameskip;
				this.framedelay = framedelay;
			}

			public static GifToken LoadFromConfig()
			{
				GifToken ret = new GifToken(0, 0);
				ret.frameskip = Global.Config.GifWriterFrameskip;
				ret.framedelay = Global.Config.GifWriterDelay;
				return ret;
			}
		}
		GifToken token;

		public void SetVideoCodecToken(IDisposable token)
		{
			if (token is GifToken)
			{
				this.token = (GifToken)token;
				CalcDelay();
			}
			else
				throw new ArgumentException("GifWriter only takes its own tokens!");
		}

		public void SetDefaultVideoCodecToken()
		{
			token = GifToken.LoadFromConfig();
			CalcDelay();
		}

		/// <summary>
		/// true if the first frame has been written to the file; false otherwise
		/// </summary>
		bool firstdone = false;

		/// <summary>
		/// the underlying stream we're writing to
		/// </summary>
		Stream f;

		/// <summary>
		/// a final byte we must write before closing the stream
		/// </summary>
		byte lastbyte;

		/// <summary>
		/// keep track of skippable frames
		/// </summary>
		int skipindex = 0;

		int fpsnum = 1, fpsden = 1;


		public void OpenFile(string baseName)
		{
			f = new FileStream(baseName, FileMode.OpenOrCreate, FileAccess.Write);
			skipindex = token.frameskip;
		}

		public void CloseFile()
		{
			f.WriteByte(lastbyte);
			f.Close();
		}

		/// <summary>
		/// precooked gif header
		/// </summary>
		static byte[] GifAnimation = {33, 255, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48, 3, 1, 0, 0, 0};
		/// <summary>
		/// little endian frame length in 10ms units
		/// </summary>
		byte[] Delay = {100, 0};
		public void AddFrame(IVideoProvider source)
		{
			if (skipindex == token.frameskip)
				skipindex = 0;
			else
			{
				skipindex++;
				return; // skip this frame
			}


			Bitmap bmp = new Bitmap(source.BufferWidth, source.BufferHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			{
				var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				System.Runtime.InteropServices.Marshal.Copy(source.GetVideoBuffer(), 0, data.Scan0, bmp.Width * bmp.Height);
				bmp.UnlockBits(data);
			}

			MemoryStream ms = new MemoryStream();
			bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
			byte[] b = ms.GetBuffer();
			if (!firstdone)
			{
				firstdone = true;
				b[10] = (byte)(b[10] & 0x78); // no global color table
				f.Write(b, 0, 13);
				f.Write(GifAnimation, 0, GifAnimation.Length);
			}
			b[785] = Delay[0];
			b[786] = Delay[1];
			b[798] = (byte)(b[798] | 0x87);
			f.Write(b, 781, 18);
			f.Write(b, 13, 768);
			f.Write(b, 799, (int)(ms.Length - 800));

			lastbyte = b[ms.Length - 1];
		}

		public void AddSamples(short[] samples)
		{
			// ignored
		}

		public IDisposable AcquireVideoCodecToken(System.Windows.Forms.IWin32Window hwnd)
		{
			return GifWriterForm.DoTokenForm(hwnd);
		}

		void CalcDelay()
		{
			if (token == null)
				return;
			int delay;
			if (token.framedelay == -1)
				delay = (100 * fpsden * (token.frameskip + 1) + (fpsnum / 2)) / fpsnum;
			else
				delay = token.framedelay;
			Delay[0] = (byte)(delay & 0xff);
			Delay[1] = (byte)(delay >> 8 & 0xff);
		}

		public void SetMovieParameters(int fpsnum, int fpsden)
		{
			this.fpsnum = fpsnum;
			this.fpsden = fpsden;
			CalcDelay();
		}

		public void SetVideoParameters(int width, int height)
		{
			// we read them directly from each individual frame, ignore the rest
		}

		public void SetAudioParameters(int sampleRate, int channels, int bits)
		{
			// ignored
		}

		public void SetMetaData(string gameName, string authors, ulong lengthMS, ulong rerecords)
		{
			// gif can't support this
		}

		public string WriterDescription()
		{
			return "Creates an animated .gif";
		}

		public string DesiredExtension()
		{
			return "gif";
		}

		public string ShortName()
		{
			return "gif";
		}

		public void Dispose()
		{
			if (f != null)
			{
				f.Dispose();
				f = null;
			}
		}

		public override string ToString()
		{
			return "GIF writer";
		}
	}
}
