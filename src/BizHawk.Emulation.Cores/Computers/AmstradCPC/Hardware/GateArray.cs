
using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
//using BizHawk.Emulation.Cores.Components.Z80A;
using System.Linq;

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
		//private Z80A<AmstradCPC.CpuLink> CPU => _machine.CPU;
		private LibFz80Wrapper CPU => _machine.CPU;
		private CRTC CRTC => _machine.CRTC;

		private CRTScreen CRT => _machine.CRTScreen;
		private IPSG PSG => _machine.AYDevice;
		//private ushort BUSRQ => CPU.MEMRQ[CPU.bus_pntr];
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

		public double CRTCClockCounter => (double)GAClockCounter / 16;
		public double CPUClockClounter => (double)GAClockCounter / 4;


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
		/// All CPC colour information
		/// Based on: https://www.grimware.org/doku.php/documentations/devices/gatearray
		/// </summary>
		private CPCColourData[] CPCPalette = new CPCColourData[32];

		private void SetPalette(GateArrayType gaType)
		{
			switch (gaType)
			{
				case GateArrayType.Amstrad40007:
				case GateArrayType.Amstrad40008:
				case GateArrayType.Amstrad40010:

					// non-asic
					CPCPalette = new CPCColourData[]
					{
						new CPCColourData { IndexFirmware = 0, IndexINKR = 0b01011000, IndexASIC = 0x000, Red = 0.0, Green = 0.9615, Blue = 0.4808 },
						new CPCColourData { IndexFirmware = 1, IndexINKR = 0b01000100, IndexASIC = 0x006, Red = 0.0, Green = 0.9615, Blue = 41.8269 },
						new CPCColourData { IndexFirmware = 2, IndexINKR = 0b01010101, IndexASIC = 0x00F, Red = 4.8077, Green = 0.9615, Blue = 95.6731 },
						new CPCColourData { IndexFirmware = 3, IndexINKR = 0b01011100, IndexASIC = 0x060, Red = 42.3077, Green = 0.9615, Blue = 0.4808 },
						new CPCColourData { IndexFirmware = 4, IndexINKR = 0b01011000, IndexASIC = 0x066, Red = 41.3462, Green = 0.9615, Blue = 40.8654 },
						new CPCColourData { IndexFirmware = 5, IndexINKR = 0b01011101, IndexASIC = 0x06F, Red = 42.3077, Green = 0.9615, Blue = 94.7115 },
						new CPCColourData { IndexFirmware = 6, IndexINKR = 0b01001100, IndexASIC = 0x0F0, Red = 95.1923, Green = 1.9231, Blue = 2.4038 },
						new CPCColourData { IndexFirmware = 7, IndexINKR = 0b01000101, IndexASIC = 0x0F6, Red = 94.2308, Green = 0.9615, Blue = 40.8654 },
						new CPCColourData { IndexFirmware = 8, IndexINKR = 0b01001101, IndexASIC = 0x0FF, Red = 95.1923, Green = 0.9615, Blue = 95.6731 },
						new CPCColourData { IndexFirmware = 9, IndexINKR = 0b01010110, IndexASIC = 0x600, Red = 0.9615, Green = 47.1154, Blue = 0.4808 },
						new CPCColourData { IndexFirmware = 10, IndexINKR = 0b01000110, IndexASIC = 0x606, Red = 0.0, Green = 47.1154, Blue = 40.8654 },
						new CPCColourData { IndexFirmware = 11, IndexINKR = 0b01010111, IndexASIC = 0x60F, Red = 4.8077, Green = 48.0769, Blue = 95.6731 },
						new CPCColourData { IndexFirmware = 12, IndexINKR = 0b01011110, IndexASIC = 0x660, Red = 43.2692, Green = 48.0769, Blue = 0.4808 },
						new CPCColourData { IndexFirmware = 13, IndexINKR = 0b01000000, IndexASIC = 0x666, Red = 43.2692, Green = 49.0385, Blue = 41.8269 },
						new CPCColourData { IndexFirmware = 14, IndexINKR = 0b01011111, IndexASIC = 0x66F, Red = 43.2692, Green = 48.0769, Blue = 96.6346 },
						new CPCColourData { IndexFirmware = 15, IndexINKR = 0b01001110, IndexASIC = 0x6F0, Red = 95.1923, Green = 49.0385, Blue = 5.2885 },
						new CPCColourData { IndexFirmware = 16, IndexINKR = 0b01000111, IndexASIC = 0x6F6, Red = 95.1923, Green = 49.0385, Blue = 41.8269 },
						new CPCColourData { IndexFirmware = 17, IndexINKR = 0b01001111, IndexASIC = 0x6FF, Red = 98.0769, Green = 50.0, Blue = 97.5962 },
						new CPCColourData { IndexFirmware = 18, IndexINKR = 0b01010010, IndexASIC = 0xF00, Red = 0.9615, Green = 94.2308, Blue = 0.4808 },
						new CPCColourData { IndexFirmware = 19, IndexINKR = 0b01000010, IndexASIC = 0xF06, Red = 0.0, Green = 95.1923, Blue = 41.8269 },
						new CPCColourData { IndexFirmware = 20, IndexINKR = 0b01010011, IndexASIC = 0xF0F, Red = 5.7692, Green = 95.1923, Blue = 94.7115 },
						new CPCColourData { IndexFirmware = 21, IndexINKR = 0b01011010, IndexASIC = 0xF60, Red = 44.2308, Green = 96.1538, Blue = 1.4423 },
						new CPCColourData { IndexFirmware = 22, IndexINKR = 0b01011001, IndexASIC = 0xF66, Red = 44.2308, Green = 95.1923, Blue = 41.8269 },
						new CPCColourData { IndexFirmware = 23, IndexINKR = 0b01011011, IndexASIC = 0xF6F, Red = 44.2308, Green = 95.1923, Blue = 95.6731 },
						new CPCColourData { IndexFirmware = 24, IndexINKR = 0b01001010, IndexASIC = 0xFF0, Red = 95.1923, Green = 95.1923, Blue = 5.2885 },
						new CPCColourData { IndexFirmware = 25, IndexINKR = 0b01000011, IndexASIC = 0xFF6, Red = 95.1923, Green = 95.1923, Blue = 42.7885 },
						new CPCColourData { IndexFirmware = 26, IndexINKR = 0b01001011, IndexASIC = 0xFFF, Red = 100.0, Green = 95.1923, Blue = 97.5962 },

						new CPCColourData { IndexFirmware = 27, IndexINKR = 0b01000001, IndexASIC = 0x666, Red = 43.2692, Green = 48.0769, Blue = 42.7885 },
						new CPCColourData { IndexFirmware = 28, IndexINKR = 0b01001000, IndexASIC = 0x0F6, Red = 95.1923, Green = 0.9615, Blue = 40.8654 },
						new CPCColourData { IndexFirmware = 29, IndexINKR = 0b01001001, IndexASIC = 0xFF6, Red = 95.1923, Green = 95.1923, Blue = 41.8269 },
						new CPCColourData { IndexFirmware = 30, IndexINKR = 0b01010000, IndexASIC = 0x006, Red = 0.0, Green = 0.9615, Blue = 40.8654 },
						new CPCColourData { IndexFirmware = 31, IndexINKR = 0b01010001, IndexASIC = 0xF06, Red = 0.9615, Green = 95.1923, Blue = 41.8269 },
					}.OrderBy(x => x.IndexHardware).ToArray();

					break;
				case GateArrayType.Amstrad40226:
				case GateArrayType.Amstrad40489:

					// asic


					break;
			}
		}

		/// <summary>
		/// The standard CPC Pallete (ordered by firmware #)
		/// http://www.cpcwiki.eu/index.php/CPC_Palette
		/// </summary>
		private static readonly int[] CPCFirmwarePalette =
		{
			Colors.ARGB(0x00, 0x00, 0x00), // Black					0
            Colors.ARGB(0x00, 0x00, 0x80), // Blue					1
            Colors.ARGB(0x00, 0x00, 0xFF), // Bright Blue			2
            Colors.ARGB(0x80, 0x00, 0x00), // Red					3
            Colors.ARGB(0x80, 0x00, 0x80), // Magenta				4
            Colors.ARGB(0x80, 0x00, 0xFF), // Mauve					5
            Colors.ARGB(0xFF, 0x00, 0x00), // Bright Red			6
            Colors.ARGB(0xFF, 0x00, 0x80), // Purple				7
            Colors.ARGB(0xFF, 0x00, 0xFF), // Bright Magenta		8
            Colors.ARGB(0x00, 0x80, 0x00), // Green					9
            Colors.ARGB(0x00, 0x80, 0x80), // Cyan					10
            Colors.ARGB(0x00, 0x80, 0xFF), // Sky Blue				11
            Colors.ARGB(0x80, 0x80, 0x00), // Yellow				12
            Colors.ARGB(0x80, 0x80, 0x80), // White					13
            Colors.ARGB(0x80, 0x80, 0xFF), // Pastel Blue			14
            Colors.ARGB(0xFF, 0x80, 0x00), // Orange				15
            Colors.ARGB(0xFF, 0x80, 0x80), // Pink					16
            Colors.ARGB(0xFF, 0x80, 0xFF), // Pastel Magenta		17
            Colors.ARGB(0x00, 0xFF, 0x00), // Bright Green			18
            Colors.ARGB(0x00, 0xFF, 0x80), // Sea Green				19
            Colors.ARGB(0x00, 0xFF, 0xFF), // Bright Cyan			20
            Colors.ARGB(0x80, 0xFF, 0x00), // Lime					21
            Colors.ARGB(0x80, 0xFF, 0x80), // Pastel Green			22
            Colors.ARGB(0x80, 0xFF, 0xFF), // Pastel Cyan			23
            Colors.ARGB(0xFF, 0xFF, 0x00), // Bright Yellow			24
            Colors.ARGB(0xFF, 0xFF, 0x80), // Pastel Yellow			25
            Colors.ARGB(0xFF, 0xFF, 0xFF), // Bright White			26
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
				else
				{
					// pen select
					_currentPen = _PENR & 0b0000_1111;
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
				if (_currentPen == 16)
				{

				}
				_colourRegisters[_currentPen] = _INKR & 0b0001_1111;
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
				_machine.UpperROMPaged = !_RMR.Bit(3);

				// Lower ROM paging
				_machine.LowerROMPaged = !_RMR.Bit(2);

				// Interrupt generation control
				if (_RMR.Bit(4))
				{
					// reset interrupt counter
					_r52 = 0;
				}
			}
		}
		private byte _RMR;

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

		public int interruptsPerFrame { get; set; }

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
					//CPU.FlagI = true;
					CPU.INT = 1;
					interruptsPerFrame++;
				}
				else if (GA_VSYNC && _r52 == 2)
				{
					// Two HSYNC’s after the start of VSYNC:
					if (_r52.Bit(5))
					{
						// An interrupt is requested by the GATE ARRAY from the Z80A only if bit 5 of R52 is 1
						//CPU.FlagI = true;
						CPU.INT = 1;
						interruptsPerFrame++;
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
					FrameEnd = true;
					LastGAFrameClocks = GAClockCounter;
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

			CPU.AttachIRQACKOnCallback(IORQA);

			SetPalette(GateArrayType);

			Reset();
		}

		/// <summary>
		/// Called when the Z80 acknowledges an interrupt
		/// </summary>
		public void IORQA()
		{
			// an armed (pending) interrupt is acknowledged/authorised by the CPU
			// R52 Bit5 is reset
			_r52 &= ~(1 << 5);
			//CPU.FlagI = false;
			CPU.INT = 0;
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

		private int GAClockCounterOriginVSYNC;
		private double GACunlockCounterOriginVSYNCCRTC => (double)GAClockCounterOriginVSYNC / 16;

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

			// signal start of new frame
			GAClockCounterOriginVSYNC = 0;
			FrameEnd = true;
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
			//FrameEnd = true;
			//GAClockCounter = -1;
			

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
			GAClockCounter++;
			GAClockCounterOriginVSYNC++;


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
					//CPU.FlagW = false;
					CPU.WAIT = 0;
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
					//CPU.FlagW = true;
					CPU.WAIT = 1;

					// PHI HIGH (2)
					CPU.ExecuteOne();
					/*
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
					*/

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
					CPU.ExecuteOne();

					/*
					if (BUSRQ > 0)
					{
						// memory action upcoming - CPU clock is halted
						CPU.TotalExecutedCycles++;
					}
					else
					{
						CPU.ExecuteOne();
					}
					*/
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
					CPU.ExecuteOne();

					/*
					if (BUSRQ > 0)
					{
						// memory action upcoming - CPU clock is halted
						CPU.TotalExecutedCycles++;
					}
					else
					{
						CPU.ExecuteOne();
					}
					*/
					break;

				case 13:
					break;

				case 14:
					break;
			}

			

			_xtal++;

			// enforce 4-bit wraparound
			_xtal &= 0x0F;

			
			/*
			if (GA_VSYNC)
			{
				FrameEnd = true;
				LastGAFrameClocks = GAClockCounter;
			}
			*/
		}

		private void OutputByte(int byteOffset)
		{
			int pen = 0;
			int colour = 0;

			var dataByte = _videoDataBuffer[VideoDataPntr + byteOffset];

			//var vid = new CPCColourData();

			for (int pixIndex = 0; pixIndex < 8; pixIndex++)
			{
				/*
				if (C_HSYNC)
					vid.C_HSYNC = true;

				if (C_VSYNC)
					vid.C_VSYNC = true;
				*/

				if (C_VSYNC_Black)
				{
					colour = 0;
					//vid.ARGB = 0;
				}
				else if (C_HSYNC_Black)
				{
					colour = 0;
					//vid.ARGB = 0;
				}
				else if (!CRTC.DISPTMG)
				{
					//colour = CPCHardwarePalette[_colourRegisters[16]];
					colour = CPCPalette[_colourRegisters[16]].ARGB;
					//vid = CPCPalette[_colourRegisters[16]];
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
						// ------------------------------------------------------------------
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

					//colour = CPCHardwarePalette[_colourRegisters[pen]];
					colour = CPCPalette[_colourRegisters[pen]].ARGB;
					//vid = CPCPalette[_colourRegisters[pen]];
				}

				CRT.VideoClock(colour, -1, C_HSYNC, C_VSYNC);
				//CRT.VideoClock(vid, -1);
			}
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

		
		public bool GateArrayUnlocked { get; set; }

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

			if (!result.Bit(7) && !result.Bit(6))
			{
				// PENR register
				PENR = (byte)result;
			}

			if (!result.Bit(7) && result.Bit(6))
			{
				// INKR register
				INKR = (byte)result;
			}

			if (result.Bit(7) && !result.Bit(6))
			{
				if (result.Bit(5) && GateArrayUnlocked)
				{
					// ASIC & Advanced ROM mapping (unlocked ASIC only)
				}
				else
				{
					// RMR (or RMR ghost) register
					RMR = (byte)result;
				}
			}

			/*
			// Gate array functions are selected by decoding the top two bits (6 and 7) of the data byte sent
			switch ((byte)(result >> 6))
			{
				case 0b_00:
					// PENR register
					PENR = (byte)result;
					break;

				case 0b_01:
					// INKR register
					INKR = (byte)result;
					break;

				case 0b_10:

					if (result.Bit(5) && GateArrayUnlocked)
					{
						// ASIC & Advanced ROM mapping (unlocked ASIC only)
					}
					else
					{
						// RMR (or RMR ghost) register
						RMR = (byte)result;
					}
					break;

				case 0b_11:
					// RAMR register
					RAMR = (byte)result;
					break;
			}

			*/

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

	/// <summary>
	/// Data structure for holding
	/// </summary>
	public class CPCColourData
	{
		/// <summary>
		/// CPC Firmware Index (also defines the green screen luminosity)
		/// </summary>
		public int IndexFirmware { get; set; }

		/// <summary>
		/// CPC Hardware Index
		/// </summary>
		public int IndexHardware => IndexINKR & 0b00011111;

		/// <summary>
		/// CPC Hardware Palette Index (the INKR number)
		/// </summary>
		public int IndexINKR { get; set; }		

		/// <summary>
		/// 12-bit ASIC index value
		/// </summary>
		public int IndexASIC { get; set; }

		/// <summary>
		/// RED channel percentage
		/// </summary>
		public double Red { get; set; }

		/// <summary>
		/// GREEN channel percentage
		/// </summary>
		public double Green { get; set; }

		/// <summary>
		/// BLUE channel percentage
		/// </summary>
		public double Blue {  get; set; }

		/// <summary>
		/// .NET ARGB value
		/// </summary>
		public int ARGB
		{
			get
			{
				var r = (int)(Red * 255 / 100);
				var g = (int)(Green * 255 / 100);
				var b = (int)(Blue * 255 / 100);

				return (r << 16) | (g << 8) | b;
			}
			set
			{
				int r = (value >> 16) & 0xFF;
				int g = (value >> 8) & 0xFF;
				int b = value & 0xFF;

				Red = r * 100 / 255;
				Green = g * 100 / 255;
				Blue = b * 100 / 255;
			}
		}

		/// <summary>
		/// Composite VSYNC - set when CSYNC is pulsed
		/// </summary>
		public bool C_VSYNC { get; set; }

		/// <summary>
		/// Composite HSYNC - set when CSYNC is pulsed
		/// </summary>
		public bool C_HSYNC { get; set; }
	}
}