using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using System.Collections;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	/// <summary>
	/// CATHODE RAY TUBE CONTROLLER (CRTC) IMPLEMENTATION
	/// http://www.cpcwiki.eu/index.php/CRTC
	/// http://cpctech.cpc-live.com/docs/cpcplus.html
	/// https://shaker.logonsystem.eu/
	/// https://shaker.logonsystem.eu/ACCC1.8-EN.pdf
	/// https://shaker.logonsystem.eu/tests
	/// This implementation aims to emulate all the various CRTC chips that appear within
	/// the CPC, CPC+ and GX4000 ranges. The CPC community have assigned them type numbers.
	/// If different implementations share the same type number it indicates that they are functionally identical
	/// 
	/// Part No.      Manufacturer    Type No.    Info.
	/// ------------------------------------------------------------------------------------------------------
	/// HD6845S       Hitachi         0
	/// Datasheet:    http://www.cpcwiki.eu/imgs/c/c0/Hd6845.hitachi.pdf
	/// ------------------------------------------------------------------------------------------------------
	/// UM6845        UMC             0
	/// Datasheet:    http://www.cpcwiki.eu/imgs/1/13/Um6845.umc.pdf
	/// ------------------------------------------------------------------------------------------------------
	/// UM6845R       UMC             1
	/// Datasheet:    http://www.cpcwiki.eu/imgs/b/b5/Um6845r.umc.pdf
	/// ------------------------------------------------------------------------------------------------------
	/// MC6845        Motorola        2
	/// Datasheet:    http://www.cpcwiki.eu/imgs/d/da/Mc6845.motorola.pdf &amp; http://bitsavers.trailing-edge.com/components/motorola/_dataSheets/6845.pdf
	/// ------------------------------------------------------------------------------------------------------
	/// AMS40489      Amstrad         3           Only exists in the CPC464+, CPC6128+ and GX4000 and is integrated into a single CPC+ ASIC chip (along with the gatearray)
	/// Datasheet:    {none}
	/// ------------------------------------------------------------------------------------------------------
	/// AMS40041      Amstrad         4           'Pre-ASIC' IC. The CRTC is integrated into a aingle ASIC IC with functionality being almost identical to the AMS40489
	/// (or 40226)                                Used in the 'Cost-Down' range of CPC464 and CPC6128 systems
	/// Datasheet:    {none}
	///
	/// </summary>
	public abstract partial class CRTC : IPortIODevice
	{
		/// <summary>
		/// Instantiation helper
		/// </summary>
		public static CRTC Create(int crtcType)
		{
			return crtcType switch
			{
				0 => new CRTC_Type0(),
				2 => new CRTC_Type2(),
				3 => new CRTC_Type3(),
				4 => new CRTC_Type4(),
				_ => new CRTC_Type1(),
			};
		}


		/// <summary>
		/// Defined CRTC type number
		/// </summary>
		public virtual int CrtcType { get; }

		/// <summary>
		/// CPC register default values
		/// </summary>
		public byte[] RegDefaults = { 63, 40, 46, 142, 38, 0, 25, 30, 0, 7, 0, 0, 48, 0, 192, 7, 0, 0 };

		/// <summary>
		/// The ClK isaTTUMOS-compatible input used to synchronize all CRT' functions except for the processor interface. 
		/// An external dot counter is used to derive this signal which is usually the character rate in an alphanumeric CRT.
		/// The active transition is high-to-low
		/// </summary>
		public bool CLK;

		/// <summary>
		/// This TTL compatible  output is an active high signal which drives the monitor directly or is fed to Video Processing Logic for composite generation.
		/// This signal determines the horizontal position of the displayed text. 
		/// </summary>
		public virtual bool HSYNC
		{
			get => _HSYNC;
			protected set
			{
				if (value != _HSYNC)
				{
					// value has changed
					if (value) { HSYNC_On_Callbacks(); }
					else { HSYNC_Off_Callbacks(); }
				}
				_HSYNC = value;
			}
		}
		private bool _HSYNC;

		/// <summary>
		/// This TTL compatible output is an active high signal which drives the monitor directly or is fed to Video Processing Logic for composite generation.
		/// This signal determines the vertical position of the displayed text.
		/// </summary>
		public virtual bool VSYNC
		{
			get => _VSYNC;
			protected set
			{
				if (value != _VSYNC)
				{
					// value has changed
					if (value) { VSYNC_On_Callbacks(); }
					else { VSYNC_Off_Callbacks(); }
				}
				_VSYNC = value;
			}
		}
		private bool _VSYNC;

		/// <summary>
		/// This TTL compatible output is an active high signal which indicates the CRTC is providing addressing in the active Display Area.
		/// </summary>      
		public virtual bool DISPTMG
		{
			get => _DISPTMG;
			protected set => _DISPTMG = value;
		}
		private bool _DISPTMG;

		/// <summary>
		/// This TTL compatible output indicates Cursor Display to external Video Processing Logic.Active high signal. 
		/// </summary>       
		public virtual bool CUDISP
		{
			get => _CUDISP;
			protected set => _CUDISP = value;
		}
		private bool _CUDISP;

		/// <summary>
		/// Linear Address Generator
		/// Character pos address (0 index).
		/// Feeds the MA lines
		/// </summary>
		protected int ma;

		/// <summary>
		/// Memory address reset latch
		/// </summary>
		protected int ma_row_start;

		/// <summary>
		/// Internal latch for storing intermediate MA values
		/// </summary>
		protected int ma_store;

		/// <summary>
		/// Generated by the Vertical Control Raster Counter
		/// Feeds the RA lines
		/// </summary>
		protected int _RA;

		/// <summary>
		/// This 16-bit property emulates how the Amstrad CPC Gate Array is wired up to the CRTC
		/// Built from LA, RA and CLK
		/// 
		/// Memory Address Signal    Signal source    Signal name
		/// A15                      6845             MA13
		/// A14                      6845             MA12
		/// A13                      6845             RA2
		/// A12                      6845             RA1
		/// A11                      6845             RA0
		/// A10                      6845             MA9
		/// A9                       6845             MA8
		/// A8                       6845             MA7
		/// A7                       6845             MA6
		/// A6                       6845             MA5
		/// A5                       6845             MA4
		/// A4                       6845             MA3
		/// A3                       6845             MA2
		/// A2                       6845             MA1
		/// A1                       6845             MA0
		/// A0                       Gate-Array       CLK
		/// </summary>		
		public ushort MA_Address
		{
			get
			{
				var MA = new BitArray(16);
				MA[0] = CLK;
				MA[1] = ma.Bit(0);
				MA[2] = ma.Bit(1);
				MA[3] = ma.Bit(2);
				MA[4] = ma.Bit(3);
				MA[5] = ma.Bit(4);
				MA[6] = ma.Bit(5);
				MA[7] = ma.Bit(6);
				MA[8] = ma.Bit(7);
				MA[9] = ma.Bit(8);
				MA[10] = ma.Bit(9);
				MA[11] = VLC.Bit(0);
				MA[12] = VLC.Bit(1);
				MA[13] = VLC.Bit(2);
				MA[14] = ma.Bit(12);
				MA[15] = ma.Bit(13);
				int[] array = new int[1];
				MA.CopyTo(array, 0);
				return (ushort)array[0];
			}
		}

		/// <summary>
		/// Public Delegate
		/// </summary>
		public delegate void CallBack();
		/// <summary>
		/// Fired on CRTC HSYNC signal rising edge
		/// </summary>
		protected CallBack HSYNC_On_Callbacks;
		/// <summary>
		/// Fired on CRTC HSYNC signal falling edge
		/// </summary>
		protected CallBack HSYNC_Off_Callbacks;
		/// <summary>
		/// Fired on CRTC VSYNC signal rising edge
		/// </summary>
		protected CallBack VSYNC_On_Callbacks;
		/// <summary>
		/// Fired on CRTC VSYNC signal falling edge
		/// </summary>
		protected CallBack VSYNC_Off_Callbacks;

		public void AttachHSYNCOnCallback(CallBack hCall) => HSYNC_On_Callbacks += hCall;
		public void AttachHSYNCOffCallback(CallBack hCall) => HSYNC_Off_Callbacks += hCall;
		public void AttachVSYNCOnCallback(CallBack vCall) => VSYNC_On_Callbacks += vCall;
		public void AttachVSYNCOffCallback(CallBack vCall) => VSYNC_Off_Callbacks += vCall;

		/// <summary>
		/// Reset Counter
		/// </summary>
		protected int _inReset;

		/// <summary>
		/// This is a 5 bit register which is used as a pointer to direct data transfers to and from the system MPU
		/// </summary>
		protected byte AddressRegister
		{
			get => (byte)(_addressRegister & 0b0001_1111);
			set => _addressRegister = (byte)(value & 0b0001_1111);
		}
		private byte _addressRegister;

		/// <summary>
		/// This 8 bit write-only register determines the horizontal frequency of HS. 
		/// It is the total of displayed plus non-displayed character time units minus one.
		/// </summary>
		protected const int R0_H_TOTAL = 0;
		/// <summary>
		/// This 8 bit write-only register determines the number of displayed characters per horizontal line.
		/// </summary>
		protected const int R1_H_DISPLAYED = 1;
		/// <summary>
		/// This 8 bit write-only register determines the horizontal sync postiion on the horizontal line.
		/// </summary>
		protected const int R2_H_SYNC_POS = 2;
		/// <summary>
		/// This 4 bit  write-only register determines the width of the HS pulse. It may not be apparent why this width needs to be programmed.However, 
		/// consider that all timing widths must be programmed as multiples of the character clock period which varies.If HS width were fixed as an integral 
		/// number of character times, it would vary with character rate and be out of tolerance for certain monitors.
		/// The rate programmable feature allows compensating HS width.
		/// NOTE: Dependent on chiptype this also may include VSYNC width - check the UpdateWidths() method
		/// </summary>
		protected const int R3_SYNC_WIDTHS = 3;

		/* Vertical Timing Register Constants */
		/// <summary>
		/// The vertical frequency of VS is determined by both R4 and R5.The calculated number of character I ine times is usual I y an integer plus a fraction to 
		/// get exactly a 50 or 60Hz vertical refresh rate. The integer number of character line times minus one is programmed in the 7 bit write-only Vertical Total Register; 
		/// the fraction is programmed in the 5 bit write-only Vertical Scan Adjust Register as a number of scan line times.
		/// </summary>
		protected const int R4_V_TOTAL = 4;
		protected const int R5_V_TOTAL_ADJUST = 5;
		/// <summary>
		/// This 7 bit write-only register determines the number of displayed character rows on the CRT screen, and is programmed in character row times.
		/// </summary>
		protected const int R6_V_DISPLAYED = 6;
		/// <summary>
		/// This 7 bit write-only register determines the vertical sync position with respect to the reference.It is programmed in character row times.
		/// </summary>
		protected const int R7_V_SYNC_POS = 7;
		/// <summary>
		/// This 2 bit write-only  register controls the raster scan mode(see Figure 11 ). When bit 0 and bit 1 are reset, or bit 0 is reset and bit 1 set, 
		/// the non· interlace raster scan mode is selected.Two interlace modes are available.Both are interlaced 2 fields per frame.When bit 0 is set and bit 1 is reset, 
		/// the interlace sync raster scan mode is selected.Also when bit 0 and bit 1 are set, the interlace sync and video raster scan mode is selected.
		/// </summary>
		protected const int R8_INTERLACE_MODE = 8;
		/// <summary>
		/// This 5 bit write·only register determines the number of scan lines per character row including spacing.
		/// The programmed value is a max address and is one less than the number of scan l1nes.
		/// </summary>
		protected const int R9_MAX_SL_ADDRESS = 9;
		/// <summary>
		/// This 7 bit write-only register controls the cursor format(see Figure 10). Bit 5 is the blink timing control.When bit 5 is low, the blink frequency is 1/16 of the 
		/// vertical field rate, and when bit 5 is high, the blink frequency is 1/32 of the vertical field rate.Bit 6 is used to enable a blink.
		/// The cursor start scan line is set by the lower 5 bits. 
		/// </summary>
		protected const int R10_CURSOR_START = 10;
		/// <summary>
		/// This 5 bit write-only register sets the cursor end scan line
		/// </summary>
		protected const int R11_CURSOR_END = 11;
		/// <summary>
		/// Start Address Register is a 14 bit write-only register which determines the first address put out as a refresh address after vertical blanking.
		/// It consists of an 8 bit lower register, and a 6 bit higher register.
		/// </summary>
		protected const int R12_START_ADDR_H = 12;
		protected const int R13_START_ADDR_L = 13;
		/// <summary>
		/// This 14 bit read/write register stores the cursor location.This register consists of an 8 bit lower and 6 bit higher register.
		/// </summary>
		protected const int R14_CURSOR_H = 14;
		protected const int R15_CURSOR_L = 15;
		/// <summary>
		/// This 14 bit read -only register is used to store the contents of the Address Register(H &amp; L) when the LPSTB input pulses high.
		/// This register consists of an 8 bit lower and 6 bit higher register.
		/// </summary>
		protected const int R16_LIGHT_PEN_H = 16;
		protected const int R17_LIGHT_PEN_L = 17;

		/// <summary>
		/// Storage for main MPU registers 
		/// 
		/// RegIdx    Register Name                 Type
		///                                         0             1             2             3                      4
		/// 0         Horizontal Total              Write Only    Write Only    Write Only    (note 2)               (note 3)
		/// 1         Horizontal Displayed          Write Only    Write Only    Write Only    (note 2)               (note 3)
		/// 2         Horizontal Sync Position      Write Only    Write Only    Write Only    (note 2)               (note 3)
		/// 3         H and V Sync Widths           Write Only    Write Only    Write Only    (note 2)               (note 3)
		/// 4         Vertical Total                Write Only    Write Only    Write Only    (note 2)               (note 3)
		/// 5         Vertical Total Adjust         Write Only    Write Only    Write Only    (note 2)               (note 3)
		/// 6         Vertical Displayed            Write Only    Write Only    Write Only    (note 2)               (note 3)
		/// 7         Vertical Sync position        Write Only    Write Only    Write Only    (note 2)               (note 3)
		/// 8         Interlace and Skew            Write Only    Write Only    Write Only    (note 2)               (note 3)
		/// 9         Maximum Raster Address        Write Only    Write Only    Write Only    (note 2)               (note 3)
		/// 10        Cursor Start Raster           Write Only    Write Only    Write Only    (note 2)               (note 3)
		/// 11        Cursor End Raster             Write Only    Write Only    Write Only    (note 2)               (note 3)
		/// 12        Disp. Start Address (High)    Read/Write    Write Only    Write Only    Read/Write (note 2)    (note 3)
		/// 13        Disp. Start Address (Low)     Read/Write    Write Only    Write Only    Read/Write (note 2)    (note 3)
		/// 14        Cursor Address (High)         Read/Write    Read/Write    Read/Write    Read/Write (note 2)    (note 3)
		/// 15        Cursor Address (Low)          Read/Write    Read/Write    Read/Write    Read/Write (note 2)    (note 3)
		/// 16        Light Pen Address (High)      Read Only     Read Only     Read Only     Read Only (note 2)     (note 3)
		/// 
		/// 18-31	  Not Used
		/// 
		/// 1. On type 0 and 1, if a Write Only register is read from, "0" is returned.
		/// 2. See the document "Extra CPC Plus Hardware Information" for more details.
		/// 3. CRTC type 4 is the same as CRTC type 3. The registers also repeat as they do on the type 3.
		/// </summary>
		protected byte[] Register = new byte[32];

		/// <summary>
		/// Internal Status Register specific to the Type 1 UM6845R
		/// </summary>
		protected byte StatusRegister;

		/// <summary>
		/// R0: CRTC-type horizontal total independent helper function
		/// </summary>
		protected virtual int R0_HorizontalTotal
		{
			get
			{
				int ht = Register[R0_H_TOTAL];
				return ht;
			}
		}

		/// <summary>
		/// R1: CRTC-type horizontal displayed independent helper function
		/// </summary>
		protected virtual int R1_HorizontalDisplayed
		{
			get
			{
				int hd = Register[R1_H_DISPLAYED];
				return hd;
			}
		}

		/// <summary>
		/// R2: CRTC-type horizontal sync position independent helper function
		/// </summary>
		protected virtual int R2_HorizontalSyncPosition
		{
			get
			{
				int hsp = Register[R2_H_SYNC_POS];
				return hsp;
			}
		}

		/// <summary>
		/// R3l: CRTC-type horizontal sync width independent helper function 
		/// </summary>
		protected virtual int R3_HorizontalSyncWidth { get; }

		/// <summary>
		/// R3h: CRTC-type vertical sync width independent helper function 
		/// </summary>
		protected virtual int R3_VerticalSyncWidth { get; }

		/// <summary>
		/// R4: CRTC-type vertical total independent helper function
		/// </summary>
		protected virtual int R4_VerticalTotal
		{
			get
			{
				int vt = Register[R4_V_TOTAL];
				return vt;
			}
		}

		/// <summary>
		/// R5: CRTC-type vertical total adjust independent helper function
		/// </summary>
		protected virtual int R5_VerticalTotalAdjust
		{
			get
			{
				int vta = Register[R5_V_TOTAL_ADJUST];
				return vta;
			}
		}

		/// <summary>
		/// R6: CRTC-type vertical displayed independent helper function
		/// </summary>
		protected virtual int R6_VerticalDisplayed
		{
			get
			{
				int vd = Register[R6_V_DISPLAYED];
				return vd;
			}
		}

		/// <summary>
		/// R7: CRTC-type vertical sync position independent helper function
		/// </summary>
		protected virtual int R7_VerticalSyncPosition
		{
			get
			{
				int vsp = Register[R7_V_SYNC_POS];
				return vsp;
			}
		}

		/// <summary>
		/// R8: CRTC-type CUDISP Active Display Skew helper function
		/// </summary>
		protected virtual int R8_Skew_CUDISP { get; }

		/// <summary>
		/// R8: CRTC-type DISPTMG Active Display Skew helper function
		/// </summary>
		protected virtual int R8_Skew_DISPTMG { get; }

		/// <summary>
		/// R8: CRTC-type Interlace Mode helper function
		/// </summary>
		protected virtual int R8_Interlace
		{
			get
			{
				// 0 = Non-interlace
				// 1 = Interlace SYNC Raster Scan
				// 2 = Interlace SYNC and Video Raster Scan
				return Register[R8_INTERLACE_MODE] & 0x03;
			}
		}

		/// <summary>
		/// R9: Max Scanlines
		/// </summary>
		protected virtual int R9_MaxScanline
		{
			get
			{
				int max = Register[R9_MAX_SL_ADDRESS];
				return max;
			}
		}

		/// <summary>
		/// C0: Horizontal Character Counter
		/// 8-bit
		/// </summary>		
		protected virtual int HCC
		{
			get => _hcCTR & 0xFF;
			set => _hcCTR = value & 0xFF;
		}
		private int _hcCTR;

		/// <summary>
		/// C3l: Horizontal Sync Width Counter (HSYNC)
		/// 4-bit
		/// </summary>		
		protected virtual int HSC
		{
			get => _hswCTR & 0x0F;
			set => _hswCTR = value & 0x0F;
		}
		private int _hswCTR;

		/// <summary>
		/// C4: Vertical Character Row Counter
		/// 7-bit
		/// </summary>
		protected virtual int VCC
		{
			get => _rowCTR & 0x7F;
			set => _rowCTR = value & 0x7F;
		}
		private int _rowCTR;

		/// <summary>
		/// C3h: Vertical Sync Width Counter (VSYNC)
		/// 4-bit
		/// </summary>
		protected virtual int VSC
		{
			get => _vswCTR & 0x0F;
			set => _vswCTR = value & 0x0F;
		}
		private int _vswCTR;

		/// <summary>
		/// C9: Vertical Line Counter (Scanline Counter)
		/// 5-bit
		/// If not in IVM mode, this counter is exposed on CRTC pins RA0..RA4
		/// </summary>
		protected virtual int VLC
		{
			get => _lineCTR & 0x1F;
			set => _lineCTR = value & 0x1F;
		}
		private int _lineCTR;

		/// <summary>
		/// C5: Vertical Total Adjust Counter
		/// 5-bit??
		/// This counter does not exist on CRTCs 0/3/4. C9 (VLC) is reused instead
		/// </summary>
		protected virtual int VTAC
		{
			get
			{
				switch (CrtcType)
				{
					case 0:
					case 3:
					case 4:
						return VLC;
					default:
						return _vtacCTR & 0x1F;
				}
			}
			set
			{
				switch (CrtcType)
				{
					case 0:
					case 3:
					case 4:
						//VLC = value;
						break;
					default:
						_vtacCTR = value & 0x1F;
						break;
				}
			}
		}
		private int _vtacCTR;

		/// <summary>
		/// Field Counter
		/// 6-bit
		/// Used for cursor flash - counts the number of completed fields
		/// </summary>
		protected virtual int CFC
		{
			get => _fieldCTR & 0x1F;
			set => _fieldCTR = value & 0x1F;
		}
		private int _fieldCTR;


		/// <summary>
		/// Constructor
		/// </summary>
		public CRTC()
		{
			//Reset();
		}

		// persistent control signals
		protected bool latch_hsync;
		protected bool latch_vadjust;
		protected bool latch_skew;

		private bool hend;
		private bool hsend;

		protected bool adjusting;

		private bool hclock;


		protected bool latch_hdisp;
		protected bool latch_vdisp;
		protected bool latch_idisp;
		protected bool hssstart;
		protected bool hhclock;
		protected bool _field;
		protected int _vma;
		protected int _vmaRowStart;

		/// <summary>
		/// CRTC is clocked at 1MHz (16 GA cycles)
		/// </summary>
		public virtual void Clock() { }

		
		public virtual void CheckReset()
		{
			if (_inReset > 0)
			{
				// reset takes a whole CRTC clock cycle
				_inReset--;

				HCC = 0;
				HSC = 0;
				VCC = 0;
				VSC = 0;
				VLC = 0;
				VTAC = 0;
				CFC = 0;
				_field = false;
				_vmaRowStart = 0;
				_vma = 0;
				ma = 0;
				ma_row_start = 0;
				ma_store = 0;
				_RA = 0;
				latch_hdisp = false;
				latch_vdisp = false;
				latch_idisp = false;

				/*
				// set regs to default
				for (int i = 0; i < 18; i++)
					Register[i] = RegDefaults[i];
				*/

				// regs aren't touched


				return;
			}
			else
				_inReset = -1;
		}

		/// <summary>
		/// Selects a specific register
		/// </summary>
		protected void SelectRegister(int value)
		{
			byte v = (byte)(value & 0x1F);
			AddressRegister = v;
		}

		/// <summary>
		/// Attempts to read from the currently selected register
		/// </summary>
		protected virtual bool ReadRegister(ref int data) => false;

		/// <summary>
		/// Attempts to write to the currently selected register
		/// </summary>
		protected virtual void WriteRegister(int data) {}

		/// <summary>
		/// Attempts to read from the internal status register (if present)
		/// </summary>
		protected virtual bool ReadStatus(ref int data) => false;

		/// <summary>
		/// Device responds to an IN instruction
		/// </summary>
		public virtual bool ReadPort(ushort port, ref int result)
		{
			byte portUpper = (byte)(port >> 8);

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
		public virtual bool WritePort(ushort port, int result)
		{
			byte portUpper = (byte)(port >> 8);

			bool accessed = false;

			// The 6845 is selected when bit 14 of the I/O port address is set to "0"
			if (portUpper.Bit(6))
				return accessed;

			int func = portUpper & 3;

			switch (func)
			{
				// reg select
				case 0:
					SelectRegister(result);
					break;

				// data write
				case 1:
					WriteRegister(result);
					break;
			}

			return accessed;
		}

		/// <summary>
		/// Simulates the RESET pin
		/// This should take at least one cycle
		/// </summary>
		public void Reset() => _inReset = 1;

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("CRTC");
			ser.Sync(nameof(CLK), ref CLK);
			ser.Sync(nameof(_VSYNC), ref _VSYNC);
			ser.Sync(nameof(_HSYNC), ref _HSYNC);
			ser.Sync(nameof(_DISPTMG), ref _DISPTMG);
			ser.Sync(nameof(_CUDISP), ref _CUDISP);
			ser.Sync(nameof(ma), ref ma);
			ser.Sync(nameof(ma_row_start), ref ma_row_start);
			ser.Sync(nameof(ma_store), ref ma_store);
			ser.Sync(nameof(_RA), ref _RA);
			ser.Sync(nameof(_addressRegister), ref _addressRegister);
			ser.Sync(nameof(Register), ref Register, false);
			ser.Sync(nameof(StatusRegister), ref StatusRegister);
			ser.Sync(nameof(_hcCTR), ref _hcCTR);
			ser.Sync(nameof(_hswCTR), ref _hswCTR);
			ser.Sync(nameof(_vswCTR), ref _vswCTR);
			ser.Sync(nameof(_rowCTR), ref _rowCTR);
			ser.Sync(nameof(_lineCTR), ref _lineCTR);
			ser.Sync(nameof(_vtacCTR), ref _vtacCTR);
			ser.Sync(nameof(_fieldCTR), ref _fieldCTR);
			ser.Sync(nameof(latch_hdisp), ref latch_hdisp);
			ser.Sync(nameof(latch_vdisp), ref latch_vdisp);
			ser.Sync(nameof(latch_hsync), ref latch_hsync);
			ser.Sync(nameof(latch_vadjust), ref latch_vadjust);
			ser.Sync(nameof(latch_skew), ref latch_skew);
			ser.Sync(nameof(_field), ref _field);
			ser.Sync(nameof(adjusting), ref adjusting);
			ser.Sync(nameof(_inReset), ref _inReset);	
			ser.Sync(nameof(hend), ref hend);
			ser.Sync(nameof(hsend), ref hsend);
			ser.Sync(nameof(hclock), ref hclock);
			ser.Sync(nameof(latch_idisp), ref latch_idisp);
			ser.Sync(nameof(hssstart), ref hssstart);
			ser.Sync(nameof(hhclock), ref hhclock);
			ser.Sync(nameof(_vma), ref _vma);
			ser.Sync(nameof(_vmaRowStart), ref _vmaRowStart);
			ser.EndSection();
		}
	}
}
