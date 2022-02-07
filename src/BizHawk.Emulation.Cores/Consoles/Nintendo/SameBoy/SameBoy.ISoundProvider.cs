using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Sameboy
{
	public partial class Sameboy : ISoundProvider
	{
		public bool CanProvideAsync => false;

		public void DiscardSamples()
		{
			_soundoutbuffcontains = 0;
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = _soundoutbuff;
			nsamp = _soundoutbuffcontains;
			DiscardSamples();
		}

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported.");
			}
		}

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void GetSamplesAsync(short[] samples)
		{
			throw new InvalidOperationException("Async mode is not supported.");
		}

		private int _soundoutbuffcontains = 0;

		private readonly short[] _soundoutbuff = new short[2048];

		private unsafe void QueueSample(IntPtr core, IntPtr sample)
		{
			short* s = (short*)sample;
			_soundoutbuff[_soundoutbuffcontains * 2] = s[0];
			_soundoutbuff[_soundoutbuffcontains * 2 + 1] = s[1];
			_soundoutbuffcontains++;
		}
	}
}
