using System;

namespace BizHawk.Emulation.CPUs.M68000
{
    partial class MC68000
    {
        void ADD0()
        {
            int Dreg = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            switch (size)
            {
                case 0: // byte
                {
                    sbyte value = ReadValueB(mode, reg);
                    int result = D[Dreg].s8 + value;
                    int uresult = D[Dreg].u8 + (byte)value;
                    X = C = (uresult & 0x100) != 0;
                    V = result > sbyte.MaxValue || result < sbyte.MinValue;
                    N = (result & 0x80) != 0;
                    Z = result == 0;
                    D[Dreg].s8 = (sbyte) result;
                    PendingCycles -= 4 + EACyclesBW[mode, reg];
                    return;
                }
                case 1: // word
                {
                    short value = ReadValueW(mode, reg);
                    int result = D[Dreg].s16 + value;
                    int uresult = D[Dreg].u16 + (ushort)value;
                    X = C = (uresult & 0x10000) != 0;
                    V = result > short.MaxValue || result < short.MinValue;
                    N = (result & 0x8000) != 0;
                    Z = result == 0;
                    D[Dreg].s16 = (short)result;
                    PendingCycles -= 4 + EACyclesBW[mode, reg];
                    return;
                }
                case 2: // long
                {
                    int value = ReadValueL(mode, reg);
                    long result = D[Dreg].s32 + value;
                    long uresult = D[Dreg].u32 + (uint)value;
                    X = C = (uresult & 0x100000000) != 0;
                    V = result > int.MaxValue || result < int.MinValue;
                    N = (result & 0x80000000) != 0;
                    Z = result == 0;
                    D[Dreg].s32 = (int)result;
                    PendingCycles -= 6 + EACyclesL[mode, reg];
                    return;
                }
            }
        }

        void ADD1()
        {
            int Dreg = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            switch (size)
            {
                case 0: // byte
                {
                    sbyte value = PeekValueB(mode, reg);
                    int result = value + D[Dreg].s8;
                    int uresult = (byte)value + D[Dreg].u8;
                    X = C = (uresult & 0x100) != 0;
                    V = result > sbyte.MaxValue || result < sbyte.MinValue;
                    N = (result & 0x80) != 0;
                    Z = result == 0;
                    WriteValueB(mode, reg, (sbyte)result);
                    PendingCycles -= 8 + EACyclesBW[mode, reg];
                    return;
                }
                case 1: // word
                {
                    short value = PeekValueW(mode, reg);
                    int result = value + D[Dreg].s16;
                    int uresult = (ushort)value + D[Dreg].u16;
                    X = C = (uresult & 0x10000) != 0;
                    V = result > short.MaxValue || result < short.MinValue;
                    N = (result & 0x8000) != 0;
                    Z = result == 0;
                    WriteValueW(mode, reg, (short)result);
                    PendingCycles -= 8 + EACyclesBW[mode, reg];
                    return;
                }
                case 2: // long
                {
                    int value = PeekValueL(mode, reg);
                    long result = value + D[Dreg].s32;
                    long uresult = (uint)value + D[Dreg].u32;
                    X = C = (uresult & 0x100000000) != 0;
                    V = result > int.MaxValue || result < int.MinValue;
                    N = (result & 0x80000000) != 0;
                    Z = result == 0;
                    WriteValueL(mode, reg, (int)result);
                    PendingCycles -= 12 + EACyclesL[mode, reg];
                    return;
                }
            }
        }

        void ADD_Disasm(DisassemblyInfo info)
        {
            int pc   = info.PC + 2;
            int Dreg = (op >> 9) & 7;
            int dir  = (op >> 8) & 1;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            string op1 = "D" + Dreg;
            string op2;

            switch (size)
            {
                case 0:  info.Mnemonic = "add.b"; op2 = DisassembleValue(mode, reg, 1, ref pc); break;
                case 1:  info.Mnemonic = "add.w"; op2 = DisassembleValue(mode, reg, 2, ref pc); break;
                default: info.Mnemonic = "add.l"; op2 = DisassembleValue(mode, reg, 4, ref pc); break;
            }
            info.Args = dir == 0 ? (op2 + ", " + op1) : (op1 + ", " + op2);
            info.Length = pc - info.PC;
        }
        
