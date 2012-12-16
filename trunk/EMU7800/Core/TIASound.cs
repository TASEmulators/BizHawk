/*
 * TIASound.cs
 *
 * Sound emulation for the 2600.  Based upon TIASound © 1997 by Ron Fries.
 *
 * Copyright © 2003, 2004 Mike Murphy
 *
 */

/*****************************************************************************/
/*                                                                           */
/*                 License Information and Copyright Notice                  */
/*                 ========================================                  */
/*                                                                           */
/* TiaSound is Copyright(c) 1997 by Ron Fries                                */
/*                                                                           */
/* This library is free software; you can redistribute it and/or modify it   */
/* under the terms of version 2 of the GNU Library General Public License    */
/* as published by the Free Software Foundation.                             */
/*                                                                           */
/* This library is distributed in the hope that it will be useful, but       */
/* WITHOUT ANY WARRANTY; without even the implied warranty of                */
/* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Library */
/* General Public License for more details.                                  */
/* To obtain a copy of the GNU Library General Public License, write to the  */
/* Free Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.   */
/*                                                                           */
/* Any permitted reproduction of these routines, in whole or in part, must   */
/* bear this legend.                                                         */
/*                                                                           */
/*****************************************************************************/

using System;

namespace EMU7800.Core
{
    public sealed class TIASound
    {
        #region Constants and Tables

        //                                   Clock Source   Clock Modifier   Source Pattern
        const int
            SET_TO_1      = 0x00,  //  0 0 0 0   3.58 Mhz/114   none (pure)      none
            //POLY4       = 0x01,  //  0 0 0 1   3.58 Mhz/114   none (pure)      4-bit poly
            //DIV31_POLY4 = 0x02,  //  0 0 1 0   3.58 Mhz/114   divide by 31     4-bit poly
            //POLY5_POLY4 = 0x03,  //  0 0 1 1   3.58 Mhz/114   5-bit poly       4-bit poly
            //PURE        = 0x04,  //  0 1 0 0   3.58 Mhz/114   none (pure)      pure (~Q)
            //PURE2       = 0x05,  //  0 1 0 1   3.58 Mhz/114   none (pure)      pure (~Q)
            //DIV31_PURE  = 0x06,  //  0 1 1 0   3.58 Mhz/114   divide by 31     pure (~Q)
            //POLY5_2     = 0x07,  //  0 1 1 1   3.58 Mhz/114   5-bit poly       pure (~Q)
            POLY9         = 0x08;  //  1 0 0 0   3.58 Mhz/114   none (pure)      9-bit poly
            //POLY5       = 0x09,  //  1 0 0 1   3.58 Mhz/114   none (pure)      5-bit poly
            //DIV31_POLY5 = 0x0a,  //  1 0 1 0   3.58 Mhz/114   divide by 31     5-bit poly
            //POLY5_POLY5 = 0x0b,  //  1 0 1 1   3.58 Mhz/114   5-bit poly       5-bit poly
            //DIV3_PURE   = 0x0c,  //  1 1 0 0   1.19 Mhz/114   none (pure)      pure (~Q)
            //DIV3_PURE2  = 0x0d,  //  1 1 0 1   1.19 Mhz/114   none (pure)      pure (~Q)
            //DIV93_PURE  = 0x0e,  //  1 1 1 0   1.19 Mhz/114   divide by 31     pure (~Q)
            //DIV3_POLY5  = 0x0f;  //  1 1 1 1   1.19 Mhz/114   5-bit poly       pure (~Q)

        const int
            AUDC0 = 0x15,        // audio control 0 (D3-0)
            AUDC1 = 0x16,        // audio control 1 (D4-0)
            AUDF0 = 0x17,        // audio frequency 0 (D4-0)
            AUDF1 = 0x18,        // audio frequency 1 (D3-0)
            AUDV0 = 0x19,        // audio volume 0 (D3-0)
            AUDV1 = 0x1a;        // audio volume 1 (D3-0)

        // The 4bit and 5bit patterns are the identical ones used in the tia chip.
        readonly byte[] Bit4 = new byte[] { 1, 1, 0, 1, 1, 1, 0, 0, 0, 0, 1, 0, 1, 0, 0 };  // 2^4 - 1 = 15
        readonly byte[] Bit5 = new byte[] { 0, 0, 1, 0, 1, 1, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0, 1, 1, 0, 1, 1, 1, 0, 1, 0, 1, 0, 0, 0, 0, 1 };  // 2^5 - 1 = 31

