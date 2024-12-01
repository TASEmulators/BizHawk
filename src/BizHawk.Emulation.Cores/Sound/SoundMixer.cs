using System.Collections.Generic;
using System.Linq;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Components
{
	// TODO: Sound mixer is a good concept, but it needs to support sync mode
	/// <summary>
	/// An interface that extends a sound provider to provide mixing capabilities through the SoundMixer class
	/// </summary>
	internal interface IMixedSoundProvider : ISoundProvider
	{
		int MaxVolume { get; set; }
	}

	// This is a straightforward class to mix/chain multiple ISoundProvider sources.
	// Relies on a hack of passing in the samples per frame for sync sound abilities
	internal sealed class SoundMixer : ISoundProvider
	{
		private readonly int _spf;
		private readonly List<IMixedSoundProvider> _soundProviders;

		public SoundMixer(int spf, params IMixedSoundProvider[] soundProviders)
		{
			if (soundProviders.Any(s => !s.CanProvideAsync))
			{
				throw new InvalidOperationException("Sound mixer only works with async sound currently");
			}

			_soundProviders = new List<IMixedSoundProvider>(soundProviders);
			_spf = spf;
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

		public bool CanProvideAsync => true;

		public void SetSyncMode(SyncSoundMode mode) => SyncMode = mode;
		public SyncSoundMode SyncMode { get; private set; }

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			if (SyncMode != SyncSoundMode.Sync)
			{
				throw new InvalidOperationException("Must be in sync mode to call a sync method");
			}

			short[] ret = new short[_spf * 2];
			GetSamplesAsync(ret);
			samples = ret;
			nsamp = _spf;
		}

		public void GetSamplesAsync(short[] samples)
		{
			foreach (var soundSource in _soundProviders)
			{
				soundSource.GetSamplesAsync(samples);
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