        void ADDI()
        {
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            switch (size)
            {
                case 0: // byte
                {
                    int immed = (sbyte) ReadWord(PC); PC += 2;
                    sbyte value = PeekValueB(mode, reg);
                    int result = value + immed;
                    int uresult = (byte)value + (byte)immed;
                    X = C = (uresult & 0x100) != 0;
                    V = result > sbyte.MaxValue || result < sbyte.MinValue;
                    N = (result & 0x80) != 0;
                    Z = result == 0;
                    WriteValueB(mode, reg, (sbyte)result);
                    if (mode == 0) PendingCycles -= 8;
                    else PendingCycles -= 12 + EACyclesBW[mode, reg];
                    return;
                }
                case 1: // word
                {
                    int immed = ReadWord(PC); PC += 2;
                    short value = PeekValueW(mode, reg);
                    int result = value + immed;
                    int uresult = (ushort)value + (ushort)immed;
                    X = C = (uresult & 0x10000) != 0;
                    V = result > short.MaxValue || result < short.MinValue;
                    N = (result & 0x8000) != 0;
                    Z = result == 0;
                    WriteValueW(mode, reg, (short)result);
                    if (mode == 0) PendingCycles -= 8;
                    else PendingCycles -= 12 + EACyclesBW[mode, reg];
                    return;
                }
                case 2: // long
                {
                    int immed = ReadLong(PC); PC += 4;
                    int value = PeekValueL(mode, reg);
                    long result = value + immed;
                    long uresult = (uint)value + (uint)immed;
                    X = C = (uresult & 0x100000000) != 0;
                    V = result > int.MaxValue || result < int.MinValue;
                    N = (result & 0x80000000) != 0;
                    Z = result == 0;
                    WriteValueL(mode, reg, (int)result);
                    if (mode == 0) PendingCycles -= 16;
                    else PendingCycles -= 20 + EACyclesBW[mode, reg];
                    return;
                }
            }
        }

        void ADDI_Disasm(DisassemblyInfo info)
        {
            int pc   = info.PC + 2;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 3;

            switch (size)
            {
                case 0:
                    info.Mnemonic = "addi.b";
                    info.Args = DisassembleImmediate(1, ref pc) + ", " + DisassembleValue(mode, reg, 1, ref pc);
                    break;
                case 1:
                    info.Mnemonic = "addi.w";
                    info.Args = DisassembleImmediate(2, ref pc) + ", " + DisassembleValue(mode, reg, 2, ref pc);
                    break;
                case 2:
                    info.Mnemonic = "addi.l";
                    info.Args = DisassembleImmediate(4, ref pc) + ", " + DisassembleValue(mode, reg, 4, ref pc);
                    break;
            }
            info.Length = pc - info.PC;
        }

        void ADDQ()
        {
            int data = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;
            
            data = data == 0 ? 8 : data; // range is 1-8; 0 represents 8

            switch (size)
            {
                case 0: // byte
                {
                    if (mode == 1) throw new Exception("ADDQ.B on address reg is invalid");
                    sbyte value = PeekValueB(mode, reg);
                    int result = value + data;
                    int uresult = (byte)value + data;
                    N = (result & 0x80) != 0;
                    Z = result == 0;
                    V = result > sbyte.MaxValue || result < sbyte.MinValue;
                    C = X = (uresult & 0x100) != 0;
                    WriteValueB(mode, reg, (sbyte) result);
                    if (mode == 0) PendingCycles -= 4;
                    else PendingCycles -= 8 + EACyclesBW[mode, reg];
                    return;
                }
                case 1: // word
                {
                    if (mode == 1)
                    {
                        int value = PeekValueL(mode, reg);
                        WriteValueL(mode, reg, value+data);
                    }
                    else
                    {
                        short value = PeekValueW(mode, reg);
                        int result = value + data;
                        int uresult = (ushort)value + data;
                        N = (result & 0x8000) != 0;
                        Z = result == 0;
                        V = result > short.MaxValue || result < short.MinValue;
                        C = X = (uresult & 0x10000) != 0;
                        WriteValueW(mode, reg, (short)result);
                    }
                    if (mode <= 1) PendingCycles -= 4;
                    else PendingCycles -= 8 + EACyclesBW[mode, reg];
                    return;
                }
                default: // long
                {
                    int value = PeekValueL(mode, reg);
                    long result = value + data;
                    long uresult = (uint)value + data;
                    if (mode != 1)
                    {
                        N = (result & 0x80000000) != 0;
                        Z = result == 0;
                        V = result > int.MaxValue || result < int.MinValue;
                        C = X = (uresult & 0x100000000) != 0;
                    }
                    WriteValueL(mode, reg, (int)result);
                    if (mode <= 1) PendingCycles -= 8;
                    else PendingCycles -= 12 + EACyclesL[mode, reg];
                    return;
                }
            }
        }

