
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Represents the piezoelectric buzzer used in the Spectrum to produce sound
    /// The beeper is controlled by rapidly toggling bit 4 of port &FE
    /// 
    /// For the purposes of emulation this devices is locked to a frame
    /// a list of Pulses is built up over the course of the frame and outputted at the end of the frame
    /// </summary>
    public class Buzzer : ISoundProvider, IBeeperDevice
    {
        /// <summary>
        /// Supplied values are right for 48K spectrum
        /// These will deviate for 128k and up (as there are more T-States per frame)
        /// </summary>
        //public int SampleRate = 44100; //35000;
        //public int SamplesPerFrame = 882; //699;
        //public int TStatesPerSample = 79; //100;
        
        /// <summary>
        /// Sample Rate 
        /// This usually has to be 44100 for ISoundProvider
        /// </summary>
        public int SampleRate
        {
            get { return _sampleRate; }
            set { _sampleRate = value; }
        }
        
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

        /// <summary>
        /// The tape loading volume
        /// Accepts an int 0-100 value
        /// </summary>
        private int _tapeVolume;
        public int TapeVolume
        {
            get
            {
                return VolumeConverterOut(_tapeVolume);
            }
            set
            {
                _tapeVolume = VolumeConverterIn(value);
            }
        }

        /// <summary>
        /// The EAR beeper volume
        /// </summary>
        private int _earVolume;
        public int EarVolume
        {
            get
            {
                return VolumeConverterOut(_earVolume);
            }
            set
            {
                _earVolume = VolumeConverterIn(value);
            }
        }

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


        private SpectrumBase _machine;

        /// <summary>
        /// State fields
        /// </summary>
        private long _frameStart;
        private bool _tapeMode;
        private long _tStatesPerFrame;
        private int _sampleRate;
        private int _samplesPerFrame;
        private int _tStatesPerSample;

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
            _tStatesPerSample = 79;
            _samplesPerFrame = (int)_tStatesPerFrame / _tStatesPerSample;

            /*

            // set the tstatesperframe
            _tStatesPerFrame = tStatesPerFrame;

            // calculate actual refresh rate
            double refresh = (double)_machine.ULADevice.ClockSpeed / (double)_tStatesPerFrame;

            // how many samples per frame are expected by ISoundProvider (at 44.1KHz)
            _samplesPerFrame = 880;// (int)((double)sampleRate / (double)refresh);

            // set the sample rate
            _sampleRate = sampleRate;

            // calculate samples per frame (what ISoundProvider will be expecting at 44100)
            //_samplesPerFrame = (int)((double)_tStatesPerFrame / (double)refresh);

            // calculate tstates per sameple
            _tStatesPerSample = 79;// _tStatesPerFrame / _samplesPerFrame;

            /*

           

            

            // get divisors
            var divs = from a in Enumerable.Range(2, _tStatesPerFrame / 2)
                       where _tStatesPerFrame % a == 0
                       select a;

            // get the highest int value under 120 (this will be TStatesPerSample)
            _tStatesPerSample = divs.Where(a => a < 100).Last();

            // get _samplesPerFrame
            _samplesPerFrame = _tStatesPerFrame / _tStatesPerSample;
            
    */
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
            if (!_machine._renderSound)
                return;

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
                    if (_tapeMode)
                        samples[sampleIndex++] = pulse.State ? (short)(_tapeVolume) : (short)0;
                    else
                        samples[sampleIndex++] = pulse.State ? (short)(_earVolume) : (short)0;
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
            nsamp = _samplesPerFrame; // soundBufferContains;
        }

        #endregion

        
        public void SyncState(Serializer ser)
        {
            ser.BeginSection("Buzzer");
            ser.Sync("_frameStart", ref _frameStart);
            ser.Sync("_tapeMode", ref _tapeMode);
            ser.Sync("_tStatesPerFrame", ref _tStatesPerFrame);
            ser.Sync("_sampleRate", ref _sampleRate);
            ser.Sync("_samplesPerFrame", ref _samplesPerFrame);
            ser.Sync("_tStatesPerSample", ref _tStatesPerSample);

            ser.Sync("soundBuffer", ref soundBuffer, false);
            ser.Sync("soundBufferContains", ref soundBufferContains);
            ser.EndSection();
        }
        

    }

    
}
