
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.SuperVision
{
	/// <summary>
	/// The LCD screen arrangement
	/// </summary>
	public class LCD : IVideoProvider
	{
		public static readonly int[] Palette_BW =
		[
			Colors.ARGB(0xFC, 0xFC, 0xFC), // OFF
			Colors.ARGB(0xA8, 0xA8, 0xA8), // 1/3 DARKNESS
			Colors.ARGB(0x54, 0x54, 0x54), // 2/3 DARKNESS
			Colors.ARGB(0x00, 0x00, 0x00), // FULL DARK
		];

		public static readonly int[] Palette_GR =
		[
			Colors.ARGB(0x56, 0x79, 0x56), // OFF
			Colors.ARGB(0x51, 0x73, 0x5A), // 1/3 DARKNESS
			Colors.ARGB(0x4A, 0x6A, 0x66), // 2/3 DARKNESS
			Colors.ARGB(0x36, 0x4E, 0x54), // FULL DARK
		];

		private readonly int[] _palette = new int[4];

		public const int PEN_BUFFER_WIDTH = 160 * 2;
		public const int PEN_BUFFER_HEIGHT = 160;

		/// <summary>
		/// The inbuilt screen is a 160*160 dot 2bpp monochrome LCD
		/// </summary>
		private int[] _penBuffer = new int[PEN_BUFFER_WIDTH * PEN_BUFFER_HEIGHT];

		/// <summary>
		/// The output framebuffer
		/// </summary>
		private int[] _frameBuffer = new int[160 * 160];

		private int _hPos;
		private int _vPos;

		public bool DisplayEnable;

		public LCD(SuperVision.ScreenType screenType)
		{
			switch (screenType)
			{
				case SuperVision.ScreenType.Green:
					_palette = Palette_GR;
					break;
				case SuperVision.ScreenType.Monochrome:
				default:
					_palette = Palette_BW;
					break;
			}
		}

		public void ResetPosition()
		{
			_hPos = 0;
			_vPos = 0;
		}

		// The LCD is physically connected with 9 outputs from the ASIC.  
		// In order, they are:
		// 1 - Data 0
		// 2 - Data 1
		// 3 - Data 2
		// 4 - Data 3
		// 5 - Pixel Clock
		// 6 - Line Latch
		// 7 - Frame Latch
		// 8 - Frame Polarity/bright control
		// 9 - Power Control

		/// <summary>
		/// It takes 6 cycles for each write to the LCD screen (so 1 pixelclock == 6 cpu cycles)
		/// </summary>
		public void PixelClock(byte data, int framePolarity = 0, bool lineLatch = false, bool frameLatch = false)
		{
			// Each scanline is composed of 246 clocks.
			// There are 40 pixel writes to the LCD, and 1 latch write, for a total of 41 writes.
			// Each write period lasts 6 clock cycles, so 41*6 = 246 cycles
			if (frameLatch)
			{
				// end of field
				// write last pixel
				if (DisplayEnable)
					WritePixels(data, framePolarity);

				// setup for next frame
				ResetPosition();
			}
			else if (lineLatch)
			{
				// end of scanline
				_hPos = 0;
				_vPos++;
			}
			else
			{
				if (DisplayEnable)
					WritePixels(data, framePolarity);
			}
		}

		/// <summary>
		/// This outputs 4 pixels at a time - we only use the first 4 bits of the data
		/// 2BPP, bit0 is written in the first field, bit1 in the second
		/// </summary>
		private void WritePixels(byte data, int framePolarity)
		{
			for (int i = 0; i < 4; i++)
			{
				if (_hPos < PEN_BUFFER_WIDTH && _vPos < PEN_BUFFER_HEIGHT)
				{
					_penBuffer[(_vPos * 160 * 2) + (_hPos + framePolarity)] = (data >> i) & 0x01;					
				}
				else
				{
					// bits out of bounds of the LCD screen
					// data is discarded
				}

				_hPos += 2;
			}
		}

		public void SetRates(int num, int dom)
		{
			VsyncNumerator = num;
			VsyncDenominator = dom;
		}

		public int VirtualWidth => (int)(BufferWidth * 1.25);
		public int VirtualHeight => BufferHeight;
		public int BufferWidth => 160;
		public int BufferHeight => 160;
		public int BackgroundColor => _palette[0];
		public int VsyncNumerator { get; private set; }
		public int VsyncDenominator { get; private set; }

		public int[] GetVideoBuffer()
		{
			for (int i = 0, d = 0; i < _penBuffer.Length; i += 2, d++)
			{
				int pen = _penBuffer[i] | (_penBuffer[i + 1] << 1);
				_frameBuffer[d] = _palette[pen];
			}

			return _frameBuffer;
		}

		public virtual void SyncState(Serializer ser)
		{
			ser.BeginSection("LCD");
			ser.Sync(nameof(_frameBuffer), ref _frameBuffer, false);
			ser.Sync(nameof(_penBuffer), ref _penBuffer, false);
			ser.Sync(nameof(_hPos), ref _hPos);
			ser.Sync(nameof(_vPos), ref _vPos);
			ser.Sync(nameof(DisplayEnable), ref DisplayEnable);
			ser.EndSection();
		}
	}
}
