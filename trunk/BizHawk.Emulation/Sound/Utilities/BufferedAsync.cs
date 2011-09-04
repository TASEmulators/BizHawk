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

    public sealed class BufferedAsync : ISoundProvider
    {
        public ISoundProvider BaseSoundProvider;

        private Queue<short> buffer = new Queue<short>(4096);

        private const int SamplesInOneFrame = 1470;
        private const int TargetExtraSamples = 882;
        private const int MaxExcessSamples = 4096;

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