        void ADDQ_Disasm(DisassemblyInfo info)
        {
            int pc   = info.PC + 2;
            int data = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            data = data == 0 ? 8 : data; // range is 1-8; 0 represents 8

            switch (size)
            {
                case 0: info.Mnemonic = "addq.b"; info.Args = data+", "+DisassembleValue(mode, reg, 1, ref pc); break;
                case 1: info.Mnemonic = "addq.w"; info.Args = data+", "+DisassembleValue(mode, reg, 2, ref pc); break;
                case 2: info.Mnemonic = "addq.l"; info.Args = data+", "+DisassembleValue(mode, reg, 4, ref pc); break;
            }
            info.Length = pc - info.PC;
        }

        void ADDA()
        {
            int aReg = (op >> 9) & 7;
            int size = (op >> 8) & 1;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            if (size == 0) // word
            {
                int value = ReadValueW(mode, reg);
                A[aReg].s32 += value;
                PendingCycles -= 8 + EACyclesBW[mode, reg];
            } else { // long
                int value = ReadValueL(mode, reg);
                A[aReg].s32 += value;
                if (mode == 0 || mode == 1 || (mode == 7 && reg == 4))
                    PendingCycles -= 8 + EACyclesL[mode, reg];
                else
                    PendingCycles -= 6 + EACyclesL[mode, reg];
            }
        }

        void ADDA_Disasm(DisassemblyInfo info)
        {
            int pc   = info.PC + 2;
            int aReg = (op >> 9) & 7;
            int size = (op >> 8) & 1;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            info.Mnemonic = (size == 0) ? "adda.w" : "adda.l";
            info.Args = DisassembleValue(mode, reg, (size == 0) ? 2 : 4, ref pc) + ", A" + aReg;

            info.Length = pc - info.PC;
        }

        void SUB0()
        {
            int dReg = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            switch (size)
            {
                case 0: // byte
                {
                    sbyte a = D[dReg].s8;
                    sbyte b = ReadValueB(mode, reg);
                    int result = a - b;
                    X = C = ((a < b) ^ ((a ^ b) >= 0) == false);
                    V = result > sbyte.MaxValue || result < sbyte.MinValue;
                    N = (result & 0x80) != 0;
                    Z = result == 0;
                    D[dReg].s8 = (sbyte) result;
                    PendingCycles -= 4 + EACyclesBW[mode, reg];
                    return;
                }
                case 1: // word
                {
                    short a = D[dReg].s16;
                    short b = ReadValueW(mode, reg);
                    int result = a - b;
                    X = C = ((a < b) ^ ((a ^ b) >= 0) == false);
                    V = result > short.MaxValue || result < short.MinValue;
                    N = (result & 0x8000) != 0;
                    Z = result == 0;
                    D[dReg].s16 = (short) result;
                    PendingCycles -= 4 + EACyclesBW[mode, reg];
                    return;
                }
                case 2: // long
                {
                    int a = D[dReg].s32;
                    int b = ReadValueL(mode, reg);
                    long result = a - b;
                    X = C = ((a < b) ^ ((a ^ b) >= 0) == false);
                    V = result > int.MaxValue || result < int.MinValue;
                    N = (result & 0x80000000) != 0;
                    Z = result == 0;
                    D[dReg].s32 = (int)result;
                    PendingCycles -= 6 + EACyclesL[mode, reg];
                    return;
                }
            }
        }

        void SUB1()
        {
            int dReg = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            switch (size)
            {
                case 0: // byte
                {
                    sbyte a = PeekValueB(mode, reg);
                    sbyte b = D[dReg].s8;
                    int result = a - b;
                    X = C = ((a < b) ^ ((a ^ b) >= 0) == false);
                    V = result > sbyte.MaxValue || result < sbyte.MinValue;
                    N = (result & 0x80) != 0;
                    Z = result == 0;
                    WriteValueB(mode, reg, (sbyte) result);
                    PendingCycles -= 8 + EACyclesBW[mode, reg];
                    return;
                }
                case 1: // word
                {
                    short a = PeekValueW(mode, reg);
                    short b = D[dReg].s16;
                    int result = a - b;
                    X = C = ((a < b) ^ ((a ^ b) >= 0) == false);
                    V = result > short.MaxValue || result < short.MinValue;
                    N = (result & 0x8000) != 0;
                    Z = result == 0;
                    WriteValueW(mode, reg, (short) result);
                    PendingCycles -= 8 + EACyclesBW[mode, reg];
                    return;
                }
                case 2: // long
                {
                    int a = PeekValueL(mode, reg);
                    int b = D[dReg].s32;
                    long result = a - b;
                    X = C = ((a < b) ^ ((a ^ b) >= 0) == false);
                    V = result > int.MaxValue || result < int.MinValue;
                    N = (result & 0x80000000) != 0;
                    Z = result == 0;
                    WriteValueL(mode, reg, (int) result);
                    PendingCycles -= 12 + EACyclesL[mode, reg];
                    return;
                }
            }
        }

