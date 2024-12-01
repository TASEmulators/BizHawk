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
	/// - https://cpcrulez.fr/coding_grimware-the_mighty_crtc_6845.htm
	/// - https://www.monitortests.com/blog/timing-parameters-explained/
	/// </summary>
	public class CRTScreen : IVideoProvider
	{
		/// <summary>
		/// The type of monitor to emulate
		/// </summary>
		public ScreenType ScreenType { get; }

		/// <summary>
		/// Checked and reset in the emulator loop. If true, framend processing happens
		/// </summary>
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
		/// Arbirary processing buffer width
		/// </summary>
		private const int FRAMEBUFFER_MAX_WIDTH = 1024;	

		/// <summary>
		/// Total scanlines per frame
		/// This is:
		/// - 625 in interlaced mode (25Hz)
		/// - 312 in non-interlaced mode (50.08Hz)
		/// </summary>
		private const int TOTAL_LINES = 625;

		/// <summary>
		/// The number or pixels being rendered every microsecond
		/// </summary>
		private const int PIXEL_TIME = 16;

		public CRTScreen(ScreenType screenType, AmstradCPC.BorderType bordertype)
		{
			ScreenType = screenType;

			TrimLeft = 121;
			TrimTop = 39;
			TrimRight = 87;
			TrimBottom = 10;

			switch (bordertype)
			{
				case AmstradCPC.BorderType.Visible:
					TrimLeft += 25;
					TrimTop += 45;
					TrimRight += 40;
					TrimBottom += 35;
					break;
			}
		}

		/// <summary>
		/// X position of the electron gun (inluding sync areas)
		/// </summary>
		private int _gunPosH;

		/// <summary>
		/// Position in frame time on the X-axis
		/// </summary>
		private int _timePosH;

		/// <summary>
		/// Y position of the electron gun (inluding sync areas)
		/// </summary>
		private int _gunPosV;

		/// <summary>
		/// Position in frame time on the V-Axis
		/// </summary>
		private int _timePosV;

		private int _verticalTiming;
		private bool _isVsync;				
		private bool _frameEndPending;

		/// <summary>
		/// Should be called at the pixel clock rate (in the case of the Amstrad CPC, 16MHz)
		/// </summary>
		public void VideoClock(int colour, int field, bool cHsync = false, bool cVsync = false)
		{
			// Beam moves continuously downwards at 50 Hz and left to right with a HSYNC pulse every 15625Hz
			// Horizontal and vertical movement are independent. 

			// PAL Horizontal:
			// - Beam moves right at a rate of 16 pixels per usec
			// - As soon as an HSYNC signal is detected, the PLL capacitor discharges and the beam moves back to the left
			// - If an HSYNC is not detected in time, the PLL will eventually move the beam back to the left
			// - https://uzebox.org/forums/viewtopic.php?t=11062 suggests that the horizontal line time if this happens is somewhere around 14000Hz (71.43 usec)
			//
			// PAL Vertical
			// - Beam moves down at a rate of 1 line per 64 microseconds (although this movement covers two scanlines)
			// - As soon as a VSYNC signal is detected, the PLL capacitor discharges and the beam moves back to the top (at the same horizontal position that VSYNC started)
			// - If a VSYNC is not detected in time, the PLL will eventually move the beam back to the top
			// - Unsure what the vertical screen time for this action is. 

			int hHold = 118;	// pixels - approx (71.43 - 64) * 16
			int vHold = 3;      // scanlines
											

			// * horizontal movement *
			// gun moves left to right at a rate of 16 pixels per usec
			_timePosH++;
			_verticalTiming++;

			if (cHsync)
			{
				// hsync signal detected - beam is moved to the left and held there until the signal is released
				_gunPosH = 0;
				_timePosH = 0;
			}
			else if (_timePosH == TOTAL_PIXELS + hHold)
			{
				// hsync signal not detected in time - PLL forces the beam back to the left
				_gunPosH = 0;
				_timePosH = 0;
			}
			else
			{
				_gunPosH++;

				if (_gunPosH >= FRAMEBUFFER_MAX_WIDTH)
				{
					// gun is at the rightmost position of the screen and is being held there
					_gunPosH = FRAMEBUFFER_MAX_WIDTH - 1;
				}
			}

			// * vertical movement *
			// gun moves downwards at a rate of 1 line per 64 microseconds - 1024 pixel times (although this movement covers two scanlines)
			if (!cVsync && _verticalTiming % TOTAL_PIXELS == 0)
			{
				// gun moves down
				_gunPosV += 2;
				_verticalTiming = 0;

				if (_gunPosV >= TOTAL_LINES - 1)
				{
					// gun is at the bottom of the screen and is being held there
					_gunPosV = TOTAL_LINES - 1;
				}
			}

			if (cVsync && !_isVsync)
			{
				// initial vsync signal detected - beam is moved back to the top of the screen and held there until the signal is released
				_gunPosV = 0;
				_isVsync = true;
				FrameEnd = true;
				HackHPos();
			}
			else if (cVsync)
			{
				// vsync signal ongoing - beam is held at the top of the screen
				_gunPosV = 0;
				//HackHPos();
			}
			else if (_gunPosV == TOTAL_LINES + vHold)
			{
				// vsync signal not detected in time - PLL forces the beam back to the top
				_gunPosV = 0;
				_isVsync = false;
				FrameEnd = true;
				HackHPos();
			}
			else
			{
				// no vsync
				_isVsync = false;
			}
			
			void HackHPos()
			{
				// the field length is actually 312.5 scanlines, so HSYNC happens half way through a scanline
				// this is for interlace mode and the first field starts half a scanline later
				// the second field should start at the same time as HSYNC
				// for now, we'll reset _verticalTiming to 0 when vertical flyback happens just so the screen isnt broken half way
				// and deal with interlacing later
				if (_verticalTiming == 665)
				{
					_verticalTiming = 0;
				}
			}

			// video output
			if (!cHsync && !cVsync)
			{
				int currPos = (_gunPosV * FRAMEBUFFER_MAX_WIDTH) + _gunPosH;
				int nextPos = ((_gunPosV + 1) * FRAMEBUFFER_MAX_WIDTH) + _gunPosH;
				_frameBuffer[currPos] = colour;

				if (nextPos < TOTAL_LINES * FRAMEBUFFER_MAX_WIDTH)
					_frameBuffer[nextPos] = colour;
			}
		}

		public void Reset()
		{
			_gunPosH = 0;
			_gunPosV = 0;
		}

		private int HDisplayable => FRAMEBUFFER_MAX_WIDTH - TrimLeft - TrimRight;
		private int VDisplayable => TOTAL_LINES - TrimTop - TrimBottom;

		private readonly int TrimLeft;
		private readonly int TrimRight;
		private readonly int TrimTop;
		private readonly int TrimBottom;

		/// <summary>
		/// Working buffer that encapsulates the entire PAL frame time
		/// </summary>
		private readonly int[] _frameBuffer = new int[FRAMEBUFFER_MAX_WIDTH * TOTAL_LINES];


		public int BackgroundColor => 0;
		public int VsyncNumerator => 16_000_000;        // pixel clock
		public int VsyncDenominator => 319_488;         // 1024 * 312

		public int BufferWidth => HDisplayable;
		public int BufferHeight => VDisplayable;
		public int VirtualWidth => HDisplayable;
		public int VirtualHeight => (int)(VDisplayable * GetVerticalModifier(HDisplayable, VDisplayable, 4.0 / 3.0));


		public int[] GetVideoBuffer() => ClampBuffer(_frameBuffer, FRAMEBUFFER_MAX_WIDTH, TOTAL_LINES, TrimLeft, TrimTop, TrimRight, TrimBottom);

		private static int[] ClampBuffer(int[] buffer, int originalWidth, int originalHeight, int trimLeft, int trimTop, int trimRight, int trimBottom)
		{
			int newWidth = originalWidth - trimLeft - trimRight;
			int newHeight = originalHeight - trimTop - trimBottom;
			int[] newBuffer = new int[newWidth * newHeight];

			for (int y = 0; y < newHeight; y++)
			{
				for (int x = 0; x < newWidth; x++)
				{
					int originalIndex = (y + trimTop) * originalWidth + (x + trimLeft);
					int newIndex = y * newWidth + x;
					newBuffer[newIndex] = buffer[originalIndex];
				}
			}

			return newBuffer;
		}

		private static double GetVerticalModifier(int bufferWidth, int bufferHeight, double targetAspectRatio)
		{
			double currentAspectRatio = (double)bufferWidth / bufferHeight;
			double verticalModifier = currentAspectRatio / targetAspectRatio;
			return verticalModifier;
		}


		public void SyncState(Serializer ser)
		{
			ser.BeginSection("CRTScreen");
			ser.Sync(nameof(_gunPosH), ref _gunPosH);
			ser.Sync(nameof(_timePosH), ref _timePosH);
			ser.Sync(nameof(_gunPosV), ref _gunPosV);
			ser.Sync(nameof(_timePosV), ref _timePosV);
			ser.Sync(nameof(_isVsync), ref _isVsync);
			ser.Sync(nameof(_frameEndPending), ref _frameEndPending);
			ser.Sync(nameof(_verticalTiming), ref _verticalTiming);
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
		public bool C_VSYNC { get; set; }
	}
}
