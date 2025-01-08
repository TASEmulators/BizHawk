
namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	/// <summary>
	/// CATHODE RAY TUBE CONTROLLER (CRTC) IMPLEMENTATION
	/// TYPE 2
	/// - Motorola MC6845
	/// http://www.cpcwiki.eu/imgs/d/da/Mc6845.motorola.pdf
	/// http://bitsavers.trailing-edge.com/components/motorola/_dataSheets/6845.pdf
	/// </summary>
	public class CRTC_Type2 : CRTC
	{
		/// <summary>
		/// Defined CRTC type number
		/// </summary>
		public override int CrtcType => 2;

		public override void Clock() => throw new InvalidOperationException("CRTC Type 2 not implemented yet");

		/// <summary>
		/// R3l: CRTC-type horizontal sync width independent helper function 
		/// </summary>
		protected override int R3_HorizontalSyncWidth
		{
			get
			{
				// Bits 3..0 define Horizontal Sync Width
				// on CRTC2, a zero value means 16 characters of HSYNC are generated
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
				// Bits 7..4 are ignored
				// on CRTC2 VSYNC is fixed at 16 lines
				return 16;
			}
		}

		/// <summary>
		/// R8: CRTC-type CUDISP Active Display Skew helper function
		/// </summary>
		protected override int R8_Skew_CUDISP
		{
			get
			{
				// CRTC2
				// Bits 7..6 are ignored
				return 0;
			}
		}

		/// <summary>
		/// R8: CRTC-type CUDISP Active Display Skew helper function
		/// </summary>
		protected override int R8_Skew_DISPTMG
		{
			get
			{
				// CRTC2
				// Bits 5..4 are ignored
				return 0;
			}
		}


		/// <summary>
		/// Attempts to read from the currently selected register
		/// </summary>
		protected override bool ReadRegister(ref int data)
		{
			switch (AddressRegister)
			{
				case R0_H_TOTAL:
				case R1_H_DISPLAYED:
				case R2_H_SYNC_POS:
				case R3_SYNC_WIDTHS:
				case R4_V_TOTAL:
				case R5_V_TOTAL_ADJUST:
				case R6_V_DISPLAYED:
				case R7_V_SYNC_POS:
				case R8_INTERLACE_MODE:
				case R9_MAX_SL_ADDRESS:
				case R10_CURSOR_START:
				case R11_CURSOR_END:
				case R12_START_ADDR_H:
				case R13_START_ADDR_L:
					// write-only registers do not respond on type 2
					return false;
				case R14_CURSOR_H:
				case R16_LIGHT_PEN_H:
					// read/write registers (6bit)
					data = Register[AddressRegister] & 0x3F;
					break;
				case R17_LIGHT_PEN_L:
				case R15_CURSOR_L:
					// read/write regiters (8bit)
					data = Register[AddressRegister];
					break;
				default:
					// non-existent registers return 0x0
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
			byte v = (byte)data;

			switch (AddressRegister)
			{
				case R0_H_TOTAL:
				case R1_H_DISPLAYED:
				case R2_H_SYNC_POS:
				case R13_START_ADDR_L:
				case R15_CURSOR_L:
					// 8-bit registers
					Register[AddressRegister] = v;
					break;
				case R4_V_TOTAL:
				case R6_V_DISPLAYED:
				case R7_V_SYNC_POS:
				case R10_CURSOR_START:
					// 7-bit registers
					Register[AddressRegister] = (byte)(v & 0x7F);
					break;
				case R12_START_ADDR_H:
				case R14_CURSOR_H:
					// 6-bit registers
					Register[AddressRegister] = (byte)(v & 0x3F);
					break;
				case R5_V_TOTAL_ADJUST:
				case R9_MAX_SL_ADDRESS:
				case R11_CURSOR_END:
					// 5-bit registers
					Register[AddressRegister] = (byte)(v & 0x1F);
					break;
				case R3_SYNC_WIDTHS:
					// 4-bit register
					Register[AddressRegister] = (byte)(v & 0x0F);
					break;
				case R8_INTERLACE_MODE:
					// Interlace & skew - 2bit
					Register[AddressRegister] = (byte)(v & 0x03);
					break;
			}
		}

		/// <summary>
		/// CRTC 2 has no status register
		/// </summary>
		protected override bool ReadStatus(ref int data)
		{
			// ACCC1.8 - 21.3.2
			// CRTC2 always returns 255 on this port
			data = 255;
			return true;
		}
	}
}
