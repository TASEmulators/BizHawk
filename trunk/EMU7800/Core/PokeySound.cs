/*
 * PokeySound.cs
 *
 * Emulation of the audio features of the Atari Pot Keyboard Integrated Circuit (POKEY, C012294).
 *
 * Implementation inspired by prior works of Greg Stanton (ProSystem Emulator) and Ron Fries.
 *
 * Copyright © 2012 Mike Murphy
 *
 */
using System;

namespace EMU7800.Core
{
    public sealed class PokeySound
    {
        #region Constants and Tables

        const int
            AUDF1             = 0x00, // write reg: channel 1 frequency
            AUDC1             = 0x01, // write reg: channel 1 generator
            AUDF2             = 0x02, // write reg: channel 2 frequency
            AUDC2             = 0x03, // write reg: channel 2 generator
            AUDF3             = 0x04, // write reg: channel 3 frequency
            AUDC3             = 0x05, // write reg: channel 3 generator
            AUDF4             = 0x06, // write reg: channel 4 frequency
            AUDC4             = 0x07, // write reg: channel 4 generator
            AUDCTL            = 0x08, // write reg: control over audio channels
            SKCTL             = 0x0f, // write reg: control over serial port
            RANDOM            = 0x0a; // read reg: random number generator value

        const int
            AUDCTL_POLY9      = 0x80, // make 17-bit poly counter into a 9-bit poly counter
            AUDCTL_CH1_179    = 0x40, // clocks channel 1 with 1.79 MHz, instead of 64 kHz
            AUDCTL_CH3_179    = 0x20, // clocks channel 3 with 1.79 MHz, instead of 64 kHz
            AUDCTL_CH1_CH2    = 0x10, // clock channel 2 with channel 1, instead of 64 kHz (16-bit)
            AUDCTL_CH3_CH4    = 0x08, // clock channel 4 with channel 3, instead of 64 kHz (16-bit)
            AUDCTL_CH1_FILTER = 0x04, // inserts high-pass filter into channel 1, clocked by channel 3
            AUDCTL_CH2_FILTER = 0x02, // inserts high-pass filter into channel 2, clocked by channel 4
            AUDCTL_CLOCK_15   = 0x01; // change normal clock base from 64 kHz to 15 kHz

        const int
            AUDC_NOTPOLY5     = 0x80,
            AUDC_POLY4        = 0x40,
            AUDC_PURE         = 0x20,
            AUDC_VOLUME_ONLY  = 0x10,
            AUDC_VOLUME_MASK  = 0x0f;

        const int
            DIV_64            = 28,
            DIV_15            = 114,
            POLY9_SIZE        = 0x01ff,
            POLY17_SIZE       = 0x0001ffff,
            POKEY_FREQ        = 1787520,
            SKCTL_RESET       = 3;

        const int CPU_TICKS_PER_AUDIO_SAMPLE = 57;

        readonly byte[] _poly04 = { 1, 1, 0, 1, 1, 1, 0, 0, 0, 0, 1, 0, 1, 0, 0 };
        readonly byte[] _poly05 = { 0, 0, 1, 1, 0, 0, 0, 1, 1, 1, 1, 0, 0, 1, 0, 1, 0, 1, 1, 0, 1, 1, 1, 0, 1, 0, 0, 0, 0, 0, 1 };
        readonly byte[] _poly17 = new byte[POLY9_SIZE]; // should be POLY17_SIZE, but instead wrapping around to conserve storage

        readonly Random _random = new Random();

        #endregion

        #region Object State

        readonly MachineBase M;

        readonly int _pokeyTicksPerSample;
        int _pokeyTicks;

        ulong _lastUpdateCpuClock;
        int _bufferIndex;

        readonly byte[] _audf = new byte[4];
        readonly byte[] _audc = new byte[4];
        byte _audctl, _skctl;

        int _baseMultiplier;
        int _poly04Counter;
        int _poly05Counter;
        int _poly17Counter, _poly17Size;

        readonly int[] _divideMax = new int[4];
        readonly int[] _divideCount = new int[4];
        readonly byte[] _output = new byte[4];
        readonly byte[] _outvol = new byte[4];

        #endregion

        #region Public Members

        public void Reset()
        {
            _poly04Counter = _poly05Counter = _poly17Counter = _audctl = _skctl = 0;

            _baseMultiplier = DIV_64;
            _poly17Size = POLY17_SIZE;

            _pokeyTicks = 0;

            for (var ch = 0; ch < 4; ch++)
            {
                _outvol[ch] = _output[ch] = _audc[ch] = _audf[ch] = 0;
                _divideCount[ch] = Int32.MaxValue;
                _divideMax[ch] = Int32.MaxValue;
            }
        }

        public void StartFrame()
        {
            _lastUpdateCpuClock = M.CPU.Clock;
            _bufferIndex = 0;
        }

        public void EndFrame()
        {
            RenderSamples(M.FrameBuffer.SoundBufferByteLength - _bufferIndex);
        }

