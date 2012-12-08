using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	public abstract partial class Sid : IDisposable
	{
		public Sound.Utilities.SpeexResampler resampler;
		/*
		public void GetSamples(out short[] samples, out int nsamp)
		{
			if (bufferIndex > bufferReadOffset)
				samples = new short[bufferIndex - bufferReadOffset];
			else
				samples = new short[bufferIndex + (bufferLength - bufferReadOffset)];
			
			nsamp = samples.Length;
			for (uint i = 0; i < nsamp; i++)
			{
				samples[i] = buffer[bufferReadOffset];
				if (bufferReadOffset != bufferIndex)
					bufferReadOffset++;
				if (bufferReadOffset == bufferLength)
					bufferReadOffset = 0;
			}
			nsamp /= 2;
		}

		public void DiscardSamples()
		{
			bufferIndex = 0;
			bufferReadOffset = 0;
		}
		*/
		public void Dispose()
		{
			if (resampler != null)
			{
				resampler.Dispose();
				resampler = null;
			}
		}
	}
}
