using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	/// <summary>
	/// CATHODE RAY TUBE CONTROLLER (CRTC) IMPLEMENTATION
	/// TYPE 3
	/// - Amstrad AMS40489
	/// </summary>
	public class CRTC_Type3 : CRTC
	{
		/// <summary>
		/// Defined CRTC type number
		/// </summary>
		public override int CrtcType => 3;

		/// <summary>
		/// CRTC is clocked at 1MHz (16 GA cycles)
		/// </summary>
		public override void Clock()
		{
			base.Clock();

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

			_LA = _vma;

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
