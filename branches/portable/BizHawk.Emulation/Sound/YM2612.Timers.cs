using System;

namespace BizHawk.Emulation.Sound
{
    // The master clock on the genesis is 53,693,175 MCLK / sec (NTSC)
    //                                    53,203,424 MCLK / sec (PAL)
    //                                     7,670,454 68K cycles / sec (7 MCLK divisor)
    //                                     3,579,545 Z80 cycles / sec (15 MCLK divisor)

    // YM2612 is fed by EXT CLOCK:         7,670,454 ECLK / sec (NTSC)
    //  (Same clock on 68000)              7,600,489 ECLK / sec (PAL)

    // YM2612 has /6 divisor on the EXT CLOCK.
    // YM2612 takes 24 cycles to generate a sample. 6*24 = 144. This is where the /144 divisor comes from.
    // YM2612 native output rate is 7670454 / 144 = 53267 hz (NTSC), 52781 hz (PAL)

    // Timer A ticks at the native output rate (53267 times per second for NTSC).
    // Timer B ticks down with a /16 divisor. (3329 times per second for NTSC).

    // Ergo, Timer A ticks every 67.2 Z80 cycles. Timer B ticks every 1075.2 Z80 cycles.

    public partial class YM2612
    {
        const float timerAZ80Factor = 67.2f;
        const float timerBZ80Factor = 1075.2f;

        int  TimerAPeriod,     TimerBPeriod;
        bool TimerATripped,    TimerBTripped;
        int  TimerAResetClock, TimerBResetClock;
        int  TimerALastReset,  TimerBLastReset;

        byte TimerControl27;
        bool TimerALoad   { get { return (TimerControl27 & 1)  != 0; } }
        bool TimerBLoad   { get { return (TimerControl27 & 2)  != 0; } }
        bool TimerAEnable { get { return (TimerControl27 & 4)  != 0; } }
        bool TimerBEnable { get { return (TimerControl27 & 8)  != 0; } }
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
    }
}