using System;
using System.Text;

namespace BizHawk.Emulation.CPUs.M68000
{
    partial class MC68000
    {
        void MOVE()
        {
            int size    = ((op >> 12) & 0x03);
            int dstMode = ((op >> 6) & 0x07);
            int dstReg  = ((op >> 9) & 0x07);
            int srcMode = ((op >> 3) & 0x07);
            int srcReg  = (op & 0x07);

            int value = 0;
            switch (size)
            {
                case 1: // Byte
                    value = ReadValueB(srcMode, srcReg);
                    WriteValueB(dstMode, dstReg, (sbyte) value);
                    PendingCycles -= MoveCyclesBW[srcMode + (srcMode == 7 ? srcReg : 0), dstMode + (dstMode == 7 ? dstReg : 0)];
                    N = (value & 0x80) != 0;
                    break;
                case 3: // Word
                    value = ReadValueW(srcMode, srcReg);
                    WriteValueW(dstMode, dstReg, (short)value);
                    PendingCycles -= MoveCyclesBW[srcMode + (srcMode == 7 ? srcReg : 0), dstMode + (dstMode == 7 ? dstReg : 0)];
                    N = (value & 0x8000) != 0;
                    break;
                case 2: // Long
                    value = ReadValueL(srcMode, srcReg);
                    WriteValueL(dstMode, dstReg, value);
                    PendingCycles -= MoveCyclesL[srcMode + (srcMode == 7 ? srcReg : 0), dstMode + (dstMode == 7 ? dstReg : 0)];
                    N = (value & 0x80000000) != 0;
                    break;
            }

            V = false;
            C = false;
            Z = (value == 0);
        }

        void MOVE_Disasm(DisassemblyInfo info)
        {
            int pc      = info.PC + 2;
            int size    = ((op >> 12) & 0x03);
            int dstMode = ((op >> 6) & 0x07);
            int dstReg  = ((op >> 9) & 0x07);
            int srcMode = ((op >> 3) & 0x07);
            int srcReg  = (op & 0x07);

            switch (size)
            {
                case 1:
                    info.Mnemonic = "move.b";
                    info.Args = DisassembleValue(srcMode, srcReg, 1, ref pc) +", ";
                    info.Args += DisassembleValue(dstMode, dstReg, 1, ref pc);
                    break;
                case 3:
                    info.Mnemonic = "move.w";
                    info.Args = DisassembleValue(srcMode, srcReg, 2, ref pc) + ", ";
                    info.Args += DisassembleValue(dstMode, dstReg, 2, ref pc);
                    break;
                case 2:
                    info.Mnemonic = "move.l";
                    info.Args = DisassembleValue(srcMode, srcReg, 4, ref pc) + ", ";
                    info.Args += DisassembleValue(dstMode, dstReg, 4, ref pc);
                    break;
            }

            info.Length = pc - info.PC;
        }

        void MOVEA()
        {
            int size    = ((op >> 12) & 0x03);
            int dstReg  = ((op >> 9) & 0x07);
            int srcMode = ((op >> 3) & 0x07);
            int srcReg  = (op & 0x07);

            if (size == 3) // Word
            {
                A[dstReg].s32 = ReadValueW(srcMode, srcReg);
                switch (srcMode)
                {
                    case 0: PendingCycles -= 4; break;
                    case 1: PendingCycles -= 4; break;
                    case 2: PendingCycles -= 8; break;
                    case 3: PendingCycles -= 8; break;
                    case 4: PendingCycles -= 10; break;
                    case 5: PendingCycles -= 12; break;
                    case 6: PendingCycles -= 14; break;
                    case 7:
                        switch (srcReg)
                        {
                            case 0: PendingCycles -= 12; break;
                            case 1: PendingCycles -= 16; break;
                            case 2: PendingCycles -= 12; break;
                            case 3: PendingCycles -= 14; break;
                            case 4: PendingCycles -= 8; break;
                            default: throw new InvalidOperationException();
                        } 
                        break;
                }
            } else { // Long
                A[dstReg].s32 = ReadValueL(srcMode, srcReg);
                switch (srcMode)
                {
                    case 0: PendingCycles -= 4; break;
                    case 1: PendingCycles -= 4; break;
                    case 2: PendingCycles -= 12; break;
                    case 3: PendingCycles -= 12; break;
                    case 4: PendingCycles -= 14; break;
                    case 5: PendingCycles -= 16; break;
                    case 6: PendingCycles -= 18; break;
                    case 7:
                        switch (srcReg)
                        {
                            case 0: PendingCycles -= 16; break;
                            case 1: PendingCycles -= 20; break;
                            case 2: PendingCycles -= 16; break;
                            case 3: PendingCycles -= 18; break;
                            case 4: PendingCycles -= 12; break;
                            default: throw new InvalidOperationException();
                        }
                        break;
                }
            }
        }

