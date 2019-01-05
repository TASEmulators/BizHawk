using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using System;
using System.Collections;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	/// <summary>
	/// CATHODE RAY TUBE CONTROLLER (CRTC) IMPLEMENTATION
	/// http://www.cpcwiki.eu/index.php/CRTC
	/// http://cpctech.cpc-live.com/docs/cpcplus.html
	/// This implementation aims to emulate all the various CRTC chips that appear within
	/// the CPC, CPC+ and GX4000 ranges. The CPC community have assigned them type numbers.
	/// If different implementations share the same type number it indicates that they are functionally identical:
	/// 
	///		Part No.	Manufacturer	Type No.	Info.
	///		------------------------------------------------------------------------------------------------------
	///		HD6845S		Hitachi			0	
	///		Datasheet:	http://www.cpcwiki.eu/imgs/c/c0/Hd6845.hitachi.pdf
	///		------------------------------------------------------------------------------------------------------
	///		UM6845		UMC				0
	///		Datasheet:	http://www.cpcwiki.eu/imgs/1/13/Um6845.umc.pdf
	///		------------------------------------------------------------------------------------------------------
	///		UM6845R		UMC				1
	///		Datasheet:	http://www.cpcwiki.eu/imgs/b/b5/Um6845r.umc.pdf
	///		------------------------------------------------------------------------------------------------------
	///		MC6845		Motorola		2
	///		Datasheet:	http://www.cpcwiki.eu/imgs/d/da/Mc6845.motorola.pdf & http://bitsavers.trailing-edge.com/components/motorola/_dataSheets/6845.pdf
	///		------------------------------------------------------------------------------------------------------
	///		AMS40489	Amstrad			3			Only exists in the CPC464+, CPC6128+ and GX4000 and is integrated into a single CPC+ ASIC chip (along with the gatearray)
	///		Datasheet:	{none}
	///		------------------------------------------------------------------------------------------------------
	///		AMS40041	Amstrad			4			'Pre-ASIC' IC. The CRTC is integrated into a aingle ASIC IC with functionality being almost identical to the AMS40489
	///		(or 40226)								Used in the 'Cost-Down' range of CPC464 and CPC6128 systems
	///		Datasheet:	{none}
	/// 
	/// </summary>
	public class CRTC6845
	{
		/// <summary>
		/// The type of CRTC we are emulating
		/// (passed in through contructor)
		/// </summary>
		private CRTCType ChipType;

		#region Construction

		/// <summary>
		/// The only constructor
		/// </summary>
		/// <param name="type"></param>
		public CRTC6845(CRTCType type)
		{
			ChipType = type;
		}

		#endregion

		#region Input Lines

		/// <summary>
		/// The ClK isaTTUMOS-compatible input used to synchronize all CRT' functions except for the processor interface. 
		/// An external dot counter is used to derive this signal which is usually the character rate in an alphanumeric CRT.
		/// The active transition is high-to-low
		/// </summary>
		public bool CLK { get { return _CLK; } }
		private bool _CLK;
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
		private bool _RESET;
		/// <summary>
		/// Light Pen Strobe (LPSTR) - This high impedance TTLIMOS compatible input latches the cu rrent Refresh Addresses in the Register File.
		/// Latching is on the low to high edge and is synchronized internally to character clock.
		/// </summary>
		public bool LPSTB { get { return _LPSTB; } }
		private bool _LPSTB;

		#endregion

		#region Output Lines

		// State output lines      
		/// <summary>
		/// This TTL compatible output is an active high signal which drives the monitor directly or is fed to Video Processing Logic for composite generation.
		/// This signal determines the vertical position of the displayed text.
		/// </summary>
		public bool VSYNC { get { return _VSYNC; } }
		private bool _VSYNC;
		/// <summary>
		/// This TTL compatible  output is an active high signal which drives the monitor directly or is fed to Video Processing Logic for composite generation.
		/// This signal determines the horizontal position of the displayed text. 
		/// </summary>
		public bool HSYNC { get { return _HSYNC; } }
		private bool _HSYNC;
		/// <summary>
		/// This TTL compatible output is an active high signal which indicates the CRTC is providing addressing in the active Display Area.
		/// </summary>      
		public bool DISPTMG { get { return _DISPTMG; } }
		private bool _DISPTMG;
		/// <summary>
		/// This TTL compatible output indicates Cursor Display to external Video Processing Logic.Active high signal. 
		/// </summary>       
		public bool CUDISP { get { return _CUDISP; } }
		private bool _CUDISP;

		// Refresh memory addresses
		/*
            Refresh Memory Addresses (MAO-MA13) - These 14 outputs are used to refresh the CRT screen with pages of
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
		public bool RA0 { get { return RowSelects.Bit(0); } }
		public bool RA1 { get { return RowSelects.Bit(1); } }
		public bool RA2 { get { return RowSelects.Bit(2); } }
		public bool RA3 { get { return RowSelects.Bit(3); } }    // cpcwiki would suggest that this isnt connected in the CPC range
		public bool RA4 { get { return RowSelects.Bit(4); } }    // cpcwiki would suggest that this isnt connected in the CPC range

		/// <summary>
		/// This 16-bit property emulates how the Amstrad CPC Gate Array is connected up to the CRTC
		/// Built from R12, R13 and CLK
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
		public ushort AddressLineCPC
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

		#region Internal State

		/// <summary>
		/// Character pos address (0 index).
		/// Feeds the MA lines
		/// </summary>
		private int LinearAddress;

		/// <summary>
		/// Generated by the Vertical Control
		/// Feeds the RA lines
		/// </summary>
		private int RowSelects;

		/// <summary>
		/// Horizontal Counter
		/// </summary>
		private int _CharacterCTR;
		private int CharacterCTR
		{
			get { return _CharacterCTR; }
			set
			{
				if (value > 255)
					_CharacterCTR = value - 255;
			}
		}

		/// <summary>
		/// HSYNC Counter
		/// </summary>
		private int _HorizontalSyncWidthCTR;
		private int HorizontalSyncWidthCTR
		{
			get { return _HorizontalSyncWidthCTR; }
			set
			{
				if (value > 15)
					_HorizontalSyncWidthCTR = value - 15;
			}
		}

		/// <summary>
		/// VSYNC Counter
		/// </summary>
		private int _VerticalSyncWidthCTR;
		private int VerticalSyncWidthCTR
		{
			get { return _VerticalSyncWidthCTR; }
			set
			{
				if (value > 15)
					_VerticalSyncWidthCTR = value - 15;
			}
		}

		/// <summary>
		/// Vertical Character Counter
		/// </summary>
		private int _LineCTR;
		private int LineCTR
		{
			get { return _LineCTR; }
			set
			{
				if (value > 127)
					_LineCTR = value - 127;
			}
		}

		/// <summary>
		/// Vertical Raster Counter
		/// </summary>
		private int _RasterCTR;
		private int RasterCTR
		{
			get { return _RasterCTR; }
			set
			{
				if (value > 31)
					_RasterCTR = value - 31;
			}
		}

		/// <summary>
		/// The CRTC latches the Display Start H & L address at different times
		/// (depending on the chip type)
		/// </summary>
		private int StartAddressLatch;		

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

		/// <summary>
		/// Internal Status Register specific to the Type 1 UM6845R
		/// </summary>
		private byte StatusRegister;

		/// <summary>
		/// Not really a true status register, but values are returned on types 3 and 4
		/// depending on the current CRTC status
		/// </summary>
		private byte AsicStatusRegister1;
		/// <summary>
		/// Not really a true status register, but values are returned on types 3 and 4
		/// depending on the current CRTC status
		/// </summary>
		private byte AsicStatusRegister2;

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

			The Read/Write functions below are geared toward Amstrad CPC only
			They could be overridden for a different implementation if needs be
     */

		/// <summary>
		/// CPU (or other device) reads from the 8-bit databus
		/// </summary>
		/// <param name="port"></param>
		/// <param name="result"></param>
		public virtual bool ReadPort(ushort port, ref int result)
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
		public virtual bool WritePort(ushort port, int value)
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

		#region Type-Specific Logic

		/// <summary>
		/// Runs a clock cycle for the current chip type
		/// CPC will call this every 1Mhz
		/// Equates to 1 generated character (2 bytes of data)
		/// Based on the various CRCT FUNCTIONAL BLOCK DIAGRAMs in the datasheets
		/// </summary>
		public void CycleClock()
		{
			switch ((int)ChipType)
			{
				case 0:
					ClockCycle_Type0();
					break;
				case 2:
					ClockCycle_Type2();
					break;
				default:
					ClockCycle_Generic();
					break;
			}
		}

		/// <summary>
		/// Type dependent
		/// Either a static value or calculated from R3
		/// </summary>
		private int HSYNCWidth
		{
			get
			{
				switch ((int)ChipType)
				{
					case 0:
					case 1:
						return HSYNCWidth_Type0_1;
					default:
						return HSYNCWidth_Type2_3_4;
				}
			}
		}

		/// <summary>
		/// Type dependent
		/// Either a static value or calculated from R3
		/// </summary>
		private int VSYNCWidth
		{
			get
			{
				switch ((int)ChipType)
				{
					case 1:
					case 2:
						return VSYNCWidth_Type1_2;
					default:
						return VSYNCWidth_Type0_3_4;
				}
			}
		}

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
		/// Attempts to read from the currently selected register
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private bool ReadRegister(ref int data)
		{
			switch ((int)ChipType)
			{
				case 0: return ReadRegister_Type0(ref data);
				case 1: return ReadRegister_Type1(ref data);
				case 2: return ReadRegister_Type2(ref data);
				case 3:
				case 4: return ReadRegister_Type3_4(ref data);
				default: return false;
			}
		}

		/// <summary>
		/// Attempts to write to the currently selected register
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private void WriteRegister(int data)
		{
			switch ((int)ChipType)
			{
				case 0: WriteRegister_Type0(data); break;
				case 1: WriteRegister_Type1(data); break;
				case 2: WriteRegister_Type2(data); break;
				case 3:
				case 4: WriteRegister_Type3_4(data); break;
			}
		}

		/// <summary>
		/// Attempts to read from the internal status register (if present)
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private bool ReadStatus(ref int data)
		{
			switch ((int)ChipType)
			{
				case 1: return ReadStatus_Type1(ref data);
				case 3:
				case 4: return ReadStatus_Type3_4(ref data);
				default: return false;					
			}
		}

		/// <summary>
		/// The status of the DisplayEnableSkew bit(s) in R8
		/// </summary>
		private int DISPTMGSkew
		{
			get
			{
				var val = Register[INTERLACE_MODE];
				int res = 0;
				switch ((int)ChipType)
				{
					// HD6845 & UM6845
					case 0:
						// Bits 5 and 4 determine the skew
						res = (val & 0x30) >> 4;
						if (res > 2)
							return -1;						
						break;

					// UMR6845R
					case 1:
						return 0;

					// MC6845
					case 2:
						return 0;

					// AMS chips
					case 3:
					case 4:
						break;
				}

				return res;
			}
		}

		/// <summary>
		/// The status of the CursorSkew bit(s) in R8
		/// </summary>
		private int CUDISPSkew
		{
			get
			{
				var val = Register[INTERLACE_MODE];
				int res = 0;
				switch ((int)ChipType)
				{
					// HD6845 & UM6845
					case 0:
						// Bits 5 and 4 determine the skew
						res = (val & 0xC0) >> 6;
						if (res > 2)
							return -1;
						break;
					
					// UMR6845R
					case 1:
						return 0;

					// MC6845
					case 2:
						return 0;

					// AMS chips
					case 3:
					case 4:
						break;
				}

				return res;
			}
		}

		/// <summary>
		/// The currently selected Interlace Mode (based on R8)
		/// Looks to be the same for all chip types
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
		/// Gets the combined value of R12 & R13
		/// </summary>
		private int StartAddressRegisterValue
		{
			get
			{
				var Reg13 = Register[START_ADDR_L];
				var Reg12 = (byte)(Register[START_ADDR_H] & 0x3f);
				return (Reg12 << 8) + Reg13;
			}
		}

		/// <summary>
		/// Gets the combined value of R14 & R15
		/// </summary>
		private int CursorAddressRegisterValue
		{
			get
			{
				var reg15 = Register[CURSOR_L];
				var reg14 = (byte)(Register[CURSOR_H] & 0x3f);
				return (reg14 << 8) + reg15;
			}
		}

		#endregion

		#region Type-Specific Internal Methods		

		#region Sync Widths

		/// <summary>
		/// Current programmed HSYNC width for Type 0 (HD6845S & UM6845) & Type 1 (UM6845R)
		/// </summary>
		private int HSYNCWidth_Type0_1
		{
			get
			{
				// Bits 3..0 define Horizontal Sync Width. 
				// If 0 is programmed no HSYNC is generated.
				return (Register[SYNC_WIDTHS] >> 0) & 0x0F;
			}
		}

		/// <summary>
		/// Current programmed HSYNC width for Type 2 (MC6845), 3 (AMS40489) & 4 (pre-ASIC)
		/// </summary>
		private int HSYNCWidth_Type2_3_4
		{
			get
			{
				// Bits 3..0 define Horizontal Sync Width. 
				// If 0 is programmed this gives a HSYNC width of 16
				var width = (Register[SYNC_WIDTHS] >> 0) & 0x0F;
				if (width == 0)
					width = 16;
				return width;
			}
		}

		/// <summary>
		/// Current programmed VSYNC width for Type 0 (HD6845S & UM6845), 3 (AMS40489) & 4 (pre-ASIC)
		/// </summary>
		private int VSYNCWidth_Type0_3_4
		{
			get
			{
				// Bits 7..4 define Vertical Sync Width
				// If 0 is programmed this gives 16 lines of VSYNC
				var width = (Register[SYNC_WIDTHS] >> 4) & 0x0F;
				if (width == 0)
					width = 16;
				return width;
			}
		}

		/// <summary>
		/// Current programmed VSYNC width for Type 1 (UM6845R) & 2 (MC6845)
		/// </summary>
		private int VSYNCWidth_Type1_2
		{
			get
			{
				// Bits 7..4 are ignored. 
				// Vertical Sync is fixed at 16 lines.
				return 16;
			}
		}

		#endregion

		#region Register Access

		/// <summary>
		/// Read Register (HD6845S & UM6845)
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private bool ReadRegister_Type0(ref int data)
		{
			// Type 0 - write only register returns 0 when it is read from
			switch (AddressRegister)
			{
				// read-only registers
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
					data = 0;
					break;
				case 12:    // Start Address H (6-bit)
				case 14:    // Cursor H (6-bit)
				case 16:    // Light Pen H (6-bit)
					data = Register[AddressRegister] & 0x3F;
					break;
				case 13:    // Start Address L (8-bit)
				case 15:    // Cursor L (8-bit)
				case 17:    // Light Pen L (8-bit)
					data = Register[AddressRegister];
					break;
				default:
					if (AddressRegister > 17 && AddressRegister < 32)
					{
						data = 0;
					}
					else
					{
						return false;
					}
					break;
			}
			return true;
		}

		/// <summary>
		/// Read Register (UM6845R)
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private bool ReadRegister_Type1(ref int data)
		{
			// Type 1 - write only register returns 0 when it is read from
			switch (AddressRegister)
			{
				// read-only registers
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
				case 12:
				case 13:
					data = 0;
					break;
				case 14:    // Cursor H (6-bit)
					data = Register[AddressRegister] & 0x3F;
					break;
				case 16:    // Light Pen H (6-bit)
					data = Register[AddressRegister] & 0x3F;
					// reading from R16 resets bit6 of the status register
					StatusRegister &= byte.MaxValue ^ (1 << 6);
					break;
				case 15:    // Cursor L (8-bit)
					data = Register[AddressRegister];
					break;
				case 17:    // Light Pen L (8-bit)
					data = Register[AddressRegister];
					// reading from R17 resets bit6 of the status register
					StatusRegister &= byte.MaxValue ^ (1 << 6);
					break;
				case 31:    // Dummy Register. Datasheet describes this as N/A but CPCWIKI suggests that reading from it return 0x0FF;
					data = 0xff;
					break;
				default:
					if (AddressRegister > 17 && AddressRegister < 31)
					{
						data = 0;
					}
					else
					{
						return false;
					}
					break;
			}
			return true;
		}

		/// <summary>
		/// Read Register (MC6845)
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private bool ReadRegister_Type2(ref int data)
		{
			switch (AddressRegister)
			{
				// read-only registers - type 2 does not respond
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
				case 12:
				case 13:
					return false;
				case 14:    // Cursor H (6-bit)
					data = Register[AddressRegister] & 0x3F;
					break;
				case 16:    // Light Pen H (6-bit)
					data = Register[AddressRegister] & 0x3F;
					break;
				case 15:    // Cursor L (8-bit)
					data = Register[AddressRegister];
					break;
				case 17:    // Light Pen L (8-bit)
					data = Register[AddressRegister];
					break;
				default:
					if (AddressRegister > 17 && AddressRegister < 32)
					{
						data = 0;
					}
					else
					{
						return false;
					}
					break;
			}
			return true;
		}

		/// <summary>
		/// Read Register (AMS40489 & pre-ASIC)
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private bool ReadRegister_Type3_4(ref int data)
		{
			// unsure of the register sizes at the moment
			// for now we will just read and write 8-bit values
			switch (AddressRegister)
			{
				case 6:
				case 7:
				case 14:
				case 15:
				case 22:
				case 23:
				case 30:
				case 31:
					// returns 0
					data = 0;
					break;
				case 0:
				case 8:
				case 16:
				case 24:
					// returns R16
					data = Register[16];
					break;
				case 1:
				case 9:
				case 17:
				case 25:
					// returns R17
					data = Register[17];
					break;
				case 4:
				case 12:
				case 20:
				case 28:
					// returns R12
					data = Register[12];
					break;
				case 5:
				case 13:
				case 21:
				case 29:
					// returns R13
					data = Register[13];
					break;
				case 2:
				case 10:
				case 18:
				case 26:
					// ASIC status 1
					data = AsicStatusRegister1;
					break;
				case 3:
				case 11:
				case 19:
				case 27:
					// ASIC status 2
					data = AsicStatusRegister2;
					break;
			}
			return true;
		}

		/// <summary>
		/// Write Active Register (HD6845S & UM6845)
		/// </summary>
		/// <param name="data"></param>
		private void WriteRegister_Type0(int data)
		{
			byte v = (byte)data;
			switch (AddressRegister)
			{
				case 0:     // 8-bit registers
				case 1:
				case 2:
				case 3:
				case 13:
				case 15:
					Register[AddressRegister] = v;
					break;
				case 4:     // 7-bit registers
				case 6:
				case 7:
				case 10:
					Register[AddressRegister] = (byte)(v & 0x7F);
					break;
				case 12:    // 6-bit registers
				case 14:
					Register[AddressRegister] = (byte)(v & 0x3F);
					break;
				case 5:     // 5-bit registers
				case 9:
				case 11:
					Register[AddressRegister] = (byte)(v & 0x1F);
					break;
				case 8:     // Interlace & skew masks bits 2 & 3
					Register[AddressRegister] = (byte)(v & 0xF3);
					break;
			}
		}

		/// <summary>
		/// Write Active Register (HD6845S & UM6845)
		/// </summary>
		/// <param name="data"></param>
		private void WriteRegister_Type1(int data)
		{
			byte v = (byte)data;
			switch (AddressRegister)
			{
				case 0:     // 8-bit registers
				case 1:
				case 2:
				case 13:
				case 15:
					Register[AddressRegister] = v;
					break;
				case 4:     // 7-bit registers
				case 6:
				case 7:
				case 10:
					Register[AddressRegister] = (byte)(v & 0x7F);
					break;
				case 12:    // 6-bit registers
				case 14:
					Register[AddressRegister] = (byte)(v & 0x3F);
					break;
				case 5:     // 5-bit registers
				case 9:
				case 11:
					Register[AddressRegister] = (byte)(v & 0x1F);
					break;
				case 3:     // 4-bit register
					Register[AddressRegister] = (byte)(v & 0x0F);
					break;
				case 8:     // Interlace & skew - 2bit
					Register[AddressRegister] = (byte)(v & 0x03);
					break;
			}
		}

		/// <summary>
		/// Write Active Register (MC6845)
		/// </summary>
		/// <param name="data"></param>
		private void WriteRegister_Type2(int data)
		{
			byte v = (byte)data;
			switch (AddressRegister)
			{
				case 0:     // 8-bit registers
				case 1:
				case 2:
				case 13:
				case 15:
					Register[AddressRegister] = v;
					break;
				case 4:     // 7-bit registers
				case 6:
				case 7:
				case 10:
					Register[AddressRegister] = (byte)(v & 0x7F);
					break;
				case 12:    // 6-bit registers
				case 14:
					Register[AddressRegister] = (byte)(v & 0x3F);
					break;
				case 5:     // 5-bit registers
				case 9:
				case 11:
					Register[AddressRegister] = (byte)(v & 0x1F);
					break;
				case 3:     // 4-bit register
					Register[AddressRegister] = (byte)(v & 0x0F);
					break;
				case 8:     // Interlace & skew - 2bit
					Register[AddressRegister] = (byte)(v & 0x03);
					break;
			}
		}

		/// <summary>
		/// Write Active Register (MC6845)
		/// </summary>
		/// <param name="data"></param>
		private void WriteRegister_Type3_4(int data)
		{
			// unsure of the register sizes at the moment
			// for now we will just read and write 8-bit values
			byte v = (byte)data;
			switch (AddressRegister)
			{
				case 16:
				case 17:
					// read only registers
					return;
				default:
					if (AddressRegister < 16)
					{
						Register[AddressRegister] = v;
					}
					else
					{
						// read only dummy registers
						return;
					}
					break;
			}
		}

		/// <summary>
		/// Read Status Register (UM6845R)
		/// This is fully implemented
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private bool ReadStatus_Type1(ref int data)
		{
			// Bit6: Set when latched LPEN strobe input is received / Reset when R17 or R16 is read from
			// Bit5: Set when CRTC is in vblank / Reset when frame is started (VCC = 0)
			data = StatusRegister & 0x60;
			return true;
		}

		/// <summary>
		/// Read Status Register (AMS40489 and costdown pre-ASIC)
		/// Status Register is unavailable but attempts to read will return the currently
		/// selected register data instead
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private bool ReadStatus_Type3_4(ref int data)
		{
			return ReadRegister(ref data);
		}

		/// <summary>
		/// Read Status Register (HD6845S & UM6845)
		/// No status register available
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private bool ReadStatus_Unavailable(ref int data)
		{
			return false;
		}

		#endregion

		#endregion

		#region Clock Cycles

		/* persistent switch signals */		
		bool s_VS;
		bool s_HDISP;
		bool s_VDISP;
		bool s_HSYNC;

		/* Other chip counters */
		/// <summary>
		/// Linear Address Generator counter latch
		/// </summary>
		int LAG_Counter_Latch;

		/// <summary>
		/// Linear Address Generator row counter latch
		/// </summary>
		int LAG_Counter_RowLatch;

		/// <summary>
		/// Linear Address Generator counter
		/// </summary>
		int LAG_Counter;

		int DISPTMG_Delay_Counter;
		int CUDISP_Delay_Counter;
		int CUR_Field_Counter;

		/// <summary>
		/// Runs a generic CRTC cycle
		/// </summary>
		private void ClockCycle_Generic()
		{
			
		}

		/// <summary>
		/// Runs a Type0 Clock Cycle
		/// Type 0 is the only chip that implements display and cursor skew
		/// http://www.cpcwiki.eu/imgs/c/c0/Hd6845.hitachi.pdf
		/// </summary>
		private void ClockCycle_Type0()
		{
			/* non-persistent clock signals */
			bool c_VT = false;
			bool c_VTOTAL = false;
			bool c_HMAX = false;
			bool c_HDISP = false;
			bool c_RASMAX = false;
			bool c_VTAMAX = false;
			bool c_HALFHTOTAL = false;
			bool c_CURMATCH = false;
			bool c_CURSKEW = false;

			// we are going to clock everything individually but simulate everything
			// happeneing at once (exactly like the chip would do)

			/* Character Counter */
			CharacterCTR++;
			if (CharacterCTR == Register[H_DISPLAYED])
			{
				c_HDISP = true;
				s_HDISP = false;
			}
			if (CharacterCTR == Register[H_TOTAL])
			{
				c_HMAX = true;
				s_HDISP = true;
			}
			if (c_HMAX)
			{
				CharacterCTR = 0;
			}
			if (CharacterCTR == Register[H_SYNC_POS])
			{
				s_HSYNC = true;
			}
			if (CharacterCTR == Register[H_TOTAL] / 2)
			{
				c_HALFHTOTAL = true;
			}

			/* Horizontal Sync Width Counter */
			if (s_HSYNC)
			{
				HorizontalSyncWidthCTR++;
				if (HorizontalSyncWidthCTR == HSYNCWidth)
				{
					s_HSYNC = false;
					HorizontalSyncWidthCTR = 0;
				}
			}

			/* Raster Counter */
			if (c_HMAX)
			{
				RasterCTR++;
				if (RasterCTR == Register[MAX_SL_ADDRESS])
				{
					c_RASMAX = true;
					RasterCTR = 0;
				}
				if (RasterCTR == Register[V_DISPLAYED])
				{
					// this will probably never happen, but the Hd6845 block diagram
					// suggests that it is actually wired up this way
					s_VDISP = false;
				}
				if (RasterCTR == Register[V_TOTAL_ADJUST])
				{
					c_VTAMAX = true;
				}
			}

			/* Line Counter */
			if (c_RASMAX)
			{
				LineCTR++;
				if (LineCTR == Register[MAX_SL_ADDRESS])
				{
					// again this seems unneccessary, but the Hd6845 block diagram suggests that
					// it is indeed wired this way
					c_RASMAX = true;
					RasterCTR = 0;
				}
				if (LineCTR == Register[V_DISPLAYED])
				{
					s_VDISP = false;
				}
				if (LineCTR == Register[V_TOTAL])
				{
					c_VTOTAL = true;
				}
				if (LineCTR == Register[V_SYNC_POS])
				{
					s_VS = true;
					VerticalSyncWidthCTR = 0;
				}
			}

			/* Vertical Sync Width Counter */
			if (c_RASMAX && s_VS)
			{
				VerticalSyncWidthCTR++;
				if (VerticalSyncWidthCTR == VSYNCWidth)
				{
					s_VS = false;
				}
			}

			/* VTOTAL Control */
			// todo - interlace logic		
			if (c_VTOTAL)
			{
				// vertical total has been reached
				LineCTR = 0;
				RasterCTR = 0;
				// reload start address
				StartAddressLatch = StartAddressRegisterValue;
				c_VT = true;
				s_VDISP = true;
			}
			if (c_VTAMAX)
			{
				// extra adjust rasterlines have all been outputted (or there were none)
				// activate the Vdisplay switch
				LineCTR = 0;
				RasterCTR = 0;
				c_VT = true;
				s_VDISP = true;
			}

			/* Interlace Control */
			// the interlace control generates the RA0-RA4 row selects
			// this is based on the VT clock and VS switch,
			// the value of the Raster Counter and the current interlace mode
			// it is also clocked by a CO circuit when the horizontal character counter == HALF of the H_Total register value
			// (this is for the interlace sync & video mode I believe)
			// It also responsible for generating the signal on the VSYNC pin
			if (s_VS)
			{
				_VSYNC = true;
			}
			if (c_VT)
			{
				_VSYNC = false;
			}
			if (c_HMAX)
			{
				RowSelects = RasterCTR;
			}
			if (c_HALFHTOTAL)
			{
				// we are half way horizontally across the screen
				// interlace sync and video logic should go here (todo)
			}

			/* Linear Address Generator */
			// counter is incremented with every CLK signal
			LAG_Counter++;

			if (c_HDISP)
			{
				// horizontal displayed reached - latch this address
				// this is needed when moving to the next vertical character
				// (the CRTC continues counting during the border and HSYNC periods)
				LAG_Counter_Latch = LAG_Counter;
			}
			if (c_HMAX)
			{
				// end of the current raster line
				if (c_RASMAX)
				{
					// this is last raster in the current line
					// counter will continue on from the last row latch
					LAG_Counter = LAG_Counter_Latch;
					// latch this value to be used in all the raster lines in the following line
					LAG_Counter_RowLatch = LAG_Counter;
				}
				else
				{
					// still within the line
					// every raster will generate the same address sequence
					LAG_Counter = LAG_Counter_RowLatch;
				}
			}
			if (c_VT)
			{
				// the VT clock has been received from the VCONTROL
				// LAG counters reset
				LAG_Counter_Latch = 0;
				LAG_Counter_RowLatch = 0;
				LAG_Counter = 0;
			}

			// setup MA0-MA13 outputs based on internal counters and programmed start address
			// (this is latched elsewhere and the timing of this is CRTC-type dependent)
			LinearAddress = StartAddressLatch + LAG_Counter;

			if (LinearAddress == CursorAddressRegisterValue)
			{
				c_CURMATCH = true;
			}

			/* HSYNC */
			_HSYNC = s_HSYNC;

			/* DISPTMG skew control */
			if (DISPTMGSkew < 0)
			{
				// no output
				_DISPTMG = false;
				DISPTMG_Delay_Counter = 0;
			}
			else if (s_HDISP && s_VDISP)
			{
				// AND gate feeding the skew control is active
				if (DISPTMGSkew > 0)
				{
					// skew value is set
					if (DISPTMG_Delay_Counter >= DISPTMGSkew)
					{
						// we have finished the start skew
						_DISPTMG = true;
					}
					else
					{
						// start skew still happening
						DISPTMG_Delay_Counter++;
						_DISPTMG = false;
					}
				}
				else
				{
					// no skew set
					_DISPTMG = true;
				}
			}
			else
			{
				// AND gate is inactive - process any possible skew leadout
				if (DISPTMGSkew > 0)
				{
					// skew value is set
					if (DISPTMG_Delay_Counter > 0)
					{
						// leadout skew is still in effect
						DISPTMG_Delay_Counter--;
						_DISPTMG = true;
					}
					else
					{
						// leadout skew has finished
						DISPTMG_Delay_Counter = 0;
						_DISPTMG = false;
					}
				}
				else
				{
					// no skew programmed
					DISPTMG_Delay_Counter = 0;
					_DISPTMG = false;
				}
			}

			/* Cursor Control */
			if (s_HDISP && s_VDISP)
			{
				// AND gate is active - cursor control is clocked
				CUR_Field_Counter++;
				bool curOutput = false;

				// info from registers
				var curStartRaster = Register[CURSOR_START] & 0x1f;
				var curEndRaster = Register[CURSOR_END] & 0x1f;
				var curDisplayMode = (Register[CURSOR_START] & 0x60) >> 5;

				switch (curDisplayMode)
				{
					// Non-blink
					case 0:
						if (RasterCTR >= curStartRaster && RasterCTR <= curEndRaster && c_CURMATCH)
							curOutput = true;
						break;
					// Cursor non-display
					case 1:
						curOutput = false;
						break;
					// Blink 1/16 field rate
					case 2:
						// not yet implemented
						if (RasterCTR >= curStartRaster && RasterCTR <= curEndRaster && c_CURMATCH)
							curOutput = true;
						break;
					// Blink 1/32 field rate
					case 3:
						// not yet implemented
						if (RasterCTR >= curStartRaster && RasterCTR <= curEndRaster && c_CURMATCH)
							curOutput = true;
						break;
				}

				if (curOutput)
				{
					c_CURSKEW = true;
				}
			}
			else
			{
				// end of the display
				CUR_Field_Counter = 0;
			}

			/* Cursor Skew Control */
			if (c_CURSKEW)
			{
				if (CUDISPSkew < 0)
				{
					// no output
					_CUDISP = false;
					CUDISP_Delay_Counter = 0;
				}
				else
				{
					if (CUDISPSkew > 0)
					{
						// skew value is set
						if (CUDISP_Delay_Counter >= CUDISPSkew)
						{
							// we have finished the start skew
							_CUDISP = true;
						}
						else
						{
							// start skew still happening
							CUDISP_Delay_Counter++;
							_CUDISP = false;
						}
					}
					else
					{
						// no skew set
						_CUDISP = true;
					}
				}
			}
			else
			{
				// process any possible skew leadout
				if (CUDISPSkew > 0)
				{
					// skew value is set
					if (CUDISP_Delay_Counter > 0)
					{
						// leadout skew is still in effect
						CUDISP_Delay_Counter--;
						_CUDISP = true;
					}
					else
					{
						// leadout skew has finished
						CUDISP_Delay_Counter = 0;
						_CUDISP = false;
					}
				}
				else
				{
					// no skew programmed
					CUDISP_Delay_Counter = 0;
					_CUDISP = false;
				}
			}

			/* Light Pen Control */
			if (LPSTB)
			{
				// strobe has been detected
				// latch the current address into the light pen registers (R16 & R17)
				Register[LIGHT_PEN_L] = (byte)(LinearAddress & 0xff);
				Register[LIGHT_PEN_H] = (byte)((LinearAddress >> 8) & 0x3f);

				_LPSTB = false;
			}
		}

		/// <summary>
		/// Runs a Type1 Clock Cycle
		/// There doesnt seem to be a block diagram in the datasheets for this, so will use the type0
		/// implementation with a few changes
		/// Type 1 has no skew program bit functionality
		/// However, it does implement a status register
		/// </summary>
		private void ClockCycle_Type1()
		{
			/* non-persistent clock signals */
			bool c_VT = false;
			bool c_VTOTAL = false;
			bool c_HMAX = false;
			bool c_HDISP = false;
			bool c_RASMAX = false;
			bool c_VTAMAX = false;
			bool c_HALFHTOTAL = false;
			bool c_CURMATCH = false;

			// we are going to clock everything individually but simulate everything
			// happeneing at once (exactly like the chip would do)

			/* Character Counter */
			CharacterCTR++;
			if (CharacterCTR == Register[H_DISPLAYED])
			{
				c_HDISP = true;
				s_HDISP = false;
			}
			if (CharacterCTR == Register[H_TOTAL])
			{
				c_HMAX = true;
				s_HDISP = true;
			}
			if (c_HMAX)
			{
				CharacterCTR = 0;
			}
			if (CharacterCTR == Register[H_SYNC_POS])
			{
				s_HSYNC = true;
			}
			if (CharacterCTR == Register[H_TOTAL] / 2)
			{
				c_HALFHTOTAL = true;
			}

			/* Horizontal Sync Width Counter */
			if (s_HSYNC)
			{
				HorizontalSyncWidthCTR++;
				if (HorizontalSyncWidthCTR == HSYNCWidth)
				{
					s_HSYNC = false;
					HorizontalSyncWidthCTR = 0;
				}
			}

			/* Raster Counter */
			if (c_HMAX)
			{
				RasterCTR++;
				if (RasterCTR == Register[MAX_SL_ADDRESS])
				{
					c_RASMAX = true;
					RasterCTR = 0;
				}
				if (RasterCTR == Register[V_DISPLAYED])
				{
					// this will probably never happen, but the Hd6845 block diagram
					// suggests that it is actually wired up this way
					s_VDISP = false;
				}
				if (RasterCTR == Register[V_TOTAL_ADJUST])
				{
					c_VTAMAX = true;
				}
			}

			/* Line Counter */
			if (c_RASMAX)
			{
				LineCTR++;
				if (LineCTR == Register[MAX_SL_ADDRESS])
				{
					// again this seems unneccessary, but the Hd6845 block diagram suggests that
					// it is indeed wired this way
					c_RASMAX = true;
					RasterCTR = 0;
				}
				if (LineCTR == Register[V_DISPLAYED])
				{
					s_VDISP = false;
				}
				if (LineCTR == Register[V_TOTAL])
				{
					c_VTOTAL = true;
				}
				if (LineCTR == Register[V_SYNC_POS])
				{
					s_VS = true;
					VerticalSyncWidthCTR = 0;
				}
			}

			/* Vertical Sync Width Counter */
			if (c_RASMAX && s_VS)
			{
				VerticalSyncWidthCTR++;
				if (VerticalSyncWidthCTR == VSYNCWidth)
				{
					s_VS = false;
				}
			}

			/* VTOTAL Control */
			// todo - interlace logic		
			if (c_VTOTAL)
			{
				// vertical total has been reached
				LineCTR = 0;
				RasterCTR = 0;
				// reload start address
				StartAddressLatch = StartAddressRegisterValue;
				c_VT = true;
				s_VDISP = true;
			}
			if (c_VTAMAX)
			{
				// extra adjust rasterlines have all been outputted (or there were none)
				// activate the Vdisplay switch
				LineCTR = 0;
				RasterCTR = 0;
				c_VT = true;
				s_VDISP = true;
			}

			/* Interlace Control */
			// the interlace control generates the RA0-RA4 row selects
			// this is based on the VT clock and VS switch,
			// the value of the Raster Counter and the current interlace mode
			// it is also clocked by a CO circuit when the horizontal character counter == HALF of the H_Total register value
			// (this is for the interlace sync & video mode I believe)
			// It also responsible for generating the signal on the VSYNC pin
			if (s_VS)
			{
				_VSYNC = true;
				// status register bit 5 'vertical blanking'
				StatusRegister |= 1 << 5;
			}
			if (c_VT)
			{
				_VSYNC = false;
				// status register bit 5 'vertical blanking'
				StatusRegister &= byte.MaxValue ^ (1 << 5);
			}
			if (c_HMAX)
			{
				RowSelects = RasterCTR;
			}
			if (c_HALFHTOTAL)
			{
				// we are half way horizontally across the screen
				// interlace sync and video logic should go here (todo)
			}

			/* Linear Address Generator */
			// counter is incremented with every CLK signal
			LAG_Counter++;

			if (c_HDISP)
			{
				// horizontal displayed reached - latch this address
				// this is needed when moving to the next vertical character
				// (the CRTC continues counting during the border and HSYNC periods)
				LAG_Counter_Latch = LAG_Counter;
			}
			if (c_HMAX)
			{
				// end of the current raster line
				if (c_RASMAX)
				{
					// this is last raster in the current line
					// counter will continue on from the last row latch
					LAG_Counter = LAG_Counter_Latch;
					// latch this value to be used in all the raster lines in the following line
					LAG_Counter_RowLatch = LAG_Counter;
				}
				else
				{
					// still within the line
					// every raster will generate the same address sequence
					LAG_Counter = LAG_Counter_RowLatch;
				}
			}
			if (c_VT)
			{
				// the VT clock has been received from the VCONTROL
				// LAG counters reset
				LAG_Counter_Latch = 0;
				LAG_Counter_RowLatch = 0;
				LAG_Counter = 0;
			}

			// setup MA0-MA13 outputs based on internal counters and programmed start address
			// (this is latched elsewhere and the timing of this is CRTC-type dependent)
			LinearAddress = StartAddressLatch + LAG_Counter;

			if (LinearAddress == CursorAddressRegisterValue)
			{
				c_CURMATCH = true;
			}

			/* HSYNC */
			_HSYNC = s_HSYNC;

			/* DISPTMG */
			if (s_HDISP && s_VDISP)
			{
				_DISPTMG = true;
			}
			else
			{
				_DISPTMG = false;
			}			

			/* Cursor Control */
			if (s_HDISP && s_VDISP)
			{
				// AND gate is active - cursor control is clocked
				CUR_Field_Counter++;
				bool curOutput = false;

				// info from registers
				var curStartRaster = Register[CURSOR_START] & 0x1f;
				var curEndRaster = Register[CURSOR_END] & 0x1f;
				var curDisplayMode = (Register[CURSOR_START] & 0x60) >> 5;

				switch (curDisplayMode)
				{
					// Non-blink
					case 0:
						if (RasterCTR >= curStartRaster && RasterCTR <= curEndRaster && c_CURMATCH)
							curOutput = true;
						break;
					// Cursor non-display
					case 1:
						curOutput = false;
						break;
					// Blink 1/16 field rate
					case 2:
						// not yet implemented
						if (RasterCTR >= curStartRaster && RasterCTR <= curEndRaster && c_CURMATCH)
							curOutput = true;
						break;
					// Blink 1/32 field rate
					case 3:
						// not yet implemented
						if (RasterCTR >= curStartRaster && RasterCTR <= curEndRaster && c_CURMATCH)
							curOutput = true;
						break;
				}

				if (curOutput)
				{
					_CUDISP = true;
				}
				else
				{
					_CUDISP = false;
				}
			}
			else
			{
				// end of the display
				CUR_Field_Counter = 0;
			}

			/* Light Pen Control */
			if (LPSTB)
			{
				// strobe has been detected
				// latch the current address into the light pen registers (R16 & R17)
				Register[LIGHT_PEN_L] = (byte)(LinearAddress & 0xff);
				Register[LIGHT_PEN_H] = (byte)((LinearAddress >> 8) & 0x3f);

				// set the 'LPEN register full' bit in the status register
				StatusRegister |= 1 << 6;

				_LPSTB = false;
			}
		}

		/// <summary>
		/// Runs a Type2 Clock Cycle
		/// The MC6845 does have a functional block diagram
		/// http://www.cpcwiki.eu/imgs/d/da/Mc6845.motorola.pdf
		/// The implementation looks a little simpler than the type 0
		/// It has NO status register and NO skew program bit support
		/// HOWEVER, there are some glaring ommissions in the block diagram, 
		/// so I am using a modified type 0/1 implementation for now
		/// </summary>
		private void ClockCycle_Type2()
		{
			/* non-persistent clock signals */
			bool c_VT = false;
			bool c_VTOTAL = false;
			bool c_HMAX = false;
			bool c_HDISP = false;
			bool c_RASMAX = false;
			bool c_VTAMAX = false;
			bool c_HALFHTOTAL = false;
			bool c_CURMATCH = false;

			// we are going to clock everything individually but simulate everything
			// happeneing at once (exactly like the chip would do)

			/* Character Counter */
			CharacterCTR++;
			if (CharacterCTR == Register[H_DISPLAYED])
			{
				c_HDISP = true;
				s_HDISP = false;
			}
			if (CharacterCTR == Register[H_TOTAL])
			{
				c_HMAX = true;
				s_HDISP = true;
			}
			if (c_HMAX)
			{
				CharacterCTR = 0;
			}
			if (CharacterCTR == Register[H_SYNC_POS])
			{
				s_HSYNC = true;
			}
			if (CharacterCTR == Register[H_TOTAL] / 2)
			{
				c_HALFHTOTAL = true;
			}

			/* Horizontal Sync Width Counter */
			if (s_HSYNC)
			{
				HorizontalSyncWidthCTR++;
				if (HorizontalSyncWidthCTR == HSYNCWidth)
				{
					s_HSYNC = false;
					HorizontalSyncWidthCTR = 0;
				}
			}

			/* Raster Counter */
			if (c_HMAX)
			{
				RasterCTR++;
				if (RasterCTR == Register[MAX_SL_ADDRESS])
				{
					c_RASMAX = true;
					RasterCTR = 0;
				}
				if (RasterCTR == Register[V_DISPLAYED])
				{
					// this will probably never happen, but the Hd6845 block diagram
					// suggests that it is actually wired up this way
					s_VDISP = false;
				}
				if (RasterCTR == Register[V_TOTAL_ADJUST])
				{
					c_VTAMAX = true;
				}
			}

			/* Line Counter */
			if (c_RASMAX)
			{
				LineCTR++;
				if (LineCTR == Register[MAX_SL_ADDRESS])
				{
					// again this seems unneccessary, but the Hd6845 block diagram suggests that
					// it is indeed wired this way
					c_RASMAX = true;
					RasterCTR = 0;
				}
				if (LineCTR == Register[V_DISPLAYED])
				{
					s_VDISP = false;
				}
				if (LineCTR == Register[V_TOTAL])
				{
					c_VTOTAL = true;
				}
				if (LineCTR == Register[V_SYNC_POS])
				{
					s_VS = true;
					VerticalSyncWidthCTR = 0;
				}
			}

			/* Vertical Sync Width Counter */
			if (c_RASMAX && s_VS)
			{
				VerticalSyncWidthCTR++;
				if (VerticalSyncWidthCTR == VSYNCWidth)
				{
					s_VS = false;
				}
			}

			/* VTOTAL Control */
			// todo - interlace logic		
			if (c_VTOTAL)
			{
				// vertical total has been reached
				LineCTR = 0;
				RasterCTR = 0;
				// reload start address
				StartAddressLatch = StartAddressRegisterValue;
				c_VT = true;
				s_VDISP = true;
			}
			if (c_VTAMAX)
			{
				// extra adjust rasterlines have all been outputted (or there were none)
				// activate the Vdisplay switch
				LineCTR = 0;
				RasterCTR = 0;
				c_VT = true;
				s_VDISP = true;
			}

			/* Interlace Control */
			// the interlace control generates the RA0-RA4 row selects
			// this is based on the VT clock and VS switch,
			// the value of the Raster Counter and the current interlace mode
			// it is also clocked by a CO circuit when the horizontal character counter == HALF of the H_Total register value
			// (this is for the interlace sync & video mode I believe)
			// It also responsible for generating the signal on the VSYNC pin
			if (s_VS)
			{
				_VSYNC = true;
			}
			if (c_VT)
			{
				_VSYNC = false;
			}
			if (c_HMAX)
			{
				RowSelects = RasterCTR;
			}
			if (c_HALFHTOTAL)
			{
				// we are half way horizontally across the screen
				// interlace sync and video logic should go here (todo)
			}

			/* Linear Address Generator */
			// counter is incremented with every CLK signal
			LAG_Counter++;

			if (c_HDISP)
			{
				// horizontal displayed reached - latch this address
				// this is needed when moving to the next vertical character
				// (the CRTC continues counting during the border and HSYNC periods)
				LAG_Counter_Latch = LAG_Counter;
			}
			if (c_HMAX)
			{
				// end of the current raster line
				if (c_RASMAX)
				{
					// this is last raster in the current line
					// counter will continue on from the last row latch
					LAG_Counter = LAG_Counter_Latch;
					// latch this value to be used in all the raster lines in the following line
					LAG_Counter_RowLatch = LAG_Counter;
				}
				else
				{
					// still within the line
					// every raster will generate the same address sequence
					LAG_Counter = LAG_Counter_RowLatch;
				}
			}
			if (c_VT)
			{
				// the VT clock has been received from the VCONTROL
				// LAG counters reset
				LAG_Counter_Latch = 0;
				LAG_Counter_RowLatch = 0;
				LAG_Counter = 0;
			}

			// setup MA0-MA13 outputs based on internal counters and programmed start address
			// (this is latched elsewhere and the timing of this is CRTC-type dependent)
			LinearAddress = StartAddressLatch + LAG_Counter;

			if (LinearAddress == CursorAddressRegisterValue)
			{
				c_CURMATCH = true;
			}

			/* HSYNC */
			_HSYNC = s_HSYNC;

			/* DISPTMG */
			if (s_HDISP && s_VDISP)
			{
				_DISPTMG = true;
			}
			else
			{
				_DISPTMG = false;
			}

			/* Cursor Control */
			if (s_HDISP && s_VDISP)
			{
				// AND gate is active - cursor control is clocked
				CUR_Field_Counter++;
				bool curOutput = false;

				// info from registers
				var curStartRaster = Register[CURSOR_START] & 0x1f;
				var curEndRaster = Register[CURSOR_END] & 0x1f;
				var curDisplayMode = (Register[CURSOR_START] & 0x60) >> 5;

				switch (curDisplayMode)
				{
					// Non-blink
					case 0:
						if (RasterCTR >= curStartRaster && RasterCTR <= curEndRaster && c_CURMATCH)
							curOutput = true;
						break;
					// Cursor non-display
					case 1:
						curOutput = false;
						break;
					// Blink 1/16 field rate
					case 2:
						// not yet implemented
						if (RasterCTR >= curStartRaster && RasterCTR <= curEndRaster && c_CURMATCH)
							curOutput = true;
						break;
					// Blink 1/32 field rate
					case 3:
						// not yet implemented
						if (RasterCTR >= curStartRaster && RasterCTR <= curEndRaster && c_CURMATCH)
							curOutput = true;
						break;
				}

				if (curOutput)
				{
					_CUDISP = true;
				}
				else
				{
					_CUDISP = false;
				}
			}
			else
			{
				// end of the display
				CUR_Field_Counter = 0;
			}

			/* Light Pen Control */
			if (LPSTB)
			{
				// strobe has been detected
				// latch the current address into the light pen registers (R16 & R17)
				Register[LIGHT_PEN_L] = (byte)(LinearAddress & 0xff);
				Register[LIGHT_PEN_H] = (byte)((LinearAddress >> 8) & 0x3f);

				_LPSTB = false;
			}
		}

		/// <summary>
		/// Runs a Type3or4 Clock Cycle
		/// </summary>
		private void ClockCycle_Type3_4()
		{

		}

		#endregion

		#region Enums & Constants

		/* Horizontal Timing Register Constants */
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

		/* Vertical Timing Register Constants */
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

		/* Other Main Register Constants */
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

		/// <summary>
		/// The types of CRCT chip found in the CPC range
		/// </summary>
		public enum CRTCType
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

		/// <summary>
		/// Vid Display RAM Addressing Mode
		/// </summary>
		private enum VideoDisplayRAMAddressing
		{
			StraightBinary = 0,
			RowColumn = 1
		}

		/// <summary>
		/// Vid Display RAM Acccess Mode
		/// </summary>
		private enum VideoDisplayRAMAccess
		{
			SharedMemory,
			TransparentMemory
		}

		#endregion

		#region Serialization

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("CRCT");
			ser.SyncEnum("ChipType", ref ChipType);
			ser.Sync("_VSYNC", ref _VSYNC);
			ser.Sync("_HSYNC", ref _HSYNC);
			ser.Sync("_DISPTMG", ref _DISPTMG);
			ser.Sync("_CUDISP", ref _CUDISP);
			ser.Sync("_CLK", ref _CLK);
			ser.Sync("_RESET", ref _RESET);
			ser.Sync("_LPSTB", ref _LPSTB);
			ser.Sync("AddressRegister", ref AddressRegister);
			ser.Sync("Register", ref Register, false);
			ser.Sync("StatusRegister", ref StatusRegister);
			ser.Sync("_CharacterCTR", ref _CharacterCTR);
			ser.Sync("_HorizontalSyncWidthCTR", ref _HorizontalSyncWidthCTR);
			ser.Sync("_LineCTR", ref _LineCTR);
			ser.Sync("_RasterCTR", ref _RasterCTR);
			ser.Sync("StartAddressLatch)", ref StartAddressLatch);
			//ser.Sync("VDisplay", ref VDisplay);
			//ser.Sync("HDisplay", ref HDisplay);
			ser.Sync("RowSelects", ref RowSelects);
			ser.Sync("DISPTMG_Delay_Counter", ref DISPTMG_Delay_Counter);
			ser.Sync("CUDISP_Delay_Counter", ref CUDISP_Delay_Counter);
			ser.Sync("AsicStatusRegister1", ref AsicStatusRegister1);
			ser.Sync("AsicStatusRegister2", ref AsicStatusRegister2);
			ser.Sync("LAG_Counter", ref LAG_Counter);
			ser.Sync("LAG_Counter_Latch", ref LAG_Counter_Latch);
			ser.Sync("LAG_Counter_RowLatch", ref LAG_Counter_RowLatch);
			ser.Sync("s_VS", ref s_VS);
			ser.Sync("s_HDISP", ref s_VS);
			ser.Sync("s_VDISP", ref s_VDISP);
			ser.Sync("s_HSYNC", ref s_HSYNC);
			ser.Sync("CUR_Field_Counter", ref CUR_Field_Counter);
			//ser.Sync("VS", ref VS);
			ser.EndSection();

			/*
			 * int ;
		int ;
             * */
		}

		#endregion
	}
}
