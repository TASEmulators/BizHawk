using System;

namespace BizHawk.Emulation.CPUs.M68K
{
    partial class MC68000
    {
        void ANDI() // AND immediate
        {
            int size    = ((op >> 6) & 0x03);
            int dstMode = ((op >> 3) & 0x07);
            int dstReg  = (op & 0x07);

            V = false;
            C = false;

            switch (size)
            {
                case 0: // Byte
                    {
                        sbyte imm = (sbyte) ReadWord(PC); PC += 2;
                        sbyte arg = PeekValueB(dstMode, dstReg);
                        sbyte result = (sbyte) (imm & arg);
                        WriteValueB(dstMode, dstReg, result);
                        PendingCycles -= (dstMode == 0) ? 8 : 12 + EACyclesBW[dstMode, dstReg];
                        N = (result & 0x80) != 0;
                        Z = (result == 0);
                        return;
                    }
                case 1: // Word
                    {
                        short imm = ReadWord(PC); PC += 2;
                        short arg = PeekValueW(dstMode, dstReg);
                        short result = (short) (imm & arg);
                        WriteValueW(dstMode, dstReg, result);
                        PendingCycles -= (dstMode == 0) ? 8 : 12 + EACyclesBW[dstMode, dstReg];
                        N = (result & 0x8000) != 0;
                        Z = (result == 0);
                        return;
                    }
                case 2: // Long
                    {
                        int imm = ReadLong(PC); PC += 2;
                        int arg = PeekValueL(dstMode, dstReg);
                        int result = imm & arg;
                        WriteValueL(dstMode, dstReg, result);
                        PendingCycles -= (dstMode == 0) ? 8 : 12 + EACyclesL[dstMode, dstReg];
                        N = (result & 0x80000000) != 0;
                        Z = (result == 0);
                        return;
                    }
            }
        }

        void ANDI_Disasm(DisassemblyInfo info)
        {
            int size = ((op >> 6) & 0x03);
            int dstMode = ((op >> 3) & 0x07);
            int dstReg = (op & 0x07);

            int pc = info.PC + 2;

            switch (size)
            {
                case 0: // Byte
                    {
                        info.Mnemonic = "andi.b";
                        sbyte imm = (sbyte)ReadWord(pc); pc += 2;
                        info.Args = string.Format("${0:X}, ", imm);
                        info.Args += DisassembleValue(dstMode, dstReg, 1, ref pc);
                        break;
                    }
                case 1: // Word
                    {
                        info.Mnemonic = "andi.w";
                        short imm = ReadWord(pc); pc += 2;
                        info.Args = string.Format("${0:X}, ", imm);
                        info.Args += DisassembleValue(dstMode, dstReg, 2, ref pc);
                        break;
                    }
                case 2: // Long
                    {
                        info.Mnemonic = "andi.l";
                        int imm = ReadLong(pc); pc += 4;
                        info.Args = string.Format("${0:X}, ", imm);
                        info.Args += DisassembleValue(dstMode, dstReg, 4, ref pc);
                        break;
                    }
            }

            info.Length = pc - info.PC;
        }

        void ORI()
        {
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg = (op >> 0) & 7;

            V = C = false;

            switch (size)
            {
                case 0: // byte
                {
                    sbyte immed = (sbyte) ReadWord(PC); PC += 2;
                    sbyte value = (sbyte) (PeekValueB(mode, reg) | immed);
                    WriteValueB(mode, reg, value);
                    N = (value & 0x80) != 0;
                    Z = value == 0;
                    PendingCycles -= mode == 0 ? 8 : 12 + EACyclesBW[mode, reg];
                    return;
                }
                case 1: // word
                {
                    short immed = ReadWord(PC); PC += 2;
                    short value = (short)(PeekValueW(mode, reg) | immed);
                    WriteValueW(mode, reg, value);
                    N = (value & 0x8000) != 0;
                    Z = value == 0;
                    PendingCycles -= mode == 0 ? 8 : 12 + EACyclesBW[mode, reg];
                    return;
                }
                case 2: // long
                {
                    int immed = ReadLong(PC); PC += 4;
                    int value = PeekValueL(mode, reg) | immed;
                    WriteValueL(mode, reg, value);
                    N = (value & 0x80000000) != 0;
                    Z = value == 0;
                    PendingCycles -= mode == 0 ? 16 : 20 + EACyclesL[mode, reg];
                    return;
                }
            }
        }

