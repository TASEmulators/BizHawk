using System;

namespace BizHawk.Emulation.Sound
{
    public partial class YM2612
    {
        public sealed class Operator
        {
            // External Settings
            public int TL_TotalLevel;
            public int AR_AttackRate;
            public int RS_RateScaling;
            public int D1R_FirstDecayRate;
            public int D2R_SecondDecayRate;
            public int D1L_FirstDecayLevel;
            public int RR_ReleaseRate;
            public int SSG_EG;

            public int DT1_Detune;
            public int MUL_Multiple;

            public bool AM_AmplitudeModulation;

            public int Frequency;

            // Internal State
            // ...
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