        // [Ron] treated the 'Div by 31' counter as another polynomial because of
        // the way it operates.  It does not have a 50% duty cycle, but instead
        // has a 13:18 ratio (of course, 13+18 = 31).  This could also be
        // implemented by using counters.
        readonly byte[] Div31 = new byte[] { 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        // Rather than have a table with 511 entries, I use a random number
        readonly byte[] Bit9 = new byte[511];  // 2^9 - 1 = 511

        readonly int[] P4 = new int[2];  // Position counter for the 4-bit POLY array
        readonly int[] P5 = new int[2];  // Position counter for the 5-bit POLY array
        readonly int[] P9 = new int[2];  // Position counter for the 9-bit POLY array

        readonly int[] DivByNCounter = new int[2];  // Divide by n counter, one for each channel
        readonly int[] DivByNMaximum = new int[2];  // Divide by n maximum, one for each channel

        readonly int _cpuClocksPerSample;

        #endregion

        #region Object State

        readonly MachineBase M;

        // The TIA Sound registers
        readonly byte[] AUDC = new byte[2];
        readonly byte[] AUDF = new byte[2];
        readonly byte[] AUDV = new byte[2];

        // The last output volume for each channel
        readonly byte[] OutputVol = new byte[2];

        // Used to determine how much sound to render
        ulong LastUpdateCPUClock;

        int BufferIndex;

        #endregion

        #region Public Members

        public void Reset()
        {
            for (var chan = 0; chan < 2; chan++)
            {
                OutputVol[chan] = 0;
                DivByNCounter[chan] = 0;
                DivByNMaximum[chan] = 0;
                AUDC[chan] = 0;
                AUDF[chan] = 0;
                AUDV[chan] = 0;
                P4[chan] = 0;
                P5[chan] = 0;
                P9[chan] = 0;
            }
        }

        public void StartFrame()
        {
            LastUpdateCPUClock = M.CPU.Clock;
            BufferIndex = 0;
        }

        public void EndFrame()
        {
            RenderSamples(M.FrameBuffer.SoundBufferByteLength - BufferIndex);
        }

        public void Update(ushort addr, byte data)
        {
            if (M.CPU.Clock > LastUpdateCPUClock)
            {
                var updCPUClocks = (int)(M.CPU.Clock - LastUpdateCPUClock);
                var samples = updCPUClocks / _cpuClocksPerSample;
                RenderSamples(samples);
                LastUpdateCPUClock += (ulong)(samples * _cpuClocksPerSample);
            }

            byte chan;

            switch (addr)
            {
                case AUDC0:
                    AUDC[0] = (byte)(data & 0x0f);
                    chan = 0;
                    break;
                case AUDC1:
                    AUDC[1] = (byte)(data & 0x0f);
                    chan = 1;
                    break;
                case AUDF0:
                    AUDF[0] = (byte)(data & 0x1f);
                    chan = 0;
                    break;
                case AUDF1:
                    AUDF[1] = (byte)(data & 0x1f);
                    chan = 1;
                    break;
                case AUDV0:
                    AUDV[0] = (byte)(data & 0x0f);
                    chan = 0;
                    break;
                case AUDV1:
                    AUDV[1] = (byte)(data & 0x0f);
                    chan = 1;
                    break;
                default:
                    return;
            }

            byte new_divn_max;

            if (AUDC[chan] == SET_TO_1)
            {
                // indicate the clock is zero so no process will occur
                new_divn_max = 0;
                // and set the output to the selected volume
                OutputVol[chan] = AUDV[chan];
            }
            else
            {
                // otherwise calculate the 'divide by N' value
                new_divn_max = (byte)(AUDF[chan] + 1);
                // if bits D2 & D3 are set, then multiply the 'div by n' count by 3
                if ((AUDC[chan] & 0x0c) == 0x0c)
                {
                    new_divn_max *= 3;
                }
            }

            // only reset those channels that have changed
            if (new_divn_max != DivByNMaximum[chan])
            {
                DivByNMaximum[chan] = new_divn_max;

                // if the channel is now volume only or was volume only...
                if (DivByNCounter[chan] == 0 || new_divn_max == 0)
                {
                    // reset the counter (otherwise let it complete the previous)
                    DivByNCounter[chan] = new_divn_max;
                }
            }
        }

        #endregion

        #region Constructors

        private TIASound()
        {
            var r = new Random();
            r.NextBytes(Bit9);
            for (var i = 0; i < Bit9.Length; i++)
            {
                Bit9[i] &= 0x01;
            }
        }

        public TIASound(MachineBase m, int cpuClocksPerSample) : this()
        {
            if (m == null)
                throw new ArgumentNullException("m");
            if (cpuClocksPerSample <= 0)
                throw new ArgumentException("cpuClocksPerSample must be positive.");

            M = m;
            _cpuClocksPerSample = cpuClocksPerSample;
        }

        #endregion

        #region Serialization Members

        public TIASound(DeserializationContext input, MachineBase m, int cpuClocksPerSample) : this(m, cpuClocksPerSample)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            input.CheckVersion(1);
            Bit9 = input.ReadExpectedBytes(511);
            P4 = input.ReadIntegers(2);
            P5 = input.ReadIntegers(2);
            P9 = input.ReadIntegers(2);
            DivByNCounter = input.ReadIntegers(2);
            DivByNMaximum = input.ReadIntegers(2);
            AUDC = input.ReadExpectedBytes(2);
            AUDF = input.ReadExpectedBytes(2);
            AUDV = input.ReadExpectedBytes(2);
            OutputVol = input.ReadExpectedBytes(2);
            LastUpdateCPUClock = input.ReadUInt64();
            BufferIndex = input.ReadInt32();
        }