        void ORI_Disasm(DisassemblyInfo info)
        {
            int pc = info.PC + 2;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            switch (size)
            {
                case 0: // byte
                {
                    info.Mnemonic = "ori.b";
                    sbyte immed = (sbyte) ReadWord(pc); pc += 2;
                    info.Args = String.Format("${0:X}, {1}", immed, DisassembleValue(mode, reg, 1, ref pc));
                    break;
                }
                case 1: // word
                {
                    info.Mnemonic = "ori.w";
                    short immed = ReadWord(pc); pc += 2;
                    info.Args = String.Format("${0:X}, {1}", immed, DisassembleValue(mode, reg, 2, ref pc));
                    break;
                }
                case 2: // long
                {
                    info.Mnemonic = "ori.l";
                    int immed = ReadLong(pc); pc += 4;
                    info.Args = String.Format("${0:X}, {1}", immed, DisassembleValue(mode, reg, 4, ref pc));
                    break;
                }
            }

            info.Length = pc - info.PC;
        }

        void OR()
        {
            throw new Exception();
            /*int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg = (op >> 0) & 7;

            V = C = false;

            switch (size)
            {
                case 0: // byte
                    {
                        sbyte immed = (sbyte)ReadWord(PC); PC += 2;
                        sbyte value = (sbyte)(PeekValueB(mode, reg) | immed);
                        WriteValueB(mode, reg, value);
                        N = (value & 0x80) != 0;
                        Z = value == 0;
                        PendingCycles -= mode == 0 ? 8 : 12 + EACyclesBW[mode, reg];
                        return;
                    }
                case 1: // word
                    {
                        short immed = ReadWord(PC); PC += 2;
                        short value = (short)(PeekValueW(mode, reg) | immed);
                        WriteValueW(mode, reg, value);
                        N = (value & 0x8000) != 0;
                        Z = value == 0;
                        PendingCycles -= mode == 0 ? 8 : 12 + EACyclesBW[mode, reg];
                        return;
                    }
                case 2: // long
                    {
                        int immed = ReadLong(PC); PC += 4;
                        int value = PeekValueL(mode, reg) | immed;
                        WriteValueL(mode, reg, value);
                        N = (value & 0x80000000) != 0;
                        Z = value == 0;
                        PendingCycles -= mode == 0 ? 17 : 20 + EACyclesL[mode, reg];
                        return;
                    }
            }*/
        }

        void OR_Disasm(DisassemblyInfo info)
        {
            int pc = info.PC + 2;
            int dReg = (op >> 9) & 3;
            int d = (op >> 8) & 1;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg = (op >> 0) & 7;

            switch (size)
            {
                case 0: // byte
                    {
                        info.Mnemonic = "ori.b";
                        sbyte immed = (sbyte)ReadWord(pc); pc += 2;
                        info.Args = String.Format("${0:X}, {1}", immed, DisassembleValue(mode, reg, 1, ref pc));
                        break;
                    }
                case 1: // word
                    {
                        info.Mnemonic = "ori.w";
                        short immed = ReadWord(pc); pc += 2;
                        info.Args = String.Format("${0:X}, {1}", immed, DisassembleValue(mode, reg, 2, ref pc));
                        break;
                    }
                case 2: // long
                    {
                        info.Mnemonic = "ori.l";
                        int immed = ReadLong(pc); pc += 4;
                        info.Args = String.Format("${0:X}, {1}", immed, DisassembleValue(mode, reg, 4, ref pc));
                        break;
                    }
            }

            info.Length = pc - info.PC;
        }


