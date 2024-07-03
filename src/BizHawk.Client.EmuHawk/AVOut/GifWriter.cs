using System.IO;
using System.Drawing;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

using PcxFileTypePlugin.Quantize;

namespace BizHawk.Client.EmuHawk
{
	[VideoWriter("gif", "GIF writer", "Creates an animated .gif")]
	public class GifWriter : IVideoWriter
	{
		public class GifToken : IDisposable
		{
			private int _frameskip, _framedelay;

			/// <summary>
			/// Gets how many frames to skip for each frame deposited
			/// </summary>
			public int Frameskip
			{
				get => _frameskip;
				private set
				{
					if (value < 0)
					{
						_frameskip = 0;
					}
					else if (value > 999)
					{
						_frameskip = 999;
					}
					else
					{
						_frameskip = value;
					}
				}
			}

			/// <summary>
			/// how long to delay between each gif frame (units of 10ms, -1 = auto)
			/// </summary>
			public int FrameDelay
			{
				get => _framedelay;
				private set
				{
					if (value < -1)
					{
						_framedelay = -1;
					}
					else if (value > 100)
					{
						_framedelay = 100;
					}
					else
					{
						_framedelay = value;
					}
				}
			}

			public static GifToken LoadFromConfig(Config config)
			{
				return new GifToken(0, 0)
				{
					Frameskip = config.GifWriterFrameskip,
					FrameDelay = config.GifWriterDelay
				};
			}

			public void Dispose()
			{
			}

			private GifToken(int frameskip, int frameDelay)
			{
				Frameskip = frameskip;
				FrameDelay = frameDelay;
			}
		}

		private readonly IDialogParent _dialogParent;

		private GifToken _token;

		public GifWriter(IDialogParent dialogParent) => _dialogParent = dialogParent;

		/// <exception cref="ArgumentException"><paramref name="token"/> does not inherit <see cref="GifWriter.GifToken"/></exception>
		public void SetVideoCodecToken(IDisposable token)
		{
			if (token is GifToken gifToken)
			{
				_token = gifToken;
				CalcDelay();
			}
			else
			{
				throw new ArgumentException(message: $"{nameof(GifWriter)} only takes its own tokens!", paramName: nameof(token));
			}
		}

		public void SetDefaultVideoCodecToken(Config config)
		{
			_token = GifToken.LoadFromConfig(config);
			CalcDelay();
		}

		/// <summary>
		/// true if the first frame has been written to the file; false otherwise
		/// </summary>
		private bool _firstDone;

		/// <summary>
		/// the underlying stream we're writing to
		/// </summary>
		private Stream _f;

		/// <summary>
		/// a final byte we must write before closing the stream
		/// </summary>
		private byte _lastByte;

		/// <summary>
		/// keep track of skippable frames
		/// </summary>
		private int _skipIndex = 0;

		private int _fpsNum = 1, _fpsDen = 1;

		public void SetFrame(int frame)
		{
		}

		public void OpenFile(string baseName)
		{
			_f = new FileStream(baseName, FileMode.OpenOrCreate, FileAccess.Write);
			_skipIndex = _token.Frameskip;
		}

		public void CloseFile()
		{
			_f.WriteByte(_lastByte);
			_f.Close();
		}

		/// <summary>
		/// precooked gif header
		/// </summary>
		private static readonly byte[] GifAnimation = { 33, 255, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48, 3, 1, 0, 0, 0 };

		/// <summary>
		/// little endian frame length in 10ms units
		/// </summary>
		private readonly byte[] _delay = { 100, 0 };

		public void AddFrame(IVideoProvider source)
		{
			if (_skipIndex == _token.Frameskip)
			{
				_skipIndex = 0;
			}
			else
			{
				_skipIndex++;
				return; // skip this frame
			}

			using var bmp = new Bitmap(source.BufferWidth, source.BufferHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			System.Runtime.InteropServices.Marshal.Copy(source.GetVideoBuffer(), 0, data.Scan0, bmp.Width * bmp.Height);
			bmp.UnlockBits(data);

			using var qBmp = new OctreeQuantizer(255, 8).Quantize(bmp);
			MemoryStream ms = new MemoryStream();
			qBmp.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
			byte[] b = ms.GetBuffer();
			if (!_firstDone)
			{
				_firstDone = true;
				b[10] = (byte)(b[10] & 0x78); // no global color table
				_f.Write(b, 0, 13);
				_f.Write(GifAnimation, 0, GifAnimation.Length);
			}

			b[785] = _delay[0];
			b[786] = _delay[1];
			b[798] = (byte)(b[798] | 0x87);
			_f.Write(b, 781, 18);
			_f.Write(b, 13, 768);
			_f.Write(b, 799, (int)(ms.Length - 800));

			_lastByte = b[ms.Length - 1];
		}

		public void AddSamples(short[] samples)
		{
			// ignored
		}

		public IDisposable AcquireVideoCodecToken(Config config)
		{
			return GifWriterForm.DoTokenForm(_dialogParent.AsWinFormsHandle(), config);
		}

		private void CalcDelay()
		{
			if (_token == null)
			{
				return;
			}

			int delay;
			if (_token.FrameDelay == -1)
			{
				delay = (100 * _fpsDen * (_token.Frameskip + 1) + (_fpsNum / 2)) / _fpsNum;
			}
			else
			{
				delay = _token.FrameDelay;
			}

			_delay[0] = (byte)(delay & 0xff);
			_delay[1] = (byte)(delay >> 8 & 0xff);
		}

		public void SetMovieParameters(int fpsNum, int fpsDen)
		{
			this._fpsNum = fpsNum;
			this._fpsDen = fpsDen;
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


		public string DesiredExtension()
		{
			return "gif";
		}

		public void Dispose()
		{
			if (_f != null)
			{
				_f.Dispose();
				_f = null;
			}
		}

		public bool UsesAudio => false;

		public bool UsesVideo => true;
	}
}
