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

		public void DiscardSamples()
		{
			sampleBuffer = new short[sampleBufferCapacity];
			ResetBuffer();
		}

		public short[] GetAllSamples()
		{
			List<short> samples = new List<short>();
			while (sampleBufferReadIndex != sampleBufferIndex)
			{
				samples.Add(sampleBuffer[sampleBufferReadIndex]);
				sampleBufferReadIndex++;
				if (sampleBufferReadIndex == sampleBufferCapacity)
					sampleBufferReadIndex = 0;
			}
			return samples.ToArray();
		}

		public void GetSamples(short[] samples)
		{
			int count = samples.Length;
			int copied = 0;

			while (copied < count)
			{
				samples[copied] = sampleBuffer[sampleBufferReadIndex];
				if (sampleBufferIndex != sampleBufferReadIndex)
					sampleBufferReadIndex++;
				copied++;
				if (sampleBufferReadIndex == sampleBufferCapacity)
					sampleBufferReadIndex = 0;
			}

			// catch buffer up
			sampleBufferReadIndex = sampleBufferIndex;
		}

		private void InitSound(int initSampleRate)
		{
			sampleBufferCapacity = initSampleRate;
			DiscardSamples();
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
				output = Mix(voices[0].OSC, voices[0].ENV, 0);
				output = Mix(voices[1].OSC, voices[1].ENV, output);
				
				// voice 3 can be disabled with a specific register, but
				// when the filter is enabled, it still plays
				if (!regs.D3 || voices[2].FILT)
					output = Mix(voices[2].OSC, voices[2].ENV, output);

				// run twice since the buffer expects stereo sound (I THINK)
				for (int i = 0; i < 2; i++)
				{
					sampleCounter = cyclesPerSample;
					sampleBufferIndex++;
					if (sampleBufferIndex == sampleBufferCapacity)
						sampleBufferIndex = 0;
					sampleBuffer[sampleBufferIndex] = output;
				}
			}
			sampleCounter--;
		}
	}

	public class SidSyncSoundProvider : ISyncSoundProvider
	{
		private Sid sid;

		public SidSyncSoundProvider(Sid source)
		{
			sid = source;
		}

		public void DiscardSamples()
		{
			sid.DiscardSamples();
		}

		public void GetSamples(out short[] samples, out int nsamp)
		{
			samples = sid.GetAllSamples();
			nsamp = samples.Length / 2;
		}
	}
}
