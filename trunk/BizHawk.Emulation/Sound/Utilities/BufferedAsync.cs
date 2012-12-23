using System.Collections.Generic;

namespace BizHawk.Emulation.Sound
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
	 * rendering to arbitrary buffers (pce not turbocd, sms), or using one of the metaspus (nes, turbocd).
	 * 
	 * Unfortunately, the vecna metaspu is not perfect, and for maintaining near-realtime playback (the usual
	 * situation which we want to optimize for), it simply sounds better with a BufferedAsync on top of it.
	 * 
	 * TODO: BufferedAsync has some hard coded parameters that assume FPS = 60.  make that more generalized.
	 * TODO: For systems that _really_ don't need BufferedAsync (pce not turbocd, sms), make a way to signal
	 *       that and then bypass the BufferedAsync.
	 */

    public sealed class BufferedAsync : ISoundProvider
    {
        public ISoundProvider BaseSoundProvider;

        Queue<short> buffer = new Queue<short>(4096);

        int SamplesInOneFrame = 1470;
        int TargetExtraSamples = 882;
        const int MaxExcessSamples = 4096;

		/// <summary>
		/// recalculates some internal parameters based on the IEmulator's framerate
		/// </summary>
		public void RecalculateMagic(double framerate)
		{
			// ceiling instead of floor here is very important (magic)
			SamplesInOneFrame = (int)System.Math.Ceiling((88200.0 / framerate));
			//TargetExtraSamples = ;// complete guess
		}

		public void DiscardSamples()
		{
			if(BaseSoundProvider != null)
				BaseSoundProvider.DiscardSamples();
		}

        public int MaxVolume { get; set; }

        public void GetSamples(short[] samples)
        {
            int samplesToGenerate = SamplesInOneFrame;
            if (buffer.Count > samples.Length + MaxExcessSamples)
                samplesToGenerate = 0;
            if (buffer.Count - samples.Length < TargetExtraSamples)
                samplesToGenerate += SamplesInOneFrame;
            if (samplesToGenerate + buffer.Count < samples.Length)
                samplesToGenerate = samples.Length - buffer.Count;

            var mySamples = new short[samplesToGenerate];

            BaseSoundProvider.GetSamples(mySamples);

            for (int i = 0; i < mySamples.Length; i++)
                buffer.Enqueue(mySamples[i]);

            for (int i = 0; i < samples.Length; i++)
                samples[i] = buffer.Dequeue();
        }
    }
}