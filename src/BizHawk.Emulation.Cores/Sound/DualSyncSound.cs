using BizHawk.Emulation.Common;
using System;

namespace BizHawk.Emulation.Cores.Sound
{
	/// <summary>
	/// this thing more or less ASSumes that the two cores input to it both provide the same number of samples in each go
	/// </summary>
	public class DualSyncSound : ISyncSoundProvider
	{
		private readonly ISyncSoundProvider _left;
		private readonly ISyncSoundProvider _right;
		private int _nsamp;
		private short[] _samp = new short[0];

		private readonly short[] _leftOverflow = new short[32];
		private int _leftOverflowCount = 0;
		private readonly short[] _rightOverflow = new short[32];
		private int _rightOverflowCount = 0;

		public DualSyncSound(ISyncSoundProvider left, ISyncSoundProvider right)
		{
			_left = left;
			_right = right;
		}

		private static short Mix(short[] buff, int idx)
		{
			int s = buff[idx * 2] + buff[idx * 2 + 1];
			if (s > 32767)
				s = 32767;
			if (s < -32768)
				s = -32768;
			return (short)s;
		}

		public void Fetch()
		{
			_left.GetSamplesSync(out var sampl, out int nsampl);
			_right.GetSamplesSync(out var sampr, out var nsampr);

			int n = Math.Min(nsampl + _leftOverflowCount, nsampr + _rightOverflowCount);
			
			if (_samp.Length < n * 2)
				_samp = new short[n * 2];

			int i, j;
			for (i = 0, j = 0; i < _leftOverflowCount; i++, j++)
				_samp[j * 2] = Mix(_leftOverflow, i);
			for (i = 0; j < n; i++, j++)
				_samp[j * 2] = Mix(sampl, i);
			_leftOverflowCount = Math.Min(nsampl - i, 16);
			Array.Copy(sampl, i * 2, _leftOverflow, 0, _leftOverflowCount * 2);
			for (i = 0, j = 0; i < _rightOverflowCount; i++, j++)
				_samp[j * 2 + 1] = Mix(_rightOverflow, i);
			for (i = 0; j < n; i++, j++)
				_samp[j * 2 + 1] = Mix(sampr, i);
			_rightOverflowCount = Math.Min(nsampr - i, 16);
			Array.Copy(sampr, i * 2, _rightOverflow, 0, _rightOverflowCount * 2);

			_nsamp = n;
		}

		public void DiscardSamples()
		{
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = _samp;
			nsamp = _nsamp;
		}
	}
}
