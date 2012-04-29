using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Atari._2600
{
	/*
	3F (Tigervision)
	-----

	Traditionally, this method was used on the Tigervision games.  The ROMs were all 8K, and 
	there's two 2K pages in the 4K of address space.  The upper bank is fixed to the last 2K
	of the ROM.

	The first 2K is selectable by writing to any location between 0000 and 003F.  Yes, this
	overlaps the TIA, but this is not a big deal.  You simply use the mirrors of the TIA at
	40-7F instead!  To select a bank, the games write to 3Fh, because it's not implemented
	on the TIA.

	The homebrew community has decided that if 8K is good, more ROM is better!  This mapper
	can support up to 512K bytes of ROM just by implementing all 8 bits on the mapper
	register, and this has been done... however I do not think 512K ROMs have been made just
	yet.
	*/

	class m3F :MapperBase 
	{

	}
}
