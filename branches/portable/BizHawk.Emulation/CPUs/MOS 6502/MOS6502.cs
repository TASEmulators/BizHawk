using System;
using System.Globalization;
using System.IO;

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
		public bool throw_unhandled;

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

		public const ushort NMIVector = 0xFFFA;
		public const ushort ResetVector = 0xFFFC;
		public const ushort BRKVector = 0xFFFE;
		public const ushort IRQVector = 0xFFFE;

		enum ExceptionType
		{
			BRK, NMI, IRQ
		}

		void TriggerException(ExceptionType type)
		{
			if (type == ExceptionType.BRK)
				PC++;
			WriteMemory((ushort)(S-- + 0x100), (byte)(PC >> 8));
			WriteMemory((ushort)(S-- + 0x100), (byte)PC);
			FlagB = type == ExceptionType.BRK;
			WriteMemory((ushort)(S-- + 0x100), P);
			FlagI = true;
			switch (type)
			{
				case ExceptionType.NMI:
					PC = ReadWord(NMIVector);
					break;
				case ExceptionType.IRQ:
					PC = ReadWord(IRQVector);
					break;
				case ExceptionType.BRK:
					PC = ReadWord(BRKVector);
					break;
				default: throw new Exception();
			}
			PendingCycles -= 7;
		}

        // ==== CPU State ====

        public byte A;
        public byte X;
        public byte Y;
        public byte P;
        public ushort PC;
        public byte S;

        public bool IRQ;
        public bool NMI;
		public bool CLI_Pending;
		public bool SEI_Pending;

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("MOS6502");
			ser.Sync("A", ref A);
			ser.Sync("X", ref X);
			ser.Sync("Y", ref Y);
			ser.Sync("P", ref P);
			ser.Sync("PC", ref PC);
			ser.Sync("S", ref S);
			ser.Sync("NMI", ref NMI);
			ser.Sync("IRQ", ref IRQ);
			ser.Sync("CLI_Pending", ref CLI_Pending);
			ser.Sync("SEI_Pending", ref SEI_Pending);
			ser.Sync("TotalExecutedCycles", ref TotalExecutedCycles);
			ser.Sync("PendingCycles", ref PendingCycles);
			ser.EndSection();
		}

		public void SaveStateBinary(BinaryWriter writer) { SyncState(Serializer.CreateBinaryWriter(writer)); }
		public void LoadStateBinary(BinaryReader reader) { SyncState(Serializer.CreateBinaryReader(reader)); }

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
        public bool FlagI
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

        public ushort ReadWord(ushort address)
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