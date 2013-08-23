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

		int latchL = 0;
		int latchR = 0;
		int accumL = 0;
		int accumR = 0;

		static int DepthFromFilterwidth(int filterwidth)
		{
			int ret = -2;
			while (filterwidth > 0)
			{
				filterwidth >>= 1;
				ret++;
			}
			return ret;
		}

		int depth;
		
		public static DCFilter AsISoundProvider(ISoundProvider input, int filterwidth)
		{
			if (input == null)
				throw new ArgumentNullException();
			return new DCFilter(input, null, filterwidth);
		}

		public static DCFilter AsISyncSoundProvider(ISyncSoundProvider syncinput, int filterwidth)
		{
			if (syncinput == null)
				throw new ArgumentNullException();
			return new DCFilter(null, syncinput, filterwidth);
		}

		public static DCFilter DetatchedMode(int filterwidth)
		{
			return new DCFilter(null, null, filterwidth);
		}

		DCFilter(ISoundProvider input, ISyncSoundProvider syncinput, int filterwidth)	
		{
			if (filterwidth < 8 || filterwidth > 65536)
				throw new ArgumentOutOfRangeException();
			this.input = input;
			this.syncinput = syncinput;
			depth = DepthFromFilterwidth(filterwidth);
		}
		
		/// <summary>
		/// pass a set of samples through the filter.  should only be used in detached mode
		/// </summary>
		/// <param name="samples">sample buffer to modify</param>
		/// <param name="length">number of samples (not pairs).  stereo</param>
		public void PushThroughSamples(short[] samples, int length)
		{
			PushThroughSamples(samples, samples, length);
		}

		void PushThroughSamples(short[] samplesin, short[] samplesout, int length)
		{
			for (int i = 0; i < length; i += 2)
			{
				int L = samplesin[i] << 12;
				int R = samplesin[i + 1] << 12;
				accumL -= accumL >> depth;
				accumR -= accumR >> depth;
				accumL += L - latchL;
				accumR += R - latchR;
				latchL = L;
				latchR = R;

				int bigL = accumL >> 12;
				int bigR = accumR >> 12;
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

		void ISoundProvider.GetSamples(short[] samples)
		{
			input.GetSamples(samples);
			PushThroughSamples(samples, samples.Length);
		}

		void ISoundProvider.DiscardSamples()
		{
			input.DiscardSamples();
		}

		int ISoundProvider.MaxVolume
		{
			get { return input.MaxVolume; }
			set { input.MaxVolume = value; }
		}

		void ISyncSoundProvider.GetSamples(out short[] samples, out int nsamp)
		{
			short[] sampin;
			int nsampin;
			syncinput.GetSamples(out sampin, out nsampin);
			short[] ret = new short[nsampin * 2];
			PushThroughSamples(sampin, ret, nsampin * 2);
			samples = ret;
			nsamp = nsampin;
		}

		void ISyncSoundProvider.DiscardSamples()
		{
			syncinput.DiscardSamples();
		}
	}
}
