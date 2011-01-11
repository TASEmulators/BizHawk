using System;

namespace BizHawk.Emulation.CPUs.M68K
{
    public partial class M68000
    {
        private void ADD0()
        {
            int Dreg = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg = (op >> 0) & 7;

            switch (size)
            {
                case 0: // byte
                {
                    int result = D[Dreg].s8 + ReadValueB(mode, reg);
                    X = C = (result & 0x100) != 0;
                    V = result > sbyte.MaxValue || result < sbyte.MinValue;
                    N = result < 0;
                    Z = result == 0;
                    D[Dreg].s8 = (sbyte)result;
                    PendingCycles -= 4 + EACyclesBW[mode, reg];
                    return;
                }
                case 1: // word
                {
                    int result = D[Dreg].s16 + ReadValueW(mode, reg);
                    X = C = (result & 0x10000) != 0;
                    V = result > short.MaxValue || result < short.MinValue;
                    N = result < 0;
                    Z = result == 0;
                    D[Dreg].s16 = (short)result;
                    PendingCycles -= 4 + EACyclesBW[mode, reg];
                    return;
                }
                case 2: // long
                {
                    long result = D[Dreg].s32 + ReadValueL(mode, reg);
                    X = C = (result & 0x100000000) != 0;
                    V = result > int.MaxValue || result < int.MinValue;
                    N = result < 0;
                    Z = result == 0;
                    D[Dreg].s32 = (int)result;
                    PendingCycles -= 6 + EACyclesL[mode, reg];
                    return;
                }
            }
        }

        private void ADD1()
        {
            int Dreg = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg = (op >> 0) & 7;

            switch (size)
            {
                case 0: // byte
                {
                    int result = PeekValueB(mode, reg) + D[Dreg].s8;
                    X = C = (result & 0x100) != 0;
                    V = result > sbyte.MaxValue || result < sbyte.MinValue;
                    N = result < 0;
                    Z = result == 0;
                    WriteValueB(mode, reg, (sbyte)result);
                    PendingCycles -= 8 + EACyclesBW[mode, reg];
                    return;
                }
                case 1: // word
                {
                    int result = PeekValueW(mode, reg) + D[Dreg].s16;
                    X = C = (result & 0x10000) != 0;
                    V = result > short.MaxValue || result < short.MinValue;
                    N = result < 0;
                    Z = result == 0;
                    WriteValueW(mode, reg, (short)result);
                    PendingCycles -= 8 + EACyclesBW[mode, reg];
                    return;
                }
                case 2: // long
                {
                    long result = PeekValueL(mode, reg) + D[Dreg].s32;
                    X = C = (result & 0x100000000) != 0;
                    V = result > int.MaxValue || result < int.MinValue;
                    N = result < 0;
                    Z = result == 0;
                    WriteValueL(mode, reg, (int)result);
                    PendingCycles -= 12 + EACyclesL[mode, reg];
                    return;
                }
            }
        }

        private void ADD_Disasm(DisassemblyInfo info)
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
        
        private void ADDI()
        {
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            switch (size)
            {
                case 0: // byte
                {
                    int immed = (sbyte) ReadWord(PC); PC += 2;
                    int result = PeekValueB(mode, reg) + immed;
                    X = C = (result & 0x100) != 0;
                    V = result > sbyte.MaxValue || result < sbyte.MinValue;
                    N = result < 0;
                    Z = result == 0;
                    WriteValueB(mode, reg, (sbyte)result);
                    if (mode == 0) PendingCycles -= 8;
                    else PendingCycles -= 12 + EACyclesBW[mode, reg];
                    return;
                }
                case 1: // word
                {
                    int immed = ReadWord(PC); PC += 2;
                    int result = PeekValueW(mode, reg) + immed;
                    X = C = (result & 0x10000) != 0;
                    V = result > short.MaxValue || result < short.MinValue;
                    N = result < 0;
                    Z = result == 0;
                    WriteValueW(mode, reg, (short)result);
                    if (mode == 0) PendingCycles -= 8;
                    else PendingCycles -= 12 + EACyclesBW[mode, reg];
                    return;
                }
                case 2: // long
                {
                    int immed = ReadLong(PC); PC += 2;
                    long result = PeekValueL(mode, reg) + immed;
                    X = C = (result & 0x100000000) != 0;
                    V = result > int.MaxValue || result < int.MinValue;
                    N = result < 0;
                    Z = result == 0;
                    WriteValueL(mode, reg, (int)result);
                    if (mode == 0) PendingCycles -= 16;
                    else PendingCycles -= 20 + EACyclesBW[mode, reg];
                    return;
                }
            }
        }

