using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Sound
{
    // We process TIMER writes immediately when writes come in.
    // All other writes are queued up with a timestamp, so that we  
    // can sift through them when we're rendering audio for the frame.
    
    public partial class YM2612
    {
        byte PartSelect;
        byte RegisterSelect;
        bool DacEnable;
        byte DacValue;

        Queue<QueuedCommand> commands = new Queue<QueuedCommand>();

        public byte ReadStatus(int clock)
        {
            UpdateTimers(clock);

            byte retval = 0;
            if (TimerATripped) retval |= 1;
            if (TimerBTripped) retval |= 2;
            return retval;
        }

        public void Write(int addr, byte value, int clock)
        {
            UpdateTimers(clock);

            if (addr == 0)
            {
                PartSelect = 1;
                RegisterSelect = value;
                return;
            }
            else if (addr == 2)
            {
                PartSelect = 2;
                RegisterSelect = value;
                return;
            }

            if (PartSelect == 1)
            {
                if (RegisterSelect == 0x24) { WriteTimerA_MSB_24(value, clock); return; }
                if (RegisterSelect == 0x25) { WriteTimerA_LSB_25(value, clock); return; }
                if (RegisterSelect == 0x26) { WriteTimerB_26(value, clock); return; }
                if (RegisterSelect == 0x27) { WriteTimerControl_27(value, clock); } // don't return on this one; we process immediately AND enqueue command for port $27.
            }

            var cmd = new QueuedCommand { Part = PartSelect, Register = RegisterSelect, Data = value, Clock = clock-frameStartClock };
            commands.Enqueue(cmd);
        }

        void WriteCommand(QueuedCommand cmd)
        {
            if (cmd.Part == 1)
                Part1_WriteRegister(cmd.Register, cmd.Data);
            else
                Part2_WriteRegister(cmd.Register, cmd.Data);
        }

        static void GetChanOpP1(byte value, out int channel, out int oper)
        {
            value &= 15;
            switch (value)
            {
                case 0:  channel = 0; oper = 0; return;
                case 4:  channel = 0; oper = 2; return;
                case 8:  channel = 0; oper = 1; return;
                case 12: channel = 0; oper = 3; return;

                case 1:  channel = 1; oper = 0; return;
                case 5:  channel = 1; oper = 2; return;
                case 9:  channel = 1; oper = 1; return;
                case 13: channel = 1; oper = 3; return;

                case 2:  channel = 2; oper = 0; return;
                case 6:  channel = 2; oper = 2; return;
                case 10: channel = 2; oper = 1; return;
                case 14: channel = 2; oper = 3; return;

                default: channel = -1; oper = -1; return;
            }
        }

        static void GetChanOpP2(byte value, out int channel, out int oper)
        {
            value &= 15;
            switch (value)
            {
                case 0:  channel = 3; oper = 0; return;
                case 4:  channel = 3; oper = 2; return;
                case 8:  channel = 3; oper = 1; return;
                case 12: channel = 3; oper = 3; return;

                case 1:  channel = 4; oper = 0; return;
                case 5:  channel = 4; oper = 2; return;
                case 9:  channel = 4; oper = 1; return;
                case 13: channel = 4; oper = 3; return;

                case 2:  channel = 5; oper = 0; return;
                case 6:  channel = 5; oper = 2; return;
                case 10: channel = 5; oper = 1; return;
                case 14: channel = 5; oper = 3; return;

                default: channel = -1; oper = -1; return;
            }
        }

        void Part1_WriteRegister(byte register, byte value)
        {
            switch (register)
            {
                //case 0x22: Console.WriteLine("LFO Control {0:X2}", value); break;
                case 0x24: break; // Timer A MSB, handled immediately
                case 0x25: break; // Timer A LSB, handled immediately
                case 0x26: break; // Timer B, handled immediately
                //case 0x27: Console.WriteLine("$27: Ch3 Mode / Timer Control {0:X2}", value); break; // determines if CH3 has 1 frequency or 4 frequencies.
                //case 0x28: Console.WriteLine("Operator Key On/Off Ctrl {0:X2}", value); break;
                case 0x2A: DacValue = value; break;
                case 0x2B: DacEnable = (value & 0x80) != 0; break;
                case 0x2C: throw new Exception("something wrote to ym2612 port $2C!"); //http://forums.sonicretro.org/index.php?showtopic=28589

                default:
                    int chan, oper;
                    GetChanOpP1(register, out chan, out oper);
                    if (chan < 0) break; // abort if invalid port number
                    switch (register & 0xF0)
                    {
                        case 0x30: Channels[chan].Operators[oper].Write_MUL_DT1(value); break;
                        case 0x40: Channels[chan].Operators[oper].Write_TL(value); break;
                        case 0x50: Channels[chan].Operators[oper].Write_AR_KS(value); break;
                        case 0x60: Channels[chan].Operators[oper].Write_DR_AM(value); break;
                        case 0x70: Channels[chan].Operators[oper].Write_SR(value); break;
                        case 0x80: Channels[chan].Operators[oper].Write_RR_SL(value); break;
                        case 0x90: Channels[chan].Operators[oper].Write_SSGEG(value); break;
                        case 0xA0:
                        case 0xB0: WriteHighBlockP1(register, value); break;
                    }
                    break;

                // "In MAME OPN emulation code register pairs for multi-frequency mode are A6A2, ACA8, AEAA, ADA9".

                //D7 - operator, which frequency defined by A6A2 
                //D6 - .. ACA8 
                //D5 - .. AEAA 
                //D4 - .. ADA9
                //Where D7=op4, D6=op3, D5=op2, and D4=op1. That matches the YM2608 document. At least that's confirmed then. 

                // PG4 has some info on frquency calculations
            }
        }

        void Part2_WriteRegister(byte register, byte value)
        {
            // NOTE. Only first bank has multi-frequency CSM/Special mode. This mode can't work on CH6.

            int chan, oper;
            GetChanOpP2(register, out chan, out oper);
            if (chan < 0) return; // abort if invalid port number
            switch (register & 0xF0)
            {
                case 0x30: Channels[chan].Operators[oper].Write_MUL_DT1(value); break;
                case 0x40: Channels[chan].Operators[oper].Write_TL(value); break;
                case 0x50: Channels[chan].Operators[oper].Write_AR_KS(value); break;
                case 0x60: Channels[chan].Operators[oper].Write_DR_AM(value); break;
                case 0x70: Channels[chan].Operators[oper].Write_SR(value); break;
                case 0x80: Channels[chan].Operators[oper].Write_RR_SL(value); break;
                case 0x90: Channels[chan].Operators[oper].Write_SSGEG(value); break;
                case 0xA0:
                case 0xB0: WriteHighBlockP2(register, value); break;
            }
        }

        void WriteHighBlockP1(byte register, byte value)
        {
            switch (register)
            {
                case 0xA0: Channels[0].WriteFrequencyLow(value); break;
                case 0xA1: Channels[1].WriteFrequencyLow(value); break;
                case 0xA2: Channels[2].WriteFrequencyLow(value); break;

                case 0xA4: Channels[0].WriteFrequencyHigh(value); break;
                case 0xA5: Channels[1].WriteFrequencyHigh(value); break;
                case 0xA6: Channels[2].WriteFrequencyHigh(value); break;

                case 0xB0: Channels[0].Write_Feedback_Algorithm(value); break;
                case 0xB1: Channels[1].Write_Feedback_Algorithm(value); break;
                case 0xB2: Channels[2].Write_Feedback_Algorithm(value); break;

                case 0xB4: Channels[0].Write_Stereo_LfoSensitivy(value); break;
                case 0xB5: Channels[1].Write_Stereo_LfoSensitivy(value); break;
                case 0xB6: Channels[2].Write_Stereo_LfoSensitivy(value); break;
            }
        }

        void WriteHighBlockP2(byte register, byte value)
        {
            switch (register)
            {
                case 0xA0: Channels[3].WriteFrequencyLow(value); break;
                case 0xA1: Channels[4].WriteFrequencyLow(value); break;
                case 0xA2: Channels[5].WriteFrequencyLow(value); break;

                case 0xA4: Channels[3].WriteFrequencyHigh(value); break;
                case 0xA5: Channels[4].WriteFrequencyHigh(value); break;
                case 0xA6: Channels[5].WriteFrequencyHigh(value); break;

                case 0xB0: Channels[3].Write_Feedback_Algorithm(value); break;
                case 0xB1: Channels[4].Write_Feedback_Algorithm(value); break;
                case 0xB2: Channels[5].Write_Feedback_Algorithm(value); break;

                case 0xB4: Channels[3].Write_Stereo_LfoSensitivy(value); break;
                case 0xB5: Channels[4].Write_Stereo_LfoSensitivy(value); break;
                case 0xB6: Channels[5].Write_Stereo_LfoSensitivy(value); break;
            }
        }

        public class QueuedCommand
        {
            public byte Part;
            public byte Register;
            public byte Data;
            public int Clock;
        }
    }
}