        void LSLd()
        {
            int rot = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int m = (op >> 5) & 1;
            int reg = op & 7;

            if (m == 0 && rot == 0) rot = 8;
            else if (m == 1) rot = D[rot].s32 & 63;

            V = false;
            C = false;

            switch (size)
            {
                case 0: // byte
                    for (int i=0; i<rot; i++)
                    {
                        C = X = (D[reg].u8 & 0x80) != 0;
                        D[reg].u8 <<= 1;
                    }
                    N = (D[reg].s8 & 0x80) != 0;
                    Z = D[reg].u8 == 0;
                    PendingCycles -= 6 + (rot * 2);
                    return;
                case 1: // word
                    for (int i = 0; i < rot; i++)
                    {
                        C = X = (D[reg].u16 & 0x8000) != 0;
                        D[reg].u16 <<= 1;
                    }
                    N = (D[reg].s16 & 0x8000) != 0;
                    Z = D[reg].u16 == 0;
                    PendingCycles -= 6 + (rot * 2);
                    return;
                case 2: // long
                    for (int i = 0; i < rot; i++)
                    {
                        C = X = (D[reg].u32 & 0x80000000) != 0;
                        D[reg].u32 <<= 1;
                    }
                    N = (D[reg].s32 & 0x80000000) != 0;
                    Z = D[reg].u32 == 0;
                    PendingCycles -= 8 + (rot * 2);
                    return;
            }
        }

        void LSLd_Disasm(DisassemblyInfo info)
        {
            int pc = info.PC + 2;
            int rot = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int m = (op >> 5) & 1;
            int reg = op & 7;

            if (m == 0 && rot == 0) rot = 8;

            switch (size)
            {
                case 0: info.Mnemonic = "lsl.b"; break;
                case 1: info.Mnemonic = "lsl.w"; break;
                case 2: info.Mnemonic = "lsl.l"; break;
            }
            if (m==0) info.Args = rot+", D"+reg;
            else info.Args = "D"+rot+", D"+reg;

            info.Length = pc - info.PC;
        }

        void LSRd()
        {
            int rot = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int m = (op >> 5) & 1;
            int reg = op & 7;

            if (m == 0 && rot == 0) rot = 8;
            else if (m == 1) rot = D[rot].s32 & 63;

            V = false;
            C = false;

            switch (size)
            {
                case 0: // byte
                    for (int i = 0; i < rot; i++)
                    {
                        C = X = (D[reg].u8 & 1) != 0;
                        D[reg].u8 >>= 1;
                    }
                    N = (D[reg].s8 & 0x80) != 0;
                    Z = D[reg].u8 == 0;
                    PendingCycles -= 6 + (rot * 2);
                    return;
                case 1: // word
                    for (int i = 0; i < rot; i++)
                    {
                        C = X = (D[reg].u16 & 1) != 0;
                        D[reg].u16 >>= 1;
                    }
                    N = (D[reg].s16 & 0x8000) != 0;
                    Z = D[reg].u16 == 0;
                    PendingCycles -= 6 + (rot * 2);
                    return;
                case 2: // long
                    for (int i = 0; i < rot; i++)
                    {
                        C = X = (D[reg].u32 & 1) != 0;
                        D[reg].u32 >>= 1;
                    }
                    N = (D[reg].s32 & 0x80000000) != 0;
                    Z = D[reg].u32 == 0;
                    PendingCycles -= 8 + (rot * 2);
                    return;
            }
        }

        void LSRd_Disasm(DisassemblyInfo info)
        {
            int pc = info.PC + 2;
            int rot = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int m = (op >> 5) & 1;
            int reg = op & 7;

            if (m == 0 && rot == 0) rot = 8;

            switch (size)
            {
                case 0: info.Mnemonic = "lsr.b"; break;
                case 1: info.Mnemonic = "lsr.w"; break;
                case 2: info.Mnemonic = "lsr.l"; break;
            }
            if (m == 0) info.Args = rot + ", D" + reg;
            else info.Args = "D" + rot + ", D" + reg;

            info.Length = pc - info.PC;
        }

