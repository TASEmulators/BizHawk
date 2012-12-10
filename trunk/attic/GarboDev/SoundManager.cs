namespace GarboDev
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class SoundManager
    {
        private Memory memory = null;
        private Queue<byte>[] soundQueue = new Queue<byte>[2];
        private byte latchedA, latchedB;
        private int frequency, cyclesPerSample;
        private int leftover = 0;

        private short[] soundBuffer = new short[40000];
        private int soundBufferPos = 0;
        private int lastSoundBufferPos = 0;

        public SoundManager(Memory memory, int frequency)
        {
            this.Frequency = frequency;

            this.memory = memory;
            this.memory.SoundManager = this;

            this.soundQueue[0] = new Queue<byte>(32);
            this.soundQueue[1] = new Queue<byte>(32);
        }

        #region Public Properties
        public int Frequency
        {
            get { return this.frequency; }
            set
            {
                this.frequency = value;
                this.cyclesPerSample = (GbaManager.cpuFreq << 5) / this.frequency;
            }
        }

        public int QueueSizeA
        {
            get { return this.soundQueue[0].Count; }
        }

        public int QueueSizeB
        {
            get { return this.soundQueue[1].Count; }
        }

        public int SamplesMixed
        {
            get
            {
                int value = this.soundBufferPos - this.lastSoundBufferPos;
                if (value < 0) value += this.soundBuffer.Length;
                return value;
            }
        }
        #endregion

        #region Public Methods
        public void GetSamples(short[] buffer, int length)
        {
            for (int i = 0; i < length; i++)
            {
                if (this.lastSoundBufferPos == this.soundBuffer.Length)
                {
                    this.lastSoundBufferPos = 0;
                }
                buffer[i] = this.soundBuffer[this.lastSoundBufferPos++];
            }
        }

        public void Mix(int cycles)
        {
            ushort soundCntH = Memory.ReadU16(this.memory.IORam, Memory.SOUNDCNT_H);
            ushort soundCntX = Memory.ReadU16(this.memory.IORam, Memory.SOUNDCNT_X);

            cycles <<= 5;
            cycles += this.leftover;

            if (cycles > 0)
            {
                // Precompute loop invariants
                short directA = (short)(sbyte)(this.latchedA);
                short directB = (short)(sbyte)(this.latchedB);

                if ((soundCntH & (1 << 2)) == 0)
                {
                    directA >>= 1;
                }
                if ((soundCntH & (1 << 3)) == 0)
                {
                    directB >>= 1;
                }

                while (cycles > 0)
                {
                    short l = 0, r = 0;

                    cycles -= this.cyclesPerSample;

                    // Mixing
                    if ((soundCntX & (1 << 7)) != 0)
                    {
                        if ((soundCntH & (1 << 8)) != 0)
                        {
                            r += directA;
                        }
                        if ((soundCntH & (1 << 9)) != 0)
                        {
                            l += directA;
                        }
                        if ((soundCntH & (1 << 12)) != 0)
                        {
                            r += directB;
                        }
                        if ((soundCntH & (1 << 13)) != 0)
                        {
                            l += directB;
                        }
                    }

                    if (this.soundBufferPos == this.soundBuffer.Length)
                    {
                        this.soundBufferPos = 0;
                    }

                    this.soundBuffer[this.soundBufferPos++] = (short)(l << 6);
                    this.soundBuffer[this.soundBufferPos++] = (short)(r << 6);
                }
            }

            this.leftover = cycles;
        }

        public void ResetFifoA()
        {
            this.soundQueue[0].Clear();
            this.latchedA = 0;
        }

        public void ResetFifoB()
        {
            this.soundQueue[1].Clear();
            this.latchedB = 0;
        }

        public void IncrementFifoA()
        {
            for (int i = 0; i < 4; i++)
            {
                this.EnqueueDSoundSample(0, this.memory.IORam[Memory.FIFO_A_L + i]);
            }
        }

        public void IncrementFifoB()
        {
            for (int i = 0; i < 4; i++)
            {
                this.EnqueueDSoundSample(1, this.memory.IORam[Memory.FIFO_B_L + i]); 
            }
        }

        public void DequeueA()
        {
            if (this.soundQueue[0].Count > 0)
            {
                this.latchedA = this.soundQueue[0].Dequeue();
            }
        }

        public void DequeueB()
        {
            if (this.soundQueue[1].Count > 0)
            {
                this.latchedB = this.soundQueue[1].Dequeue();
            }
        }
        #endregion Public Methods

        private void EnqueueDSoundSample(int channel, byte sample)
        {
            if (this.soundQueue[channel].Count < 32)
            {
                this.soundQueue[channel].Enqueue(sample);
            }
        }
    }
}
