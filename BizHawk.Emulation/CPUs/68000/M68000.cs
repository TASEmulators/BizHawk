using System;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.CPUs.M68K
{
    public sealed partial class M68000
    {
        // Machine State
        public Register[] D = new Register[8];
        public Register[] A = new Register[8];
        public int PC;

        public int TotalExecutedCycles;
        public int PendingCycles;

        // Status Registers
        private int InterruptMaskLevel;

        private bool s, m;
        private int usp, ssp;
        
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

        public M68000()
        {
            BuildOpcodeTable();
        }

        public void Reset()
        {
            s = true;
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
                //Console.WriteLine(Disassemble(PC));
                op = (ushort)ReadWord(PC);
                PC += 2;
                Opcodes[op]();
            }
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