        void MOVEA_Disasm(DisassemblyInfo info)
        {
            int pc      = info.PC + 2;
            int size    = ((op >> 12) & 0x03);
            int dstReg  = ((op >> 9) & 0x07);
            int srcMode = ((op >> 3) & 0x07);
            int srcReg  = (op & 0x07);

            if (size == 3)
            {
                info.Mnemonic = "movea.w";
                info.Args = DisassembleValue(srcMode, srcReg, 2, ref pc) + ", A" + dstReg;
            } else {
                info.Mnemonic = "movea.l";
                info.Args = DisassembleValue(srcMode, srcReg, 4, ref pc) + ", A" + dstReg;
            }
            info.Length = pc - info.PC;
        }

        void MOVEQ()
        {
            int value = (sbyte) op; // 8-bit data payload is sign-extended to 32-bits.
            N = (value & 0x80) != 0;
            Z = (value == 0);
            V = false;
            C = false;
            D[(op >> 9) & 7].s32 = value;
            PendingCycles -= 4;
        }

        void MOVEQ_Disasm(DisassemblyInfo info)
        {
            info.Mnemonic = "moveq";
            info.Args = String.Format("{0}, D{1}", (sbyte) op, (op >> 9) & 7);
        }

        void MOVEM0()
        {
            // Move register to memory
            int size    = (op >> 6) & 1;
            int dstMode = (op >> 3) & 7;
            int dstReg  = (op >> 0) & 7;

            ushort registers = (ushort) ReadWord(PC); PC += 2;
            int address = ReadAddress(dstMode, dstReg);
            int regCount = 0;

            if (size == 0) 
            {
                // word-assign
                if (dstMode == 4) // decrement address
                {
                    for (int i = 7; i >= 0; i--)
                    {
                        if ((registers & 1) == 1)
                        {
                            address -= 2;
                            WriteWord(address, A[i].s16);
                            regCount++;
                        }
                        registers >>= 1;
                    }
                    for (int i = 7; i >= 0; i--)
                    {
                        if ((registers & 1) == 1)
                        {
                            address -= 2;
                            WriteWord(address, D[i].s16);
                            regCount++;
                        }
                        registers >>= 1;
                    }
                    A[dstReg].s32 = address;
                }
                else
                { // increment address
                    for (int i = 7; i >= 0; i--)
                    {
                        if ((registers & 1) == 1)
                        {
                            WriteWord(address, A[i].s16);
                            address += 2;
                            regCount++;
                        }
                        registers >>= 1;
                    }
                    for (int i = 7; i >= 0; i--)
                    {
                        if ((registers & 1) == 1)
                        {
                            WriteWord(address, D[i].s16);
                            address += 2;
                            regCount++;
                        }
                        registers >>= 1;
                    }
                }
                PendingCycles -= regCount*4;
            } else { 
                // long-assign
                if (dstMode == 4) // decrement address
                {
                    for (int i=7; i>= 0; i--)
                    {
                        if ((registers & 1) == 1)
                        {
                            address -= 4;
                            WriteLong(address, A[i].s32);
                            regCount++;
                        }
                        registers >>= 1;
                    }
                    for (int i = 7; i >= 0; i--)
                    {
                        if ((registers & 1) == 1)
                        {
                            address -= 4;
                            WriteLong(address, D[i].s32);
                            regCount++;
                        }
                        registers >>= 1;
                    }
                    A[dstReg].s32 = address;
                } else { // increment address
                    for (int i = 7; i >= 0; i--)
                    {
                        if ((registers & 1) == 1)
                        {
                            WriteLong(address, A[i].s32);
                            address += 4;
                            regCount++;
                        }
                        registers >>= 1;
                    }
                    for (int i = 7; i >= 0; i--)
                    {
                        if ((registers & 1) == 1)
                        {
                            WriteLong(address, D[i].s32);
                            address += 4;
                            regCount++;
                        }
                        registers >>= 1;
                    }
                }
                PendingCycles -= regCount * 8;
            }

            switch (dstMode)
            {
                case 2: PendingCycles -= 8; break;
                case 3: PendingCycles -= 8; break;
                case 4: PendingCycles -= 8; break;
                case 5: PendingCycles -= 12; break;
                case 6: PendingCycles -= 14; break;
                case 7:
                    switch (dstReg)
                    {
                        case 0: PendingCycles -= 12; break;
                        case 1: PendingCycles -= 16; break;
                    }
                    break;
            }
        }

