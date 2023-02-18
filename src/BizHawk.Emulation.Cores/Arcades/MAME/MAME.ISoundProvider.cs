using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	public partial class MAME : ISyncSoundProvider
	{
		private const int _sampleRate = 44100;
		private readonly short[] _sampleBuffer = new short[_sampleRate * 2]; // MAME internally guarentees refresh rate is never < 1Hz
		private int _nsamps = 0;

		private void UpdateSound()
		{
			_nsamps = _core.mame_sound_get_samples(_sampleBuffer);
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = _sampleBuffer;
			nsamp = _nsamps;
		}

		public void DiscardSamples()
		{
			_nsamps = 0;
		}
	}
}