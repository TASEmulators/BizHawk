using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// My attempt at mixing multiple ISoundProvider sources together and outputting another ISoundProvider
    /// Currently only supports SyncSoundMode.Sync
    /// Attached ISoundProvider sources must already be stereo 44.1khz and ideally sound buffers should be the same length (882)
    /// (if not, only 882 samples of their buffer will be used)
    /// </summary>
    internal sealed class SoundProviderMixer : ISoundProvider
    {
        private class Provider
        {
            public ISoundProvider SoundProvider { get; set; }
            public string ProviderDescription { get; set; }
            public int MaxVolume { get; set; }
            public short[] Buffer { get; set; }
            public int NSamp { get; set; }
        }

        private bool _stereo = true;
        public bool Stereo
        {
            get { return _stereo; }
            set { _stereo = value; }
        }

        private readonly List<Provider> SoundProviders;
        
        public SoundProviderMixer(params ISoundProvider[] soundProviders)
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

        public SoundProviderMixer(short maxVolume, string description, params ISoundProvider[] soundProviders)
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

        public void AddSource(ISoundProvider source, string description)
        {
            SoundProviders.Add(new Provider
            {
                SoundProvider = source,
                MaxVolume = short.MaxValue,
                ProviderDescription = description
            });

            EqualizeVolumes();
        }

        public void AddSource(ISoundProvider source, short maxVolume, string description)
        {
            SoundProviders.Add(new Provider
            {
                SoundProvider = source,
                MaxVolume = maxVolume,
                ProviderDescription = description
            });

            EqualizeVolumes();
        }

        public void DisableSource(ISoundProvider source)
        {
            var sp = SoundProviders.Where(a => a.SoundProvider == source);
            if (sp.Count() == 1)
                SoundProviders.Remove(sp.First());
            else if (sp.Count() > 1)
                foreach (var s in sp)
                    SoundProviders.Remove(s);

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

        #region ISoundProvider

        public bool CanProvideAsync => false;
        public SyncSoundMode SyncMode => SyncSoundMode.Sync;

        public void SetSyncMode(SyncSoundMode mode)
        {
            if (mode != SyncSoundMode.Sync)
                throw new InvalidOperationException("Only Sync mode is supported.");
        }

        public void GetSamplesAsync(short[] samples)
        {
            throw new NotSupportedException("Async is not available");
        }

        public void DiscardSamples()
        {
            foreach (var soundSource in SoundProviders)
            {
                soundSource.SoundProvider.DiscardSamples();
            }
        }

        public void GetSamplesSync(out short[] samples, out int nsamp)
        {
            samples = null;
            nsamp = 0;

            // get samples from all the providers
            foreach (var sp in SoundProviders)
            {
                int sampCount;
                short[] samp;
                sp.SoundProvider.GetSamplesSync(out samp, out sampCount);
                sp.NSamp = sampCount;
                sp.Buffer = samp;
            }

            // are all the sample lengths the same?
            var firstEntry = SoundProviders.First();
            bool sameCount = SoundProviders.All(s => s.NSamp == firstEntry.NSamp);

            if (!sameCount)
            {
                // this is a bit hacky, really all ISoundProviders should be supplying 44100 with 882 samples per frame.
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
                    if (sp.Buffer[i] > sp.MaxVolume)
                        sectorVal += (short)sp.MaxVolume;
                    else
                        sectorVal += sp.Buffer[i];
                }

                samples[i] = sectorVal;
            }
        }

        #endregion

    }
}