        void ASLd()
        {
            int rot = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int m = (op >> 5) & 1;
            int reg = op & 7;

            if (m == 0 && rot == 0) rot = 8;
            else if (m == 1) rot = D[rot].s32 & 63;

            V = false;
            C = false;

            switch (size)
            {
                case 0: // byte
                    for (int i = 0; i < rot; i++)
                    {
                        C = X = (D[reg].u8 & 0x80) != 0;
                        D[reg].s8 <<= 1;
                    }
                    N = (D[reg].s8 & 0x80) != 0;
                    Z = D[reg].u8 == 0;
                    PendingCycles -= 6 + (rot * 2);
                    return;
                case 1: // word
                    for (int i = 0; i < rot; i++)
                    {
                        C = X = (D[reg].u16 & 0x8000) != 0;
                        D[reg].s16 <<= 1;
                    }
                    N = (D[reg].s16 & 0x8000) != 0;
                    Z = D[reg].u16 == 0;
                    PendingCycles -= 6 + (rot * 2);
                    return;
                case 2: // long
                    for (int i = 0; i < rot; i++)
                    {
                        C = X = (D[reg].u32 & 0x80000000) != 0;
                        D[reg].s32 <<= 1;
                    }
                    N = (D[reg].s32 & 0x80000000) != 0;
                    Z = D[reg].u32 == 0;
                    PendingCycles -= 8 + (rot * 2);
                    return;
            }
        }

        void ASLd_Disasm(DisassemblyInfo info)
        {
            int pc = info.PC + 2;
            int rot = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int m = (op >> 5) & 1;
            int reg = op & 7;

            if (m == 0 && rot == 0) rot = 8;

            switch (size)
            {
                case 0: info.Mnemonic = "asl.b"; break;
                case 1: info.Mnemonic = "asl.w"; break;
                case 2: info.Mnemonic = "asl.l"; break;
            }
            if (m == 0) info.Args = rot + ", D" + reg;
            else info.Args = "D" + rot + ", D" + reg;

            info.Length = pc - info.PC;
        }

        void ASRd()
        {
            int rot = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int m = (op >> 5) & 1;
            int reg = op & 7;

            if (m == 0 && rot == 0) rot = 8;
            else if (m == 1) rot = D[rot].s32 & 63;

            V = false;
            C = false;

            switch (size)
            {
                case 0: // byte
                    for (int i = 0; i < rot; i++)
                    {
                        C = X = (D[reg].u8 & 1) != 0;
                        D[reg].s8 >>= 1;
                    }
                    N = (D[reg].s8 & 0x80) != 0;
                    Z = D[reg].u8 == 0;
                    PendingCycles -= 6 + (rot * 2);
                    return;
                case 1: // word
                    for (int i = 0; i < rot; i++)
                    {
                        C = X = (D[reg].u16 & 1) != 0;
                        D[reg].s16 >>= 1;
                    }
                    N = (D[reg].s16 & 0x8000) != 0;
                    Z = D[reg].u16 == 0;
                    PendingCycles -= 6 + (rot * 2);
                    return;
                case 2: // long
                    for (int i = 0; i < rot; i++)
                    {
                        C = X = (D[reg].u32 & 1) != 0;
                        D[reg].s32 >>= 1;
                    }
                    N = (D[reg].s32 & 0x80000000) != 0;
                    Z = D[reg].u32 == 0;
                    PendingCycles -= 8 + (rot * 2);
                    return;
            }
        }

        void ASRd_Disasm(DisassemblyInfo info)
        {
            int pc = info.PC + 2;
            int rot = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int m = (op >> 5) & 1;
            int reg = op & 7;

            if (m == 0 && rot == 0) rot = 8;

            switch (size)
            {
                case 0: info.Mnemonic = "asr.b"; break;
                case 1: info.Mnemonic = "asr.w"; break;
                case 2: info.Mnemonic = "asr.l"; break;
            }
            if (m == 0) info.Args = rot + ", D" + reg;
            else info.Args = "D" + rot + ", D" + reg;

            info.Length = pc - info.PC;
        }