        void SUB_Disasm(DisassemblyInfo info)
        {
            int pc   = info.PC + 2;
            int dReg = (op >> 9) & 7;
            int dir  = (op >> 8) & 1;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            string op1 = "D" + dReg;
            string op2;

            switch (size)
            {
                case 0:  info.Mnemonic = "sub.b"; op2 = DisassembleValue(mode, reg, 1, ref pc); break;
                case 1:  info.Mnemonic = "sub.w"; op2 = DisassembleValue(mode, reg, 2, ref pc); break;
                default: info.Mnemonic = "sub.l"; op2 = DisassembleValue(mode, reg, 4, ref pc); break;
            }
            info.Args = dir == 0 ? (op2 + ", " + op1) : (op1 + ", " + op2);
            info.Length = pc - info.PC;
        }

        void SUBI()
        {
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            switch (size)
            {
                case 0: // byte
                {
                    sbyte b = (sbyte) ReadWord(PC); PC += 2;
                    sbyte a = PeekValueB(mode, reg);
                    int result = a - b;
                    X = C = ((a < b) ^ ((a ^ b) >= 0) == false);
                    V = result > sbyte.MaxValue || result < sbyte.MinValue;
                    N = (result & 0x80) != 0;
                    Z = result == 0;
                    WriteValueB(mode, reg, (sbyte)result);
                    if (mode == 0) PendingCycles -= 8;
                    else PendingCycles -= 12 + EACyclesBW[mode, reg];
                    return;
                }
                case 1: // word
                {
                    short b = ReadWord(PC); PC += 2;
                    short a = PeekValueW(mode, reg);
                    int result = a - b;
                    X = C = ((a < b) ^ ((a ^ b) >= 0) == false);
                    V = result > short.MaxValue || result < short.MinValue;
                    N = (result & 0x8000) != 0;
                    Z = result == 0;
                    WriteValueW(mode, reg, (short)result);
                    if (mode == 0) PendingCycles -= 8;
                    else PendingCycles -= 12 + EACyclesBW[mode, reg];
                    return;
                }
                case 2: // long
                {
                    int b = ReadLong(PC); PC += 4;
                    int a = PeekValueL(mode, reg);
                    long result = a - b;
                    X = C = ((a < b) ^ ((a ^ b) >= 0) == false);
                    V = result > int.MaxValue || result < int.MinValue;
                    N = (result & 0x80000000) != 0;
                    Z = result == 0;
                    WriteValueL(mode, reg, (int)result);
                    if (mode == 0) PendingCycles -= 16;
                    else PendingCycles -= 20 + EACyclesBW[mode, reg];
                    return;
                }
            }
        }

        void SUBI_Disasm(DisassemblyInfo info)
        {
            int pc   = info.PC + 2;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 3;

            switch (size)
            {
                case 0:
                    info.Mnemonic = "subi.b";
                    info.Args = DisassembleImmediate(1, ref pc) + ", " + DisassembleValue(mode, reg, 1, ref pc);
                    break;
                case 1:
                    info.Mnemonic = "subi.w";
                    info.Args = DisassembleImmediate(2, ref pc) + ", " + DisassembleValue(mode, reg, 2, ref pc);
                    break;
                case 2:
                    info.Mnemonic = "subi.l";
                    info.Args = DisassembleImmediate(4, ref pc) + ", " + DisassembleValue(mode, reg, 4, ref pc);
                    break;
            }
            info.Length = pc - info.PC;
        }

