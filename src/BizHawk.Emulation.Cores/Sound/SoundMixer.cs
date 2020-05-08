using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Components
{
	// TODO: Sound mixer is a good concept, but it needs to be refactored to use an ISoundProvider, it perhaps can enforce only receiving providers in Async mode
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
		private readonly List<IMixedSoundProvider> _soundProviders;

		public SoundMixer(params IMixedSoundProvider[] soundProviders)
		{
			_soundProviders = new List<IMixedSoundProvider>(soundProviders);
		}

		public void DisableSource(IMixedSoundProvider source)
		{
			_soundProviders.Remove(source);
		}

		public void DiscardSamples()
		{
			foreach (var soundSource in _soundProviders)
			{
				soundSource.DiscardSamples();
			}
		}

		public void GetSamples(short[] samples)
		{
			foreach (var soundSource in _soundProviders)
			{
				soundSource.GetSamples(samples);
			}
		}

		// Splits the volume space equally between available sources.
		public void EqualizeVolumes()
		{
			int eachVolume = short.MaxValue / _soundProviders.Count;
			foreach (var source in _soundProviders)
			{
				source.MaxVolume = eachVolume;
			}
		}
	}
}
