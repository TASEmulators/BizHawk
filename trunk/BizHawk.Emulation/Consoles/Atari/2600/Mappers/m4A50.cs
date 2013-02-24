using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Atari._2600
{
	/*
	4A50 (no name)
	-----

	Upon review, I don't think this method is terribly workable on real
	hardware.  There's so many problems that I kinda gave up trying to
	count them all.  Seems that this is more of a "pony" method than something
	actually usable. ("pony" referring to "I want this, and that, and that, and
	a pony too!")

	One major problem is that it specifies that memory can be read and written
	to at the same address, but this is nearly impossible to detect on a 2600
	cartridge.  You'd almost have to try and figure out what opcodes are being
	run, and what cycle it's on somehow, all just by watching address and
	data bus state.  Not very practical.

	The other problem is just the sheer volume of things it is supposed to do.
	There's just tons and tons of unnecessary things like attempting to detect
	BIT instructions, handling page wraps and other silly things.

	This all supposidly fit into a Xilinx XC9536XL but I am not sure how the
	chip could handle the RAM issue above at all.  It almost needs to see R/W
	and M2 (clock) to be able to properly do most of the things it's doing.
	*/

	class m4A50 : MapperBase 
	{
		private int myLastData = 0xFF;
		private int myLastAddress = 0xFFFF;

		private bool myIsRomHigh = true;
		private bool myIsRomLow = true;
		private bool myIsRomMiddle = true;

		private int mySliceHigh = 0;
		private int mySliceLow = 0;
		private int mySliceMiddle = 0;

		private ByteBuffer myRAM = new ByteBuffer(32768);

		public override byte ReadMemory(ushort addr)
		{
			byte val = 0;
			if (addr < 0x1000)
			{
				val = base.ReadMemory(addr);
				checkBankSwitch(addr, val);
				
			}
			else
			{
				if ((addr & 0x1800) == 0x1000)           // 2K region from 0x1000 - 0x17ff
				{
					val = myIsRomLow ? core.rom[(addr & 0x7ff) + mySliceLow]
									   : myRAM[(addr & 0x7ff) + mySliceLow];
				}
				else if (((addr & 0x1fff) >= 0x1800) &&  // 1.5K region from 0x1800 - 0x1dff
						((addr & 0x1fff) <= 0x1dff))
				{
					val = myIsRomMiddle ? core.rom[(addr & 0x7ff) + mySliceMiddle]
										  : myRAM[(addr & 0x7ff) + mySliceMiddle];
				}
				else if ((addr & 0x1f00) == 0x1e00)      // 256B region from 0x1e00 - 0x1eff
				{
					val = myIsRomHigh ? core.rom[(addr & 0xff) + mySliceHigh]
										: myRAM[(addr & 0xff) + mySliceHigh];
				}
				else if ((addr & 0x1f00) == 0x1f00)      // 256B region from 0x1f00 - 0x1fff
				{
					val = core.rom[(addr & 0xff) + (core.rom.Length - 256)];
					if (((myLastData & 0xe0) == 0x60) &&
					   ((myLastAddress >= 0x1000) || (myLastAddress < 0x200)))
						mySliceHigh = (mySliceHigh & 0xf0ff) | ((addr & 0x8) << 8) |
										((addr & 0x70) << 4);
				}
			}

			myLastData = val;
			myLastAddress = addr & 0x1fff;
			return val;
		}

		public override void WriteMemory(ushort addr, byte value)
		{
			if (addr < 0x1000)	// Hotspots below 0x1000
			{
				base.WriteMemory(addr, value);
				checkBankSwitch(addr, value);
			}
			else
			{
				if (addr < 0x1800)           // 2K region at 0x1000 - 0x17ff
				{
					if (!myIsRomLow)
					{
						myRAM[(addr & 0x7ff) + mySliceLow] = value;
					}
				}
				else if (((addr & 0x1fff) >= 0x1800) &&  // 1.5K region at 0x1800 - 0x1dff
						((addr & 0x1fff) <= 0x1dff))
				{
					if (!myIsRomMiddle)
					{
						myRAM[(addr & 0x7ff) + mySliceMiddle] = value;
					}
				}
				else if ((addr & 0x1f00) == 0x1e00)      // 256B region at 0x1e00 - 0x1eff
				{
					if (!myIsRomHigh)
					{
						myRAM[(addr & 0xff) + mySliceHigh] = value;
					}
				}
				else if ((addr & 0x1f00) == 0x1f00)      // 256B region at 0x1f00 - 0x1fff
				{
					if (((myLastData & 0xe0) == 0x60) &&
					   ((myLastAddress >= 0x1000) || (myLastAddress < 0x200)))
					{
						mySliceHigh = (mySliceHigh & 0xf0ff) | ((addr & 0x8) << 8) |
									  ((addr & 0x70) << 4);
					}
				}
			}
			myLastData = value;
			myLastAddress = addr & 0x1fff;
		}

		void checkBankSwitch(ushort address, byte value)
		{
			if (((myLastData & 0xe0) == 0x60) &&      // Switch lower/middle/upper bank
				((myLastAddress >= 0x1000) || (myLastAddress < 0x200)))
			{
				if ((address & 0x0f00) == 0x0c00)       // Enable 256B of ROM at 0x1e00 - 0x1eff
				{
					myIsRomHigh = true;
					mySliceHigh = (address & 0xff) << 8;
				}
				else if ((address & 0x0f00) == 0x0d00)  // Enable 256B of RAM at 0x1e00 - 0x1eff
				{
					myIsRomHigh = false;
					mySliceHigh = (address & 0x7f) << 8;
				}
				else if ((address & 0x0f40) == 0x0e00)  // Enable 2K of ROM at 0x1000 - 0x17ff
				{
					myIsRomLow = true;
					mySliceLow = (address & 0x1f) << 11;
				}
				else if ((address & 0x0f40) == 0x0e40)  // Enable 2K of RAM at 0x1000 - 0x17ff
				{
					myIsRomLow = false;
					mySliceLow = (address & 0xf) << 11;
				}
				else if ((address & 0x0f40) == 0x0f00)  // Enable 1.5K of ROM at 0x1800 - 0x1dff
				{
					myIsRomMiddle = true;
					mySliceMiddle = (address & 0x1f) << 11;
				}
				else if ((address & 0x0f50) == 0x0f40)  // Enable 1.5K of RAM at 0x1800 - 0x1dff
				{
					myIsRomMiddle = false;
					mySliceMiddle = (address & 0xf) << 11;
				}
				else if ((address & 0x0f00) == 0x0400)   // Toggle bit A11 of lower block address
				{
					mySliceLow = mySliceLow ^ 0x800;
				}
				else if ((address & 0x0f00) == 0x0500)   // Toggle bit A12 of lower block address
				{
					mySliceLow = mySliceLow ^ 0x1000;
				}
				else if ((address & 0x0f00) == 0x0800)   // Toggle bit A11 of middle block address
				{
					mySliceMiddle = mySliceMiddle ^ 0x800;
				}
				else if ((address & 0x0f00) == 0x0900)   // Toggle bit A12 of middle block address
				{
					mySliceMiddle = mySliceMiddle ^ 0x1000;
				}

				// Zero-page hotspots for upper page
				//   0xf4, 0xf6, 0xfc, 0xfe for ROM
				//   0xf5, 0xf7, 0xfd, 0xff for RAM
				//   0x74 - 0x7f (0x80 bytes lower)
				if ((address & 0xf75) == 0x74)         // Enable 256B of ROM at 0x1e00 - 0x1eff
				{
					myIsRomHigh = true;
					mySliceHigh = value << 8;
				}
				else if ((address & 0xf75) == 0x75)    // Enable 256B of RAM at 0x1e00 - 0x1eff
				{
					myIsRomHigh = false;
					mySliceHigh = (value & 0x7f) << 8;
				}

				// Zero-page hotspots for lower and middle blocks
				//   0xf8, 0xf9, 0xfa, 0xfb
				//   0x78, 0x79, 0x7a, 0x7b (0x80 bytes lower)
				else if ((address & 0xf7c) == 0x78)
				{
					if ((value & 0xf0) == 0)           // Enable 2K of ROM at 0x1000 - 0x17ff
					{
						myIsRomLow = true;
						mySliceLow = (value & 0xf) << 11;
					}
					else if ((value & 0xf0) == 0x40)   // Enable 2K of RAM at 0x1000 - 0x17ff
					{
						myIsRomLow = false;
						mySliceLow = (value & 0xf) << 11;
					}
					else if ((value & 0xf0) == 0x90)   // Enable 1.5K of ROM at 0x1800 - 0x1dff
					{
						myIsRomMiddle = true;
						mySliceMiddle = ((value & 0xf) | 0x10) << 11;
					}
					else if ((value & 0xf0) == 0xc0)   // Enable 1.5K of RAM at 0x1800 - 0x1dff
					{
						myIsRomMiddle = false;
						mySliceMiddle = (value & 0xf) << 11;
					}
				}
			}
		}
	}
}
