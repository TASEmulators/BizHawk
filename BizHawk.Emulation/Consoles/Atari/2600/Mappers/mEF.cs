using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Atari._2600
{
	/*
	EF (no name?)
	-----

	This is a fairly simple method that allows for up to 64K of ROM, using 16 4K banks.
	It works similar to F8, F6, etc.  Only the addresses to perform the switch is
	1FE0-1FEF.  Accessing one of these will select the desired bank. 1FE0 = bank 0,
	1FE1 = bank 1, etc.
	*/

	class mEF : MapperBase 
	{

	}
}
