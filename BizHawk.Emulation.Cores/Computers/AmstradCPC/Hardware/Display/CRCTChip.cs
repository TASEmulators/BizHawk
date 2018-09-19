using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using System.Collections;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// CRT CONTROLLER (CRTC)
    /// http://archive.pcjs.org/pubs/pc/datasheets/MC6845-CRT.pdf
    /// http://www.cpcwiki.eu/imgs/c/c0/Hd6845.hitachi.pdf
    /// http://www.cpcwiki.eu/imgs/1/13/Um6845.umc.pdf
    /// http://www.cpcwiki.eu/imgs/b/b5/Um6845r.umc.pdf
    /// http://www.cpcwiki.eu/index.php/CRTC
    /// </summary>
    public class CRCTChip
    {
        #region Devices

        private CPCBase _machine { get; set; }
        private CRCTType ChipType;

        #endregion

        #region Construction

        public CRCTChip(CRCTType chipType, CPCBase machine)
        {
            _machine = machine;
            ChipType = chipType;
            //Reset();
        }

        #endregion

        #region Output Lines

        // State output lines      
        /// <summary>
        /// This TTL compatible output is an active high signal which drives the monitor directly or is fed to Video Processing Logic for composite generation.
        /// This signal determines the vertical position of the displayed text.
        /// </summary>
        public bool VSYNC { get { return _VSYNC; } }
        /// <summary>
        /// This TTL compatible  output is an active high signal which drives the monitor directly or is fed to Video Processing Logic for composite generation.
        /// This signal determines the horizontal position of the displayed text. 
        /// </summary>
        public bool HSYNC { get { return _HSYNC; } }
        /// <summary>
        /// This TTL compatible output is an active high signal which indicates the CRTC is providing addressing in the active Display Area.
        /// </summary>      
        public bool DisplayEnable { get { return DisplayEnable; } }
        /// <summary>
        /// This TTL compatible output indicates Cursor Display to external Video Processing Logic.Active high signal. 
        /// </summary>       
        public bool Cursor { get { return _Cursor; } }

        private bool _VSYNC;
        private bool _HSYNC;
        private bool _DisplayEnable;
        private bool _Cursor;

        // Refresh memory addresses
        /*
            Refresh Memory Addresses (MAO-MA13) -These 14 outputs are used to refresh the CRT screen with pages of
            data located within a 16K block of refresh memory. These outputs drive a TTL load and 30pF. A high level on
            MAO-MA 13 is a logical "1." 
         */
        public bool MA0 { get { return LinearAddress.Bit(0); } }
        public bool MA1 { get { return LinearAddress.Bit(1); } }
        public bool MA2 { get { return LinearAddress.Bit(2); } }
        public bool MA3 { get { return LinearAddress.Bit(3); } }
        public bool MA4 { get { return LinearAddress.Bit(4); } }
        public bool MA5 { get { return LinearAddress.Bit(5); } }
        public bool MA6 { get { return LinearAddress.Bit(6); } }
        public bool MA7 { get { return LinearAddress.Bit(7); } }
        public bool MA8 { get { return LinearAddress.Bit(8); } }
        public bool MA9 { get { return LinearAddress.Bit(9); } }
        public bool MA10 { get { return LinearAddress.Bit(10); } }   // cpcwiki would suggest that this isnt connected in the CPC range
        public bool MA11 { get { return LinearAddress.Bit(11); } }   // cpcwiki would suggest that this isnt connected in the CPC range
        public bool MA12 { get { return LinearAddress.Bit(12); } }   // cpcwiki would suggest that this is connected in the CPC range but not used
        public bool MA13 { get { return LinearAddress.Bit(13); } }   // cpcwiki would suggest that this is connected in the CPC range but not used

        // Row addresses for character generators
        /*
            Raster Addresses (RAO-RA4) - These 5 outputs from the internal Raster Counter address the Character ROM
            for the row of a character. These outputs drive a TTL load and 30pF. A high level (on RAO-RA4) is a logical "1." 
         */
        public bool RA0 { get { return ScanLineCTR.Bit(0); } }
        public bool RA1 { get { return ScanLineCTR.Bit(1); } }
        public bool RA2 { get { return ScanLineCTR.Bit(2); } }
        public bool RA3 { get { return ScanLineCTR.Bit(3); } }    // cpcwiki would suggest that this isnt connected in the CPC range
        public bool RA4 { get { return ScanLineCTR.Bit(4); } }    // cpcwiki would suggest that this isnt connected in the CPC range

        /// <summary>
        /// Built from R12, R13 and CLK
        /// This is a logical emulator output and is how the CPC gatearray would translate the lines
        /*
            Memory Address Signal	Signal source	Signal name
            A15	                    6845	        MA13
            A14	                    6845	        MA12
            A13	                    6845	        RA2
            A12	                    6845	        RA1
            A11	                    6845	        RA0
            A10	                    6845	        MA9
            A9	                    6845	        MA8
            A8	                    6845	        MA7
            A7	                    6845	        MA6
            A6	                    6845	        MA5
            A5	                    6845	        MA4
            A4	                    6845	        MA3
            A3	                    6845	        MA2
            A2	                    6845	        MA1
            A1	                    6845	        MA0
            A0	                    Gate-Array	    CLK

         */
        /// </summary>
        public ushort AddressLine
        {
            get
            {
                BitArray MA = new BitArray(16);
                MA[0] = _CLK;
                MA[1] = MA0;
                MA[2] = MA1;
                MA[3] = MA2;
                MA[4] = MA3;
                MA[5] = MA4;
                MA[6] = MA5;
                MA[7] = MA6;
                MA[8] = MA7;
                MA[9] = MA8;
                MA[10] = MA9;
                MA[11] = RA0;
                MA[12] = RA1;
                MA[13] = RA2;
                MA[14] = MA12;
                MA[15] = MA13;
                ushort[] array = new ushort[1];
                MA.CopyTo(array, 0);
                return array[0];
            }
        }

        #endregion

        #region Input Lines

        /// <summary>
        /// This TTL compatible output indicates Cursor Display to external Video Processing Logic.Active high signal. 
        /// </summary>
        public bool CLK { get { return _CLK; } }
        /// <summary>
        /// The RES input is used to Reset the CRTC. An input low level on RES forces CRTC into following status: 
        ///     (A) All the counters in CRTC are cleared and the device stops the display operation.  
        ///     (C) Control registers in CRTC are not affected and remain unchanged. 
        /// This signal is different from other M6800 family in the following functions: 
        ///     (A) RES signal has capability of reset function only. when LPSTB is at low level. 
        ///     (B) After RES has gone down to low level, output s ignals of MAO -MA13 and RAO - RA4, synchronizing with CLK low level, goes down to low level. 
        ///         (At least 1 cycle CLK signal is necessary for reset.) 
        ///     (C) The CRTC starts the Display operation immediately after the release of RES signal.
        /// </summary>
        public bool RESET { get { return _RESET; } }
        /// <summary>
        /// Light Pen Strobe (LPSTR) - This high impedance TTLIMOS compatible input latches the cu rrent Refresh Addresses in the Register File.
        /// Latching is on the low to high edge and is synchronized internally to character clock.
        /// </summary>
        public bool LPSTB { get { return _LPSTB; } }

        private bool _CLK;
        private bool _RESET;        
        private bool _LPSTB;

        #endregion

        #region Internal Registers

        /// <summary>
        /// The currently selected register
        /// </summary>
        private byte AddressRegister;

        /// <summary>
        /// The internal register
        /// The Address Register is a 5 bit write-only register used as an "indirect" or "pointer" register.
        /// Its contents are the address of one of the other 18 registers in the file.When RS and CS are low, 
        /// the Address Register itself is addressed.When RS is high, the Register File is accessed.
        /// </summary>
        private byte[] Register = new byte[18];

        // Horizontal timing register constants
        /// <summary>
        /// This 8 bit write-only register determines the horizontal frequency of HS. 
        /// It is the total of displayed plus non-displayed character time units minus one.
        /// </summary>
        private const int H_TOTAL = 0;
        /// <summary>
        /// This 8 bit write-only register determines the number of displayed characters per horizontal line.
        /// </summary>
        private const int H_DISPLAYED = 1;
        /// <summary>
        /// This 8 bit write-only register determines the horizontal sync postiion on the horizontal line.
        /// </summary>
        private const int H_SYNC_POS = 2;
        /// <summary>
        /// This 4 bit  write-only register determines the width of the HS pulse. It may not be apparent why this width needs to be programmed.However, 
        /// consider that all timing widths must be programmed as multiples of the character clock period which varies.If HS width were fixed as an integral 
        /// number of character times, it would vary with character rate and be out of tolerance for certain monitors.
        /// The rate programmable feature allows compensating HS width.
        /// NOTE: Dependent on chiptype this also may include VSYNC width - check the UpdateWidths() method
        /// </summary>
        private const int SYNC_WIDTHS = 3;

        // Vertical timing register constants
        /// <summary>
        /// The vertical frequency of VS is determined by both R4 and R5.The calculated number of character I ine times is usual I y an integer plus a fraction to 
        /// get exactly a 50 or 60Hz vertical refresh rate. The integer number of character line times minus one is programmed in the 7 bit write-only Vertical Total Register; 
        /// the fraction is programmed in the 5 bit write-only Vertical Scan Adjust Register as a number of scan line times.
        /// </summary>
        private const int V_TOTAL = 4;
        private const int V_TOTAL_ADJUST = 5;
        /// <summary>
        /// This 7 bit write-only register determines the number of displayed character rows on the CRT screen, and is programmed in character row times.
        /// </summary>
        private const int V_DISPLAYED = 6;
        /// <summary>
        /// This 7 bit write-only register determines the vertical sync position with respect to the reference.It is programmed in character row times.
        /// </summary>
        private const int V_SYNC_POS = 7;
        /// <summary>
        /// This 2 bit write-only  register controls the raster scan mode(see Figure 11 ). When bit 0 and bit 1 are reset, or bit 0 is reset and bit 1 set, 
        /// the non· interlace raster scan mode is selected.Two interlace modes are available.Both are interlaced 2 fields per frame.When bit 0 is set and bit 1 is reset, 
        /// the interlace sync raster scan mode is selected.Also when bit 0 and bit 1 are set, the interlace sync and video raster scan mode is selected.
        /// </summary>
        private const int INTERLACE_MODE = 8;
        /// <summary>
        /// This 5 bit write·only register determines the number of scan lines per character row including spacing.
        /// The programmed value is a max address and is one less than the number of scan l1nes.
        /// </summary>
        private const int MAX_SL_ADDRESS = 9;

        // Other register constants
        /// <summary>
        /// This 7 bit write-only register controls the cursor format(see Figure 10). Bit 5 is the blink timing control.When bit 5 is low, the blink frequency is 1/16 of the 
        /// vertical field rate, and when bit 5 is high, the blink frequency is 1/32 of the vertical field rate.Bit 6 is used to enable a blink.
        /// The cursor start scan line is set by the lower 5 bits. 
        /// </summary>
        private const int CURSOR_START = 10;
        /// <summary>
        /// This 5 bit write-only register sets the cursor end scan line
        /// </summary>
        private const int CURSOR_END = 11;
        /// <summary>
        /// Start Address Register is a 14 bit write-only register which determines the first address put out as a refresh address after vertical blanking.
        /// It consists of an 8 bit lower register, and a 6 bit higher register.
        /// </summary>
        private const int START_ADDR_H = 12;
        private const int START_ADDR_L = 13;
        /// <summary>
        /// This 14 bit read/write register stores the cursor location.This register consists of an 8 bit lower and 6 bit higher register.
        /// </summary>
        private const int CURSOR_H = 14;
        private const int CURSOR_L = 15;
        /// <summary>
        /// This 14 bit read -only register is used to store the contents of the Address Register(H & L) when the LPSTB input pulses high.
        /// This register consists of an 8 bit lower and 6 bit higher register.
        /// </summary>
        private const int LIGHT_PEN_H = 16;
        private const int LIGHT_PEN_L = 17;

        #endregion

        #region Internal Fields & Properties

        /// <summary>
        /// Calculated when set based on R3
        /// </summary>
        private int HSYNCWidth;
        /// <summary>
        /// Calculated when set based on R3
        /// </summary>
        private int VSYNCWidth;

        /// <summary>
        /// Character pos address (0 index).
        /// Feeds the MA lines
        /// </summary>
        private int LinearAddress;

        /// <summary>
        /// The currently selected Interlace Mode (based on R8)
        /// </summary>
        private InterlaceMode CurrentInterlaceMode
        {
            get
            {
                if (!Register[INTERLACE_MODE].Bit(0))
                {
                    return InterlaceMode.NormalSyncMode;
                }
                else if (Register[INTERLACE_MODE].Bit(0))
                {
                    if (Register[INTERLACE_MODE].Bit(1))
                    {
                        return InterlaceMode.InterlaceSyncAndVideoMode;
                    }
                    else
                    {
                        return InterlaceMode.InterlaceSyncMode;
                    }
                }

                return InterlaceMode.NormalSyncMode;
            }
        }

        /// <summary>
        /// The current cursor display mode (based on R14 & R15)
        /// </summary>
        private CursorControl CurrentCursorMode
        {
            get
            {
                if (!Register[CURSOR_START].Bit(6) && !Register[CURSOR_START].Bit(5))
                {
                    return CursorControl.NonBlink;
                }
                else if (!Register[CURSOR_START].Bit(6) && Register[CURSOR_START].Bit(5))
                {
                    return CursorControl.CursorNonDisplay;
                }
                else if (Register[CURSOR_START].Bit(6) && !Register[CURSOR_START].Bit(5))
                {
                    return CursorControl.Blink1_16;
                }
                else
                {
                    return CursorControl.Blink1_32;
                }
            }
        }

        // Counter microchips
        private int HorizontalCTR { get { return _HorizontalCTR; }
        set
            {
                if (value > 255)
                    _HorizontalCTR = value - 255;
            }
        }
              
        private int HorizontalSyncWidthCTR
        {
            get { return _HorizontalSyncWidthCTR; }
            set
            {
                if (value > 15)
                    _HorizontalSyncWidthCTR = value - 15;
            }
        }

        private int CharacterRowCTR
        {
            get { return CharacterRowCTR; }
            set
            {
                if (value > 127)
                    _CharacterRowCTR = value - 127;
            }
        }

        private int ScanLineCTR
        {
            get { return ScanLineCTR; }
            set
            {
                if (value > 31)
                    _ScanLineCTR = value - 31;
            }
        }

        private int _HorizontalCTR;
        private int _HorizontalSyncWidthCTR;
        private int _CharacterRowCTR;
        private int _ScanLineCTR;

        #endregion

        #region Databus Interface
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

        /* CPC:
        #BCXX	%x0xxxx00 xxxxxxxx	6845 CRTC Index	                        -	    Write
        #BDXX	%x0xxxx01 xxxxxxxx	6845 CRTC Data Out	                    -	    Write
        #BEXX	%x0xxxx10 xxxxxxxx	6845 CRTC Status (as far as supported)	Read	-
        #BFXX	%x0xxxx11 xxxxxxxx	6845 CRTC Data In (as far as supported)	Read	-
     */

        /// <summary>
        /// CPU (or other device) reads from the 8-bit databus
        /// </summary>
        /// <param name="port"></param>
        /// <param name="result"></param>
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
        /// CPU (or other device) writes to the 8-bit databus
        /// </summary>
        /// <param name="port"></param>
        /// <param name="result"></param>
        public bool WritePort(ushort port, int value)
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
                    SelectRegister(value);
                    break;

                // data write
                case 1:
                    WriteRegister(value);
                    break;
            }

            return accessed;
        }

        #endregion

        #region Internal IO Methods

        /// <summary>
        /// Selects a specific register
        /// </summary>
        /// <param name="value"></param>
        private void SelectRegister(int value)
        {
            var v = (byte)((byte)value & 0x1F);
            if (v > 0 && v < 18)
            {
                AddressRegister = v;
            }
        }

        /// <summary>
        /// Writes to the currently latched address register
        /// </summary>
        /// <param name="value"></param>
        private void WriteRegister(int value)
        {
            byte val = (byte)value;

            // lightpen regs are readonly on all models
            if (AddressRegister == 16 || AddressRegister == 17)
                return;

            // all other models can be written to
            switch (AddressRegister)
            {
                case H_TOTAL:
                case H_DISPLAYED:
                case H_SYNC_POS:
                case START_ADDR_L:
                    Register[AddressRegister] = val;
                    break;

                case SYNC_WIDTHS:
                    Register[AddressRegister] = val;
                    UpdateWidths();
                    break;

                case V_TOTAL_ADJUST:
                case CURSOR_END:
                case MAX_SL_ADDRESS:
                    Register[AddressRegister] = (byte)(val & 0x1F);
                    break;

                case START_ADDR_H:
                case CURSOR_H:
                    Register[AddressRegister] = (byte)(val & 0x3F);
                    break;

                case V_TOTAL:
                case V_DISPLAYED:
                case V_SYNC_POS:
                case CURSOR_START:
                    Register[AddressRegister] = (byte)(val & 0x7F);
                    break;

                case INTERLACE_MODE:
                    Register[AddressRegister] = (byte)(val & 0x3);
                    break;
            }
        }

        /// <summary>
        /// Reads from the currently selected register
        /// </summary>
        /// <param name="data"></param>
        private bool ReadRegister(ref int data)
        {
            bool addressed = false;
            switch (AddressRegister)
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
                        data = Register[AddressRegister];
                    else if ((int)ChipType == 1)
                        data = 0;
                    break;
                case 14:
                case 15:
                case 16:
                case 17:
                    addressed = true;
                    data = Register[AddressRegister];
                    break;

                default:
                    // registers 18-31 read as 0, on type 0 and 2. registers 18-30 read as 0 on type1, register 31 reads as 0x0ff.
                    if (AddressRegister >= 18 && AddressRegister <= 30)
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
                    else if (AddressRegister == 31)
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
        /// Updates the V and H SYNC widths
        /// </summary>
        private void UpdateWidths()
        {
            switch (ChipType)
            {
                case CRCTType.HD6845S:
                    // Bits 7..4 define Vertical Sync Width. If 0 is programmed this gives 16 lines of VSYNC. Bits 3..0 define Horizontal Sync Width. 
                    // If 0 is programmed no HSYNC is generated.
                    HSYNCWidth = (Register[SYNC_WIDTHS] >> 0) & 0x0F;
                    VSYNCWidth = (Register[SYNC_WIDTHS] >> 4) & 0x0F;
                    break;
                case CRCTType.UM6845R:
                    // Bits 7..4 are ignored. Vertical Sync is fixed at 16 lines. Bits 3..0 define Horizontal Sync Width. If 0 is programmed no HSYNC is generated.
                    HSYNCWidth = (Register[SYNC_WIDTHS] >> 0) & 0x0F;
                    VSYNCWidth = 16;
                    break;
                case CRCTType.MC6845:
                    // Bits 7..4 are ignored. Vertical Sync is fixed at 16 lines. Bits 3..0 define Horizontal Sync Width. If 0 is programmed this gives a HSYNC width of 16.
                    HSYNCWidth = (Register[SYNC_WIDTHS] >> 0) & 0x0F;
                    if (HSYNCWidth == 0)
                        HSYNCWidth = 16;
                    VSYNCWidth = 16;
                    break;
                case CRCTType.AMS40489:
                case CRCTType.AMS40226:
                    // Bits 7..4 define Vertical Sync Width. If 0 is programmed this gives 16 lines of VSYNC.Bits 3..0 define Horizontal Sync Width. 
                    // If 0 is programmed this gives a HSYNC width of 16.
                    HSYNCWidth = (Register[SYNC_WIDTHS] >> 0) & 0x0F;
                    VSYNCWidth = (Register[SYNC_WIDTHS] >> 4) & 0x0F;
                    if (HSYNCWidth == 0)
                        HSYNCWidth = 16;
                    if (VSYNCWidth == 0)
                        VSYNCWidth = 16;
                    break;
            }
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

        #endregion

        #region Public Functions

        /// <summary>
        /// Performs a CRCT clock cycle.
        /// On CPC this is called at 1Mhz == 1 Character cycle (2 bytes)
        /// </summary>
        public void ClockCycle()
        {
            // H clock
            HorizontalCTR++;

            if (HorizontalCTR == Register[H_TOTAL])
            {
                // end of current scanline
                HorizontalCTR = 0;
                // CRCT starts its scalines at the display area
                _DisplayEnable = true;

                ScanLineCTR++;

                if (ScanLineCTR > Register[MAX_SL_ADDRESS])
                {
                    // end of vertical character
                    ScanLineCTR = 0;
                    CharacterRowCTR++;

                    if (CharacterRowCTR == Register[V_TOTAL])
                    {
                        // check for vertical adjust
                        if (Register[V_TOTAL_ADJUST] > 0)
                        {

                        }
                        else
                        {
                            // end of CRCT frame
                            CharacterRowCTR = 0;
                        }
                    }
                }
            }
            else if (HorizontalCTR == Register[H_DISPLAYED] + 1)
            {
                // end of display area
                _DisplayEnable = false;
            }
        }

        #endregion

        #region Internal Functions



        #endregion

        #region Serialization

        public void SyncState(Serializer ser)
        {
            ser.BeginSection("CRCT");
            ser.SyncEnum("ChipType", ref ChipType);
            ser.Sync("_VSYNC", ref _VSYNC);
            ser.Sync("_HSYNC", ref _HSYNC);
            ser.Sync("_DisplayEnable", ref _DisplayEnable);
            ser.Sync("_Cursor", ref _Cursor);
            ser.Sync("_CLK", ref _CLK);
            ser.Sync("_RESET", ref _RESET);
            ser.Sync("_LPSTB", ref _LPSTB);
            ser.Sync("AddressRegister", ref AddressRegister);
            ser.Sync("Register", ref Register, false);
            ser.Sync("HSYNCWidth", ref HSYNCWidth);
            ser.Sync("VSYNCWidth", ref VSYNCWidth);
            ser.Sync("_HorizontalCTR", ref _HorizontalCTR);
            ser.Sync("_HorizontalSyncWidthCTR", ref _HorizontalSyncWidthCTR);
            ser.Sync("_CharacterRowCTR", ref _CharacterRowCTR);
            ser.Sync("_ScanLineCTR", ref _ScanLineCTR);
            ser.EndSection();

            /*
             
             * */
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

        /// <summary>
        /// The available interlace modes in the CRCT
        /// </summary>
        private enum InterlaceMode
        {
            NormalSyncMode,
            InterlaceSyncMode,
            InterlaceSyncAndVideoMode
        }

        /// <summary>
        /// Cursor display modes
        /// </summary>
        private enum CursorControl
        {
            NonBlink,
            CursorNonDisplay,
            Blink1_16,
            Blink1_32
        }

        #endregion
    }
}
