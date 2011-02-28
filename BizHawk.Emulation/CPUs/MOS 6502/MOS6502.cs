using System;

namespace BizHawk.Emulation.CPUs.M6502
{
    public sealed partial class MOS6502
    {
        public MOS6502()
        {
            //InitTableNZ();
            Reset();
        }
/*
        private byte[] TableNZ;
        private void InitTableNZ()
        {
            TableNZ = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                byte b = 0;
                if (i == 0) b |= 0x02;
                if (i > 127) b |= 0x80;
                TableNZ[i] = b;
            }
        }*/

		public bool debug;

        public void Reset()
        {
            A = 0;
            X = 0;
            Y = 0;
            P = 0;
            S = 0;
            PC = 0;
            PendingCycles = 0;
            TotalExecutedCycles = 0;
        }

        public void ResetPC()
        {
            PC = ReadWord(0xFFFE);
        }

        public string State()
        {
            int notused;
            string a = string.Format("{0:X4}  {1:X2} {2} ", PC, ReadMemory(PC), Disassemble(PC, out notused)).PadRight(30);
            string b = string.Format("A:{0:X2} X:{1:X2} Y:{2:X2} P:{3:X2} SP:{4:X2} Cy:{5}", A, X, Y, P, S, TotalExecutedCycles);
            string val = a + b + "   ";
            if (FlagN) val = val + "N";
            if (FlagV) val = val + "V";
            if (FlagT) val = val + "T";
            if (FlagB) val = val + "B";
            if (FlagD) val = val + "D";
            if (FlagI) val = val + "I";
            if (FlagZ) val = val + "Z";
            if (FlagC) val = val + "C";
            return val;
        }

        // ==== CPU State ====

        public byte A;
        public byte X;
        public byte Y;
        public byte P;
        public ushort PC;
        public byte S;

        // TODO IRQ, NMI functions
        public bool Interrupt;
        public bool NMI;

		private const ushort NMIVector = 0xFFFA;
		private const ushort ResetVector = 0xFFFC;
		private const ushort BRKVector = 0xFFFE;

		enum ExceptionType
		{
			BRK, NMI
		}

		void TriggerException(ExceptionType type)
		{
			WriteMemory((ushort)(S-- + 0x100), (byte)(PC >> 8));
			WriteMemory((ushort)(S-- + 0x100), (byte)PC);
			byte oldP = P;
			FlagB = false;
			FlagT = true;
			WriteMemory((ushort)(S-- + 0x100), P);
			P = oldP;
			FlagI = true;
			if(type == ExceptionType.NMI)
				PC = ReadWord(NMIVector);
			else
				PC = ReadWord(BRKVector);
			PendingCycles -= 7;
		}

        // ==== End State ====

        /// <summary>Carry Flag</summary>
        private bool FlagC
        {
            get { return (P & 0x01) != 0; }
            set { P = (byte)((P & ~0x01) | (value ? 0x01 : 0x00)); }
        }

        /// <summary>Zero Flag</summary>
        private bool FlagZ
        {
            get { return (P & 0x02) != 0; }
            set { P = (byte)((P & ~0x02) | (value ? 0x02 : 0x00)); }
        }

        /// <summary>Interrupt Disable Flag</summary>
        private bool FlagI
        {
            get { return (P & 0x04) != 0; }
            set { P = (byte)((P & ~0x04) | (value ? 0x04 : 0x00)); }
        }

        /// <summary>Decimal Mode Flag</summary>
        private bool FlagD
        {
            get { return (P & 0x08) != 0; }
            set { P = (byte)((P & ~0x08) | (value ? 0x08 : 0x00)); }
        }

        /// <summary>Break Flag</summary>
        private bool FlagB
        {
            get { return (P & 0x10) != 0; }
            set { P = (byte)((P & ~0x10) | (value ? 0x10 : 0x00)); }
        }

        /// <summary>T... Flag</summary>
        private bool FlagT
        {
            get { return (P & 0x20) != 0; }
            set { P = (byte)((P & ~0x20) | (value ? 0x20 : 0x00)); }
        }

        /// <summary>Overflow Flag</summary>
        private bool FlagV
        {
            get { return (P & 0x40) != 0; }
            set { P = (byte)((P & ~0x40) | (value ? 0x40 : 0x00)); }
        }

        /// <summary>Negative Flag</summary>
        private bool FlagN
        {
            get { return (P & 0x80) != 0; }
            set { P = (byte)((P & ~0x80) | (value ? 0x80 : 0x00)); }
        }

        public int TotalExecutedCycles;
        public int PendingCycles;

        public Func<ushort, byte> ReadMemory;
        public Action<ushort, byte> WriteMemory;

        public void UnregisterMemoryMapper()
        {
            ReadMemory = null;
            WriteMemory = null;
        }

        private ushort ReadWord(ushort address)
        {
            byte l = ReadMemory(address);
            byte h = ReadMemory(++address);
            return (ushort)((h << 8) | l);
        }

        private void WriteWord(ushort address, ushort value)
        {
            byte l = (byte)(value & 0xFF);
            byte h = (byte)(value >> 8);
            WriteMemory(address, l);
            WriteMemory(++address, h);
        }

        private ushort ReadWordPageWrap(ushort address)
        {
            ushort highAddress = (ushort)((address & 0xFF00) + ((address + 1) & 0xFF));
            return (ushort)(ReadMemory(address) | (ReadMemory(highAddress) << 8));
        }

        private static readonly byte[] TableNZ = 
        { 
            0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80
        };
    }
}