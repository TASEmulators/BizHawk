using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Globalization;

namespace BizHawk.Emulation.CPUs.M68000
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
                    s = false;
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
        public short SR
        {
            get
            {
                short value = 0;
                if (C) value |= 0x0001;
                if (V) value |= 0x0002;
                if (Z) value |= 0x0004;
                if (N) value |= 0x0008;
                if (X) value |= 0x0010;
                if (M) value |= 0x1000;
                if (S) value |= 0x2000;
                value |= (short) ((InterruptMaskLevel & 7) << 8);
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

        public int Interrupt { get; set; }

        // Memory Access
        public Func<int, sbyte> ReadByte;
        public Func<int, short> ReadWord;
        public Func<int, int>   ReadLong;

        public Action<int, sbyte> WriteByte;
        public Action<int, short> WriteWord;
        public Action<int, int>   WriteLong;

        public Action<int> IrqCallback;

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
                if (Interrupt > 0 && (Interrupt > InterruptMaskLevel || Interrupt > 7))
                {
                    // TODO: Entering interrupt is not free. how many cycles does it take?
                    //Log.Error("CPU","****** ENTER INTERRUPT {0} *******", Interrupt);
                    short sr = (short) SR;                  // capture current SR.
                    S = true;                               // switch to supervisor mode, if not already in it.
                    A[7].s32 -= 4;                          // Push PC on stack
                    WriteLong(A[7].s32, PC);
                    A[7].s32 -= 2;                          // Push SR on stack
                    WriteWord(A[7].s32, sr);
                    PC = ReadLong((24 + Interrupt) * 4);    // Jump to interrupt vector
                    InterruptMaskLevel = Interrupt;         // Set interrupt mask to level currently being entered
                    Interrupt = 0;                          // "ack" interrupt. Note: this is wrong.
                    IrqCallback(InterruptMaskLevel);        // Invoke the "Interrupt accepted" callback handler
                }

                int prevCycles = PendingCycles;
                //Log.Note("CPU", State());
                op = (ushort)ReadWord(PC);
                if (Opcodes[op] == null) throw new Exception(string.Format("unhandled opcode at pc={0:X6}",PC));
                PC += 2;
                Opcodes[op]();
                int delta = prevCycles - PendingCycles;
                TotalExecutedCycles += delta;
            }
        }

        public string State()
        {
            string a = Disassemble(PC).ToString().PadRight(64);
            //string a = string.Format("{0:X6}: {1:X4}", PC, ReadWord(PC)).PadRight(64);
            string b = string.Format("D0:{0:X8} D1:{1:X8} D2:{2:X8} D3:{3:X8} D4:{4:X8} D5:{5:X8} D6:{6:X8} D7:{7:X8} ", D[0].u32, D[1].u32, D[2].u32, D[3].u32, D[4].u32, D[5].u32, D[6].u32, D[7].u32);
            string c = string.Format("A0:{0:X8} A1:{1:X8} A2:{2:X8} A3:{3:X8} A4:{4:X8} A5:{5:X8} A6:{6:X8} A7:{7:X8} ", A[0].u32, A[1].u32, A[2].u32, A[3].u32, A[4].u32, A[5].u32, A[6].u32, A[7].u32);
            string d = string.Format("SR:{0:X4} Pending {1}", SR, PendingCycles);
            return a + b + c + d;
        }

        public void SaveStateText(TextWriter writer, string id)
        {
            writer.WriteLine("[{0}]", id);
            writer.WriteLine("D0 {0:X8}", D[0].s32);
            writer.WriteLine("D1 {0:X8}", D[1].s32);
            writer.WriteLine("D2 {0:X8}", D[2].s32);
            writer.WriteLine("D3 {0:X8}", D[3].s32);
            writer.WriteLine("D4 {0:X8}", D[4].s32);
            writer.WriteLine("D5 {0:X8}", D[5].s32);
            writer.WriteLine("D6 {0:X8}", D[6].s32);
            writer.WriteLine("D7 {0:X8}", D[7].s32);
            writer.WriteLine();

            writer.WriteLine("A0 {0:X8}", A[0].s32);
            writer.WriteLine("A1 {0:X8}", A[1].s32);
            writer.WriteLine("A2 {0:X8}", A[2].s32);
            writer.WriteLine("A3 {0:X8}", A[3].s32);
            writer.WriteLine("A4 {0:X8}", A[4].s32);
            writer.WriteLine("A5 {0:X8}", A[5].s32);
            writer.WriteLine("A6 {0:X8}", A[6].s32);
            writer.WriteLine("A7 {0:X8}", A[7].s32);
            writer.WriteLine();

            writer.WriteLine("PC {0:X6}", PC);
            writer.WriteLine("InterruptMaskLevel {0}", InterruptMaskLevel);
            writer.WriteLine("USP {0:X8}", usp);
            writer.WriteLine("SSP {0:X8}", ssp);
            writer.WriteLine("S {0}", s);
            writer.WriteLine("M {0}", m);
            writer.WriteLine();

            writer.WriteLine("TotalExecutedCycles {0}", TotalExecutedCycles);
            writer.WriteLine("PendingCycles {0}", PendingCycles);
            
            writer.WriteLine("[/{0}]", id);
        }

        public void LoadStateText(TextReader reader, string id)
        {
            while (true)
            {
                string[] args = reader.ReadLine().Split(' ');
                if (args[0].Trim() == "") continue;
                if (args[0] == "[/"+id+"]") break;
                else if (args[0] == "D0") D[0].s32 = int.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "D1") D[1].s32 = int.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "D2") D[2].s32 = int.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "D3") D[3].s32 = int.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "D4") D[4].s32 = int.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "D5") D[5].s32 = int.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "D6") D[6].s32 = int.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "D7") D[7].s32 = int.Parse(args[1], NumberStyles.HexNumber);

                else if (args[0] == "A0") A[0].s32 = int.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "A1") A[1].s32 = int.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "A2") A[2].s32 = int.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "A3") A[3].s32 = int.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "A4") A[4].s32 = int.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "A5") A[5].s32 = int.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "A6") A[6].s32 = int.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "A7") A[7].s32 = int.Parse(args[1], NumberStyles.HexNumber);
                
                else if (args[0] == "PC") PC = int.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "InterruptMaskLevel") InterruptMaskLevel = int.Parse(args[1]);
                else if (args[0] == "USP") usp = int.Parse(args[1], NumberStyles.HexNumber);                
                else if (args[0] == "SSP") ssp = int.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "S") s = bool.Parse(args[1]);
                else if (args[0] == "M") m = bool.Parse(args[1]);

                else if (args[0] == "TotalExecutedCycles") TotalExecutedCycles = int.Parse(args[1]);
                else if (args[0] == "PendingCycles") PendingCycles = int.Parse(args[1]);

                else
                    Console.WriteLine("Skipping unrecognized identifier " + args[0]);
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
