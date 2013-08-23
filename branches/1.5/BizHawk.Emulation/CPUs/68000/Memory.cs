using System;

namespace BizHawk.Emulation.CPUs.M68000
{
    partial class MC68000
    {
        sbyte ReadValueB(int mode, int reg)
        {
            sbyte value;
            switch (mode)
            {
                case 0: // Dn
                    return D[reg].s8;
                case 1: // An
                    return A[reg].s8;
                case 2: // (An)
                    return ReadByte(A[reg].s32);
                case 3: // (An)+
                    value = ReadByte(A[reg].s32);
                    A[reg].s32 += reg == 7 ? 2 : 1;
                    return value;
                case 4: // -(An)
                    A[reg].s32 -= reg == 7 ? 2 : 1;
                    return ReadByte(A[reg].s32);
                case 5: // (d16,An)
                    value = ReadByte((A[reg].s32 + ReadWord(PC))); PC += 2;
                    return value;
                case 6: // (d8,An,Xn)
                    return ReadByte(A[reg].s32 + GetIndex());
                case 7:
                    switch (reg)
                    {
                        case 0: // (imm).W
                            value = ReadByte(ReadWord(PC)); PC += 2;
                            return value;
                        case 1: // (imm).L
                            value = ReadByte(ReadLong(PC)); PC += 4;
                            return value;
                        case 2: // (d16,PC)
                            value = ReadByte(PC + ReadWord(PC)); PC += 2;
                            return value;
                        case 3: // (d8,PC,Xn)
                            int pc = PC;
                            value = ReadByte((pc + GetIndex()));
                            return value;
                        case 4: // immediate
                            value = (sbyte) ReadWord(PC); PC += 2;
                            return value;
                        default:
                            throw new Exception("Invalid addressing mode!");
                    }
            }
            throw new Exception("Invalid addressing mode!");
        }

        short ReadValueW(int mode, int reg)
        {
            short value;
            switch (mode)
            {
                case 0: // Dn
                    return D[reg].s16;
                case 1: // An
                    return A[reg].s16;
                case 2: // (An)
                    return ReadWord(A[reg].s32);
                case 3: // (An)+
                    value = ReadWord(A[reg].s32);
                    A[reg].s32 += 2;
                    return value;
                case 4: // -(An)
                    A[reg].s32 -= 2;
                    return ReadWord(A[reg].s32);
                case 5: // (d16,An)
                    value = ReadWord((A[reg].s32 + ReadWord(PC))); PC += 2;
                    return value;
                case 6: // (d8,An,Xn)
                    return ReadWord(A[reg].s32 + GetIndex());
                case 7:
                    switch (reg)
                    {
                        case 0: // (imm).W
                            value = ReadWord(ReadWord(PC)); PC += 2;
                            return value;
                        case 1: // (imm).L
                            value = ReadWord(ReadLong(PC)); PC += 4;
                            return value;
                        case 2: // (d16,PC)
                            value = ReadWord(PC + ReadWord(PC)); PC += 2;
                            return value;
                        case 3: // (d8,PC,Xn)
                            int pc = PC;
                            value = ReadWord((pc + GetIndex()));
                            return value;
                        case 4: // immediate
                            value = ReadWord(PC); PC += 2;
                            return value;
                        default:
                            throw new Exception("Invalid addressing mode!");
                    }
            }
            throw new Exception("Invalid addressing mode!");
        }

        int ReadValueL(int mode, int reg)
        {
            int value;
            switch (mode)
            {
                case 0: // Dn
                    return D[reg].s32;
                case 1: // An
                    return A[reg].s32;
                case 2: // (An)
                    return ReadLong(A[reg].s32);
                case 3: // (An)+
                    value = ReadLong(A[reg].s32);
                    A[reg].s32 += 4;
                    return value;
                case 4: // -(An)
                    A[reg].s32 -= 4;
                    return ReadLong(A[reg].s32);
                case 5: // (d16,An)
                    value = ReadLong((A[reg].s32 + ReadWord(PC))); PC += 2;
                    return value;
                case 6: // (d8,An,Xn)
                    return ReadLong(A[reg].s32 + GetIndex());
                case 7:
                    switch (reg)
                    {
                        case 0: // (imm).W
                            value = ReadLong(ReadWord(PC)); PC += 2;
                            return value;
                        case 1: // (imm).L
                            value = ReadLong(ReadLong(PC)); PC += 4;
                            return value;
                        case 2: // (d16,PC)
                            value = ReadLong(PC + ReadWord(PC)); PC += 2;
                            return value;
                        case 3: // (d8,PC,Xn)
                            int pc = PC;
                            value = ReadLong((pc + GetIndex()));
                            return value;
                        case 4: // immediate
                            value = ReadLong(PC); PC += 4;
                            return value;
                        default:
                            throw new Exception("Invalid addressing mode!");
                    }
            }
            throw new Exception("Invalid addressing mode!");
        }

