using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Sound.Utilities
{
    /// <summary>
    /// provides pass-through sound for the dumping tool to use, while making a "best effort"
    /// to have something available for audio output
    /// </summary>
    public class DualSound : ISoundProvider
    {
        /// <summary>
        /// implementation of a "slave" ISoundProvider that recieves best effort audio
        /// </summary>
        class SecondPin : ISoundProvider
        {
            /// <summary>
            /// the source to draw from
            /// </summary>
            DualSound master;

            public SecondPin(DualSound master)
            {
                this.master = master;
            }

            public void GetSamples(short[] samples)
            {
                int i;
                for (i = 0; i < Math.Min(samples.Length, master.ringbuffer.Count); i++)
                    samples[i] = master.ringbuffer.Dequeue();
                for (; i < samples.Length; i++)
                    // underflow
                    samples[i] = 0;
            }

            public void DiscardSamples()
            {
                master.ringbuffer.Clear();
            }

            public int MaxVolume
            {
                // ignored
                get;
                set;
            }
        }

        /// <summary>
        /// original input source
        /// </summary>
        ISoundProvider input;

        /// <summary>
        /// threshold at which to discard samples
        /// </summary>
        int killsize;

        /// <summary>
        /// storage of samples waiting to go to second pin
        /// </summary>
        Queue<short> ringbuffer;

        /// <summary>
        /// get the slave pin
        /// </summary>
        public ISoundProvider secondpin
        {
            get;
            private set;
        }

        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="input">the ISoundProvider to use as input</param>
        /// <param name="buffsize">how many sample pairs to save for the second pin</param>
        public DualSound(ISoundProvider input, int buffsize)
        {
            this.input = input;
            killsize = buffsize * 2;
            ringbuffer = new Queue<short>(killsize);
            secondpin = new SecondPin(this);
        }

        public void GetSamples(short[] samples)
        {
            input.GetSamples(samples);
            if (ringbuffer.Count >= killsize)
                ringbuffer.Clear();
            foreach (var sample in samples)
                ringbuffer.Enqueue(sample);
        }

        public void DiscardSamples()
        {
            throw new Exception("Dumpers should never discard samples!");
        }

        public int MaxVolume
        {
            get { return input.MaxVolume; }
            set { input.MaxVolume = value; }
        }
    }
}
