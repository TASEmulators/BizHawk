using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class Sid : ISoundProvider
	{
		private short[] sampleBuffer;
		private int sampleBufferCapacity;
		private int sampleBufferIndex;
		private int sampleBufferReadIndex;
		private int sampleCounter;

		public void GetSamples(short[] samples)
		{
			int count = samples.Length;
			int copied = 0;

			while ((sampleBufferIndex != sampleBufferReadIndex) && (copied < count))
			{
				samples[copied] = sampleBuffer[sampleBufferReadIndex++];
				copied++;
				if (sampleBufferReadIndex == sampleBufferCapacity)
					sampleBufferReadIndex = 0;
			}

			// catch the buffer up
			sampleBufferReadIndex = sampleBufferIndex;
		}

		private void InitSound(int initSampleRate)
		{
			sampleBufferCapacity = initSampleRate;
			DiscardSamples();
		}

		public void DiscardSamples()
		{
			sampleBuffer = new short[sampleBufferCapacity];
			ResetBuffer();
		}

		public int MaxVolume
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}

		private void ResetBuffer()
		{
			sampleBufferReadIndex = 0;
			sampleBufferIndex = 0;
		}

		private void SubmitSample()
		{
			if (sampleCounter == 0)
			{
				short output;
				output = Mix(regs.OSC[0], 0);
				output = Mix(regs.OSC[1], output);
				
				// voice 3 can be disabled with a specific register, but
				// when the filter is enabled, it still plays
				if (!regs.D3 || regs.FILT[2])
					output = Mix(regs.OSC[2], output);

				// run twice since the buffer expects stereo sound (I THINK)
				for (int i = 0; i < 2; i++)
				{
					sampleCounter = cyclesPerSample;
					sampleBuffer[sampleBufferIndex] = output;
					sampleBufferIndex++;
					if (sampleBufferIndex == sampleBufferCapacity)
						sampleBufferIndex = 0;
				}
			}
			sampleCounter--;
		}
	}
}
