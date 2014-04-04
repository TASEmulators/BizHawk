using System;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/**
	Cartridge class used for DPC+.  There are six 4K program banks, a 4K
	display bank, 1K frequency table and the DPC chip.  For complete details on
	the DPC chip see David P. Crane's United States Patent Number 4,644,495.
	*/
	internal class mDPCPlus : MapperBase
	{
		public mDPCPlus()
		{
			throw new NotImplementedException();
		}
	}
}
