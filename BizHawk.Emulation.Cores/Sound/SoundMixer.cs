using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Components
{
	// TODO: Sound mixer is a good concept, but it needs to be refactored to use an ISoundProvider, it perhaps can enforce only recieving providers in Async mode
	/// <summary>
	/// An interface that extends a sound provider to provide mixing capabilities through the SoundMixer class
	/// </summary>
	internal interface IMixedSoundProvider : IAsyncSoundProvider
	{
		int MaxVolume { get; set; }
	}

	// This is a straightforward class to mix/chain multiple ISoundProvider sources.
	internal sealed class SoundMixer : IAsyncSoundProvider
	{
		private readonly List<IMixedSoundProvider> SoundProviders;

		public SoundMixer(params IMixedSoundProvider[] soundProviders)
		{
			SoundProviders = new List<IMixedSoundProvider>(soundProviders);
		}

		public void AddSource(IMixedSoundProvider source)
		{
			SoundProviders.Add(source);
		}

		public void DisableSource(IMixedSoundProvider source)
		{
			SoundProviders.Remove(source);
		}

		public void DiscardSamples()
		{
			foreach (var soundSource in SoundProviders)
			{
				soundSource.DiscardSamples();
			}
		}

		public void GetSamples(short[] samples)
		{
			foreach (var soundSource in SoundProviders)
			{
				soundSource.GetSamples(samples);
			}
		}

		// Splits the volume space equally between available sources.
		public void EqualizeVolumes()
		{
			int eachVolume = short.MaxValue / SoundProviders.Count;
			foreach (var source in SoundProviders)
			{
				source.MaxVolume = eachVolume;
			}
		}
	}
}
