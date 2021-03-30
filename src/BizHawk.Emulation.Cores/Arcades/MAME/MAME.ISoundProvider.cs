using System;
using System.Collections.Generic;
using System.Linq;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	public partial class MAME : ISoundProvider
	{
		public bool CanProvideAsync => false;
		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		private readonly Queue<short> _audioSamples = new Queue<short>();
		private decimal _dAudioSamples = 0;
		private readonly int _sampleRate = 44100;
		private int _numSamples = 0;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported.");
			}
		}

		/*
		 * GetSamplesSync() and MAME
		 * 
		 * MAME generates samples 50 times per second, regardless of the VBlank
		 * rate of the emulated machine. It then uses complicated logic to
		 * output the required amount of audio to the OS driver and to the AVI,
		 * where it's meant to tie flashed samples to video frame duration.
		 * 
		 * I'm doing my own logic here for now. I grab MAME's audio buffer
		 * whenever it's filled (MAMESoundCallback()) and enqueue it.
		 * 
		 * Whenever Hawk wants new audio, I dequeue it, but with a little quirk.
		 * Since sample count per frame may not align with frame duration, I
		 * subtract the entire decimal fraction of "required" samples from total
		 * samples. I check if the fractional reminder of total samples is > 0.5
		 * by rounding it. I invert it to see what number I should add to the
		 * integer representation of "required" samples, to compensate for
		 * misalignment between fractional and integral "required" samples.
		 * 
		 * TODO: Figure out how MAME does this and maybe use their method instead.
		 */
		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			decimal dSamplesPerFrame = (decimal)_sampleRate * VsyncDenominator / VsyncNumerator;

			if (_audioSamples.Any())
			{
				_dAudioSamples -= dSamplesPerFrame;
				int remainder = (int)Math.Round(_dAudioSamples - Math.Truncate(_dAudioSamples)) ^ 1;
				nsamp = (int)Math.Round(dSamplesPerFrame) + remainder;
			}
			else
			{
				nsamp = (int)Math.Round(dSamplesPerFrame);
			}

			samples = new short[nsamp * 2];

			for (int i = 0; i < nsamp * 2; i++)
			{
				if (_audioSamples.Any())
				{
					samples[i] = _audioSamples.Dequeue();
				}
				else
				{
					samples[i] = 0;
				}
			}
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new InvalidOperationException("Async mode is not supported.");
		}

		public void DiscardSamples()
		{
			_audioSamples.Clear();
		}
	}
}