        public void GetObjectData(SerializationContext output)
        {
            if (output == null)
                throw new ArgumentNullException("output");

            output.WriteVersion(1);
            output.Write(Bit9);
            output.Write(P4);
            output.Write(P5);
            output.Write(P9);
            output.Write(DivByNCounter);
            output.Write(DivByNMaximum);
            output.Write(AUDC);
            output.Write(AUDF);
            output.Write(AUDV);
            output.Write(OutputVol);
            output.Write(LastUpdateCPUClock);
            output.Write(BufferIndex);
        }

        #endregion

        #region Helpers

        void RenderSamples(int count)
        {
            for (; BufferIndex < M.FrameBuffer.SoundBufferByteLength && count-- > 0; BufferIndex++)
            {
                if (DivByNCounter[0] > 1)
                {
                    DivByNCounter[0]--;
                }
                else if (DivByNCounter[0] == 1)
                {
                    DivByNCounter[0] = DivByNMaximum[0];
                    ProcessChannel(0);
                }
                if (DivByNCounter[1] > 1)
                {
                    DivByNCounter[1]--;
                }
                else if (DivByNCounter[1] == 1)
                {
                    DivByNCounter[1] = DivByNMaximum[1];
                    ProcessChannel(1);
                }

                M.FrameBuffer.SoundBuffer[BufferIndex] += (byte)(OutputVol[0] + OutputVol[1]);
            }
        }

        void ProcessChannel(int chan)
        {
            // the P5 counter has multiple uses, so we inc it here
            if (++P5[chan] >= 31)
            {  // POLY5 size: 2^5 - 1 = 31
                P5[chan] = 0;
            }

            // check clock modifier for clock tick
            if ((AUDC[chan] & 0x02) == 0 ||
               ((AUDC[chan] & 0x01) == 0 && Div31[P5[chan]] == 1) ||
               ((AUDC[chan] & 0x01) == 1 && Bit5[P5[chan]] == 1))
            {
                if ((AUDC[chan] & 0x04) != 0)
                {   // pure modified clock selected
                    OutputVol[chan] = (OutputVol[chan] != 0) ? (byte)0 : AUDV[chan];
                }
                else if ((AUDC[chan] & 0x08) != 0)
                {    // check for poly5/poly9
                    if (AUDC[chan] == POLY9)
                    {   // check for poly9
                        if (++P9[chan] >= 511)
                        {   // poly9 size: 2^9 - 1 = 511
                            P9[chan] = 0;
                        }
                        OutputVol[chan] = (Bit9[P9[chan]] == 1) ? AUDV[chan] : (byte)0;
                    }
                    else
                    {   // must be poly5
                        OutputVol[chan] = (Bit5[P5[chan]] == 1) ? AUDV[chan] : (byte)0;
                    }
                }
                else
                {   // poly4 is the only remaining possibility
                    if (++P4[chan] >= 15)
                    {           // POLY4 size: 2^4 - 1 = 15
                        P4[chan] = 0;
                    }
                    OutputVol[chan] = (Bit4[P4[chan]] == 1) ? AUDV[chan] : (byte)0;
                }
            }
        }

        #endregion
    }
}
