using System;
using System.Diagnostics;

namespace BizHawk.Emulation.Sound
{
    public sealed partial class YM2612 : ISoundProvider
    {
        public readonly Channel[] Channels;
        
        int frameStartClock;
        int frameEndClock;

        public YM2612()
        {
            Channels = new Channel[6];
            Channels[0] = new Channel();
            Channels[1] = new Channel();
            Channels[2] = new Channel();
            Channels[3] = new Channel();
            Channels[4] = new Channel();
            Channels[5] = new Channel();
        }

        public void Reset()
        {
        }

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

        public void DiscardSamples() { }        
        public int MaxVolume { get; set; }

        public void GetSamples(short[] samples) 
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

            for (int i=0; i<length/2; i++)
            {
                // TODO: channels 1-5
                // TODO, non-DAC

                if (DacEnable)
                {
                    short dacValue = (short)(((DacValue-80) * channelVolume) / 80);
                    samples[pos] += dacValue;
                    samples[pos + 1] += dacValue;
                }
                pos += 2;
            }

        }

        // Pg 8 major post w/ info on clock rates, envelope generator, loudness
        // Note: part about EG clock is wrong, see pg 12. :|
        // pg 27 contains further sets of corrections to previous pages. Jesus. Including data on SSG-EG.
        // pg 32 contains some details about the LFO
        // pg 33 paul jensens contains frequency conversion. gmaniac corrected. 
        // pg 33 blargg comments on DAC size - depends on if I WANT to emulate the inaccuracies of the DAC or a hypotheticaly perfect YM2612. more on DAC on page 37 - contradicting blargg.

      /*  
Chilly Willy wrote:
Hmm - does the FM channel still update even if it is set to PCM?

Yes, it does. FM Channel 6 and Reg $2A are independent of each other. "DAC mode" (Reg $2B) only chose output of one of them. If it is in "FM Mode" it outputs Channel 6 output. If it is in "DAC Mode", it outputs value stored in Reg $2A and normalized to scale of DAC (-512 to + 512). Normalization formula is 
(v - $80) * 4 
where v = ($2A). 

I tested in on HW.
    */
    }
}