        void MOVEM1()
        {
            // Move memory to register
            int size    = (op >> 6) & 1;
            int srcMode = (op >> 3) & 7;
            int srcReg  = (op >> 0) & 7;

            ushort registers = (ushort)ReadWord(PC); PC += 2;
            int address = ReadAddress(srcMode, srcReg);
            int regCount = 0;

            if (size == 0)
            {
                // word-assign
                for (int i = 0; i < 8; i++)
                {
                    if ((registers & 1) == 1)
                    {
                        D[i].s32 = ReadWord(address);
                        address += 2;
                        regCount++;
                    }
                    registers >>= 1;
                }
                for (int i = 0; i < 8; i++)
                {
                    if ((registers & 1) == 1)
                    {
                        A[i].s32 = ReadWord(address);
                        address += 2;
                        regCount++;
                    }
                    registers >>= 1;
                }
                PendingCycles -= regCount * 4;
                if (srcMode == 3)
                    A[srcReg].s32 = address;
            } else {
                // long-assign
                for (int i = 0; i < 8; i++)
                {
                    if ((registers & 1) == 1)
                    {
                        D[i].s32 = ReadLong(address);
                        address += 4;
                        regCount++;
                    }
                    registers >>= 1;
                }
                for (int i = 0; i < 8; i++)
                {
                    if ((registers & 1) == 1)
                    {
                        A[i].s32 = ReadLong(address);
                        address += 4;
                        regCount++;
                    }
                    registers >>= 1;
                }
                PendingCycles -= regCount * 8;
                if (srcMode == 3)
                    A[srcReg].s32 = address;
            }

            switch (srcMode)
            {
                case 2: PendingCycles -= 12; break;
                case 3: PendingCycles -= 12; break;
                case 4: PendingCycles -= 12; break;
                case 5: PendingCycles -= 16; break;
                case 6: PendingCycles -= 18; break;
                case 7:
                    switch (srcReg)
                    {
                        case 0: PendingCycles -= 16; break;
                        case 1: PendingCycles -= 20; break;
                        case 2: PendingCycles -= 16; break;
                        case 3: PendingCycles -= 18; break;
                    }
                    break;
            }
        }

        static string DisassembleRegisterList0(ushort registers)
        {
            var str = new StringBuilder();
            int count = 0;
            for (int i = 0; i<8; i++)
            {
                if ((registers & 0x8000) != 0)
                {
                    if (count > 0) str.Append(",");
                    str.Append("D"+i);
                    count++;
                }
                registers <<= 1;
            }
            for (int i = 0; i < 8; i++)
            {
                if ((registers & 0x8000) != 0)
                {
                    if (count > 0) str.Append(",");
                    str.Append("A"+i);
                    count++;
                }
                registers <<= 1;
            }
            return str.ToString();
        }

        static string DisassembleRegisterList1(ushort registers)
        {
            var str = new StringBuilder();
            int count = 0;
            for (int i = 0; i < 8; i++)
            {
                if ((registers & 1) != 0)
                {
                    if (count > 0) str.Append(",");
                    str.Append("D" + i);
                    count++;
                }
                registers >>= 1;
            }
            for (int i = 0; i < 8; i++)
            {
                if ((registers & 1) != 0)
                {
                    if (count > 0) str.Append(",");
                    str.Append("A" + i);
                    count++;
                }
                registers >>= 1;
            }
            return str.ToString();
        }

        void MOVEM0_Disasm(DisassemblyInfo info)
        {
            int pc   = info.PC + 2;
            int size = (op >> 6) & 1;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;
            
            ushort registers = (ushort)ReadWord(pc); pc += 2;
            string address = DisassembleAddress(mode, reg, ref pc);

            info.Mnemonic = size == 0 ? "movem.w" : "movem.l";
            info.Args = DisassembleRegisterList0(registers) + ", " + address;
            info.Length = pc - info.PC;
        }

