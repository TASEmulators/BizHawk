using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Sound
{
	/// <summary>
	/// this thing more or less ASSumes that the two cores input to it both provide the same number of samples in each go
	/// </summary>
	public class DualSyncSound : ISoundProvider
	{
		private ISoundProvider _left;
		private ISoundProvider _right;
		private int _nsamp;
		private short[] _samp = new short[0];

		private short[] _leftOverflow = new short[32];
		private int _leftOverflowCount = 0;
		private short[] _rightOverflow = new short[32];
		private int _rightOverflowCount = 0;


		public DualSyncSound(ISoundProvider left, ISoundProvider right)
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
			int nsampl, nsampr;
			short[] sampl, sampr;
			_left.GetSamplesSync(out sampl, out nsampl);
			_right.GetSamplesSync(out sampr, out nsampr);

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

		public bool CanProvideAsync => false;

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void DiscardSamples()
		{
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new InvalidOperationException();
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = _samp;
			nsamp = _nsamp;
		}

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Sync)
				throw new InvalidOperationException();
		}
	}
}