        sbyte PeekValueB(int mode, int reg)
        {
            sbyte value;
            switch (mode)
            {
                case 0: // Dn
                    return D[reg].s8;
                case 1: // An
                    return A[reg].s8;
                case 2: // (An)
                    return ReadByte(A[reg].s32);
                case 3: // (An)+
                    value = ReadByte(A[reg].s32);
                    A[reg].s32 += reg == 7 ? 2 : 1;
                    return value;
                case 4: // -(An)
                    A[reg].s32 -= reg == 7 ? 2 : 1;
                    return ReadByte(A[reg].s32);
                case 5: // (d16,An)
                    value = ReadByte((A[reg].s32 + ReadWord(PC)));
                    return value;
                case 6: // (d8,An,Xn)
                    return ReadByte(A[reg].s32 + PeekIndex());
                case 7:
                    switch (reg)
                    {
                        case 0: // (imm).W
                            value = ReadByte(ReadWord(PC));
                            return value;
                        case 1: // (imm).L
                            value = ReadByte(ReadLong(PC));
                            return value;
                        case 2: // (d16,PC)
                            value = ReadByte(PC + ReadWord(PC));
                            return value;
                        case 3: // (d8,PC,Xn)
                            value = ReadByte((PC + PeekIndex()));
                            return value;
                        case 4: // immediate
                            return (sbyte) ReadWord(PC);
                        default:
                            throw new Exception("Invalid addressing mode!");
                    }
            }
            throw new Exception("Invalid addressing mode!");
        }

        short PeekValueW(int mode, int reg)
        {
            short value;
            switch (mode)
            {
                case 0: // Dn
                    return D[reg].s16;
                case 1: // An
                    return A[reg].s16;
                case 2: // (An)
                    return ReadWord(A[reg].s32);
                case 3: // (An)+
                    value = ReadWord(A[reg].s32);
                    A[reg].s32 += 2;
                    return value;
                case 4: // -(An)
                    A[reg].s32 -= 2;
                    return ReadWord(A[reg].s32);
                case 5: // (d16,An)
                    value = ReadWord((A[reg].s32 + ReadWord(PC)));
                    return value;
                case 6: // (d8,An,Xn)
                    return ReadWord(A[reg].s32 + PeekIndex());
                case 7:
                    switch (reg)
                    {
                        case 0: // (imm).W
                            value = ReadWord(ReadWord(PC));
                            return value;
                        case 1: // (imm).L
                            value = ReadWord(ReadLong(PC));
                            return value;
                        case 2: // (d16,PC)
                            value = ReadWord(PC + ReadWord(PC));
                            return value;
                        case 3: // (d8,PC,Xn)
                            value = ReadWord((PC + PeekIndex()));
                            return value;
                        case 4: // immediate
                            return ReadWord(PC);
                        default:
                            throw new Exception("Invalid addressing mode!");
                    }
            }
            throw new Exception("Invalid addressing mode!");
        }

