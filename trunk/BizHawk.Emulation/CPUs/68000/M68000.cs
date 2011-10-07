using System;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.CPUs.M68K
{
    public sealed partial class MC68000
    {
        // Machine State
        public Register[] D = new Register[8];
        public Register[] A = new Register[8];
        public int PC;

        public int TotalExecutedCycles;
        public int PendingCycles;

        // Status Registers
        int InterruptMaskLevel;

        bool s, m;
        int usp, ssp;
        
        /// <summary>Machine/Interrupt mode</summary>
        public bool M { get { return m; } set { m = value; } } // TODO probably have some switch logic maybe
        
        /// <summary>Supervisor/User mode</summary>
        public bool S
        {
            get { return s; }
            set
            {
                if (value == s) return;
                if (value == true) // entering supervisor mode
                {
                    Console.WriteLine("&^&^&^&^& ENTER SUPERVISOR MODE");
                    usp = A[7].s32;
                    A[7].s32 = ssp;
                    s = true;
                } else { // exiting supervisor mode
                    Console.WriteLine("&^&^&^&^& LEAVE SUPERVISOR MODE");
                    ssp = A[7].s32;
                    A[7].s32 = usp;
                }
            }
        }

        /// <summary>Extend Flag</summary>
        public bool X;
        /// <summary>Negative Flag</summary>
        public bool N;
        /// <summary>Zero Flag</summary>
        public bool Z;
        /// <summary>Overflow Flag</summary>
        public bool V;
        /// <summary>Carry Flag</summary>
        public bool C;

        /// <summary>Status Register</summary>
        public int SR
        {
            get
            {
                int value = 0;
                if (C) value |= 0x0001;
                if (V) value |= 0x0002;
                if (Z) value |= 0x0004;
                if (N) value |= 0x0008;
                if (X) value |= 0x0010;
                if (M) value |= 0x1000;
                if (S) value |= 0x2000;
                value |= (InterruptMaskLevel & 7) << 8;
                return value;
            }
            set
            {
                C = (value & 0x0001) != 0;
                V = (value & 0x0002) != 0;
                Z = (value & 0x0004) != 0;
                N = (value & 0x0008) != 0; 
                X = (value & 0x0010) != 0;
                M = (value & 0x1000) != 0;
                S = (value & 0x2000) != 0;
                InterruptMaskLevel = (value >> 8) & 7;
            }
        }

        // Memory Access
        public Func<int, sbyte> ReadByte;
        public Func<int, short> ReadWord;
        public Func<int, int>   ReadLong;

        public Action<int, sbyte> WriteByte;
        public Action<int, short> WriteWord;
        public Action<int, int>   WriteLong;

        // Initialization

        public MC68000()
        {
            BuildOpcodeTable();
        }

        public void Reset()
        {
            S = true;
            InterruptMaskLevel = 7;
            A[7].s32 = ReadLong(0);
            PC = ReadLong(4);
        }

        public Action[] Opcodes = new Action[0x10000];
        public ushort op;

        public void Step()
        {
            Console.WriteLine(Disassemble(PC));

            op = (ushort) ReadWord(PC);
            PC += 2;
            Opcodes[op]();
        }

        public void ExecuteCycles(int cycles)
        {
            PendingCycles += cycles;
            while (PendingCycles > 0)
            {
                int prevCycles = PendingCycles;
                Log.Note("CPU", State());
                op = (ushort)ReadWord(PC);
                PC += 2;
                Opcodes[op]();
                int delta = prevCycles - PendingCycles;
                TotalExecutedCycles += delta;
            }
        }

        public string State()
        {
            string a = Disassemble(PC).ToString().PadRight(64);
            string b = string.Format("D0:{0:X8} D1:{1:X8} D2:{2:X8} D3:{3:X8} D4:{4:X8} D5:{5:X8} D6:{6:X8} D7:{7:X8} ", D[0].u32, D[1].u32, D[2].u32, D[3].u32, D[4].u32, D[5].u32, D[6].u32, D[7].u32);
            string c = string.Format("A0:{0:X8} A1:{1:X8} A2:{2:X8} A3:{3:X8} A4:{4:X8} A5:{5:X8} A6:{6:X8} A7:{7:X8} ", A[0].u32, A[1].u32, A[2].u32, A[3].u32, A[4].u32, A[5].u32, A[6].u32, A[7].u32);
            string d = string.Format("SR:{0:X4} Pending {1} Cycles {2}", SR, PendingCycles, TotalExecutedCycles);
            return a + b + c + d;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Register
    {
        [FieldOffset(0)]
        public uint u32;
        [FieldOffset(0)]
        public int s32;

        [FieldOffset(0)]
        public ushort u16;
        [FieldOffset(0)]
        public short s16;

        [FieldOffset(0)]
        public byte u8;
        [FieldOffset(0)]
        public sbyte s8;

        public override string ToString()
        {
            return String.Format("{0:X8}", u32);
        }
    }
}
