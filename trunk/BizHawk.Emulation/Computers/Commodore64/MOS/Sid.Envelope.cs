using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
    sealed public partial class Sid
    {
        sealed class Envelope
        {
            const int stateAttack = 0;
            const int stateDecay = 1;
            const int stateRelease = 2;

            int attack;
            int decay;
            bool delay;
            int envCounter;
            int expCounter;
            int expPeriod;
            bool freeze;
            int lfsr;
            bool gate;
            int rate;
            int release;
            int state;
            int sustain;

            static int[] adsrTable = new int[]
			{
				0x7F00, 0x0006, 0x003C, 0x0330,
				0x20C0, 0x6755, 0x3800, 0x500E,
				0x1212, 0x0222, 0x1848, 0x59B8,
				0x3840, 0x77E2, 0x7625, 0x0A93
			};

            static int[] expCounterTable = new int[]
			{
				0xFF, 0x5D, 0x36, 0x1A, 0x0E, 0x06, 0x00
			};

            static int[] expPeriodTable = new int[]
			{
				0x01, 0x02, 0x04, 0x08, 0x10, 0x1E, 0x01
			};

            static int[] sustainTable = new int[]
			{
				0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
				0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF
			};

            public Envelope()
            {
                HardReset();
            }

            public void ExecutePhase2()
            {

                {
                    if (!delay)
                    {
                        envCounter--;
                        delay = true;
                        UpdateExpCounter();
                    }

                    if (lfsr != rate)
                    {
                        int feedback = ((lfsr >> 14) ^ (lfsr >> 13)) & 0x1;
                        lfsr = ((lfsr << 1) & 0x7FFF) | feedback;
                        return;
                    }
                    lfsr = 0x7FFF;

                    if (state == stateAttack || ++expCounter == expPeriod)
                    {
                        expCounter = 0;
                        if (freeze)
                            return;

                        switch (state)
                        {
                            case stateAttack:
                                envCounter++;
                                if (envCounter == 0xFF)
                                {
                                    state = stateDecay;
                                    rate = adsrTable[decay];
                                }
                                break;
                            case stateDecay:
                                if (envCounter == sustainTable[sustain])
                                {
                                    return;
                                }
                                if (expPeriod != 1)
                                {
                                    delay = false;
                                    return;
                                }
                                envCounter--;
                                break;
                            case stateRelease:
                                if (expPeriod != 1)
                                {
                                    delay = false;
                                    return;
                                }
                                envCounter--;
                                break;
                        }
                        envCounter &= 0xFF;
                        UpdateExpCounter();
                    }
                }
            }

            public void HardReset()
            {
                attack = 0;
                decay = 0;
                delay = true;
                envCounter = 0;
                expCounter = 0;
                expPeriod = expPeriodTable[0];
                freeze = false;
                gate = false;
                lfsr = 0x7FFF;
                rate = adsrTable[release];
                release = 0;
                state = stateRelease;
                sustain = 0;
            }

            private void UpdateExpCounter()
            {

                {
                    for (int i = 0; i < 7; i++)
                    {
                        if (envCounter == expCounterTable[i])
                            expPeriod = expPeriodTable[i];
                    }
                    if (envCounter == 0)
                        freeze = true;
                }
            }

            // ------------------------------------

            public int Attack
            {
                get
                {
                    return attack;
                }
                set
                {
                    attack = (value & 0xF);
                    if (state == stateAttack)
                        rate = adsrTable[attack];
                }
            }

            public int Decay
            {
                get
                {
                    return decay;
                }
                set
                {
                    decay = (value & 0xF);
                    if (state == stateDecay)
                        rate = adsrTable[decay];
                }
            }

            public bool Gate
            {
                get
                {
                    return gate;
                }
                set
                {
                    bool nextGate = value;
                    if (nextGate && !gate)
                    {
                        state = stateAttack;
                        rate = adsrTable[attack];
                        delay = true;
                        freeze = false;
                    }
                    else if (!nextGate && gate)
                    {
                        state = stateRelease;
                        rate = adsrTable[release];
                    }
                    gate = nextGate;
                }
            }

            public int Level
            {
                get
                {
                    return envCounter;
                }
            }

            public int Release
            {
                get
                {
                    return release;
                }
                set
                {
                    release = (value & 0xF);
                    if (state == stateRelease)
                        rate = adsrTable[release];
                }
            }

            public int Sustain
            {
                get
                {
                    return sustain;
                }
                set
                {
                    sustain = (value & 0xF);
                }
            }

            // ------------------------------------

            public void SyncState(Serializer ser)
            {
                SaveState.SyncObject(ser, this);
            }

            // ------------------------------------
        }
    }
}
