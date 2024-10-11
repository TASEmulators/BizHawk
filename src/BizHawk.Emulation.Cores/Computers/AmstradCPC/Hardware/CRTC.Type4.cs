
namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	/// <summary>
	/// CATHODE RAY TUBE CONTROLLER (CRTC) IMPLEMENTATION
	/// TYPE 4
	/// - Amstrad AMS40041
	/// - Amstrad AMS40226
	/// </summary>
	public class CRTC_Type4 : CRTC
	{
		/// <summary>
		/// Defined CRTC type number
		/// </summary>
		public override int CrtcType => 4;

		public override void Clock() => throw new InvalidOperationException("CRTC Type 4 not implemented yet");

		/// <summary>
		/// R3l: CRTC-type horizontal sync width independent helper function 
		/// </summary>
		protected override int R3_HorizontalSyncWidth
		{
			get
			{
				// Bits 3..0 define Horizontal Sync Width
				// on CRTC4, a zero value means 16 characters of HSYNC are generated
				return (Register[R3_SYNC_WIDTHS] & 0x0F) == 0 ? 16 : Register[R3_SYNC_WIDTHS] & 0x0F;
			}
		}

		/// <summary>
		/// R3h: CRTC-type vertical sync width independent helper function 
		/// </summary>
		protected override int R3_VerticalSyncWidth
		{
			get
			{
				// Bits 7..4 define Vertical Sync Width
				// on CRTC4 if 0 is programmed this gives 16 lines of VSYNC
				return ((Register[R3_SYNC_WIDTHS] >> 4) & 0x0F) == 0 ? 16 : Register[R3_SYNC_WIDTHS] >> 4 & 0x0F;
			}
		}

		/// <summary>
		/// R8: CRTC-type CUDISP Active Display Skew helper function
		/// </summary>
		protected override int R8_Skew_CUDISP
		{
			get
			{
				// CRTC4
				// Bits 7..6 define the skew (delay) of the CUDISP signal
				// 00 = 0
				// 01 = 1
				// 10 = 2
				// 11 = non-output
				return (Register[R8_INTERLACE_MODE] >> 6) & 0x03;
			}
		}

		/// <summary>
		/// R8: CRTC-type CUDISP Active Display Skew helper function
		/// </summary>
		protected override int R8_Skew_DISPTMG
		{
			get
			{
				// CRTC4
				// Bits 5..4 define the skew (delay) of the DISPTMG signal
				// 00 = 0
				// 01 = 1
				// 10 = 2
				// 11 = non-output
				return ((Register[R8_INTERLACE_MODE] & 0b0011_0000) >> 4) & 0x03;
			}
		}

		/// <summary>
		/// Attempts to read from the currently selected register
		/// </summary>
		protected override bool ReadRegister(ref int data)
		{
			// http://cpctech.cpc-live.com/docs/cpcplus.html
			switch (AddressRegister & 0x6F)
			{
				case 0:
					data = Register[R16_LIGHT_PEN_H] & 0x3F;
					break;
				case 1:
					data = Register[R17_LIGHT_PEN_L];
					break;
				case 2:
					// Status 1
					break;
				case 3:
					// Status 2
					break;
				case 4:
					data = Register[R12_START_ADDR_H] & 0x3F;
					break;
				case 5:
					data = Register[R13_START_ADDR_L];
					break;
				case 6:
				case 7:
					data = 0;
					break;
			}

			return true;
		}

		/// <summary>
		/// Attempts to write to the currently selected register
		/// </summary>
		protected override void WriteRegister(int data)
		{
			byte v3 = (byte)data;
			switch (AddressRegister)
			{
				case 16:
				case 17:
					// read only registers
					return;
				default:
					if (AddressRegister < 16)
					{
						Register[AddressRegister] = v3;
					}
					else
					{
						// read only dummy registers
						return;
					}
					break;
			}
		}
	}
}
