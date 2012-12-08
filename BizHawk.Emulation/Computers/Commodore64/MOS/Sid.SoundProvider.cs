using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	/*
	 * this wasn't sounding as good as a vecna metaspu
	 */
	public abstract partial class Sid //: ISoundProvider
	{
		/*
		private short[] buffer;
		private uint bufferCounter;
		private uint bufferFrequency;
		private uint bufferIndex;
		private uint bufferLength;
		private uint bufferReadOffset;
		*/
		private uint cyclesPerSec;
		/*
		public void GetSamples(short[] samples)
		{
			bool overrun = false;
			uint count = (uint)samples.Length;
			uint overrunOffset;

			if (bufferIndex == 0)
				overrunOffset = bufferLength - 1;
			else
				overrunOffset = bufferIndex - 1;
				
			uint i = 0;
			while (i < count)
			{
				if (bufferReadOffset == bufferIndex)
					overrun = true;
				if (!overrun)
				{
					samples[i++] = buffer[bufferReadOffset++];
					samples[i++] = buffer[bufferReadOffset++];
				}
				else
				{
					samples[i++] = buffer[overrunOffset];
					samples[i++] = buffer[overrunOffset];
				}
				if (bufferReadOffset == bufferLength)
					bufferReadOffset = 0;
			}
		}

		public int MaxVolume
		{
			get
			{
				return 15;
			}
			set
			{
				// no change in volume
			}
		}
		*/
	}
}
