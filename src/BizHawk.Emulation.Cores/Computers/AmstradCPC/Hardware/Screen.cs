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
	/// - https://uzebox.org/forums/viewtopic.php?t=11062
	/// - https://www.cpcwiki.eu/forum/emulators/wish-60hz60fps-support-in-emulators-thought-on-emulating-a-crt/
	/// - https://www.batsocks.co.uk/readme/video_timing.htm
	/// - https://web.archive.org/web/20170202185019/https://www.retroleum.co.uk/PALTVtimingandvoltages.html
	/// - https://web.archive.org/web/20131125145905/http://lipas.uwasa.fi/~f76998/video/modes/
	/// </summary>
	public class CRTScreen : IVideoProvider
	{
		/// <summary>
		/// The type of monitor to emulate
		/// </summary>
		public ScreenType ScreenType { get; private set; }

		public bool FrameEnd { get; set; }

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
		private const int DISPLAY_END_LINE = (312 - 6) * 2;

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

		public CRTScreen(ScreenType screenType, AmstradCPC.BorderType bordertype)
		{
			ScreenType = screenType;

			switch (bordertype)
			{
				case AmstradCPC.BorderType.Uncropped:
					TrimLeft = 0;
					TrimTop = 39;
					TrimRight = 210;
					TrimBottom = 11;
					break;

				case AmstradCPC.BorderType.Visible:
					TrimLeft = 0 + 22;
					TrimTop = 39 + 45;
					TrimRight = 210 + 35;
					TrimBottom = 11 + 35;
					break;
			}
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
		private int _latchedHorPixelPos;

		public void VideoClock_(int colour, int field, bool cHsync = false, bool cVsync = false)
		{
			_gunPosH++;

			// enforce horiztonal wrap-around
			_gunPosH %= TOTAL_PIXELS;

			if (_gunPosH == 0)
			{
				// end of scanline
				_gunPosV++;

				if (_gunPosV == DISPLAY_END_LINE)
				{
					// monitor VSYNC starts
					_isVsync = true;

					// latch horizonal position
					// During vertical flyback the beam traverses diagonally from bottom left, to middle right, to top left,
					// so when vsync completes, the beam is at the same horizontal position as it was when vsync started
					_latchedHorPixelPos = _gunPosH;
				}

				// enforce vertical wraparound
				_gunPosV %= TOTAL_LINES;

				if (_gunPosV == 0)
				{
					// monitor vsync is over
					_isVsync = false;

					// signal bizhawk frame end
					FrameEnd = true;

					_gunPosH = _latchedHorPixelPos;
				}
			}

			if (cVsync)
			{
				// vsync signal is being received - turn off the beam as the flyback happens
				// and the CRT will attempt to syncronise with the signal
				_isVsync = true;
				_latchedHorPixelPos = _gunPosH;
			}
			else if (cHsync)
			{
				// hsync signal is being received - turn off the beam as the flyback happens
				// and the CRT will attempt to syncronise with the signal
				_isHsync = true;
			}

			if (_isVsync && !cVsync)
			{
				// incoming vsync signal has ended
				// CRT should be vertically syncronised with the signal
				_isVsync = false;
				_gunPosV = 0;
				FrameEnd = true;
			}
			else if (_isHsync && !cHsync)
			{
				// incoming hsync signal has ended
				// CRT should be horizontally syncronised with the signal
				_isHsync = false;
			}

			if (_isVsync || _isHsync)
			{
				// crt beam is off
				colour = 0;
			}

			var currPos = (_gunPosV * TOTAL_PIXELS) + _gunPosH;
			var nextPos = ((_gunPosV + 1) * TOTAL_PIXELS) + _gunPosH;

			if (currPos < _frameBuffer.Length)
			{
				_frameBuffer[currPos] = colour;
			}

			if (field == -1 && nextPos < _frameBuffer.Length)
			{
				_frameBuffer[nextPos] = colour;
			}
		}

		/// <summary>
		/// Should be called at the pixel clock rate (in the case of the Amstrad CPC, 16MHz)
		/// </summary>
		public void VideoClock(CPCColourData v, int field)
		{
			var colour = 0;

			if (++_gunPosH == LINE_PERIOD * PIXEL_TIME)
			{
				_gunPosH = 0;

				if (++_gunPosV == TOTAL_LINES)
				{
					_gunPosV = 0;
				}
				else
				{
					switch (field)
					{
						case -1:
							_gunPosV += 1;
							break;
						case 1:
							_gunPosV += field;
							break;

						default:
							break;
					}

					//_gunPosV += field;
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

			if (!_isVsync && v.C_VSYNC)
			{
				// vsync is just starting
				_isVsync = true;
			}
			else if (_isVsync && !v.C_VSYNC)
			{
				// vsync ends
				_isVsync = false;

				_gunPosH = 0;
				_gunPosV = 0;

				FrameEnd = true;
			}			

			var currPos = (_gunPosV * TOTAL_PIXELS) + _gunPosH;
			var nextPos = ((_gunPosV + 1) * TOTAL_PIXELS) + _gunPosH;


			if (v.C_VSYNC || v.C_HSYNC)
			{
				// crt beam is off
				colour = 0;
			}
			else
			{
				colour = v.ARGB;
			}

			if (currPos < _frameBuffer.Length)
			{
				_frameBuffer[currPos] = colour;
			}

			if (field == -1 && nextPos < _frameBuffer.Length)
			{
				_frameBuffer[nextPos] = colour;
			}
		}

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
					switch (field)
					{
						case -1:
							_gunPosV += 1;
							break;
						case 1:
							_gunPosV += field;
							break;

						default:
							break;
					}

					//_gunPosV += field;
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

				FrameEnd = true;
			}

			if (cVsync || cHsync)
			{
				// crt beam is off
				colour = 0;
			}

			var currPos = (_gunPosV * TOTAL_PIXELS) + _gunPosH;
			var nextPos = ((_gunPosV + 1) * TOTAL_PIXELS) + _gunPosH;

			if (currPos < _frameBuffer.Length)
			{
				_frameBuffer[currPos] = colour;
			}

			if (field == -1 && nextPos < _frameBuffer.Length)
			{
				_frameBuffer[nextPos] = colour;
			}
		}

		public void Reset()
		{
			_gunPosH = 0;
			_gunPosV = 0;
		}

		private int HDisplayable => TOTAL_PIXELS - TrimLeft - TrimRight;
		private int VDisplayable => TOTAL_LINES - TrimTop - TrimBottom;

		private int TrimLeft;
		private int TrimRight;
		private int TrimTop;
		private int TrimBottom;

		/// <summary>
		/// Working buffer that encapsulates the entire PAL frame time
		/// </summary>
		private int[] _frameBuffer = new int[LINE_PERIOD * PIXEL_TIME * TOTAL_LINES];


		public int BackgroundColor => 0;
		public int VsyncNumerator => 16_000_000;        // pixel clock
		public int VsyncDenominator => 319_488;         // 1024 * 312

		public int BufferWidth => HDisplayable;
		public int BufferHeight => VDisplayable;
		public int VirtualWidth => HDisplayable;
		public int VirtualHeight => (int)(VDisplayable * GetVerticalModifier(HDisplayable, VDisplayable, 4.0 / 3.0));

		


		public int[] GetVideoBuffer() => ClampBuffer(_frameBuffer, TOTAL_PIXELS, TOTAL_LINES, TrimLeft, TrimTop, TrimRight, TrimBottom);

		private static int[] ClampBuffer(int[] buffer, int originalWidth, int originalHeight, int trimLeft, int trimTop, int trimRight, int trimBottom)
		{
			var newWidth = originalWidth - trimLeft - trimRight;
			var newHeight = originalHeight - trimTop - trimBottom;
			var newBuffer = new int[newWidth * newHeight];

			for (var y = 0; y < newHeight; y++)
			{
				for (var x = 0; x < newWidth; x++)
				{
					var originalIndex = (y + trimTop) * originalWidth + (x + trimLeft);
					var newIndex = y * newWidth + x;
					newBuffer[newIndex] = buffer[originalIndex];
				}
			}

			return newBuffer;
		}

		private static double GetVerticalModifier(int bufferWidth, int bufferHeight, double targetAspectRatio)
		{
			var currentAspectRatio = (double)bufferWidth / bufferHeight;
			var verticalModifier = currentAspectRatio / targetAspectRatio;
			return verticalModifier;
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


	public class VideoData
	{
		public int R { get; set; }
		public int G { get; set; }
		public int B { get; set; }
		public bool C_HSYNC { get; set; }
		public bool c_VSYNC { get; set; }
	}
}
