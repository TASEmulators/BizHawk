using System;

namespace BizHawk.Emulation.Sound
{
    public partial class YM2612
    {
        public sealed class Operator
        {
            // External Settings
            public int TL_TotalLevel;
            public int SL_SustainLevel;
            public int AR_AttackRate;
            public int DR_DecayRate;
            public int SR_SustainRate;
            public int RR_ReleaseRate;
            public int KS_KeyScale;
            public int SSG_EG;

            public int DT1_Detune;
            public int MUL_Multiple;

            public bool AM_AmplitudeModulation;

            public int Frequency;

            // Internal State
            public int PhaseCounter;

            // I/O
            public void Write_MUL_DT1(byte value)
            {
                MUL_Multiple = value & 15;
                DT1_Detune = (value >> 4) & 7;
            }

            public void Write_TL(byte value)
            {
                TL_TotalLevel = value & 127;
            }

            public void Write_AR_KS(byte value)
            {
                AR_AttackRate = value & 31;
                KS_KeyScale = value >> 6;
            }

            public void Write_DR_AM(byte value)
            {
                DR_DecayRate = value & 31;
                AM_AmplitudeModulation = (value & 128) != 0;
            }

            public void Write_SR(byte value)
            {
                SR_SustainRate = value & 31;
            }

            public void Write_RR_SL(byte value)
            {
                RR_ReleaseRate = value & 15;
                SL_SustainLevel = value >> 4;
            }

            public void Write_SSGEG(byte value)
            {
                SSG_EG = value & 15;
            }
        }
    }
    //TODO "the shape of the waves of the envelope changes in a exponential when attacking it, and it changes in the straight line at other rates."
    
    // pg 8, read it
    // pg 11, detailed overview of how operator works.
    // pg 12, detailed description of phase generator.

    //TL      Total Level     7 bits 
    //SL      Sustain Level   4 bits 
    //AR      Attack Rate     5 bits 
    //DR      Decay Rate      5 bits 
    //SR      Sustain Rate    5 bits 
    //RR      Release Rate    4 bits 
    //SSG-EG  SSG-EG Mode     4 bits
}