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
		private readonly int _sampleRate = 44100;
		private long _soundRemainder = 0;

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
		 * Whenever Hawk wants new audio, I dequeue it, while preserving the
		 * fractinal part of the sample count, to use it later.
		 */
		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			long nSampNumerator = _sampleRate * (long)VsyncDenominator + _soundRemainder;
			nsamp = (int)(nSampNumerator / VsyncNumerator);			
			_soundRemainder = nSampNumerator % VsyncNumerator; // exactly remember fractional parts of an audio sample
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
			_soundRemainder = 0;
			_audioSamples.Clear();
		}
	}
}