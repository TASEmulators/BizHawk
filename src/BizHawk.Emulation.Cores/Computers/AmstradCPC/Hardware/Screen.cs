using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	/// <summary>
	/// Basic emulation of a PAL Amstrad CPC CRT screen
	/// 
	/// Decoding of C-SYNC pulses are not emulated properly, 
	/// we just assume that so long as the C-VSYNC and C-HSYNC periods are correct, the screen will display.
	/// 
	/// References used:
	/// - https://www.cpcwiki.eu/index.php/GT64/GT65
	/// - https://www.cpcwiki.eu/index.php/CTM640/CTM644
	/// - https://martin.hinner.info/vga/pal.html
	/// </summary>
	public class CRTScreen : IVideoProvider
	{
		/// <summary>
		/// The type of monitor to emulate
		/// </summary>
		public ScreenType ScreenType { get; private set; }

		/// <summary>
		/// Total line period in microseconds
		/// </summary>
		private const int LINE_PERIOD = 64;

		/// <summary>
		/// Total horizontal time in pixels
		/// </summary>
		private const int TOTAL_PIXELS = LINE_PERIOD * PIXEL_TIME;

		/// <summary>
		/// The first 4 microseconds of a scan line are taken up by the horizontal sync signal (a low pulse)
		/// </summary>
		private const int HSYNC_PERIOD = 4;

		/// <summary>
		/// The second 8 microseconds of what is known as the "back porch" (this holds colour burst data for composite signals)
		/// </summary>
		private const int BACK_PORCH_PERIOD = 8;		

		/// <summary>
		/// Total scanlines per frame
		/// This is:
		/// - 625 in interlaced mode (25Hz)
		/// - 312 in non-interlaced mode (50.08Hz)
		/// </summary>
		private const int TOTAL_LINES = 625;

		/// <summary>
		/// The scanline on which VSYNC has ended and display can start
		/// </summary>
		private const int DISPLAY_START_LINE = 6 * 2;

		/// <summary>
		/// The scanline on which VSYNC starts
		/// </summary>
		private const int DISPLAY_END_LINE = 312 * 2;

		/// <summary>
		/// The number or pixels being rendered every microsecond
		/// </summary>
		private const int PIXEL_TIME = 16;

		/// <summary>
		/// Display buffer width in pixels
		/// </summary>
		private const int VISIBLE_PIXEL_WIDTH = (LINE_PERIOD * PIXEL_TIME) - (HSYNC_PERIOD * PIXEL_TIME) - (BACK_PORCH_PERIOD * PIXEL_TIME);

		/// <summary>
		/// Display buffer height in pixels
		/// </summary>
		private const int VISIBLE_PIXEL_HEIGHT = TOTAL_LINES - DISPLAY_START_LINE;

		public CRTScreen(ScreenType screenType)
		{
			ScreenType = screenType;
		}

		/// <summary>
		/// X position of the electron gun (inluding sync areas)
		/// </summary>
		private int _gunPosH;

		/// <summary>
		/// Y position of the electron gun (inluding sync areas)
		/// </summary>
		private int _gunPosV;

		private bool _isVsync;
		private bool _isHsync;
		

		/// <summary>
		/// Should be called at the pixel clock rate (in the case of the Amstrad CPC, 16MHz)
		/// </summary>
		public void VideoClock(int colour, int field, bool cHsync = false, bool cVsync = false)
		{
			if (++_gunPosH == LINE_PERIOD * PIXEL_TIME)
			{
				_gunPosH = 0;

				if (++_gunPosV == TOTAL_LINES)
				{
					_gunPosV = 0;
				}
				else
				{
					_gunPosV += field;
					/*
					if (field == 0)
					{
						_gunPosV += 2;
					}
					else
					{
						_gunPosV++;
					}
					*/
				}
			}
			/*
			else
			{
				_gunPosH++;
			}
			*/

			if (!_isVsync && cVsync)
			{
				// vsync is just starting
				_isVsync = true;
			}
			else if (_isVsync && !cVsync)
			{
				// vsync ends
				_isVsync = false;

				_gunPosH = 0;
				_gunPosV = 0;
			}

			if (cVsync || cHsync)
			{
				// crt beam is off
				_frameBuffer[(_gunPosV * TOTAL_PIXELS) + _gunPosH] = 0;
			}
			else
			{
				// beam should be painting
				_frameBuffer[(_gunPosV * TOTAL_PIXELS) + _gunPosH] = colour;
			}
		}

		public void Reset()
		{
			_gunPosH = 0;
			_gunPosV = 0;
		}


		public int BackgroundColor => 0;
		public int VsyncNumerator => 16_000_000;        // pixel clock
		public int VsyncDenominator => 319_488;         // 1024 * 312

		public int BufferWidth => TOTAL_PIXELS; // VISIBLE_PIXEL_WIDTH;
		public int BufferHeight => TOTAL_LINES; // VISIBLE_PIXEL_HEIGHT;
		public int VirtualWidth => BufferWidth;
		public int VirtualHeight => BufferHeight;

		/// <summary>
		/// Working buffer that encapsulates the entire frame time
		/// </summary>
		private int[] _frameBuffer = new int[LINE_PERIOD * PIXEL_TIME * TOTAL_LINES];

		/// <summary>
		/// Output buffer sent to the video renderer
		/// </summary>
		private int[] _outputBuff = new int[VISIBLE_PIXEL_WIDTH * VISIBLE_PIXEL_HEIGHT];

		public int[] GetVideoBuffer()
		{
			return _frameBuffer;

			for (int y = 0; y < VISIBLE_PIXEL_HEIGHT; y++)
			{
				int sourceIndex = (DISPLAY_START_LINE + y) * LINE_PERIOD * PIXEL_TIME + HSYNC_PERIOD + BACK_PORCH_PERIOD;
				int destIndex = y * VISIBLE_PIXEL_WIDTH;
				Array.Copy(_frameBuffer, sourceIndex, _outputBuff, destIndex, VISIBLE_PIXEL_WIDTH);
			}

			return _outputBuff;
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("CRTScreen");
			ser.Sync(nameof(_gunPosH), ref _gunPosH);
			ser.Sync(nameof(_gunPosV), ref _gunPosV);
			ser.Sync(nameof(_isVsync), ref _isVsync);
			ser.Sync(nameof(_isHsync), ref _isHsync);
			ser.EndSection();
		}
	}

	/// <summary>
	/// The type of Amstrad CRT to emulate
	/// </summary>
	public enum ScreenType
	{
		/// <summary>
		/// CTM640/CTM644
		/// Amstrad colour monitors
		/// </summary>
		CTM064x,

		/// <summary>
		/// GT64/GT65/GT65-2
		/// Amstrad green screen monitors
		/// </summary>
		GT6x
	}
}
