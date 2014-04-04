using System;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/**
	Cartridge class used for Homestar Runner by Paul Slocum.
	There are 16 4K banks (total of 64K ROM) with 128 bytes of RAM.
	Accessing $1FE0 - $1FEF switches to each bank.
	*/
	internal class mEFSC : MapperBase
	{
		public mEFSC()
		{
			throw new NotImplementedException();
		}
	}
}
