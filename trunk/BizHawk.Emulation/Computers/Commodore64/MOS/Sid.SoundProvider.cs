using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	public abstract partial class Sid : ISoundProvider
	{
		private short[] buffer;
		private uint bufferCounter;
		private uint bufferFrequency;
		private uint bufferIndex;
		private uint bufferLength;
		private uint bufferReadOffset;
		private uint cyclesPerSec;

		public void GetSamples(short[] samples)
		{
			uint count = (uint)samples.Length;
			for (uint i = 0; i < count; i++)
			{
				samples[i] = buffer[bufferReadOffset];
				if (bufferReadOffset != bufferIndex)
					bufferReadOffset++;
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
	}
}