        int PeekValueL(int mode, int reg)
        {
            int value;
            switch (mode)
            {
                case 0: // Dn
                    return D[reg].s32;
                case 1: // An
                    return A[reg].s32;
                case 2: // (An)
                    return ReadLong(A[reg].s32);
                case 3: // (An)+
                    value = ReadLong(A[reg].s32);
                    A[reg].s32 += 4;
                    return value;
                case 4: // -(An)
                    A[reg].s32 -= 4;
                    return ReadLong(A[reg].s32);
                case 5: // (d16,An)
                    value = ReadLong((A[reg].s32 + ReadWord(PC)));
                    return value;
                case 6: // (d8,An,Xn)
                    return ReadLong(A[reg].s32 + PeekIndex());
                case 7:
                    switch (reg)
                    {
                        case 0: // (imm).W
                            value = ReadLong(ReadWord(PC));
                            return value;
                        case 1: // (imm).L
                            value = ReadLong(ReadLong(PC));
                            return value;
                        case 2: // (d16,PC)
                            value = ReadLong(PC + ReadWord(PC));
                            return value;
                        case 3: // (d8,PC,Xn)
                            value = ReadLong((PC + PeekIndex()));
                            return value;
                        case 4: // immediate
                            return ReadLong(PC);
                        default:
                            throw new Exception("Invalid addressing mode!");
                    }
            }
            throw new Exception("Invalid addressing mode!");
        }

        int ReadAddress(int mode, int reg)
        {
            int addr;
            switch (mode)
            {
                case 0: throw new Exception("Invalid addressing mode!"); // Dn
                case 1: throw new Exception("Invalid addressing mode!"); // An
                case 2: return A[reg].s32; // (An)
                case 3: return A[reg].s32; // (An)+
                case 4: return A[reg].s32; // -(An)
                case 5: addr = A[reg].s32 + ReadWord(PC); PC += 2; return addr; // (d16,An)
                case 6: return A[reg].s32 + GetIndex(); // (d8,An,Xn)
                case 7:
                    switch (reg)
                    {
                        case 0: addr = ReadWord(PC); PC += 2; return addr; // (imm).w
                        case 1: addr = ReadLong(PC); PC += 4; return addr; // (imm).l
                        case 2: addr = PC; addr += ReadWord(PC); PC += 2; return addr; // (d16,PC)
                        case 3: addr = PC; addr += GetIndex(); return addr; // (d8,PC,Xn)
                        case 4: throw new Exception("Invalid addressing mode!"); // immediate
                    }
                    break;
            }
            throw new Exception("Invalid addressing mode!");
        }

        string DisassembleValue(int mode, int reg, int size, ref int pc)
        {
            string value;
            int addr;
            switch (mode)
            {
                case 0: return "D"+reg;       // Dn
                case 1: return "A"+reg;       // An
                case 2: return "(A"+reg+")";  // (An)
                case 3: return "(A"+reg+")+"; // (An)+
                case 4: return "-(A"+reg+")"; // -(An)
                case 5: value = string.Format("(${0:X},A{1})", ReadWord(pc), reg); pc += 2; return value; // (d16,An)
                case 6: addr = ReadWord(pc); pc += 2; return DisassembleIndex("A" + reg, (short) addr); // (d8,An,Xn)
                case 7:
                    switch (reg)
                    {
                        case 0: value = String.Format("(${0:X})", ReadWord(pc)); pc += 2; return value; // (imm).W
                        case 1: value = String.Format("(${0:X})", ReadLong(pc)); pc += 4; return value; // (imm).L
                        case 2: value = String.Format("(${0:X})", pc + ReadWord(pc)); pc += 2; return value; // (d16,PC)
                        case 3: addr = ReadWord(pc); pc += 2; return DisassembleIndex("PC", (short)addr); // (d8,PC,Xn)
                        case 4:
                            switch (size)
                            {
                                case 1: value = String.Format("${0:X}", (byte)ReadWord(pc)); pc += 2; return value;
                                case 2: value = String.Format("${0:X}", ReadWord(pc)); pc += 2; return value;
                                case 4: value = String.Format("${0:X}", ReadLong(pc)); pc += 4; return value;
                            }
                            break;
                    }
                    break;
            }
            throw new Exception("Invalid addressing mode!");
        }

        string DisassembleImmediate(int size, ref int pc)
        {
            int immed;
            switch (size)
            {
                case 1:
                    immed = (byte)ReadWord(pc); pc += 2;
                    return String.Format("${0:X}", immed);
                case 2:
                    immed = (ushort) ReadWord(pc); pc += 2;
                    return String.Format("${0:X}", immed);
                case 4:
                    immed = ReadLong(pc); pc += 4;
                    return String.Format("${0:X}", immed);
            }
            throw new ArgumentException("Invalid size");
        }

