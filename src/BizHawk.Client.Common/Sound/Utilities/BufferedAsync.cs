using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	// Generates SEMI-synchronous sound, or "buffered asynchronous" sound.

	// This class will try as hard as it can to request the correct number of samples on each frame and then
	// send them out to the sound card as it needs them.

	// However, it has minimum/maximum buffer targets and will request smaller or larger frames if it has to.
	// The ultimate goal of this strategy is to make MOST frames 100% correct, and if errors must occur,
	// concentrate it on a single frame, rather than distribute small errors across most frames, as
	// distributing error to most frames tends to result in persistently distorted audio, especially when
	// sample playback is involved.


	/*
	 * Why use this, when each core has it's own Async sound output?
	 *
	 * It is true that each emulation core provides its own async sound output, either through directly
	 * rendering to arbitrary buffers (pce not TurboCD, sms), or using one of the metaspus (nes, TurboCD).
	 *
	 * Unfortunately, the vecna metaspu is not perfect, and for maintaining near-realtime playback (the usual
	 * situation which we want to optimize for), it simply sounds better with a BufferedAsync on top of it.
	 *
	 * TODO: BufferedAsync has some hard coded parameters that assume FPS = 60.  make that more generalized.
	 * TODO: For systems that _really_ don't need BufferedAsync (pce not turbocd, sms), make a way to signal
	 *       that and then bypass the BufferedAsync.
	 */
	public sealed class BufferedAsync : ISoundProvider, IBufferedSoundProvider
	{
		public ISoundProvider BaseSoundProvider { get; set; }

		private readonly Queue<short> buffer = new Queue<short>(MaxExcessSamples);

		private int SamplesInOneFrame = 1470;
		private readonly int TargetExtraSamples = 882;
		private const int MaxExcessSamples = 4096;

		/// <summary>
		/// recalculates some internal parameters based on the IEmulator's framerate
		/// </summary>
		public void RecalculateMagic(double framerate)
		{
			// ceiling instead of floor here is very important (magic)
			SamplesInOneFrame = 2 * (int)Math.Ceiling((44100.0 / framerate));
			//TargetExtraSamples = ;// complete guess
		}

		public void DiscardSamples()
		{
			buffer.Clear();
			BaseSoundProvider?.DiscardSamples();
		}

		/// <exception cref="InvalidOperationException"><see cref="BaseSoundProvider"/>.<see cref="ISoundProvider.SyncMode"/> is not <see cref="SyncSoundMode.Async"/></exception>
		public void GetSamplesAsync(short[] samples)
		{
			int samplesToGenerate = SamplesInOneFrame;
			if (buffer.Count > samples.Length + MaxExcessSamples)
				samplesToGenerate = 0;
			if (buffer.Count - samples.Length < TargetExtraSamples)
				samplesToGenerate += SamplesInOneFrame;
			if (samplesToGenerate + buffer.Count < samples.Length)
				samplesToGenerate = samples.Length - buffer.Count;

			var mySamples = new short[samplesToGenerate];

			if (BaseSoundProvider.SyncMode != SyncSoundMode.Async)
			{
				throw new InvalidOperationException("Base sound provider must be in async mode.");
			}
			BaseSoundProvider.GetSamplesAsync(mySamples);

			foreach (short s in mySamples)
			{
				buffer.Enqueue(s);
			}

			for (int i = 0; i < samples.Length; i++)
			{
				samples[i] = buffer.Dequeue();
			}
		}

		public bool CanProvideAsync => true;

		public SyncSoundMode SyncMode => SyncSoundMode.Async;

		/// <exception cref="NotSupportedException"><paramref name="mode"/> is not <see cref="SyncSoundMode.Async"/></exception>
		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Async)
			{
				throw new NotSupportedException("Sync mode is not supported.");
			}
		}

		/// <exception cref="InvalidOperationException">always</exception>
		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			throw new InvalidOperationException("Sync mode is not supported.");
		}
	}
}