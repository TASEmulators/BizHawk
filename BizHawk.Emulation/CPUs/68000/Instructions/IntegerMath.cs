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
            int reg = (op >> 0) & 7;

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
            int reg = (op >> 0) & 7;

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
            int pc = info.PC + 2;

            int Dreg = (op >> 9) & 7;
            int dir = (op >> 8) & 1;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg = (op >> 0) & 7;

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
                    int immed = ReadLong(PC); PC += 2;
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
            int pc = info.PC + 2;

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
                    short value = PeekValueW(mode, reg);
                    int result = value + data;
                    int uresult = (ushort)value + data;
                    if (mode != 1)
                    {
                        N = (result & 0x8000) != 0;
                        Z = result == 0;
                        V = result > short.MaxValue || result < short.MinValue;
                        C = X = (uresult & 0x10000) != 0;
                    }
                    WriteValueW(mode, reg, (short)result);
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
            int pc = info.PC + 2;
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
                A[aReg].s32 -= value;
                PendingCycles += 6 + EACyclesL[mode, reg];
            }
        }

        void ADDA_Disasm(DisassemblyInfo info)
        {
            int pc = info.PC + 2;

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
            int Dreg = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg = (op >> 0) & 7;

            switch (size)
            {
                case 0: // byte
                {
                    sbyte value = ReadValueB(mode, reg);
                    int result = D[Dreg].s8 - value;
                    int uresult = D[Dreg].u8 - (byte)value;
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
                    int result = D[Dreg].s16 - value;
                    int uresult = D[Dreg].u16 - (ushort)value;
                    X = C = (uresult & 0x10000) != 0;
                    V = result > short.MaxValue || result < short.MinValue;
                    N = (result & 0x8000) != 0;
                    Z = result == 0;
                    D[Dreg].s16 = (short) result;
                    PendingCycles -= 4 + EACyclesBW[mode, reg];
                    return;
                }
                case 2: // long
                {
                    int value = ReadValueL(mode, reg);
                    long result = D[Dreg].s32 - value;
                    long uresult = D[Dreg].u32 - (uint)value;
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

        void SUB1()
        {
            int Dreg = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg = (op >> 0) & 7;

            switch (size)
            {
                case 0: // byte
                {
                    sbyte value = PeekValueB(mode, reg);
                    int result = value - D[Dreg].s8;
                    int uresult = (byte)value - D[Dreg].u8;
                    X = C = (uresult & 0x100) != 0;
                    V = result > sbyte.MaxValue || result < sbyte.MinValue;
                    N = (result & 0x80) != 0;
                    Z = result == 0;
                    WriteValueB(mode, reg, (sbyte) result);
                    PendingCycles -= 8 + EACyclesBW[mode, reg];
                    return;
                }
                case 1: // word
                {
                    short value = PeekValueW(mode, reg);
                    int result = value - D[Dreg].s16;
                    int uresult = (ushort)value - D[Dreg].u16;
                    X = C = (uresult & 0x10000) != 0;
                    V = result > short.MaxValue || result < short.MinValue;
                    N = (result & 0x8000) != 0;
                    Z = result == 0;
                    WriteValueW(mode, reg, (short) result);
                    PendingCycles -= 8 + EACyclesBW[mode, reg];
                    return;
                }
                case 2: // long
                {
                    int value = PeekValueL(mode, reg);
                    long result = value - D[Dreg].s32;
                    long uresult = (uint)value - D[Dreg].u32;
                    X = C = (uresult & 0x100000000) != 0;
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
            int pc = info.PC + 2;

            int Dreg = (op >> 9) & 7;
            int dir  = (op >> 8) & 1;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            string op1 = "D" + Dreg;
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
                    int immed = (sbyte) ReadWord(PC); PC += 2;
                    sbyte value = PeekValueB(mode, reg);
                    int result = value - immed;
                    int uresult = (byte)value - (byte)immed;
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
                    int result = value - immed;
                    int uresult = (ushort)value - (ushort)immed;
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
                    int immed = ReadLong(PC); PC += 2;
                    int value = PeekValueL(mode, reg);
                    long result = value - immed;
                    long uresult = (uint)value - (uint)immed;
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

        void SUBI_Disasm(DisassemblyInfo info)
        {
            int pc = info.PC + 2;

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
            int reg = (op >> 0) & 7;

            data = data == 0 ? 8 : data; // range is 1-8; 0 represents 8

            switch (size)
            {
                case 0: // byte
                {
                    if (mode == 1) throw new Exception("SUBQ.B on address reg is invalid");
                    sbyte value = PeekValueB(mode, reg);
                    int result = value - data;
                    int uresult = (byte)value - data;
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
                    short value = PeekValueW(mode, reg);
                    int result = value - data;
                    int uresult = (ushort)value - data;
                    if (mode != 1)
                    {
                        N = (result & 0x8000) != 0;
                        Z = result == 0;
                        V = result > short.MaxValue || result < short.MinValue;
                        C = X = (uresult & 0x10000) != 0;
                    }
                    WriteValueW(mode, reg, (short)result);
                    if (mode <= 1) PendingCycles -= 4;
                    else PendingCycles -= 8 + EACyclesBW[mode, reg];
                    return;
                }
                default: // long
                {
                    int value = PeekValueL(mode, reg);
                    long result = value - data;
                    long uresult = (uint)value - data;
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

        void SUBQ_Disasm(DisassemblyInfo info)
        {
            int pc = info.PC + 2;
            int data = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg = (op >> 0) & 7;

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
            int reg = op & 0x07;

            if (mode == 1) throw new Exception("NEG on address reg is invalid");

            switch (size)
            {
                case 0: // Byte
                    {
                        sbyte value = PeekValueB(mode, reg);
                        int result = 0 - value;
                        int uresult = 0 - (byte)value;
                        N = (result & 0x80) != 0;
                        Z = result == 0;
                        V = result > sbyte.MaxValue || result < sbyte.MinValue;
                        C = X = (uresult & 0x100) != 0;
                        WriteValueB(mode, reg, (sbyte)result);
                        if (mode == 0) PendingCycles -= 4;
                        else PendingCycles -= 8 + EACyclesBW[mode, reg];
                        return;
                    }
                case 1: // Word
                    {
                        short value = PeekValueW(mode, reg);
                        int result = 0 - value;
                        int uresult = 0 - (ushort)value;
                        N = (result & 0x8000) != 0;
                        Z = result == 0;
                        V = result > short.MaxValue || result < short.MinValue;
                        C = X = (uresult & 0x10000) != 0;
                        WriteValueW(mode, reg, (short)result);
                        if (mode == 0) PendingCycles -= 4;
                        else PendingCycles -= 8 + EACyclesBW[mode, reg];
                        return;
                    }
                case 2: // Long
                    {
                        int value = PeekValueL(mode, reg);
                        long result = 0 - value;
                        long uresult = 0 - (uint)value;
                        N = (result & 0x80000000) != 0;
                        Z = result == 0;
                        V = result > int.MaxValue || result < int.MinValue;
                        C = X = (uresult & 0x100000000) != 0;
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
            int reg = op & 0x07;

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
                    sbyte value = ReadValueB(mode, reg);
                    int result = value - D[dReg].s8;
                    int uresult = (byte)value - D[dReg].u8;
                    N = (result & 0x80) != 0;
                    Z = result == 0;
                    V = result > sbyte.MaxValue || result < sbyte.MinValue;
                    C = (uresult & 0x100) != 0;
                    if (mode == 0) PendingCycles -= 8;
                    PendingCycles -= 4 + EACyclesBW[mode, reg];
                    return;
                }
                case 1: // word
                {
                    short value = ReadValueW(mode, reg);
                    int result = value - D[dReg].s16;
                    int uresult = (ushort)value - D[dReg].u16;
                    N = (result & 0x8000) != 0;
                    Z = result == 0;
                    V = result > short.MaxValue || result < short.MinValue;
                    C = (uresult & 0x10000) != 0;
                    if (mode == 0) PendingCycles -= 8;
                    PendingCycles -= 4 + EACyclesBW[mode, reg];
                    return;
                }
                case 2: // long
                {
                    int value = ReadValueL(mode, reg);
                    long result = value - D[dReg].s32;
                    long uresult = (uint)value - D[dReg].u32;
                    N = (result & 0x80000000) != 0;
                    Z = result == 0;
                    V = result > int.MaxValue || result < int.MinValue;
                    C = (uresult & 0x100000000) != 0;
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
                    short value = ReadValueW(mode, reg);
                    int result = A[aReg].s16 - value;
                    int uresult = A[aReg].u16 - (ushort)value;
                    N = (result & 0x8000) != 0;
                    Z = result == 0;
                    V = result > short.MaxValue || result < short.MinValue;
                    C = (uresult & 0x10000) != 0;
                    PendingCycles -= 6 + EACyclesBW[mode, reg];
                    return;
                }
                case 1: // long
                {
                    int value = ReadValueL(mode, reg);
                    long result = A[aReg].s32 - value;
                    long uresult = A[aReg].u32 - (uint)value;
                    N = (result & 0x80000000) != 0;
                    Z = result == 0;
                    V = result > int.MaxValue || result < int.MinValue;
                    C = (uresult & 0x100000000) != 0;
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


        void CMPI()
        {
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg = (op >> 0) & 7;

            switch (size)
            {
                case 0: // byte
                {
                    int immed = (sbyte) ReadWord(PC); PC += 2;
                    sbyte value = ReadValueB(mode, reg);
                    int result = value - immed;
                    int uresult = (byte)value - (byte)immed;
                    N = (result & 0x80) != 0;
                    Z = result == 0;
                    V = result > sbyte.MaxValue || result < sbyte.MinValue;
                    C = (uresult & 0x100) != 0;
                    if (mode == 0) PendingCycles -= 8;
                    else PendingCycles -= 8 + EACyclesBW[mode, reg];
                    return;
                }
                case 1: // word
                {
                    int immed = ReadWord(PC); PC += 2;
                    short value = ReadValueW(mode, reg);
                    int result = value - immed;
                    int uresult = (ushort)value - (ushort)immed;
                    N = (result & 0x8000) != 0;
                    Z = result == 0;
                    V = result > short.MaxValue || result < short.MinValue;
                    C = (uresult & 0x10000) != 0;
                    if (mode == 0) PendingCycles -= 8;
                    else PendingCycles -= 8 + EACyclesBW[mode, reg];
                    return;
                }
                case 2: // long
                {
                    int immed = ReadLong(PC); PC += 4;
                    int value = ReadValueL(mode, reg);
                    long result = value - immed;
                    long uresult = (uint)value - (uint)immed;
                    N = (result & 0x80000000) != 0;
                    Z = result == 0;
                    V = result > int.MaxValue || result < int.MinValue;
                    C = (uresult & 0x100000000) != 0;
                    if (mode == 0) PendingCycles -= 14;
                    else PendingCycles -= 12 + EACyclesL[mode, reg];
                    return;
                }
            }
        }

        void CMPI_Disasm(DisassemblyInfo info)
        {
            int pc = info.PC + 2;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg = (op >> 0) & 7;
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
    }
}