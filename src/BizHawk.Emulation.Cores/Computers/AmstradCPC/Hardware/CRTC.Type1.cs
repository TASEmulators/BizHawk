using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	/// <summary>
	/// CATHODE RAY TUBE CONTROLLER (CRTC) IMPLEMENTATION
	/// TYPE 1
	/// - UMC UM6845R		http://www.cpcwiki.eu/imgs/b/b5/Um6845r.umc.pdf
	/// </summary>
	public class CRTC_Type1 : CRTC
	{
		/// <summary>
		/// Defined CRTC type number
		/// </summary>
		public override int CrtcType => 1;

		private int cnt;

		/// <summary>
		/// CRTC is clocked at 1MHz (16 GA cycles)
		/// </summary>
		public override void Clock()
		{
			CheckReset();
			cnt++;

			// linear address generator clocked
			ma = (ma + 1) & 0x3FFF;

			// horizontal character counter clocked
			HCC++;

			if (HCC == R0_HorizontalTotal + 1)          // C0 == R0
			{
				// new scanline
				// increment raster counter
				VLC++;

				if (VLC == R9_MaxScanline + 1)              // C9 == R9
				{
					// new character row
					// vertical character counter reset
					VLC = 0;

					// incremement vertical character counter
					VCC++;

					if (VCC == R6_VerticalDisplayed)        // C4 == R6
					{
						// vertical display is disabled
						latch_vdisp = false;
					}

					if (VCC == R4_VerticalTotal + 1)        // C4 == R4
					{
						// new CRTC frame
						// vertical character counter reset
						VCC = 0;

						// vertical display enabled
						latch_vdisp = true;

						// cached VMA loaded with register start address
						ma_store = (Register[R12_START_ADDR_H] << 8) | Register[R13_START_ADDR_L];
					}

					if (VCC == R7_VerticalSyncPosition      // C4 == R7
						&& !VSYNC)							// ACCC 16.3: It is not possible to trigger or inhibit VSYNC during a VSYNC     
					{
						// VSYNC enabled
						VSYNC = true;

						// reset vertical sync counter
						VSC = 0;
					}

					ma_row_start = ma_store;
				}

				if (VSYNC)
				{
					// increment vertical sync counter
					VSC++;

					if (VSC == R3_VerticalSyncWidth         // C3h == R3h
						|| VSC == 0)                        // VSC counter wrap-around
					{
						// VSYNC disabled
						VSYNC = false;
						cnt = 0;
					}
				}

				// H display is enabled
				latch_hdisp = true;

				// horizontal character counter reset
				HCC = 0;

				// VMA updated with row start address
				ma = ma_row_start;
			}

			if (HCC == R1_HorizontalDisplayed)          // CO == R1
			{
				// H display is disabled
				latch_hdisp = false;

				// current VMA is copied into memory
				ma_store = ma;
			}

			if (HCC == R2_HorizontalSyncPosition        // C0 == R2
				&& R3_HorizontalSyncWidth > 0)			// ACCC 14.6: When R3 == 0, CRTC 0 and 1 do not produce HSYNC     
			{
				// HSYNC is enabled
				HSYNC = true;

				// reset horizontal sync counter
				HSC = 0;
			}

			if (HSYNC)
			{
				// HSYNC still enabled - increment counter
				HSC++;

				if (HSC == R3_HorizontalSyncWidth       // C3l == R3l
					|| HSC == 0)                        // HSC counter wrap-around                        
				{
					// disable HSYNC
					HSYNC = false;
				}
			}

			// outputs
			if (!latch_hdisp || !latch_vdisp)
			{
				// HSYNC output pin is fed through a NOR gate with either 2 or 3 inputs
				// - H Display
				// - V Display
				DISPTMG = false;
			}
			else
			{
				DISPTMG = true;
			}
		}

		public void Clock2()
		{
			base.Clock();

			ma = (ma + 1) & 0x3FFF;
			HCC++;

			// C0 == R0
			if (HCC == R0_HorizontalTotal + 1)
			{
				// new scanline
				HCC = 0;

				// C9 == R9
				if (++VLC == R9_MaxScanline + 1 || latch_vadjust)
				{
					VLC = 0;

					// C4 == R4
					if (++VCC == R4_VerticalTotal + 1)
					{
						latch_vadjust = true;

						_vmaRowStart = (Register[R12_START_ADDR_H] << 8) | Register[R13_START_ADDR_L];

						// C5 == R5
						if (VTAC == R5_VerticalTotalAdjust)
						{
							latch_vadjust = false;
							VCC = 0;
							VTAC = 0;
						}
						else
						{
							VTAC++;
						}
					}
					else
					{
						// MA set to row start at the beginning of each line
						_vmaRowStart += R1_HorizontalDisplayed;
					}

					// C4 == 0
					if (VCC == 0)
					{
						latch_vdisp = true;
					}

					if (VCC == R6_VerticalDisplayed)
					{
						latch_vdisp = false;
					}
				}

				if (!latch_vadjust)
				{
					// C4 == R7
					if (VCC == R7_VerticalSyncPosition && VLC == 0)
					{
						VSYNC = true;
						VSC = 0;
					}

					if (VSYNC)
					{
						VSC++;

						// C3h == R3h
						if (VSC == R3_VerticalSyncWidth || VSC == 0)
						{
							VSYNC = false;
						}
					}
				}


			}

			if (HCC == 0)
			{
				ma = _vmaRowStart;
			}

			// C0 == R2
			if (HCC == R2_HorizontalSyncPosition)
			{
				HSYNC = true;
				HSC = 0;
			}
			if (HSYNC)
			{
				if (R3_HorizontalSyncWidth > 0)
				{
					HSC++;
				}

				// C3l == R3l
				if (HSC == R3_HorizontalSyncWidth)
				{
					HSYNC = false;
				}
			}

			int dSkew = R8_Skew_CUDISP > 2 ? -1 : R8_Skew_CUDISP;

			// C0 == 0
			if (dSkew >= 0 && HCC == dSkew)
			{
				latch_hdisp = true;
			}

			// C0 == R1
			if (dSkew >= 0 && HCC == R1_HorizontalDisplayed + dSkew)
			{
				latch_hdisp = false;
			}




			// outputs
			if (!latch_hdisp || !latch_vdisp)
			{
				// HSYNC output pin is fed through a NOR gate with either 2 or 3 inputs
				// - H Display
				// - V Display
				DISPTMG = false;
			}
			else
			{
				DISPTMG = true;
			}
		}

		
		public void Clocko()
		{
			CheckReset();

			int maxScanLine;

			if (HCC == R0_HorizontalTotal)
			{
				// end of displayable area reached
				// set up for the next line
				HCC = 0;

				// TODO: handle interlace setup
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

			/* Hor active video */
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

			/* Hor sync */
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

			/* Ver active video */
			if (VCC == 0)
			{
				// active display
				latch_vdisp = true;
			}

			if (VCC == R6_VerticalDisplayed)
			{
				// inactive display
				latch_vdisp = false;

				// ACCC1.8 - 21.3.3
				// On CRTC 1, bit 5 of the Status register is updated when C0=R0 according to the BORDER R6
				// conditions (False: C4=C9=C0=0 / True: C4=R6 & C9=C0=0)
				StatusRegister |= (1 << 5);
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


			/* Address Generation */
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


		/// <summary>
		/// R3l: CRTC-type horizontal sync width independent helper function 
		/// </summary>
		protected override int R3_HorizontalSyncWidth
		{
			get
			{
				// Bits 3..0 define Horizontal Sync Width
				// on CRTC1, a zero value means no HSYNC is generated
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
				// Bits 7..4 are ignored
				// on CRTC1 VSYNC is fixed at 16 lines
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
				// CRTC1
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
				// CRTC1
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
					// write-only registers return 0x0 on Type 1 CRTC
					data = 0;
					break;
				case R14_CURSOR_H:
					data = Register[AddressRegister] & 0x3F;
					break;
				case R15_CURSOR_L:
					data = Register[AddressRegister];
					break;
				case R16_LIGHT_PEN_H:
					// read/write registers (6bit)
					data = Register[AddressRegister] & 0x3F;
					// reading from R16 resets bit6 of the status register
					StatusRegister &= byte.MaxValue ^ (1 << 6);
					break;
				case R17_LIGHT_PEN_L:
					// read/write regiters (8bit)
					data = Register[AddressRegister];
					// reading from R17 resets bit6 of the status register
					StatusRegister &= byte.MaxValue ^ (1 << 6);
					break;
				case 31:
					// Dummy Register. Datasheet describes this as N/A but CPCWIKI suggests that reading from it return 0xFF;
					data = 0xFF;
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
		/// CRTC 1 has a status register
		/// </summary>
		protected override bool ReadStatus(ref int data)
		{
			// ACCC1.8 - 21.3.1
			// Only CRTC 1 has a status register present on the specific port &BE00.
			// This port is a mirror of the read port for CRTC’s 3 and 4, which handle status differently			
			data = StatusRegister;

			return true;
		}
	}
}
