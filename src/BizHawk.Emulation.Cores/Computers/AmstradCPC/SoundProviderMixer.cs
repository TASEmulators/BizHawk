using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	/// <summary>
	/// My attempt at mixing multiple <see cref="ISyncSoundProvider"/> sources together and outputting another <see cref="ISyncSoundProvider"/><br/>
	/// Currently does not support <see cref="IAsyncSoundProvider"/><br/>
	/// Attached ISyncSoundProvider sources must already be stereo 44.1khz and ideally sound buffers should be the same length (882)
	/// (if not, only 882 samples of their buffer will be used)
	/// </summary>
	internal sealed class SoundProviderMixer : ISyncSoundProvider
	{
		private class Provider
		{
			public ISyncSoundProvider SoundProvider { get; set; }
			public string ProviderDescription { get; set; }
			public int MaxVolume { get; set; }
			public short[] Buffer { get; set; }
			public int NSamp { get; set; }
		}

		private bool _stereo = true;
		public bool Stereo
		{
			get => _stereo;
			set => _stereo = value;
		}

		private readonly List<Provider> SoundProviders;

		public SoundProviderMixer(params ISyncSoundProvider[] soundProviders)
		{
			SoundProviders = new List<Provider>();

			foreach (var s in soundProviders)
			{
				SoundProviders.Add(new Provider
				{
					SoundProvider = s,
					MaxVolume = short.MaxValue,
				});
			}

			EqualizeVolumes();
		}

		public SoundProviderMixer(short maxVolume, string description, params ISyncSoundProvider[] soundProviders)
		{
			SoundProviders = new List<Provider>();

			foreach (var s in soundProviders)
			{
				SoundProviders.Add(new Provider
				{
					SoundProvider = s,
					MaxVolume = maxVolume,
					ProviderDescription = description
				});
			}

			EqualizeVolumes();
		}

		public void AddSource(ISyncSoundProvider source, string description)
		{
			SoundProviders.Add(new Provider
			{
				SoundProvider = source,
				MaxVolume = short.MaxValue,
				ProviderDescription = description
			});

			EqualizeVolumes();
		}

		public void AddSource(ISyncSoundProvider source, short maxVolume, string description)
		{
			SoundProviders.Add(new Provider
			{
				SoundProvider = source,
				MaxVolume = maxVolume,
				ProviderDescription = description
			});

			EqualizeVolumes();
		}

		public void DisableSource(ISyncSoundProvider source)
		{
			SoundProviders.RemoveAll(a => a.SoundProvider == source);
			EqualizeVolumes();
		}

		public void EqualizeVolumes()
		{
			if (SoundProviders.Count < 1)
				return;

			int eachVolume = short.MaxValue / SoundProviders.Count;
			foreach (var source in SoundProviders)
			{
				source.MaxVolume = eachVolume;
			}
		}

		public void DiscardSamples()
		{
			foreach (var soundSource in SoundProviders)
			{
				soundSource.SoundProvider.DiscardSamples();
			}
		}

		public void GetSyncSoundSamples(out short[] samples, out int nsamp)
		{
			samples = null;
			nsamp = 0;

			// get samples from all the providers
			foreach (var sp in SoundProviders)
			{
				sp.SoundProvider.GetSyncSoundSamples(out var samp, out var sampCount);
				sp.NSamp = sampCount;
				sp.Buffer = samp;
			}

			// are all the sample lengths the same?
			var firstEntry = SoundProviders[0];
			bool sameCount = SoundProviders.All(s => s.NSamp == firstEntry.NSamp);

			if (!sameCount)
			{
				// this is a bit hacky, really all ISyncSoundProviders should be supplying 44100 with 882 samples per frame.
				// we will make sure this happens (no matter how it sounds)
				if (SoundProviders.Count > 1)
				{
					for (int i = 0; i < SoundProviders.Count; i++)
					{
						int ns = SoundProviders[i].NSamp;
						short[] buff = new short[882 * 2];

						for (int b = 0; b < 882 * 2; b++)
						{
							if (b == SoundProviders[i].Buffer.Length - 1)
							{
								// end of source buffer
								break;
							}

							buff[b] = SoundProviders[i].Buffer[b];
						}

						// save back to the soundprovider
						SoundProviders[i].NSamp = 882;
						SoundProviders[i].Buffer = buff;
					}
				}
				else
				{
					// just process what we have as-is 
				}
			}

			// mix the soundproviders together
			nsamp = 882;
			samples = new short[nsamp * 2];

			for (int i = 0; i < samples.Length; i++)
			{
				short sectorVal = 0;
				foreach (var sp in SoundProviders)
				{
					if (i < sp.Buffer.Length)
					{
						if (sp.Buffer[i] > sp.MaxVolume)
							sectorVal += (short)sp.MaxVolume;
						else
							sectorVal += sp.Buffer[i];
					}

				}

				samples[i] = sectorVal;
			}
		}
	}
}
