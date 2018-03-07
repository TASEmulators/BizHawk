
using BizHawk.Common;
using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    public class AY38912 : ISoundProvider
    {
        private int _tStatesPerFrame;
        private int _sampleRate;
        private int _samplesPerFrame;
        private int _tStatesPerSample;
        private int _sampleCounter;
        const int AY_SAMPLE_RATE = 16;
        private int _AYCyclesPerFrame;
        private int _nsamp;
        private int _AYCount;
        

        /// <summary>
        /// The final sample buffer
        /// </summary>
        private short[] _samples = new short[0];

        /// <summary>
        /// Number of samples in one frame
        /// </summary>
        public int SamplesPerFrame
        {
            get { return _samplesPerFrame; }
            set { _samplesPerFrame = value; }
        }

        /// <summary>
        /// Number of TStates in each sample
        /// </summary>
        public int TStatesPerSample
        {
            get { return _tStatesPerSample; }
            set { _tStatesPerSample = value; }
        }

        #region Construction & Initialisation

        public AY38912()
        {
            Reset();
        }

        /// <summary>
        /// Initialises the AY chip
        /// </summary>
        public void Init(int sampleRate, int tStatesPerFrame)
        {
            _sampleRate = sampleRate;
            _tStatesPerFrame = tStatesPerFrame;
            _tStatesPerSample = 79;
            _samplesPerFrame = _tStatesPerFrame / _tStatesPerSample;
            _AYCyclesPerFrame = _tStatesPerFrame / AY_SAMPLE_RATE;

            _samples = new short[_samplesPerFrame * 2];
            _nsamp = _samplesPerFrame;
        }

        #endregion

        public void UpdateSound(int currentFrameCycle)
        {
            //if (currentFrameCycle >= _tStatesPerFrame)
                //currentFrameCycle = _tStatesPerFrame;

            for (int i = 0; i < (currentFrameCycle / AY_SAMPLE_RATE) - _AYCount; i++)
            {
                Update();
                SampleAY();
                _AYCount++;
            }

            // calculate how many samples must be processed
            int samplesToGenerate = (currentFrameCycle / _tStatesPerSample) - (_sampleCounter / 2);

            // begin generation
            if (samplesToGenerate > 0)
            {
                // ensure the required resolution
                while (soundSampleCounter < 4)
                {
                    SampleAY();
                }
                EndSampleAY();

                // generate needed samples
                for (int i = 0; i < samplesToGenerate; i++)
                {
                    _samples[_sampleCounter++] = (short)(averagedChannelSamples[0]);
                    _samples[_sampleCounter++] = (short)(averagedChannelSamples[1]);

                    samplesToGenerate--;
                }

                averagedChannelSamples[0] = 0;
                averagedChannelSamples[1] = 0;
                averagedChannelSamples[2] = 0;
            }            
        }

        public void StartFrame()
        {
            _AYCount = 0;
            
            // the stereo _samples buffer should already have been processed as a part of
            // ISoundProvider at the end of the last frame
            //_samples = new short[_samplesPerFrame * 2];
            //_nsamp = _samplesPerFrame;
            _sampleCounter = 0;

            //Init(44100, _tStatesPerFrame);
        }

        public void EndFrame()
        {
        }


        public void Reset()
        {
            // reset volumes
            for (int i = 0; i < 16; i++)
                AY_SpecVolumes[i] = (short)(AY_Volumes[i] * 8191);

            soundSampleCounter = 0;
            regs[AY_NOISEPER] = 0xFF;
            noiseOut = 0x01;
            envelopeVolume = 0;
            noiseCount = 0;

            // reset state of all channels
            for (int f = 0; f < 3; f++)
            {
                channel_count[f] = 0;
                channel_mix[f] = 0;
                channel_out[f] = 0;
                averagedChannelSamples[f] = 0;
            }

            envelopeCount = 0;
            randomSeed = 1;
            selectedRegister = 0;
        }

        #region IStatable

        public void SyncState(Serializer ser)
        {
            ser.BeginSection("AY38912");
            ser.Sync("_tStatesPerFrame", ref _tStatesPerFrame);
            ser.Sync("_sampleRate", ref _sampleRate);
            ser.Sync("_samplesPerFrame", ref _samplesPerFrame);
            ser.Sync("_tStatesPerSample", ref _tStatesPerSample);
            ser.Sync("_sampleCounter", ref _sampleCounter);

            ser.Sync("ChannelLeft", ref ChannelLeft);
            ser.Sync("ChannelRight", ref ChannelRight);
            ser.Sync("ChannelCenter", ref ChannelCenter);
            ser.Sync("Regs", ref regs, false);
            ser.Sync("NoiseOut", ref noiseOut);
            ser.Sync("envelopeVolume", ref envelopeVolume);
            ser.Sync("noiseCount", ref noiseCount);
            ser.Sync("envelopeCount", ref envelopeCount);
            ser.Sync("randomSeed", ref randomSeed);
            ser.Sync("envelopeClock", ref envelopeClock);
            ser.Sync("selectedRegister", ref selectedRegister);
            ser.Sync("soundSampleCounter", ref soundSampleCounter);
            ser.Sync("stereoSound", ref stereoSound);
            ser.Sync("sustaining", ref sustaining);
            ser.Sync("sustain", ref sustain);
            ser.Sync("alternate", ref alternate);
            ser.Sync("attack", ref attack);
            ser.Sync("envelopeStep", ref envelopeStep);

            ser.Sync("channel_out", ref channel_out, false);
            ser.Sync("channel_count", ref channel_count, false);
            ser.Sync("averagedChannelSamples", ref averagedChannelSamples, false);
            ser.Sync("channel_mix", ref channel_mix, false);

            ser.Sync("AY_SpecVolumes", ref AY_SpecVolumes, false);

            ser.Sync("_samples", ref _samples, false);
            ser.Sync("_nsamp", ref _nsamp);
            ser.EndSection();
        }

        #endregion

        #region AY Sound Implementation

        /*
            Based on the AYSound class from ArjunNair's Zero-Emulator
            https://github.com/ArjunNair/Zero-Emulator/
            *MIT LICENSED*

            The MIT License (MIT)
            Copyright (c) 2009 Arjun Nair
            Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files 
            (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, 
            publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
            subject to the following conditions:

            The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

            THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
            MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE 
            FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION 
            WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
        */

        /// <summary>
        /// Register constants
        /// </summary>
        private const byte AY_A_FINE = 0;
        private const byte AY_A_COARSE = 1;
        private const byte AY_B_FINE = 2;
        private const byte AY_B_COARSE = 3;
        private const byte AY_C_FINE = 4;
        private const byte AY_C_COARSE = 5;
        private const byte AY_NOISEPER = 6;
        private const byte AY_ENABLE = 7;
        private const byte AY_A_VOL = 8;
        private const byte AY_B_VOL = 9;
        private const byte AY_C_VOL = 10;
        private const byte AY_E_FINE = 11;
        private const byte AY_E_COARSE = 12;
        private const byte AY_E_SHAPE = 13;
        private const byte AY_PORT_A = 14;
        private const byte AY_PORT_B = 15;

        /// <summary>
        /// Channels
        /// </summary>
        internal enum Channel
        {
            A, B, C
        }

        /// <summary>
        /// ACB configuration
        /// </summary>
        private int ChannelLeft = 0;
        private int ChannelRight = 1;   //2 if ABC
        private int ChannelCenter = 2;  //1 if ABC

        /// <summary>
        /// Register storage
        /// </summary>
        private int[] regs = new int[16];

        /// <summary>
        /// State
        /// </summary>
        private int noiseOut;
        private int envelopeVolume;
        private int noiseCount;
        private int envelopeCount;
        private ulong randomSeed;
        private byte envelopeClock = 0;
        private int selectedRegister;
        public ushort soundSampleCounter;
        private bool stereoSound = false;
        private bool sustaining;
        private bool sustain;
        private bool alternate;
        private int attack;
        private int envelopeStep;

        /// <summary>
        /// Buffer arrays
        /// </summary>
        private int[] channel_out = new int[3];
        private int[] channel_count = new int[3];
        private int[] averagedChannelSamples = new int[3];
        private short[] channel_mix = new short[3];

        /// <summary>
        /// Measurements from comp.sys.sinclair (2001 Matthew Westcott)
        /// </summary>
        private float[] AY_Volumes =
        {
            0.0000f, 0.0137f, 0.0205f, 0.0291f,
            0.0423f, 0.0618f, 0.0847f, 0.1369f,
            0.1691f, 0.2647f, 0.3527f, 0.4499f,
            0.5704f, 0.6873f, 0.8482f, 1.0000f
        };

        /// <summary>
        /// Volume storage (short)
        /// </summary>
        private short[] AY_SpecVolumes = new short[16];

        /// <summary>
        /// Sets the ACB configuration
        /// </summary>
        /// <param name="val"></param>
        public void SetSpeakerACB(bool val)
        {
            // ACB
            if (val)
            {
                ChannelCenter = 2;
                ChannelRight = 1;
            }
            // ABC
            else
            {
                ChannelCenter = 1;
                ChannelRight = 2;
            }
        }

        /// <summary>
        /// Utility method to set all registers externally
        /// </summary>
        /// <param name="_regs"></param>
        public void SetRegisters(byte[] _regs)
        {
            for (int f = 0; f < 16; f++)
                regs[f] = _regs[f];
        }

        /// <summary>
        /// Utility method to get all registers externally
        /// </summary>
        /// <returns></returns>
        public byte[] GetRegisters()
        {
            byte[] newArray = new byte[16];
            for (int f = 0; f < 16; f++)
                newArray[f] = (byte)(regs[f] & 0xff);
            return newArray;
        }

        /// <summary>
        /// Selected Register property
        /// </summary>
        public int SelectedRegister
        {
            get { return selectedRegister; }
            set { if (value < 16) selectedRegister = value; }
        }

        /// <summary>
        /// Simulates a port write to the AY chip
        /// </summary>
        /// <param name="val"></param>
        public void PortWrite(int val)
        {
            switch (SelectedRegister)
            {
                // not implemented / necessary
                case AY_A_FINE:
                case AY_B_FINE:
                case AY_C_FINE:
                case AY_E_FINE:
                case AY_E_COARSE:
                    break;

                case AY_A_COARSE:
                case AY_B_COARSE:
                case AY_C_COARSE:
                    val &= 0x0f;
                    break;

                case AY_NOISEPER:
                case AY_A_VOL:
                case AY_B_VOL:
                case AY_C_VOL:
                    val &= 0x1f;
                    break;

                case AY_ENABLE:
                    /*
                    if ((lastEnable == -1) || ((lastEnable & 0x40) != (regs[AY_ENABLE] & 0x40))) {
                        SelectedRegister = ((regs[AY_ENABLE] & 0x40) > 0 ? regs[AY_PORT_B] : 0xff);
                    }
                    if ((lastEnable == -1) || ((lastEnable & 0x80) != (regs[AY_ENABLE] & 0x80))) {
                         PortWrite((regs[AY_ENABLE] & 0x80) > 0 ? regs[AY_PORT_B] : 0xff);
                    }
                    lastEnable = regs[AY_ENABLE];*/
                    break;                

                case AY_E_SHAPE:
                    val &= 0x0f;
                    attack = ((val & 0x04) != 0 ? 0x0f : 0x00);
                    // envelopeCount = 0;
                    if ((val & 0x08) == 0)
                    {
                        /* if Continue = 0, map the shape to the equivalent one which has Continue = 1 */
                        sustain = true;
                        alternate = (attack != 0);
                    }
                    else
                    {
                        sustain = (val & 0x01) != 0;
                        alternate = (val & 0x02) != 0;
                    }
                    envelopeStep = 0x0f;
                    sustaining = false;
                    envelopeVolume = (envelopeStep ^ attack);
                    break;

                case AY_PORT_A:
                    /*
                    if ((regs[AY_ENABLE] & 0x40) > 0) {
                        selectedRegister = regs[AY_PORT_A];
                    }*/
                    break;

                case AY_PORT_B:
                    /*
                    if ((regs[AY_ENABLE] & 0x80) > 0) {
                        PortWrite(regs[AY_PORT_A]);
                    }*/
                    break;
            }

            regs[SelectedRegister] = val;
        }

        /// <summary>
        /// Simulates port reads from the AY chip
        /// </summary>
        /// <returns></returns>
        public int PortRead()
        {
            if (SelectedRegister == AY_PORT_B)
            {
                if ((regs[AY_ENABLE] & 0x80) == 0)
                    return 0xff;
                else
                    return regs[AY_PORT_B];
            }

            return regs[selectedRegister];
        }

        private void EndSampleAY()
        {
            averagedChannelSamples[0] = (short)((averagedChannelSamples[ChannelLeft] + averagedChannelSamples[ChannelCenter]) / soundSampleCounter);
            averagedChannelSamples[1] = (short)((averagedChannelSamples[ChannelRight] + averagedChannelSamples[ChannelCenter]) / soundSampleCounter);
            
            soundSampleCounter = 0;
        }

        private void SampleAY()
        {
            int ah;

            ah = regs[AY_ENABLE];

            channel_mix[(int)Channel.A] = MixChannel(ah, regs[AY_A_VOL], (int)Channel.A);

            ah >>= 1;
            channel_mix[(int)Channel.B] = MixChannel(ah, regs[AY_B_VOL], (int)Channel.B);

            ah >>= 1;
            channel_mix[(int)Channel.C] = MixChannel(ah, regs[AY_C_VOL], (int)Channel.C);

            averagedChannelSamples[0] += channel_mix[(int)Channel.A];
            averagedChannelSamples[1] += channel_mix[(int)Channel.B];
            averagedChannelSamples[2] += channel_mix[(int)Channel.C];
            soundSampleCounter++;
        }

        private short MixChannel(int ah, int cl, int chan)
        {
            int al = channel_out[chan];
            int bl, bh;
            bl = ah;
            bh = ah;
            bh &= 0x1;
            bl >>= 3;

            al |= (bh); //Tone | AY_ENABLE
            bl |= (noiseOut); //Noise | AY_ENABLE
            al &= bl;

            if ((al != 0))
            {
                if ((cl & 16) != 0)
                    cl = envelopeVolume;

                cl &= 15;

                //return (AY_Volumes[cl]);
                return (AY_SpecVolumes[cl]);
            }
            return 0;
        }

        /// <summary>
        /// Gets the tone period for the specified channel
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        private int TonePeriod(int channel)
        {
            return (regs[(channel) << 1] | ((regs[((channel) << 1) | 1] & 0x0f) << 8));
        }

        /// <summary>
        /// Gets the noise period for the specified channel
        /// </summary>
        /// <returns></returns>
        private int NoisePeriod()
        {
            return (regs[AY_NOISEPER] & 0x1f);
        }

        /// <summary>
        /// Gets the envelope period for the specified channel
        /// </summary>
        /// <returns></returns>
        private int EnvelopePeriod()
        {
            return ((regs[AY_E_FINE] | (regs[AY_E_COARSE] << 8)));
        }

        /// <summary>
        /// Gets the noise enable value for the specified channel
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        private int NoiseEnable(int channel)
        {
            return ((regs[AY_ENABLE] >> (3 + channel)) & 1);
        }

        /// <summary>
        /// Gets the tone enable value for the specified channel
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        private int ToneEnable(int channel)
        {
            return ((regs[AY_ENABLE] >> (channel)) & 1);
        }

        /// <summary>
        /// Gets the tone envelope value for the specified channel
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        private int ToneEnvelope(int channel)
        {
            //return ((regs[AY_A_VOL + channel] & 0x10) >> 4);
            return ((regs[AY_A_VOL + channel] >> 4) & 0x1);
        }

        /// <summary>
        /// Updates noise
        /// </summary>
        private void UpdateNoise()
        {
            noiseCount++;
            if (noiseCount >= NoisePeriod() && (noiseCount > 4))
            {
                /* Is noise output going to change? */
                if (((randomSeed + 1) & 2) != 0) /* (bit0^bit1)? */
                {
                    noiseOut ^= 1;
                }

                /* The Random Number Generator of the 8910 is a 17-bit shift */
                /* register. The input to the shift register is bit0 XOR bit3 */
                /* (bit0 is the output). This was verified on AY-3-8910 and YM2149 chips. */

                /* The following is a fast way to compute bit17 = bit0^bit3. */
                /* Instead of doing all the logic operations, we only check */
                /* bit0, relying on the fact that after three shifts of the */
                /* register, what now is bit3 will become bit0, and will */
                /* invert, if necessary, bit14, which previously was bit17. */
                if ((randomSeed & 1) != 0)
                    randomSeed ^= 0x24000; /* This version is called the "Galois configuration". */
                randomSeed >>= 1;
                noiseCount = 0;
            }
        }

        /// <summary>
        /// Updates envelope
        /// </summary>
        private void UpdateEnvelope()
        {
            /* update envelope */
            if (!sustaining)
            {
                envelopeCount++;
                if ((envelopeCount >= EnvelopePeriod()))
                {
                    envelopeStep--;

                    /* check envelope current position */
                    if (envelopeStep < 0)
                    {
                        if (sustain)
                        {
                            if (alternate)
                                attack ^= 0x0f;
                            sustaining = true;
                            envelopeStep = 0;
                        }
                        else
                        {
                            /* if CountEnv has looped an odd number of times (usually 1), */
                            /* invert the output. */
                            if (alternate && ((envelopeStep & (0x0f + 1)) != 0) && (envelopeCount > 4))
                                attack ^= 0x0f;

                            envelopeStep &= 0x0f;
                        }
                    }
                    envelopeCount = 0;
                }
            }
            envelopeVolume = (envelopeStep ^ attack);
        }


        public void Update()
        {
            envelopeClock ^= 1;

            if (envelopeClock == 1)
            {
                envelopeCount++;

                //if ((((regs[AY_A_VOL + 0] & 0x10) >> 4) & (((regs[AY_A_VOL + 1] & 0x10) >> 4) & ((regs[AY_A_VOL + 2] & 0x10) >> 4))) != 1)
                //if ((((regs[AY_A_VOL + 0] >> 4) & 0x1) & (((regs[AY_A_VOL + 1] >> 4) & 0x1) & ((regs[AY_A_VOL + 2] >> 4) & 0x1))) != 0)
                if (((regs[AY_A_VOL + 0] & 0x10) & (regs[AY_A_VOL + 1] & 0x10) & (regs[AY_A_VOL + 2] & 0x10)) != 1)
                {
                    // update envelope
                    if (!sustaining)
                        UpdateEnvelope();

                    envelopeVolume = (envelopeStep ^ attack);
                }
            }

            // update noise
            if ((regs[AY_ENABLE] & 0x38) != 0x38)
            {
                UpdateNoise();
            }

            // update channels
            channel_count[0]++;
            int regs1 = (regs[1] & 0x0f) << 8;
            if (((regs[0] | regs1) > 4) && (channel_count[0] >= (regs[0] | regs1)))
            {
                channel_out[0] ^= 1;
                channel_count[0] = 0;
            }

            int regs3 = (regs[3] & 0x0f) << 8;
            channel_count[1]++;
            if (((regs[2] | regs3) > 4) && (channel_count[1] >= (regs[2] | regs3)))
            {
                channel_out[1] ^= 1;
                channel_count[1] = 0;
            }

            int regs5 = (regs[5] & 0x0f) << 8;
            channel_count[2]++;
            if (((regs[4] | regs5) > 4) && (channel_count[2] >= (regs[4] | regs5)))
            {
                channel_out[2] ^= 1;
                channel_count[2] = 0;
            }
        }


        #endregion

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

        }

        public void GetSamplesSync(out short[] samples, out int nsamp)
        {
            samples = _samples;
            nsamp = _nsamp;
        }

        #endregion

    }
}
