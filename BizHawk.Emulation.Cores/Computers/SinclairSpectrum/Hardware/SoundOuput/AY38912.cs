using BizHawk.Common;
using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// AY-3-8912 Emulated Device
    /// 
    /// Based heavily on the YM-2149F / AY-3-8910 emulator used in Unreal Speccy
    /// (Originally created under Public Domain license by SMT jan.2006)
    /// 
    /// https://github.com/mkoloberdin/unrealspeccy/blob/master/sndrender/sndchip.cpp
    /// https://github.com/mkoloberdin/unrealspeccy/blob/master/sndrender/sndchip.h
    /// </summary>
    public class AY38912 : IPSG
    {
        #region Device Fields

        /// <summary>
        /// The emulated machine (passed in via constructor)
        /// </summary>
        private SpectrumBase _machine;

        private int _tStatesPerFrame;
        private int _sampleRate;
        private int _samplesPerFrame;
        private int _tStatesPerSample;
        private short[] _audioBuffer;
        private int _audioBufferIndex;
        private int _lastStateRendered;

        #endregion

        #region Construction & Initialization

        /// <summary>
        /// Main constructor
        /// </summary>
        public AY38912(SpectrumBase machine)
        {
            _machine = machine;
        }

        /// <summary>
        /// Initialises the AY chip
        /// </summary>
        public void Init(int sampleRate, int tStatesPerFrame)
        {
            InitTiming(sampleRate, tStatesPerFrame);
            UpdateVolume();
            Reset();
        }

        #endregion

        #region IPortIODevice

        public bool ReadPort(ushort port, ref int value)
        {
            if (port != 0xfffd)
            {
                // port read is not addressing this device
                return false;
            }

            value = PortRead();

            return true;
        }

        public bool WritePort(ushort port, int value)
        {
            if (port == 0xfffd)
            {
                // register select
                SelectedRegister = value & 0x0f;
                return true;
            }
            else if (port == 0xbffd)
            {
                // Update the audiobuffer based on the current CPU cycle
                // (this process the previous data BEFORE writing to the currently selected register)                
                int d = (int)(_machine.CurrentFrameCycle);
                BufferUpdate(d);

                // write to register
                PortWrite(value);
                return true;
            }
            return false;
        }

        #endregion

        #region AY Implementation

        #region Public Properties

        /// <summary>
        /// AY mixer panning configuration
        /// </summary>
        [Flags]
        public enum AYPanConfig
        {
            MONO = 0,
            ABC = 1,
            ACB = 2,
            BAC = 3,
            BCA = 4,
            CAB = 5,
            CBA = 6,
        }

        /// <summary>
        /// The AY panning configuration
        /// </summary>
        public AYPanConfig PanningConfiguration
        {
            get
            {
                return _currentPanTab;
            }
            set
            {
                if (value != _currentPanTab)
                {
                    _currentPanTab = value;
                    UpdateVolume();
                }
            }
        }

        /// <summary>
        /// The AY chip output volume
        /// (0 - 100)
        /// </summary>
        public int Volume
        {
            get
            {
                return _volume;
            }
            set
            {
                //value = Math.Max(0, value);
                //value = Math.Max(100, value);
                if (_volume == value)
                {
                    return;
                }
                _volume = value;
                UpdateVolume();
            }
        }

        /// <summary>
        /// The currently selected register
        /// </summary>
        public int SelectedRegister
        {
            get { return _activeRegister; }
            set
            {
                _activeRegister = (byte)value;
            }
        }

        /// <summary>
        /// Used for snapshot generation
        /// </summary>
        /// <returns></returns>
        public int[] ExportRegisters()
        {
            return _registers;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Resets the PSG
        /// </summary>
        public void Reset()
        {
            for (int i = 0; i < 16; i++)
            {
                if (i == 6)
                    _registers[i] = 0xff;
                else
                    _registers[i] = 0;
            }

            /*
            _noiseVal = 0x0FFFF;
            _outABC = 0;
            _outNoiseABC = 0;
            _counterNoise = 0;
            _counterA = 0;
            _counterB = 0;
            _counterC = 0;
            _EnvelopeCounterBend = 0;

            // clear all the registers
            for (int i = 0; i < 14; i++)
            {
                SelectedRegister = i;
                PortWrite(0);
            }

            randomSeed = 1;

            // number of frames to update
            var fr = (_audioBufferIndex * _tStatesPerFrame) / _audioBuffer.Length;

            // update the audio buffer
            BufferUpdate(fr);
            */
        }

        /// <summary>
        /// Reads the value from the currently selected register
        /// </summary>
        /// <returns></returns>
        public int PortRead()
        {
            return _registers[_activeRegister];
        }

        /// <summary>
        /// Writes to the currently selected register
        /// </summary>
        /// <param name="value"></param>
        public void PortWrite(int value)
        {
            if (_activeRegister >= 0x10)
                return;

            byte val = (byte)value;

            if (((1 << _activeRegister) & ((1 << 1) | (1 << 3) | (1 << 5) | (1 << 13))) != 0)
                val &= 0x0F;

            if (((1 << _activeRegister) & ((1 << 6) | (1 << 8) | (1 << 9) | (1 << 10))) != 0)
                val &= 0x1F;

            if (_activeRegister != 13 && _registers[_activeRegister] == val)
                return;

            _registers[_activeRegister] = val;

            switch (_activeRegister)
            {
                // Channel A (Combined Pitch)
                // (not written to directly)
                case 0:
                case 1:
                    _dividerA = _registers[AY_A_FINE] | (_registers[AY_A_COARSE] << 8);
                    break;
                // Channel B (Combined Pitch)
                // (not written to directly)
                case 2:
                case 3:
                    _dividerB = _registers[AY_B_FINE] | (_registers[AY_B_COARSE] << 8);
                    break;
                // Channel C (Combined Pitch)
                // (not written to directly)
                case 4:
                case 5:
                    _dividerC = _registers[AY_C_FINE] | (_registers[AY_C_COARSE] << 8);
                    break;
                // Noise Pitch
                case 6:
                    _dividerN = val * 2;
                    break;
                // Mixer
                case 7:
                    _bit0 = 0 - ((val >> 0) & 1);
                    _bit1 = 0 - ((val >> 1) & 1);
                    _bit2 = 0 - ((val >> 2) & 1);
                    _bit3 = 0 - ((val >> 3) & 1);
                    _bit4 = 0 - ((val >> 4) & 1);
                    _bit5 = 0 - ((val >> 5) & 1);
                    break;
                // Channel Volumes
                case 8:
                    _eMaskA = (val & 0x10) != 0 ? -1 : 0;
                    _vA = ((val & 0x0F) * 2 + 1) & ~_eMaskA;
                    break;
                case 9:
                    _eMaskB = (val & 0x10) != 0 ? -1 : 0;
                    _vB = ((val & 0x0F) * 2 + 1) & ~_eMaskB;
                    break;
                case 10:
                    _eMaskC = (val & 0x10) != 0 ? -1 : 0;
                    _vC = ((val & 0x0F) * 2 + 1) & ~_eMaskC;
                    break;
                // Envelope (Combined Duration)
                // (not written to directly)
                case 11:
                case 12:
                    _dividerE = _registers[AY_E_FINE] | (_registers[AY_E_COARSE] << 8);
                    break;
                // Envelope Shape
                case 13:
                    // reset the envelope counter
                    _countE = 0;

                    if ((_registers[AY_E_SHAPE] & 4) != 0)
                    {
                        // attack
                        _eState = 0;
                        _eDirection = 1;
                    }
                    else
                    {
                        // decay
                        _eState = 31;
                        _eDirection = -1;
                    }
                    break;
                case 14:
                    // IO Port - not implemented
                    break;
            }
        }

        /// <summary>
        /// Start of frame
        /// </summary>
        public void StartFrame()
        {
            _audioBufferIndex = 0;
            BufferUpdate(0);
        }

        /// <summary>
        /// End of frame
        /// </summary>
        public void EndFrame()
        {
            BufferUpdate(_tStatesPerFrame);
        }

        /// <summary>
        /// Updates the audiobuffer based on the current frame t-state
        /// </summary>
        /// <param name="frameCycle"></param>
        public void UpdateSound(int frameCycle)
        {
            BufferUpdate(frameCycle);
        }

        #endregion

        #region Private Fields

        /// <summary>
        /// Register indicies
        /// </summary>
        private const int AY_A_FINE = 0;
        private const int AY_A_COARSE = 1;
        private const int AY_B_FINE = 2;
        private const int AY_B_COARSE = 3;
        private const int AY_C_FINE = 4;
        private const int AY_C_COARSE = 5;
        private const int AY_NOISEPITCH = 6;
        private const int AY_MIXER = 7;
        private const int AY_A_VOL = 8;
        private const int AY_B_VOL = 9;
        private const int AY_C_VOL = 10;
        private const int AY_E_FINE = 11;
        private const int AY_E_COARSE = 12;
        private const int AY_E_SHAPE = 13;
        private const int AY_PORT_A = 14;
        private const int AY_PORT_B = 15;

        /// <summary>
        /// The register array
        /*
            The AY-3-8910/8912 contains 16 internal registers as follows:

            Register    Function	                Range
            0	        Channel A fine pitch	    8-bit (0-255)
            1	        Channel A course pitch	    4-bit (0-15)
            2	        Channel B fine pitch	    8-bit (0-255)
            3	        Channel B course pitch	    4-bit (0-15)
            4	        Channel C fine pitch	    8-bit (0-255)
            5	        Channel C course pitch	    4-bit (0-15)
            6	        Noise pitch	                5-bit (0-31)
            7	        Mixer	                    8-bit (see below)
            8	        Channel A volume	        4-bit (0-15, see below)
            9	        Channel B volume	        4-bit (0-15, see below)
            10	        Channel C volume	        4-bit (0-15, see below)
            11	        Envelope fine duration	    8-bit (0-255)
            12	        Envelope course duration	8-bit (0-255)
            13	        Envelope shape	            4-bit (0-15)
            14	        I/O port A	                8-bit (0-255)
            15	        I/O port B	                8-bit (0-255) (Not present on the AY-3-8912)

            * The volume registers (8, 9 and 10) contain a 4-bit setting but if bit 5 is set then that channel uses the 
                envelope defined by register 13 and ignores its volume setting.
            * The mixer (register 7) is made up of the following bits (low=enabled):
            
            Bit:        7	    6	    5	    4	    3	    2	    1	    0
            Register:   I/O	    I/O	    Noise	Noise	Noise	Tone	Tone	Tone
            Channel:    B       A	    C	    B	    A	    C	    B	    A

            The AY-3-8912 ignores bit 7 of this register.    
        */
        /// </summary>
        private int[] _registers = new int[16];

        /// <summary>
        /// The currently selected register
        /// </summary>
        private byte _activeRegister;

        /// <summary>
        /// The frequency of the AY chip
        /// </summary>
        private static int _chipFrequency = 1773400;

        /// <summary>
        /// The rendering resolution of the chip
        /// </summary>
        private double _resolution = 50D * 8D / _chipFrequency;

        /// <summary>
        /// Channel generator state
        /// </summary>
        private int _bitA;
        private int _bitB;
        private int _bitC;

        /// <summary>
        /// Envelope state
        /// </summary>
        private int _eState;

        /// <summary>
        /// Envelope direction
        /// </summary>
        private int _eDirection;

        /// <summary>
        /// Noise seed
        /// </summary>
        private int _noiseSeed;

        /// <summary>
        /// Mixer state
        /// </summary>
        private int _bit0;
        private int _bit1;
        private int _bit2;
        private int _bit3;
        private int _bit4;
        private int _bit5;

        /// <summary>
        /// Noise generator state
        /// </summary>
        private int _bitN;

        /// <summary>
        /// Envelope masks
        /// </summary>
        private int _eMaskA;
        private int _eMaskB;
        private int _eMaskC;

        /// <summary>
        /// Amplitudes
        /// </summary>
        private int _vA;
        private int _vB;
        private int _vC;

        /// <summary>
        /// Channel gen counters
        /// </summary>
        private int _countA;
        private int _countB;
        private int _countC;

        /// <summary>
        /// Envelope gen counter
        /// </summary>
        private int _countE;

        /// <summary>
        /// Noise gen counter
        /// </summary>
        private int _countN;

        /// <summary>
        /// Channel gen dividers
        /// </summary>
        private int _dividerA;
        private int _dividerB;
        private int _dividerC;

        /// <summary>
        ///  Envelope gen divider
        /// </summary>
        private int _dividerE;

        /// <summary>
        /// Noise gen divider
        /// </summary>
        private int _dividerN;

        /// <summary>
        /// Panning table list
        /// </summary>
        private static List<uint[]> PanTabs = new List<uint[]>
        {
            // MONO
            new uint[] { 50,50, 50,50, 50,50 },
            // ABC
            new uint[] { 100,10,  66,66,   10,100 },
            // ACB
            new uint[] { 100,10,  10,100,  66,66 },
            // BAC
            new uint[] { 66,66,   100,10,  10,100 },
            // BCA
            new uint[] { 10,100,  100,10,  66,66 },
            // CAB
            new uint[] { 66,66,   10,100,  100,10 },
            // CBA
            new uint[] { 10,100,  66,66,   100,10 }
        };

        /// <summary>
        /// The currently selected panning configuration
        /// </summary>
        private AYPanConfig _currentPanTab = AYPanConfig.ABC;

        /// <summary>
        /// The current volume
        /// </summary>
        private int _volume = 75;

        /// <summary>
        /// Volume tables state
        /// </summary>
        private uint[][] _volumeTables;

        /// <summary>
        /// Volume table to be used
        /// </summary>
        private static uint[] AYVolumes = new uint[]
        {
            0x0000,0x0000,0x0340,0x0340,0x04C0,0x04C0,0x06F2,0x06F2,
            0x0A44,0x0A44,0x0F13,0x0F13,0x1510,0x1510,0x227E,0x227E,
            0x289F,0x289F,0x414E,0x414E,0x5B21,0x5B21,0x7258,0x7258,
            0x905E,0x905E,0xB550,0xB550,0xD7A0,0xD7A0,0xFFFF,0xFFFF,
        };

        #endregion

        #region Private Methods

        /// <summary>
        /// Forces an update of the volume tables
        /// </summary>
        private void UpdateVolume()
        {
            int upperFloor = 40000;
            var inc = (0xFFFF - upperFloor) / 100;

            var vol = inc * _volume; // ((ulong)0xFFFF * (ulong)_volume / 100UL) - 20000 ;
            _volumeTables = new uint[6][];

            // parent array
            for (int j = 0; j < _volumeTables.Length; j++)
            {
                _volumeTables[j] = new uint[32];

                // child array
                for (int i = 0; i < _volumeTables[j].Length; i++)
                {
                    _volumeTables[j][i] = (uint)(
                        (PanTabs[(int)_currentPanTab][j] * AYVolumes[i] * vol) /
                        (3 * 65535 * 100));
                }
            }
        }

        private int mult_const;

        /// <summary>
        /// Initializes timing information for the frame
        /// </summary>
        /// <param name="sampleRate"></param>
        /// <param name="frameTactCount"></param>
        private void InitTiming(int sampleRate, int frameTactCount)
        {
            _sampleRate = sampleRate;
            _tStatesPerFrame = frameTactCount;
            _samplesPerFrame = 882;

            _tStatesPerSample = 79; //(int)Math.Round(((double)_tStatesPerFrame * 50D) / 
                                    //(16D * (double)_sampleRate),
                                    //MidpointRounding.AwayFromZero);

            //_samplesPerFrame = _tStatesPerFrame / _tStatesPerSample;
            _audioBuffer = new short[_samplesPerFrame * 2]; //[_sampleRate / 50];
            _audioBufferIndex = 0;

            mult_const = ((_chipFrequency / 8) << 14) / _machine.ULADevice.ClockSpeed;

            var aytickspercputick = (double)_machine.ULADevice.ClockSpeed / (double)_chipFrequency;
            int ayCyclesPerSample = (int)((double)_tStatesPerSample * (double)aytickspercputick);
        }

        /// <summary>
        /// Updates the audiobuffer based on the current frame t-state
        /// </summary>
        /// <param name="cycle"></param>
        private void BufferUpdate(int cycle)
        {
            if (cycle > _tStatesPerFrame)
            {
                // we are outside of the frame - just process the last value
                cycle = _tStatesPerFrame;
            }

            // get the current length of the audiobuffer
            int bufferLength = _samplesPerFrame; // _audioBuffer.Length;

            int toEnd = ((bufferLength * cycle) / _tStatesPerFrame);

            // loop through the number of samples we need to render
            while (_audioBufferIndex < toEnd)
            {
                // run the AY chip processing at the correct resolution
                for (int i = 0; i < _tStatesPerSample / 14; i++)
                {
                    if (++_countA >= _dividerA)
                    {
                        _countA = 0;
                        _bitA ^= -1;
                    }

                    if (++_countB >= _dividerB)
                    {
                        _countB = 0;
                        _bitB ^= -1;
                    }

                    if (++_countC >= _dividerC)
                    {
                        _countC = 0;
                        _bitC ^= -1;
                    }

                    if (++_countN >= _dividerN)
                    {
                        _countN = 0;
                        _noiseSeed = (_noiseSeed * 2 + 1) ^ (((_noiseSeed >> 16) ^ (_noiseSeed >> 13)) & 1);
                        _bitN = 0 - ((_noiseSeed >> 16) & 1);
                    }

                    if (++_countE >= _dividerE)
                    {
                        _countE = 0;
                        _eState += +_eDirection;

                        if ((_eState & ~31) != 0)
                        {
                            var mask = (1 << _registers[AY_E_SHAPE]);

                            if ((mask & ((1 << 0) | (1 << 1) | (1 << 2) |
                                (1 << 3) | (1 << 4) | (1 << 5) | (1 << 6) |
                                (1 << 7) | (1 << 9) | (1 << 15))) != 0)
                            {
                                _eState = _eDirection = 0;
                            }
                            else if ((mask & ((1 << 8) | (1 << 12))) != 0)
                            {
                                _eState &= 31;
                            }
                            else if ((mask & ((1 << 10) | (1 << 14))) != 0)
                            {
                                _eDirection = -_eDirection;
                                _eState += _eDirection;
                            }
                            else
                            {
                                // 11,13
                                _eState = 31;
                                _eDirection = 0;
                            }
                        }
                    }
                }

                // mix the sample
                var mixA = ((_eMaskA & _eState) | _vA) & ((_bitA | _bit0) & (_bitN | _bit3));
                var mixB = ((_eMaskB & _eState) | _vB) & ((_bitB | _bit1) & (_bitN | _bit4));
                var mixC = ((_eMaskC & _eState) | _vC) & ((_bitC | _bit2) & (_bitN | _bit5));

                var l = _volumeTables[0][mixA];
                var r = _volumeTables[1][mixA];

                l += _volumeTables[2][mixB];
                r += _volumeTables[3][mixB];
                l += _volumeTables[4][mixC];
                r += _volumeTables[5][mixC];

                _audioBuffer[_audioBufferIndex * 2] = (short)l;
                _audioBuffer[(_audioBufferIndex * 2) + 1] = (short)r;

                _audioBufferIndex++;
            }

            _lastStateRendered = cycle;
        }

        #endregion

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
            _audioBuffer = new short[_samplesPerFrame * 2];
        }

        public void GetSamplesSync(out short[] samples, out int nsamp)
        {
            nsamp = _samplesPerFrame;
            samples = _audioBuffer;
            DiscardSamples();
        }

        #endregion

        #region State Serialization

        public int nullDump = 0;

        /// <summary>
        /// State serialization
        /// </summary>
        /// <param name="ser"></param>
        public void SyncState(Serializer ser)
        {
            ser.BeginSection("PSG-AY");

            ser.Sync("_tStatesPerFrame", ref _tStatesPerFrame);
            ser.Sync("_sampleRate", ref _sampleRate);
            ser.Sync("_samplesPerFrame", ref _samplesPerFrame);
            ser.Sync("_tStatesPerSample", ref _tStatesPerSample);
            ser.Sync("_audioBufferIndex", ref _audioBufferIndex);
            ser.Sync("_audioBuffer", ref _audioBuffer, false);

            ser.Sync("_registers", ref _registers, false);
            ser.Sync("_activeRegister", ref _activeRegister);
            ser.Sync("_bitA", ref _bitA);
            ser.Sync("_bitB", ref _bitB);
            ser.Sync("_bitC", ref _bitC);
            ser.Sync("_eState", ref _eState);
            ser.Sync("_eDirection", ref _eDirection);
            ser.Sync("_noiseSeed", ref _noiseSeed);
            ser.Sync("_bit0", ref _bit0);
            ser.Sync("_bit1", ref _bit1);
            ser.Sync("_bit2", ref _bit2);
            ser.Sync("_bit3", ref _bit3);
            ser.Sync("_bit4", ref _bit4);
            ser.Sync("_bit5", ref _bit5);
            ser.Sync("_bitN", ref _bitN);
            ser.Sync("_eMaskA", ref _eMaskA);
            ser.Sync("_eMaskB", ref _eMaskB);
            ser.Sync("_eMaskC", ref _eMaskC);
            ser.Sync("_vA", ref _vA);
            ser.Sync("_vB", ref _vB);
            ser.Sync("_vC", ref _vC);
            ser.Sync("_countA", ref _countA);
            ser.Sync("_countB", ref _countB);
            ser.Sync("_countC", ref _countC);
            ser.Sync("_countE", ref _countE);
            ser.Sync("_countN", ref _countN);
            ser.Sync("_dividerA", ref _dividerA);
            ser.Sync("_dividerB", ref _dividerB);
            ser.Sync("_dividerC", ref _dividerC);
            ser.Sync("_dividerE", ref _dividerE);
            ser.Sync("_dividerN", ref _dividerN);
            ser.SyncEnum("_currentPanTab", ref _currentPanTab);
            ser.Sync("_volume", ref nullDump);

            for (int i = 0; i < 6; i++)
            {
                ser.Sync("volTable" + i, ref _volumeTables[i], false);
            }

            if (ser.IsReader)
                _volume = _machine.Spectrum.Settings.AYVolume;

            ser.EndSection();
        }

        #endregion
    }
}
