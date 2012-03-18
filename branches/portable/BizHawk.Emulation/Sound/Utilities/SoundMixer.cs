using System.Collections.Generic;

namespace BizHawk.Emulation.Sound
{
    // This is a straightforward class to mix/chain multiple ISoundProvider sources.

    public sealed class SoundMixer : ISoundProvider
    {
        List<ISoundProvider> SoundProviders;

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

        // Splits the volume space equally between available sources.
        public void EqualizeVolumes()
        {
            int eachVolume = short.MaxValue / SoundProviders.Count;
            foreach (var source in SoundProviders)
                source.MaxVolume = eachVolume;
        }

        // Not actually supported on mixer.
        public int MaxVolume { get; set; }
    }
}