        string DisassembleAddress(int mode, int reg, ref int pc)
        {
            int addr;
            switch (mode)
            {
                case 0: return "INVALID"; // Dn
                case 1: return "INVALID"; // An
                case 2: return "(A"+reg+")"; // (An)
                case 3: return "(A"+reg+")+"; // (An)+
                case 4: return "-(A"+reg+")"; // -(An)
                case 5: addr = ReadWord(pc); pc += 2; return String.Format("({0},A{1})", addr, reg); // (d16,An)
                case 6: addr = ReadWord(pc); pc += 2; return DisassembleIndex("A" + reg, (short)addr); // (d8,An,Xn)
                case 7:
                    switch (reg)
                    {
                        case 0: addr = ReadWord(pc); pc += 2; return String.Format("${0:X}.w",addr); // (imm).w
                        case 1: addr = ReadLong(pc); pc += 4; return String.Format("${0:X}.l",addr); // (imm).l
                        case 2: addr = ReadWord(pc); pc += 2; return String.Format("(${0:X},PC)",addr); // (d16,PC)
                        case 3: addr = ReadWord(pc); pc += 2; return DisassembleIndex("PC", (short)addr); // (d8,PC,Xn)
                        case 4: return "INVALID"; // immediate
                    }
                    break;
            }
            throw new Exception("Invalid addressing mode!");
        }

        void WriteValueB(int mode, int reg, sbyte value)
        {
            switch (mode)
            {
                case 0x00: // Dn
                    D[reg].s8 = value;
                    return;
                case 0x01: // An
                    A[reg].s32 = value;
                    return;
                case 0x02: // (An)
                    WriteByte(A[reg].s32, value);
                    return;
                case 0x03: // (An)+
                    WriteByte(A[reg].s32, value);
                    A[reg].s32 += reg == 7 ? 2 : 1;
                    return;
                case 0x04: // -(An)
                    A[reg].s32 -= reg == 7 ? 2 : 1;
                    WriteByte(A[reg].s32, value);
                    return;
                case 0x05: // (d16,An)
                    WriteByte(A[reg].s32 + ReadWord(PC), value); PC += 2;
                    return;
                case 0x06: // (d8,An,Xn)
                    WriteByte(A[reg].s32 + GetIndex(), value);
                    return;
                case 0x07: 
                    switch (reg)
                    {
                        case 0x00: // (imm).W
                            WriteByte(ReadWord(PC), value); PC += 2;
                            return;
                        case 0x01: // (imm).L
                            WriteByte(ReadLong(PC), value); PC += 4;
                            return;
                        case 0x02: // (d16,PC)
                            WriteByte(PC + ReadWord(PC), value); PC += 2;
                            return;
                        case 0x03: // (d8,PC,Xn)
                            int pc = PC;
                            WriteByte(pc + PeekIndex(), value);
                            PC += 2;
                            return;
                        default: throw new Exception("Invalid addressing mode!");
                    }
            }
        }

        void WriteValueW(int mode, int reg, short value)
        {
            switch (mode)
            {
                case 0x00: // Dn
                    D[reg].s16 = value;
                    return;
                case 0x01: // An
                    A[reg].s32 = value;
                    return;
                case 0x02: // (An)
                    WriteWord(A[reg].s32, value);
                    return;
                case 0x03: // (An)+
                    WriteWord(A[reg].s32, value);
                    A[reg].s32 += 2;
                    return;
                case 0x04: // -(An)
                    A[reg].s32 -= 2;
                    WriteWord(A[reg].s32, value);
                    return;
                case 0x05: // (d16,An)
                    WriteWord(A[reg].s32 + ReadWord(PC), value); PC += 2;
                    return;
                case 0x06: // (d8,An,Xn)
                    WriteWord(A[reg].s32 + GetIndex(), value);
                    return;
                case 0x07:
                    switch (reg)
                    {
                        case 0x00: // (imm).W
                            WriteWord(ReadWord(PC), value); PC += 2;
                            return;
                        case 0x01: // (imm).L
                            WriteWord(ReadLong(PC), value); PC += 4;
                            return;
                        case 0x02: // (d16,PC)
                            WriteWord(PC + ReadWord(PC), value); PC += 2;
                            return;
                        case 0x03: // (d8,PC,Xn)
                            int pc = PC;
                            WriteWord(pc + PeekIndex(), value);
                            PC += 2;
                            return;
                        default: throw new Exception("Invalid addressing mode!");
                    }
            }
        }