        void SUBQ()
        {
            int data = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            data = data == 0 ? 8 : data; // range is 1-8; 0 represents 8

            switch (size)
            {
                case 0: // byte
                {
                    if (mode == 1) throw new Exception("SUBQ.B on address reg is invalid");
                    sbyte value = PeekValueB(mode, reg);
                    int result = value - data;
                    N = (result & 0x80) != 0;
                    Z = result == 0;
                    V = result > sbyte.MaxValue || result < sbyte.MinValue;
                    C = X = ((value < data) ^ ((value ^ data) >= 0) == false);
                    WriteValueB(mode, reg, (sbyte) result);
                    if (mode == 0) PendingCycles -= 4;
                    else PendingCycles -= 8 + EACyclesBW[mode, reg];
                    return;
                }
                case 1: // word
                {
                    if (mode == 1)
                    {
                        int value = PeekValueL(mode, reg);
                        WriteValueL(mode, reg, value - data);
                    }
                    else
                    {
                        short value = PeekValueW(mode, reg);
                        int result = value - data;
                        N = (result & 0x8000) != 0;
                        Z = result == 0;
                        V = result > short.MaxValue || result < short.MinValue;
                        C = X = ((value < data) ^ ((value ^ data) >= 0) == false);
                        WriteValueW(mode, reg, (short)result);
                    }
                    if (mode <= 1) PendingCycles -= 4;
                    else PendingCycles -= 8 + EACyclesBW[mode, reg];
                    return;
                }
                default: // long
                {
                    int value = PeekValueL(mode, reg);
                    long result = value - data;
                    if (mode != 1)
                    {
                        N = (result & 0x80000000) != 0;
                        Z = result == 0;
                        V = result > int.MaxValue || result < int.MinValue;
                        C = X = ((value < data) ^ ((value ^ data) >= 0) == false);
                    }
                    WriteValueL(mode, reg, (int)result);
                    if (mode <= 1) PendingCycles -= 8;
                    else PendingCycles -= 12 + EACyclesL[mode, reg];
                    return;
                }
            }
        }

        void SUBQ_Disasm(DisassemblyInfo info)
        {
            int pc   = info.PC + 2;
            int data = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            data = data == 0 ? 8 : data; // range is 1-8; 0 represents 8

            switch (size)
            {
                case 0: info.Mnemonic = "subq.b"; info.Args = data+", "+DisassembleValue(mode, reg, 1, ref pc); break;
                case 1: info.Mnemonic = "subq.w"; info.Args = data+", "+DisassembleValue(mode, reg, 2, ref pc); break;
                case 2: info.Mnemonic = "subq.l"; info.Args = data+", "+DisassembleValue(mode, reg, 4, ref pc); break;
            }
            info.Length = pc - info.PC;   
        }

        void SUBA()
        {
            int aReg = (op >> 9) & 7;
            int size = (op >> 8) & 1;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            if (size == 0) // word
            {
                int value = ReadValueW(mode, reg);
                A[aReg].s32 -= value;
                PendingCycles -= 8 + EACyclesBW[mode, reg];
            } else { // long
                int value = ReadValueL(mode, reg);
                A[aReg].s32 -= value;
                if (mode == 0 || mode == 1 || (mode == 7 && reg == 4))
                    PendingCycles -= 8 + EACyclesL[mode, reg];
                else 
                    PendingCycles -= 6 + EACyclesL[mode, reg];
            }
        }

        void SUBA_Disasm(DisassemblyInfo info)
        {
            int pc = info.PC + 2;

            int aReg = (op >> 9) & 7;
            int size = (op >> 8) & 1;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            info.Mnemonic = (size == 0) ? "suba.w" : "suba.l";
            info.Args = DisassembleValue(mode, reg, (size == 0) ? 2 : 4, ref pc) + ", A"+aReg;

            info.Length = pc - info.PC;
        }

        void NEG()
        {
            int size = (op >> 6) & 0x03;
            int mode = (op >> 3) & 0x07;
            int reg  = op & 0x07;

            if (mode == 1) throw new Exception("NEG on address reg is invalid");

            switch (size)
            {
                case 0: // Byte
                    {
                        sbyte value = PeekValueB(mode, reg);
                        int result = 0 - value;
                        N = (result & 0x80) != 0;
                        Z = result == 0;
                        V = result > sbyte.MaxValue || result < sbyte.MinValue;
                        C = X = ((0 < value) ^ ((0 ^ value) >= 0) == false);
                        WriteValueB(mode, reg, (sbyte)result);
                        if (mode == 0) PendingCycles -= 4;
                        else PendingCycles -= 8 + EACyclesBW[mode, reg];
                        return;
                    }
                case 1: // Word
                    {
                        short value = PeekValueW(mode, reg);
                        int result = 0 - value;
                        N = (result & 0x8000) != 0;
                        Z = result == 0;
                        V = result > short.MaxValue || result < short.MinValue;
                        C = X = ((0 < value) ^ ((0 ^ value) >= 0) == false);
                        WriteValueW(mode, reg, (short)result);
                        if (mode == 0) PendingCycles -= 4;
                        else PendingCycles -= 8 + EACyclesBW[mode, reg];
                        return;
                    }
                case 2: // Long
                    {
                        int value = PeekValueL(mode, reg);
                        long result = 0 - value;
                        N = (result & 0x80000000) != 0;
                        Z = result == 0;
                        V = result > int.MaxValue || result < int.MinValue;
                        C = X = ((0 < value) ^ ((0 ^ value) >= 0) == false);
                        WriteValueL(mode, reg, (int)result);
                        if (mode == 0) PendingCycles -= 8;
                        else PendingCycles -= 12 + EACyclesL[mode, reg];
                        return;
                    }
            }
        }

