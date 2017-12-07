using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// My attempt at mixing multiple ISoundProvider sources together and outputting another ISoundProvider
    /// Currently only supports SyncSoundMode.Sync
    /// Attached ISoundProvider sources must already be stereo 44.1khz and ideally sound buffers should be the same length
    /// </summary>
    internal sealed class SoundProviderMixer : ISoundProvider
    {
        private class Provider
        {
            public ISoundProvider SoundProvider { get; set; }
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

        public void AddSource(ISoundProvider source)
        {
            SoundProviders.Add(new Provider
            {
                SoundProvider = source,
                MaxVolume = short.MaxValue
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

            
            if (sameCount)
            {
                nsamp = firstEntry.NSamp;
                samples = new short[nsamp * 2];

                if (_stereo)
                {
                    for (int i = 0; i < samples.Length; i++)
                    {
                        short sectorVal = 0;
                        foreach (var sp in SoundProviders)
                        {
                            if (sp.Buffer[i] > sp.MaxVolume)
                                sectorVal += (short)sp.MaxVolume;
                            else
                            {
                                sectorVal += sp.Buffer[i];
                            }
                        }

                        samples[i] = sectorVal;
                    }
                }
                else
                {
                    // convert to mono
                    for (int i = 0; i < samples.Length; i += 2)
                    {
                        short s = 0;
                        foreach (var sp in SoundProviders)
                        {
                            s += (short)((sp.Buffer[i] + sp.Buffer[i + 1]) / 2);
                        }

                        samples[i] = s;
                        samples[i + 1] = s;
                    }
                }
            }

            else if (!sameCount)
            {
                // this is a pretty poor implementation that doesnt work very well
                // ideally soundproviders should ensure that their number of samples is identical       
                int divisor = 1;
                int highestCount = 0;

                // get the lowest divisor of all the soundprovider nsamps
                for (int d = 2; d < 999; d++)
                {
                    bool divFound = false;
                    foreach (var sp in SoundProviders)
                    {
                        if (sp.NSamp > highestCount)
                            highestCount = sp.NSamp;

                        if (sp.NSamp % d == 0)
                            divFound = true;
                        else
                            divFound = false;
                    }

                    if (divFound)
                    {
                        divisor = d;
                        break;
                    }
                }

                // now we have the largest current number of samples among the providers
                // along with a common divisor for all of them
                nsamp = highestCount * divisor;
                samples = new short[nsamp * 2];

                // take a pass at populating the samples array for each provider
                foreach (var sp in SoundProviders)
                {
                    short sectorVal = 0;
                    int pos = 0;
                    for (int i = 0; i < sp.Buffer.Length; i++)
                    {
                        if (sp.Buffer[i] > sp.MaxVolume)
                            sectorVal = (short)sp.MaxVolume;
                        else
                            sectorVal = sp.Buffer[i];

                        for (int s = 0; s < divisor; s++)
                        {
                            samples[pos++] += sectorVal;
                        }
                    }
                }

                /*
                // get the highest number of samples
                int max = SoundProviders.Aggregate((i, j) => i.Buffer.Length > j.Buffer.Length ? i : j).Buffer.Length;

                nsamp = max;
                samples = new short[nsamp * 2];

                // take a pass at populating the samples array for each provider
                foreach (var sp in SoundProviders)
                {
                    short sectorVal = 0;
                    int pos = 0;
                    for (int i = 0; i < sp.Buffer.Length; i++)
                    {
                        if (sp.Buffer[i] > sp.MaxVolume)
                            sectorVal = (short)sp.MaxVolume;
                        else
                        {
                            if (sp.SoundProvider is AY38912)
                            {
                                // boost audio
                                sectorVal += (short)(sp.Buffer[i] * 2);
                            }
                            else
                            {
                                sectorVal += sp.Buffer[i];
                            }
                        }

                        samples[pos++] += sectorVal;
                    }
                }
               */

            }
        }

        #endregion


    }
}