        void WriteValueL(int mode, int reg, int value)
        {
            switch (mode)
            {
                case 0x00: // Dn
                    D[reg].s32 = value;
                    return;
                case 0x01: // An
                    A[reg].s32 = value;
                    return;
                case 0x02: // (An)
                    WriteLong(A[reg].s32, value);
                    return;
                case 0x03: // (An)+
                    WriteLong(A[reg].s32, value);
                    A[reg].s32 += 4;
                    return;
                case 0x04: // -(An)
                    A[reg].s32 -= 4;
                    WriteLong(A[reg].s32, value);
                    return;
                case 0x05: // (d16,An)
                    WriteLong(A[reg].s32 + ReadWord(PC), value); PC += 2;
                    return;
                case 0x06: // (d8,An,Xn)
                    WriteLong(A[reg].s32 + GetIndex(), value);
                    return;
                case 0x07:
                    switch (reg)
                    {
                        case 0x00: // (imm).W
                            WriteLong(ReadWord(PC), value); PC += 2;
                            return;
                        case 0x01: // (imm).L
                            WriteLong(ReadLong(PC), value); PC += 4;
                            return;
                        case 0x02: // (d16,PC)
                            WriteLong(PC + ReadWord(PC), value); PC += 2;
                            return;
                        case 0x03: // (d8,PC,Xn)
                            int pc = PC;
                            WriteLong(pc + PeekIndex(), value);
                            PC += 2;
                            return;
                        default: throw new Exception("Invalid addressing mode!");
                    }
            }
        }

        int GetIndex()
        {
            //Console.WriteLine("IN INDEX PORTION - NOT VERIFIED!!!");
            // TODO kid chameleon triggers this in startup sequence

            short extension = ReadWord(PC); PC += 2;

            int da    = (extension >> 15) & 0x1;
            int reg   = (extension >> 12) & 0x7;
            int size  = (extension >> 11) & 0x1;
            int scale = (extension >> 9) & 0x3;
            sbyte displacement = (sbyte)extension;

            int indexReg;
            switch (scale)
            {
                case 0: indexReg = 1; break;
                case 1: indexReg = 2; break;
                case 2: indexReg = 4; break;
                default: indexReg = 8; break;
            }
            if (da == 0)
                indexReg *= size == 0 ? D[reg].s16 : D[reg].s32;
            else
                indexReg *= size == 0 ? A[reg].s16 : A[reg].s32;

            return displacement + indexReg;
        }

        int PeekIndex()
        {
           //Console.WriteLine("IN INDEX PORTION - NOT VERIFIED!!!");

            short extension = ReadWord(PC);

            int da = (extension >> 15) & 0x1;
            int reg = (extension >> 12) & 0x7;
            int size = (extension >> 11) & 0x1;
            int scale = (extension >> 9) & 0x3;
            sbyte displacement = (sbyte)extension;

            int indexReg;
            switch (scale)
            {
                case 0: indexReg = 1; break;
                case 1: indexReg = 2; break;
                case 2: indexReg = 4; break;
                default: indexReg = 8; break;
            }
            if (da == 0)
                indexReg *= size == 0 ? D[reg].s16 : D[reg].s32;
            else
                indexReg *= size == 0 ? A[reg].s16 : A[reg].s32;

            return displacement + indexReg;
        }

        string DisassembleIndex(string baseRegister, short extension)
        {
            int d_a   = (extension >> 15) & 0x1;
            int reg   = (extension >> 12) & 0x7;
            int size  = (extension >> 11) & 0x1;
            int scale = (extension >> 9) & 0x3;
            sbyte displacement = (sbyte)extension;

            string scaleFactor;
            switch (scale)
            {
                case 0:  scaleFactor = ""; break;
                case 1:  scaleFactor = "2"; break;
                case 2:  scaleFactor = "4"; break;
                default: scaleFactor = "8"; break;
            }

            string offsetRegister = (d_a == 0) ? "D" : "A";
            string sizeStr = size == 0 ? ".w" : ".l";
            string displacementStr = displacement == 0 ? "" : ("," + displacement);
            return string.Format("({0},{1}{2}{3}{4}{5})", baseRegister, scaleFactor, offsetRegister, reg, sizeStr, displacementStr);
        }
    }
}