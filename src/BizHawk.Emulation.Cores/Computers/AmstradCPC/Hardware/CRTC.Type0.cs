using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	/// <summary>
	/// CATHODE RAY TUBE CONTROLLER (CRTC) IMPLEMENTATION
	/// TYPE 0
	/// - Hitachi HD6845S	http://www.cpcwiki.eu/imgs/c/c0/Hd6845.hitachi.pdf
	/// - UMC UM6845		http://www.cpcwiki.eu/imgs/1/13/Um6845.umc.pdf
	/// </summary>
	public class CRTC_Type0 : CRTC
	{
		/// <summary>
		/// Defined CRTC type number
		/// </summary>
		public override int CrtcType => 0;

		public override void Clock() => throw new InvalidOperationException("CRTC Type 0 not implemented yet");
		

		/*
		public override void Clock()
		{
			CheckReset();

			int maxScanLine;

			if (HCC == R0_HorizontalTotal)
			{
				// end of displayable area reached
				// set up for the next line
				HCC = 0;

				if (R8_Interlace == 3)
				{
					// in interlace sync and video mask off bit 0 of the max scanline address
					maxScanLine = R9_MaxScanline & 0b11110;
				}
				else
				{
					maxScanLine = R9_MaxScanline;
				}

				if (VLC == maxScanLine)
				{
					// we have reached the final scanline within this vertical character row
					// move to next character
					VLC = 0;

					// TODO: implement vertical adjust


					if (VCC == R4_VerticalTotal)
					{
						// check the interlace mode
						if (R8_Interlace.Bit(0))
						{
							// toggle the field
							_field = !_field;
						}
						else
						{
							// stay on the even field
							_field = false;
						}

						// we have reached the end of the vertical display area
						// address loaded from start address register at the top of each field
						_vmaRowStart = (Register[R12_START_ADDR_H] << 8) | Register[R13_START_ADDR_L];

						// reset the vertical character counter
						VCC = 0;

						// increment field counter
						CFC++;
					}
					else
					{
						// row start address is increased by Horiztonal Displayed
						_vmaRowStart += R1_HorizontalDisplayed;

						// increment vertical character counter
						VCC++;
					}
				}
				else
				{
					// next scanline
					if (R8_Interlace == 3)
					{
						// interlace sync+video mode
						// vertical line counter increments by 2
						VLC += 2;

						// ensure vertical line counter is an even value
						VLC &= ~1;
					}
					else
					{
						// non-interlace mode
						// increment vertical line counter
						VLC++;
					}
				}

				// MA set to row start at the beginning of each line
				_vma = _vmaRowStart;
			}
			else
			{
				// next horizontal character (1us)
				// increment horizontal character counter
				HCC++;

				// increment VMA
				_vma++;
			}

			hssstart = false;
			hhclock = false;

			if (HCC == R2_HorizontalSyncPosition)
			{
				// start of horizontal sync
				hssstart = true;
			}

			if (HCC == R2_HorizontalSyncPosition / 2)
			{
				// we are half way through the line
				hhclock = true;
			}

			if (HCC == 0)
			{
				// active display
				latch_hdisp = true;
			}

			if (HCC == R1_HorizontalDisplayed)
			{
				// inactive display
				latch_hdisp = false;
			}

			if (hssstart ||     // start of horizontal sync
				HSYNC)          // already in horizontal sync
			{
				// start of horizontal sync
				HSYNC = true;
				HSC++;
			}
			else
			{
				// reset hsync counter
				HSC = 0;
			}

			if (HSC == R3_HorizontalSyncWidth)
			{
				// end of horizontal sync
				HSYNC = false;
			}

			if (VCC == 0)
			{
				// active display
				latch_vdisp = true;
			}

			if (VCC == R6_VerticalDisplayed)
			{
				// inactive display
				latch_vdisp = false;
			}

			// vertical sync occurs at different times depending on the interlace field
			// even field:	the same time as HSYNC
			// odd field:	half a line later than HSYNC
			if ((!_field && hssstart) || (_field && hhclock))
			{
				if ((VCC == R7_VerticalSyncPosition && VLC == 0)    // vsync starts on the first line
					|| VSYNC)                                       // vsync is already in progress
				{
					// start of vertical sync
					VSYNC = true;
					// increment vertical sync counter
					VSC++;
				}
				else
				{
					// reset vsync counter
					VSC = 0;
				}

				if (VSYNC && VSC == R3_VerticalSyncWidth - 1)
				{
					// end of vertical sync
					VSYNC = false;
				}
			}


			int line = VLC;

			if (R8_Interlace == 3)
			{
				// interlace sync+video mode
				// the least significant bit is based on the current field number
				int fNum = _field ? 1 : 0;
				int lNum = VLC.Bit(0) ? 1 : 0;
				line &= ~1;

				_RA = line & (fNum | lNum);
			}
			else
			{
				// raster address is just the VLC
				_RA = VLC;
			}

			ma = _vma;

			// DISPTMG Generation
			if (!latch_hdisp || !latch_vdisp)
			{
				// HSYNC output pin is fed through a NOR gate with either 2 or 3 inputs
				// - H Display
				// - V Display
				// - TODO: R8 DISPTMG Skew (only on certain CRTC types)
				DISPTMG = false;
			}
			else
			{
				DISPTMG = true;
			}
		}
		*/

		/// <summary>
		/// R3l: CRTC-type horizontal sync width independent helper function 
		/// </summary>
		protected override int R3_HorizontalSyncWidth
		{
			get
			{
				// Bits 3..0 define Horizontal Sync Width
				// on CRTC0, a zero value means no HSYNC is generated
				return Register[R3_SYNC_WIDTHS] & 0x0F;
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
				// on CRTC0 if 0 is programmed this gives 16 lines of VSYNC
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
				// CRTC0
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
				// CRTC0
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
			bool addressed;

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
					// write-only registers return 0x0 on Type 0 CRTC
					addressed = true;
					data = 0;
					break;
				case R12_START_ADDR_H:
				case R14_CURSOR_H:
				case R16_LIGHT_PEN_H:
					// read/write registers (6bit)
					addressed = true;
					data = Register[AddressRegister] & 0x3F;
					break;
				case R13_START_ADDR_L:
				case R15_CURSOR_L:
				case R17_LIGHT_PEN_L:
					// read/write regiters (8bit)
					addressed = true;
					data = Register[AddressRegister];
					break;
				default:
					// non-existent registers return 0x0
					addressed = true;
					data = 0;
					break;
			}

			return addressed;
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
				case R3_SYNC_WIDTHS:
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
				case R8_INTERLACE_MODE:
					// Interlace & skew masks bits 2 & 3
					Register[AddressRegister] = (byte)(v & 0xF3);
					break;
			}
		}

		/// <summary>
		/// CRTC 0 has no status register
		/// </summary>
		protected override bool ReadStatus(ref int data)
		{
			// ACCC1.8 - 21.3.2
			// CRTC0 randomly apparently returns 255 or 127 on this port
			
			// For the purposes of Bizhawk determinism, we will return one of the above values based on the current HCC
			data = HCC.Bit(0) ? 255 : 127;
			return true;
		}
	}
}
