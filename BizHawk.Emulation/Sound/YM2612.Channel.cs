using System;

namespace BizHawk.Emulation.Sound
{
    public partial class YM2612
    {
        public sealed class Channel
        {
            public readonly Operator[] Operators;

            public int Frequency;
            public int Feedback;
            public int Algorithm;

            public bool SpecialMode; // Enables separate frequency for each operator, available on CH3 and CH6 only
            // TODO. CSM.   Pg6 details CSM mode.
            public bool LeftOutput;
            public bool RightOutput;

            public int AMS_AmplitudeModulationSensitivity;
            public int FMS_FrequencyModulationSensitivity;

            public Channel()
            {
                Operators = new Operator[4];
                Operators[0] = new Operator();
                Operators[1] = new Operator();
                Operators[2] = new Operator();
                Operators[3] = new Operator();
            }

            //---------------------- 
            //|Mode| Behaviour     | 
            //|----|---------------| 
            //| 00 | Normal        | 
            //| 01 | Special       | 
            //| 10 | Special + CSM | 
            //| 11 | Special       | 
            //----------------------
        }
    }
}