        void NEG_Disasm(DisassemblyInfo info)
        {
            int size = (op >> 6) & 0x03;
            int mode = (op >> 3) & 0x07;
            int reg  = op & 0x07;

            int pc = info.PC + 2;

            switch (size)
            {
                case 0: // Byte
                    info.Mnemonic = "neg.b";
                    info.Args = DisassembleValue(mode, reg, 1, ref pc);
                    break;
                case 1: // Word
                    info.Mnemonic = "neg.w";
                    info.Args = DisassembleValue(mode, reg, 2, ref pc);
                    break;
                case 2: // Long
                    info.Mnemonic = "neg.l";
                    info.Args = DisassembleValue(mode, reg, 4, ref pc);
                    break;
            }

            info.Length = pc - info.PC;
        }

        void CMP()
        {
            int dReg = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            switch (size)
            {
                case 0: // byte
                {
                    sbyte a = D[dReg].s8;
                    sbyte b = ReadValueB(mode, reg);
                    int result = a - b;
                    N = (result & 0x80) != 0;
                    Z = result == 0;
                    V = result > sbyte.MaxValue || result < sbyte.MinValue;
                    C = ((a < b) ^ ((a ^ b) >= 0) == false);
                    if (mode == 0) PendingCycles -= 8;
                    PendingCycles -= 4 + EACyclesBW[mode, reg];
                    return;
                }
                case 1: // word
                {
                    short a = D[dReg].s16;
                    short b = ReadValueW(mode, reg);
                    int result = a - b;
                    N = (result & 0x8000) != 0;
                    Z = result == 0;
                    V = result > short.MaxValue || result < short.MinValue;
                    C = ((a < b) ^ ((a ^ b) >= 0) == false);
                    if (mode == 0) PendingCycles -= 8;
                    PendingCycles -= 4 + EACyclesBW[mode, reg];
                    return;
                }
                case 2: // long
                {
                    int a = D[dReg].s32;
                    int b = ReadValueL(mode, reg);
                    long result = a - b;
                    N = (result & 0x80000000) != 0;
                    Z = result == 0;
                    V = result > int.MaxValue || result < int.MinValue;
                    C = ((a < b) ^ ((a ^ b) >= 0) == false);
                    PendingCycles -= 6 + EACyclesL[mode, reg];
                    return;
                }
            }
        }

        void CMP_Disasm(DisassemblyInfo info)
        {
            int pc = info.PC + 2;

            int dReg = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            switch (size)
            {
                case 0:
                    info.Mnemonic = "cmp.b";
                    info.Args = DisassembleValue(mode, reg, 1, ref pc) + ", D" + dReg;
                    break;
                case 1:
                    info.Mnemonic = "cmp.w";
                    info.Args = DisassembleValue(mode, reg, 2, ref pc) + ", D" + dReg;
                    break;
                case 2:
                    info.Mnemonic = "cmp.l";
                    info.Args = DisassembleValue(mode, reg, 4, ref pc) + ", D" + dReg;
                    break;
            }
            info.Length = pc - info.PC;
        }

        void CMPA()
        {
            int aReg = (op >> 9) & 7;
            int size = (op >> 8) & 1;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            switch (size)
            {
                case 0: // word
                {
                    short a = A[aReg].s16;
                    short b = ReadValueW(mode, reg);
                    int result = a - b;
                    N = (result & 0x8000) != 0;
                    Z = result == 0;
                    V = result > short.MaxValue || result < short.MinValue;
                    C = ((a < b) ^ ((a ^ b) >= 0) == false);
                    PendingCycles -= 6 + EACyclesBW[mode, reg];
                    return;
                }
                case 1: // long
                {
                    int a = A[aReg].s32;
                    int b = ReadValueL(mode, reg);
                    long result = a - b;
                    N = (result & 0x80000000) != 0;
                    Z = result == 0;
                    V = result > int.MaxValue || result < int.MinValue;
                    C = ((a < b) ^ ((a ^ b) >= 0) == false);
                    PendingCycles -= 6 + EACyclesL[mode, reg];
                    return;
                }
            }
        }

