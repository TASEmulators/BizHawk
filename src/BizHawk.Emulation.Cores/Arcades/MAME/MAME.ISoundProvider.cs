using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	public partial class MAME : ISoundProvider
	{
		public bool CanProvideAsync => false;
		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		private const int _sampleRate = 44100;
		private readonly short[] _sampleBuffer = new short[_sampleRate * 2]; // MAME internally guarentees refresh rate is never < 1Hz
		private int _nsamps = 0;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported.");
			}
		}

		private void UpdateSound()
		{
			_nsamps = _core.mame_sound_get_samples(_sampleBuffer);
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = _sampleBuffer;
			nsamp = _nsamps;
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new InvalidOperationException("Async mode is not supported.");
		}

		public void DiscardSamples()
		{
			_nsamps = 0;
		}
	}
}