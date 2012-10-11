using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Sound.Utilities
{
	/// <summary>
	/// implements a DC block filter on top of an ISoundProvider.  rather simple.
	/// </summary>
	public class DCFilter : ISoundProvider, ISyncSoundProvider
	{
		/*
		 * A note about accuracy:
		 * 
		 * DCFilter can be added to the final output of any console, and this change will be faithful to the original hardware.
		 * Analog output hardware ALWAYS has dc blocking caps.
		 */

		ISoundProvider input;
		ISyncSoundProvider syncinput;

		int sumL = 0;
		int sumR = 0;

		Queue<short> buffer;

		int depth;
		
		/// <summary>
		/// if input == null, run in detatched push mode
		/// </summary>
		/// <param name="input"></param>
		public DCFilter(ISoundProvider input = null, int filterwidth = 65536)
		{
			if (filterwidth < 1 || filterwidth > 65536)
				throw new ArgumentOutOfRangeException();
			this.input = input;
			this.syncinput = null;
			this.depth = filterwidth;
			this.buffer = new Queue<short>(depth * 2);
			for (int i = 0; i < depth * 2; i++)
				buffer.Enqueue(0);			
		}

		public DCFilter(ISyncSoundProvider input, int filterwidth)
		{
			if (filterwidth < 1 || filterwidth > 65536)
				throw new ArgumentOutOfRangeException();
			this.input = null;
			this.syncinput = input;
			this.depth = filterwidth;
			this.buffer = new Queue<short>(depth * 2);
			for (int i = 0; i < depth * 2; i++)
				buffer.Enqueue(0);
		}
		
		/// <summary>
		/// pass a set of samples through the filter.  should not be mixed with pull (ISoundProvider) mode or sync mode
		/// </summary>
		public void PushThroughSamples(short[] samples, int length)
		{
			PushThroughSamples(samples, samples, length);
		}

		void PushThroughSamples(short[] samplesin, short[] samplesout, int length)
		{
			for (int i = 0; i < length; i += 2)
			{
				sumL -= buffer.Dequeue();
				sumR -= buffer.Dequeue();
				short L = samplesin[i];
				short R = samplesin[i + 1];
				sumL += L;
				sumR += R;
				buffer.Enqueue(L);
				buffer.Enqueue(R);
				int bigL = L - sumL / depth;
				int bigR = R - sumR / depth;
				// check for clipping
				if (bigL > 32767)
					samplesout[i] = 32767;
				else if (bigL < -32768)
					samplesout[i] = -32768;
				else
					samplesout[i] = (short)bigL;
				if (bigR > 32767)
					samplesout[i + 1] = 32767;
				else if (bigR < -32768)
					samplesout[i + 1] = -32768;
				else
					samplesout[i + 1] = (short)bigR;
			}

		}

		public void GetSamples(short[] samples)
		{
			input.GetSamples(samples);
			PushThroughSamples(samples, samples.Length);
		}

		public void DiscardSamples()
		{
			if (input != null)
				input.DiscardSamples();
			if (syncinput != null)
				syncinput.DiscardSamples();
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

		public void GetSamples(out short[] samples, out int nsamp)
		{
			short[] sampin;
			int nsampin;
			syncinput.GetSamples(out sampin, out nsampin);
			short[] ret = new short[nsampin * 2];
			PushThroughSamples(sampin, ret, nsampin * 2);
			samples = ret;
			nsamp = nsampin;
		}
	}
}