        private void ADDI_Disasm(DisassemblyInfo info)
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

        private void ADDQ()
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
                    int result = PeekValueB(mode, reg) + data;
                    N = result < 0;
                    Z = result == 0;
                    V = result > sbyte.MaxValue || result < sbyte.MinValue;
                    C = X = (result & 0x100) != 0;
                    WriteValueB(mode, reg, (sbyte) result);
                    if (mode == 0) PendingCycles -= 4;
                    else PendingCycles -= 8 + EACyclesBW[mode, reg];
                    return;
                }
                case 1: // word
                {
                    int result;
                    if (mode == 1)
                    {
                        result = PeekValueL(mode, reg) + data;
                        WriteValueL(mode, reg, (short) result);
                    } else {
                        result = PeekValueW(mode, reg) + data;
                        N = result < 0;
                        Z = result == 0;
                        V = result > short.MaxValue || result < short.MinValue;
                        C = X = (result & 0x10000) != 0;
                        WriteValueW(mode, reg, (short)result);
                    }
                    if (mode <= 1) PendingCycles -= 4;
                    else PendingCycles -= 8 + EACyclesBW[mode, reg];
                    return;
                }
                default: // long
                {
                    long result = PeekValueL(mode, reg) + data;
                    if (mode != 1)
                    {
                        N = result < 0;
                        Z = result == 0;
                        V = result > int.MaxValue || result < int.MinValue;
                        C = X = (result & 0x100000000) != 0;
                    }
                    WriteValueL(mode, reg, (int)result);
                    if (mode <= 1) PendingCycles -= 8;
                    else PendingCycles -= 12 + EACyclesL[mode, reg];
                    return;
                }
            }
        }

        private void ADDQ_Disasm(DisassemblyInfo info)
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

        private void ADDA()
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

        private void ADDA_Disasm(DisassemblyInfo info)
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

        private void SUB0()
        {
            int Dreg = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg = (op >> 0) & 7;

            switch (size)
            {
                case 0: // byte
                {
                    int result = D[Dreg].s8 - ReadValueB(mode, reg);
                    X = C = (result & 0x100) != 0;
                    V = result > sbyte.MaxValue || result < sbyte.MinValue;
                    N = result < 0;
                    Z = result == 0;
                    D[Dreg].s8 = (sbyte) result;
                    PendingCycles -= 4 + EACyclesBW[mode, reg];
                    return;
                }
                case 1: // word
                {
                    int result = D[Dreg].s16 - ReadValueW(mode, reg);
                    X = C = (result & 0x10000) != 0;
                    V = result > short.MaxValue || result < short.MinValue;
                    N = result < 0;
                    Z = result == 0;
                    D[Dreg].s16 = (short) result;
                    PendingCycles -= 4 + EACyclesBW[mode, reg];
                    return;
                }
                case 2: // long
                {
                    long result = D[Dreg].s32 - ReadValueL(mode, reg);
                    X = C = (result & 0x100000000) != 0;
                    V = result > int.MaxValue || result < int.MinValue;
                    N = result < 0;
                    Z = result == 0;
                    D[Dreg].s32 = (int)result;
                    PendingCycles -= 6 + EACyclesL[mode, reg];
                    return;
                }
            }
        }

        private void SUB1()
        {
            int Dreg = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg = (op >> 0) & 7;

            switch (size)
            {
                case 0: // byte
                {
                    int result = PeekValueB(mode, reg) - D[Dreg].s8;
                    X = C = (result & 0x100) != 0;
                    V = result > sbyte.MaxValue || result < sbyte.MinValue;
                    N = result < 0;
                    Z = result == 0;
                    WriteValueB(mode, reg, (sbyte) result);
                    PendingCycles -= 8 + EACyclesBW[mode, reg];
                    return;
                }
                case 1: // word
                {
                    int result = PeekValueW(mode, reg) - D[Dreg].s16;
                    X = C = (result & 0x10000) != 0;
                    V = result > short.MaxValue || result < short.MinValue;
                    N = result < 0;
                    Z = result == 0;
                    WriteValueW(mode, reg, (short) result);
                    PendingCycles -= 8 + EACyclesBW[mode, reg];
                    return;
                }
                case 2: // long
                {
                    long result = PeekValueL(mode, reg) - D[Dreg].s32;
                    X = C = (result & 0x100000000) != 0;
                    V = result > int.MaxValue || result < int.MinValue;
                    N = result < 0;
                    Z = result == 0;
                    WriteValueL(mode, reg, (int) result);
                    PendingCycles -= 12 + EACyclesL[mode, reg];
                    return;
                }
            }
        }

        private void SUB_Disasm(DisassemblyInfo info)
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

        private void SUBI()
        {
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            switch (size)
            {
                case 0: // byte
                {
                    int immed = (sbyte) ReadWord(PC); PC += 2;
                    int result = PeekValueB(mode, reg) - immed;
                    X = C = (result & 0x100) != 0;
                    V = result > sbyte.MaxValue || result < sbyte.MinValue;
                    N = result < 0;
                    Z = result == 0;
                    WriteValueB(mode, reg, (sbyte)result);
                    if (mode == 0) PendingCycles -= 8;
                    else PendingCycles -= 12 + EACyclesBW[mode, reg];
                    return;
                }
                case 1: // word
                {
                    int immed = ReadWord(PC); PC += 2;
                    int result = PeekValueW(mode, reg) - immed;
                    X = C = (result & 0x10000) != 0;
                    V = result > short.MaxValue || result < short.MinValue;
                    N = result < 0;
                    Z = result == 0;
                    WriteValueW(mode, reg, (short)result);
                    if (mode == 0) PendingCycles -= 8;
                    else PendingCycles -= 12 + EACyclesBW[mode, reg];
                    return;
                }
                case 2: // long
                {
                    int immed = ReadLong(PC); PC += 2;
                    long result = PeekValueL(mode, reg) - immed;
                    X = C = (result & 0x100000000) != 0;
                    V = result > int.MaxValue || result < int.MinValue;
                    N = result < 0;
                    Z = result == 0;
                    WriteValueL(mode, reg, (int)result);
                    if (mode == 0) PendingCycles -= 16;
                    else PendingCycles -= 20 + EACyclesBW[mode, reg];
                    return;
                }
            }
        }

        private void SUBI_Disasm(DisassemblyInfo info)
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

        private void SUBQ()
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
                    int result = PeekValueB(mode, reg) - data;
                    N = result < 0;
                    Z = result == 0;
                    V = result > sbyte.MaxValue || result < sbyte.MinValue;
                    C = X = (result & 0x100) != 0;
                    WriteValueB(mode, reg, (sbyte) result);
                    if (mode == 0) PendingCycles -= 4;
                    else PendingCycles -= 8 + EACyclesBW[mode, reg];
                    return;
                }
                case 1: // word
                {
                    int result = PeekValueW(mode, reg) - data;
                    if (mode != 1)
                    {
                        N = result < 0;
                        Z = result == 0;
                        V = result > short.MaxValue || result < short.MinValue;
                        C = X = (result & 0x10000) != 0;
                    }
                    WriteValueW(mode, reg, (short)result);
                    if (mode <= 1) PendingCycles -= 4;
                    else PendingCycles -= 8 + EACyclesBW[mode, reg];
                    return;
                }
                default: // long
                {
                    long result = PeekValueL(mode, reg) - data;
                    if (mode != 1)
                    {
                        N = result < 0;
                        Z = result == 0;
                        V = result > int.MaxValue || result < int.MinValue;
                        C = X = (result & 0x100000000) != 0;
                    }
                    WriteValueL(mode, reg, (int)result);
                    if (mode <= 1) PendingCycles -= 8;
                    else PendingCycles -= 12 + EACyclesL[mode, reg];
                    return;
                }
            }
        }

        private void SUBQ_Disasm(DisassemblyInfo info)
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

        private void SUBA()
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

        private void SUBA_Disasm(DisassemblyInfo info)
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

        private void CMP()
        {
            int dReg = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            switch (size)
            {
                case 0: // byte
                {
                    int result = ReadValueB(mode, reg) - D[dReg].s8;
                    N = result < 0;
                    Z = result == 0;
                    V = result > sbyte.MaxValue || result < sbyte.MinValue;
                    C = (result & 0x100) != 0;
                    if (mode == 0) PendingCycles -= 8;
                    PendingCycles -= 4 + EACyclesBW[mode, reg];
                    return;
                }
                case 1: // word
                {
                    int result = ReadValueW(mode, reg) - D[dReg].s16;
                    N = result < 0;
                    Z = result == 0;
                    V = result > short.MaxValue || result < short.MinValue;
                    C = (result & 0x10000) != 0;
                    if (mode == 0) PendingCycles -= 8;
                    PendingCycles -= 4 + EACyclesBW[mode, reg];
                    return;
                }
                case 2: // long
                {
                    long result = ReadValueL(mode, reg) - D[dReg].s32;
                    N = result < 0;
                    Z = result == 0;
                    V = result > int.MaxValue || result < int.MinValue;
                    C = (result & 0x100000000) != 0;
                    PendingCycles -= 6 + EACyclesL[mode, reg];
                    return;
                }
            }
        }

        private void CMP_Disasm(DisassemblyInfo info)
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

        private void CMPA()
        {
            int aReg = (op >> 9) & 7;
            int size = (op >> 8) & 1;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            switch (size)
            {
                case 0: // word
                {
                    long result = A[aReg].s32 - ReadValueW(mode, reg);
                    N = result < 0;
                    Z = result == 0;
                    V = result > int.MaxValue || result < int.MinValue;
                    C = (result & 0x100000000) != 0;
                    PendingCycles -= 6 + EACyclesBW[mode, reg];
                    return;
                }
                case 1: // long
                {
                    long result = A[aReg].s32 - ReadValueL(mode, reg);
                    N = result < 0;
                    Z = result == 0;
                    V = result > int.MaxValue || result < int.MinValue;
                    C = (result & 0x100000000) != 0;
                    PendingCycles -= 6 + EACyclesL[mode, reg];
                    return;
                }
            }
        }

        private void CMPA_Disasm(DisassemblyInfo info)
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


        private void CMPI()
        {
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg = (op >> 0) & 7;

            switch (size)
            {
                case 0: // byte
                {
                    int immed = (sbyte) ReadWord(PC); PC += 2;
                    int result = ReadValueB(mode, reg) - immed;
                    N = result < 0;
                    Z = result == 0;
                    V = result > sbyte.MaxValue || result < sbyte.MinValue;
                    C = (result & 0x100) != 0;
                    if (mode == 0) PendingCycles -= 8;
                    else PendingCycles -= 8 + EACyclesBW[mode, reg];
                    return;
                }
                case 1: // word
                {
                    int immed = ReadWord(PC); PC += 2;
                    int result = ReadValueW(mode, reg) - immed;
                    N = result < 0;
                    Z = result == 0;
                    V = result > short.MaxValue || result < short.MinValue;
                    C = (result & 0x10000) != 0;
                    if (mode == 0) PendingCycles -= 8;
                    else PendingCycles -= 8 + EACyclesBW[mode, reg];
                    return;
                }
                case 2: // long
                {
                    int immed = ReadLong(PC); PC += 4;
                    long result = ReadValueL(mode, reg) - immed;
                    N = result < 0;
                    Z = result == 0;
                    V = result > int.MaxValue || result < int.MinValue;
                    C = (result & 0x100000000) != 0;
                    if (mode == 0) PendingCycles -= 14;
                    else PendingCycles -= 12 + EACyclesL[mode, reg];
                    return;
                }
            }
        }

        private void CMPI_Disasm(DisassemblyInfo info)
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
