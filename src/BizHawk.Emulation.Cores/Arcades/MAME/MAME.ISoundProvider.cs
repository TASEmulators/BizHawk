using System;
using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	public partial class MAME : ISoundProvider
	{
		public bool CanProvideAsync => false;
		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		private readonly Queue<short> _audioSamples = new();
		private const int _sampleRate = 44100;
		private int _samplesPerFrame;
		private short[] _sampleBuffer;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported.");
			}
		}

		private void InitSound()
		{
			_samplesPerFrame = (int)Math.Ceiling(((long)_sampleRate * (long)VsyncDenominator / (double)VsyncNumerator));
			_sampleBuffer = new short[_samplesPerFrame * 2];
		}

		/*
		 * GetSamplesSync() and MAME
		 * 
		 * MAME generates samples 50 times per second, regardless of the VBlank
		 * rate of the emulated machine. It then uses complicated logic to
		 * output the required amount of audio to the OS driver and to the AVI,
		 * where it's meant to tie flushed samples to video frame duration.
		 * 
		 * I'm doing my own logic here for now. I grab MAME's audio buffer
		 * whenever it's filled (MAMESoundCallback()) and enqueue it.
		 * 
		 * Whenever Hawk wants new audio, I dequeue it, but never more than the
		 * maximum samples a frame contains, keeping pending samples for the next frame
		 */
		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = _sampleBuffer;
			nsamp = Math.Min(_samplesPerFrame, _audioSamples.Count / 2);

			for (int i = 0; i < nsamp * 2; i++)
			{
				samples[i] = _audioSamples.Dequeue();
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