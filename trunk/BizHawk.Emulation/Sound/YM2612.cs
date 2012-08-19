using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace BizHawk.Emulation.Sound
{
    // ======================================================================
    //  Yamaha YM2612 Emulation Core
    //  Primarily sourced from Nemesis's documentation on Sprite's Mind forums: 
    //     http://gendev.spritesmind.net/forum/viewtopic.php?t=386
    //
    //  Notes:
    //   - In order to facilitate asynchronous sound generation, timer commands
    //     and reads are emulated immediately, while all other commands are 
    //     queued together with a timestamp and processed at the end of the frame.
    //   - Commands are stretched in time to match the number of samples requested 
    //     for the frame. For accurate, synchronous sound, simply request the correct
    //     number of samples for each frame.
    //   - Output is emulated at native output rate and downsampled (badly) to 44100hz.
    // ======================================================================

    // TODO: Finish testing Envelope generator
    // TODO: maybe add guards when changing envelope parameters to immediately change envelope state (ie dont wait for EG cycle?)
    // TODO: Detune
    // TODO: LFO
    // TODO: Switch from Perfect Operator to Accurate Operator.
    // TODO: Operator1 Self-Feedback
    // TODO: MEM delayed samples
    // TODO: CSM mode
    // TODO: SSG-EG
    // TODO: Seriously, I think we need better resampling code.
    // TODO: Experiment with low-pass filters, etc.

    public sealed class YM2612 : ISoundProvider
    {
        public readonly Channel[] Channels = { new Channel(), new Channel(), new Channel(), new Channel(), new Channel(), new Channel() };

        public YM2612()
        {
            InitTimers();
            MaxVolume = short.MaxValue;
        }

        // ====================================================================================

        int frameStartClock;
        int frameEndClock;

        public void BeginFrame(int clock)
        {
            frameStartClock = clock;
            while (commands.Count > 0)
            {
                var cmd = commands.Dequeue();
                WriteCommand(cmd);
            }
        }

        public void EndFrame(int clock)
        {
            frameEndClock = clock;
        }

        // ====================================================================================
        //                                      YM2612 I/O
        // ====================================================================================

        public class QueuedCommand
        {
            public byte Part;
            public byte Register;
            public byte Data;
            public int Clock;
        }

        byte PartSelect;
        byte RegisterSelect;
        bool DacEnable;
        byte DacValue;

        Queue<QueuedCommand> commands = new Queue<QueuedCommand>();

        const int Slot1 = 0;
        const int Slot2 = 2;
        const int Slot3 = 1;
        const int Slot4 = 3;

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
                if (RegisterSelect == 0x27) { WriteTimerControl_27(value, clock); } // we process immediately AND enqueue command for port $27. Allows accurate tracking of CH3 special modes.
            }

            // If its not timer related just queue the command write
            var cmd = new QueuedCommand { Part = PartSelect, Register = RegisterSelect, Data = value, Clock = clock - frameStartClock };
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
            int chan, oper;
            GetChanOpP1(register, out chan, out oper);

            switch (register & 0xF0)
            {
                case 0x20: WriteLowBlock(register, value); break;
                case 0x30: Write_MUL_DT(chan, oper, value); break;
                case 0x40: Write_TL(chan, oper, value); break;
                case 0x50: Write_AR_KS(chan, oper, value); break;
                case 0x60: Write_DR_AM(chan, oper, value); break;
                case 0x70: Write_SR(chan, oper, value); break;
                case 0x80: Write_RR_SL(chan, oper, value); break;
                case 0x90: Write_SSGEG(chan, oper, value); break;
                case 0xA0:
                case 0xB0: WriteHighBlockP1(register, value); break;
            }
        }

        void Part2_WriteRegister(byte register, byte value)
        {
            int chan, oper;
            GetChanOpP2(register, out chan, out oper);

            switch (register & 0xF0)
            {
                case 0x30: Write_MUL_DT(chan, oper, value); break;
                case 0x40: Write_TL(chan, oper, value); break;
                case 0x50: Write_AR_KS(chan, oper, value); break;
                case 0x60: Write_DR_AM(chan, oper, value); break;
                case 0x70: Write_SR(chan, oper, value); break;
                case 0x80: Write_RR_SL(chan, oper, value); break;
                case 0x90: Write_SSGEG(chan, oper, value); break;
                case 0xA0:
                case 0xB0: WriteHighBlockP2(register, value); break;
            }
        }

        void WriteLowBlock(byte register, byte value)
        {
            switch (register)
            {
                //case 0x22: Console.WriteLine("LFO Control {0:X2}", value); break;
                case 0x24: break; // Timer A MSB, handled immediately
                case 0x25: break; // Timer A LSB, handled immediately
                case 0x26: break; // Timer B, handled immediately
                //case 0x27: Console.WriteLine("$27: Ch3 Mode / Timer Control {0:X2}", value); break; // determines if CH3 has 1 frequency or 4 frequencies.
                case 0x28: KeyOnOff(value); break;
                case 0x2A: DacValue = value; break;
                case 0x2B: DacEnable = (value & 0x80) != 0; break;
                case 0x2C: throw new Exception("something wrote to ym2612 port $2C!"); //http://forums.sonicretro.org/index.php?showtopic=28589
            }
        }

        void WriteHighBlockP1(byte register, byte value)
        {
            switch (register)
            {
                case 0xA0: WriteFrequencyLow(Channels[0], value); break;
                case 0xA1: WriteFrequencyLow(Channels[1], value); break;
                case 0xA2: WriteFrequencyLow(Channels[2], value); break;

                case 0xA4: WriteFrequencyHigh(Channels[0], value); break;
                case 0xA5: WriteFrequencyHigh(Channels[1], value); break;
                case 0xA6: WriteFrequencyHigh(Channels[2], value); break;

                case 0xB0: Write_Feedback_Algorithm(Channels[0], value); break;
                case 0xB1: Write_Feedback_Algorithm(Channels[1], value); break;
                case 0xB2: Write_Feedback_Algorithm(Channels[2], value); break;

                case 0xB4: Write_Stereo_LfoSensitivy(Channels[0], value); break;
                case 0xB5: Write_Stereo_LfoSensitivy(Channels[1], value); break;
                case 0xB6: Write_Stereo_LfoSensitivy(Channels[2], value); break;
            }
        }

        void WriteHighBlockP2(byte register, byte value)
        {
            switch (register)
            {
                case 0xA0: WriteFrequencyLow(Channels[3], value); break;
                case 0xA1: WriteFrequencyLow(Channels[4], value); break;
                case 0xA2: WriteFrequencyLow(Channels[5], value); break;

                case 0xA4: WriteFrequencyHigh(Channels[3], value); break;
                case 0xA5: WriteFrequencyHigh(Channels[4], value); break;
                case 0xA6: WriteFrequencyHigh(Channels[5], value); break;

                case 0xB0: Write_Feedback_Algorithm(Channels[3], value); break;
                case 0xB1: Write_Feedback_Algorithm(Channels[4], value); break;
                case 0xB2: Write_Feedback_Algorithm(Channels[5], value); break;

                case 0xB4: Write_Stereo_LfoSensitivy(Channels[3], value); break;
                case 0xB5: Write_Stereo_LfoSensitivy(Channels[4], value); break;
                case 0xB6: Write_Stereo_LfoSensitivy(Channels[5], value); break;
            }
        }

        void KeyOnOff(byte value)
        {
            int channel = value & 3;
            if (channel == 3) return; // illegal channel number, abort
            if ((value & 4) != 0) channel += 3; // select part 2

            var chan = Channels[channel];

            //Console.WriteLine("KeyOnOff for channel {0}", channel);

            if ((value & 0x10) != 0) KeyOn(chan.Operators[Slot1]); else KeyOff(chan.Operators[Slot1]);
            if ((value & 0x20) != 0) KeyOn(chan.Operators[Slot2]); else KeyOff(chan.Operators[Slot2]);
            if ((value & 0x40) != 0) KeyOn(chan.Operators[Slot3]); else KeyOff(chan.Operators[Slot3]);
            if ((value & 0x80) != 0) KeyOn(chan.Operators[Slot4]); else KeyOff(chan.Operators[Slot4]);
        }

        static void WriteFrequencyLow(Channel channel, byte value)
        {
            channel.FrequencyNumber &= 0x700;
            channel.FrequencyNumber |= value;

            // TODO maybe its 4-frequency mode
            // TODO is this right, only reflect change when writing LSB?

            Operator op;
            op = channel.Operators[0]; op.FrequencyNumber = channel.FrequencyNumber; op.Block = channel.Block; CalcKeyCode(op); CalcRks(op);
            op = channel.Operators[1]; op.FrequencyNumber = channel.FrequencyNumber; op.Block = channel.Block; CalcKeyCode(op); CalcRks(op);
            op = channel.Operators[2]; op.FrequencyNumber = channel.FrequencyNumber; op.Block = channel.Block; CalcKeyCode(op); CalcRks(op);
            op = channel.Operators[3]; op.FrequencyNumber = channel.FrequencyNumber; op.Block = channel.Block; CalcKeyCode(op); CalcRks(op);
        }

        static void WriteFrequencyHigh(Channel channel, byte value)
        {
            channel.FrequencyNumber &= 0x0FF;
            channel.FrequencyNumber |= (value & 15) << 8;
            channel.Block = (value >> 3) & 7;
        }

        static void CalcKeyCode(Operator op)
        {
            int freq = op.FrequencyNumber;
            bool f11 = ((freq >> 10) & 1) != 0;
            bool f10 = ((freq >>  9) & 1) != 0;
            bool f09 = ((freq >>  8) & 1) != 0;
            bool f08 = ((freq >>  7) & 1) != 0;

            bool n3a = f11 & (f10 | f09 | f08);
            bool n3b = !f11 & f10 & f09 & f08;
            bool n3  = n3a | n3b;

            op.KeyCode = (op.Block << 2) | (f11 ? 2 : 0) | (n3 ? 1 :0);
        }

        static void CalcRks(Operator op)
        {
            int shiftVal = 3 - op.KS_KeyScale;
            op.Rks = op.KeyCode >> shiftVal;
        }

        static void Write_Feedback_Algorithm(Channel channel, byte value)
        {
            channel.Algorithm = value & 7;
            channel.Feedback = (value >> 3) & 7;
        }

        static void Write_Stereo_LfoSensitivy(Channel channel, byte value)
        {
            channel.FMS_FrequencyModulationSensitivity = value & 3;
            channel.AMS_AmplitudeModulationSensitivity = (value >> 3) & 7;
            channel.RightOutput = (value & 0x40) != 0;
            channel.LeftOutput  = (value & 0x80) != 0;
        }

        void Write_MUL_DT(int chan, int op, byte value)
        {
            if (chan < 0) return;
            var oper = Channels[chan].Operators[op];
            oper.MUL_Multiple = value & 15;
            oper.DT_Detune = (value >> 4) & 7;
        }

        public void Write_TL(int chan, int op, byte value)
        {
            if (chan < 0) return;
            var oper = Channels[chan].Operators[op];
            oper.TL_TotalLevel = value & 127;
        }

        public void Write_AR_KS(int chan, int op, byte value)
        {
            if (chan < 0) return;
            var oper = Channels[chan].Operators[op];
            oper.AR_AttackRate = value & 31;
            oper.KS_KeyScale = value >> 6;
            CalcRks(oper);
        }

        public void Write_DR_AM(int chan, int op, byte value)
        {
            if (chan < 0) return;
            var oper = Channels[chan].Operators[op];
            oper.DR_DecayRate = value & 31;
            oper.AM_AmplitudeModulation = (value & 128) != 0;
        }

        public void Write_SR(int chan, int op, byte value)
        {
            if (chan < 0) return;
            var oper = Channels[chan].Operators[op];
            oper.SR_SustainRate = value & 31;
        }

        public void Write_RR_SL(int chan, int op, byte value)
        {
            if (chan < 0) return;
            var oper = Channels[chan].Operators[op];
            oper.RR_ReleaseRate = value & 15;
            oper.SL_SustainLevel = value >> 4;
        }

        public void Write_SSGEG(int chan, int op, byte value)
        {
            if (chan < 0) return;
            var oper = Channels[chan].Operators[op];
            oper.SSG_EG = value & 15;
        }

        // ====================================================================================
        //                                        Timers
        // ====================================================================================

        // Assuming this is connected to a Genesis/MegaDrive:

        // The master clock on the Genesis is 53,693,175 MCLK / sec (NTSC)
        //                                    53,203,424 MCLK / sec (PAL)
        //                                     7,670,454 68K cycles / sec (7 MCLK divisor) (NTSC)
        //                                     3,579,545 Z80 cycles / sec (15 MCLK divisor) (NTSC)

        // YM2612 is fed by 68000 Clock:       7,670,454 ECLK / sec (NTSC)
        //                                     7,600,489 ECLK / sec (PAL)

        // YM2612 has /6 divisor on the EXT CLOCK.
        // YM2612 takes 24 cycles to generate a sample. 6*24 = 144. This is where the /144 divisor comes from.
        // YM2612 native output rate is 7670454 / 144 = 53267 hz (NTSC), 52781 hz (PAL)

        // Timer A ticks at the native output rate (53267 times per second for NTSC).
        // Timer B ticks down with a /16 divisor. (3329 times per second for NTSC).

        // Ergo, Timer A ticks every 67.2 Z80 cycles. Timer B ticks every 1075.2 Z80 cycles.

        // TODO: make this not hardcoded to Genesis timing.

        const float timerAZ80Factor = 67.2f;
        const float timerBZ80Factor = 1075.2f;

        const int ntscOutputRate = 53267;
        const int palOutputRate = 52781;

        const float ntsc44100Factor = 1.20786848f;
        const float pal44100Factor = 1.19684807f;

        int TimerAPeriod, TimerBPeriod;
        bool TimerATripped, TimerBTripped;
        int TimerAResetClock, TimerBResetClock;
        int TimerALastReset, TimerBLastReset;

        byte TimerControl27;
        bool TimerALoad   { get { return (TimerControl27 & 1) != 0; } }
        bool TimerBLoad   { get { return (TimerControl27 & 2) != 0; } }
        bool TimerAEnable { get { return (TimerControl27 & 4) != 0; } }
        bool TimerBEnable { get { return (TimerControl27 & 8) != 0; } }
        bool TimerAReset  { get { return (TimerControl27 & 16) != 0; } }
        bool TimerBReset  { get { return (TimerControl27 & 32) != 0; } }

        void InitTimers()
        {
            TimerAResetClock = 68812;
            TimerBResetClock = 275200;
        }

        void UpdateTimers(int clock)
        {
            int elapsedCyclesSinceLastTimerAReset = clock - TimerALastReset;
            if (elapsedCyclesSinceLastTimerAReset > TimerAResetClock)
            {
                if (TimerAEnable)
                    TimerATripped = true;

                int numTimesTripped = elapsedCyclesSinceLastTimerAReset / TimerAResetClock;
                TimerALastReset += (TimerAResetClock * numTimesTripped);
            }

            int elapsedCyclesSinceLastTimerBReset = clock - TimerBLastReset;
            if (elapsedCyclesSinceLastTimerBReset > TimerBResetClock)
            {
                if (TimerBEnable)
                    TimerBTripped = true;

                int numTimesTripped = elapsedCyclesSinceLastTimerBReset / TimerBResetClock;
                TimerBLastReset += (TimerBResetClock * numTimesTripped);
            }
        }

        void WriteTimerA_MSB_24(byte value, int clock)
        {
            TimerAPeriod = (value << 2) | (TimerAPeriod & 3);
            TimerAResetClock = (int)((1024 - TimerAPeriod) * timerAZ80Factor);
        }

        void WriteTimerA_LSB_25(byte value, int clock)
        {
            TimerAPeriod = (TimerAPeriod & 0x3FC) | (value & 3);
            TimerAResetClock = (int)((1024 - TimerAPeriod) * timerAZ80Factor);
        }

        void WriteTimerB_26(byte value, int clock)
        {
            TimerBPeriod = value;
            TimerBResetClock = (int)((256 - TimerBPeriod) * timerBZ80Factor);
        }

        void WriteTimerControl_27(byte value, int clock)
        {
            bool lagALoad = TimerALoad;
            bool lagBLoad = TimerBLoad;

            TimerControl27 = value;

            if (!lagALoad && TimerALoad)
                TimerALastReset = clock;

            if (!lagBLoad && TimerBLoad)
                TimerBLastReset = clock;

            if (TimerAReset) TimerATripped = false;
            if (TimerBReset) TimerBTripped = false;
        }

        // ====================================================================================
        //                                       Support Tables
        // ====================================================================================

        #region tables
        static readonly byte[] egRateCounterShiftValues = 
            {
                11, 11, 11, 11, // Rates 0-3
                10, 10, 10, 10, // Rates 4-7
                9,  9,  9,  9,  // Rates 8-11
                8,  8,  8,  8,  // Rates 12-15
                7,  7,  7,  7,  // Rates 16-19
                6,  6,  6,  6,  // Rates 20-23
                5,  5,  5,  5,  // Rates 24-27
                4,  4,  4,  4,  // Rates 28-31
                3,  3,  3,  3,  // Rates 32-35
                2,  2,  2,  2,  // Rates 36-39
                1,  1,  1,  1,  // Rates 40-43
                0,  0,  0,  0,  // Rates 44-47
                0,  0,  0,  0,  // Rates 48-51
                0,  0,  0,  0,  // Rates 52-55
                0,  0,  0,  0,  // Rates 56-59
                0,  0,  0,  0   // Rates 60-63
            };

        static readonly byte[] egRateIncrementValues = 
            {
                0,0,0,0,0,0,0,0, // Rate 0
                0,0,0,0,0,0,0,0, // Rate 1
                0,1,0,1,0,1,0,1, // Rate 2
                0,1,0,1,0,1,0,1, // Rate 3
                0,1,0,1,0,1,0,1, // Rate 4
                0,1,0,1,0,1,0,1, // Rate 5
                0,1,1,1,0,1,1,1, // Rate 6
                0,1,1,1,0,1,1,1, // Rate 7
                0,1,0,1,0,1,0,1, // Rate 8
                0,1,0,1,1,1,0,1, // Rate 9
                0,1,1,1,0,1,1,1, // Rate 10
                0,1,1,1,1,1,1,1, // Rate 11
                0,1,0,1,0,1,0,1, // Rate 12
                0,1,0,1,1,1,0,1, // Rate 13
                0,1,1,1,0,1,1,1, // Rate 14
                0,1,1,1,1,1,1,1, // Rate 15
                0,1,0,1,0,1,0,1, // Rate 16
                0,1,0,1,1,1,0,1, // Rate 17
                0,1,1,1,0,1,1,1, // Rate 18
                0,1,1,1,1,1,1,1, // Rate 19
                0,1,0,1,0,1,0,1, // Rate 20
                0,1,0,1,1,1,0,1, // Rate 21
                0,1,1,1,0,1,1,1, // Rate 22
                0,1,1,1,1,1,1,1, // Rate 23
                0,1,0,1,0,1,0,1, // Rate 24
                0,1,0,1,1,1,0,1, // Rate 25
                0,1,1,1,0,1,1,1, // Rate 26
                0,1,1,1,1,1,1,1, // Rate 27
                0,1,0,1,0,1,0,1, // Rate 28
                0,1,0,1,1,1,0,1, // Rate 29
                0,1,1,1,0,1,1,1, // Rate 30
                0,1,1,1,1,1,1,1, // Rate 31
                0,1,0,1,0,1,0,1, // Rate 32
                0,1,0,1,1,1,0,1, // Rate 33
                0,1,1,1,0,1,1,1, // Rate 34
                0,1,1,1,1,1,1,1, // Rate 35
                0,1,0,1,0,1,0,1, // Rate 36
                0,1,0,1,1,1,0,1, // Rate 37
                0,1,1,1,0,1,1,1, // Rate 38
                0,1,1,1,1,1,1,1, // Rate 39
                0,1,0,1,0,1,0,1, // Rate 40
                0,1,0,1,1,1,0,1, // Rate 41
                0,1,1,1,0,1,1,1, // Rate 42
                0,1,1,1,1,1,1,1, // Rate 43
                0,1,0,1,0,1,0,1, // Rate 44
                0,1,0,1,1,1,0,1, // Rate 45
                0,1,1,1,0,1,1,1, // Rate 46
                0,1,1,1,1,1,1,1, // Rate 47
                1,1,1,1,1,1,1,1, // Rate 48
                1,1,1,2,1,1,1,2, // Rate 49
                1,2,1,2,1,2,1,2, // Rate 50
                1,2,2,2,1,2,2,2, // Rate 51
                2,2,2,2,2,2,2,2, // Rate 52
                2,2,2,4,2,2,2,4, // Rate 53
                2,4,2,4,2,4,2,4, // Rate 54
                2,4,4,4,2,4,4,4, // Rate 55
                4,4,4,4,4,4,4,4, // Rate 56
                4,4,4,8,4,4,4,8, // Rate 57
                4,8,4,8,4,8,4,8, // Rate 58
                4,8,8,8,4,8,8,8, // Rate 59
                8,8,8,8,8,8,8,8, // Rate 60
                8,8,8,8,8,8,8,8, // Rate 61
                8,8,8,8,8,8,8,8, // Rate 62
                8,8,8,8,8,8,8,8  // Rate 63
            };

        static readonly int[] slTable = // translates a 4-bit SL value into a 10-bit attenuation value
            {
                0x000, 0x020, 0x040, 0x060, 0x080, 0x0A0, 0x0C0, 0x0E0,
                0x100, 0x120, 0x140, 0x160, 0x180, 0x1A0, 0x1C0, 0x3FF
            };

        static readonly int[] detuneTable = 
            {
                0,  0,  1,  2,  // Key-Code 0
                0,  0,  1,  2,  // Key-Code 1
                0,  0,  1,  2,  // Key-Code 2
                0,  0,  1,  2,  // Key-Code 3
                0,  1,  2,  2,  // Key-Code 4
                0,  1,  2,  3,  // Key-Code 5
                0,  1,  2,  3,  // Key-Code 6
                0,  1,  2,  3,  // Key-Code 7
                0,  1,  2,  4,  // Key-Code 8
                0,  1,  3,  4,  // Key-Code 9
                0,  1,  3,  4,  // Key-Code 10
                0,  1,  3,  5,  // Key-Code 11
                0,  2,  4,  5,  // Key-Code 12
                0,  2,  4,  6,  // Key-Code 13
                0,  2,  4,  6,  // Key-Code 14
                0,  2,  5,  7,  // Key-Code 15
                0,  2,  5,  8,  // Key-Code 16
                0,  3,  6,  8,  // Key-Code 17
                0,  3,  6,  9,  // Key-Code 18
                0,  3,  7, 10,  // Key-Code 19
                0,  4,  8, 11,  // Key-Code 20
                0,  4,  8, 12,  // Key-Code 21
                0,  4,  9, 13,  // Key-Code 22
                0,  5, 10, 14,  // Key-Code 23
                0,  5, 11, 16,  // Key-Code 24
                0,  6, 12, 17,  // Key-Code 25
                0,  6, 13, 19,  // Key-Code 26
                0,  7, 14, 20,  // Key-Code 27
                0,  8, 16, 22,  // Key-Code 28
                0,  8, 16, 22,  // Key-Code 29
                0,  8, 16, 22,  // Key-Code 30
                0,  8, 16, 22   // Key-Code 31
            };
        #endregion

        // ====================================================================================
        //                                     Envelope Generator
        // ====================================================================================

        int egDivisorCounter; // This provides the /3 divisor to run the envelope generator once for every 3 FM sample output ticks.
        int egCycleCounter;   // This provides a rolling counter of the envelope generator update ticks. (/3 divisor already applied)

        const int MaxAttenuation = 1023;

        void MaybeRunEnvelopeGenerator()
        {
            if (egDivisorCounter == 0)
            {
                for (int c = 0; c < 6; c++)
                    for (int o = 0; o < 4; o++)
                        EnvelopeGeneratorTick(Channels[c].Operators[o]);

                egCycleCounter++;
            }

            egDivisorCounter++;
            if (egDivisorCounter == 3)
                egDivisorCounter = 0;
        }

        void EnvelopeGeneratorTick(Operator op)
        {
            // First, let's handle envelope generator phase transitions.

            if (op.EnvelopeState == EnvelopeState.Off)
                return;

            if (op.EnvelopeState != EnvelopeState.Attack && op.EgAttenuation == MaxAttenuation)
            {
                op.EnvelopeState = EnvelopeState.Off;
                return;
            }

            if (op.EnvelopeState == EnvelopeState.Attack && op.EgAttenuation == 0)
            {
                op.EnvelopeState = EnvelopeState.Decay;
                if (op.SL_SustainLevel == 0) // If Sustain Level is 0, we skip Decay and go straight to Sustain phase.
                    op.EnvelopeState = EnvelopeState.Sustain;
            }

            if (op.EnvelopeState == EnvelopeState.Decay && op.EgAttenuation >= op.Normalized10BitSL)
            {
                op.EnvelopeState = EnvelopeState.Sustain;
            }

            // At this point, we've determined what envelope phase we're in. Lets do the update.
            // Start by calculating Rate.

            int rate = 0;
            switch (op.EnvelopeState)
            {
                case EnvelopeState.Attack:  rate = op.AR_AttackRate; break;
                case EnvelopeState.Decay:   rate = op.DR_DecayRate; break;
                case EnvelopeState.Sustain: rate = op.SR_SustainRate; break;
                case EnvelopeState.Release: rate = (op.RR_ReleaseRate << 1) + 1; break;
            }

            if (rate != 0) // rate=0 is 0 no matter the value of Rks.
                rate = Math.Min((rate * 2) + op.Rks, 63);

            // Now we have rate. figure out shift value and cycle offset
            int shiftValue = egRateCounterShiftValues[rate];

            if (egCycleCounter % (1 << shiftValue) == 0)
            {
                // Update attenuation value this tick
                int updateCycleOffset = (egCycleCounter >> shiftValue) & 7; // gives the offset within the 8-step cycle
                int attenuationAdjustment = egRateIncrementValues[(rate * 8) + updateCycleOffset];

                if (op.EnvelopeState == EnvelopeState.Attack)
                    op.EgAttenuation += (~op.EgAttenuation * attenuationAdjustment) >> 4;
                else // One of the decay phases
                    op.EgAttenuation += attenuationAdjustment;
            }
        }

        static void KeyOn(Operator op)
        {
            op.PhaseCounter = 0; // Reset Phase Generator
            
            if (op.AR_AttackRate >= 30) 
            {
                
                // AR of 30 or 31 skips attack phase
                op.EgAttenuation = 0; // Force minimum attenuation

                op.EnvelopeState = EnvelopeState.Decay;
                if (op.SL_SustainLevel == 0) // If Sustain Level is 0, we skip Decay and go straight to Sustain phase.
                    op.EnvelopeState = EnvelopeState.Sustain;

            } else {

                // Regular Key-On
                op.EnvelopeState = EnvelopeState.Attack;

            }
        }

        static void KeyOff(Operator op)
        {
            op.EnvelopeState = EnvelopeState.Release;
        }

        // ====================================================================================
        //                                     Operator Unit
        // ====================================================================================

        int GetOperatorOutput(Operator op, int phaseModulationInput14)
        {
            if (op.EgAttenuation == MaxAttenuation)
                return 0;

            RunPhaseGenerator(op);
            int phase10 = op.PhaseCounter >> 10;

            // operators return a 14-bit output, but take a 10-bit input.  What 4 bits are discarded? not the obvious ones...
            // the input is shifted right by one; the least significant bit and the 3 most significant bits are discarded.
            int phaseModulationInput10 = (phaseModulationInput14 >> 1) & 0x3FF;

            phase10 += phaseModulationInput10;
            phase10 &= 0x3FF;
            
            return OperatorCalc(phase10, op.AdjustedEGOutput);
        }

        static void RunPhaseGenerator(Operator op)
        {
            // Take the Frequency Number & shift based on Block 
            int phaseIncrement = op.FrequencyNumber;
            switch (op.Block)
            {
                case 0: phaseIncrement >>= 1; break;
                case 1: break;
                default: phaseIncrement <<= op.Block - 1; break;
            }

            // Apply Detune
            int detuneAdjustment = detuneTable[(op.KeyCode * 4) + (op.DT_Detune & 3)];
            if ((op.DT_Detune & 4) != 0)
                detuneAdjustment = -detuneAdjustment;
            phaseIncrement += detuneAdjustment;
            phaseIncrement &= 0x1FFFF; // mask to 17-bits, which is the current size of the register at this point in the calculation. This allows proper detune overflow.

            // Apply MUL
            switch (op.MUL_Multiple)
            {
                case 0:  phaseIncrement /= 2; break;
                default: phaseIncrement *= op.MUL_Multiple; break;
            }

            op.PhaseCounter += phaseIncrement;
            op.PhaseCounter &= 0xFFFFF;
        }

        static int OperatorCalc(int phase10, int attenuation)
        {
            // calculate sin
            double phaseNormalized = (phase10 / 1023d);
            double sinResult = Math.Sin(phaseNormalized * Math.PI * 2);

            // convert attenuation into linear power representation
            const double attenuationIndividualBitWeighting = 48.0 / 1024.0;
            double attenuationInBels = (((double)attenuation * attenuationIndividualBitWeighting) / 10.0);
            double powerLinear = Math.Pow(10.0, -attenuationInBels);

            // attenuate result
            double resultNormalized = sinResult * powerLinear;

            // calculate 14-bit operator output
            const int maxOperatorOutput = 8191;
            return (int)(resultNormalized * maxOperatorOutput);
        }

        // ====================================================================================
        //                                      Channel Unit
        // ====================================================================================

        const int max14bitValue = 0x1FFF; // maximum signed value

        int GetChannelOutput(Channel channel, int maxVolume)
        {
            int outc = 0;

            switch (channel.Algorithm)
            {
                case 0:
                    {
                        int out1;
                        out1 = GetOperatorOutput(channel.Operators[0], 0);
                        out1 = GetOperatorOutput(channel.Operators[1], out1);
                        out1 = GetOperatorOutput(channel.Operators[2], out1);
                        outc = GetOperatorOutput(channel.Operators[3], out1);
                        break;
                    }

                case 1:
                    {
                        int out1, out2;
                        out1 = GetOperatorOutput(channel.Operators[0], 0);
                        out2 = GetOperatorOutput(channel.Operators[1], 0);
                        outc = GetOperatorOutput(channel.Operators[2], Limit(out1 + out2, -8191, 8191)); // TODO test whether these Limit calls are actually correct. technically I expect it to be overflowing in a 10-bit space.
                        outc = GetOperatorOutput(channel.Operators[3], outc);
                        break;
                    }

                case 2:
                    {
                        int out1, out2;
                        out1 = GetOperatorOutput(channel.Operators[0], 0);
                        out2 = GetOperatorOutput(channel.Operators[1], 0);
                        out2 = GetOperatorOutput(channel.Operators[2], out2);
                        outc = GetOperatorOutput(channel.Operators[3], Limit(out1 + out2, -8191, 8191));
                        break;
                    }

                case 3:
                    {
                        int out1, out2;
                        out1 = GetOperatorOutput(channel.Operators[0], 0);
                        out1 = GetOperatorOutput(channel.Operators[1], out1);
                        out2 = GetOperatorOutput(channel.Operators[2], 0);
                        outc = GetOperatorOutput(channel.Operators[3], Limit(out1 + out2, -8191, 8191));
                        break;
                    }

                case 4:
                    {
                        int out1, out2;
                        out1 = GetOperatorOutput(channel.Operators[0], 0);
                        out1 = GetOperatorOutput(channel.Operators[1], out1);
                        out2 = GetOperatorOutput(channel.Operators[2], 0);
                        out2 = GetOperatorOutput(channel.Operators[3], out2);
                        outc = Limit(out1 + out2, -8191, 8191);
                        break;
                    }

                case 5:
                    {
                        int out1, out2, out3, out4;
                        out1 = GetOperatorOutput(channel.Operators[0], 0);
                        out2 = GetOperatorOutput(channel.Operators[1], out1);
                        out3 = GetOperatorOutput(channel.Operators[2], out1);
                        out4 = GetOperatorOutput(channel.Operators[3], out1);
                        outc = Limit(out2 + out3 + out4, -8191, 8191);
                        break;
                    }

                case 6:
                    {
                        int out1, out2, out3, out4;
                        out1 = GetOperatorOutput(channel.Operators[0], 0);
                        out2 = GetOperatorOutput(channel.Operators[1], out1);
                        out3 = GetOperatorOutput(channel.Operators[2], out1);
                        out4 = GetOperatorOutput(channel.Operators[3], out1);
                        outc = Limit(out2 + out3 + out4, -8191, 8191);
                        break;
                    }

                case 7:
                    {
                        int out1, out2, out3, out4;
                        out1 = GetOperatorOutput(channel.Operators[0], 0);
                        out2 = GetOperatorOutput(channel.Operators[1], out1);
                        out3 = GetOperatorOutput(channel.Operators[2], 0);
                        out4 = GetOperatorOutput(channel.Operators[3], 0);
                        outc = Limit(out2 + out3 + out4, -8191, 8191);
                        break;
                    }
            }

            return outc * maxVolume / max14bitValue;
        }

        static int Limit(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        // ====================================================================================
        //                               Support Classes/Structs/Enums
        // ====================================================================================

        public enum EnvelopeState
        {
            Attack,
            Decay,
            Sustain,
            Release,
            Off
        }

        public sealed class Operator
        {
            // External Settings

            public int TL_TotalLevel;                                   // 7 bits
            public int SL_SustainLevel;                                 // 4 bits
            public int AR_AttackRate;                                   // 5 bits
            public int DR_DecayRate;                                    // 5 bits
            public int SR_SustainRate;                                  // 5 bits
            public int RR_ReleaseRate;                                  // 4 bits
            public int KS_KeyScale;                                     // 2 bits
            public int SSG_EG;                                          // 4 bits

            public int DT_Detune;                                       // 3 bits
            public int MUL_Multiple;                                    // 4 bits

            public bool AM_AmplitudeModulation;                         // 1 bit

            public int FrequencyNumber;                                 // 11 bits
            public int Block;                                           // 3 bits
            
            public int KeyCode;                                         // 5 bits (described on pg 25 of YM2608 docs)
            public int Rks;                                             // 5 bits (described on pg 29 of YM2608 docs)

            // Internal State

            public int PhaseCounter;                                    // 20 bits, where the 10 most significant bits are output to the operator.

            public EnvelopeState EnvelopeState = EnvelopeState.Off;

            private int egAttenuation = MaxAttenuation;                 // 10-bit attenuation value output from envelope generator
            public int EgAttenuation
            {
                get { return egAttenuation; }
                set
                {
                    egAttenuation = value;
                    if (egAttenuation < 0)    egAttenuation = 0;
                    if (egAttenuation > 1023) egAttenuation = 1023;
                }
            }

            public int Normalized10BitSL { get { return slTable[SL_SustainLevel]; } }
            public int Normalized10BitTL { get { return TL_TotalLevel << 3; } }
            public int AdjustedEGOutput  { get { return Math.Min(egAttenuation + Normalized10BitTL, 1023); } }
        }

        public sealed class Channel
        {
            public readonly Operator[] Operators = { new Operator(), new Operator(), new Operator(), new Operator() };

            public int FrequencyNumber;                                 // 11 bits
            public int Block;                                           // 3 bits
            public int Feedback;                                        // 3 bits
            public int Algorithm;                                       // 3 bits (algorithms 0 - 7)

            public bool SpecialMode;                                    // TODO, there are 2 special modes, a bool is not going to do the trick.
            public bool LeftOutput = true;                              // These apparently need to be initialized on.
            public bool RightOutput = true;                             // These apparently need to be initialized on.

            public int AMS_AmplitudeModulationSensitivity;              // 3 bits
            public int FMS_FrequencyModulationSensitivity;              // 2 bits
        }

        // ====================================================================================
        //                                     ISoundProvider
        // ====================================================================================

        public void GetSamples(short[] samples)
        {
            // Generate raw samples at native sampling rate (~53hz)
            int numStereoSamples = samples.Length / 2;
            int nativeStereoSamples = (int)(numStereoSamples * ntsc44100Factor) * 2;
            short[] nativeSamples = new short[nativeStereoSamples];
            GetSamplesNative(nativeSamples);

            // downsample from native output rate to 44100.
            //CrappyNaiveResampler(nativeSamples, samples);
            MaybeBetterDownsampler(nativeSamples, samples);
        }

        static void CrappyNaiveResampler(short[] input, short[] output)
        {
            // this is not good resampling code.
            int numStereoSamples = output.Length / 2;
            
            int offset = 0;
            for (int i = 0; i < numStereoSamples; i++)
            {
                int nativeOffset = ((i * ntscOutputRate) / 44100) * 2;
                output[offset++] += input[nativeOffset++]; // left
                output[offset++] += input[nativeOffset];   // right
            }
        }

        static double Fraction(double value)
        {
            return value - Math.Floor(value);
        }

        static void MaybeBetterDownsampler(short[] input, short[] output)
        {
            // This is still not a good resampler. But it's better than the other one. Unsure how much difference it makes.
            // The difference with this one is that all source samples will be sampled and weighted, none skipped over.
            double nativeSamplesPerOutputSample = (double) input.Length / (double) output.Length;
            int outputStereoSamples = output.Length / 2;
            int inputStereoSamples = input.Length / 2;

            int offset = 0;
            for (int i = 0; i < outputStereoSamples; i++)
            {
                
                double startSample = nativeSamplesPerOutputSample * i;
                double endSample   = nativeSamplesPerOutputSample * (i+1);

                int iStartSample = (int) Math.Floor(startSample);
                int iEndSample = (int) Math.Floor(endSample);
                double leftSample = 0;
                double rightSample = 0;
                for (int j = iStartSample; j <= iEndSample; j++)
                {
                    if (j == inputStereoSamples) 
                        break;

                    double weight = 1.0;

                    if (j == iStartSample)
                        weight = 1.0 - Fraction(startSample);
                    else if (j == iEndSample)
                        weight = Fraction(endSample);

                    leftSample  += ((double) input[(j * 2) + 0] * weight);
                    rightSample += ((double) input[(j * 2) + 1] * weight);
                }
                output[offset++] = (short) leftSample;
                output[offset++] = (short) rightSample;
            }
        }

        void GetSamplesNative(short[] samples)
        {
            int elapsedCycles = frameEndClock - frameStartClock;
            int start = 0;
            while (commands.Count > 0)
            {
                var cmd = commands.Dequeue();
                int pos = ((cmd.Clock * samples.Length) / elapsedCycles) & ~1;
                GetSamplesImmediate(samples, start, pos - start);
                start = pos;
                WriteCommand(cmd);
            }
            GetSamplesImmediate(samples, start, samples.Length - start);
        }

        void GetSamplesImmediate(short[] samples, int pos, int length)
        {
            int channelVolume = MaxVolume / 6;

            for (int i = 0; i < length / 2; i++)
            {
                MaybeRunEnvelopeGenerator();

                // Generate FM output
                for (int ch = 0; ch < 6; ch++)
                {
                    short sample = (short)GetChannelOutput(Channels[ch], channelVolume);

                    if (ch < 5 || DacEnable == false)
                    {
                        if (Channels[ch].LeftOutput) samples[pos] += sample;
                        if (Channels[ch].RightOutput) samples[pos + 1] += sample;
                    }
                    else
                    {
                        short dacValue = (short)(((DacValue - 80) * channelVolume) / 80);
                        if (Channels[5].LeftOutput) samples[pos] += dacValue;
                        if (Channels[5].RightOutput) samples[pos + 1] += dacValue;
                    }
                }
                pos += 2;
            }
        }

        public void DiscardSamples() { }
        public int MaxVolume { get; set; }
    }
}