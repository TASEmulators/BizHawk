using System;

namespace BizHawk.Emulation.Sound
{
    public partial class YM2612
    {
        public sealed class Channel
        {
            public readonly Operator[] Operators;

            public int FrequencyNumber;
            public int Block;
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

                LeftOutput = true; // Revenge of Shinobi does not output DAC if these arent initialized ??
                RightOutput = true;
            }

            public void WriteFrequencyLow(byte value)
            {
                FrequencyNumber &= 0x700;
                FrequencyNumber |= value;

                // TODO maybe its 4-frequency mode
                // TODO is this right, only reflect change when writing LSB?
                Operators[0].FrequencyNumber = FrequencyNumber;
                Operators[1].FrequencyNumber = FrequencyNumber;
                Operators[2].FrequencyNumber = FrequencyNumber;
                Operators[3].FrequencyNumber = FrequencyNumber;
            }

            public void WriteFrequencyHigh(byte value)
            {
                FrequencyNumber &= 0x0FF;
                FrequencyNumber |= (value & 15) << 8;
                Block = (value >> 3) & 7;
            }

            public void Write_Feedback_Algorithm(byte value)
            {
                Algorithm = value & 7;
                Feedback = (value >> 3) & 7;
            }

            public void Write_Stereo_LfoSensitivy(byte value)
            {
                FMS_FrequencyModulationSensitivity = value & 3;
                AMS_AmplitudeModulationSensitivity = (value >> 3) & 7;
                RightOutput = (value & 0x40) != 0;
                LeftOutput = (value & 0x80) != 0;
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