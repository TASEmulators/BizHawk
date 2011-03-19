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

		private const ushort NMIVector = 0xFFFA;
		private const ushort ResetVector = 0xFFFC;
		private const ushort BRKVector = 0xFFFE;
		private const ushort IRQVector = 0xFFFE;

		enum ExceptionType
		{
			BRK, NMI, IRQ
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

		public void SaveStateText(TextWriter writer)
		{
			writer.WriteLine("[MOS6502]");
			writer.WriteLine("A {0:X2}", A);
			writer.WriteLine("X {0:X2}", X);
			writer.WriteLine("Y {0:X2}", Y);
			writer.WriteLine("P {0:X2}", P);
			writer.WriteLine("PC {0:X4}", PC);
			writer.WriteLine("S {0:X2}", S);
			writer.WriteLine("NMI {0}", NMI);
			writer.WriteLine("IRQ {0}", IRQ);
			writer.WriteLine("TotalExecutedCycles {0}", TotalExecutedCycles);
			writer.WriteLine("PendingCycles {0}", PendingCycles);
			writer.WriteLine("[/MOS6502]\n");
		}

		public void LoadStateText(TextReader reader)
		{
			while (true)
			{
				string[] args = reader.ReadLine().Split(' ');
				if (args[0].Trim() == "") continue;
				if (args[0] == "[/MOS6502]") break;
				if (args[0] == "A")
					A = byte.Parse(args[1], NumberStyles.HexNumber);
				else if (args[0] == "X")
					X = byte.Parse(args[1], NumberStyles.HexNumber);
				else if (args[0] == "Y")
					Y = byte.Parse(args[1], NumberStyles.HexNumber);
				else if (args[0] == "P")
					P = byte.Parse(args[1], NumberStyles.HexNumber);
				else if (args[0] == "PC")
					PC = ushort.Parse(args[1], NumberStyles.HexNumber);
				else if (args[0] == "S")
					S = byte.Parse(args[1], NumberStyles.HexNumber);
				else if (args[0] == "NMI")
					NMI = bool.Parse(args[1]);
				else if (args[0] == "IRQ")
					IRQ = bool.Parse(args[1]);
				else if (args[0] == "TotalExecutedCycles")
					TotalExecutedCycles = int.Parse(args[1]);
				else if (args[0] == "PendingCycles")
					PendingCycles = int.Parse(args[1]);
				else
					Console.WriteLine("Skipping unrecognized identifier " + args[0]);
			}
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