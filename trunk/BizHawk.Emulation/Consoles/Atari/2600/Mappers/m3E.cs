using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Atari._2600
{
	/*
	3E (Boulderdash
	-----

	This works similar to 3F (Tigervision) above, except RAM has been added.  The range of
	addresses has been restricted, too.  Only 3E and 3F can be written to now.

	1000-17FF - this bank is selectable
	1800-1FFF - this bank is the last 2K of the ROM

	To select a particular 2K ROM bank, its number is poked into address 3F.  Because there's
	8 bits, there's enough for 256 2K banks, or a maximum of 512K of ROM.

	Writing to 3E, however, is what's new.  Writing here selects a 1K RAM bank into 
	1000-17FF.  The example (Boulderdash) uses 16K of RAM, however there's theoretically
	enough space for 256K of RAM.  When RAM is selected, 1000-13FF is the read port while
	1400-17FF is the write port.
	*/
	class m3E : MapperBase 
	{

	}
}