        public byte Read(ushort addr)
        {
            addr &= 0xf;

            switch (addr)
            {
                // If the 2 least significant bits of SKCTL are 0, the random number generator is disabled (return all 1s.)
                // Ballblazer music relies on this.
                case RANDOM:
                    return (_skctl & SKCTL_RESET) == 0 ? (byte)0xff : (byte)_random.Next(0xff);
                default:
                    return 0;
            }
        }

        public void Update(ushort addr, byte data)
        {
            if (M.CPU.Clock > _lastUpdateCpuClock)
            {
                var updCpuClocks = (int)(M.CPU.Clock - _lastUpdateCpuClock);
                var samples = updCpuClocks / CPU_TICKS_PER_AUDIO_SAMPLE;
                RenderSamples(samples);
                _lastUpdateCpuClock += (ulong)(samples * CPU_TICKS_PER_AUDIO_SAMPLE);
            }

            addr &= 0xf;

            switch (addr)
            {
                case AUDF1:
                    _audf[0] = data;
                    ResetChannel1();
                    if ((_audctl & AUDCTL_CH1_CH2) != 0)
                        ResetChannel2();
                    break;
                case AUDC1:
                    _audc[0] = data;
                    ResetChannel1();
                    break;
                case AUDF2:
                    _audf[1] = data;
                    ResetChannel2();
                    break;
                case AUDC2:
                    _audc[1] = data;
                    ResetChannel2();
                    break;
                case AUDF3:
                    _audf[2] = data;
                    ResetChannel3();
                    if ((_audctl & AUDCTL_CH3_CH4) != 0)
                        ResetChannel4();
                    break;
                case AUDC3:
                    _audc[2] = data;
                    ResetChannel3();
                    break;
                case AUDF4:
                    _audf[3] = data;
                    ResetChannel4();
                    break;
                case AUDC4:
                    _audc[3] = data;
                    ResetChannel4();
                    break;
                case AUDCTL:
                    _audctl = data;
                    _poly17Size = ((_audctl & AUDCTL_POLY9) != 0) ? POLY9_SIZE : POLY17_SIZE;
                    _baseMultiplier = ((_audctl & AUDCTL_CLOCK_15) != 0) ? DIV_15 : DIV_64;
                    ResetChannel1();
                    ResetChannel2();
                    ResetChannel3();
                    ResetChannel4();
                    break;
                case SKCTL:
                    _skctl = data;
                    break;
            }
        }

        #endregion

        #region Constructors

        private PokeySound()
        {
            _random.NextBytes(_poly17);
            for (var i = 0; i < _poly17.Length; i++)
                _poly17[i] &= 0x01;

            Reset();
        }

        public PokeySound(MachineBase m) : this()
        {
            if (m == null)
                throw new ArgumentNullException("m");

            M = m;

            // Add 8-bits of fractional representation to reduce distortion on output
            _pokeyTicksPerSample = (POKEY_FREQ << 8) / M.SoundSampleFrequency;
        }

        #endregion

        #region Serialization Members

        public PokeySound(DeserializationContext input, MachineBase m) : this(m)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            input.CheckVersion(1);
            _lastUpdateCpuClock = input.ReadUInt64();
            _bufferIndex = input.ReadInt32();
            _audf = input.ReadBytes();
            _audc = input.ReadBytes();
            _audctl = input.ReadByte();
            _skctl = input.ReadByte();
            _output = input.ReadBytes();
            _outvol = input.ReadBytes();
            _divideMax = input.ReadIntegers(4);
            _divideCount = input.ReadIntegers(4);
            _pokeyTicks = input.ReadInt32();
            _pokeyTicksPerSample = input.ReadInt32();
            _baseMultiplier = input.ReadInt32();
            _poly04Counter = input.ReadInt32();
            _poly05Counter = input.ReadInt32();
            _poly17Counter = input.ReadInt32();
            _poly17Size = input.ReadInt32();
        }

        public void GetObjectData(SerializationContext output)
        {
            if (output == null)
                throw new ArgumentNullException("output");

            output.WriteVersion(1);
            output.Write(_lastUpdateCpuClock);
            output.Write(_bufferIndex);
            output.Write(_audf);
            output.Write(_audc);
            output.Write(_audctl);
            output.Write(_skctl);
            output.Write(_output);
            output.Write(_outvol);
            output.Write(_divideMax);
            output.Write(_divideCount);
            output.Write(_pokeyTicks);
            output.Write(_pokeyTicksPerSample);
            output.Write(_baseMultiplier);
            output.Write(_poly04Counter);
            output.Write(_poly05Counter);
            output.Write(_poly17Counter);
            output.Write(_poly17Size);
        }

        #endregion

        #region Helpers

