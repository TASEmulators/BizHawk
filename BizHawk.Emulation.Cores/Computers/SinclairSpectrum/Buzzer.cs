
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components;
using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Represents the piezoelectric buzzer used in the Spectrum to produce sound
    /// The beeper is controlled by rapidly toggling bit 4 of port &FE
    /// 
    /// For the purposes of emulation this devices is locked to a frame
    /// a list of Pulses is built up over the course of the frame and outputted at the end of the frame
    /// </summary>
    public class Buzzer : ISoundProvider
    {
        /// <summary>
        /// Supplied values are right for 48K spectrum
        /// These will deviate for 128k and up (as there are more T-States per frame)
        /// </summary>
        public int SampleRate = 44100; //35000;
        public int SamplesPerFrame = 882; //699;
        public int TStatesPerSample = 79; //100;

        public BlipBuffer BlipL { get; set; }
        public BlipBuffer BlipR { get; set; }

        private SpectrumBase _machine;

        private long _frameStart;
        private bool _tapeMode;
        private int _tStatesPerFrame;

        public SpeexResampler resampler { get; set; }

        /// <summary>
        /// Pulses collected during the last frame
        /// </summary>
        public List<Pulse> Pulses { get; private set; }

        /// <summary>
        /// The last pulse
        /// </summary>
        public bool LastPulse { get; private set; }

        /// <summary>
        /// The last T-State (cpu cycle) that the last pulse was received
        /// </summary>
        public int LastPulseTState { get; set; }

        #region Construction & Initialisation

        public Buzzer(SpectrumBase machine)
        {
            _machine = machine;
        }

        /// <summary>
        /// Initialises the buzzer
        /// </summary>
        public void Init()
        {
            _tStatesPerFrame = _machine.UlaFrameCycleCount;
            Pulses = new List<Pulse>(1000);  
        }

        #endregion

        /// <summary>
        /// When the pulse value from the EAR output changes it is processed here
        /// </summary>
        /// <param name="fromTape"></param>
        /// <param name="earPulse"></param>
        public void ProcessPulseValue(bool fromTape, bool earPulse)
        {
            if (!fromTape && _tapeMode)
            {
                // tape mode is active but the pulse value came from an OUT instruction
                // do not process the value
                //return;
            }

            if (earPulse == LastPulse)
            {
                // no change detected
                return;
            }

            // set the lastpulse
            LastPulse = earPulse;

            // get where we are in the frame
            var currentULACycle = _machine.CurrentFrameCycle;
            var currentBuzzerCycle = currentULACycle <= _tStatesPerFrame ? currentULACycle : _tStatesPerFrame;
            var length = currentBuzzerCycle - LastPulseTState;

            if (length == 0)
            {
                // the first T-State has changed the pulse
                // do not add it
            }
            else if (length > 0)
            {
                // add the pulse
                Pulse p = new Pulse
                {
                    State = !earPulse,
                    Length = length
                };
                Pulses.Add(p);
            }

            // set the last pulse tstate
            LastPulseTState = currentBuzzerCycle;
        }

        /// <summary>
        /// New frame starts
        /// </summary>
        public void StartFrame()
        {
            //DiscardSamples();
            Pulses.Clear();
            LastPulseTState = 0;
        }

        /// <summary>
        /// Frame is completed
        /// </summary>
        public void EndFrame()
        {
            // store the last pulse information
            if (LastPulseTState <= _tStatesPerFrame - 1)
            {
                Pulse p = new Pulse
                {
                    State = LastPulse,
                    Length = _tStatesPerFrame - LastPulseTState
                };
                Pulses.Add(p);
            }

            // create the sample array
            var firstSampleOffset = _frameStart % TStatesPerSample == 0 ? 0 : TStatesPerSample - (_frameStart + TStatesPerSample) % TStatesPerSample;
            var samplesInFrame = (_tStatesPerFrame - firstSampleOffset - 1) / TStatesPerSample + 1;
            var samples = new short[samplesInFrame];

            // convert pulses to samples
            var sampleIndex = 0;
            var currentEnd = _frameStart;

            foreach (var pulse in Pulses)
            {
                var firstSample = currentEnd % TStatesPerSample == 0
                    ? currentEnd : currentEnd + TStatesPerSample - currentEnd % TStatesPerSample;

                for (var i = firstSample; i < currentEnd + pulse.Length; i += TStatesPerSample)
                {
                    samples[sampleIndex++] = pulse.State ? (short)(short.MaxValue / 2) : (short)0;

                    //resampler.EnqueueSample(samples[sampleIndex - 1], samples[sampleIndex - 1]);
                    
                }
                    

                currentEnd += pulse.Length;
            }

            // fill the _sampleBuffer for ISoundProvider
            soundBufferContains = (int)samplesInFrame;

            if (soundBuffer.Length != soundBufferContains)
                soundBuffer = new short[soundBufferContains];

            samples.CopyTo(soundBuffer, 0);

            _frameStart += _tStatesPerFrame;
        }

        /// <summary>
        /// When the spectrum is set to receive tape input, the EAR output on the ULA is disabled
        /// (so no buzzer sound is emitted)
        /// </summary>
        /// <param name="tapeMode"></param>
        public void SetTapeMode(bool tapeMode)
        {
            _tapeMode = tapeMode;
        }


        #region ISoundProvider

        private short[] soundBuffer = new short[882];
        private int soundBufferContains = 0;

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
            short[] stereoBuffer = new short[soundBuffer.Length * 2];
            int index = 0;
            for (int i = 0; i < soundBufferContains; i++)
            {
                stereoBuffer[index++] = soundBuffer[i];
                stereoBuffer[index++] = soundBuffer[i];
            }

            samples = stereoBuffer;
        }

        public void DiscardSamples()
        {
            soundBufferContains = 0;
            soundBuffer = new short[SamplesPerFrame];
        }

        public void GetSamplesSync(out short[] samples, out int nsamp)
        {
            // convert to stereo
            short[] stereoBuffer = new short[soundBufferContains * 2];
            int index = 0;
            for (int i = 0; i < soundBufferContains; i++)
            {
                stereoBuffer[index++] = soundBuffer[i];
                stereoBuffer[index++] = soundBuffer[i];
            }

            samples = stereoBuffer;
            nsamp = soundBufferContains;
        }

        #endregion

    }
}
