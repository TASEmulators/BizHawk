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

	}
}
