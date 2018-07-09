using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.Z80A;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// * Amstrad Gate Array *
    /// http://www.cpcwiki.eu/index.php/Gate_Array
    /// https://web.archive.org/web/20170612081209/http://www.grimware.org/doku.php/documentations/devices/gatearray
    /// </summary>
    public class AmstradGateArray : IVideoProvider, IPortIODevice
    {
        #region Devices

        private CPCBase _machine;
        private Z80A CPU => _machine.CPU;
        private CRCT_6845 CRCT => _machine.CRCT;
        private IPSG PSG => _machine.AYDevice;
        private NECUPD765 FDC => _machine.UPDDiskDevice;
        private DatacorderDevice DATACORDER => _machine.TapeDevice;
        private ushort BUSRQ => CPU.MEMRQ[CPU.bus_pntr];
        public const ushort PCh = 1;

        private GateArrayType ChipType;

        #endregion

        #region Clocks and Timing

        /// <summary>
        /// The Gate Array Clock Speed
        /// </summary>
        public int GAClockSpeed = 16000000;

        /// <summary>
        /// The CPU Clock Speed
        /// </summary>
        public int Z80ClockSpeed = 4000000;

        /// <summary>
        /// CRCT Clock Speed
        /// </summary>
        public int CRCTClockSpeed = 1000000;

        /// <summary>
        /// AY-3-8912 Clock Speed
        /// </summary>
        public int PSGClockSpeed = 1000000;

        /// <summary>
        /// Number of CPU cycles in one frame
        /// </summary>
        public int FrameLength = 79872;

        /// <summary>
        /// Number of Gate Array cycles within one frame
        /// </summary>
        public int GAFrameLength = 319488;

        #endregion

        #region Palletes

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

        #endregion

        #region Construction

        public AmstradGateArray(CPCBase machine, GateArrayType chipType)
        {
            _machine = machine;
            ChipType = chipType;
            //PenColours = new int[17];
            SetupScreenSize();
            //Reset();
        }

        #endregion

        #region Registers and Internal State

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
        private byte _PENR;
        public byte PENR
        {
            get { return _PENR; }
            set
            {
                _PENR = value;
                if (_PENR.Bit(4))
                {
                    // border select
                    CurrentPen = 16;
                }
                else
                {
                    // pen select
                    CurrentPen = _PENR & 0x0f;
                }
            }
        }

        /// <summary>
        /// 0-16:   Pen Registers
        /// 17:     Border Colour
        /// </summary>
        private int[] ColourRegisters = new int[17];

        /// <summary>
        /// The currently selected Pen
        /// </summary>
        private int CurrentPen;

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
        private byte _INKR;
        public byte INKR
        {
            get { return _INKR; }
            set
            {
                _INKR = value;
                ColourRegisters[CurrentPen] = _INKR & 0x1f;
            }
        }

        /// <summary>
        /// RMR (register 2) - Select screen mode and ROM configuration
        /// This register control the interrupt counter (reset), the upper and lower ROM paging and the video mode.
        /// RMR	        Commands
        /// 7	6	5	4	3	2	1	0
        /// 1	0	0	I   UR  LR  VM-->
        /// 
        /// I   : if set (1), this will reset the interrupt counter
        /// UR  : Enable (0) or Disable (1) the upper ROM paging (&C000 to &FFFF). You can select which upper ROM with the I/O address &DF00
        /// LR  : Enable (0) or Disable (1) the lower ROM paging
        /// VM  : Select the video mode 0,1,2 or 3 (it will take effect after the next HSync)
        /// </summary>
        private byte _RMR;
        public byte RMR
        {
            get { return _RMR; }
            set
            {
                _RMR = value;
                ScreenMode = _RMR & 0x03;

                if ((_RMR & 0x08) != 0)
                    _machine.UpperROMPaged = false;
                else
                    _machine.UpperROMPaged = true;

                if ((_RMR & 0x04) != 0)
                    _machine.LowerROMPaged = false;
                else
                    _machine.LowerROMPaged = true;
            }
        }

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
        public byte RAMR
        {
            get { return _RAMR; }
            set
            {
                _RAMR = value;

                // still todo
            }
        }

        /// <summary>
        /// The selected screen mode
        /// </summary>
        private int ScreenMode;

        /// <summary>
        /// Simulates the internal 6bit INT counter
        /// </summary>
        private int _interruptCounter;
        public int InterruptCounter
        {
            get { return _interruptCounter; }
            set { _interruptCounter = value; }
        }

        /// <summary>
        /// Signals that the frame end has been reached
        /// </summary>
        public bool FrameEnd;

        /// <summary>
        /// Internal phase clock
        /// </summary>
        private int ClockCounter;

        /// <summary>
        /// Master frame clock counter
        /// </summary>
        private int FrameClock;

        /// <summary>
        /// Simulates the gate array memory /WAIT line
        /// </summary>
        private bool WaitLine;     

        /// <summary>
        /// 16-bit address - read from the CRCT
        /// </summary>
        private short _MA;
        private short MA
        {
            get
            {
                _MA = CRCT.MA;
                return _MA;
            }
        }

        /// <summary>
        /// Video mode change is syncronised with HSYNC. When the mode is change it takes effect
        /// from the next HSYNC
        /// </summary>
        private int LatchedMode;

        /// <summary>
        /// Set when the HSYNC signal is detected from the CRCT
        /// </summary>
        private bool HSYNC;

        /// <summary>
        /// Is set when an initial HSYNC is seen from the CRCT
        /// On real hardware interrupt generation is based on the falling edge of the HSYNC signal
        /// So in this emulation, once the falling edge is detected, interrupt processing happens
        /// </summary>
        private bool HSYNC_falling;

        /// <summary>
        /// Used to count HSYNCs during a VSYNC
        /// </summary>
        private int HSYNC_counter;

        /// <summary>
        /// Set when the VSYNC signal is detected from the CRCT
        /// </summary>
        private bool VSYNC;

        /// <summary>
        /// TRUE when the /INT pin is held low
        /// </summary>
        private bool InterruptRaised;

        /// <summary>
        /// Counts the GA cycles that the /INT pin should be held low
        /// </summary>
        private int InterruptHoldCounter;

        /// <summary>
        /// 2 state field
        /// Because the renderer outputs 1 pixel for every 2 GA cycles
        /// </summary>
        private bool RendererFlipFlop = true;

        /// <summary>
        /// Used for counting the screen buffer positions
        /// </summary>
        private int RenderCounter;


        #endregion

        #region Internal Methods

        /// <summary>
        /// Increments the internal clock counters by one
        /// </summary>
        private void AdvanceClock()
        {
            FrameClock++;
            ClockCounter++;

            if (ClockCounter == 16)
                ClockCounter = 0;

            // check for frame end
            if (FrameClock == GAFrameLength)
            {
                FrameClock = 0;
                //FrameEnd = true;
            }
        }

        /// <summary>
        /// Runs a 4 Mhz CPU cycle if neccessary
        /// /WAIT line status is a factor here
        /// </summary>
        private void DoConditionalCPUCycle()
        {
            if (!WaitLine)
            {
                // /WAIT line is NOT active
                CPU.ExecuteOne();
                return;
            }

            // /WAIT line is active            
            switch (ClockCounter)
            {
                case 8:
                case 12:
                    // gate array video fetch is occuring
                    // check for memory access
                    if (BUSRQ > 0)
                    {
                        // memory action upcoming - CPU clock is halted
                        CPU.TotalExecutedCycles++;
                    }
                    break;

                case 4:
                    // CPU accesses RAM if it's performing a non-opcode read or write
                    // assume for now that an opcode fetch is always looking at PC
                    if (BUSRQ == PCh)
                    {
                        // opcode fetch memory action upcoming - CPU clock is halted
                        CPU.TotalExecutedCycles++;
                    }
                    else
                    {
                        // no fetch, or non-opcode fetch
                        CPU.ExecuteOne();
                    }
                    break;
            }
        }

        /// <summary>
        /// Performs one gate array (rendering) cycle
        /// </summary>
        private void DoCycle()
        {
            /*
                http://www.cpcmania.com/Docs/Programming/Painting_pixels_introduction_to_video_memory.htm
                http://www.cantrell.org.uk/david/tech/cpc/cpc-firmware/
                All 3 video modes are 200 lines in height.
                Each line is 80 video bytes in size, representing 160, 320 or 640 pixels in width depending on the mode

                Video memory organisation:
            
                LINE	    R0W0	R0W1	R0W2	R0W3	R0W4	R0W5	R0W6	R0W7
                1	        C000	C800	D000	D800	E000	E800	F000	F800
                2	        C050	C850	D050	D850	E050	E850	F050	F850
                3	        C0A0	C8A0	D0A0	D8A0	E0A0	E8A0	F0A0	F8A0
                4	        C0F0	C8F0	D0F0	D8F0	E0F0	E8F0	F0F0	F8F0
                5	        C140	C940	D140	D940	E140	E940	F140	F940
                6	        C190	C990	D190	D990	E190	E990	F190	F990
                7	        C1E0	C9E0	D1E0	D9E0	E1E0	E9E0	F1E0	F9E0
                8	        C230	CA30	D230	DA30	E230	EA30	F230	FA30
                9	        C280	CA80	D280	DA80	E280	EA80	F280	FA80
                10	        C2D0	CAD0	D2D0	DAD0	E2D0	EAD0	F2D0	FAD0
                11	        C320	CB20	D320	DB20	E320	EB20	F320	FB20
                12	        C370	CB70	D370	DB70	E370	EB70	F370	FB70
                13	        C3C0	CBC0	D3C0	DBC0	E3C0	EBC0	F3C0	FBC0
                14	        C410	CC10	D410	DC10	E410	EC10	F410	FC10
                15	        C460	CC60	D460	DC60	E460	EC60	F460	FC60
                16	        C4B0	CCB0	D4B0	DCB0	E4B0	ECB0	F4B0	FCB0
                17	        C500	CD00	D500	DD00	E500	ED00	F500	FD00
                18	        C550	CD50	D550	DD50	E550	ED50	F550	FD50
                19	        C5A0	CDA0	D5A0	DDA0	E5A0	EDA0	F5A0	FDA0
                20	        C5F0	CDF0	D5F0	DDF0	E5F0	ED50	F550	FD50
                21	        C640	CE40	D640	DE40	E640	EE40	F640	FE40
                22	        C690	CE90	D690	DE90	E690	EE90	F690	FE90
                23	        C6E0	CEE0	D6E0	DEE0	E6E0	EEE0	F6E0	FEE0
                24	        C730	CF30	D730	DF30	E730	EF30	F730	FF30
                25	        C780	CF80	D780	DF80	E780	EF80	F780	FF80
                spare start	C7D0	CFD0	D7D0	DFD0	E7D0	EFD0	F7D0	FFD0
                spare end	C7FF	CFFF	D7FF	DFFF	E7FF	EFFF	F7FF	FFFF

                Each byte represents 2, 4 or 8 pixels of the display depending on the mode.

                Mode 2, 640×200, 2 colors (each byte of video memory represents 8 pixels):
                --------------------------------------------------------------------------
                bit 7	bit 6	bit 5	bit 4	bit 3	bit 2	bit 1	bit 0
                pixel 0	pixel 1	pixel 2	pixel 3	pixel 4	pixel 5	pixel 6	pixel 7

                Mode 1, 320×200, 4 colors (each byte of video memory represents 4 pixels):
                --------------------------------------------------------------------------
                bit 7	        bit 6	        bit 5	        bit 4	        bit 3	        bit 2	        bit 1	        bit 0
                pixel 0 (bit 1)	pixel 1 (bit 1)	pixel 2 (bit 1)	pixel 3 (bit 1)	pixel 0 (bit 0)	pixel 1(bit 0)	pixel 2 (bit 0)	pixel 3 (bit 0)

                Mode 0, 160×200, 16 colors (each byte of video memory represents 2 pixels):
                ---------------------------------------------------------------------------
                bit 7	        bit 6	        bit 5	        bit 4	        bit 3	        bit 2	        bit 1	        bit 0
                pixel 0 (bit 0)	pixel 1 (bit 0)	pixel 0 (bit 2)	pixel 1 (bit 2)	pixel 0 (bit 1)	pixel 1 (bit 1)	pixel 0 (bit 3)	pixel 1 (bit 3)


                Screen layout and generation:  http://www.cpcwiki.eu/forum/programming/rupture/?action=dlattach;attach=16221
            */

            // run the interrupt generator routine
            InterruptGenerator();

            #region Testing

            if (CRCT.DISPTMG)
            {
                displayCounter++;
            }
            else if (CRCT.HSYNC)
            {
                hsyncCounter++;
            }
            else if (!CRCT.DISPTMG)
            {
                borderCounter++;
            }
            else if (CRCT.VSYNC)
            {
                vsyncCounter++;
            }

            if (!CRCT.HSYNC && HSYNC)
            {
                // end of line
                displayCounter = 0;
                hsyncCounter = 0;
                borderCounter = 0;
                vsyncCounter = 0;

                lineCounter++;
            }

            if (borderCounter > 160)
            {

            }

            if (CRCT.VSYNC)
            {

            }
            if (!CRCT.VSYNC && VSYNC)
            {
                // end of screen
                lineCounter = 0;
            }

            #endregion

            // When the start of the vertical sync is seen by the monitor it starts the next frame. This means border
            // is effectively split between top and bottom of the display. Border above the VSYNC is the bottom
            // border, Border below the VSYNC is the top border
            if (!VSYNC && CRCT.VSYNC)
            {
                VSYNC = true;
                FrameEnd = true;
                return;
            }

            // update HSYNC & VSYNC from CRCT
            HSYNC = CRCT.HSYNC;
            VSYNC = CRCT.VSYNC;

            // 2 GA cycles per pixel
            RendererFlipFlop = !RendererFlipFlop;
            if (RendererFlipFlop)
            {
                if (HSYNC)
                {
                    // HSYNC in progress
                    // output black
                }
                else if (!CRCT.DISPTMG)
                {
                    // outputting border colour
                    ScreenBuffer[RenderCounter++] = CPCHardwarePalette[ColourRegisters[16]];
                }
                else if (CRCT.DISPTMG)
                {
                    // outputting vid RAM
                    Random rnd = new Random();
                    ScreenBuffer[RenderCounter++] = CPCHardwarePalette[ColourRegisters[1]];
                }
                if (CRCT.VSYNC)
                {
                    RenderCounter = 40;
                }
            }
        }

        int displayCounter = 0;
        int hsyncCounter = 0;
        int borderCounter = 0;
        int vsyncCounter = 0;

        int lineCounter = 0;

        /// <summary>
        /// Handles interrupt generation
        /// </summary>
        private void InterruptGenerator()
        {
            if (HSYNC && !CRCT.HSYNC)
            {
                // falling edge of the HSYNC detected
                InterruptCounter++;

                if (CRCT.VSYNC)
                {
                    if (HSYNC_counter == 2)
                    {
                        // x2 HSYNC have happened during VSYNC
                        if (InterruptCounter >= 32)
                        {
                            // no interrupt
                            InterruptCounter = 0;
                        }
                        else if (InterruptCounter < 32)
                        {
                            // interrupt
                            InterruptRaised = true;
                            InterruptCounter = 0;
                        }

                        HSYNC_counter = 0;
                    }
                }

                if (InterruptCounter == 52)
                {
                    // gatearray should raise an interrupt
                    InterruptRaised = true;
                    InterruptCounter = 0;
                }
            }

            if (InterruptRaised)
            {
                // interrupt has been raised
                CPU.IFF1 = true;
                InterruptHoldCounter++;

                // the INT signal should be held low for 1.4us.
                // in gatearray cycles, this equates to 22.4
                // we will round down to 22 for emulation purposes
                if (InterruptHoldCounter >= 22)
                {
                    CPU.IFF1 = false;
                    InterruptRaised = false;
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// The gate array is clocked at 16Mhz
        /// It provides the CPU clock at 4Mhz
        /// The CRCT clock at 1Mhz
        /// The PSG clock at 1Mhz
        /// 
        /// Each time this method is called, the gatearray performs 16 cycles
        /// (equivalent to 4 uncontended CPU cycles)
        /// </summary>
        public void DoCycles()
        {
            // 16 gatearray cycles
            switch (ClockCounter)
            {
                // 0Mhz
                case 0:
                    // wait line inactive
                    WaitLine = false;

                    CRCT.ClockCycle();
                    //psg

                    // GA render cycle
                    DoCycle();

                    // CPU
                    DoConditionalCPUCycle();

                    // cycle the tape device
                    if (FDC == null || !FDC.FDD_IsDiskLoaded)
                        DATACORDER.TapeCycle();
                    break;
                // 4Mhz
                case 4:
                    // wait line active
                    WaitLine = true;

                    // GA render cycle
                    DoCycle();

                    // CPU
                    DoConditionalCPUCycle();

                    // cycle the tape device
                    if (FDC == null || !FDC.FDD_IsDiskLoaded)
                        DATACORDER.TapeCycle();
                    break;
                // 8Mhz
                case 8:
                    // wait line active
                    WaitLine = true;

                    // GA render cycle
                    DoCycle();

                    // CPU
                    DoConditionalCPUCycle();

                    // video fetch

                    // cycle the tape device
                    if (FDC == null || !FDC.FDD_IsDiskLoaded)
                        DATACORDER.TapeCycle();
                    break;
                // 12Mhz
                case 12:
                    // wait line active
                    WaitLine = true;

                    // GA render cycle
                    DoCycle();

                    // CPU
                    DoConditionalCPUCycle();

                    // video fetch

                    // cycle the tape device
                    if (FDC == null || !FDC.FDD_IsDiskLoaded)
                        DATACORDER.TapeCycle();
                    break;
                // all other GA cycles
                default:
                    // GA render cycle
                    DoCycle();
                    break;
            }

            AdvanceClock();
        }

        #endregion

        #region VideoLookups


        public struct PixelLookupTable
        {

        }

        /// <summary>
        /// Runs at the start of a frame in order to setup the 
        /// video buffer (in case the CRCT has changed anything)
        /// </summary>
        public void SetupVideo()
        {

        }

        #endregion



        #region IPortIODevice

        /// <summary>
        /// Device responds to an IN instruction
        /// </summary>
        /// <param name="port"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool ReadPort(ushort port, ref int result)
        {
            // gate array is OUT only
            return false;
        }

        /// <summary>
        /// Device responds to an OUT instruction
        /// </summary>
        /// <param name="port"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool WritePort(ushort port, int result)
        {
            BitArray portBits = new BitArray(BitConverter.GetBytes(port));
            BitArray dataBits = new BitArray(BitConverter.GetBytes((byte)result));

            // The gate array responds to port 0x7F
            bool accessed = !portBits[15];
            if (!accessed)
                return false;

            // Bit 9 and 8 of the data byte define the function to access
            if (!dataBits[6] && !dataBits[7])
            {
                // select pen
                PENR = (byte)result;
            }

            if (dataBits[6] && !dataBits[7])
            {
                // select colour for selected pen
                INKR = (byte)result;
            }

            if (!dataBits[6] && dataBits[7])
            {
                // select screen mode, ROM configuration and interrupt control
                RMR = (byte)result;
            }

            if (dataBits[6] && dataBits[7])
            {
                // RAM memory management
                RAMR = (byte)result;
            }

            return true;
        }

        #endregion

        #region IVideoProvider

        /// <summary>
        /// Video output buffer
        /// </summary>
        public int[] ScreenBuffer;

        private int _virtualWidth;
        private int _virtualHeight;
        private int _bufferWidth;
        private int _bufferHeight;

        public int BackgroundColor
        {
            get { return CPCHardwarePalette[16]; }
        }

        public int VirtualWidth
        {
            get { return _virtualWidth; }
            set { _virtualWidth = value; }
        }

        public int VirtualHeight
        {
            get { return _virtualHeight; }
            set { _virtualHeight = value; }
        }

        public int BufferWidth
        {
            get { return _bufferWidth; }
            set { _bufferWidth = value; }
        }

        public int BufferHeight
        {
            get { return _bufferHeight; }
            set { _bufferHeight = value; }
        }

        public int VsyncNumerator
        {
            get { return Z80ClockSpeed * 50; }
            set { }
        }

        public int VsyncDenominator
        {
            get { return Z80ClockSpeed; }
        }

        public int[] GetVideoBuffer()
        {
            /*
            Random rnd = new Random();
            for (int i = 0; i < BufferWidth * BufferHeight; i++)
            {
                ScreenBuffer[i] = CPCHardwarePalette[rnd.Next(0, CPCHardwarePalette.Length - 1)];
            }
            */
            //RenderCounter = 0;
            return ScreenBuffer;
        }

        protected void SetupScreenSize()
        {
            /*
             *  Rect Pixels:        Mode 0: 160×200 pixels with 16 colors (4 bpp)
                Square Pixels:      Mode 1: 320×200 pixels with 4 colors (2 bpp)
                Rect Pixels:        Mode 2: 640×200 pixels with 2 colors (1 bpp)
                Rect Pixels:        Mode 3: 160×200 pixels with 4 colors (2bpp) (this is not an official mode, but rather a side-effect of the hardware)
             * 
             * */

            // define maximum screen buffer size
            // to fit all possible resolutions, 640x400 should do it
            // therefore a scanline will take two buffer rows
            // and buffer columns will be:
            //  Mode 1: 2 pixels
            //  Mode 2: 1 pixel
            //  Mode 0: 4 pixels
            //  Mode 3: 4 pixels

            BufferWidth = 400; // 640;
            BufferHeight = 400;
            VirtualHeight = BufferHeight;
            VirtualWidth = BufferWidth;
            ScreenBuffer = new int[BufferWidth * BufferHeight];
            croppedBuffer = ScreenBuffer;
        }

        protected int[] croppedBuffer;

        #endregion

        #region Serialization

        public void SyncState(Serializer ser)
        {
            ser.BeginSection("GateArray");
            ser.SyncEnum("ChipType", ref ChipType);
            ser.Sync("_PENR", ref _PENR);
            ser.Sync("_INKR", ref _INKR);
            ser.Sync("_RMR", ref _RMR);
            ser.Sync("_RAMR", ref _RAMR);
            ser.Sync("ColourRegisters", ref ColourRegisters, false);
            ser.Sync("CurrentPen", ref CurrentPen);
            ser.Sync("ClockCounter", ref ClockCounter);
            ser.Sync("FrameClock", ref FrameClock);
            ser.Sync("WaitLine", ref WaitLine);
            ser.Sync("_interruptCounter", ref _interruptCounter);
            ser.Sync("ScreenMode", ref ScreenMode);
            ser.Sync("LatchedMode", ref LatchedMode);
            ser.Sync("HSYNC", ref HSYNC);
            ser.Sync("HSYNC_falling", ref HSYNC_falling);
            ser.Sync("HSYNC_counter", ref HSYNC_counter);
            ser.Sync("VSYNC", ref VSYNC);
            ser.Sync("InterruptRaised", ref InterruptRaised);
            ser.Sync("InterruptHoldCounter", ref InterruptHoldCounter);
            ser.Sync("RendererFlipFlop", ref RendererFlipFlop);
            ser.Sync("_MA", ref _MA);
            ser.EndSection();

            /*
             * /// <summary>
        /// Is set when an initial HSYNC is seen from the CRCT
        /// On real hardware interrupt generation is based on the falling edge of the HSYNC signal
        /// So in this emulation, once the falling edge is detected, processing happens
        /// </summary>
        private bool ;

        /// <summary>
        /// Used to count HSYNCs during a VSYNC
        /// </summary>
        private int ;
             * */
        }

        #endregion

        #region Enums

        public enum GateArrayType
        {
            /// <summary>
            /// CPC 464
            /// The first version of the Gate Array is the 40007 and was released with the CPC 464
            /// </summary>
            Amstrad40007,
            /// <summary>
            /// CPC 664
            /// Later, the CPC 664 came out fitted with the 40008 version (and at the same time, the CPC 464 was also upgraded with this version). 
            /// This version is pinout incompatible with the 40007 (that's why the upgraded 464 of this period have two Gate Array slots on the motherboard, 
            /// one for a 40007 and one for a 40008)
            /// </summary>
            Amstrad40008,
            /// <summary>
            /// CPC 6128
            /// The CPC 6128 was released with the 40010 version (and the CPC 464 and 664 manufactured at that time were also upgraded to this version). 
            /// The 40010 is pinout compatible with the previous 40008
            /// </summary>
            Amstrad40010,
            /// <summary>
            /// Costdown CPC
            /// In the last serie of CPC 464 and 6128 produced by Amstrad in 1988, a small ASIC chip have been used to reduce the manufacturing costs. 
            /// This ASIC emulates the Gate Array, the PAL and the CRTC 6845. And no, there is no extra features like on the Amstrad Plus. 
            /// The only noticeable difference seems to be about the RGB output levels which are not exactly the same than those produced with a real Gate Array
            /// </summary>
            Amstrad40226,
            /// <summary>
            /// Plus & GX-4000
            /// All the Plus range is built upon a bigger ASIC chip which is integrating many features of the classic CPC (FDC, CRTC, PPI, Gate Array/PAL) and all 
            /// the new Plus specific features. The Gate Array on the Plus have a new register, named RMR2, to expand the ROM mapping functionnalities of the machine. 
            /// This register requires to be unlocked first to be available. And finally, the RGB levels produced by the ASIC on the Plus are noticeably differents
            /// </summary>
            Amstrad40489,
        }

        #endregion
    }
}
