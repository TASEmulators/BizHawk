using BizHawk.Common;
using BizHawk.Emulation.Common;
using System;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Logical Beeper class
    /// Represents the piezoelectric buzzer used in the Spectrum to produce sound
    /// The beeper is controlled by rapidly toggling bit 4 of port &FE
    /// 
    /// It is instantiated twice, once for speccy beeper output, and once tape buzzer emulation
    /// 
    /// This implementation uses BlipBuffer and should *always* output at 44100 with 882 samples per frame
    /// (so that it can be mixed easily further down the line)
    /// </summary>
    public class Beeper : ISoundProvider, IBeeperDevice
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
                var newVol = VolumeConverterIn(value);
                if (newVol != _volume)
                    blip.Clear();
                _volume = VolumeConverterIn(value);
            }
        }

        /// <summary>
        /// The last used volume (used to modify blipbuffer delta values)
        /// </summary>
        private int lastVolume;

        /// <summary>
        /// The number of cpu cycles per frame
        /// </summary>
        private long _tStatesPerFrame;

        /// <summary>
        /// The parent emulated machine
        /// </summary>
        private SpectrumBase _machine;

        /// <summary>
        /// The last pulse
        /// </summary>
        private bool LastPulse;

        /// <summary>
        /// The last T-State (cpu cycle) that the last pulse was received
        /// </summary>
        private long LastPulseTState;

        /// <summary>
        /// Device blipbuffer
        /// </summary>
        private readonly BlipBuffer blip = new BlipBuffer(882);

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

        public Beeper(SpectrumBase machine)
        {
            _machine = machine;
        }

        /// <summary>
        /// Initialises the beeper
        /// </summary>
        public void Init(int sampleRate, int tStatesPerFrame)
        {
            blip.SetRates((tStatesPerFrame * 50), sampleRate);
            _sampleRate = sampleRate;
            _tStatesPerFrame = tStatesPerFrame;
        }

        #endregion

        #region IBeeperDevice

        /// <summary>
        /// Processes an incoming pulse value and adds it to the blipbuffer
        /// </summary>
        /// <param name="pulse"></param>
        public void ProcessPulseValue(bool pulse)
        {
            if (!_machine._renderSound)
                return;

            if (LastPulse == pulse)
            {
                // no change
                blip.AddDelta((uint)_machine.CurrentFrameCycle, 0);
            }
                
            else
            {
                if (pulse)
                    blip.AddDelta((uint)_machine.CurrentFrameCycle, (short)(_volume));
                else
                    blip.AddDelta((uint)_machine.CurrentFrameCycle, -(short)(_volume));

                lastVolume = _volume;
            }

            LastPulse = pulse;            
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
            blip.Clear();
        }

        public void GetSamplesSync(out short[] samples, out int nsamp)
        {
            blip.EndFrame((uint)_tStatesPerFrame);
            nsamp = blip.SamplesAvailable();
            samples = new short[nsamp * 2];
            blip.ReadSamples(samples, nsamp, true);
            for (int i = 0; i < nsamp * 2; i += 2)
            {
                samples[i + 1] = samples[i];
            }
        }

        #endregion

        #region State Serialization

        public void SyncState(Serializer ser)
        {
            ser.BeginSection("Buzzer");
            ser.Sync("_tStatesPerFrame", ref _tStatesPerFrame);
            ser.Sync("_sampleRate", ref _sampleRate);
            ser.Sync("LastPulse", ref LastPulse);
            ser.Sync("LastPulseTState", ref LastPulseTState);
            ser.EndSection();
        }

        #endregion
    }
}