        void RenderSamples(int count)
        {
            const int POKEY_SAMPLE = 4;
            var poly17Length = (_poly17Size > _poly17.Length ? _poly17.Length : _poly17Size);

            while (count > 0 && _bufferIndex < M.FrameBuffer.SoundBufferByteLength)
            {
                var nextEvent = POKEY_SAMPLE;
                var wholeTicksToConsume = (_pokeyTicks >> 8);

                for (var ch = 0; ch < 4; ch++)
                {
                    if (_divideCount[ch] <= wholeTicksToConsume)
                    {
                        wholeTicksToConsume = _divideCount[ch];
                        nextEvent = ch;
                    }
                }

                for (var ch = 0; ch < 4; ch++)
                    _divideCount[ch] -= wholeTicksToConsume;

                _pokeyTicks -= (wholeTicksToConsume << 8);

                if (nextEvent == POKEY_SAMPLE)
                {
                    _pokeyTicks += _pokeyTicksPerSample;

                    byte sample = 0;
                    for (var ch = 0; ch < 4; ch++)
                        sample += _outvol[ch];

                    M.FrameBuffer.SoundBuffer[_bufferIndex >> BufferElement.SHIFT][_bufferIndex++] += sample;
                    count--;

                    continue;
                }

                _divideCount[nextEvent] += _divideMax[nextEvent];

                _poly04Counter += wholeTicksToConsume;
                _poly04Counter %= _poly04.Length;

                _poly05Counter += wholeTicksToConsume;
                _poly05Counter %= _poly05.Length;

                _poly17Counter += wholeTicksToConsume;
                _poly17Counter %= poly17Length;

                if ((_audc[nextEvent] & AUDC_NOTPOLY5) != 0 || _poly05[_poly05Counter] != 0)
                {
                    if ((_audc[nextEvent] & AUDC_PURE) != 0)
                        _output[nextEvent] ^= 1;
                    else if ((_audc[nextEvent] & AUDC_POLY4) != 0)
                        _output[nextEvent] = _poly04[_poly04Counter];
                    else
                        _output[nextEvent] = _poly17[_poly17Counter];
                }

                _outvol[nextEvent] = (_output[nextEvent] != 0) ? (byte)(_audc[nextEvent] & AUDC_VOLUME_MASK) : (byte)0;
            }
        }

        // As defined in the manual, the exact divider values are different depending on the frequency and resolution:
        //     64 kHz or 15 kHz     AUDF + 1
        //      1 MHz, 8-bit        AUDF + 4
        //      1 MHz, 16-bit       AUDF[CHAN1] + 256 * AUDF[CHAN2] + 7

        void ResetChannel1()
        {
            var val = ((_audctl & AUDCTL_CH1_179) != 0) ? (_audf[0] + 4) : ((_audf[0] + 1) * _baseMultiplier);
            if (val != _divideMax[0])
            {
                _divideMax[0] = val;
                if (val < _divideCount[0])
                    _divideCount[0] = val;
            }
            UpdateVolumeSettingsForChannel(0);
        }

        void ResetChannel2()
        {
            int val;
            if ((_audctl & AUDCTL_CH1_CH2) != 0)
            {
                val = ((_audctl & AUDCTL_CH1_179) != 0) ? (_audf[1] * 256 + _audf[0] + 7) : ((_audf[1] * 256 + _audf[0] + 1) * _baseMultiplier);
            }
            else
            {
                val = ((_audf[1] + 1) * _baseMultiplier);
            }
            if (val != _divideMax[1])
            {
                _divideMax[1] = val;
                if (val < _divideCount[1])
                    _divideCount[1] = val;
            }
            UpdateVolumeSettingsForChannel(1);
        }

        void ResetChannel3()
        {
            var val = ((_audctl & AUDCTL_CH3_179) != 0) ? (_audf[2] + 4) : ((_audf[2] + 1) * _baseMultiplier);
            if (val != _divideMax[2])
            {
                _divideMax[2] = val;
                if (val < _divideCount[2])
                    _divideCount[2] = val;
            }
            UpdateVolumeSettingsForChannel(2);
        }

        void ResetChannel4()
        {
            int val;
            if ((_audctl & AUDCTL_CH3_CH4) != 0)
            {
                val = ((_audctl & AUDCTL_CH3_179) != 0) ? (_audf[3] * 256 + _audf[2] + 7) : ((_audf[3] * 256 + _audf[2] + 1) * _baseMultiplier);
            }
            else
            {
                val = ((_audf[3] + 1) * _baseMultiplier);
            }
            if (val != _divideMax[3])
            {
                _divideMax[3] = val;
                if (val < _divideCount[3])
                    _divideCount[3] = val;
            }
            UpdateVolumeSettingsForChannel(3);
        }

        void UpdateVolumeSettingsForChannel(int ch)
        {
            if (((_audc[ch] & AUDC_VOLUME_ONLY) != 0) || ((_audc[ch] & AUDC_VOLUME_MASK) == 0) || (_divideMax[ch] < (_pokeyTicksPerSample >> 8)))
            {
                _outvol[ch] = (byte)(_audc[ch] & AUDC_VOLUME_MASK);
                _divideCount[ch] = Int32.MaxValue;
                _divideMax[ch] = Int32.MaxValue;
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        void LogDebug(string format, params object[] args)
        {
            if (M == null || M.Logger == null)
                return;
            M.Logger.WriteLine(format, args);
        }

        #endregion
    }
}
