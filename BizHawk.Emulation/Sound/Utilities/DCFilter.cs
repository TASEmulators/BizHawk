using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Sound.Utilities
{
	/// <summary>
	/// implements a DC block filter on top of an ISoundProvider.  rather simple.
	/// </summary>
	public class DCFilter : ISoundProvider
	{
		/*
		 * A note about accuracy:
		 * 
		 * DCFilter can be added to the final output of any console, and this change will be faithful to the original hardware.
		 * Analog output hardware ALWAYS has dc blocking caps.
		 */

		ISoundProvider input;

		int sumL = 0;
		int sumR = 0;

		Queue<short> buffer;

		const int depth = 65536;
		
		/// <summary>
		/// if input == null, run in detatched push mode
		/// </summary>
		/// <param name="input"></param>
		public DCFilter(ISoundProvider input = null)
		{
			this.input = input;
			this.buffer = new Queue<short>(depth * 2);
			for (int i = 0; i < depth * 2; i++)
				buffer.Enqueue(0);
		}

		/// <summary>
		/// returns the original sound provider (in case you lost it).
		/// after calling, the DCFilter is no longer valid
		/// </summary>
		/// <returns></returns>
		public ISoundProvider Detatch()
		{
			var ret = input;
			input = null;
			return ret;
		}
		
		/// <summary>
		/// pass a set of samples through the filter.  should not be mixed with pull (ISoundProvider) mode
		/// </summary>
		public void PushThroughSamples(short[] samples, int length)
		{
			for (int i = 0; i < length; i += 2)
			{
				sumL -= buffer.Dequeue();
				sumR -= buffer.Dequeue();
				short L = samples[i];
				short R = samples[i + 1];
				sumL += L;
				sumR += R;
				buffer.Enqueue(L);
				buffer.Enqueue(R);
				int bigL = L - (sumL >> 16); // / depth;
				int bigR = R - (sumR >> 16); // / depth;
				// check for clipping
				if (bigL > 32767)
					samples[i] = 32767;
				else if (bigL < -32768)
					samples[i] = -32768;
				else
					samples[i] = (short)bigL;
				if (bigR > 32767)
					samples[i + 1] = 32767;
				else if (bigR < -32768)
					samples[i + 1] = -32768;
				else
					samples[i + 1] = (short)bigR;

			}
		}

		public void GetSamples(short[] samples)
		{
			input.GetSamples(samples);
			PushThroughSamples(samples, samples.Length);
		}

		public void DiscardSamples()
		{
			input.DiscardSamples();
		}

		public int MaxVolume
		{
			get
			{
				return input.MaxVolume;
			}
			set
			{
				input.MaxVolume = value;
			}
		}
	}
}