        void CMPA_Disasm(DisassemblyInfo info)
        {
            int pc = info.PC + 2;

            int aReg = (op >> 9) & 7;
            int size = (op >> 8) & 1;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            switch (size)
            {
                case 0:
                    info.Mnemonic = "cmpa.w";
                    info.Args = DisassembleValue(mode, reg, 2, ref pc) + ", A" + aReg;
                    break;
                case 1:
                    info.Mnemonic = "cmpa.l";
                    info.Args = DisassembleValue(mode, reg, 4, ref pc) + ", A" + aReg;
                    break;
            }
            info.Length = pc - info.PC;
        }

        void CMPM()
        {
            int axReg = (op >> 9) & 7;
            int size  = (op >> 6) & 3;
            int ayReg = (op >> 0) & 7;

            switch (size)
            {
                case 0: // byte
                    {
                        sbyte a = ReadByte(A[axReg].s32); A[axReg].s32 += 1; // Does A7 stay word aligned???
                        sbyte b = ReadByte(A[ayReg].s32); A[ayReg].s32 += 1;
                        int result = a - b;
                        N = (result & 0x80) != 0;
                        Z = result == 0;
                        V = result > sbyte.MaxValue || result < sbyte.MinValue;
                        C = ((a < b) ^ ((a ^ b) >= 0) == false);
                        PendingCycles -= 12;
                        return;
                    }
                case 1: // word
                    {
                        short a = ReadWord(A[axReg].s32); A[axReg].s32 += 2;
                        short b = ReadWord(A[ayReg].s32); A[ayReg].s32 += 2;
                        int result = a - b;
                        N = (result & 0x8000) != 0;
                        Z = result == 0;
                        V = result > short.MaxValue || result < short.MinValue;
                        C = ((a < b) ^ ((a ^ b) >= 0) == false);
                        PendingCycles -= 12;
                        return;
                    }
                case 2: // long
                    {
                        int a = ReadLong(A[axReg].s32); A[axReg].s32 += 4;
                        int b = ReadLong(A[ayReg].s32); A[ayReg].s32 += 4;
                        long result = a - b;
                        N = (result & 0x80000000) != 0;
                        Z = result == 0;
                        V = result > int.MaxValue || result < int.MinValue;
                        C = ((a < b) ^ ((a ^ b) >= 0) == false);
                        PendingCycles -= 20;
                        return;
                    }
            }
        }

        void CMPM_Disasm(DisassemblyInfo info)
        {
            int pc    = info.PC + 2;
            int axReg = (op >> 9) & 7;
            int size  = (op >> 6) & 3;
            int ayReg = (op >> 0) & 7;

            switch (size)
            {
                case 0: info.Mnemonic = "cmpm.b"; break;
                case 1: info.Mnemonic = "cmpm.w"; break;
                case 2: info.Mnemonic = "cmpm.l"; break;
            }
            info.Args = string.Format("(A{0})+, (A{1})+", ayReg, axReg);
            info.Length = pc - info.PC;
        }

        void CMPI()
        {
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            switch (size)
            {
                case 0: // byte
                {
                    sbyte b = (sbyte) ReadWord(PC); PC += 2;
                    sbyte a = ReadValueB(mode, reg);
                    int result = a - b;
                    N = (result & 0x80) != 0;
                    Z = result == 0;
                    V = result > sbyte.MaxValue || result < sbyte.MinValue;
                    C = ((a < b) ^ ((a ^ b) >= 0) == false);
                    if (mode == 0) PendingCycles -= 8;
                    else PendingCycles -= 8 + EACyclesBW[mode, reg];
                    return;
                }
                case 1: // word
                {
                    short b = ReadWord(PC); PC += 2;
                    short a = ReadValueW(mode, reg);
                    int result = a - b;
                    N = (result & 0x8000) != 0;
                    Z = result == 0;
                    V = result > short.MaxValue || result < short.MinValue;
                    C = ((a < b) ^ ((a ^ b) >= 0) == false);
                    if (mode == 0) PendingCycles -= 8;
                    else PendingCycles -= 8 + EACyclesBW[mode, reg];
                    return;
                }
                case 2: // long
                {
                    int b = ReadLong(PC); PC += 4;
                    int a = ReadValueL(mode, reg);
                    long result = a - b;
                    N = (result & 0x80000000) != 0;
                    Z = result == 0;
                    V = result > int.MaxValue || result < int.MinValue;
                    C = ((a < b) ^ ((a ^ b) >= 0) == false);
                    if (mode == 0) PendingCycles -= 14;
                    else PendingCycles -= 12 + EACyclesL[mode, reg];
                    return;
                }
            }
        }

