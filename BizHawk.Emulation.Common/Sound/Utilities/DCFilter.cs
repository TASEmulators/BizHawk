using System;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// implements a DC block filter on top of an ISoundProvider.  rather simple.
	/// </summary>
	sealed public class DCFilter : ISoundProvider
	{
		private ISoundProvider _soundProvider;

		private int latchL = 0;
		private int latchR = 0;
		private int accumL = 0;
		private int accumR = 0;

		private int depth;

		private static int DepthFromFilterwidth(int filterwidth)
		{
			int ret = -2;
			while (filterwidth > 0)
			{
				filterwidth >>= 1;
				ret++;
			}
			return ret;
		}

		public DCFilter(ISoundProvider input, int filterwidth)
		{
			if (input == null)
			{
				throw new ArgumentNullException();
			}

			if (filterwidth < 8 || filterwidth > 65536)
			{
				throw new ArgumentOutOfRangeException();
			}

			depth = DepthFromFilterwidth(filterwidth);

			_soundProvider = input;
		}

		// Detached mode
		public DCFilter(int filterwidth)
		{
			if (filterwidth < 8 || filterwidth > 65536)
			{
				throw new ArgumentOutOfRangeException();
			}

			depth = DepthFromFilterwidth(filterwidth);

			_soundProvider = null;
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

		private void PushThroughSamples(short[] samplesin, short[] samplesout, int length)
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

		public void GetSamplesAsync(short[] samples)
		{
			_soundProvider.GetSamplesAsync(samples);
			PushThroughSamples(samples, samples.Length);
		}

		public void DiscardSamples()
		{
			_soundProvider.DiscardSamples();
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			short[] sampin;
			int nsampin;

			_soundProvider.GetSamplesSync(out sampin, out nsampin);

			short[] ret = new short[nsampin * 2];
			PushThroughSamples(sampin, ret, nsampin * 2);
			samples = ret;
			nsamp = nsampin;
		}

		public SyncSoundMode SyncMode
		{
			get { return _soundProvider.SyncMode; }
		}

		public bool CanProvideAsync
		{
			get { return _soundProvider.CanProvideAsync; }
		}

		public void SetSyncMode(SyncSoundMode mode)
		{
			_soundProvider.SetSyncMode(mode);
		}
	}
}