        void MOVEM1_Disasm(DisassemblyInfo info)
        {
            int pc   = info.PC + 2;
            int size = (op >> 6) & 1;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            ushort registers = (ushort)ReadWord(pc); pc += 2;
            string address = DisassembleAddress(mode, reg, ref pc);
            
            info.Mnemonic = size == 0 ? "movem.w" : "movem.l";
            info.Args = address + ", " + DisassembleRegisterList1(registers);
            info.Length = pc - info.PC;
        }

        void LEA()
        {
            int mode = (op >> 3) & 7;
            int sReg = (op >> 0) & 7;
            int dReg = (op >> 9) & 7;

            A[dReg].u32 = (uint)ReadAddress(mode, sReg);
            switch (mode)
            {
                case 2: PendingCycles -= 4; break;
                case 5: PendingCycles -= 8; break;
                case 6: PendingCycles -= 12; break;
                case 7: 
                    switch (sReg)
                    {
                        case 0: PendingCycles -= 8; break;
                        case 1: PendingCycles -= 12; break;
                        case 2: PendingCycles -= 8; break;
                        case 3: PendingCycles -= 12; break;
                    }
                    break;
            }
        }

        void LEA_Disasm(DisassemblyInfo info)
        {
            int pc   = info.PC + 2;
            int mode = (op >> 3) & 7;
            int sReg = (op >> 0) & 7;
            int dReg = (op >> 9) & 7;
            
            info.Mnemonic = "lea";
            info.Args = DisassembleAddress(mode, sReg, ref pc);
            info.Args += ", A"+dReg;

            info.Length = pc - info.PC;
        }

        void CLR()
        {
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            switch (size)
            {
                case 0: WriteValueB(mode, reg, 0); PendingCycles -= mode == 0 ? 4 : 8 + EACyclesBW[mode, reg]; break;
                case 1: WriteValueW(mode, reg, 0); PendingCycles -= mode == 0 ? 4 : 8 + EACyclesBW[mode, reg]; break;
                case 2: WriteValueL(mode, reg, 0); PendingCycles -= mode == 0 ? 6 : 12 + EACyclesL[mode, reg]; break;
            }

            N = V = C = false;
            Z = true;
        }

        void CLR_Disasm(DisassemblyInfo info)
        {
            int pc   = info.PC + 2;
            int size = (op >> 6) & 3;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;
            
            switch (size)
            {
                case 0: info.Mnemonic = "clr.b"; info.Args = DisassembleValue(mode, reg, 1, ref pc); break;
                case 1: info.Mnemonic = "clr.w"; info.Args = DisassembleValue(mode, reg, 2, ref pc); break;
                case 2: info.Mnemonic = "clr.l"; info.Args = DisassembleValue(mode, reg, 4, ref pc); break;
            }
            info.Length = pc - info.PC;
        }

        void EXT()
        {
            int size = (op >> 6) & 1;
            int reg  = op & 7;

            switch (size)
            {
                case 0: // ext.w
                    D[reg].s16 = D[reg].s8;
                    N = (D[reg].s16 & 0x8000) != 0;
                    Z = (D[reg].s16 == 0);
                    break;
                case 1: // ext.l
                    D[reg].s32 = D[reg].s16;
                    N = (D[reg].s32 & 0x80000000) != 0;
                    Z = (D[reg].s32 == 0);
                    break;
            }

            V = false;
            C = false;
            PendingCycles -= 4;
        }

        void EXT_Disasm(DisassemblyInfo info)
        {
            int size = (op >> 6) & 1;
            int reg  = op & 7;

            switch (size)
            {
                case 0: info.Mnemonic = "ext.w"; info.Args = "D" + reg; break;
                case 1: info.Mnemonic = "ext.l"; info.Args = "D" + reg; break;
            }
        }

        void PEA()
        {
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;
            int ea   = ReadAddress(mode, reg);

            A[7].s32 -= 4;
            WriteLong(A[7].s32, ea);

            switch (mode)
            {
                case 2: PendingCycles -= 12; break;
                case 5: PendingCycles -= 16; break;
                case 6: PendingCycles -= 20; break;
                case 7:
                    switch (reg)
                    {
                        case 0: PendingCycles -= 16; break;
                        case 1: PendingCycles -= 20; break;
                        case 2: PendingCycles -= 16; break;
                        case 3: PendingCycles -= 20; break;
                    }
                    break;
            }
        }

        void PEA_Disasm(DisassemblyInfo info)
        {
            int pc   = info.PC + 2;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            info.Mnemonic = "pea";
            info.Args = DisassembleAddress(mode, reg, ref pc);
            info.Length = pc - info.PC;
        }
    }
}
