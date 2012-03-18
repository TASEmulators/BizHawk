using System;

// Do not modify this file directly! This is GENERATED code.
// Please open the CpuCoreGenerator solution and make your modifications there.

namespace BizHawk.Emulation.CPUs.M6502
{
    public partial class MOS6502
    {
        public void Execute(int cycles)
        {
            sbyte rel8;
            byte value8, temp8;
            ushort value16, temp16;
            int temp;

            PendingCycles += cycles;
            while (PendingCycles > 0)
            {
                if (NMI)
                {
                    TriggerException(ExceptionType.NMI);
                    NMI = false;
                }
                if (IRQ && !FlagI)
                {
                    if (SEI_Pending)
                        FlagI = true;
                    TriggerException(ExceptionType.IRQ);
                }
                if (CLI_Pending)
                {
                    FlagI = false;
                    CLI_Pending = false;
                }
                if (SEI_Pending)
                {
                    FlagI = true;
                    SEI_Pending = false;
                }
                if(debug) Console.WriteLine(State());

                ushort this_pc = PC;
                byte opcode = ReadMemory(PC++);
                switch (opcode)
                {
                    case 0x00: // BRK
                        TriggerException(ExceptionType.BRK);
                        break;
                    case 0x01: // ORA (addr,X)
                        value8 = ReadMemory(ReadWordPageWrap((byte)(ReadMemory(PC++)+X)));
                        A |= value8;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 6; TotalExecutedCycles += 6;
                        break;
                    case 0x04: // NOP zp
                        PC += 1;
                        PendingCycles -= 3; TotalExecutedCycles += 3;
                        break;
                    case 0x05: // ORA zp
                        value8 = ReadMemory(ReadMemory(PC++));
                        A |= value8;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 3; TotalExecutedCycles += 3;
                        break;
                    case 0x06: // ASL zp
                        value16 = ReadMemory(PC++);
                        value8 = ReadMemory(value16);
                        FlagC = (value8 & 0x80) != 0;
                        value8 = (byte)(value8 << 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 5; TotalExecutedCycles += 5;
                        break;
                    case 0x08: // PHP
                        FlagB = true; //why would it do this?? how weird
                        WriteMemory((ushort)(S-- + 0x100), P);
                        PendingCycles -= 3; TotalExecutedCycles += 3;
                        break;
                    case 0x09: // ORA #nn
                        value8 = ReadMemory(PC++);
                        A |= value8;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x0A: // ASL A
                        FlagC = (A & 0x80) != 0;
                        A = (byte) (A << 1);
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x0C: // NOP (addr)
                        PC += 2;
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x0D: // ORA addr
                        value8 = ReadMemory(ReadWord(PC)); PC += 2;
                        A |= value8;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x0E: // ASL addr
                        value16 = ReadWord(PC); PC += 2;
                        value8 = ReadMemory(value16);
                        FlagC = (value8 & 0x80) != 0;
                        value8 = (byte)(value8 << 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 6; TotalExecutedCycles += 6;
                        break;
                    case 0x10: // BPL +/-rel
                        rel8 = (sbyte)ReadMemory(PC++);
                        value16 = (ushort)(PC+rel8);
                        if (FlagN == false) {
                            PendingCycles--; TotalExecutedCycles++;
                            if ((PC & 0xFF00) != (value16 & 0xFF00)) 
                                { PendingCycles--; TotalExecutedCycles++; }
                            PC = value16;
                        }
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x11: // ORA (addr),Y*
                        temp16 = ReadWordPageWrap(ReadMemory(PC++));
                        value8 = ReadMemory((ushort)(temp16+Y));
                        if ((temp16 & 0xFF00) != ((temp16+Y) & 0xFF00)) 
                            { PendingCycles--; TotalExecutedCycles++; }
                        A |= value8;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 5; TotalExecutedCycles += 5;
                        break;
                    case 0x14: // NOP zp,X
                        PC += 1;
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x15: // ORA zp,X
                        value8 = ReadMemory((byte)(ReadMemory(PC++)+X));
                        A |= value8;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x16: // ASL zp,X
                        value16 = (byte)(ReadMemory(PC++)+X);
                        value8 = ReadMemory(value16);
                        FlagC = (value8 & 0x80) != 0;
                        value8 = (byte)(value8 << 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 6; TotalExecutedCycles += 6;
                        break;
                    case 0x18: // CLC
                        FlagC = false;
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x19: // ORA addr,Y*
                        temp16 = ReadWord(PC);
                        value8 = ReadMemory((ushort)(temp16+Y));
                        if ((temp16 & 0xFF00) != ((temp16 + Y) & 0xFF00)) 
                            { PendingCycles--; TotalExecutedCycles++; }
                        PC += 2;
                        A |= value8;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x1A: // NOP
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x1C: // NOP (addr,X)
                        PC += 1;
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x1D: // ORA addr,X*
                        temp16 = ReadWord(PC);
                        value8 = ReadMemory((ushort)(temp16+X));
                        if ((temp16 & 0xFF00) != ((temp16 + X) & 0xFF00)) 
                            { PendingCycles--; TotalExecutedCycles++; }
                        PC += 2;
                        A |= value8;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x1E: // ASL addr,X
                        value16 = (ushort)(ReadWord(PC)+X);
                        PC += 2;
                        value8 = ReadMemory(value16);
                        FlagC = (value8 & 0x80) != 0;
                        value8 = (byte)(value8 << 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 7; TotalExecutedCycles += 7;
                        break;
                    case 0x20: // JSR addr
                        temp16 = (ushort)(PC+1);
                        WriteMemory((ushort)(S-- + 0x100), (byte)(temp16 >> 8));
                        WriteMemory((ushort)(S-- + 0x100), (byte)temp16);
                        PC = ReadWord(PC);
                        PendingCycles -= 6; TotalExecutedCycles += 6;
                        break;
                    case 0x21: // AND (addr,X)
                        value8 = ReadMemory(ReadWordPageWrap((byte)(ReadMemory(PC++)+X)));
                        A &= value8;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 6; TotalExecutedCycles += 6;
                        break;
                    case 0x24: // BIT zp
                        value8 = ReadMemory(ReadMemory(PC++));
                        FlagN = (value8 & 0x80) != 0;
                        FlagV = (value8 & 0x40) != 0;
                        FlagZ = (A & value8) == 0;
                        PendingCycles -= 3; TotalExecutedCycles += 3;
                        break;
                    case 0x25: // AND zp
                        value8 = ReadMemory(ReadMemory(PC++));
                        A &= value8;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 3; TotalExecutedCycles += 3;
                        break;
                    case 0x26: // ROL zp
                        value16 = ReadMemory(PC++);
                        value8 = temp8 = ReadMemory(value16);
                        value8 = (byte)((value8 << 1) | (P & 1));
                        WriteMemory(value16, value8);
                        FlagC = (temp8 & 0x80) != 0;
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 5; TotalExecutedCycles += 5;
                        break;
                    case 0x28: // PLP
                        //handle I flag differently. sort of a sloppy way to do the job, but it does finish it off.
                        value8 = ReadMemory((ushort)(++S + 0x100));
                        if ((value8 & 0x04) != 0 && !FlagI)
                        	SEI_Pending = true;
                        if ((value8 & 0x04) == 0 && FlagI)
                        	CLI_Pending = true;
                        value8 &= unchecked((byte)~0x04);
                        P &= 0x04;
                        P |= value8;
FlagT = true;//this seems wrong
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x29: // AND #nn
                        value8 = ReadMemory(PC++);
                        A &= value8;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x2A: // ROL A
                        temp8 = A;
                        A = (byte)((A << 1) | (P & 1));
                        FlagC = (temp8 & 0x80) != 0;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x2C: // BIT addr
                        value8 = ReadMemory(ReadWord(PC)); PC += 2;
                        FlagN = (value8 & 0x80) != 0;
                        FlagV = (value8 & 0x40) != 0;
                        FlagZ = (A & value8) == 0;
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x2D: // AND addr
                        value8 = ReadMemory(ReadWord(PC)); PC += 2;
                        A &= value8;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x2E: // ROL addr
                        value16 = ReadWord(PC); PC += 2;
                        value8 = temp8 = ReadMemory(value16);
                        value8 = (byte)((value8 << 1) | (P & 1));
                        WriteMemory(value16, value8);
                        FlagC = (temp8 & 0x80) != 0;
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 6; TotalExecutedCycles += 6;
                        break;
                    case 0x30: // BMI +/-rel
                        rel8 = (sbyte)ReadMemory(PC++);
                        value16 = (ushort)(PC+rel8);
                        if (FlagN == true) {
                            PendingCycles--; TotalExecutedCycles++;
                            if ((PC & 0xFF00) != (value16 & 0xFF00)) 
                                { PendingCycles--; TotalExecutedCycles++; }
                            PC = value16;
                        }
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x31: // AND (addr),Y*
                        temp16 = ReadWordPageWrap(ReadMemory(PC++));
                        value8 = ReadMemory((ushort)(temp16+Y));
                        if ((temp16 & 0xFF00) != ((temp16+Y) & 0xFF00)) 
                            { PendingCycles--; TotalExecutedCycles++; }
                        A &= value8;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 5; TotalExecutedCycles += 5;
                        break;
                    case 0x34: // NOP zp,X
                        PC += 1;
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x35: // AND zp,X
                        value8 = ReadMemory((byte)(ReadMemory(PC++)+X));
                        A &= value8;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x36: // ROL zp,X
                        value16 = (byte)(ReadMemory(PC++)+X);
                        value8 = temp8 = ReadMemory(value16);
                        value8 = (byte)((value8 << 1) | (P & 1));
                        WriteMemory(value16, value8);
                        FlagC = (temp8 & 0x80) != 0;
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 6; TotalExecutedCycles += 6;
                        break;
                    case 0x38: // SEC
                        FlagC = true;
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x39: // AND addr,Y*
                        temp16 = ReadWord(PC);
                        value8 = ReadMemory((ushort)(temp16+Y));
                        if ((temp16 & 0xFF00) != ((temp16 + Y) & 0xFF00)) 
                            { PendingCycles--; TotalExecutedCycles++; }
                        PC += 2;
                        A &= value8;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x3A: // NOP
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x3C: // NOP (addr,X)
                        PC += 1;
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x3D: // AND addr,X*
                        temp16 = ReadWord(PC);
                        value8 = ReadMemory((ushort)(temp16+X));
                        if ((temp16 & 0xFF00) != ((temp16 + X) & 0xFF00)) 
                            { PendingCycles--; TotalExecutedCycles++; }
                        PC += 2;
                        A &= value8;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x3E: // ROL addr,X
                        value16 = (ushort)(ReadWord(PC)+X);
                        PC += 2;
                        value8 = temp8 = ReadMemory(value16);
                        value8 = (byte)((value8 << 1) | (P & 1));
                        WriteMemory(value16, value8);
                        FlagC = (temp8 & 0x80) != 0;
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 7; TotalExecutedCycles += 7;
                        break;
                    case 0x40: // RTI
                        P = ReadMemory((ushort)(++S + 0x100));
FlagT = true;// this seems wrong
                        PC = ReadMemory((ushort)(++S + 0x100));
                        PC |= (ushort)(ReadMemory((ushort)(++S + 0x100)) << 8);
                        PendingCycles -= 6; TotalExecutedCycles += 6;
                        break;
                    case 0x41: // EOR (addr,X)
                        value8 = ReadMemory(ReadWordPageWrap((byte)(ReadMemory(PC++)+X)));
                        A ^= value8;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 6; TotalExecutedCycles += 6;
                        break;
                    case 0x44: // NOP zp
                        PC += 1;
                        PendingCycles -= 3; TotalExecutedCycles += 3;
                        break;
                    case 0x45: // EOR zp
                        value8 = ReadMemory(ReadMemory(PC++));
                        A ^= value8;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 3; TotalExecutedCycles += 3;
                        break;
                    case 0x46: // LSR zp
                        value16 = ReadMemory(PC++);
                        value8 = ReadMemory(value16);
                        FlagC = (value8 & 1) != 0;
                        value8 = (byte)(value8 >> 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 5; TotalExecutedCycles += 5;
                        break;
                    case 0x48: // PHA
                        WriteMemory((ushort)(S-- + 0x100), A);
                        PendingCycles -= 3; TotalExecutedCycles += 3;
                        break;
                    case 0x49: // EOR #nn
                        value8 = ReadMemory(PC++);
                        A ^= value8;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x4A: // LSR A
                        FlagC = (A & 1) != 0;
                        A = (byte) (A >> 1);
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x4C: // JMP addr
                        PC = ReadWord(PC);
                        PendingCycles -= 3; TotalExecutedCycles += 3;
                        break;
                    case 0x4D: // EOR addr
                        value8 = ReadMemory(ReadWord(PC)); PC += 2;
                        A ^= value8;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x4E: // LSR addr
                        value16 = ReadWord(PC); PC += 2;
                        value8 = ReadMemory(value16);
                        FlagC = (value8 & 1) != 0;
                        value8 = (byte)(value8 >> 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 6; TotalExecutedCycles += 6;
                        break;
                    case 0x50: // BVC +/-rel
                        rel8 = (sbyte)ReadMemory(PC++);
                        value16 = (ushort)(PC+rel8);
                        if (FlagV == false) {
                            PendingCycles--; TotalExecutedCycles++;
                            if ((PC & 0xFF00) != (value16 & 0xFF00)) 
                                { PendingCycles--; TotalExecutedCycles++; }
                            PC = value16;
                        }
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x51: // EOR (addr),Y*
                        temp16 = ReadWordPageWrap(ReadMemory(PC++));
                        value8 = ReadMemory((ushort)(temp16+Y));
                        if ((temp16 & 0xFF00) != ((temp16+Y) & 0xFF00)) 
                            { PendingCycles--; TotalExecutedCycles++; }
                        A ^= value8;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 5; TotalExecutedCycles += 5;
                        break;
                    case 0x54: // NOP zp,X
                        PC += 1;
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x55: // EOR zp,X
                        value8 = ReadMemory((byte)(ReadMemory(PC++)+X));
                        A ^= value8;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x56: // LSR zp,X
                        value16 = (byte)(ReadMemory(PC++)+X);
                        value8 = ReadMemory(value16);
                        FlagC = (value8 & 1) != 0;
                        value8 = (byte)(value8 >> 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 6; TotalExecutedCycles += 6;
                        break;
                    case 0x58: // CLI
                        //FlagI = false;
                        CLI_Pending = true;
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x59: // EOR addr,Y*
                        temp16 = ReadWord(PC);
                        value8 = ReadMemory((ushort)(temp16+Y));
                        if ((temp16 & 0xFF00) != ((temp16 + Y) & 0xFF00)) 
                            { PendingCycles--; TotalExecutedCycles++; }
                        PC += 2;
                        A ^= value8;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x5A: // NOP
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x5C: // NOP (addr,X)
                        PC += 1;
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x5D: // EOR addr,X*
                        temp16 = ReadWord(PC);
                        value8 = ReadMemory((ushort)(temp16+X));
                        if ((temp16 & 0xFF00) != ((temp16 + X) & 0xFF00)) 
                            { PendingCycles--; TotalExecutedCycles++; }
                        PC += 2;
                        A ^= value8;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x5E: // LSR addr,X
                        value16 = (ushort)(ReadWord(PC)+X);
                        PC += 2;
                        value8 = ReadMemory(value16);
                        FlagC = (value8 & 1) != 0;
                        value8 = (byte)(value8 >> 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 7; TotalExecutedCycles += 7;
                        break;
                    case 0x60: // RTS
                        PC = ReadMemory((ushort)(++S + 0x100));
                        PC |= (ushort)(ReadMemory((ushort)(++S + 0x100)) << 8);
                        PC++;
                        PendingCycles -= 6; TotalExecutedCycles += 6;
                        break;
                    case 0x61: // ADC (addr,X)
                        value8 = ReadMemory(ReadWordPageWrap((byte)(ReadMemory(PC++)+X)));
                        temp = value8 + A + (FlagC ? 1 : 0);
                        FlagV = (~(A ^ value8) & (A ^ temp) & 0x80) != 0;
                        FlagC = temp > 0xFF;
                        A = (byte)temp;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 6; TotalExecutedCycles += 6;
                        break;
                    case 0x64: // NOP zp
                        PC += 1;
                        PendingCycles -= 3; TotalExecutedCycles += 3;
                        break;
                    case 0x65: // ADC zp
                        value8 = ReadMemory(ReadMemory(PC++));
                        temp = value8 + A + (FlagC ? 1 : 0);
                        FlagV = (~(A ^ value8) & (A ^ temp) & 0x80) != 0;
                        FlagC = temp > 0xFF;
                        A = (byte)temp;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 3; TotalExecutedCycles += 3;
                        break;
                    case 0x66: // ROR zp
                        value16 = ReadMemory(PC++);
                        value8 = temp8 = ReadMemory(value16);
                        value8 = (byte)((value8 >> 1) | ((P & 1)<<7));
                        WriteMemory(value16, value8);
                        FlagC = (temp8 & 1) != 0;
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 5; TotalExecutedCycles += 5;
                        break;
                    case 0x68: // PLA
                        A = ReadMemory((ushort)(++S + 0x100));
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x69: // ADC #nn
                        value8 = ReadMemory(PC++);
                        temp = value8 + A + (FlagC ? 1 : 0);
                        FlagV = (~(A ^ value8) & (A ^ temp) & 0x80) != 0;
                        FlagC = temp > 0xFF;
                        A = (byte)temp;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x6A: // ROR A
                        temp8 = A;
                        A = (byte)((A >> 1) | ((P & 1)<<7));
                        FlagC = (temp8 & 1) != 0;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x6C: // JMP (addr)
                        PC = ReadWordPageWrap(ReadWord(PC));
                        PendingCycles -= 5; TotalExecutedCycles += 5;
                        break;
                    case 0x6D: // ADC addr
                        value8 = ReadMemory(ReadWord(PC)); PC += 2;
                        temp = value8 + A + (FlagC ? 1 : 0);
                        FlagV = (~(A ^ value8) & (A ^ temp) & 0x80) != 0;
                        FlagC = temp > 0xFF;
                        A = (byte)temp;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x6E: // ROR addr
                        value16 = ReadWord(PC); PC += 2;
                        value8 = temp8 = ReadMemory(value16);
                        value8 = (byte)((value8 >> 1) | ((P & 1)<<7));
                        WriteMemory(value16, value8);
                        FlagC = (temp8 & 1) != 0;
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 6; TotalExecutedCycles += 6;
                        break;
                    case 0x70: // BVS +/-rel
                        rel8 = (sbyte)ReadMemory(PC++);
                        value16 = (ushort)(PC+rel8);
                        if (FlagV == true) {
                            PendingCycles--; TotalExecutedCycles++;
                            if ((PC & 0xFF00) != (value16 & 0xFF00)) 
                                { PendingCycles--; TotalExecutedCycles++; }
                            PC = value16;
                        }
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x71: // ADC (addr),Y*
                        temp16 = ReadWordPageWrap(ReadMemory(PC++));
                        value8 = ReadMemory((ushort)(temp16+Y));
                        if ((temp16 & 0xFF00) != ((temp16+Y) & 0xFF00)) 
                            { PendingCycles--; TotalExecutedCycles++; }
                        temp = value8 + A + (FlagC ? 1 : 0);
                        FlagV = (~(A ^ value8) & (A ^ temp) & 0x80) != 0;
                        FlagC = temp > 0xFF;
                        A = (byte)temp;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 5; TotalExecutedCycles += 5;
                        break;
                    case 0x74: // NOP zp,X
                        PC += 1;
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x75: // ADC zp,X
                        value8 = ReadMemory((byte)(ReadMemory(PC++)+X));
                        temp = value8 + A + (FlagC ? 1 : 0);
                        FlagV = (~(A ^ value8) & (A ^ temp) & 0x80) != 0;
                        FlagC = temp > 0xFF;
                        A = (byte)temp;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x76: // ROR zp,X
                        value16 = (byte)(ReadMemory(PC++)+X);
                        value8 = temp8 = ReadMemory(value16);
                        value8 = (byte)((value8 >> 1) | ((P & 1)<<7));
                        WriteMemory(value16, value8);
                        FlagC = (temp8 & 1) != 0;
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 6; TotalExecutedCycles += 6;
                        break;
                    case 0x78: // SEI
                        //FlagI = true;
                        SEI_Pending = true;
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x79: // ADC addr,Y*
                        temp16 = ReadWord(PC);
                        value8 = ReadMemory((ushort)(temp16+Y));
                        if ((temp16 & 0xFF00) != ((temp16 + Y) & 0xFF00)) 
                            { PendingCycles--; TotalExecutedCycles++; }
                        PC += 2;
                        temp = value8 + A + (FlagC ? 1 : 0);
                        FlagV = (~(A ^ value8) & (A ^ temp) & 0x80) != 0;
                        FlagC = temp > 0xFF;
                        A = (byte)temp;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x7A: // NOP
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x7C: // NOP (addr,X)
                        PC += 1;
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x7D: // ADC addr,X*
                        temp16 = ReadWord(PC);
                        value8 = ReadMemory((ushort)(temp16+X));
                        if ((temp16 & 0xFF00) != ((temp16 + X) & 0xFF00)) 
                            { PendingCycles--; TotalExecutedCycles++; }
                        PC += 2;
                        temp = value8 + A + (FlagC ? 1 : 0);
                        FlagV = (~(A ^ value8) & (A ^ temp) & 0x80) != 0;
                        FlagC = temp > 0xFF;
                        A = (byte)temp;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x7E: // ROR addr,X
                        value16 = (ushort)(ReadWord(PC)+X);
                        PC += 2;
                        value8 = temp8 = ReadMemory(value16);
                        value8 = (byte)((value8 >> 1) | ((P & 1)<<7));
                        WriteMemory(value16, value8);
                        FlagC = (temp8 & 1) != 0;
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 7; TotalExecutedCycles += 7;
                        break;
                    case 0x80: // NOP #nn
                        PC += 1;
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x81: // STA (addr,X)
                        temp8 = (byte)(ReadMemory(PC++) + X);
                        value16 = ReadWordPageWrap(temp8);
                        WriteMemory(value16, A);
                        PendingCycles -= 6; TotalExecutedCycles += 6;
                        break;
                    case 0x82: // NOP #nn
                        PC += 1;
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x84: // STY zp
                        value16 = ReadMemory(PC++);
                        WriteMemory(value16, Y);
                        PendingCycles -= 3; TotalExecutedCycles += 3;
                        break;
                    case 0x85: // STA zp
                        value16 = ReadMemory(PC++);
                        WriteMemory(value16, A);
                        PendingCycles -= 3; TotalExecutedCycles += 3;
                        break;
                    case 0x86: // STX zp
                        value16 = ReadMemory(PC++);
                        WriteMemory(value16, X);
                        PendingCycles -= 3; TotalExecutedCycles += 3;
                        break;
                    case 0x88: // DEY
                        P = (byte)((P & 0x7D) | TableNZ[--Y]);
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x89: // NOP #nn
                        PC += 1;
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x8A: // TXA
                        A = X;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x8C: // STY addr
                        value16 = ReadWord(PC); PC += 2;
                        WriteMemory(value16, Y);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x8D: // STA addr
                        value16 = ReadWord(PC); PC += 2;
                        WriteMemory(value16, A);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x8E: // STX addr
                        value16 = ReadWord(PC); PC += 2;
                        WriteMemory(value16, X);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x90: // BCC +/-rel
                        rel8 = (sbyte)ReadMemory(PC++);
                        value16 = (ushort)(PC+rel8);
                        if (FlagC == false) {
                            PendingCycles--; TotalExecutedCycles++;
                            if ((PC & 0xFF00) != (value16 & 0xFF00)) 
                                { PendingCycles--; TotalExecutedCycles++; }
                            PC = value16;
                        }
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x91: // STA (addr),Y
                        temp16 = ReadWordPageWrap(ReadMemory(PC++));
                        value16 = (ushort)(temp16+Y);
                        WriteMemory(value16, A);
                        PendingCycles -= 6; TotalExecutedCycles += 6;
                        break;
                    case 0x94: // STY zp,X
                        value16 = (byte)(ReadMemory(PC++)+X);
                        WriteMemory(value16, Y);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x95: // STA zp,X
                        value16 = (byte)(ReadMemory(PC++)+X);
                        WriteMemory(value16, A);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x96: // STX zp,Y
                        value16 = (byte)(ReadMemory(PC++)+Y);
                        WriteMemory(value16, X);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0x98: // TYA
                        A = Y;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x99: // STA addr,Y
                        value16 = (ushort)(ReadWord(PC)+Y);
                        PC += 2;
                        WriteMemory(value16, A);
                        PendingCycles -= 5; TotalExecutedCycles += 5;
                        break;
                    case 0x9A: // TXS
                        S = X;
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0x9D: // STA addr,X
                        value16 = (ushort)(ReadWord(PC)+X);
                        PC += 2;
                        WriteMemory(value16, A);
                        PendingCycles -= 5; TotalExecutedCycles += 5;
                        break;
                    case 0xA0: // LDY #nn
                        Y = ReadMemory(PC++);
                        P = (byte)((P & 0x7D) | TableNZ[Y]);
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0xA1: // LDA (addr,X)
                        A = ReadMemory(ReadWordPageWrap((byte)(ReadMemory(PC++)+X)));
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 6; TotalExecutedCycles += 6;
                        break;
                    case 0xA2: // LDX #nn
                        X = ReadMemory(PC++);
                        P = (byte)((P & 0x7D) | TableNZ[X]);
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0xA4: // LDY zp
                        Y = ReadMemory(ReadMemory(PC++));
                        P = (byte)((P & 0x7D) | TableNZ[Y]);
                        PendingCycles -= 3; TotalExecutedCycles += 3;
                        break;
                    case 0xA5: // LDA zp
                        A = ReadMemory(ReadMemory(PC++));
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 3; TotalExecutedCycles += 3;
                        break;
                    case 0xA6: // LDX zp
                        X = ReadMemory(ReadMemory(PC++));
                        P = (byte)((P & 0x7D) | TableNZ[X]);
                        PendingCycles -= 3; TotalExecutedCycles += 3;
                        break;
                    case 0xA8: // TAY
                        Y = A;
                        P = (byte)((P & 0x7D) | TableNZ[Y]);
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0xA9: // LDA #nn
                        A = ReadMemory(PC++);
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0xAA: // TAX
                        X = A;
                        P = (byte)((P & 0x7D) | TableNZ[X]);
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0xAC: // LDY addr
                        Y = ReadMemory(ReadWord(PC)); PC += 2;
                        P = (byte)((P & 0x7D) | TableNZ[Y]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0xAD: // LDA addr
                        A = ReadMemory(ReadWord(PC)); PC += 2;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0xAE: // LDX addr
                        X = ReadMemory(ReadWord(PC)); PC += 2;
                        P = (byte)((P & 0x7D) | TableNZ[X]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0xB0: // BCS +/-rel
                        rel8 = (sbyte)ReadMemory(PC++);
                        value16 = (ushort)(PC+rel8);
                        if (FlagC == true) {
                            PendingCycles--; TotalExecutedCycles++;
                            if ((PC & 0xFF00) != (value16 & 0xFF00)) 
                                { PendingCycles--; TotalExecutedCycles++; }
                            PC = value16;
                        }
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0xB1: // LDA (addr),Y*
                        temp16 = ReadWordPageWrap(ReadMemory(PC++));
                        A = ReadMemory((ushort)(temp16+Y));
                        if ((temp16 & 0xFF00) != ((temp16+Y) & 0xFF00)) 
                            { PendingCycles--; TotalExecutedCycles++; }
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 5; TotalExecutedCycles += 5;
                        break;
                    case 0xB4: // LDY zp,X
                        Y = ReadMemory((byte)(ReadMemory(PC++)+X));
                        P = (byte)((P & 0x7D) | TableNZ[Y]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0xB5: // LDA zp,X
                        A = ReadMemory((byte)(ReadMemory(PC++)+X));
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0xB6: // LDX zp,Y
                        X = ReadMemory((byte)(ReadMemory(PC++)+Y));
                        P = (byte)((P & 0x7D) | TableNZ[X]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0xB8: // CLV
                        FlagV = false;
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0xB9: // LDA addr,Y*
                        temp16 = ReadWord(PC);
                        A = ReadMemory((ushort)(temp16+Y));
                        if ((temp16 & 0xFF00) != ((temp16 + Y) & 0xFF00)) 
                            { PendingCycles--; TotalExecutedCycles++; }
                        PC += 2;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0xBA: // TSX
                        X = S;
                        P = (byte)((P & 0x7D) | TableNZ[X]);
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0xBC: // LDY addr,X*
                        temp16 = ReadWord(PC);
                        Y = ReadMemory((ushort)(temp16+X));
                        if ((temp16 & 0xFF00) != ((temp16 + X) & 0xFF00)) 
                            { PendingCycles--; TotalExecutedCycles++; }
                        PC += 2;
                        P = (byte)((P & 0x7D) | TableNZ[Y]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0xBD: // LDA addr,X*
                        temp16 = ReadWord(PC);
                        A = ReadMemory((ushort)(temp16+X));
                        if ((temp16 & 0xFF00) != ((temp16 + X) & 0xFF00)) 
                            { PendingCycles--; TotalExecutedCycles++; }
                        PC += 2;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0xBE: // LDX addr,Y*
                        temp16 = ReadWord(PC);
                        X = ReadMemory((ushort)(temp16+Y));
                        if ((temp16 & 0xFF00) != ((temp16 + Y) & 0xFF00)) 
                            { PendingCycles--; TotalExecutedCycles++; }
                        PC += 2;
                        P = (byte)((P & 0x7D) | TableNZ[X]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0xC0: // CPY #nn
                        value8 = ReadMemory(PC++);
                        value16 = (ushort) (Y - value8);
                        FlagC = (Y >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 2;  TotalExecutedCycles += 2;
                        break;
                    case 0xC1: // CMP (addr,X)
                        value8 = ReadMemory(ReadWordPageWrap((byte)(ReadMemory(PC++)+X)));
                        value16 = (ushort) (A - value8);
                        FlagC = (A >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 6;  TotalExecutedCycles += 6;
                        break;
                    case 0xC2: // NOP #nn
                        PC += 1;
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0xC4: // CPY zp
                        value8 = ReadMemory(ReadMemory(PC++));
                        value16 = (ushort) (Y - value8);
                        FlagC = (Y >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 3;  TotalExecutedCycles += 3;
                        break;
                    case 0xC5: // CMP zp
                        value8 = ReadMemory(ReadMemory(PC++));
                        value16 = (ushort) (A - value8);
                        FlagC = (A >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 3;  TotalExecutedCycles += 3;
                        break;
                    case 0xC6: // DEC zp
                        value16 = ReadMemory(PC++);
                        value8 = (byte)(ReadMemory(value16) - 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 5; TotalExecutedCycles += 5;
                        break;
                    case 0xC8: // INY
                        P = (byte)((P & 0x7D) | TableNZ[++Y]);
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0xC9: // CMP #nn
                        value8 = ReadMemory(PC++);
                        value16 = (ushort) (A - value8);
                        FlagC = (A >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 2;  TotalExecutedCycles += 2;
                        break;
                    case 0xCA: // DEX
                        P = (byte)((P & 0x7D) | TableNZ[--X]);
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0xCC: // CPY addr
                        value8 = ReadMemory(ReadWord(PC)); PC += 2;
                        value16 = (ushort) (Y - value8);
                        FlagC = (Y >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 4;  TotalExecutedCycles += 4;
                        break;
                    case 0xCD: // CMP addr
                        value8 = ReadMemory(ReadWord(PC)); PC += 2;
                        value16 = (ushort) (A - value8);
                        FlagC = (A >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 4;  TotalExecutedCycles += 4;
                        break;
                    case 0xCE: // DEC addr
                        value16 = ReadWord(PC); PC += 2;
                        value8 = (byte)(ReadMemory(value16) - 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 6; TotalExecutedCycles += 6;
                        break;
                    case 0xD0: // BNE +/-rel
                        rel8 = (sbyte)ReadMemory(PC++);
                        value16 = (ushort)(PC+rel8);
                        if (FlagZ == false) {
                            PendingCycles--; TotalExecutedCycles++;
                            if ((PC & 0xFF00) != (value16 & 0xFF00)) 
                                { PendingCycles--; TotalExecutedCycles++; }
                            PC = value16;
                        }
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0xD1: // CMP (addr),Y*
                        temp16 = ReadWordPageWrap(ReadMemory(PC++));
                        value8 = ReadMemory((ushort)(temp16+Y));
                        if ((temp16 & 0xFF00) != ((temp16+Y) & 0xFF00)) 
                            { PendingCycles--; TotalExecutedCycles++; }
                        value16 = (ushort) (A - value8);
                        FlagC = (A >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 5;  TotalExecutedCycles += 5;
                        break;
                    case 0xD4: // NOP zp,X
                        PC += 1;
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0xD5: // CMP zp,X
                        value8 = ReadMemory((byte)(ReadMemory(PC++)+X));
                        value16 = (ushort) (A - value8);
                        FlagC = (A >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 4;  TotalExecutedCycles += 4;
                        break;
                    case 0xD6: // DEC zp,X
                        value16 = (byte)(ReadMemory(PC++)+X);
                        value8 = (byte)(ReadMemory(value16) - 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 6; TotalExecutedCycles += 6;
                        break;
                    case 0xD8: // CLD
                        FlagD = false;
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0xD9: // CMP addr,Y*
                        temp16 = ReadWord(PC);
                        value8 = ReadMemory((ushort)(temp16+Y));
                        if ((temp16 & 0xFF00) != ((temp16 + Y) & 0xFF00)) 
                            { PendingCycles--; TotalExecutedCycles++; }
                        PC += 2;
                        value16 = (ushort) (A - value8);
                        FlagC = (A >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 4;  TotalExecutedCycles += 4;
                        break;
                    case 0xDA: // NOP
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0xDC: // NOP (addr,X)
                        PC += 1;
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0xDD: // CMP addr,X*
                        temp16 = ReadWord(PC);
                        value8 = ReadMemory((ushort)(temp16+X));
                        if ((temp16 & 0xFF00) != ((temp16 + X) & 0xFF00)) 
                            { PendingCycles--; TotalExecutedCycles++; }
                        PC += 2;
                        value16 = (ushort) (A - value8);
                        FlagC = (A >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 4;  TotalExecutedCycles += 4;
                        break;
                    case 0xDE: // DEC addr,X
                        value16 = (ushort)(ReadWord(PC)+X);
                        PC += 2;
                        value8 = (byte)(ReadMemory(value16) - 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 7; TotalExecutedCycles += 7;
                        break;
                    case 0xE0: // CPX #nn
                        value8 = ReadMemory(PC++);
                        value16 = (ushort) (X - value8);
                        FlagC = (X >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 2;  TotalExecutedCycles += 2;
                        break;
                    case 0xE1: // SBC (addr,X)
                        value8 = ReadMemory(ReadWordPageWrap((byte)(ReadMemory(PC++)+X)));
                        temp = A - value8 - (FlagC?0:1);
                        FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
                        FlagC = temp >= 0;
                        A = (byte)temp;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 6; TotalExecutedCycles += 6;
                        break;
                    case 0xE2: // NOP #nn
                        PC += 1;
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0xE4: // CPX zp
                        value8 = ReadMemory(ReadMemory(PC++));
                        value16 = (ushort) (X - value8);
                        FlagC = (X >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 3;  TotalExecutedCycles += 3;
                        break;
                    case 0xE5: // SBC zp
                        value8 = ReadMemory(ReadMemory(PC++));
                        temp = A - value8 - (FlagC?0:1);
                        FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
                        FlagC = temp >= 0;
                        A = (byte)temp;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 3; TotalExecutedCycles += 3;
                        break;
                    case 0xE6: // INC zp
                        value16 = ReadMemory(PC++);
                        value8 = (byte)(ReadMemory(value16) + 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 5; TotalExecutedCycles += 5;
                        break;
                    case 0xE8: // INX
                        P = (byte)((P & 0x7D) | TableNZ[++X]);
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0xE9: // SBC #nn
                        value8 = ReadMemory(PC++);
                        temp = A - value8 - (FlagC?0:1);
                        FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
                        FlagC = temp >= 0;
                        A = (byte)temp;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0xEA: // NOP
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0xEC: // CPX addr
                        value8 = ReadMemory(ReadWord(PC)); PC += 2;
                        value16 = (ushort) (X - value8);
                        FlagC = (X >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 4;  TotalExecutedCycles += 4;
                        break;
                    case 0xED: // SBC addr
                        value8 = ReadMemory(ReadWord(PC)); PC += 2;
                        temp = A - value8 - (FlagC?0:1);
                        FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
                        FlagC = temp >= 0;
                        A = (byte)temp;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0xEE: // INC addr
                        value16 = ReadWord(PC); PC += 2;
                        value8 = (byte)(ReadMemory(value16) + 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 6; TotalExecutedCycles += 6;
                        break;
                    case 0xF0: // BEQ +/-rel
                        rel8 = (sbyte)ReadMemory(PC++);
                        value16 = (ushort)(PC+rel8);
                        if (FlagZ == true) {
                            PendingCycles--; TotalExecutedCycles++;
                            if ((PC & 0xFF00) != (value16 & 0xFF00)) 
                                { PendingCycles--; TotalExecutedCycles++; }
                            PC = value16;
                        }
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0xF1: // SBC (addr),Y*
                        temp16 = ReadWordPageWrap(ReadMemory(PC++));
                        value8 = ReadMemory((ushort)(temp16+Y));
                        if ((temp16 & 0xFF00) != ((temp16+Y) & 0xFF00)) 
                            { PendingCycles--; TotalExecutedCycles++; }
                        temp = A - value8 - (FlagC?0:1);
                        FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
                        FlagC = temp >= 0;
                        A = (byte)temp;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 5; TotalExecutedCycles += 5;
                        break;
                    case 0xF4: // NOP zp,X
                        PC += 1;
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0xF5: // SBC zp,X
                        value8 = ReadMemory((byte)(ReadMemory(PC++)+X));
                        temp = A - value8 - (FlagC?0:1);
                        FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
                        FlagC = temp >= 0;
                        A = (byte)temp;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0xF6: // INC zp,X
                        value16 = (byte)(ReadMemory(PC++)+X);
                        value8 = (byte)(ReadMemory(value16) + 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 6; TotalExecutedCycles += 6;
                        break;
                    case 0xF8: // SED
                        FlagD = true;
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0xF9: // SBC addr,Y*
                        temp16 = ReadWord(PC);
                        value8 = ReadMemory((ushort)(temp16+Y));
                        if ((temp16 & 0xFF00) != ((temp16 + Y) & 0xFF00)) 
                            { PendingCycles--; TotalExecutedCycles++; }
                        PC += 2;
                        temp = A - value8 - (FlagC?0:1);
                        FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
                        FlagC = temp >= 0;
                        A = (byte)temp;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0xFA: // NOP
                        PendingCycles -= 2; TotalExecutedCycles += 2;
                        break;
                    case 0xFC: // NOP (addr,X)
                        PC += 1;
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0xFD: // SBC addr,X*
                        temp16 = ReadWord(PC);
                        value8 = ReadMemory((ushort)(temp16+X));
                        if ((temp16 & 0xFF00) != ((temp16 + X) & 0xFF00)) 
                            { PendingCycles--; TotalExecutedCycles++; }
                        PC += 2;
                        temp = A - value8 - (FlagC?0:1);
                        FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
                        FlagC = temp >= 0;
                        A = (byte)temp;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4; TotalExecutedCycles += 4;
                        break;
                    case 0xFE: // INC addr,X
                        value16 = (ushort)(ReadWord(PC)+X);
                        PC += 2;
                        value8 = (byte)(ReadMemory(value16) + 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 7; TotalExecutedCycles += 7;
                        break;
                   default:
                       if(throw_unhandled)
                           throw new Exception(String.Format("Unhandled opcode: {0:X2}", opcode));
                      break;
                }
            }
        }
    }
}
