using BizHawk.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// Cathode Ray Tube Controller Chip - 6845
    /// http://www.cpcwiki.eu/index.php/CRTC
    /// 
    /// </summary>
    public class CRCT_6845 : IPortIODevice
    {
        #region Devices

        private CPCBase _machine { get; set; }
        private CRCTType ChipType;

        #endregion

        #region Construction

        public CRCT_6845(CRCTType chipType, CPCBase machine)
        {
            _machine = machine;
            ChipType = chipType;
        }

        #endregion

        #region State

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

        /// <summary>
        /// The currently selected register
        /// </summary>
        private int SelectedRegister;

        /// <summary>
        /// CPC register default values
        /// </summary>
        private byte[] RegDefaults = new byte[] { 63, 40, 52, 52, 20, 8, 16, 19, 0, 11, 73, 10, 0, 0, 0, 0, 0, 0 };

        /// <summary>
        /// Register masks
        /// </summary>
        private byte[] CPCMask = new byte[] { 255, 255, 255, 255, 127, 31, 127, 126, 3, 31, 31, 31, 63, 255, 63, 255, 63, 255 };

        /// <summary>
        /// CRTC Register constants
        /// </summary>
        public const int HOR_TOTAL = 0;
        public const int HOR_DISPLAYED = 1;
        public const int HOR_SYNC_POS = 2;
        public const int HOR_AND_VER_SYNC_WIDTHS = 3;
        public const int VER_TOTAL = 4;
        public const int VER_TOTAL_ADJUST = 5;
        public const int VER_DISPLAYED = 6;
        public const int VER_SYNC_POS = 7;
        public const int INTERLACE_SKEW = 8;
        public const int MAX_RASTER_ADDR = 9;
        public const int CUR_START_RASTER = 10;
        public const int CUR_END_RASTER = 11;
        public const int DISP_START_ADDR_H = 12;
        public const int DISP_START_ADDR_L = 13;
        public const int CUR_ADDR_H = 14;
        public const int CUR_ADDR_L = 15;
        public const int LPEN_ADDR_H = 16;
        public const int LPEN_ADDR_L = 17;

        public int horSyncWidth;
        public int verSyncWidth;

        public int currCol;
        public int currLineInRow;
        public int currRow;

        public bool isHSYNC;
        public bool isVSYNC;

        public int HSYNCcnt;
        public int VSYNCcnt;

        public bool DISPTMG;

        private long LastTCycle;

        #endregion

        #region Public Methods

        /// <summary>
        /// The CRTC runs at 1MHz, so effectively every 4 T-States
        /// </summary>
        public void CycleClock()
        {
            if (HSYNCcnt > 0)
            {
                HSYNCcnt--;
                if (HSYNCcnt == 0)
                {
                    // HSYNC is over
                    isHSYNC = false;
                }
            }

            // move one column
            currCol++;

            // scanline
            if (currCol == Regs[HOR_TOTAL] + 1)
            {
                // we have reached the end of the current scanline
                currCol = 0;

                // take care of VSYNC
                if (VSYNCcnt > 0)
                {
                    VSYNCcnt--;
                    if (VSYNCcnt == 0)
                    {
                        // VSYNC is over
                        isVSYNC = false;
                    }
                }

                // increment row
                currLineInRow++;

                if (currLineInRow == Regs[MAX_RASTER_ADDR] + 1)
                {
                    currLineInRow = 0;
                    currRow++;

                    if (currRow == Regs[VER_TOTAL] + 1)
                    {
                        currRow = 0;
                    }
                }

                // check for vsync
                if (!isVSYNC && currRow == Regs[VER_SYNC_POS])
                {
                    isVSYNC = true;
                    VSYNCcnt = verSyncWidth;
                    //vsync happening
                }
            }
            else if (!isHSYNC && currCol == Regs[HOR_SYNC_POS])
            {
                // HSYNC starts
                isHSYNC = true;
                HSYNCcnt = horSyncWidth;
                // hsync happening
            }
        }

        /// <summary>
        /// Resets the chip
        /// </summary>
        public void Reset()
        {
            // set regs to default
            for (int i = 0; i < 18; i++)
                Regs[i] = RegDefaults[i];

            SelectedRegister = 0;
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

            Regs[SelectedRegister] = (byte)(data & CPCMask[SelectedRegister]);

            if (SelectedRegister == HOR_AND_VER_SYNC_WIDTHS)
            {
                switch (ChipType)
                {
                    case CRCTType.Hitachi_HD6845S:
                        // Bits 7..4 define Vertical Sync Width. If 0 is programmed this gives 16 lines of VSYNC. Bits 3..0 define Horizontal Sync Width. 
                        // If 0 is programmed no HSYNC is generated.
                        horSyncWidth = (Regs[HOR_AND_VER_SYNC_WIDTHS] >> 0) & 0x0F;
                        verSyncWidth = (Regs[HOR_AND_VER_SYNC_WIDTHS] >> 4) & 0x0F;
                        break;
                    case CRCTType.UMC_UM6845R:
                        // Bits 7..4 are ignored. Vertical Sync is fixed at 16 lines. Bits 3..0 define Horizontal Sync Width. If 0 is programmed no HSYNC is generated.
                        horSyncWidth = (Regs[HOR_AND_VER_SYNC_WIDTHS] >> 0) & 0x0F;
                        verSyncWidth = 16;
                        break;
                    case CRCTType.Motorola_MC6845:
                        // Bits 7..4 are ignored. Vertical Sync is fixed at 16 lines. Bits 3..0 define Horizontal Sync Width. If 0 is programmed this gives a HSYNC width of 16.
                        horSyncWidth = (Regs[HOR_AND_VER_SYNC_WIDTHS] >> 0) & 0x0F;
                        if (horSyncWidth == 0)
                            horSyncWidth = 16;
                        verSyncWidth = 16;
                        break;
                    case CRCTType.Amstrad_AMS40489:
                    case CRCTType.Amstrad_40226:
                        // Bits 7..4 define Vertical Sync Width. If 0 is programmed this gives 16 lines of VSYNC.Bits 3..0 define Horizontal Sync Width. 
                        // If 0 is programmed this gives a HSYNC width of 16.
                        horSyncWidth = (Regs[HOR_AND_VER_SYNC_WIDTHS] >> 0) & 0x0F;
                        verSyncWidth = (Regs[HOR_AND_VER_SYNC_WIDTHS] >> 4) & 0x0F;
                        if (horSyncWidth == 0)
                            horSyncWidth = 16;
                        if (verSyncWidth == 0)
                            verSyncWidth = 16;
                        break;
                }
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

        #endregion

        #region PortIODevice

        /// <summary>
        /// Device responds to an IN instruction
        /// </summary>
        /// <param name="port"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool ReadPort(ushort port, ref int result)
        {
            BitArray portBits = new BitArray(BitConverter.GetBytes(port));

            // The 6845 is selected when bit 14 of the I/O port address is set to "0"
            bool accessed = !portBits[14];
            if (!accessed)
                return false;

            // Bit 9 and 8 of the I/O port address define the function to access
            if (portBits[8] == false && portBits[9] == true)
            {
                // read status register
                accessed = ReadStatus(ref result);
            }
            else if (portBits[8] == true && portBits[9] == true)
            {
                // read data register
                accessed = ReadRegister(ref result);
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
            BitArray portBits = new BitArray(BitConverter.GetBytes(port));

            // The 6845 is selected when bit 14 of the I/O port address is set to "0"
            bool accessed = !portBits[14];
            if (!accessed)
                return false;

            // Bit 9 and 8 of the I/O port address define the function to access
            if (portBits[8] == false && portBits[9] == false)
            {
                // Select 6845 register
                RegisterSelect(result);
            }
            else if (portBits[8] == true && portBits[9] == false)
            {
                // Write 6845 register data
                WriteRegister(result);
            }

            return true;
        }

        #endregion

        #region Serialization

        public void SyncState(Serializer ser)
        {
            ser.BeginSection("CRTC");
            ser.SyncEnum("ChipType", ref ChipType);
            ser.Sync("Regs", ref Regs, false);
            ser.Sync("SelectedRegister", ref SelectedRegister);
            ser.Sync("verSyncWidth", ref verSyncWidth);
            ser.Sync("currCol", ref currCol);
            ser.Sync("currLineInRow", ref currLineInRow);
            ser.Sync("currRow", ref currRow);
            ser.Sync("isHSYNC", ref isHSYNC);
            ser.Sync("isVSYNC", ref isVSYNC);
            ser.Sync("HSYNCcnt", ref HSYNCcnt);
            ser.Sync("VSYNCcnt", ref VSYNCcnt);
            ser.Sync("LastTCycle", ref LastTCycle);
            ser.Sync("DISPTMG", ref DISPTMG);
            ser.EndSection();
        }

        #endregion

        #region Enums

        /// <summary>
        /// The types of CRCT chip found in the CPC range
        /// </summary>
        public enum CRCTType
        {
            Hitachi_HD6845S = 0,
            UMC_UM6845R = 1,
            Motorola_MC6845 = 2,
            Amstrad_AMS40489 = 3,
            Amstrad_40226 = 4
        }

        #endregion
    }
}
