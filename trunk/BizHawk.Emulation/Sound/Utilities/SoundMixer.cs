using System.Collections.Generic;

namespace BizHawk.Emulation.Sound
{
    // This is a straightforward class to mix/chain multiple ISoundProvider sources.
    // TODO: Fine-tuned volume control would be a good thing.

    public sealed class SoundMixer : ISoundProvider
    {
        private List<ISoundProvider> SoundProviders;

        public SoundMixer(params ISoundProvider[] soundProviders) 
        {
            SoundProviders = new List<ISoundProvider>(soundProviders);
        }
        
        public void AddSource(ISoundProvider source)
        {
            SoundProviders.Add(source);
        }

        public void DisableSource(ISoundProvider source)
        {
            SoundProviders.Remove(source);
        }

		public void DiscardSamples()
		{
			foreach (var soundSource in SoundProviders)
				soundSource.DiscardSamples();
		}

        public void GetSamples(short[] samples)
        {
            foreach (var soundSource in SoundProviders)
                soundSource.GetSamples(samples);
        }
    }
}
