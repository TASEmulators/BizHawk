
using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.Z80A;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	/// <summary>
	/// * Amstrad Gate Array *
	/// http://www.cpcwiki.eu/index.php/Gate_Array
	/// https://web.archive.org/web/20170612081209/http://www.grimware.org/doku.php/documentations/devices/gatearray
	/// http://bread80.com/2021/06/03/understanding-the-amstrad-cpc-video-ram-and-gate-array-subsystem/
	/// </summary>
	public class GateArray : IPortIODevice, IVideoProvider
	{
		private readonly CPCBase _machine;
		private Z80A<AmstradCPC.CpuLink> CPU => _machine.CPU;
		private CRTC CRTC => _machine.CRTC;
		private IPSG PSG => _machine.AYDevice;
		private ushort BUSRQ => CPU.MEMRQ[CPU.bus_pntr];
		private GateArrayType GateArrayType;

		/// <summary>
		/// Length of a GA frame in 1MHz clock cycles
		/// </summary>
		public int FrameLength => (MAX_SCREEN_WIDTH_PIXELS * TOTAL_DISPLAY_SCANLINES) / 16;

		/// <summary>
		/// Clock speed of the Z80 in Hz
		/// </summary>
		public double Z80ClockSpeed => 4_000_000;

		/// <summary>
		/// The current GA clock count within the current frame
		/// Set to -1 at the start of a new frame
		/// </summary>
		public int GAClockCounter
		{
			get { return _GAClockCounter; }
			set { _GAClockCounter = value; }
		}
		private int _GAClockCounter;


		/// <summary>
		/// Previous frame clock count. Latched at the end of the frame (VSYNC off)
		/// </summary>
		public int LastGAFrameClocks
		{
			get { return _lastGAFrameClocks; }
			set { _lastGAFrameClocks = value; }
		}
		private int _lastGAFrameClocks;


		/// <summary>
		/// 0-15:   Pen Registers
		/// 16:     Border Colour
		/// </summary>
		private int[] _colourRegisters = new int[17];

		/// <summary>
		/// The currently selected Pen
		/// </summary>
		private int _currentPen;

		/// <summary>
		/// The standard CPC Pallete (ordered by firmware #)
		/// http://www.cpcwiki.eu/index.php/CPC_Palette
		/// </summary>
		private static readonly int[] CPCFirmwarePalette =
		{
			Colors.ARGB(0x00, 0x00, 0x00), // Black
            Colors.ARGB(0x00, 0x00, 0x80), // Blue
            Colors.ARGB(0x00, 0x00, 0xFF), // Bright Blue
            Colors.ARGB(0x80, 0x00, 0x00), // Red            
            Colors.ARGB(0x80, 0x00, 0x80), // Magenta
            Colors.ARGB(0x80, 0x00, 0xFF), // Mauve
            Colors.ARGB(0xFF, 0x00, 0x00), // Bright Red
            Colors.ARGB(0xFF, 0x00, 0x80), // Purple
            Colors.ARGB(0xFF, 0x00, 0xFF), // Bright Magenta
            Colors.ARGB(0x00, 0x80, 0x00), // Green
            Colors.ARGB(0x00, 0x80, 0x80), // Cyan
            Colors.ARGB(0x00, 0x80, 0xFF), // Sky Blue
            Colors.ARGB(0x80, 0x80, 0x00), // Yellow
            Colors.ARGB(0x80, 0x80, 0x80), // White
            Colors.ARGB(0x80, 0x80, 0xFF), // Pastel Blue
            Colors.ARGB(0xFF, 0x80, 0x00), // Orange
            Colors.ARGB(0xFF, 0x80, 0x80), // Pink
            Colors.ARGB(0xFF, 0x80, 0xFF), // Pastel Magenta
            Colors.ARGB(0x00, 0xFF, 0x00), // Bright Green
            Colors.ARGB(0x00, 0xFF, 0x80), // Sea Green
            Colors.ARGB(0x00, 0xFF, 0xFF), // Bright Cyan
            Colors.ARGB(0x80, 0xFF, 0x00), // Lime
            Colors.ARGB(0x80, 0xFF, 0x80), // Pastel Green
            Colors.ARGB(0x80, 0xFF, 0xFF), // Pastel Cyan
            Colors.ARGB(0xFF, 0xFF, 0x00), // Bright Yellow
            Colors.ARGB(0xFF, 0xFF, 0x80), // Pastel Yellow
            Colors.ARGB(0xFF, 0xFF, 0xFF), // Bright White
        };

		/// <summary>
		/// The standard CPC Pallete (ordered by hardware #)
		/// http://www.cpcwiki.eu/index.php/CPC_Palette
		/// </summary>
		private static readonly int[] CPCHardwarePalette =
		{
			Colors.ARGB(0x80, 0x80, 0x80), // White
            Colors.ARGB(0x80, 0x80, 0x80), // White (duplicate)
            Colors.ARGB(0x00, 0xFF, 0x80), // Sea Green
            Colors.ARGB(0xFF, 0xFF, 0x80), // Pastel Yellow
            Colors.ARGB(0x00, 0x00, 0x80), // Blue
            Colors.ARGB(0xFF, 0x00, 0x80), // Purple
            Colors.ARGB(0x00, 0x80, 0x80), // Cyan
            Colors.ARGB(0xFF, 0x80, 0x80), // Pink
            Colors.ARGB(0xFF, 0x00, 0x80), // Purple (duplicate)
            Colors.ARGB(0xFF, 0xFF, 0x80), // Pastel Yellow (duplicate)
            Colors.ARGB(0xFF, 0xFF, 0x00), // Bright Yellow
            Colors.ARGB(0xFF, 0xFF, 0xFF), // Bright White
            Colors.ARGB(0xFF, 0x00, 0x00), // Bright Red
            Colors.ARGB(0xFF, 0x00, 0xFF), // Bright Magenta
            Colors.ARGB(0xFF, 0x80, 0x00), // Orange
            Colors.ARGB(0xFF, 0x80, 0xFF), // Pastel Magenta
            Colors.ARGB(0x00, 0x00, 0x80), // Blue (duplicate)
            Colors.ARGB(0x00, 0xFF, 0x80), // Sea Green (duplicate)
            Colors.ARGB(0x00, 0xFF, 0x00), // Bright Green
            Colors.ARGB(0x00, 0xFF, 0xFF), // Bright Cyan
            Colors.ARGB(0x00, 0x00, 0x00), // Black
            Colors.ARGB(0x00, 0x00, 0xFF), // Bright Blue
            Colors.ARGB(0x00, 0x80, 0x00), // Green
            Colors.ARGB(0x00, 0x80, 0xFF), // Sky Blue
            Colors.ARGB(0x80, 0x00, 0x80), // Magenta
            Colors.ARGB(0x80, 0xFF, 0x80), // Pastel Green
            Colors.ARGB(0x80, 0xFF, 0x00), // Lime
            Colors.ARGB(0x80, 0xFF, 0xFF), // Pastel Cyan
            Colors.ARGB(0x80, 0x00, 0x00), // Red     
            Colors.ARGB(0x80, 0x00, 0xFF), // Mauve
            Colors.ARGB(0x80, 0x80, 0x00), // Yellow            
            Colors.ARGB(0x80, 0x80, 0xFF), // Pastel Blue
        };

		/// <summary>
		/// 4bit Screen Mode Value
		/// - Mode 0, 160x200 resolution, 16 colours
		/// - Mode 1, 320x200 resolution, 4 colours
		/// - Mode 2, 640x200 resolution, 2 colours
		/// - Mode 3, 160x200 resolution, 4 colours (undocumented)
		/// 
		/// When screenmode is updated it will take effect after the next HSync
		/// </summary>
		private byte _screenMode;

		/// <summary>
		/// PENR (register 0) - Pen Selection
		/// This register can be used to select one of the 17 color-registers (pen 0 to 15 or the border). 
		/// It will remain selected until another PENR command is executed.
		/// PENR	    Index	
		/// 7	6	5	4	3	2	1	0	color register selected
		/// 0	0	0	0	n   n   n   n   pen n from 0 to 15 (4bits)
		/// 0	0	0	1	x   x   x   x   border
		/// 
		/// x can be 0 or 1, it doesn't matter
		/// </summary>     
		public byte PENR
		{
			get => _PENR;
			set
			{
				_PENR = value;
				if (_PENR.Bit(4))
				{
					// border select
					_currentPen = 16;
				}
				else if (!_PENR.Bit(6) && !_PENR.Bit(7))
				{
					// pen select
					_currentPen = _PENR & 0x0f;
				}
				else
				{
					// invalid?	
					// TODO: check what happens here				
				}
			}
		}
		private byte _PENR;

		/// <summary>
		/// INKR (register 1) - Colour Selection
		/// This register takes a 5bits parameter which is a color-code. This color-code range from 0 to 31 but there's only 27 differents colors 
		/// (because the Gate Array use a 3-states logic on the R,G and B signals, thus 3x3x3=27).
		/// INKR	    Color	
		/// 7	6	5	4	3	2	1	0	
		/// 0	1	0	n   n   n   n   n   where n is a color code (5 bits)
		/// 
		/// The PEN affected by the INKR command is updated (almost) immediatly
		/// </summary>     
		public byte INKR
		{
			get => _INKR;
			set
			{
				_INKR = value;
				_colourRegisters[_currentPen] = _INKR & 0x1f;
			}
		}
		private byte _INKR;

		/// <summary>
		/// RMR (register 2) - Select screen mode and ROM configuration
		/// This register control the interrupt counter (reset), the upper and lower ROM paging and the video mode.
		/// RMR	        Commands
		/// 7	6	5	4	3	2	1	0
		/// 1	0	0	I   UR  LR  VM-->
		/// 
		/// I   : if set (1), this will reset the interrupt counter
		/// UR  : Enable (0) or Disable (1) the upper ROM paging (&amp;C000 to &amp;FFFF). You can select which upper ROM with the I/O address &amp;DF00
		/// LR  : Enable (0) or Disable (1) the lower ROM paging
		/// VM  : Select the video mode 0,1,2 or 3 (it will take effect after the next HSync)
		/// </summary>		
		public byte RMR
		{
			get => _RMR;
			set
			{
				_RMR = value;

				// Upper ROM paging
				if (_RMR.Bit(3))
					_machine.UpperROMPaged = false;
				else
					_machine.UpperROMPaged = true;

				// Lower ROM paging
				if (_RMR.Bit(2))
					_machine.LowerROMPaged = false;
				else
					_machine.LowerROMPaged = true;

				// Interrupt generation control
				if (_RMR.Bit(4))
				{
					// reset interrupt counter
					InterruptCounter = 0;
				}
			}
		}
		private byte _RMR;

		/// <summary>
		/// RAMR (register 3) - RAM Banking
		/// This register exists only in CPCs with 128K RAM (like the CPC 6128, or CPCs with Standard Memory Expansions)
		/// Note: In the CPC 6128, the register is a separate PAL that assists the Gate Array chip
		/// 
		/// Bit	Value	Function
		/// 7	1	    Gate Array function 3
		/// 6	1
		/// 5	b	    64K bank number(0..7); always 0 on an unexpanded CPC6128, 0-7 on Standard Memory Expansions
		/// 4	b
		/// 3	b
		/// 2	x       RAM Config(0..7)
		/// 1	x       ""  
		/// 0	x       ""
		/// 
		/// The 3bit RAM Config value is used to access the second 64K of the total 128K RAM that is built into the CPC 6128 or the additional 64K-512K of standard memory expansions. 
		/// These contain up to eight 64K ram banks, which are selected with bit 3-5. A standard CPC 6128 only contains bank 0. Normally the register is set to 0, so that only the 
		/// first 64K RAM are used (identical to the CPC 464 and 664 models). The register can be used to select between the following eight predefined configurations only:
		/// 
		/// -Address-   0       1       2       3       4       5       6       7
		/// 0000-3FFF   RAM_0   RAM_0   RAM_4   RAM_0   RAM_0   RAM_0   RAM_0   RAM_0
		/// 4000-7FFF   RAM_1   RAM_1   RAM_5   RAM_3   RAM_4   RAM_5   RAM_6   RAM_7
		/// 8000-BFFF   RAM_2   RAM_2   RAM_6   RAM_2   RAM_2   RAM_2   RAM_2   RAM_2
		/// C000-FFFF   RAM_3   RAM_7   RAM_7   RAM_7   RAM_3   RAM_3   RAM_3   RAM_3
		/// 
		/// The Video RAM is always located in the first 64K, VRAM is in no way affected by this register
		/// </summary>
		private byte _RAMR;
		/// <summary>
		/// This is actually implemented outside of here. These values do nothing.
		/// </summary>
		public byte RAMR
		{
			get => _RAMR;
			set => _RAMR = value;
		}

		/// <summary>
		/// Simulates the internal 6bit INT counter (R52 - R is for Raster)
		/// This is incremented at the end of every HSYNC signal from the CRTC
		/// On all CRTCs, R52 interrupts always start 1µs after the end of an HSYNC
		/// But on CRTCs 3/4, HSYNCs occur 1µs later than on CRTCs 0/1/2. Which means that on CRTCs 3/4,
		/// interrupts start 1µs later than on CRTCs 0/1/2.
		/// </summary>		
		private int InterruptCounter
		{
			get => _interruptCounter & 0x3F;
			set
			{
				_interruptCounter = value & 0x3F;

				// When the counter equals "52", it is cleared to "0" and the Gate-Array will issue a interrupt request to the Z80,
				// the interrupt request remains active for 1.4us (22.4 gate array cycles)
				if (_interruptCounter >= 52)
				{
					_interruptCounter = 0;

					// interrupt should be raised
					CPU.FlagI = true;
					_holdingInterrupt = true;
					_interruptHoldCounter = 0;
				}
			}
		}
		private int _interruptCounter;

		/// <summary>
		/// Signals that an interrupt is being held
		/// </summary>
		private bool _holdingInterrupt;

		/// <summary>
		/// Used to count the cycles holding the interrupt
		/// </summary>
		private int _interruptHoldCounter;

		/// <summary>
		/// Set when the VSYNC signal is detected from the CRTC
		/// </summary>
		private bool GA_VSYNC;

		/// <summary>
		/// Set when the HSYNC signal is detected from the CRCT
		/// </summary>
		private bool GA_HSYNC;

		/// <summary>
		/// Counter used in determining the width of the HSYNC signal that is sent to the CRT
		/// </summary>
		private int _HSYNCWidthCounter;

		/// <summary>
		/// When true, the CSYNC (HSYNC + VSYNC) signal is being sent to the CRT
		/// </summary>
		private bool CRT_HSYNC;

		/// <summary>
		/// The gatearray counts the number of HSYNCs observed during a VSYNC
		/// </summary>
		private int HSYNCCounter;


		public GateArray(CPCBase machine, GateArrayType gateArrayType)
		{
			_machine = machine;
			GateArrayType = gateArrayType;
			CRTC.AttachHSYNCOnCallback(OnHSYNCOn);
			CRTC.AttachHSYNCOffCallback(OnHSYNCOff);
			CRTC.AttachVSYNCOnCallback(OnVSYNCOn);
			CRTC.AttachVSYNCOffCallback(OnVSYNCOff);
			Reset();
		}

		/// <summary>
		/// Called when the Z80 acknowledges an interrupt
		/// </summary>
		public void IORQA()
		{
			// bit 5 of the interrupt counter is reset so next interrupt is not closer than 32 lines
			InterruptCounter &= ~(1 << 5);
			CPU.FlagI = false;
			//interruptsPerFrame++;
		}

		/// <summary>
		/// Fired when rising edge of CRTC HSYNC signal is detected
		/// </summary>
		public void OnHSYNCOn()
		{
			GA_HSYNC = true;

			// latch screenmode
			_screenMode = (byte)(_RMR & 0x03);
		}

		/// <summary>
		/// Fired when falling edge of CRTC HSYNC signal is detected
		/// </summary>
		public void OnHSYNCOff()
		{
			GA_HSYNC = false;

			// latch screenmode
			_screenMode = (byte)(_RMR & 0x03);

			// The 6-bit counter is incremented after each HSYNC from the CRTC
			// (When standard CRTC display settings are used, this is equivalent to counting scan-lines)
			InterruptCounter++;

			if (GA_VSYNC)
			{
				// gate array is counting HSYNCs during the VSYNC period
				HSYNCCounter++;
			}

			// The Gate-Array senses the VSYNC signal. If two HSYNCs have been detected following the start of the VSYNC then there are two possible actions:
			if (GA_VSYNC && HSYNCCounter >= 2)
			{
				if (InterruptCounter.Bit(5))
				{
					// If the top bit of the 6-bit counter is set to "1" (i.e. the counter >=32), then there is no interrupt request, and the 6-bit counter is reset to "0".
					// (If a interrupt was requested and acknowledged it would be closer than 32 HSYNCs compared to the position of the previous interrupt).
					InterruptCounter = 0;
				}
				else
				{
					// If the top bit of the 6-bit counter is set to "0" (i.e. the counter <32), then a interrupt request is issued, and the 6-bit counter is reset to "0".
					CPU.FlagI = true;
					InterruptCounter = 0;
				}
			}

			// increment the vertical scanline counter
			_verScanlineCounter++;
		}

		/// <summary>
		/// Fired when rising edge of CRTC VSYNC signal is detected
		/// </summary>
		public void OnVSYNCOn()
		{
			GA_VSYNC = true;
			HSYNCCounter = 0;
			_horCharCounter = 0;

			//CRT_VSYNC_Pending = true;
		}

		/// <summary>
		/// Fired when falling edge of CRTC VSYNC signal is detected
		/// </summary>
		public void OnVSYNCOff()
		{
			GA_VSYNC = false;
			// this is effectively the start of a new frame
			// latch the current frame clock count
			LastGAFrameClocks = GAClockCounter;

			// reset the frame clock counter
			GAClockCounter = -1;

			// interrupts should be syncronised with the start of the frame now (i.e. InterruptCounter = 0)
			// CRT beam position should be at the start of the display area

			// reset counters
			_verScanlineCounter = 0;
			_horCharCounter = 0;
			InterruptCounter = 0;
		}

		/// <summary>
		/// 4bit GA clock counter
		/// </summary>
		private int _clockCounter;

		/// <summary>
		/// Temporary storage for the first video data byte read from memory
		/// </summary>
		private byte _videoDataByte1;

		/// <summary>
		/// Temporaryy storage for the second video data byte read from memory
		/// </summary>
		private byte _videoDataByte2;

		/// <summary>
		/// Two bytes of video data are read over 16 GA clocks.
		/// During the following 16 GA clocks, the video data is output to the screen
		/// </summary>
		private byte[] _videoData = new byte[2];

		/// <summary>
		/// Horizontal character counter
		/// </summary>
		private int _horCharCounter;

		/// <summary>
		/// Vertical scanline counter
		/// </summary>
		private int _verScanlineCounter;


		/// <summary>
		/// Gate array is clocked at 16MHz (which is also the pixel clock)
		/// It implements a 4-phase clock that is in charge of clocking the following devices:
		/// 
		/// * CPU:		4MHz (4 GA clocks per z80 PHI clock)
		/// * CRTC:		1MHz (16 GA clocks per CRTC clock)
		/// * PSG:		1MHz (16 GA clocks per PSG clock)
		/// 
		/// Regardless of screen mode, the GA will output a single pixel every 1 GA clock (so a 16MHz pixel clock)
		/// It constantly reads video data at 2MHz (8 GA clocks per byte)
		/// 
		/// So:
		/// - 16 GA clocks (1MHz) takes 1 microsecond, one CRTC character
		/// - 8 GA clocks (2MHz) takes 0.5 microseconds, a byte of video data is read and 8 pixels are output
		/// - So each CRTC character is 16 pixels wide
		/// </summary>
		public void Clock()
		{
			switch (_clockCounter)
			{
				// PHI Clock 1
				case 0:

					CheckInterrupt();
					CheckCRTTimings();

					// /WAIT line is inactive
					CPU.ExecuteOne();

					// clock CRTC (active low)
					CRTC.CLK = false;
					CRTC.Clock();

					// latch the video data first byte (it will be output to screen over the next 8 GA clocks)
					_videoData[0] = _videoDataByte1;

					// GA reads first byte of video data
					_videoDataByte1 = _machine.FetchScreenMemory(CRTC.MA_Address);

					break;

				// PHI Clock 2
				case 4:

					CheckInterrupt();

					// /WAIT is active
					// Z80 will sample /WAIT during the cycle it intends to access the bus when reading opcodes
					// but will sample *before* the cycle for other memory accesses
					if (BUSRQ == 1)	// PCh
					{
						// opcode fetch memory action upcoming - CPU will wait
						CPU.TotalExecutedCycles++;
					}
					else
					{
						// no fetch, or non-opcode fetch - CPU does not wait
						CPU.ExecuteOne();
					}

					break;

				// PHI Clock 3
				case 8:

					CheckInterrupt();

					if (BUSRQ > 0)
					{
						// memory action upcoming - CPU clock is halted
						CPU.TotalExecutedCycles++;
					}
					else
					{
						CPU.ExecuteOne();
					}

					// un-clock CRTC (inactive high)
					CRTC.CLK = true;

					// latch the video data first byte (it will be output to screen over the next 8 GA clocks)
					_videoData[1] = _videoDataByte2;

					// GA reads second byte of video data
					_videoDataByte2 = _machine.FetchScreenMemory(CRTC.MA_Address);

					break;

				// PHI Clock 4
				case 12:

					CheckInterrupt();

					// /WAIT is active
					if (BUSRQ > 0)
					{
						// memory action upcoming - CPU clock is halted
						CPU.TotalExecutedCycles++;
					}
					else
					{
						CPU.ExecuteOne();
					}

					break;
			}

			// output a pixel at 16MHz - each byte contains 8 pixels, 2 bytes per 16 GA clocks, 16 pixels per CRTC character
			WritePixel();

			if (_clockCounter == 0)
			{
				// start of a new character (or end of the old one if you like).
				// increment the horizontal character counter
				_horCharCounter++;
			}

			_clockCounter++;

			// enforce 4-bit wraparound
			_clockCounter &= 0x0F;			
		}

		private void CheckInterrupt()
		{
			if (_holdingInterrupt)
			{
				_interruptHoldCounter++;

				if (_interruptHoldCounter >= 6)
				{
					// 1.4us (ish) have past - stop holding the interrupt
					_holdingInterrupt = false;
					CPU.FlagI = false;
					_interruptHoldCounter = 0;
				}
			}
		}

		private void CheckCRTTimings()
		{
			// HSYNC
			if (GA_HSYNC)
			{
				_HSYNCWidthCounter++;

				if (_HSYNCWidthCounter > 3)
				{
					// CSYNC (HSYNC + VSYNC) is sent to the CRT 2µs after the start of the GA HSYNC
					CRT_HSYNC = false;
				}
				else if (_HSYNCWidthCounter < 6)
				{
					CRT_HSYNC = true;
				}
				else
				{
					// HSYNC width is 4µs wide
					CRT_HSYNC = false;
				}
			}	
		}

		/// <summary>
		/// Writes the correct pixel from the currently read video data byte to the framebuffer
		/// </summary>
		private void WritePixel()
		{
			var byteToUse = _clockCounter < 8 ? _videoData[0] : _videoData[1];
			var pixelPos = _clockCounter & 0x07;

			var hPos = (_horCharCounter * 16) + pixelPos;
			var vPos = _verScanlineCounter * 2;
			var bufferPos = (vPos * MAX_SCREEN_WIDTH_PIXELS) + hPos;

			int colour = 0;

			// https://www.cpcwiki.eu/index.php/Gate_Array#CSYNC_signal
			// The HSYNC and VSYNC signals are received from the CRTC.
			// These signals are then modified by the Gate Array to C - HSYNC and C-VSYNC and merged into a single CSYNC signal that will be
			// sent to the display.

			// When CRTC HSYNC is active, the Gate Array immediately outputs the palette colour black.
			// If the HSYNC is set to 14 characters then black will be output for 14µs.
			if (CRT_HSYNC || GA_VSYNC)
			{
				// gate array outputs true black (not affected by any luminosity settings)
				colour = Colors.ARGB(0x00, 0x00, 0x00);
			}
			else if (CRTC.DISPTMG)
			{
				// display enable is active from the CRTC
				// gate array outputs pixel colour data from RAM
				// http://www.cpcmania.com/Docs/Programming/Painting_pixels_introduction_to_video_memory.htm
				switch (_screenMode)
				{
					// Mode 0, 4-bits per pixel, 160x200 resolution, 16 colours
					// ------------------------------------------------------------------
					// Video Byte Bit:	|  7  |  6  |  5  |  4  |  3  |  2  |  1  |  0  |
					// Pixel:			|  0  |  1  |  0  |  1  |  0  |  1  |  0  |  1  |
					// Pixel Bit Enc.:	|  0  |  0  |  2  |  2  |  1  |  1  |  3  |  3  |
					// Pixel Timing:	|           0           |           1           |
					// ------------------------------------------------------------------
					case 0:

						if (pixelPos < 4)
						{
							// pixel 0
							colour = 
								((byteToUse & 0x80) >> 7) |
								((byteToUse & 0x08) >> 2) |
								((byteToUse & 0x20) >> 3) |
								((byteToUse & 0x02) << 2);
						}
						else
						{
							// pixel 1
							colour =
								((byteToUse & 0x40) >> 6) |
								((byteToUse & 0x04) >> 1) |
								((byteToUse & 0x10) >> 2) |
								((byteToUse & 0x01) << 3);
						}

						break;

					// Mode 1, 2-bits per pixel, 320x200 resolution, 4 colours
					// ------------------------------------------------------------------
					// Video Byte Bit:	|  7  |  6  |  5  |  4  |  3  |  2  |  1  |  0  |
					// Pixel:			|  0  |  1  |  2  |  3  |  0  |  1  |  2  |  3  |
					// Pixel Bit Enc.:	|  0  |  0  |  0  |  0  |  1  |  1  |  1  |  1  |
					// Pixel Timing:	|     0     |     1     |	  2     |     3     |
					// ------------------------------------------------------------------
					case 1:

						switch (pixelPos)
						{
							case 0:
							case 1:
								// pixel 0
								colour = ((byteToUse & 0x80) >> 7) | ((byteToUse & 0x08) >> 2);
								break;

							case 2:
							case 3:
								// pixel 1
								colour = ((byteToUse & 0x40) >> 6) | ((byteToUse & 0x04) >> 1);
								break;

							case 4:
							case 5:
								// pixel 2
								colour = ((byteToUse & 0x20) >> 5) | (byteToUse & 0x02);
								break;

							case 6:
							case 7:
								// pixel 3
								colour = ((byteToUse & 0x10) >> 4) | ((byteToUse & 0x01) << 1);
								break;
						}
						
						break;

					// Mode 2, 1-bit per pixel, 640x200 resolution, 2 colours
					// ------------------------------------------------------------------
					// Video Byte Bit:	|  7  |  6  |  5  |  4  |  3  |  2  |  1  |  0  |
					// Pixel:			|  0  |  1  |  2  |  3  |  4  |  5  |  6  |  7  |
					// Pixel Bit Enc.:	|  7  |  6  |  5  |  4  |  3  |  2  |  1  |  0  |
					// Pixel Timing:	|  0  |  1  |  2  |  3  |  4  |  5  |  6  |  7  |
					case 2:
						


						break;

					// Mode 3, 2-bits per pixel, 160x200 resolution, 4 colours (undocumented)
					// ------------------------------------------------------------------
					// Video Byte Bit:	|  7  |  6  |  5  |  4  |  3  |  2  |  1  |  0  |
					// Pixel:			|  0  |  1  |  x  |  x  |  0  |  1  |  x  |  x  |
					// Pixel Bit Enc.:	|  0  |  0  |  x  |  x  |  1  |  1  |  x  |  x  |
					// Pixel Timing:	|           0           |           1           |
					// ------------------------------------------------------------------
					case 3:

						if (pixelPos < 4)
						{
							// pixel 0
							colour =
								((byteToUse & 0x80) >> 7) | ((byteToUse & 0x08) >> 2);
						}
						else
						{
							// pixel 1
							colour =
								((byteToUse & 0x40) >> 6) | ((byteToUse & 0x04) >> 1);
						}

						break;
				}
			}
			else
			{
				// display enable is inactive from the CRTC
				// gate array outputs border colour
				colour = CPCHardwarePalette[_colourRegisters[16]];
			}

			_frameBuffer[bufferPos] = colour;
		}





		/// <summary>
		/// Device responds to an IN instruction
		/// </summary>
		public bool ReadPort(ushort port, ref int result)
		{
			switch (GateArrayType)
			{
				case GateArrayType.Amstrad40489:
					// CPC+ and GX4000 return 0x79 for all reads to the gate array (according to mame)
					result = 0x79;
					return true;

				default:
					// Gate array is OUT only
					return false;
			}
		}

		/// <summary>
		/// Device responds to an OUT instruction
		/// </summary>
		public bool WritePort(ushort port, int result)
		{
			var portUpper = (byte)(port >> 8);
			var portLower = (byte)(port & 0xff);

			// The gate array is selected when bit 15 of the I/O port address is set to "0" and bit 14 of the I/O port address is set to "1"
			bool accessed = false;
			if (!portUpper.Bit(7) && portUpper.Bit(6))
				accessed = true;

			if (!accessed)
				return accessed;

			var regSelect = (byte)(result >> 5);

			switch (regSelect)
			{
				case 0b_000:
				case 0b_001:
					// PENR register
					PENR = (byte)result;
					break;

				case 0b_010:
				case 0b_011:
					// INKR register
					INKR = (byte)result;
					break;

				case 0b_100:
					// RMR register
					RMR = (byte)result;
					break;

				case 0b_101:
					switch (GateArrayType)
					{
						case GateArrayType.Amstrad40007:
						case GateArrayType.Amstrad40008:
						case GateArrayType.Amstrad40010:
						case GateArrayType.Amstrad40226:
							// RMR ghost register
							RMR = (byte)result;
							break;
						
						case GateArrayType.Amstrad40489:
							// TODO: RMR2 register
							break;
					}
					break;	

				case 0b_110:
				case 0b_111:
					// RAMR register
					RAMR = (byte)result;
					break;
			}

			return true;
		}

		public void Reset()
		{

		}

		/// <summary>
		/// CPC always has a 64 character total width (including HSYNC and VSYNC)
		/// </summary>
		private const int MAX_SCR_CHA_WIDTH = 64;

		/// <summary>
		/// Maximum number of scanlines on the screen (including HSYNC and VSYNC)
		/// </summary>
		private const int MAX_SCREEN_SCANLINES = 312;

		/// <summary>
		/// 16 pixels per character, each pixel is output at 16MHz - so each CRTC character is 1 microsecond
		/// </summary>
		private const int PIXEL_WIDTH_PER_CHAR = 16;

		/// <summary>
		/// Maximum screen width in pixels
		/// </summary>
		private const int MAX_SCREEN_WIDTH_PIXELS = PIXEL_WIDTH_PER_CHAR * MAX_SCR_CHA_WIDTH;   // 1024 pixels 

		/// <summary>
		/// Beam renders 2 scanlines at a time
		/// </summary>
		private const int TOTAL_DISPLAY_SCANLINES = MAX_SCREEN_SCANLINES * 2;

		/// <summary>
		/// Initial framebuffer that the gate array will output to
		/// This will include HSYNC and VSYNC timings (which we will trim afterwards)
		/// </summary>
		private int[] _frameBuffer = new int[MAX_SCREEN_WIDTH_PIXELS * TOTAL_DISPLAY_SCANLINES];


		public int BackgroundColor => CPCHardwarePalette[1];
		public int VsyncNumerator => 16_000_000;		// pixel clock
		public int VsyncDenominator => 319_488;			// 1024 * 312

		public int BufferWidth => MAX_SCREEN_WIDTH_PIXELS;
		public int BufferHeight => TOTAL_DISPLAY_SCANLINES;
		public int VirtualWidth => BufferWidth;
		public int VirtualHeight => BufferHeight;

		public int[] GetVideoBuffer()
		{
			return _frameBuffer;
		}


		public void SyncState(Serializer ser)
		{
			ser.BeginSection("GateArray");
			ser.Sync(nameof(_PENR), ref _PENR);
			ser.Sync(nameof(_INKR), ref _INKR);
			ser.Sync(nameof(_RMR), ref _RMR);
			ser.Sync(nameof(_RAMR), ref _RAMR);
			ser.Sync(nameof(_colourRegisters), ref _colourRegisters, false);
			ser.Sync(nameof(_currentPen), ref _currentPen);
			ser.Sync(nameof(_screenMode), ref _screenMode);
			//ser.Sync(nameof(_screenModePending), ref _screenModePending);
			ser.Sync(nameof(_interruptCounter), ref _interruptCounter);
			ser.Sync(nameof(_holdingInterrupt), ref _holdingInterrupt);
			ser.Sync(nameof(_interruptHoldCounter), ref _interruptHoldCounter);
			ser.Sync(nameof(_clockCounter), ref _clockCounter);
			ser.Sync(nameof(_videoData), ref _videoData, false);
			ser.Sync(nameof(_videoDataByte1), ref _videoDataByte1);
			ser.Sync(nameof(_videoDataByte2), ref _videoDataByte2);
			ser.Sync(nameof(GA_VSYNC), ref GA_VSYNC);
			ser.Sync(nameof(GA_HSYNC), ref GA_HSYNC);
			ser.Sync(nameof(CRT_HSYNC), ref CRT_HSYNC);
			ser.Sync(nameof(HSYNCCounter), ref HSYNCCounter);
			ser.Sync(nameof(_HSYNCWidthCounter), ref _HSYNCWidthCounter);
			ser.Sync(nameof(GAClockCounter), ref _GAClockCounter);
			ser.Sync(nameof(LastGAFrameClocks), ref _lastGAFrameClocks);
			ser.Sync(nameof(_horCharCounter), ref _horCharCounter);
			ser.EndSection();
		}
	}
}