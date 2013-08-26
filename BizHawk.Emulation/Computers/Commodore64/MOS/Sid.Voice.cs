using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
    sealed public partial class Sid
    {
        sealed class Voice
        {
            int accBits;
            int accNext;
            int accumulator;
            bool controlTestPrev;
            int controlWavePrev;
            int delay;
            int floatOutputTTL;
            int frequency;
            bool msbRising;
            int noise;
            int noNoise;
            int noNoiseOrNoise;
            int noPulse;
            int output;
            int pulse;
            int pulseWidth;
            bool ringMod;
            int ringMsbMask;
            int shiftRegister;
            int shiftRegisterReset;
            bool sync;
            bool test;
            int[] wave;
            int waveform;
            int waveformIndex;
            int[][] waveTable;

            public Voice(int[][] newWaveTable)
            {
                waveTable = newWaveTable;
                HardReset();
            }

            public void HardReset()
            {
                accumulator = 0;
                delay = 0;
                floatOutputTTL = 0;
                frequency = 0;
                msbRising = false;
                noNoise = 0xFFF;
                noPulse = 0xFFF;
                output = 0x000;
                pulse = 0xFFF;
                pulseWidth = 0;
                ringMsbMask = 0;
                sync = false;
                test = false;
                wave = waveTable[0];
                waveform = 0;

                ResetShiftReg();
            }

            public void ExecutePhase2()
            {

                {
                    if (test)
                    {
                        if (shiftRegisterReset != 0 && --shiftRegisterReset == 0)
                        {
                            ResetShiftReg();
                        }
                        pulse = 0xFFF;
                    }
                    else
                    {
                        accNext = (accumulator + frequency) & 0xFFFFFF;
                        accBits = ~accumulator & accNext;
                        accumulator = accNext;
                        msbRising = ((accBits & 0x800000) != 0);

                        if ((accBits & 0x080000) != 0)
                            delay = 2;
                        else if (delay != 0 && --delay == 0)
                            ClockShiftReg();
                    }
                }
            }

            // ------------------------------------

            private void ClockShiftReg()
            {

                {
                    shiftRegister = ((shiftRegister << 1) |
                        (((shiftRegister >> 22) ^ (shiftRegister >> 17)) & 0x1)
                        ) & 0x7FFFFF;
                    SetNoise();
                }
            }

            private void ResetShiftReg()
            {

                {
                    shiftRegister = 0x7FFFFF;
                    shiftRegisterReset = 0;
                    SetNoise();
                }
            }

            private void SetNoise()
            {

                {
                    noise =
                        ((shiftRegister & 0x100000) >> 9) |
                        ((shiftRegister & 0x040000) >> 8) |
                        ((shiftRegister & 0x004000) >> 5) |
                        ((shiftRegister & 0x000800) >> 3) |
                        ((shiftRegister & 0x000200) >> 2) |
                        ((shiftRegister & 0x000020) << 1) |
                        ((shiftRegister & 0x000004) << 3) |
                        ((shiftRegister & 0x000001) << 4);
                    noNoiseOrNoise = noNoise | noise;
                }
            }

            private void WriteShiftReg()
            {

                {
                    output &=
                        0xBB5DA |
                        ((output & 0x800) << 9) |
                        ((output & 0x400) << 8) |
                        ((output & 0x200) << 5) |
                        ((output & 0x100) << 3) |
                        ((output & 0x040) >> 1) |
                        ((output & 0x020) >> 3) |
                        ((output & 0x010) >> 4);
                    noise &= output;
                    noNoiseOrNoise = noNoise | noise;
                }
            }

            // ------------------------------------

            public int Control
            {
                set
                {
                    controlWavePrev = waveform;
                    controlTestPrev = test;

                    sync = ((value & 0x02) != 0);
                    ringMod = ((value & 0x04) != 0);
                    test = ((value & 0x08) != 0);
                    waveform = (value >> 4) & 0x0F;
                    wave = waveTable[waveform & 0x07];
                    ringMsbMask = ((~value >> 5) & (value >> 2) & 0x1) << 23;
                    noNoise = ((waveform & 0x8) != 0) ? 0x000 : 0xFFF;
                    noNoiseOrNoise = noNoise | noise;
                    noPulse = ((waveform & 0x4) != 0) ? 0x000 : 0xFFF;

                    if (!controlTestPrev && test)
                    {
                        accumulator = 0;
                        delay = 0;
                        shiftRegisterReset = 0x8000;
                    }
                    else if (controlTestPrev && !test)
                    {
                        shiftRegister = ((shiftRegister << 1) |
                            ((~shiftRegister >> 17) & 0x1)
                            ) & 0x7FFFFF;
                        SetNoise();
                    }

                    if (waveform == 0 && controlWavePrev != 0)
                        floatOutputTTL = 0x28000;
                }
            }

            public int Frequency
            {
                get
                {
                    return frequency;
                }
                set
                {
                    frequency = value;
                }
            }

            public int FrequencyLo
            {
                get
                {
                    return (frequency & 0xFF);
                }
                set
                {
                    frequency &= 0xFF00;
                    frequency |= value & 0x00FF;
                }
            }

            public int FrequencyHi
            {
                get
                {
                    return (frequency >> 8);
                }
                set
                {
                    frequency &= 0x00FF;
                    frequency |= (value & 0x00FF) << 8;
                }
            }

            public int Oscillator
            {
                get
                {
                    return output;
                }
            }

            public int Output(Voice ringModSource)
            {

                {
                    if (waveform != 0)
                    {
                        waveformIndex = (accumulator ^ (ringModSource.accumulator & ringMsbMask)) >> 12;
                        output = wave[waveformIndex] & (noPulse | pulse) & noNoiseOrNoise;
                        if (waveform > 8)
                            WriteShiftReg();
                    }
                    else
                    {
                        if (floatOutputTTL != 0 && --floatOutputTTL == 0)
                            output = 0x000;
                    }
                    pulse = ((accumulator >> 12) >= pulseWidth) ? 0xFFF : 0x000;
                    return output;
                }
            }

            public int PulseWidth
            {
                get
                {
                    return pulseWidth;
                }
                set
                {
                    pulseWidth = value;
                }
            }

            public int PulseWidthLo
            {
                get
                {
                    return (pulseWidth & 0xFF);
                }
                set
                {
                    pulseWidth &= 0x0F00;
                    pulseWidth |= value & 0x00FF;
                }
            }

            public int PulseWidthHi
            {
                get
                {
                    return (pulseWidth >> 8);
                }
                set
                {
                    pulseWidth &= 0x00FF;
                    pulseWidth |= (value & 0x000F) << 8;
                }
            }

            public bool RingMod
            {
                get
                {
                    return ringMod;
                }
            }

            public bool Sync
            {
                get
                {
                    return sync;
                }
            }

            public void Synchronize(Voice target, Voice source)
            {
                if (msbRising && target.sync && !(sync && source.msbRising))
                    target.accumulator = 0;
            }

            public bool Test
            {
                get
                {
                    return test;
                }
            }

            public int Waveform
            {
                get
                {
                    return waveform;
                }
            }

            // ------------------------------------

            public void SyncState(Serializer ser)
            {
                SaveState.SyncObject(ser, this);

                if (ser.IsReader)
                    wave = waveTable[waveform];
            }
        }

    }
}
