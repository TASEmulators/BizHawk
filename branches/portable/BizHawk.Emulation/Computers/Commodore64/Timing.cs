using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public class Timing
	{
		public int crystalFreq;
		public uint timer;

		public Timing(Region timingRegion)
		{
			switch (timingRegion)
			{
				case Region.NTSC:
					crystalFreq = 14318181;
					break;
				case Region.PAL:
					crystalFreq = 17734472;
					break;
			}
		}

		public void Advance()
		{
			// need an unchecked block here since the timer will wrap
			unchecked
			{
				timer++;
			}
		}

		public bool IsCycle(int divisor)
		{
			return (timer % divisor) == 0;
		}
	}
}