        void CMPI_Disasm(DisassemblyInfo info)
        {
            int pc   = info.PC + 2;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;
            int immediate;

            switch (size)
            {
                case 0:
                    immediate = (byte)ReadWord(pc); pc += 2;
                    info.Mnemonic = "cmpi.b"; 
                    info.Args = String.Format("${0:X}, {1}", immediate, DisassembleValue(mode, reg, 1, ref pc)); 
                    break;
                case 1:
                    immediate = ReadWord(pc); pc += 2;
                    info.Mnemonic = "cmpi.w";
                    info.Args = String.Format("${0:X}, {1}", immediate, DisassembleValue(mode, reg, 2, ref pc));
                    break;
                case 2:
                    immediate = ReadLong(pc); pc += 4;
                    info.Mnemonic = "cmpi.l";
                    info.Args = String.Format("${0:X}, {1}", immediate, DisassembleValue(mode, reg, 4, ref pc));
                    break;
            }
            info.Length = pc - info.PC;
        }

        void MULU()
        {
            int dreg = (op >> 9) & 7;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            uint result = (uint) (D[dreg].u16 * (ushort)ReadValueW(mode, reg));
            D[dreg].u32 = result;
            
            V = false;
            C = false;
            N = (result & 0x80000000) != 0;
            Z = result == 0;

            PendingCycles -= 70 + EACyclesBW[mode, reg];
        }

        void MULU_Disasm(DisassemblyInfo info)
        {
            int dreg = (op >> 9) & 7;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            int pc = info.PC + 2;
            info.Mnemonic = "mulu";
            info.Args = String.Format("{0}, D{1}", DisassembleValue(mode, reg, 2, ref pc), dreg);
            info.Length = pc - info.PC;
        }

        void MULS()
        {
            int dreg = (op >> 9) & 7;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            int result = D[dreg].s16 * ReadValueW(mode, reg);
            D[dreg].s32 = result;

            V = false;
            C = false;
            N = (result & 0x80000000) != 0;
            Z = result == 0;

            PendingCycles -= 70 + EACyclesBW[mode, reg];
        }

        void MULS_Disasm(DisassemblyInfo info)
        {
            int dreg = (op >> 9) & 7;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            int pc = info.PC + 2;
            info.Mnemonic = "muls";
            info.Args = String.Format("{0}, D{1}", DisassembleValue(mode, reg, 2, ref pc), dreg);
            info.Length = pc - info.PC;
        }

        void DIVU()
        {
            int dreg = (op >> 9) & 7;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            uint source = (ushort) ReadValueW(mode, reg);
            uint dest = D[dreg].u32;

            if (source == 0) 
                throw new Exception("divide by zero");

            uint quotient = dest / source;
            uint remainder = dest % source;

            V = ((int) quotient < short.MinValue || (int) quotient > short.MaxValue);
            N = (quotient & 0x8000) != 0;
            Z = quotient == 0;
            C = false;

            D[dreg].u32 = (quotient & 0xFFFF) | (remainder << 16);
            PendingCycles -= 140 + EACyclesBW[mode, reg]; // this is basically a rough approximation at best.
        }

        void DIVU_Disasm(DisassemblyInfo info)
        {
            int dreg = (op >> 9) & 7;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            int pc = info.PC + 2;
            info.Mnemonic = "divu";
            info.Args = String.Format("{0}, D{1}", DisassembleValue(mode, reg, 2, ref pc), dreg);
            info.Length = pc - info.PC;
        }

        void DIVS()
        {
            int dreg = (op >> 9) & 7;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            int source = ReadValueW(mode, reg);
            int dest = D[dreg].s32;

            if (source == 0)
                throw new Exception("divide by zero");

            int quotient = dest / source;
            int remainder = dest % source;

            V = ((int)quotient < short.MinValue || (int)quotient > short.MaxValue);
            N = (quotient & 0x8000) != 0;
            Z = quotient == 0;
            C = false;

            D[dreg].s32 = (quotient & 0xFFFF) | (remainder << 16);
            PendingCycles -= 140 + EACyclesBW[mode, reg];
        }

        void DIVS_Disasm(DisassemblyInfo info)
        {
            int dreg = (op >> 9) & 7;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            int pc = info.PC + 2;
            info.Mnemonic = "divs";
            info.Args = String.Format("{0}, D{1}", DisassembleValue(mode, reg, 2, ref pc), dreg);
            info.Length = pc - info.PC;
        }
    }
}