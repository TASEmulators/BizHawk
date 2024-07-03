#nullable disable

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// implements a DC block filter on top of an ISoundProvider.  rather simple.
	/// </summary>
	public sealed class DCFilter : ISoundProvider
	{
		private readonly ISoundProvider _soundProvider;
		private readonly int _depth;

		private int _latchL;
		private int _latchR;
		private int _accumL;
		private int _accumR;

		private static int DepthFromFilterWidth(int filterWidth)
		{
			int ret = -2;
			while (filterWidth > 0)
			{
				filterWidth >>= 1;
				ret++;
			}

			return ret;
		}

		/// <exception cref="ArgumentNullException"><paramref name="input"/> is null</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="filterWidth"/> is not in 8..65536</exception>
		public DCFilter(ISoundProvider input, int filterWidth)
		{
			if (input is null) throw new ArgumentNullException(paramName: nameof(input));
			if (filterWidth is < 8 or > 65536) throw new ArgumentOutOfRangeException(paramName: nameof(filterWidth), filterWidth, message: "invalid width");

			_depth = DepthFromFilterWidth(filterWidth);

			_soundProvider = input;
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

		private void PushThroughSamples(short[] samplesIn, short[] samplesOut, int length)
		{
			for (int i = 0; i < length; i += 2)
			{
				int l = samplesIn[i] << 12;
				int r = samplesIn[i + 1] << 12;
				_accumL -= _accumL >> _depth;
				_accumR -= _accumR >> _depth;
				_accumL += l - _latchL;
				_accumR += r - _latchR;
				_latchL = l;
				_latchR = r;

				int bigL = _accumL >> 12;
				int bigR = _accumR >> 12;

				// check for clipping
				if (bigL > 32767)
				{
					samplesOut[i] = 32767;
				}
				else if (bigL < -32768)
				{
					samplesOut[i] = -32768;
				}
				else
				{
					samplesOut[i] = (short)bigL;
				}

				if (bigR > 32767)
				{
					samplesOut[i + 1] = 32767;
				}
				else if (bigR < -32768)
				{
					samplesOut[i + 1] = -32768;
				}
				else
				{
					samplesOut[i + 1] = (short)bigR;
				}
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
			_soundProvider.GetSamplesSync(out var sampIn, out var nsampIn);

			short[] ret = new short[nsampIn * 2];
			PushThroughSamples(sampIn, ret, nsampIn * 2);
			samples = ret;
			nsamp = nsampIn;
		}

		public SyncSoundMode SyncMode => _soundProvider.SyncMode;

		public bool CanProvideAsync => _soundProvider.CanProvideAsync;

		public void SetSyncMode(SyncSoundMode mode)
		{
			_soundProvider.SetSyncMode(mode);
		}
	}
}
