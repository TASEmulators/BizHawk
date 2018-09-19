using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using System;
using System.Collections;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// Cathode Ray Tube Controller Chip - 6845
    /// http://www.cpcwiki.eu/index.php/CRTC
    /// https://web.archive.org/web/20170501112330/http://www.grimware.org/doku.php/documentations/devices/crtc
    /// </summary>
    public class CRCT_6845 : IPortIODevice
    {
        #region Devices

        private CPCBase _machine { get; set; }
        private CRCTType ChipType;

        #endregion

        #region CallBacks

        public delegate void CallBack();

        private CallBack HSYNC_Callbacks;
        private CallBack VSYNC_Callbacks;

        public void AttachVSYNCCallback(CallBack vCall)
        {
            VSYNC_Callbacks += vCall;
        }

        public void AttachHSYNCCallback(CallBack hCall)
        {
            HSYNC_Callbacks += hCall;
        }

        #endregion

        #region Construction

        public CRCT_6845(CRCTType chipType, CPCBase machine)
        {
            _machine = machine;
            ChipType = chipType;
            Reset();
        }

        private const int WRITE = 0;
        private const int READ = 1;

        #endregion

        #region Public Lines

        /// <summary>
        /// Denotes that HSYNC is active
        /// </summary>
        public bool HSYNC = false;

        /// <summary>
        /// Denotes that VSYNC is active
        /// </summary>
        public bool VSYNC = false;

        /// <summary>
        /// TRUE:   bits outputted to screen from video RAM
        /// FALSE:  current border colour is outputted
        /// </summary>
        public bool DISPTMG = true;

        /// <summary>
        /// 16-bit memory address lines
        /// The gate array uses this to grab the correct bits from video RAM
        /// </summary>
        public short MA;

        /// <summary>
        /// Vertical Character Count
        /// </summary>
        public int VCC;

        /// <summary>
        /// Vertical Scanline Count (within the current vertical character)
        /// </summary>
        public int VLC;

        #endregion

        #region Public Lookups

        /*
         *  These are not accessible directlyon real hardware
         *  It just makes screen generation easier to have these accessbile from the gate array
         */

        /// <summary>
        /// The total frame width (in characters)
        /// </summary>
        public int FrameWidth
        {
            get
            {
                return (int)Regs[HOR_TOTAL] + 1;
            }
        }

        /// <summary>
        /// The total frame height (in scanlines)
        /// </summary>
        public int FrameHeight
        {
            get
            {
                return ((int)Regs[VER_TOTAL] + 1) * ((int)Regs[MAX_RASTER_ADDR] + 1);
            }
        }

        /// <summary>
        /// The total frame height (in scanlines)
        /// </summary>
        public int FrameHeightInChars
        {
            get
            {
                return ((int)Regs[VER_TOTAL] + 1);
            }
        }

        /// <summary>
        /// The width of the display area (in characters)
        /// </summary>
        public int DisplayWidth
        {
            get
            {
                return (int)Regs[HOR_DISPLAYED];
            }
        }

        /// <summary>
        /// The width of the display area (in scanlines)
        /// </summary>
        public int DisplayHeight
        {
            get
            {
                return (int)Regs[VER_DISPLAYED] * ((int)Regs[MAX_RASTER_ADDR] + 1);
            }
        }

        /// <summary>
        /// The width of the display area (in scanlines)
        /// </summary>
        public int DisplayHeightInChars
        {
            get
            {
                return (int)Regs[VER_DISPLAYED];
            }
        }

        /// <summary>
        /// The character at which to start HSYNC
        /// </summary>
        public int HorizontalSyncPos
        {
            get
            {
                return (int)Regs[HOR_SYNC_POS];
            }
        }

        /// <summary>
        /// Width (in characters) of the HSYNC
        /// </summary>
        public int HorizontalSyncWidth
        {
            get
            {
                return HSYNCWidth;
            }
        }

        /// <summary>
        /// The vertical scanline at which to start VSYNC
        /// </summary>
        public int VerticalSyncPos
        {
            get
            {
                return (int)Regs[VER_SYNC_POS] * ((int)Regs[MAX_RASTER_ADDR] + 1);
            }
        }

        /// <summary>
        /// Height (in scanlines) of the VSYNC
        /// </summary>
        public int VerticalSyncHeight
        {
            get
            {
                return VSYNCWidth; // * ((int)Regs[MAX_RASTER_ADDR] + 1);
            }
        }

        /// <summary>
        /// The number of scanlines in one character (MAXRASTER)
        /// </summary>
        public int ScanlinesPerCharacter
        {
            get
            {
                return (int)Regs[MAX_RASTER_ADDR] + 1;
            }
        }

        /// <summary>
        /// Returns the starting video page address as specified within R12
        /// </summary>
        public int VideoPageBase
        {
            get
            {
                if (!Regs[12].Bit(4) && Regs[12].Bit(5))
                    return 0x8000;

                if (Regs[12].Bit(4) && !Regs[12].Bit(5))
                    return 0x4000;

                if (!Regs[12].Bit(4) && !Regs[12].Bit(5))
                    return 0x0000;

                return 0xC000;
            }
        }

        public int DStartHigh
        { get { return Regs[DISP_START_ADDR_H]; } }

        public int DStartLow
        { get { return Regs[DISP_START_ADDR_L]; } }

        /// <summary>
        /// Returns the video buffer size as specified within R12
        /// </summary>
        public int VideoBufferSize
        {
            get
            {
                if (Regs[12].Bit(3) && Regs[12].Bit(2))
                    return 0x8000;

                return 0x4000;
            }
        }

        /// <summary>
        /// The offset into vRAM
        /// </summary>
        public int VideoRAMOffset
        {
            get
            {
                ushort combined = (ushort)(Regs[12] << 8 | Regs[13]);
                int offset = combined & 0x3ff;                
                return offset;
            }
        }


        /* Easier memory functions */

        /// <summary>
        /// The current byte address
        /// </summary>
        public ushort CurrentByteAddress;

        /// <summary>
        /// ByteCounter
        /// </summary>
        public int ByteCounter;

        /// <summary>
        /// Set at every HSYNC
        /// </summary>
        public int LatchedRAMOffset;

        /// <summary>
        /// set at every HSYNC
        /// </summary>
        public int LatchedRAMStartAddress;

        /// <summary>
        /// set at every HSYNC
        /// </summary>
        public int LatchedScreenWidthBytes;

        #endregion

        #region Internal Registers and State

        /*
        Index	Register Name	                    Range	    CPC Setting	Notes
        0	    Horizontal Total	                00000000	63	        Width of the screen, in characters. Should always be 63 (64 characters). 1 character == 1μs.
        1	    Horizontal Displayed	            00000000	40	        Number of characters displayed. Once horizontal character count (HCC) matches this value, DISPTMG is set to 1.
        2	    Horizontal Sync Position	        00000000	46	        When to start the HSync signal.
        3	    Horizontal and Vertical Sync Widths	VVVVHHHH	128+14	    HSync pulse width in characters (0 means 16 on some CRTC), should always be more than 8; VSync width in scan-lines. (0 means 16 on some CRTC. Not present on all CRTCs, fixed to 16 lines on these)
        4	    Vertical Total	                    x0000000	38	        Height of the screen, in characters.
        5	    Vertical Total Adjust	            xxx00000	0	        Measured in scanlines, can be used for smooth vertical scrolling on CPC.
        6	    Vertical Displayed	                x0000000	25	        Height of displayed screen in characters. Once vertical character count (VCC) matches this value, DISPTMG is set to 1.
        7	    Vertical Sync position	            x0000000	30	        When to start the VSync signal, in characters.
        8	    Interlace and Skew	                xxxxxx00	0	        00: No interlace; 01: Interlace Sync Raster Scan Mode; 10: No Interlace; 11: Interlace Sync and Video Raster Scan Mode
        9	    Maximum Raster Address	            xxx00000	7	        Maximum scan line address on CPC can hold between 0 and 7, higher values' upper bits are ignored
        10	    Cursor Start Raster	                xBP00000	0	        Cursor not used on CPC. B = Blink On/Off; P = Blink Period Control (Slow/Fast). Sets first raster row of character that cursor is on to invert.
        11	    Cursor End Raster	                xxx00000	0	        Sets last raster row of character that cursor is on to invert
        12	    Display Start Address (High)	    xx000000	32
        13	    Display Start Address (Low)	        00000000	0	        Allows you to offset the start of screen memory for hardware scrolling, and if using memory from address &0000 with the firmware.
        14	    Cursor Address (High)	            xx000000	0
        15	    Cursor Address (Low)	            00000000	0
        16	    Light Pen Address (High)	        xx000000		        Read Only
        17	    Light Pen Address (Low)	            00000000		        Read Only
        */
        /// <summary>
        /// 6845 internal registers
        /// </summary>
        private byte[] Regs = new byte[18];

        // CRTC Register constants
        /// <summary>
        /// R0:     Horizontal total character number
        /// Unit:   Character
        /// Notes:  Defines the width of a scanline
        /// </summary>     
        public const int HOR_TOTAL = 0;
        /// <summary>
        /// R1:     Horizontal displayed character number
        /// Unit:   Character
        /// Notes:  Defines when DISPEN goes OFF on the scanline
        /// </summary>     
        public const int HOR_DISPLAYED = 1;
        /// <summary>
        /// R2:     Position of horizontal sync. pulse
        /// Unit:   Character
        /// Notes:  Defines when the HSync goes ON on the scanline
        /// </summary>
        public const int HOR_SYNC_POS = 2;
        /// <summary>
        /// R3:     Width of horizontal/vertical sync. pulses
        /// Unit:   Character
        /// Notes:  VSync width can only be changed on type 3 and 4
        /// </summary>
        public const int HOR_AND_VER_SYNC_WIDTHS = 3;
        /// <summary>
        /// R4:     Vertical total Line character number
        /// Unit:   Character
        /// Notes:  Defines the height of a screen
        /// </summary>
        public const int VER_TOTAL = 4;
        /// <summary>
        /// R5:     Vertical raster adjust
        /// Unit:   Scanline
        /// Notes:  Defines additionnal scanlines at the end of a screen
        ///         can be used for smooth vertical scrolling on CPC
        /// </summary>
        public const int VER_TOTAL_ADJUST = 5;
        /// <summary>
        /// R6:     Vertical displayed character number
        /// Unit:   Character
        /// Notes:  Define when DISPEN remains OFF until a new screen starts
        ///         Height of displayed screen in characters (Once vertical character count (VCC) matches this value, DISPTMG is set to 1)
        /// </summary>
        public const int VER_DISPLAYED = 6;
        /// <summary>
        /// R7:     Position of vertical sync. pulse
        /// Unit:   Character
        /// Notes:  Define when the VSync goes ON on a screen
        /// </summary>
        public const int VER_SYNC_POS = 7;
        /// <summary>
        /// R8:     Interlaced mode
        /// Unit:   
        /// Notes:  00: No interlace; 01: Interlace Sync Raster Scan Mode; 10: No Interlace; 11: Interlace Sync and Video Raster Scan Mode
        ///         (crct type specific)
        /// </summary>
        public const int INTERLACE_SKEW = 8;
        /// <summary>
        /// R9:     Maximum raster
        /// Unit:   Scanline
        /// Notes:  Defines the height of a CRTC-Char in scanlines
        /// </summary>
        public const int MAX_RASTER_ADDR = 9;
        /// <summary>
        /// R10:    Cursor start raster
        /// Unit:   
        /// Notes:  Cursor not used on CPC.
        ///         (xBP00000)
        ///         B = Blink On/Off; 
        ///         P = Blink Period Control (Slow/Fast). 
        ///         Sets first raster row of character that cursor is on to invert
        /// </summary>
        public const int CUR_START_RASTER = 10;
        /// <summary>
        /// R11:    Cursor end
        /// Unit:   
        /// Notes:  Sets last raster row of character that cursor is on to invert
        /// </summary>
        public const int CUR_END_RASTER = 11;
        /// <summary>
        /// R12:    Display Start Address (High)
        /// Unit:   
        /// Notes:  Define the MSB of MA when a CRTC-screen starts
        /// </summary>
        public const int DISP_START_ADDR_H = 12;
        /// <summary>
        /// R13:    Display Start Address (Low)
        /// Unit:   
        /// Notes:  Define the LSB of MA when a CRTC-screen starts
        ///         Allows you to offset the start of screen memory for hardware scrolling, and if using memory from address &0000 with the firmware.
        /// </summary>
        public const int DISP_START_ADDR_L = 13;
        /// <summary>
        /// R14:    Cursor Address (High)
        /// Unit:   
        /// Notes:  Useless on the Amstrad CPC/Plus (text-mode is not wired)
        /// </summary>
        public const int CUR_ADDR_H = 14;
        /// <summary>
        /// R15:    Cursor Address (Low)
        /// Unit:   
        /// Notes:  Useless on the Amstrad CPC/Plus (text-mode is not wired)
        /// </summary>
        public const int CUR_ADDR_L = 15;
        /// <summary>
        /// R16:    Light Pen Address (High)
        /// Unit:   
        /// Notes:  Hold the MSB of the cursor position when the lightpen was ON
        /// </summary>
        public const int LPEN_ADDR_H = 16;
        /// <summary>
        /// R17:    Light Pen Address (Low)
        /// Unit:   
        /// Notes:  Hold the LSB of the cursor position when the lightpen was ON
        /// </summary>
        public const int LPEN_ADDR_L = 17;

        /// <summary>
        /// The currently selected register
        /// </summary>
        private int SelectedRegister;

        /// <summary>
        /// CPC register default values
        /// Taken from https://web.archive.org/web/20170501112330/http://www.grimware.org/doku.php/documentations/devices/crtc
        /// http://www.cantrell.org.uk/david/tech/cpc/cpc-firmware/firmware.pdf
        /// (The defaults values given here are those programmed by the firmware ROM after a cold/warm boot of the CPC/Plus)
        /// </summary>
        private byte[] RegDefaults = new byte[] { 63, 40, 46, 112, 38, 0, 25, 30, 0, 7, 0, 0, 48, 0, 192, 7, 0, 0 };

        /// <summary>
        /// Register masks
        /// 0 = WRITE
        /// 1 = READ
        /// </summary>
        private byte[] CPCMask = new byte[] { 255, 255, 255, 255, 127, 31, 127, 126, 3, 31, 31, 31, 63, 255, 63, 255, 63, 255 };

        /// <summary>
        /// Horizontal Character Count
        /// </summary>
        private int HCC;

        /// <summary>
        /// Internal cycle counter
        /// </summary>
        private int CycleCounter;

        /// <summary>
        /// Signs that we have finished the last character row
        /// </summary>
        private bool EndOfScreen;

        /// <summary>
        /// HSYNC pulse width (in characters)
        /// </summary>
        private int HSYNCWidth;

        /// <summary>
        /// Internal HSYNC counter
        /// </summary>
        private int HSYNCCounter;

        /// <summary>
        /// VSYNC pulse width (in characters)
        /// </summary>
        private int VSYNCWidth;

        /// <summary>
        /// Internal VSYNC counter
        /// </summary>
        private int VSYNCCounter;

        #endregion

        #region Public Methods

        public void ClockCycle()
        {
            CheckHSYNCOff();           

            HCC++;

            if (HCC == Regs[HOR_TOTAL] + 1)
            {
                // end of scanline
                HCC = 0;                
                
                if (VSYNCCounter > 0)
                {
                    VSYNCCounter--;
                    if (VSYNCCounter == 0)
                    {
                        VSYNC = false;
                    }
                }

                VLC++;

                if (VLC == Regs[MAX_RASTER_ADDR] + 1)
                {
                    // end of rasterline
                    VLC = 0;
                    VCC++;

                    if (VCC == Regs[VER_TOTAL] + 1)
                    {
                        // end of screen
                        VCC = 0;
                    }

                    if (VCC == Regs[VER_SYNC_POS] && !VSYNC)
                    {
                        VSYNC = true;
                        VSYNCCounter = VSYNCWidth;
                        VSYNC_Callbacks();
                    }
                }
            }
            else
            {
                // still on the current scanline
                if (HCC == Regs[HOR_SYNC_POS] && !HSYNC)
                {
                    HSYNC = true;
                    HSYNCCounter = HSYNCWidth;
                    HSYNC_Callbacks();
                    ByteCounter = 0;
                }

                if (HCC >= Regs[HOR_DISPLAYED] + 1 || VCC >= Regs[VER_DISPLAYED])
                {
                    DISPTMG = false;
                }
                else
                {
                    DISPTMG = true;

                    var line = VCC;
                    var row = VLC;
                    var addrX = (LatchedRAMOffset * 2) + ((VCC * LatchedScreenWidthBytes) & 0x7ff) + ByteCounter;
                    // remove artifacts caused by certain hardware scrolling addresses
                    addrX &= 0x7ff;
                    var addrY = LatchedRAMStartAddress + (2048 * VLC);

                    //var addr = VideoPageBase + (line * (0x50)) + (row * 0x800) + (ByteCounter);
                    CurrentByteAddress = (ushort)(addrX + addrY);

                    ByteCounter += 2;
                }
            }
        }

        private void CheckHSYNCOff()
        {
            if (HSYNCCounter > 0)
            {
                HSYNCCounter--;
                if (HSYNCCounter == 0)
                {
                    HSYNC = false;
                }
            }
        }

        /// <summary>
        /// Runs a CRCT clock cycle
        /// This should be called at 1Mhz / 1us / every 4 uncontended CPU t-states
        /// </summary>
        public void ClockCycle2()
        {
            if (HSYNC)
            {
                // HSYNC in progress
                HSYNCCounter++;

                ByteCounter = 0;

                if (HSYNCCounter >= HSYNCWidth)
                {
                    // end of HSYNC
                    HSYNCCounter = 0;
                    HSYNC = false;
                }
            }

            if (HSYNC && HSYNCCounter == 1)
            {

            }

            // move one horizontal character
            HCC++;

            // check for DISPTMG
            if (HCC >= Regs[HOR_DISPLAYED] + 1)
            {
                DISPTMG = false;
            }
            else if (VCC >= Regs[VER_DISPLAYED])
            {
                DISPTMG = false;
            }
            else
            {
                DISPTMG = true;

                var line = VCC;
                var row = VLC;
                var addrX = (LatchedRAMOffset * 2) + ((VCC * LatchedScreenWidthBytes) & 0x7ff) + ByteCounter;
                // remove artifacts caused by certain hardware scrolling addresses
                addrX &= 0x7ff;
                var addrY = LatchedRAMStartAddress + (2048 * VLC);

                //var addr = VideoPageBase + (line * (0x50)) + (row * 0x800) + (ByteCounter);
                CurrentByteAddress = (ushort)(addrX + addrY);

                ByteCounter += 2;
            }

            // check for the end of the current scanline
            if (HCC == Regs[HOR_TOTAL] + 1)
            {
                // end of the current scanline
                HCC = 0;


                if (ChipType == (CRCTType)1 && VLC <= Regs[MAX_RASTER_ADDR])
                {
                    // https://web.archive.org/web/20170501112330/http://www.grimware.org/doku.php/documentations/devices/crtc
                    // The MA is reloaded with the value from R12 and R13 when VCC=0 and VLC=0 (that's when a new CRTC screen begin). 
                    // However, CRTC Type 1 keep updating the MA on every new scanline while VCC=0 (and VLC=<R9).
                    MA = (short)(((Regs[DISP_START_ADDR_H]) & 0xff) << 8 | (Regs[DISP_START_ADDR_L]) & 0xff);
                }

                if (VSYNC)
                {
                    // VSYNC in progress
                    VSYNCCounter++;

                    if (VSYNCCounter == VSYNCWidth)
                    {
                        // end of VSYNC
                        VSYNCCounter = 0;
                        VSYNC = false;
                    }
                }

                // increment line counter
                VLC++;

                if (EndOfScreen)
                {
                    // we have finished the last character row
                    // are their additional scanlines specified?
                    if (VLC < Regs[VER_TOTAL_ADJUST] + 1)
                    {
                        // still doing extra scanlines
                    }
                    else
                    {
                        // finished doing extra scanlines
                        EndOfScreen = false;
                        VLC = 0;
                        VCC = 0;

                        // populate MA address
                        MA = (short)(((Regs[DISP_START_ADDR_H]) & 0xff) << 8 | (Regs[DISP_START_ADDR_L]) & 0xff);
                    }
                }
                else
                {
                    // check for the completion of a vertical character
                    if (VLC == Regs[MAX_RASTER_ADDR] + 1)
                    {
                        // vertical character line has been completed
                        // increment vcc and reset vlc
                        VCC++;
                        VLC = 0;
                    }

                    // end of screen?
                    if (VCC >= Regs[VER_TOTAL] + 1)
                    {
                        VCC = 0;
                        EndOfScreen = true;
                    }
                }

                // does VSYNC need to be raised?
                if (!VSYNC)
                {
                    if (VCC == Regs[VER_SYNC_POS])
                    {
                        VSYNC = true;
                        VSYNCCounter = 0;
                        VSYNC_Callbacks();
                    }
                }
            }
            else
            {
                // still processing a scanline
                // check whether HSYNC needs raising
                if (!HSYNC)
                {
                    if (HCC == Regs[HOR_SYNC_POS])
                    {
                        HSYNC = true;
                        HSYNCCounter = 0;
                        HSYNC_Callbacks();
                        lineCounter++;

                        LatchedRAMStartAddress = VideoPageBase;
                        LatchedRAMOffset = VideoRAMOffset;
                        LatchedScreenWidthBytes = DisplayWidth * 2;

                    }
                }
            }
        }

        /// <summary>
        /// Runs a CRCT clock cycle
        /// This should be called at 1Mhz / 1us / every 4 uncontended CPU t-states
        /// </summary>
        public void ClockCycle1()
        {
            // HSYNC processing
            if (HSYNCCounter > 0)
            {
                HSYNCCounter--;
                if (HSYNCCounter == 0)
                    HSYNC = false;
            }

            HCC++;

            if (HCC == FrameWidth)
            {
                // we have finished the current scanline
                HCC = 0;

                if (VSYNCCounter > 0)
                {
                    VSYNCCounter--;
                    if (VSYNCCounter == 0)
                        VSYNC = false;
                }

                VLC++;

                if (VLC == ScanlinesPerCharacter)
                {
                    // completed a vertical character
                    VLC = 0;
                    VCC++;

                    if (VCC == FrameHeight)
                    {
                        // screen has completed
                        VCC = 0;
                    }
                }

                // check whether VSYNC should be raised
                if (VCC == VerticalSyncPos && !VSYNC)
                {
                    VSYNC = true;
                    VSYNCCounter = VSYNCWidth;
                    VSYNC_Callbacks();
                }
            }
            else if (HCC == HorizontalSyncPos && !HSYNC)
            {
                // start of HSYNC period
                HSYNC = true;
                HSYNCCounter = HSYNCWidth;
                HSYNC_Callbacks();
            }

            // DISPTMG
            if (HCC >= Regs[HOR_DISPLAYED] || VCC >= Regs[VER_DISPLAYED])
            {
                DISPTMG = false;
            }
            else
            {
                DISPTMG = true;
            }
            /*
            // check for DISPTMG
            if (HCC >= Regs[HOR_DISPLAYED] + 1)
            {
                DISPTMG = false;
            }
            else if (VCC >= Regs[VER_DISPLAYED])
            {
                DISPTMG = false;
            }
            else
            {
                DISPTMG = true;
            }
            */
        }

        public int lineCounter = 0;

        

        /// <summary>
        /// Resets the chip
        /// </summary>
        public void Reset()
        {
            // set regs to default
            for (int i = 0; i < 18; i++)
                Regs[i] = RegDefaults[i];

            SelectedRegister = 0;

            // populate initial MA address
            MA = (short)(((Regs[DISP_START_ADDR_H]) & 0xff) << 8 | (Regs[DISP_START_ADDR_L]) & 0xff);

            // updates widths
            UpdateWidths();

            HSYNC = false;
            VSYNC = false;

            HSYNCCounter = 0;
            VSYNCCounter = 0;

            HCC = 0;
            VCC = 0;
            VLC = 0;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Selects a register
        /// </summary>
        /// <param name="data"></param>
        private void RegisterSelect(int data)
        {
            SelectedRegister = data & 0x1F;
        }

        /*
                RegIdx	Register Name	            Type
                                                    0	        1	        2	        3	                4
                0	    Horizontal Total	        Write Only	Write Only	Write Only	(note 2)	        (note 3)
                1	    Horizontal Displayed	    Write Only	Write Only	Write Only	(note 2)	        (note 3)
                2	    Horizontal Sync Position	Write Only	Write Only	Write Only	(note 2)	        (note 3)
                3	    H and V Sync Widths	        Write Only	Write Only	Write Only	(note 2)	        (note 3)
                4	    Vertical Total	            Write Only	Write Only	Write Only	(note 2)	        (note 3)
                5	    Vertical Total Adjust	    Write Only	Write Only	Write Only	(note 2)	        (note 3)
                6	    Vertical Displayed	        Write Only	Write Only	Write Only	(note 2)	        (note 3)
                7	    Vertical Sync position	    Write Only	Write Only	Write Only	(note 2)	        (note 3)
                8	    Interlace and Skew	        Write Only	Write Only	Write Only	(note 2)	        (note 3)
                9	    Maximum Raster Address	    Write Only	Write Only	Write Only	(note 2)	        (note 3)
                10	    Cursor Start Raster	        Write Only	Write Only	Write Only	(note 2)	        (note 3)
                11	    Cursor End Raster	        Write Only	Write Only	Write Only	(note 2)	        (note 3)
                12	    Disp. Start Address (High)	Read/Write	Write Only	Write Only	Read/Write (note 2)	(note 3)
                13	    Disp. Start Address (Low)	Read/Write	Write Only	Write Only	Read/Write (note 2)	(note 3)
                14	    Cursor Address (High)	    Read/Write	Read/Write	Read/Write	Read/Write (note 2)	(note 3)
                15	    Cursor Address (Low)	    Read/Write	Read/Write	Read/Write	Read/Write (note 2)	(note 3)
                16	    Light Pen Address (High)	Read Only	Read Only	Read Only	Read Only (note 2)	(note 3)
                17	    Light Pen Address (Low)	    Read Only	Read Only	Read Only	Read Only (note 2)	(note 3)

                1. On type 0 and 1, if a Write Only register is read from, "0" is returned.
                2. See the document "Extra CPC Plus Hardware Information" for more details.
                3. CRTC type 4 is the same as CRTC type 3. The registers also repeat as they do on the type 3.
        */

        /// <summary>
        /// Writes to the currently selected register
        /// </summary>
        /// <param name="data"></param>
        private void WriteRegister(int data)
        {
            // 16 and 17 are read only registers on all types
            if (SelectedRegister == 16 || SelectedRegister == 17)
                return;

            // non existing registers
            if (SelectedRegister > 17)
                return;

            if (SelectedRegister == DISP_START_ADDR_L)
            {

            }

            if (SelectedRegister == DISP_START_ADDR_H)
            {

            }

            if (SelectedRegister == HOR_TOTAL)
            {
                // always 63
                if (data != 63)
                    return;
            }

            if (SelectedRegister == 1)
            {
                var d = data;
            }

            Regs[SelectedRegister] = (byte)(data & CPCMask[SelectedRegister]);

            if (SelectedRegister == HOR_AND_VER_SYNC_WIDTHS)
            {
                UpdateWidths();
            }
        }

        /// <summary>
        /// Reads from the currently selected register
        /// </summary>
        /// <param name="data"></param>
        private bool ReadRegister(ref int data)
        {
            bool addressed = false;
            switch (SelectedRegister)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                case 10:
                case 11:
                    if ((int)ChipType == 0 || (int)ChipType == 1)
                    {
                        addressed = true;
                        data = 0;
                    }                        
                    break;
                case 12:
                case 13:
                    addressed = true;
                    if ((int)ChipType == 0)
                        data = Regs[SelectedRegister];
                    else if ((int)ChipType == 1)
                        data = 0;
                    break;
                case 14:
                case 15:
                case 16:
                case 17:
                    addressed = true;
                    data = Regs[SelectedRegister];
                    break;

                default:
                    // registers 18-31 read as 0, on type 0 and 2. registers 18-30 read as 0 on type1, register 31 reads as 0x0ff.
                    if (SelectedRegister >= 18 && SelectedRegister <= 30)
                    {
                        switch ((int)ChipType)
                        {
                            case 0:
                            case 2:
                            case 1:
                                addressed = true;
                                data = 0;
                                break;
                        }
                    }
                    else if (SelectedRegister == 31)
                    {
                        if ((int)ChipType == 1)
                        {
                            addressed = true;
                            data = 0x0ff;
                        }
                        else if ((int)ChipType == 0 || (int)ChipType == 2)
                        {
                            addressed = true;
                            data = 0;
                        }
                    }
                    break;
            }

            return addressed;
        }

        /// <summary>
        /// Reads from the status register
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool ReadStatus(ref int data)
        {
            bool addressed = false;
            switch ((int)ChipType)
            {
                case 1:
                    // read status
                    //todo!!
                    addressed = true;
                    break;
                case 0:
                case 2:
                    // status reg not available
                    break;
                case 3:
                case 4:
                    // read from internal register instead
                    addressed = ReadRegister(ref data);
                    break;
            }
            return addressed;
        }

        /// <summary>
        /// Updates the V and H SYNC widths
        /// </summary>
        private void UpdateWidths()
        {
            switch (ChipType)
            {
                case CRCTType.HD6845S:
                    // Bits 7..4 define Vertical Sync Width. If 0 is programmed this gives 16 lines of VSYNC. Bits 3..0 define Horizontal Sync Width. 
                    // If 0 is programmed no HSYNC is generated.
                    HSYNCWidth = (Regs[HOR_AND_VER_SYNC_WIDTHS] >> 0) & 0x0F;
                    VSYNCWidth = (Regs[HOR_AND_VER_SYNC_WIDTHS] >> 4) & 0x0F;
                    break;
                case CRCTType.UM6845R:
                    // Bits 7..4 are ignored. Vertical Sync is fixed at 16 lines. Bits 3..0 define Horizontal Sync Width. If 0 is programmed no HSYNC is generated.
                    HSYNCWidth = (Regs[HOR_AND_VER_SYNC_WIDTHS] >> 0) & 0x0F;
                    VSYNCWidth = 16;
                    break;
                case CRCTType.MC6845:
                    // Bits 7..4 are ignored. Vertical Sync is fixed at 16 lines. Bits 3..0 define Horizontal Sync Width. If 0 is programmed this gives a HSYNC width of 16.
                    HSYNCWidth = (Regs[HOR_AND_VER_SYNC_WIDTHS] >> 0) & 0x0F;
                    if (HSYNCWidth == 0)
                        HSYNCWidth = 16;
                    VSYNCWidth = 16;
                    break;
                case CRCTType.AMS40489:
                case CRCTType.AMS40226:
                    // Bits 7..4 define Vertical Sync Width. If 0 is programmed this gives 16 lines of VSYNC.Bits 3..0 define Horizontal Sync Width. 
                    // If 0 is programmed this gives a HSYNC width of 16.
                    HSYNCWidth = (Regs[HOR_AND_VER_SYNC_WIDTHS] >> 0) & 0x0F;
                    VSYNCWidth = (Regs[HOR_AND_VER_SYNC_WIDTHS] >> 4) & 0x0F;
                    if (HSYNCWidth == 0)
                        HSYNCWidth = 16;
                    if (VSYNCWidth == 0)
                        VSYNCWidth = 16;
                    break;
            }
        }

        #endregion

        #region PortIODevice

        /*
            #BCXX	%x0xxxx00 xxxxxxxx	6845 CRTC Index	                        -	    Write
            #BDXX	%x0xxxx01 xxxxxxxx	6845 CRTC Data Out	                    -	    Write
            #BEXX	%x0xxxx10 xxxxxxxx	6845 CRTC Status (as far as supported)	Read	-
            #BFXX	%x0xxxx11 xxxxxxxx	6845 CRTC Data In (as far as supported)	Read	-
         */

        /// <summary>
        /// Device responds to an IN instruction
        /// </summary>
        /// <param name="port"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool ReadPort(ushort port, ref int result)
        {
            byte portUpper = (byte)(port >> 8);
            byte portLower = (byte)(port & 0xff);

            bool accessed = false;

            // The 6845 is selected when bit 14 of the I/O port address is set to "0"
            if (portUpper.Bit(6))
                return accessed;

            // Bit 9 and 8 of the I/O port address define the function to access
            if (portUpper.Bit(1) && !portUpper.Bit(0))
            {
                // read status register
                accessed = ReadStatus(ref result);
            }
            else if ((portUpper & 3) == 3)
            {
                // read data register
                accessed = ReadRegister(ref result);
            }
            else
            {
                result = 0;
            }

            return accessed;
        }

        /// <summary>
        /// Device responds to an OUT instruction
        /// </summary>
        /// <param name="port"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool WritePort(ushort port, int result)
        {
            byte portUpper = (byte)(port >> 8);
            byte portLower = (byte)(port & 0xff);

            bool accessed = false;

            // The 6845 is selected when bit 14 of the I/O port address is set to "0"
            if (portUpper.Bit(6))
                return accessed;

            var func = portUpper & 3;

            switch (func)
            {
                // reg select
                case 0:
                    RegisterSelect(result);
                    break;

                // data write
                case 1:
                    WriteRegister(result);
                    break;
            }

            return accessed;
        }

        #endregion

        #region Serialization

        public void SyncState(Serializer ser)
        {
            ser.BeginSection("CRTC");
            ser.SyncEnum("ChipType", ref ChipType);
            ser.Sync("HSYNC", ref HSYNC);
            ser.Sync("VSYNC", ref VSYNC);
            ser.Sync("DISPTMG", ref DISPTMG);
            ser.Sync("MA", ref MA);
            ser.Sync("CurrentByteAddress", ref CurrentByteAddress);
            ser.Sync("ByteCounter", ref ByteCounter);
            ser.Sync("Regs", ref Regs, false);
            ser.Sync("SelectedRegister", ref SelectedRegister);
            ser.Sync("HCC", ref HCC);
            ser.Sync("VCC", ref VCC);
            ser.Sync("VLC", ref VLC);
            ser.Sync("CycleCounter", ref CycleCounter);
            ser.Sync("EndOfScreen", ref EndOfScreen);
            ser.Sync("HSYNCWidth", ref HSYNCWidth);
            ser.Sync("HSYNCCounter", ref HSYNCCounter);
            ser.Sync("VSYNCWidth", ref VSYNCWidth);
            ser.Sync("VSYNCCounter", ref VSYNCCounter);
            ser.EndSection();
        }

        #endregion

        #region Enums

        /// <summary>
        /// The types of CRCT chip found in the CPC range
        /// </summary>
        public enum CRCTType
        {
            HD6845S = 0,
            UM6845 = 0,
            UM6845R = 1,
            MC6845 = 2,
            AMS40489 = 3,
            AMS40226 = 4
        }

        #endregion
    }
}
