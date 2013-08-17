using System;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	// base class for MOS Technology timer chips

	public abstract class Timer
	{
        public byte PortAMask = 0xFF;
        public byte PortBMask = 0xFF;

		protected bool pinIRQ;
        protected LatchedPort portA;
        protected LatchedPort portB;
        protected int[] timer;
		protected int[] timerLatch;
		protected bool[] timerOn;
		protected bool[] underflow;

		public Func<byte> ReadPortA = (() => { return 0xFF; });
		public Func<byte> ReadPortB = (() => { return 0xFF; });

		public Timer()
		{
            portA = new LatchedPort();
            portB = new LatchedPort();
			timer = new int[2];
			timerLatch = new int[2];
			timerOn = new bool[2];
			underflow = new bool[2];
		}

		protected void HardResetInternal()
		{
			timer[0] = 0xFFFF;
			timer[1] = 0xFFFF;
			timerLatch[0] = timer[0];
			timerLatch[1] = timer[1];
			pinIRQ = true;
		}

        public byte PortAData
        {
            get
            {
                return portA.ReadOutput();
            }
        }

        public byte PortADirection
        {
            get
            {
                return portA.Direction;
            }
        }

        public byte PortALatch
        {
            get
            {
                return portA.Latch;
            }
        }

        public byte PortBData
        {
            get
            {
                return portB.ReadOutput();
            }
        }

        public byte PortBDirection
        {
            get
            {
                return portB.Direction;
            }
        }

        public byte PortBLatch
        {
            get
            {
                return portB.Latch;
            }
        }

        public bool ReadIRQBuffer() { return pinIRQ; }

		protected void SyncInternal(Serializer ser)
		{
			ser.Sync("pinIRQ", ref pinIRQ);
			ser.Sync("timer0", ref timer[0]);
			ser.Sync("timer1", ref timer[1]);
			ser.Sync("timerLatch0", ref timerLatch[0]);
			ser.Sync("timerLatch1", ref timerLatch[1]);
			ser.Sync("timerOn0", ref timerOn[0]);
			ser.Sync("timerOn1", ref timerOn[1]);
			ser.Sync("underflow0", ref underflow[0]);
			ser.Sync("underflow1", ref underflow[1]);
		}
	}
}
