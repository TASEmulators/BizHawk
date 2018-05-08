
using BizHawk.Common;
using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /*
    /// <summary>
    /// Represents the piezoelectric buzzer used in the Spectrum to produce sound
    /// The beeper is controlled by rapidly toggling bit 4 of port &FE
    /// 
    /// For the purposes of emulation this devices is locked to a frame
    /// a list of Pulses is built up over the course of the frame and outputted at the end of the frame
    /// 
    /// ZXHawk instantiates multiples of these buzzers to achieve separation between tape and spectrum audio output
    /// </summary>
    public class Buzzer : ISoundProvider, IBeeperDevice
    {
        #region Fields and Properties

        /// <summary>
        /// Sample Rate 
        /// This usually has to be 44100 for ISoundProvider
        /// </summary>
        private int _sampleRate;
        public int SampleRate
        {
            get { return _sampleRate; }
            set { _sampleRate = value; }
        }

        /// <summary>
        /// Number of samples in one frame
        /// </summary>
        private int _samplesPerFrame;
        public int SamplesPerFrame
        {
            get { return _samplesPerFrame; }
            set { _samplesPerFrame = value; }
        }

        /// <summary>
        /// Number of TStates in each sample
        /// </summary>
        private int _tStatesPerSample;
        public int TStatesPerSample
        {
            get { return _tStatesPerSample; }
            set { _tStatesPerSample = value; }
        }

        /// <summary>
        /// Buzzer volume
        /// Accepts an int 0-100 value
        /// </summary>
        private int _volume;
        public int Volume
        {
            get
            {
                return VolumeConverterOut(_volume);
            }
            set
            {
                _volume = VolumeConverterIn(value);
            }
        }

        /// <summary>
        /// The number of cpu cycles per frame
        /// </summary>
        private long _tStatesPerFrame;

        /// <summary>
        /// The cycle at which the frame starts
        /// </summary>
        private long _frameStart;

        /// <summary>
        /// The parent emulated machine
        /// </summary>
        private SpectrumBase _machine;

        /// <summary>
        /// Pulses collected during the last frame
        /// </summary>
        public List<Pulse> Pulses { get; private set; }

        /// <summary>
        /// The last pulse
        /// </summary>
        public bool LastPulse { get; set; }

        /// <summary>
        /// The last T-State (cpu cycle) that the last pulse was received
        /// </summary>
        public long LastPulseTState { get; set; }

        #endregion

        #region Private Methods

        /// <summary>
        /// Takes an int 0-100 and returns the relevant short volume to output
        /// </summary>
        /// <param name="vol"></param>
        /// <returns></returns>
        private int VolumeConverterIn(int vol)
        {
            int maxLimit = short.MaxValue / 3;
            int increment = maxLimit / 100;

            return vol * increment;
        }

        /// <summary>
        /// Takes an short volume and returns the relevant int value 0-100
        /// </summary>
        /// <param name="vol"></param>
        /// <returns></returns>
        private int VolumeConverterOut(int shortvol)
        {
            int maxLimit = short.MaxValue / 3;
            int increment = maxLimit / 100;

            if (shortvol > maxLimit)
                shortvol = maxLimit;

            return shortvol / increment;
        }

        #endregion

        #region Construction & Initialisation

        public Buzzer(SpectrumBase machine)
        {
            _machine = machine;
        }

        /// <summary>
        /// Initialises the buzzer
        /// </summary>
        public void Init(int sampleRate, int tStatesPerFrame)
        {
            _sampleRate = sampleRate;
            _tStatesPerFrame = tStatesPerFrame;
            _tStatesPerSample = 99; // 79;
            _samplesPerFrame = 705; // 882; // (int)_tStatesPerFrame / _tStatesPerSample;
            
            Pulses = new List<Pulse>(1000);  
        }

        #endregion

        #region IBeeperDevice

        /// <summary>
        /// When the pulse value changes it is processed here
        /// </summary>
        /// <param name="pulse"></param>
        public void ProcessPulseValue(bool pulse)
        {
            if (!_machine._renderSound)
                return;

            if (pulse == LastPulse)
            {
                // no change detected
                return;
            }

            // set the lastpulse
            LastPulse = pulse;

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
                    State = !pulse,
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
                    samples[sampleIndex++] = pulse.State ? (short)(_volume) : (short)0;
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

        #endregion

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
            nsamp = soundBufferContains; // _samplesPerFrame; // soundBufferContains;
        }

        #endregion

        #region State Serialization

        public void SyncState(Serializer ser)
        {
            ser.BeginSection("Buzzer");
            ser.Sync("_frameStart", ref _frameStart);
            ser.Sync("_tStatesPerFrame", ref _tStatesPerFrame);
            ser.Sync("_sampleRate", ref _sampleRate);
            ser.Sync("_samplesPerFrame", ref _samplesPerFrame);
            ser.Sync("_tStatesPerSample", ref _tStatesPerSample);
            ser.Sync("soundBuffer", ref soundBuffer, false);
            ser.Sync("soundBufferContains", ref soundBufferContains);
            ser.EndSection();
        }

        #endregion
    }

    */
}
