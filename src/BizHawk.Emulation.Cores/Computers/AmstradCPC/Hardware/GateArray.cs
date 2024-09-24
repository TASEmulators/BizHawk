
using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Cores.Components.Z80A;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	/// <summary>
	/// * Amstrad Gate Array *
	/// http://www.cpcwiki.eu/index.php/Gate_Array
	/// https://web.archive.org/web/20170612081209/http://www.grimware.org/doku.php/documentations/devices/gatearray
	/// http://bread80.com/2021/06/03/understanding-the-amstrad-cpc-video-ram-and-gate-array-subsystem/
	/// https://cpctech.cpcwiki.de/docs/crtcnew.html
	/// </summary>
	public class GateArray : IPortIODevice
	{
		private readonly CPCBase _machine;
		private Z80A<AmstradCPC.CpuLink> CPU => _machine.CPU;
		private CRTC CRTC => _machine.CRTC;

		private CRTScreen CRT => _machine.CRTScreen;
		private IPSG PSG => _machine.AYDevice;
		private ushort BUSRQ => CPU.MEMRQ[CPU.bus_pntr];
		private GateArrayType GateArrayType;

		/// <summary>
		/// True when the frame has ended
		/// </summary>
		public bool FrameEnd;

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
					//InterruptCounter = 0;
					R52 = 0;
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
		/// Set when the VSYNC signal is detected from the CRTC
		/// </summary>
		private bool GA_VSYNC;

		/// <summary>
		/// Set when the HSYNC signal is detected from the CRCT
		/// </summary>
		private bool GA_HSYNC;

		/// <summary>
		/// VSYNC signal that is generated by the GA and combined with C_HSYNC before being sent to the CRT
		/// </summary>
		private bool C_VSYNC;

		/// <summary>
		/// HSYNC signal that is generated by the GA and combined with C_VSYNC before being sent to the CRT
		/// </summary>
		private bool C_HSYNC;
		
		/// <summary>
		/// Gatearray is outputting black colour during vsync
		/// </summary>
		private bool C_VSYNC_Black;

		/// <summary>
		/// Gatearray is outputting black colour during hsync
		/// </summary>
		private bool C_HSYNC_Black;

		/// <summary>
		/// GA raster counter incremented at the end of every HSYNC signal from the CRTC
		/// (interrupt counter)
		/// </summary>
		private int R52
		{
			get => _r52 & 0x3F;
			set
			{
				_r52 = value & 0x3F;

				if (_r52 > 51)
				{
					_r52 = 0;
				}

				if (_r52 == 0)
				{
					// The GATE ARRAY sends an interrupt request when R52=0
					CPU.FlagI = true;
				}
				else if (GA_VSYNC && _r52 == 2)
				{
					// Two HSYNC’s after the start of VSYNC:
					if (_r52.Bit(5))
					{
						// An interrupt is requested by the GATE ARRAY from the Z80A only if bit 5 of R52 is 1
						CPU.FlagI = true;
					}

					// R52 is set to 0 unconditionally
					_r52 = 0;
				}
			}
		}
		private int _r52;

		/// <summary>
		/// GA counter that counts the number of HSYNC signals that have been detected during the VSYNC period
		/// </summary>
		private int V26
		{
			get => _v26 & 0x3F;
			set
			{
				_v26 = value & 0x3F;

				if (_v26 == 2)
				{
					// GA activates C-SYNC
					C_VSYNC = true;
				}

				if (_v26 == 6)
				{
					C_VSYNC = false;
				}

				if (_v26 == 26)
				{
					C_VSYNC_Black = false;
					// vsync completed
					GA_VSYNC = false;
				}

				
			}
		}
		private int _v26;

		

		/// <summary>
		/// Counts the number of CRTC characters processed during a (CRTC) HSYNC signal
		/// </summary>
		private int H06
		{
			get => _h06 & 0xFF;
			set
			{
				_h06 = value & 0xFF;

				if (_h06 == 2)
				{
					C_HSYNC = true;

					// latch screenmode
					_screenMode = (byte)(_RMR & 0x03);
				}

				if (_h06 == 6)
				{
					C_HSYNC = false;
				}
			}
		}
		private int _h06;


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
			// an armed (pending) interrupt is acknowledged/authorised by the CPU
			// R52 Bit5 is reset
			R52 &= ~(1 << 5);
			CPU.FlagI = false;
		}

		/// <summary>
		/// Fired when rising edge of CRTC HSYNC signal is detected
		/// </summary>
		public void OnHSYNCOn()
		{
			// CRTC character counter initialised
			H06 = 0;
			// gate array hsync is enabled
			GA_HSYNC = true;
			// hsync black colour enabled
			C_HSYNC_Black = true;			
		}

		/// <summary>
		/// Fired when falling edge of CRTC HSYNC signal is detected
		/// </summary>
		public void OnHSYNCOff()
		{
			GA_HSYNC = false;

			// turn off composite hsync
			C_HSYNC = false;

			// disable composite black
			C_HSYNC_Black = false;

			// The 6-bit counter is incremented after each HSYNC from the CRTC
			// (When standard CRTC display settings are used, this is equivalent to counting scan-lines)
			R52++;

			if (GA_VSYNC)
			{
				// vsync is active - count hsyncs
				V26++;
			}
		}

		/// <summary>
		/// Fired when CRTC VSYNC active signal is detected
		/// </summary>
		public void OnVSYNCOn()
		{
			// hsync counter initialised
			V26 = 0;
			// gate array vsync is enabled
			GA_VSYNC = true;
			// black colour enabled for vsync
			C_VSYNC_Black = true;
		}

		/// <summary>
		/// Fired when falling edge of CRTC VSYNC signal is detected
		/// </summary>
		public void OnVSYNCOff()
		{
			//GA_VSYNC = false;
			// this is effectively the start of a new frame from a bizhawk perspective
			// latch the current frame clock count
			LastGAFrameClocks = GAClockCounter;

			// reset the frame clock counter
			GAClockCounter = -1;
			FrameEnd = true;

			// interrupts should be syncronised with the start of the frame now (i.e. InterruptCounter = 0)
			// CRT beam position should be at the start of the display area
		}

		/// <summary>
		/// 16 MHz XTAL crystal that clocks the GA
		/// We will use this as a 4bit clock counter
		/// </summary>
		private int _xtal;

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

		private ushort[] _videoAddr = new ushort[2]; 


		private byte[] _videoDataBuffer = new byte[64];
		private int VideoDataPntr
		{
			get => _videoDataPntr & 0x3F;
			set => _videoDataPntr = value & 0x3F;
		}
		private int _videoDataPntr = 0;

		private void LatchVideoByte(byte data)
		{
			VideoDataPntr++;
			_videoDataBuffer[VideoDataPntr] = data;
		}
		

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
			// Gatearray should be constantly outputting a single pixel of video data ever 1/16th of a microsecond
			// (so every 1 GA clock cycle)
			// We will do this for now to get the accuracy right, but will probably need to optimise this down the line
			//OutputPixel(0);

			// Based on timing oscilloscope traces from 
			// https://bread80.com
			// and section 16.2.2 of ACCC1.8
			switch (_xtal)
			{
				case 15:
					// /CPU_ADDR LOW
					// /RAS HIGH

					if (GA_HSYNC)
					{
						H06++;
					}

					break;

				case 0:

					OutputByte(0);

					// READY HIGH (Z80 /WAIT is inactive)
					// /RAS LOW

					// /CCLK LOW
					CRTC.CLK = false;
					CRTC.Clock();

					// PHI HIGH (1)
					CPU.ExecuteOne();
					break;

				case 1:
					// /CAS_ADDR LOW

					// RAM is outputting video data and the gatearray should be latching it in
					LatchVideoByte(_machine.FetchScreenMemory(CRTC.MA_Address));
					//_videoData[1] = _videoDataByte2;
					//_videoDataByte2 = _machine.FetchScreenMemory(CRTC.MA_Address);



					break;

				case 2:
					// PHI LOW (1)
					break;

				case 3:
					break;

				case 4:
					// READY LOW (Z80 /WAIT is active)

					// PHI HIGH (2)
					if (BUSRQ == 1) // PCh
					{
						// Z80 will sample /WAIT during the cycle it intends to access the bus when reading opcodes
						// but will sample *before* the cycle for other memory accesses

						// opcode fetch memory action upcoming - CPU will wait
						CPU.TotalExecutedCycles++;
					}
					else
					{
						// no fetch, or non-opcode fetch - CPU does not wait
						CPU.ExecuteOne();
					}

					break;

				case 5:
					// /RAS HIGH
					// /CPU_ADDR HIGH
					// /CAS_ADDR HIGH
					break;

				case 6:
					// PHI LOW (2)
					break;

				case 7:
					// /RAS LOW
					// /CAS_ADDR LOW					
					break;

				case 8:

					OutputByte(0);

					// PHI HIGH (3)
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

				case 9:
					break;

				case 10:
					// PHI LOW (3)
					break;

				case 11:
					// /CCLK HIGH
					CRTC.CLK = true;

					// RAM is outputting video data and the gatearray should be latching it in
					LatchVideoByte(_machine.FetchScreenMemory(CRTC.MA_Address));
					//_videoData[0] = _videoDataByte1;
					//_videoDataByte1 = _machine.FetchScreenMemory(CRTC.MA_Address);
					break;

				case 12:
					// PHI HIGH (4)
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

				case 13:
					break;

				case 14:
					break;
			}

			

			_xtal++;

			// enforce 4-bit wraparound
			_xtal &= 0x0F;

			GAClockCounter++;
		}

		private void OutputByte(int byteOffset)
		{
			int pen = 0;
			int colour = 0;

			var dataByte = _videoDataBuffer[VideoDataPntr + byteOffset];

			for (int pixIndex = 0; pixIndex < 8; pixIndex++)
			{
				if (C_VSYNC_Black)
				{
					colour = 0;
				}
				else if (C_HSYNC_Black)
				{
					colour = 0;
				}
				else if (!CRTC.DISPTMG)
				{
					colour = CPCHardwarePalette[_colourRegisters[16]];
				}
				else if (CRTC.DISPTMG)
				{
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
							pen = pixIndex < 4
								? ((dataByte & 0x80) >> 7) | ((dataByte & 0x08) >> 2) | ((dataByte & 0x20) >> 3) | ((dataByte & 0x02) << 2)
								: ((dataByte & 0x40) >> 6) | ((dataByte & 0x04) >> 1) | ((dataByte & 0x10) >> 2) | ((dataByte & 0x01) << 3);
							break;

						// Mode 1, 2-bits per pixel, 320x200 resolution, 4 colours
						// ------------------------------------------------------------------
						// Video Byte Bit:	|  7  |  6  |  5  |  4  |  3  |  2  |  1  |  0  |
						// Pixel:			|  0  |  1  |  2  |  3  |  0  |  1  |  2  |  3  |
						// Pixel Bit Enc.:	|  0  |  0  |  0  |  0  |  1  |  1  |  1  |  1  |
						// Pixel Timing:	|     0     |     1     |	  2     |     3     |
						// ------------------------------------------------------------------
						case 1:
							switch (pixIndex)
							{
								// pixel 0
								case 0:
								case 1:
									pen = ((dataByte & 0x80) >> 7) | ((dataByte & 0x08) >> 2);
									break;

								case 2:
								case 3:
									pen = ((dataByte & 0x40) >> 6) | ((dataByte & 0x04) >> 1);
									break;

								case 4:
								case 5:
									pen = ((dataByte & 0x20) >> 5) | (dataByte & 0x02);
									break;

								case 6:
								case 7:
									pen = ((dataByte & 0x10) >> 4) | ((dataByte & 0x01) << 1);
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
							switch (pixIndex)
							{
								case 0:
									pen = dataByte.Bit(7) ? 1 : 0;
									break;

								case 1:
									pen = dataByte.Bit(6) ? 1 : 0;
									break;

								case 2:
									pen = dataByte.Bit(5) ? 1 : 0;
									break;

								case 3:
									pen = dataByte.Bit(4) ? 1 : 0;
									break;

								case 4:
									pen = dataByte.Bit(3) ? 1 : 0;
									break;

								case 5:
									pen = dataByte.Bit(2) ? 1 : 0;
									break;

								case 6:
									pen = dataByte.Bit(1) ? 1 : 0;
									break;

								case 7:
									pen = dataByte.Bit(0) ? 1 : 0;
									break;
							}
							break;

						// Mode 3, 2-bits per pixel, 160x200 resolution, 4 colours (undocumented)
						// ------------------------------------------------------------------
						// Video Byte Bit:	|  7  |  6  |  5  |  4  |  3  |  2  |  1  |  0  |
						// Pixel:			|  0  |  1  |  x  |  x  |  0  |  1  |  x  |  x  |
						// Pixel Bit Enc.:	|  0  |  0  |  x  |  x  |  1  |  1  |  x  |  x  |
						// Pixel Timing:	|           0           |           1           |
						// ------------------------------------------------------------------
						case 3:
							pen = pixIndex < 4
								? ((dataByte & 0x80) >> 7) | ((dataByte & 0x08) >> 2)
								: ((dataByte & 0x40) >> 6) | ((dataByte & 0x04) >> 1);
							break;
					}

					colour = CPCHardwarePalette[_colourRegisters[pen]];
				}

				CRT.VideoClock(colour, -1, C_HSYNC, C_VSYNC);
			}
		}
		

		/// <summary>
		/// Outputs a single pixel to the monitor at 16MHz
		/// </summary>
		private void OutputPixel(int pixelOffset)
		{
			int pen = 0;
			int colour = 0;

			var pixIndex = (_xtal + pixelOffset) & 0x0F;

			var vidByteIndex = _xtal < 8 ? 0 : 1;
			var dataByte = _xtal < 8 ? _videoDataByte1 : _videoDataByte2; // _videoData[vidByteIndex];
			pixIndex &= 0x07;

			if (dataByte != 0)
			{

			}

			if (C_VSYNC_Black)
			{
				colour = 0;
			}
			else if (C_HSYNC_Black)
			{
				colour = 0;
			}
			else if (!CRTC.DISPTMG)
			{
				colour = CPCHardwarePalette[_colourRegisters[16]];
			}
			else if (CRTC.DISPTMG)
			{
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
						pen = pixIndex < 4
							? ((dataByte & 0x80) >> 7) | ((dataByte & 0x08) >> 2) | ((dataByte & 0x20) >> 3) | ((dataByte & 0x02) << 2)
							: ((dataByte & 0x40) >> 6) | ((dataByte & 0x04) >> 1) | ((dataByte & 0x10) >> 2) | ((dataByte & 0x01) << 3);
						break;

					// Mode 1, 2-bits per pixel, 320x200 resolution, 4 colours
					// ------------------------------------------------------------------
					// Video Byte Bit:	|  7  |  6  |  5  |  4  |  3  |  2  |  1  |  0  |
					// Pixel:			|  0  |  1  |  2  |  3  |  0  |  1  |  2  |  3  |
					// Pixel Bit Enc.:	|  0  |  0  |  0  |  0  |  1  |  1  |  1  |  1  |
					// Pixel Timing:	|     0     |     1     |	  2     |     3     |
					// ------------------------------------------------------------------
					case 1:
						switch (pixIndex)
						{
							// pixel 0
							case 0:
							case 1:
								pen = ((dataByte & 0x80) >> 7) | ((dataByte & 0x08) >> 2);
								break;

							case 2:
							case 3:
								pen = ((dataByte & 0x40) >> 6) | ((dataByte & 0x04) >> 1);
								break;

							case 4:
							case 5:
								pen = ((dataByte & 0x20) >> 5) | (dataByte & 0x02);
								break;

							case 6:
							case 7:
								pen = ((dataByte & 0x10) >> 4) | ((dataByte & 0x01) << 1);
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
						switch (pixIndex)
						{
							case 0:
								pen = dataByte.Bit(7) ? 1 : 0;
								break;

							case 1:
								pen = dataByte.Bit(6) ? 1 : 0;
								break;

							case 2:
								pen = dataByte.Bit(5) ? 1 : 0;
								break;

							case 3:
								pen = dataByte.Bit(4) ? 1 : 0;
								break;

							case 4:
								pen = dataByte.Bit(3) ? 1 : 0;
								break;

							case 5:
								pen = dataByte.Bit(2) ? 1 : 0;
								break;

							case 6:
								pen = dataByte.Bit(1) ? 1 : 0;
								break;

							case 7:
								pen = dataByte.Bit(0) ? 1 : 0;
								break;
						}
						break;

					// Mode 3, 2-bits per pixel, 160x200 resolution, 4 colours (undocumented)
					// ------------------------------------------------------------------
					// Video Byte Bit:	|  7  |  6  |  5  |  4  |  3  |  2  |  1  |  0  |
					// Pixel:			|  0  |  1  |  x  |  x  |  0  |  1  |  x  |  x  |
					// Pixel Bit Enc.:	|  0  |  0  |  x  |  x  |  1  |  1  |  x  |  x  |
					// Pixel Timing:	|           0           |           1           |
					// ------------------------------------------------------------------
					case 3:
						pen = pixIndex < 4
							? ((dataByte & 0x80) >> 7) | ((dataByte & 0x08) >> 2)
							: ((dataByte & 0x40) >> 6) | ((dataByte & 0x04) >> 1);
						break;
				}

				colour = CPCHardwarePalette[_colourRegisters[pen]];
			}
			else
			{

			}

			CRT.VideoClock(colour, -1, C_HSYNC, C_VSYNC);
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
			ser.Sync(nameof(_v26), ref _v26);
			ser.Sync(nameof(_h06), ref _h06);
			ser.Sync(nameof(_r52), ref _r52);
			ser.Sync(nameof(C_HSYNC), ref C_HSYNC);
			ser.Sync(nameof(C_HSYNC_Black), ref C_HSYNC_Black);
			ser.Sync(nameof(C_VSYNC), ref C_VSYNC);
			ser.Sync(nameof(C_VSYNC_Black), ref C_VSYNC_Black);
			ser.Sync(nameof(_xtal), ref _xtal);
			ser.Sync(nameof(_videoData), ref _videoData, false);
			ser.Sync(nameof(_videoDataByte1), ref _videoDataByte1);
			ser.Sync(nameof(_videoDataByte2), ref _videoDataByte2);
			ser.Sync(nameof(GA_VSYNC), ref GA_VSYNC);
			ser.Sync(nameof(GA_HSYNC), ref GA_HSYNC);
			ser.Sync(nameof(GAClockCounter), ref _GAClockCounter);
			ser.Sync(nameof(LastGAFrameClocks), ref _lastGAFrameClocks);
			ser.EndSection();
		}
	}
}