        void ROLd()
        {
            int rot = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int m = (op >> 5) & 1;
            int reg = op & 7;

            if (m == 0 && rot == 0) rot = 8;
            else if (m == 1) rot = D[rot].s32 & 63;

            V = false;
            C = false;

            switch (size)
            {
                case 0: // byte
                    for (int i = 0; i < rot; i++)
                    {
                        C = (D[reg].u8 & 0x80) != 0;
                        D[reg].u8 = (byte) ((D[reg].u8 << 1) | (D[reg].u8 >> 7));
                    }
                    N = (D[reg].s8 & 0x80) != 0;
                    Z = D[reg].u8 == 0;
                    PendingCycles -= 6 + (rot * 2);
                    return;
                case 1: // word
                    for (int i = 0; i < rot; i++)
                    {
                        C = (D[reg].u16 & 0x8000) != 0;
                        D[reg].u16 = (ushort) ((D[reg].u16 << 1) | (D[reg].u16 >> 15));
                    }
                    N = (D[reg].s16 & 0x8000) != 0;
                    Z = D[reg].u16 == 0;
                    PendingCycles -= 6 + (rot * 2);
                    return;
                case 2: // long
                    for (int i = 0; i < rot; i++)
                    {
                        C = (D[reg].u32 & 0x80000000) != 0;
                        D[reg].u32 = ((D[reg].u32 << 1) | (D[reg].u32 >> 31));
                    }
                    N = (D[reg].s32 & 0x80000000) != 0;
                    Z = D[reg].u32 == 0;
                    PendingCycles -= 8 + (rot * 2);
                    return;
            }
        }

        void ROLd_Disasm(DisassemblyInfo info)
        {
            int pc = info.PC + 2;
            int rot = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int m = (op >> 5) & 1;
            int reg = op & 7;

            if (m == 0 && rot == 0) rot = 8;

            switch (size)
            {
                case 0: info.Mnemonic = "rol.b"; break;
                case 1: info.Mnemonic = "rol.w"; break;
                case 2: info.Mnemonic = "rol.l"; break;
            }
            if (m == 0) info.Args = rot + ", D" + reg;
            else info.Args = "D" + rot + ", D" + reg;

            info.Length = pc - info.PC;
        }

        void RORd()
        {
            int rot = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int m = (op >> 5) & 1;
            int reg = op & 7;

            if (m == 0 && rot == 0) rot = 8;
            else if (m == 1) rot = D[rot].s32 & 63;

            V = false;
            C = false;

            switch (size)
            {
                case 0: // byte
                    for (int i = 0; i < rot; i++)
                    {
                        C = (D[reg].u8 & 1) != 0;
                        D[reg].u8 = (byte)((D[reg].u8 >> 1) | (D[reg].u8 << 7));
                    }
                    N = (D[reg].s8 & 0x80) != 0;
                    Z = D[reg].u8 == 0;
                    PendingCycles -= 6 + (rot * 2);
                    return;
                case 1: // word
                    for (int i = 0; i < rot; i++)
                    {
                        C = (D[reg].u16 & 1) != 0;
                        D[reg].u16 = (ushort)((D[reg].u16 >> 1) | (D[reg].u16 << 15));
                    }
                    N = (D[reg].s16 & 0x8000) != 0;
                    Z = D[reg].u16 == 0;
                    PendingCycles -= 6 + (rot * 2);
                    return;
                case 2: // long
                    for (int i = 0; i < rot; i++)
                    {
                        C = (D[reg].u32 & 1) != 0;
                        D[reg].u32 = ((D[reg].u32 >> 1) | (D[reg].u32 << 31));
                    }
                    N = (D[reg].s32 & 0x80000000) != 0;
                    Z = D[reg].u32 == 0;
                    PendingCycles -= 8 + (rot * 2);
                    return;
            }
        }

        void RORd_Disasm(DisassemblyInfo info)
        {
            int pc = info.PC + 2;
            int rot = (op >> 9) & 7;
            int size = (op >> 6) & 3;
            int m = (op >> 5) & 1;
            int reg = op & 7;

            if (m == 0 && rot == 0) rot = 8;

            switch (size)
            {
                case 0: info.Mnemonic = "ror.b"; break;
                case 1: info.Mnemonic = "ror.w"; break;
                case 2: info.Mnemonic = "ror.l"; break;
            }
            if (m == 0) info.Args = rot + ", D" + reg;
            else info.Args = "D" + rot + ", D" + reg;

            info.Length = pc - info.PC;
        }

        void SWAP()
        {
            int reg = op & 7;
            D[reg].u32 = (D[reg].u32 << 16) | (D[reg].u32 >> 16);
            V = C = false;
            Z = D[reg].u32 == 0;
            N = (D[reg].s32 & 0x80000000) != 0;
            PendingCycles -= 4;
        }

        void SWAP_Disasm(DisassemblyInfo info)
        {
            int reg = op & 7;
            info.Mnemonic = "swap";
            info.Args = "D" + reg;
        }
    }
}
