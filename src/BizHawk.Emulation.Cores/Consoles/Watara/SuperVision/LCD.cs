
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

		public static readonly int[] Palette_AM =
		[
			Colors.ARGB(0xFC, 0xFE, 0x00), // OFF
			Colors.ARGB(0xA8, 0x66, 0x00), // 1/3 DARKNESS
			Colors.ARGB(0x54, 0x33, 0x00), // 2/3 DARKNESS
			Colors.ARGB(0x00, 0x00, 0x00), // FULL DARK
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
				case SuperVision.ScreenType.BlackAndWhite:
				default:
					_palette = Palette_BW;
					break;
				case SuperVision.ScreenType.Amber:
					_palette = Palette_AM;
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
		/// Pulsed at the end of a field
		/// Clears the column and row shift registers
		/// </summary>
		public void FrameLatch() => ResetPosition();

		/// <summary>
		/// When pulsed, data from the column shift register latched into the current LCD glass column
		/// The row shift register is then clocked
		/// </summary>
		public void LineLatch()
		{
			_vPos++;
			_hPos = 0;
		}

		/// <summary>
		/// The frame polarity/bright control signal is toggled every field
		/// This inverts all the driver signals
		/// It also darkens the display a bit so you can get a true 2 bits per pixel
		/// The polarity toggling is done to prevent destruction of the LCD display glass via electrolytic plating action
		/// </summary>
		public bool FramePolarity
		{
			get { return _framePolarity; }
			set { _framePolarity = value; }
		}
		private bool _framePolarity;


		/// <summary>
		/// Bit offset when writing to the framebuffer
		/// </summary>
		private int _polarityOffset => FramePolarity ? 1 : 0;

		/// <summary>
		/// It takes 6 cycles for each write to the LCD screen (so 1 pixelclock == 6 cpu cycles)
		/// </summary>
		public void PixelClock(byte data)
		{
			for (int i = 0; i < 4; i++)
			{
				if (_hPos < PEN_BUFFER_WIDTH && _vPos < PEN_BUFFER_HEIGHT)
				{
					_penBuffer[(_vPos * 160 * 2) + (_hPos + _polarityOffset)] = (data >> i) & 0x01;
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

		public int VirtualWidth => BufferWidth;
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
			ser.Sync(nameof(FramePolarity), ref _framePolarity);
			ser.EndSection();
		}
	}
}
