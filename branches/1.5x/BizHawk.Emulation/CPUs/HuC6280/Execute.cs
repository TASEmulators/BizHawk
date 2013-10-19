using System;

// Do not modify this file directly! This is GENERATED code.
// Please open the CpuCoreGenerator solution and make your modifications there.

namespace BizHawk.Emulation.CPUs.H6280
{
    public partial class HuC6280
    {
        public bool Debug;
        public Action<string> Logger;

        public void Execute(int cycles)
        {
            sbyte rel8;
            byte value8, temp8, source8;
            ushort value16, temp16;
            int temp, lo, hi;

            PendingCycles += cycles;
            while (PendingCycles > 0)
            {
                int lastCycles = PendingCycles;

                if (IRQ1Assert && FlagI == false && LagIFlag == false && (IRQControlByte & IRQ1Selector) == 0 && InBlockTransfer == false)
                {
                    WriteMemory((ushort)(S-- + 0x2100), (byte)(PC >> 8));
                    WriteMemory((ushort)(S-- + 0x2100), (byte)PC);
                    WriteMemory((ushort)(S-- + 0x2100), (byte)(P & (~0x10)));
                    FlagD = false;
                    FlagI = true;
                    PC = ReadWord(IRQ1Vector);
                    PendingCycles -= 8;
                }

                if (TimerAssert && FlagI == false && LagIFlag == false && (IRQControlByte & TimerSelector) == 0 && InBlockTransfer == false)
                {
                    WriteMemory((ushort)(S-- + 0x2100), (byte)(PC >> 8));
                    WriteMemory((ushort)(S-- + 0x2100), (byte)PC);
                    WriteMemory((ushort)(S-- + 0x2100), (byte)(P & (~0x10)));
                    FlagD = false;
                    FlagI = true;
                    PC = ReadWord(TimerVector);
                    PendingCycles -= 8;
                }

                if (IRQ2Assert && FlagI == false && LagIFlag == false && (IRQControlByte & IRQ2Selector) == 0 && InBlockTransfer == false)
                {
                    WriteMemory((ushort)(S-- + 0x2100), (byte)(PC >> 8));
                    WriteMemory((ushort)(S-- + 0x2100), (byte)PC);
                    WriteMemory((ushort)(S-- + 0x2100), (byte)(P & (~0x10)));
                    FlagD = false;
                    FlagI = true;
                    PC = ReadWord(IRQ2Vector);
                    PendingCycles -= 8;
                }

                IRQControlByte = IRQNextControlByte;
                LagIFlag = FlagI;

                if (Debug) Logger(State());

                byte opcode = ReadMemory(PC++);
                switch (opcode)
                {
                    case 0x00: // BRK
                        Console.WriteLine("EXEC BRK");
                        PC++;
                        WriteMemory((ushort)(S-- + 0x2100), (byte)(PC >> 8));
                        WriteMemory((ushort)(S-- + 0x2100), (byte)PC);
                        WriteMemory((ushort)(S-- + 0x2100), (byte)(P & (~0x10)));
                        FlagT = false;
                        FlagB = true;
                        FlagD = false;
                        FlagI = true;
                        PC = ReadWord(IRQ2Vector);
                        PendingCycles -= 8;
                        break;
                    case 0x01: // ORA (addr,X)
                        value8 = ReadMemory(ReadWordPageWrap((ushort)((byte)(ReadMemory(PC++)+X)+0x2000)));
                        if (FlagT == false)
                        {
                            A |= value8;
                            P = (byte)((P & 0x7D) | TableNZ[A]);
                            PendingCycles -= 7;
                        } else {
                            source8 = ReadMemory((ushort)(0x2000 + X));
                            source8 |= value8;
                            P = (byte)((P & 0x7D) | TableNZ[source8]);
                            WriteMemory((ushort)(0x2000 + X), source8);
                            PendingCycles -= 10;
                        }
                        break;
                    case 0x02: // SXY
                        temp8 = X;
                        X = Y;
                        Y = temp8;
                        PendingCycles -= 3;
                        break;
                    case 0x03: // ST0 #nn
                        value8 = ReadMemory(PC++);
                        WriteVDC(0,value8);
                        PendingCycles -= 4;
                        break;
                    case 0x04: // TSB zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        value8 = ReadMemory(value16);
                        WriteMemory(value16, (byte)(value8 | A));
                        FlagN = (value8 & 0x80) != 0;
                        FlagV = (value8 & 0x40) != 0;
                        FlagZ = (A | value8) == 0;
                        PendingCycles -= 6;
                        break;
                    case 0x05: // ORA zp
                        value8 = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        if (FlagT == false)
                        {
                            A |= value8;
                            P = (byte)((P & 0x7D) | TableNZ[A]);
                            PendingCycles -= 4;
                        } else {
                            source8 = ReadMemory((ushort)(0x2000 + X));
                            source8 |= value8;
                            P = (byte)((P & 0x7D) | TableNZ[source8]);
                            WriteMemory((ushort)(0x2000 + X), source8);
                            PendingCycles -= 7;
                        }
                        break;
                    case 0x06: // ASL zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        value8 = ReadMemory(value16);
                        FlagC = (value8 & 0x80) != 0;
                        value8 = (byte)(value8 << 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 6;
                        break;
                    case 0x07: // RMB0 zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        value8 = ReadMemory(value16);
                        value8 &= 0xFE;
                        WriteMemory(value16, value8);
                        PendingCycles -= 7;
                        break;
                    case 0x08: // PHP
                        WriteMemory((ushort)(S-- + 0x2100), P);
                        PendingCycles -= 3;
                        break;
                    case 0x09: // ORA #nn
                        value8 = ReadMemory(PC++);
                        if (FlagT == false)
                        {
                            A |= value8;
                            P = (byte)((P & 0x7D) | TableNZ[A]);
                            PendingCycles -= 2;
                        } else {
                            source8 = ReadMemory((ushort)(0x2000 + X));
                            source8 |= value8;
                            P = (byte)((P & 0x7D) | TableNZ[source8]);
                            WriteMemory((ushort)(0x2000 + X), source8);
                            PendingCycles -= 5;
                        }
                        break;
                    case 0x0A: // ASL A
                        FlagC = (A & 0x80) != 0;
                        A = (byte) (A << 1);
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 2;
                        break;
                    case 0x0C: // TSB addr
                        value16 = ReadWord(PC); PC += 2;
                        value8 = ReadMemory(value16);
                        WriteMemory(value16, (byte)(value8 | A));
                        FlagN = (value8 & 0x80) != 0;
                        FlagV = (value8 & 0x40) != 0;
                        FlagZ = (A | value8) == 0;
                        PendingCycles -= 7;
                        break;
                    case 0x0D: // ORA addr
                        value8 = ReadMemory(ReadWord(PC)); PC += 2;
                        if (FlagT == false)
                        {
                            A |= value8;
                            P = (byte)((P & 0x7D) | TableNZ[A]);
                            PendingCycles -= 5;
                        } else {
                            source8 = ReadMemory((ushort)(0x2000 + X));
                            source8 |= value8;
                            P = (byte)((P & 0x7D) | TableNZ[source8]);
                            WriteMemory((ushort)(0x2000 + X), source8);
                            PendingCycles -= 8;
                        }
                        break;
                    case 0x0E: // ASL addr
                        value16 = ReadWord(PC); PC += 2;
                        value8 = ReadMemory(value16);
                        FlagC = (value8 & 0x80) != 0;
                        value8 = (byte)(value8 << 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 7;
                        break;
                    case 0x0F: // BBR0
                        value8 = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        rel8 = (sbyte) ReadMemory(PC++);
                        if ((value8 & 0x01) == 0) {
                            PendingCycles -= 2;
                            PC = (ushort)(PC+rel8);
                        }
                        PendingCycles -= 6;
                        break;
                    case 0x10: // BPL +/-rel
                        rel8 = (sbyte)ReadMemory(PC++);
                        value16 = (ushort)(PC+rel8);
                        if (FlagN == false) {
                            PendingCycles -= 2;
                            PC = value16;
                        }
                        PendingCycles -= 2;
                        break;
                    case 0x11: // ORA (addr),Y
                        temp16 = ReadWordPageWrap((ushort)(ReadMemory(PC++)+0x2000));
                        value8 = ReadMemory((ushort)(temp16+Y));
                        if (FlagT == false)
                        {
                            A |= value8;
                            P = (byte)((P & 0x7D) | TableNZ[A]);
                            PendingCycles -= 7;
                        } else {
                            source8 = ReadMemory((ushort)(0x2000 + X));
                            source8 |= value8;
                            P = (byte)((P & 0x7D) | TableNZ[source8]);
                            WriteMemory((ushort)(0x2000 + X), source8);
                            PendingCycles -= 10;
                        }
                        break;
                    case 0x12: // ORA (addr)
                        value8 = ReadMemory(ReadWordPageWrap((ushort)(ReadMemory(PC++)+0x2000)));
                        if (FlagT == false)
                        {
                            A |= value8;
                            P = (byte)((P & 0x7D) | TableNZ[A]);
                            PendingCycles -= 7;
                        } else {
                            source8 = ReadMemory((ushort)(0x2000 + X));
                            source8 |= value8;
                            P = (byte)((P & 0x7D) | TableNZ[source8]);
                            WriteMemory((ushort)(0x2000 + X), source8);
                            PendingCycles -= 10;
                        }
                        break;
                    case 0x13: // ST1 #nn
                        value8 = ReadMemory(PC++);
                        WriteVDC(2,value8);
                        PendingCycles -= 4;
                        break;
                    case 0x14: // TRB zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        value8 = ReadMemory(value16);
                        WriteMemory(value16, (byte)(value8 & ~A));
                        FlagN = (value8 & 0x80) != 0;
                        FlagV = (value8 & 0x40) != 0;
                        FlagZ = (A & value8) == 0;
                        PendingCycles -= 6;
                        break;
                    case 0x15: // ORA zp,X
                        value8 = ReadMemory((ushort)(((ReadMemory(PC++)+X)&0xFF)+0x2000));
                        if (FlagT == false)
                        {
                            A |= value8;
                            P = (byte)((P & 0x7D) | TableNZ[A]);
                            PendingCycles -= 4;
                        } else {
                            source8 = ReadMemory((ushort)(0x2000 + X));
                            source8 |= value8;
                            P = (byte)((P & 0x7D) | TableNZ[source8]);
                            WriteMemory((ushort)(0x2000 + X), source8);
                            PendingCycles -= 7;
                        }
                        break;
                    case 0x16: // ASL zp,X
                        value16 = (ushort)(((ReadMemory(PC++)+X)&0xFF)+0x2000);
                        value8 = ReadMemory(value16);
                        FlagC = (value8 & 0x80) != 0;
                        value8 = (byte)(value8 << 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 6;
                        break;
                    case 0x17: // RMB1 zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        value8 = ReadMemory(value16);
                        value8 &= 0xFD;
                        WriteMemory(value16, value8);
                        PendingCycles -= 7;
                        break;
                    case 0x18: // CLC
                        FlagC = false;
                        PendingCycles -= 2;
                        break;
                    case 0x19: // ORA addr,Y
                        value8 = ReadMemory((ushort)(ReadWord(PC)+Y));
                        PC += 2;
                        if (FlagT == false)
                        {
                            A |= value8;
                            P = (byte)((P & 0x7D) | TableNZ[A]);
                            PendingCycles -= 5;
                        } else {
                            source8 = ReadMemory((ushort)(0x2000 + X));
                            source8 |= value8;
                            P = (byte)((P & 0x7D) | TableNZ[source8]);
                            WriteMemory((ushort)(0x2000 + X), source8);
                            PendingCycles -= 8;
                        }
                        break;
                    case 0x1A: // INC A
                        P = (byte)((P & 0x7D) | TableNZ[++A]);
                        PendingCycles -= 2;
                        break;
                    case 0x1C: // TRB addr
                        value16 = ReadWord(PC); PC += 2;
                        value8 = ReadMemory(value16);
                        WriteMemory(value16, (byte)(value8 & ~A));
                        FlagN = (value8 & 0x80) != 0;
                        FlagV = (value8 & 0x40) != 0;
                        FlagZ = (A & value8) == 0;
                        PendingCycles -= 7;
                        break;
                    case 0x1D: // ORA addr,X
                        value8 = ReadMemory((ushort)(ReadWord(PC)+X));
                        PC += 2;
                        if (FlagT == false)
                        {
                            A |= value8;
                            P = (byte)((P & 0x7D) | TableNZ[A]);
                            PendingCycles -= 5;
                        } else {
                            source8 = ReadMemory((ushort)(0x2000 + X));
                            source8 |= value8;
                            P = (byte)((P & 0x7D) | TableNZ[source8]);
                            WriteMemory((ushort)(0x2000 + X), source8);
                            PendingCycles -= 8;
                        }
                        break;
                    case 0x1E: // ASL addr,X
                        value16 = (ushort)(ReadWord(PC)+X);
                        PC += 2;
                        value8 = ReadMemory(value16);
                        FlagC = (value8 & 0x80) != 0;
                        value8 = (byte)(value8 << 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 7;
                        break;
                    case 0x1F: // BBR1
                        value8 = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        rel8 = (sbyte) ReadMemory(PC++);
                        if ((value8 & 0x02) == 0) {
                            PendingCycles -= 2;
                            PC = (ushort)(PC+rel8);
                        }
                        PendingCycles -= 6;
                        break;
                    case 0x20: // JSR addr
                        temp16 = (ushort)(PC+1);
                        WriteMemory((ushort)(S-- + 0x2100), (byte)(temp16 >> 8));
                        WriteMemory((ushort)(S-- + 0x2100), (byte)temp16);
                        PC = ReadWord(PC);
                        PendingCycles -= 7;
                        break;
                    case 0x21: // AND (addr,X)
                        value8 = ReadMemory(ReadWordPageWrap((ushort)((byte)(ReadMemory(PC++)+X)+0x2000)));
                        if (FlagT == false) { 
                            A &= value8;
                            P = (byte)((P & 0x7D) | TableNZ[A]);
                            PendingCycles -= 7;
                        } else {
                            temp8 = ReadMemory((ushort)(0x2000 + X));
                            temp8 &= value8;
                            P = (byte)((P & 0x7D) | TableNZ[temp8]);
                            WriteMemory((ushort)(0x2000 + X), temp8);
                            PendingCycles -= 10;
                        }
                        break;
                    case 0x22: // SAX
                        temp8 = A;
                        A = X;
                        X = temp8;
                        PendingCycles -= 3;
                        break;
                    case 0x23: // ST2 #nn
                        value8 = ReadMemory(PC++);
                        WriteVDC(3,value8);
                        PendingCycles -= 4;
                        break;
                    case 0x24: // BIT zp
                        value8 = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        FlagN = (value8 & 0x80) != 0;
                        FlagV = (value8 & 0x40) != 0;
                        FlagZ = (A & value8) == 0;
                        PendingCycles -= 4;
                        break;
                    case 0x25: // AND zp
                        value8 = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        if (FlagT == false) { 
                            A &= value8;
                            P = (byte)((P & 0x7D) | TableNZ[A]);
                            PendingCycles -= 4;
                        } else {
                            temp8 = ReadMemory((ushort)(0x2000 + X));
                            temp8 &= value8;
                            P = (byte)((P & 0x7D) | TableNZ[temp8]);
                            WriteMemory((ushort)(0x2000 + X), temp8);
                            PendingCycles -= 7;
                        }
                        break;
                    case 0x26: // ROL zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        value8 = temp8 = ReadMemory(value16);
                        value8 = (byte)((value8 << 1) | (P & 1));
                        WriteMemory(value16, value8);
                        FlagC = (temp8 & 0x80) != 0;
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 6;
                        break;
                    case 0x27: // RMB2 zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        value8 = ReadMemory(value16);
                        value8 &= 0xFB;
                        WriteMemory(value16, value8);
                        PendingCycles -= 7;
                        break;
                    case 0x28: // PLP
                        P = ReadMemory((ushort)(++S + 0x2100));
                        PendingCycles -= 4;
                        goto AfterClearTFlag;
                    case 0x29: // AND #nn
                        value8 = ReadMemory(PC++);
                        if (FlagT == false) { 
                            A &= value8;
                            P = (byte)((P & 0x7D) | TableNZ[A]);
                            PendingCycles -= 2;
                        } else {
                            temp8 = ReadMemory((ushort)(0x2000 + X));
                            temp8 &= value8;
                            P = (byte)((P & 0x7D) | TableNZ[temp8]);
                            WriteMemory((ushort)(0x2000 + X), temp8);
                            PendingCycles -= 5;
                        }
                        break;
                    case 0x2A: // ROL A
                        temp8 = A;
                        A = (byte)((A << 1) | (P & 1));
                        FlagC = (temp8 & 0x80) != 0;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 2;
                        break;
                    case 0x2C: // BIT addr
                        value8 = ReadMemory(ReadWord(PC)); PC += 2;
                        FlagN = (value8 & 0x80) != 0;
                        FlagV = (value8 & 0x40) != 0;
                        FlagZ = (A & value8) == 0;
                        PendingCycles -= 5;
                        break;
                    case 0x2D: // AND addr
                        value8 = ReadMemory(ReadWord(PC)); PC += 2;
                        if (FlagT == false) { 
                            A &= value8;
                            P = (byte)((P & 0x7D) | TableNZ[A]);
                            PendingCycles -= 5;
                        } else {
                            temp8 = ReadMemory((ushort)(0x2000 + X));
                            temp8 &= value8;
                            P = (byte)((P & 0x7D) | TableNZ[temp8]);
                            WriteMemory((ushort)(0x2000 + X), temp8);
                            PendingCycles -= 8;
                        }
                        break;
                    case 0x2E: // ROL addr
                        value16 = ReadWord(PC); PC += 2;
                        value8 = temp8 = ReadMemory(value16);
                        value8 = (byte)((value8 << 1) | (P & 1));
                        WriteMemory(value16, value8);
                        FlagC = (temp8 & 0x80) != 0;
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 7;
                        break;
                    case 0x2F: // BBR2
                        value8 = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        rel8 = (sbyte) ReadMemory(PC++);
                        if ((value8 & 0x04) == 0) {
                            PendingCycles -= 2;
                            PC = (ushort)(PC+rel8);
                        }
                        PendingCycles -= 6;
                        break;
                    case 0x30: // BMI +/-rel
                        rel8 = (sbyte)ReadMemory(PC++);
                        value16 = (ushort)(PC+rel8);
                        if (FlagN == true) {
                            PendingCycles -= 2;
                            PC = value16;
                        }
                        PendingCycles -= 2;
                        break;
                    case 0x31: // AND (addr),Y
                        temp16 = ReadWordPageWrap((ushort)(ReadMemory(PC++)+0x2000));
                        value8 = ReadMemory((ushort)(temp16+Y));
                        if (FlagT == false) { 
                            A &= value8;
                            P = (byte)((P & 0x7D) | TableNZ[A]);
                            PendingCycles -= 7;
                        } else {
                            temp8 = ReadMemory((ushort)(0x2000 + X));
                            temp8 &= value8;
                            P = (byte)((P & 0x7D) | TableNZ[temp8]);
                            WriteMemory((ushort)(0x2000 + X), temp8);
                            PendingCycles -= 10;
                        }
                        break;
                    case 0x32: // AND (addr)
                        value8 = ReadMemory(ReadWordPageWrap((ushort)(ReadMemory(PC++)+0x2000)));
                        if (FlagT == false) { 
                            A &= value8;
                            P = (byte)((P & 0x7D) | TableNZ[A]);
                            PendingCycles -= 7;
                        } else {
                            temp8 = ReadMemory((ushort)(0x2000 + X));
                            temp8 &= value8;
                            P = (byte)((P & 0x7D) | TableNZ[temp8]);
                            WriteMemory((ushort)(0x2000 + X), temp8);
                            PendingCycles -= 10;
                        }
                        break;
                    case 0x34: // BIT zp,X
                        value8 = ReadMemory((ushort)(((ReadMemory(PC++)+X)&0xFF)+0x2000));
                        FlagN = (value8 & 0x80) != 0;
                        FlagV = (value8 & 0x40) != 0;
                        FlagZ = (A & value8) == 0;
                        PendingCycles -= 4;
                        break;
                    case 0x35: // AND zp,X
                        value8 = ReadMemory((ushort)(((ReadMemory(PC++)+X)&0xFF)+0x2000));
                        if (FlagT == false) { 
                            A &= value8;
                            P = (byte)((P & 0x7D) | TableNZ[A]);
                            PendingCycles -= 4;
                        } else {
                            temp8 = ReadMemory((ushort)(0x2000 + X));
                            temp8 &= value8;
                            P = (byte)((P & 0x7D) | TableNZ[temp8]);
                            WriteMemory((ushort)(0x2000 + X), temp8);
                            PendingCycles -= 7;
                        }
                        break;
                    case 0x36: // ROL zp,X
                        value16 = (ushort)(((ReadMemory(PC++)+X)&0xFF)+0x2000);
                        value8 = temp8 = ReadMemory(value16);
                        value8 = (byte)((value8 << 1) | (P & 1));
                        WriteMemory(value16, value8);
                        FlagC = (temp8 & 0x80) != 0;
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 6;
                        break;
                    case 0x37: // RMB3 zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        value8 = ReadMemory(value16);
                        value8 &= 0xF7;
                        WriteMemory(value16, value8);
                        PendingCycles -= 7;
                        break;
                    case 0x38: // SEC
                        FlagC = true;
                        PendingCycles -= 2;
                        break;
                    case 0x39: // AND addr,Y
                        value8 = ReadMemory((ushort)(ReadWord(PC)+Y));
                        PC += 2;
                        if (FlagT == false) { 
                            A &= value8;
                            P = (byte)((P & 0x7D) | TableNZ[A]);
                            PendingCycles -= 5;
                        } else {
                            temp8 = ReadMemory((ushort)(0x2000 + X));
                            temp8 &= value8;
                            P = (byte)((P & 0x7D) | TableNZ[temp8]);
                            WriteMemory((ushort)(0x2000 + X), temp8);
                            PendingCycles -= 8;
                        }
                        break;
                    case 0x3A: // DEC A
                        P = (byte)((P & 0x7D) | TableNZ[--A]);
                        PendingCycles -= 2;
                        break;
                    case 0x3C: // BIT addr,X
                        value8 = ReadMemory((ushort)(ReadWord(PC)+X));
                        PC += 2;
                        FlagN = (value8 & 0x80) != 0;
                        FlagV = (value8 & 0x40) != 0;
                        FlagZ = (A & value8) == 0;
                        PendingCycles -= 5;
                        break;
                    case 0x3D: // AND addr,X
                        value8 = ReadMemory((ushort)(ReadWord(PC)+X));
                        PC += 2;
                        if (FlagT == false) { 
                            A &= value8;
                            P = (byte)((P & 0x7D) | TableNZ[A]);
                            PendingCycles -= 5;
                        } else {
                            temp8 = ReadMemory((ushort)(0x2000 + X));
                            temp8 &= value8;
                            P = (byte)((P & 0x7D) | TableNZ[temp8]);
                            WriteMemory((ushort)(0x2000 + X), temp8);
                            PendingCycles -= 8;
                        }
                        break;
                    case 0x3E: // ROL addr,X
                        value16 = (ushort)(ReadWord(PC)+X);
                        PC += 2;
                        value8 = temp8 = ReadMemory(value16);
                        value8 = (byte)((value8 << 1) | (P & 1));
                        WriteMemory(value16, value8);
                        FlagC = (temp8 & 0x80) != 0;
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 7;
                        break;
                    case 0x3F: // BBR3
                        value8 = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        rel8 = (sbyte) ReadMemory(PC++);
                        if ((value8 & 0x08) == 0) {
                            PendingCycles -= 2;
                            PC = (ushort)(PC+rel8);
                        }
                        PendingCycles -= 6;
                        break;
                    case 0x40: // RTI
                        P = ReadMemory((ushort)(++S + 0x2100));
                        PC = ReadMemory((ushort)(++S + 0x2100));
                        PC |= (ushort)(ReadMemory((ushort)(++S + 0x2100)) << 8);
                        PendingCycles -= 7;
                        goto AfterClearTFlag;
                    case 0x41: // EOR (addr,X)
                        value8 = ReadMemory(ReadWordPageWrap((ushort)((byte)(ReadMemory(PC++)+X)+0x2000)));
                        if (FlagT == false) { 
                            A ^= value8;
                            P = (byte)((P & 0x7D) | TableNZ[A]);
                            PendingCycles -= 7;
                        } else {
                            temp8 = ReadMemory((ushort)(0x2000 + X));
                            temp8 ^= value8;
                            P = (byte)((P & 0x7D) | TableNZ[temp8]);
                            WriteMemory((ushort)(0x2000 + X), temp8);
                            PendingCycles -= 10;
                        }
                        break;
                    case 0x42: // SAY
                        temp8 = A;
                        A = Y;
                        Y = temp8;
                        PendingCycles -= 3;
                        break;
                    case 0x43: // TMA #nn
                        value8 = ReadMemory(PC++);
                             if ((value8 & 0x01) != 0) A = MPR[0];
                        else if ((value8 & 0x02) != 0) A = MPR[1];
                        else if ((value8 & 0x04) != 0) A = MPR[2];
                        else if ((value8 & 0x08) != 0) A = MPR[3];
                        else if ((value8 & 0x10) != 0) A = MPR[4];
                        else if ((value8 & 0x20) != 0) A = MPR[5];
                        else if ((value8 & 0x40) != 0) A = MPR[6];
                        else if ((value8 & 0x80) != 0) A = MPR[7];
                        PendingCycles -= 4;
                        break;
                    case 0x44: // BSR +/-rel
                        rel8 = (sbyte)ReadMemory(PC++);
                        value16 = (ushort)(PC+rel8);
                        temp16 = (ushort)(PC-1);
                        WriteMemory((ushort)(S-- + 0x2100), (byte)(temp16 >> 8));
                        WriteMemory((ushort)(S-- + 0x2100), (byte)temp16);
                        PC = value16;
                        PendingCycles -= 8;
                        break;
                    case 0x45: // EOR zp
                        value8 = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        if (FlagT == false) { 
                            A ^= value8;
                            P = (byte)((P & 0x7D) | TableNZ[A]);
                            PendingCycles -= 4;
                        } else {
                            temp8 = ReadMemory((ushort)(0x2000 + X));
                            temp8 ^= value8;
                            P = (byte)((P & 0x7D) | TableNZ[temp8]);
                            WriteMemory((ushort)(0x2000 + X), temp8);
                            PendingCycles -= 7;
                        }
                        break;
                    case 0x46: // LSR zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        value8 = ReadMemory(value16);
                        FlagC = (value8 & 1) != 0;
                        value8 = (byte)(value8 >> 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 6;
                        break;
                    case 0x47: // RMB4 zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        value8 = ReadMemory(value16);
                        value8 &= 0xEF;
                        WriteMemory(value16, value8);
                        PendingCycles -= 7;
                        break;
                    case 0x48: // PHA
                        WriteMemory((ushort)(S-- + 0x2100), A);
                        PendingCycles -= 3;
                        break;
                    case 0x49: // EOR #nn
                        value8 = ReadMemory(PC++);
                        if (FlagT == false) { 
                            A ^= value8;
                            P = (byte)((P & 0x7D) | TableNZ[A]);
                            PendingCycles -= 2;
                        } else {
                            temp8 = ReadMemory((ushort)(0x2000 + X));
                            temp8 ^= value8;
                            P = (byte)((P & 0x7D) | TableNZ[temp8]);
                            WriteMemory((ushort)(0x2000 + X), temp8);
                            PendingCycles -= 5;
                        }
                        break;
                    case 0x4A: // LSR A
                        FlagC = (A & 1) != 0;
                        A = (byte) (A >> 1);
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 2;
                        break;
                    case 0x4C: // JMP addr
                        PC = ReadWord(PC);
                        PendingCycles -= 4;
                        break;
                    case 0x4D: // EOR addr
                        value8 = ReadMemory(ReadWord(PC)); PC += 2;
                        if (FlagT == false) { 
                            A ^= value8;
                            P = (byte)((P & 0x7D) | TableNZ[A]);
                            PendingCycles -= 5;
                        } else {
                            temp8 = ReadMemory((ushort)(0x2000 + X));
                            temp8 ^= value8;
                            P = (byte)((P & 0x7D) | TableNZ[temp8]);
                            WriteMemory((ushort)(0x2000 + X), temp8);
                            PendingCycles -= 8;
                        }
                        break;
                    case 0x4E: // LSR addr
                        value16 = ReadWord(PC); PC += 2;
                        value8 = ReadMemory(value16);
                        FlagC = (value8 & 1) != 0;
                        value8 = (byte)(value8 >> 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 7;
                        break;
                    case 0x4F: // BBR4
                        value8 = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        rel8 = (sbyte) ReadMemory(PC++);
                        if ((value8 & 0x10) == 0) {
                            PendingCycles -= 2;
                            PC = (ushort)(PC+rel8);
                        }
                        PendingCycles -= 6;
                        break;
                    case 0x50: // BVC +/-rel
                        rel8 = (sbyte)ReadMemory(PC++);
                        value16 = (ushort)(PC+rel8);
                        if (FlagV == false) {
                            PendingCycles -= 2;
                            PC = value16;
                        }
                        PendingCycles -= 2;
                        break;
                    case 0x51: // EOR (addr),Y
                        temp16 = ReadWordPageWrap((ushort)(ReadMemory(PC++)+0x2000));
                        value8 = ReadMemory((ushort)(temp16+Y));
                        if (FlagT == false) { 
                            A ^= value8;
                            P = (byte)((P & 0x7D) | TableNZ[A]);
                            PendingCycles -= 7;
                        } else {
                            temp8 = ReadMemory((ushort)(0x2000 + X));
                            temp8 ^= value8;
                            P = (byte)((P & 0x7D) | TableNZ[temp8]);
                            WriteMemory((ushort)(0x2000 + X), temp8);
                            PendingCycles -= 10;
                        }
                        break;
                    case 0x52: // EOR (addr)
                        value8 = ReadMemory(ReadWordPageWrap((ushort)(ReadMemory(PC++)+0x2000)));
                        if (FlagT == false) { 
                            A ^= value8;
                            P = (byte)((P & 0x7D) | TableNZ[A]);
                            PendingCycles -= 7;
                        } else {
                            temp8 = ReadMemory((ushort)(0x2000 + X));
                            temp8 ^= value8;
                            P = (byte)((P & 0x7D) | TableNZ[temp8]);
                            WriteMemory((ushort)(0x2000 + X), temp8);
                            PendingCycles -= 10;
                        }
                        break;
                    case 0x53: // TAM #nn
                        value8 = ReadMemory(PC++);
                        for (byte reg=0; reg<8; reg++)
                        {
                            if ((value8 & (1 << reg)) != 0)
                                MPR[reg] = A;
                        }
                        PendingCycles -= 5;
                        break;
                    case 0x54: // CSL
                        LowSpeed = true;
                        PendingCycles -= 3;
                        break;
                    case 0x55: // EOR zp,X
                        value8 = ReadMemory((ushort)(((ReadMemory(PC++)+X)&0xFF)+0x2000));
                        if (FlagT == false) { 
                            A ^= value8;
                            P = (byte)((P & 0x7D) | TableNZ[A]);
                            PendingCycles -= 4;
                        } else {
                            temp8 = ReadMemory((ushort)(0x2000 + X));
                            temp8 ^= value8;
                            P = (byte)((P & 0x7D) | TableNZ[temp8]);
                            WriteMemory((ushort)(0x2000 + X), temp8);
                            PendingCycles -= 7;
                        }
                        break;
                    case 0x56: // LSR zp,X
                        value16 = (ushort)(((ReadMemory(PC++)+X)&0xFF)+0x2000);
                        value8 = ReadMemory(value16);
                        FlagC = (value8 & 1) != 0;
                        value8 = (byte)(value8 >> 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 6;
                        break;
                    case 0x57: // RMB5 zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        value8 = ReadMemory(value16);
                        value8 &= 0xDF;
                        WriteMemory(value16, value8);
                        PendingCycles -= 7;
                        break;
                    case 0x58: // CLI
                        FlagI = false;
                        PendingCycles -= 2;
                        break;
                    case 0x59: // EOR addr,Y
                        value8 = ReadMemory((ushort)(ReadWord(PC)+Y));
                        PC += 2;
                        if (FlagT == false) { 
                            A ^= value8;
                            P = (byte)((P & 0x7D) | TableNZ[A]);
                            PendingCycles -= 5;
                        } else {
                            temp8 = ReadMemory((ushort)(0x2000 + X));
                            temp8 ^= value8;
                            P = (byte)((P & 0x7D) | TableNZ[temp8]);
                            WriteMemory((ushort)(0x2000 + X), temp8);
                            PendingCycles -= 8;
                        }
                        break;
                    case 0x5A: // PHY
                        WriteMemory((ushort)(S-- + 0x2100), Y);
                        PendingCycles -= 3;
                        break;
                    case 0x5D: // EOR addr,X
                        value8 = ReadMemory((ushort)(ReadWord(PC)+X));
                        PC += 2;
                        if (FlagT == false) { 
                            A ^= value8;
                            P = (byte)((P & 0x7D) | TableNZ[A]);
                            PendingCycles -= 5;
                        } else {
                            temp8 = ReadMemory((ushort)(0x2000 + X));
                            temp8 ^= value8;
                            P = (byte)((P & 0x7D) | TableNZ[temp8]);
                            WriteMemory((ushort)(0x2000 + X), temp8);
                            PendingCycles -= 8;
                        }
                        break;
                    case 0x5E: // LSR addr,X
                        value16 = (ushort)(ReadWord(PC)+X);
                        PC += 2;
                        value8 = ReadMemory(value16);
                        FlagC = (value8 & 1) != 0;
                        value8 = (byte)(value8 >> 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 7;
                        break;
                    case 0x5F: // BBR5
                        value8 = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        rel8 = (sbyte) ReadMemory(PC++);
                        if ((value8 & 0x20) == 0) {
                            PendingCycles -= 2;
                            PC = (ushort)(PC+rel8);
                        }
                        PendingCycles -= 6;
                        break;
                    case 0x60: // RTS
                        PC = ReadMemory((ushort)(++S + 0x2100));
                        PC |= (ushort)(ReadMemory((ushort)(++S + 0x2100)) << 8);
                        PC++;
                        PendingCycles -= 7;
                        break;
                    case 0x61: // ADC (addr,X)
                        value8 = ReadMemory(ReadWordPageWrap((ushort)((byte)(ReadMemory(PC++)+X)+0x2000)));
                        source8 = FlagT ? ReadMemory((ushort)(0x2000 + X)) : A;

                        if ((P & 0x08) != 0) {
                            lo = (source8 & 0x0F) + (value8 & 0x0F) + (FlagC ? 1 : 0);
                            hi = (source8 & 0xF0) + (value8 & 0xF0);
                            if (lo > 0x09) {
                                hi += 0x10;
                                lo += 0x06;
                            }
                            if (hi > 0x90) hi += 0x60;
                            FlagV = (~(source8^value8) & (source8^hi) & 0x80) != 0;
                            FlagC = (hi & 0xFF00) != 0;
                            source8 = (byte) ((lo & 0x0F) | (hi & 0xF0));
                            PendingCycles--;
                        } else {
                            temp = value8 + source8 + (FlagC ? 1 : 0);
                            FlagV = (~(source8 ^ value8) & (source8 ^ temp) & 0x80) != 0;
                            FlagC = temp > 0xFF;
                            source8 = (byte)temp;
                        }
                        if (FlagT == false)
                            A = source8;
                        else { 
                            WriteMemory((ushort)(0x2000 + X), source8);
                            PendingCycles -= 3;
                        }
                        P = (byte)((P & 0x7D) | TableNZ[source8]);
                        PendingCycles -= 7;
                        break;
                    case 0x62: // CLA
                        A = 0;
                        PendingCycles -= 2;
                        break;
                    case 0x64: // STZ zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        WriteMemory(value16, 0);
                        PendingCycles -= 4;
                        break;
                    case 0x65: // ADC zp
                        value8 = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        source8 = FlagT ? ReadMemory((ushort)(0x2000 + X)) : A;

                        if ((P & 0x08) != 0) {
                            lo = (source8 & 0x0F) + (value8 & 0x0F) + (FlagC ? 1 : 0);
                            hi = (source8 & 0xF0) + (value8 & 0xF0);
                            if (lo > 0x09) {
                                hi += 0x10;
                                lo += 0x06;
                            }
                            if (hi > 0x90) hi += 0x60;
                            FlagV = (~(source8^value8) & (source8^hi) & 0x80) != 0;
                            FlagC = (hi & 0xFF00) != 0;
                            source8 = (byte) ((lo & 0x0F) | (hi & 0xF0));
                            PendingCycles--;
                        } else {
                            temp = value8 + source8 + (FlagC ? 1 : 0);
                            FlagV = (~(source8 ^ value8) & (source8 ^ temp) & 0x80) != 0;
                            FlagC = temp > 0xFF;
                            source8 = (byte)temp;
                        }
                        if (FlagT == false)
                            A = source8;
                        else { 
                            WriteMemory((ushort)(0x2000 + X), source8);
                            PendingCycles -= 3;
                        }
                        P = (byte)((P & 0x7D) | TableNZ[source8]);
                        PendingCycles -= 4;
                        break;
                    case 0x66: // ROR zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        value8 = temp8 = ReadMemory(value16);
                        value8 = (byte)((value8 >> 1) | ((P & 1)<<7));
                        WriteMemory(value16, value8);
                        FlagC = (temp8 & 1) != 0;
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 6;
                        break;
                    case 0x67: // RMB6 zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        value8 = ReadMemory(value16);
                        value8 &= 0xBF;
                        WriteMemory(value16, value8);
                        PendingCycles -= 7;
                        break;
                    case 0x68: // PLA
                        A = ReadMemory((ushort)(++S + 0x2100));
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4;
                        break;
                    case 0x69: // ADC #nn
                        value8 = ReadMemory(PC++);
                        source8 = FlagT ? ReadMemory((ushort)(0x2000 + X)) : A;

                        if ((P & 0x08) != 0) {
                            lo = (source8 & 0x0F) + (value8 & 0x0F) + (FlagC ? 1 : 0);
                            hi = (source8 & 0xF0) + (value8 & 0xF0);
                            if (lo > 0x09) {
                                hi += 0x10;
                                lo += 0x06;
                            }
                            if (hi > 0x90) hi += 0x60;
                            FlagV = (~(source8^value8) & (source8^hi) & 0x80) != 0;
                            FlagC = (hi & 0xFF00) != 0;
                            source8 = (byte) ((lo & 0x0F) | (hi & 0xF0));
                            PendingCycles--;
                        } else {
                            temp = value8 + source8 + (FlagC ? 1 : 0);
                            FlagV = (~(source8 ^ value8) & (source8 ^ temp) & 0x80) != 0;
                            FlagC = temp > 0xFF;
                            source8 = (byte)temp;
                        }
                        if (FlagT == false)
                            A = source8;
                        else { 
                            WriteMemory((ushort)(0x2000 + X), source8);
                            PendingCycles -= 3;
                        }
                        P = (byte)((P & 0x7D) | TableNZ[source8]);
                        PendingCycles -= 2;
                        break;
                    case 0x6A: // ROR A
                        temp8 = A;
                        A = (byte)((A >> 1) | ((P & 1)<<7));
                        FlagC = (temp8 & 1) != 0;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 2;
                        break;
                    case 0x6C: // JMP
                        PC = ReadWord(ReadWord(PC));
                        PendingCycles -= 7;
                        break;
                    case 0x6D: // ADC addr
                        value8 = ReadMemory(ReadWord(PC)); PC += 2;
                        source8 = FlagT ? ReadMemory((ushort)(0x2000 + X)) : A;

                        if ((P & 0x08) != 0) {
                            lo = (source8 & 0x0F) + (value8 & 0x0F) + (FlagC ? 1 : 0);
                            hi = (source8 & 0xF0) + (value8 & 0xF0);
                            if (lo > 0x09) {
                                hi += 0x10;
                                lo += 0x06;
                            }
                            if (hi > 0x90) hi += 0x60;
                            FlagV = (~(source8^value8) & (source8^hi) & 0x80) != 0;
                            FlagC = (hi & 0xFF00) != 0;
                            source8 = (byte) ((lo & 0x0F) | (hi & 0xF0));
                            PendingCycles--;
                        } else {
                            temp = value8 + source8 + (FlagC ? 1 : 0);
                            FlagV = (~(source8 ^ value8) & (source8 ^ temp) & 0x80) != 0;
                            FlagC = temp > 0xFF;
                            source8 = (byte)temp;
                        }
                        if (FlagT == false)
                            A = source8;
                        else { 
                            WriteMemory((ushort)(0x2000 + X), source8);
                            PendingCycles -= 3;
                        }
                        P = (byte)((P & 0x7D) | TableNZ[source8]);
                        PendingCycles -= 5;
                        break;
                    case 0x6E: // ROR addr
                        value16 = ReadWord(PC); PC += 2;
                        value8 = temp8 = ReadMemory(value16);
                        value8 = (byte)((value8 >> 1) | ((P & 1)<<7));
                        WriteMemory(value16, value8);
                        FlagC = (temp8 & 1) != 0;
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 7;
                        break;
                    case 0x6F: // BBR6
                        value8 = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        rel8 = (sbyte) ReadMemory(PC++);
                        if ((value8 & 0x40) == 0) {
                            PendingCycles -= 2;
                            PC = (ushort)(PC+rel8);
                        }
                        PendingCycles -= 6;
                        break;
                    case 0x70: // BVS +/-rel
                        rel8 = (sbyte)ReadMemory(PC++);
                        value16 = (ushort)(PC+rel8);
                        if (FlagV == true) {
                            PendingCycles -= 2;
                            PC = value16;
                        }
                        PendingCycles -= 2;
                        break;
                    case 0x71: // ADC (addr),Y
                        temp16 = ReadWordPageWrap((ushort)(ReadMemory(PC++)+0x2000));
                        value8 = ReadMemory((ushort)(temp16+Y));
                        source8 = FlagT ? ReadMemory((ushort)(0x2000 + X)) : A;

                        if ((P & 0x08) != 0) {
                            lo = (source8 & 0x0F) + (value8 & 0x0F) + (FlagC ? 1 : 0);
                            hi = (source8 & 0xF0) + (value8 & 0xF0);
                            if (lo > 0x09) {
                                hi += 0x10;
                                lo += 0x06;
                            }
                            if (hi > 0x90) hi += 0x60;
                            FlagV = (~(source8^value8) & (source8^hi) & 0x80) != 0;
                            FlagC = (hi & 0xFF00) != 0;
                            source8 = (byte) ((lo & 0x0F) | (hi & 0xF0));
                            PendingCycles--;
                        } else {
                            temp = value8 + source8 + (FlagC ? 1 : 0);
                            FlagV = (~(source8 ^ value8) & (source8 ^ temp) & 0x80) != 0;
                            FlagC = temp > 0xFF;
                            source8 = (byte)temp;
                        }
                        if (FlagT == false)
                            A = source8;
                        else { 
                            WriteMemory((ushort)(0x2000 + X), source8);
                            PendingCycles -= 3;
                        }
                        P = (byte)((P & 0x7D) | TableNZ[source8]);
                        PendingCycles -= 7;
                        break;
                    case 0x72: // ADC (addr)
                        value8 = ReadMemory(ReadWordPageWrap((ushort)(ReadMemory(PC++)+0x2000)));
                        source8 = FlagT ? ReadMemory((ushort)(0x2000 + X)) : A;

                        if ((P & 0x08) != 0) {
                            lo = (source8 & 0x0F) + (value8 & 0x0F) + (FlagC ? 1 : 0);
                            hi = (source8 & 0xF0) + (value8 & 0xF0);
                            if (lo > 0x09) {
                                hi += 0x10;
                                lo += 0x06;
                            }
                            if (hi > 0x90) hi += 0x60;
                            FlagV = (~(source8^value8) & (source8^hi) & 0x80) != 0;
                            FlagC = (hi & 0xFF00) != 0;
                            source8 = (byte) ((lo & 0x0F) | (hi & 0xF0));
                            PendingCycles--;
                        } else {
                            temp = value8 + source8 + (FlagC ? 1 : 0);
                            FlagV = (~(source8 ^ value8) & (source8 ^ temp) & 0x80) != 0;
                            FlagC = temp > 0xFF;
                            source8 = (byte)temp;
                        }
                        if (FlagT == false)
                            A = source8;
                        else { 
                            WriteMemory((ushort)(0x2000 + X), source8);
                            PendingCycles -= 3;
                        }
                        P = (byte)((P & 0x7D) | TableNZ[source8]);
                        PendingCycles -= 7;
                        break;
                    case 0x73: // TII src, dest, len
                        if (InBlockTransfer == false)
                        {
                            InBlockTransfer = true;
                            btFrom = ReadWord(PC); PC += 2;
                            btTo = ReadWord(PC); PC += 2;
                            btLen = ReadWord(PC); PC += 2;
                            PendingCycles -= 14;
                            PC -= 7;
                            break;
                        }

                        if (btLen-- != 0)
                        {
                            WriteMemory(btTo++, ReadMemory(btFrom++));
                            PendingCycles -= 6;
                            PC--;
                            break;
                        }

                        InBlockTransfer = false;
                        PendingCycles -= 3;
                        PC += 6;
                        break;
                    case 0x74: // STZ zp,X
                        value16 = (ushort)(((ReadMemory(PC++)+X)&0xFF)+0x2000);
                        WriteMemory(value16, 0);
                        PendingCycles -= 4;
                        break;
                    case 0x75: // ADC zp,X
                        value8 = ReadMemory((ushort)(((ReadMemory(PC++)+X)&0xFF)+0x2000));
                        source8 = FlagT ? ReadMemory((ushort)(0x2000 + X)) : A;

                        if ((P & 0x08) != 0) {
                            lo = (source8 & 0x0F) + (value8 & 0x0F) + (FlagC ? 1 : 0);
                            hi = (source8 & 0xF0) + (value8 & 0xF0);
                            if (lo > 0x09) {
                                hi += 0x10;
                                lo += 0x06;
                            }
                            if (hi > 0x90) hi += 0x60;
                            FlagV = (~(source8^value8) & (source8^hi) & 0x80) != 0;
                            FlagC = (hi & 0xFF00) != 0;
                            source8 = (byte) ((lo & 0x0F) | (hi & 0xF0));
                            PendingCycles--;
                        } else {
                            temp = value8 + source8 + (FlagC ? 1 : 0);
                            FlagV = (~(source8 ^ value8) & (source8 ^ temp) & 0x80) != 0;
                            FlagC = temp > 0xFF;
                            source8 = (byte)temp;
                        }
                        if (FlagT == false)
                            A = source8;
                        else { 
                            WriteMemory((ushort)(0x2000 + X), source8);
                            PendingCycles -= 3;
                        }
                        P = (byte)((P & 0x7D) | TableNZ[source8]);
                        PendingCycles -= 4;
                        break;
                    case 0x76: // ROR zp,X
                        value16 = (ushort)(((ReadMemory(PC++)+X)&0xFF)+0x2000);
                        value8 = temp8 = ReadMemory(value16);
                        value8 = (byte)((value8 >> 1) | ((P & 1)<<7));
                        WriteMemory(value16, value8);
                        FlagC = (temp8 & 1) != 0;
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 6;
                        break;
                    case 0x77: // RMB7 zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        value8 = ReadMemory(value16);
                        value8 &= 0x7F;
                        WriteMemory(value16, value8);
                        PendingCycles -= 7;
                        break;
                    case 0x78: // SEI
                        FlagI = true;
                        PendingCycles -= 2;
                        break;
                    case 0x79: // ADC addr,Y
                        value8 = ReadMemory((ushort)(ReadWord(PC)+Y));
                        PC += 2;
                        source8 = FlagT ? ReadMemory((ushort)(0x2000 + X)) : A;

                        if ((P & 0x08) != 0) {
                            lo = (source8 & 0x0F) + (value8 & 0x0F) + (FlagC ? 1 : 0);
                            hi = (source8 & 0xF0) + (value8 & 0xF0);
                            if (lo > 0x09) {
                                hi += 0x10;
                                lo += 0x06;
                            }
                            if (hi > 0x90) hi += 0x60;
                            FlagV = (~(source8^value8) & (source8^hi) & 0x80) != 0;
                            FlagC = (hi & 0xFF00) != 0;
                            source8 = (byte) ((lo & 0x0F) | (hi & 0xF0));
                            PendingCycles--;
                        } else {
                            temp = value8 + source8 + (FlagC ? 1 : 0);
                            FlagV = (~(source8 ^ value8) & (source8 ^ temp) & 0x80) != 0;
                            FlagC = temp > 0xFF;
                            source8 = (byte)temp;
                        }
                        if (FlagT == false)
                            A = source8;
                        else { 
                            WriteMemory((ushort)(0x2000 + X), source8);
                            PendingCycles -= 3;
                        }
                        P = (byte)((P & 0x7D) | TableNZ[source8]);
                        PendingCycles -= 5;
                        break;
                    case 0x7A: // PLY
                        Y = ReadMemory((ushort)(++S + 0x2100));
                        P = (byte)((P & 0x7D) | TableNZ[Y]);
                        PendingCycles -= 4;
                        break;
                    case 0x7C: // JMP
                        PC = ReadWord((ushort)(ReadWord(PC)+X));
                        PendingCycles -= 7;
                        break;
                    case 0x7D: // ADC addr,X
                        value8 = ReadMemory((ushort)(ReadWord(PC)+X));
                        PC += 2;
                        source8 = FlagT ? ReadMemory((ushort)(0x2000 + X)) : A;

                        if ((P & 0x08) != 0) {
                            lo = (source8 & 0x0F) + (value8 & 0x0F) + (FlagC ? 1 : 0);
                            hi = (source8 & 0xF0) + (value8 & 0xF0);
                            if (lo > 0x09) {
                                hi += 0x10;
                                lo += 0x06;
                            }
                            if (hi > 0x90) hi += 0x60;
                            FlagV = (~(source8^value8) & (source8^hi) & 0x80) != 0;
                            FlagC = (hi & 0xFF00) != 0;
                            source8 = (byte) ((lo & 0x0F) | (hi & 0xF0));
                            PendingCycles--;
                        } else {
                            temp = value8 + source8 + (FlagC ? 1 : 0);
                            FlagV = (~(source8 ^ value8) & (source8 ^ temp) & 0x80) != 0;
                            FlagC = temp > 0xFF;
                            source8 = (byte)temp;
                        }
                        if (FlagT == false)
                            A = source8;
                        else { 
                            WriteMemory((ushort)(0x2000 + X), source8);
                            PendingCycles -= 3;
                        }
                        P = (byte)((P & 0x7D) | TableNZ[source8]);
                        PendingCycles -= 5;
                        break;
                    case 0x7E: // ROR addr,X
                        value16 = (ushort)(ReadWord(PC)+X);
                        PC += 2;
                        value8 = temp8 = ReadMemory(value16);
                        value8 = (byte)((value8 >> 1) | ((P & 1)<<7));
                        WriteMemory(value16, value8);
                        FlagC = (temp8 & 1) != 0;
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 7;
                        break;
                    case 0x7F: // BBR7
                        value8 = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        rel8 = (sbyte) ReadMemory(PC++);
                        if ((value8 & 0x80) == 0) {
                            PendingCycles -= 2;
                            PC = (ushort)(PC+rel8);
                        }
                        PendingCycles -= 6;
                        break;
                    case 0x80: // BRA +/-rel
                        rel8 = (sbyte)ReadMemory(PC++);
                        PC = (ushort)(PC+rel8);
                        PendingCycles -= 4;
                        break;
                    case 0x81: // STA (addr,X)
                        value16 = ReadWordPageWrap((ushort)((byte)(ReadMemory(PC++)+X)+0x2000));
                        WriteMemory(value16, A);
                        PendingCycles -= 7;
                        break;
                    case 0x82: // CLX
                        X = 0;
                        PendingCycles -= 2;
                        break;
                    case 0x83: // TST
                        value8 = ReadMemory(PC++);
                        temp8 = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        FlagN = (temp8 & 0x80) != 0;
                        FlagV = (temp8 & 0x40) != 0;
                        FlagZ = (temp8 & value8) == 0;
                        PendingCycles -= 7;
                        break;
                    case 0x84: // STY zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        WriteMemory(value16, Y);
                        PendingCycles -= 4;
                        break;
                    case 0x85: // STA zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        WriteMemory(value16, A);
                        PendingCycles -= 4;
                        break;
                    case 0x86: // STX zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        WriteMemory(value16, X);
                        PendingCycles -= 4;
                        break;
                    case 0x87: // SMB0 zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        value8 = ReadMemory(value16);
                        value8 |= 0x01;
                        WriteMemory(value16, value8);
                        PendingCycles -= 7;
                        break;
                    case 0x88: // DEY
                        P = (byte)((P & 0x7D) | TableNZ[--Y]);
                        PendingCycles -= 2;
                        break;
                    case 0x89: // BIT #nn
                        value8 = ReadMemory(PC++);
                        FlagN = (value8 & 0x80) != 0;
                        FlagV = (value8 & 0x40) != 0;
                        FlagZ = (A & value8) == 0;
                        PendingCycles -= 2;
                        break;
                    case 0x8A: // TXA
                        A = X;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 2;
                        break;
                    case 0x8C: // STY addr
                        value16 = ReadWord(PC); PC += 2;
                        WriteMemory(value16, Y);
                        PendingCycles -= 5;
                        break;
                    case 0x8D: // STA addr
                        value16 = ReadWord(PC); PC += 2;
                        WriteMemory(value16, A);
                        PendingCycles -= 5;
                        break;
                    case 0x8E: // STX addr
                        value16 = ReadWord(PC); PC += 2;
                        WriteMemory(value16, X);
                        PendingCycles -= 5;
                        break;
                    case 0x8F: // BBS0
                        value8 = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        rel8 = (sbyte) ReadMemory(PC++);
                        if ((value8 & 0x01) != 0) {
                            PendingCycles -= 2;
                            PC = (ushort)(PC+rel8);
                        }
                        PendingCycles -= 6;
                        break;
                    case 0x90: // BCC +/-rel
                        rel8 = (sbyte)ReadMemory(PC++);
                        value16 = (ushort)(PC+rel8);
                        if (FlagC == false) {
                            PendingCycles -= 2;
                            PC = value16;
                        }
                        PendingCycles -= 2;
                        break;
                    case 0x91: // STA (addr),Y
                        temp16 = ReadWordPageWrap((ushort)(ReadMemory(PC++)+0x2000));
                        value16 = (ushort)(temp16+Y);
                        WriteMemory(value16, A);
                        PendingCycles -= 7;
                        break;
                    case 0x92: // STA (addr)
                        value16 = ReadWordPageWrap((ushort)(ReadMemory(PC++)+0x2000));
                        WriteMemory(value16, A);
                        PendingCycles -= 7;
                        break;
                    case 0x93: // TST
                        value8 = ReadMemory(PC++);
                        temp8 = ReadMemory(ReadWord(PC)); PC += 2;
                        FlagN = (temp8 & 0x80) != 0;
                        FlagV = (temp8 & 0x40) != 0;
                        FlagZ = (temp8 & value8) == 0;
                        PendingCycles -= 8;
                        break;
                    case 0x94: // STY zp,X
                        value16 = (ushort)(((ReadMemory(PC++)+X)&0xFF)+0x2000);
                        WriteMemory(value16, Y);
                        PendingCycles -= 4;
                        break;
                    case 0x95: // STA zp,X
                        value16 = (ushort)(((ReadMemory(PC++)+X)&0xFF)+0x2000);
                        WriteMemory(value16, A);
                        PendingCycles -= 4;
                        break;
                    case 0x96: // STX zp,Y
                        value16 = (ushort)(((ReadMemory(PC++)+Y)&0xFF)+0x2000);
                        WriteMemory(value16, X);
                        PendingCycles -= 4;
                        break;
                    case 0x97: // SMB1 zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        value8 = ReadMemory(value16);
                        value8 |= 0x02;
                        WriteMemory(value16, value8);
                        PendingCycles -= 7;
                        break;
                    case 0x98: // TYA
                        A = Y;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 2;
                        break;
                    case 0x99: // STA addr,Y
                        value16 = (ushort)(ReadWord(PC)+Y);
                        PC += 2;
                        WriteMemory(value16, A);
                        PendingCycles -= 5;
                        break;
                    case 0x9A: // TXS
                        S = X;
                        PendingCycles -= 2;
                        break;
                    case 0x9C: // STZ addr
                        value16 = ReadWord(PC); PC += 2;
                        WriteMemory(value16, 0);
                        PendingCycles -= 5;
                        break;
                    case 0x9D: // STA addr,X
                        value16 = (ushort)(ReadWord(PC)+X);
                        PC += 2;
                        WriteMemory(value16, A);
                        PendingCycles -= 5;
                        break;
                    case 0x9E: // STZ addr,X
                        value16 = (ushort)(ReadWord(PC)+X);
                        PC += 2;
                        WriteMemory(value16, 0);
                        PendingCycles -= 5;
                        break;
                    case 0x9F: // BBS1
                        value8 = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        rel8 = (sbyte) ReadMemory(PC++);
                        if ((value8 & 0x02) != 0) {
                            PendingCycles -= 2;
                            PC = (ushort)(PC+rel8);
                        }
                        PendingCycles -= 6;
                        break;
                    case 0xA0: // LDY #nn
                        Y = ReadMemory(PC++);
                        P = (byte)((P & 0x7D) | TableNZ[Y]);
                        PendingCycles -= 2;
                        break;
                    case 0xA1: // LDA (addr,X)
                        A = ReadMemory(ReadWordPageWrap((ushort)((byte)(ReadMemory(PC++)+X)+0x2000)));
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 7;
                        break;
                    case 0xA2: // LDX #nn
                        X = ReadMemory(PC++);
                        P = (byte)((P & 0x7D) | TableNZ[X]);
                        PendingCycles -= 2;
                        break;
                    case 0xA3: // TST
                        value8 = ReadMemory(PC++);
                        temp8 = ReadMemory((ushort)(((ReadMemory(PC++)+X)&0xFF)+0x2000));
                        FlagN = (temp8 & 0x80) != 0;
                        FlagV = (temp8 & 0x40) != 0;
                        FlagZ = (temp8 & value8) == 0;
                        PendingCycles -= 7;
                        break;
                    case 0xA4: // LDY zp
                        Y = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        P = (byte)((P & 0x7D) | TableNZ[Y]);
                        PendingCycles -= 4;
                        break;
                    case 0xA5: // LDA zp
                        A = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4;
                        break;
                    case 0xA6: // LDX zp
                        X = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        P = (byte)((P & 0x7D) | TableNZ[X]);
                        PendingCycles -= 4;
                        break;
                    case 0xA7: // SMB2 zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        value8 = ReadMemory(value16);
                        value8 |= 0x04;
                        WriteMemory(value16, value8);
                        PendingCycles -= 7;
                        break;
                    case 0xA8: // TAY
                        Y = A;
                        P = (byte)((P & 0x7D) | TableNZ[Y]);
                        PendingCycles -= 2;
                        break;
                    case 0xA9: // LDA #nn
                        A = ReadMemory(PC++);
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 2;
                        break;
                    case 0xAA: // TAX
                        X = A;
                        P = (byte)((P & 0x7D) | TableNZ[X]);
                        PendingCycles -= 2;
                        break;
                    case 0xAC: // LDY addr
                        Y = ReadMemory(ReadWord(PC)); PC += 2;
                        P = (byte)((P & 0x7D) | TableNZ[Y]);
                        PendingCycles -= 5;
                        break;
                    case 0xAD: // LDA addr
                        A = ReadMemory(ReadWord(PC)); PC += 2;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 5;
                        break;
                    case 0xAE: // LDX addr
                        X = ReadMemory(ReadWord(PC)); PC += 2;
                        P = (byte)((P & 0x7D) | TableNZ[X]);
                        PendingCycles -= 5;
                        break;
                    case 0xAF: // BBS2
                        value8 = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        rel8 = (sbyte) ReadMemory(PC++);
                        if ((value8 & 0x04) != 0) {
                            PendingCycles -= 2;
                            PC = (ushort)(PC+rel8);
                        }
                        PendingCycles -= 6;
                        break;
                    case 0xB0: // BCS +/-rel
                        rel8 = (sbyte)ReadMemory(PC++);
                        value16 = (ushort)(PC+rel8);
                        if (FlagC == true) {
                            PendingCycles -= 2;
                            PC = value16;
                        }
                        PendingCycles -= 2;
                        break;
                    case 0xB1: // LDA (addr),Y
                        temp16 = ReadWordPageWrap((ushort)(ReadMemory(PC++)+0x2000));
                        A = ReadMemory((ushort)(temp16+Y));
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 7;
                        break;
                    case 0xB2: // LDA (addr)
                        A = ReadMemory(ReadWordPageWrap((ushort)(ReadMemory(PC++)+0x2000)));
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 7;
                        break;
                    case 0xB3: // TST
                        value8 = ReadMemory(PC++);
                        temp8 = ReadMemory((ushort)(ReadWord(PC)+X));
                        PC += 2;
                        FlagN = (temp8 & 0x80) != 0;
                        FlagV = (temp8 & 0x40) != 0;
                        FlagZ = (temp8 & value8) == 0;
                        PendingCycles -= 8;
                        break;
                    case 0xB4: // LDY zp,X
                        Y = ReadMemory((ushort)(((ReadMemory(PC++)+X)&0xFF)+0x2000));
                        P = (byte)((P & 0x7D) | TableNZ[Y]);
                        PendingCycles -= 4;
                        break;
                    case 0xB5: // LDA zp,X
                        A = ReadMemory((ushort)(((ReadMemory(PC++)+X)&0xFF)+0x2000));
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4;
                        break;
                    case 0xB6: // LDX zp,Y
                        X = ReadMemory((ushort)(((ReadMemory(PC++)+Y)&0xFF)+0x2000));
                        P = (byte)((P & 0x7D) | TableNZ[X]);
                        PendingCycles -= 4;
                        break;
                    case 0xB7: // SMB3 zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        value8 = ReadMemory(value16);
                        value8 |= 0x08;
                        WriteMemory(value16, value8);
                        PendingCycles -= 7;
                        break;
                    case 0xB8: // CLV
                        FlagV = false;
                        PendingCycles -= 2;
                        break;
                    case 0xB9: // LDA addr,Y
                        A = ReadMemory((ushort)(ReadWord(PC)+Y));
                        PC += 2;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 5;
                        break;
                    case 0xBA: // TSX
                        X = S;
                        P = (byte)((P & 0x7D) | TableNZ[X]);
                        PendingCycles -= 2;
                        break;
                    case 0xBC: // LDY addr,X
                        Y = ReadMemory((ushort)(ReadWord(PC)+X));
                        PC += 2;
                        P = (byte)((P & 0x7D) | TableNZ[Y]);
                        PendingCycles -= 5;
                        break;
                    case 0xBD: // LDA addr,X
                        A = ReadMemory((ushort)(ReadWord(PC)+X));
                        PC += 2;
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 5;
                        break;
                    case 0xBE: // LDX addr,Y
                        X = ReadMemory((ushort)(ReadWord(PC)+Y));
                        PC += 2;
                        P = (byte)((P & 0x7D) | TableNZ[X]);
                        PendingCycles -= 5;
                        break;
                    case 0xBF: // BBS3
                        value8 = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        rel8 = (sbyte) ReadMemory(PC++);
                        if ((value8 & 0x08) != 0) {
                            PendingCycles -= 2;
                            PC = (ushort)(PC+rel8);
                        }
                        PendingCycles -= 6;
                        break;
                    case 0xC0: // CPY #nn
                        value8 = ReadMemory(PC++);
                        value16 = (ushort) (Y - value8);
                        FlagC = (Y >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 2;
                        break;
                    case 0xC1: // CMP (addr,X)
                        value8 = ReadMemory(ReadWordPageWrap((ushort)((byte)(ReadMemory(PC++)+X)+0x2000)));
                        value16 = (ushort) (A - value8);
                        FlagC = (A >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 7;
                        break;
                    case 0xC2: // CLY
                        Y = 0;
                        PendingCycles -= 2;
                        break;
                    case 0xC3: // TDD src, dest, len
                        if (InBlockTransfer == false)
                        {
                            InBlockTransfer = true;
                            btFrom = ReadWord(PC); PC += 2;
                            btTo = ReadWord(PC); PC += 2;
                            btLen = ReadWord(PC); PC += 2;
                            PendingCycles -= 14;
                            PC -= 7;
                            break;
                        }

                        if (btLen-- != 0)
                        {
                            WriteMemory(btTo--, ReadMemory(btFrom--));
                            PendingCycles -= 6;
                            PC--;
                            break;
                        }

                        InBlockTransfer = false;
                        PendingCycles -= 3;
                        PC += 6;
                        break;
                    case 0xC4: // CPY zp
                        value8 = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        value16 = (ushort) (Y - value8);
                        FlagC = (Y >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 4;
                        break;
                    case 0xC5: // CMP zp
                        value8 = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        value16 = (ushort) (A - value8);
                        FlagC = (A >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 4;
                        break;
                    case 0xC6: // DEC zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        value8 = (byte)(ReadMemory(value16) - 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 6;
                        break;
                    case 0xC7: // SMB4 zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        value8 = ReadMemory(value16);
                        value8 |= 0x10;
                        WriteMemory(value16, value8);
                        PendingCycles -= 7;
                        break;
                    case 0xC8: // INY
                        P = (byte)((P & 0x7D) | TableNZ[++Y]);
                        PendingCycles -= 2;
                        break;
                    case 0xC9: // CMP #nn
                        value8 = ReadMemory(PC++);
                        value16 = (ushort) (A - value8);
                        FlagC = (A >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 2;
                        break;
                    case 0xCA: // DEX
                        P = (byte)((P & 0x7D) | TableNZ[--X]);
                        PendingCycles -= 2;
                        break;
                    case 0xCC: // CPY addr
                        value8 = ReadMemory(ReadWord(PC)); PC += 2;
                        value16 = (ushort) (Y - value8);
                        FlagC = (Y >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 5;
                        break;
                    case 0xCD: // CMP addr
                        value8 = ReadMemory(ReadWord(PC)); PC += 2;
                        value16 = (ushort) (A - value8);
                        FlagC = (A >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 5;
                        break;
                    case 0xCE: // DEC addr
                        value16 = ReadWord(PC); PC += 2;
                        value8 = (byte)(ReadMemory(value16) - 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 7;
                        break;
                    case 0xCF: // BBS4
                        value8 = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        rel8 = (sbyte) ReadMemory(PC++);
                        if ((value8 & 0x10) != 0) {
                            PendingCycles -= 2;
                            PC = (ushort)(PC+rel8);
                        }
                        PendingCycles -= 6;
                        break;
                    case 0xD0: // BNE +/-rel
                        rel8 = (sbyte)ReadMemory(PC++);
                        value16 = (ushort)(PC+rel8);
                        if (FlagZ == false) {
                            PendingCycles -= 2;
                            PC = value16;
                        }
                        PendingCycles -= 2;
                        break;
                    case 0xD1: // CMP (addr),Y
                        temp16 = ReadWordPageWrap((ushort)(ReadMemory(PC++)+0x2000));
                        value8 = ReadMemory((ushort)(temp16+Y));
                        value16 = (ushort) (A - value8);
                        FlagC = (A >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 7;
                        break;
                    case 0xD2: // CMP (addr)
                        value8 = ReadMemory(ReadWordPageWrap((ushort)(ReadMemory(PC++)+0x2000)));
                        value16 = (ushort) (A - value8);
                        FlagC = (A >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 7;
                        break;
                    case 0xD3: // TIN src, dest, len
                        if (InBlockTransfer == false)
                        {
                            InBlockTransfer = true;
                            btFrom = ReadWord(PC); PC += 2;
                            btTo = ReadWord(PC); PC += 2;
                            btLen = ReadWord(PC); PC += 2;
                            PendingCycles -= 14;
                            PC -= 7;
                            break;
                        }

                        if (btLen-- != 0)
                        {
                            WriteMemory(btTo, ReadMemory(btFrom++));
                            PendingCycles -= 6;
                            PC--;
                            break;
                        }

                        InBlockTransfer = false;
                        PendingCycles -= 3;
                        PC += 6;
                        break;
                    case 0xD4: // CSH
                        LowSpeed = false;
                        PendingCycles -= 3;
                        break;
                    case 0xD5: // CMP zp,X
                        value8 = ReadMemory((ushort)(((ReadMemory(PC++)+X)&0xFF)+0x2000));
                        value16 = (ushort) (A - value8);
                        FlagC = (A >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 4;
                        break;
                    case 0xD6: // DEC zp,X
                        value16 = (ushort)(((ReadMemory(PC++)+X)&0xFF)+0x2000);
                        value8 = (byte)(ReadMemory(value16) - 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 6;
                        break;
                    case 0xD7: // SMB5 zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        value8 = ReadMemory(value16);
                        value8 |= 0x20;
                        WriteMemory(value16, value8);
                        PendingCycles -= 7;
                        break;
                    case 0xD8: // CLD
                        FlagD = false;
                        PendingCycles -= 2;
                        break;
                    case 0xD9: // CMP addr,Y
                        value8 = ReadMemory((ushort)(ReadWord(PC)+Y));
                        PC += 2;
                        value16 = (ushort) (A - value8);
                        FlagC = (A >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 5;
                        break;
                    case 0xDA: // PHX
                        WriteMemory((ushort)(S-- + 0x2100), X);
                        PendingCycles -= 3;
                        break;
                    case 0xDD: // CMP addr,X
                        value8 = ReadMemory((ushort)(ReadWord(PC)+X));
                        PC += 2;
                        value16 = (ushort) (A - value8);
                        FlagC = (A >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 5;
                        break;
                    case 0xDE: // DEC addr,X
                        value16 = (ushort)(ReadWord(PC)+X);
                        PC += 2;
                        value8 = (byte)(ReadMemory(value16) - 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 7;
                        break;
                    case 0xDF: // BBS5
                        value8 = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        rel8 = (sbyte) ReadMemory(PC++);
                        if ((value8 & 0x20) != 0) {
                            PendingCycles -= 2;
                            PC = (ushort)(PC+rel8);
                        }
                        PendingCycles -= 6;
                        break;
                    case 0xE0: // CPX #nn
                        value8 = ReadMemory(PC++);
                        value16 = (ushort) (X - value8);
                        FlagC = (X >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 2;
                        break;
                    case 0xE1: // SBC (addr,X)
                        value8 = ReadMemory(ReadWordPageWrap((ushort)((byte)(ReadMemory(PC++)+X)+0x2000)));
                        temp = A - value8 - (FlagC ? 0 : 1);
                        if ((P & 0x08) != 0) {
                            lo = (A & 0x0F) - (value8 & 0x0F) - (FlagC ? 0 : 1);
                            hi = (A & 0xF0) - (value8 & 0xF0);
                            if ((lo & 0xF0) != 0) lo -= 0x06;
                            if ((lo & 0x80) != 0) hi -= 0x10;
                            if ((hi & 0x0F00) != 0) hi -= 0x60;
                            FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
                            FlagC = (hi & 0xFF00) == 0;
                            A = (byte) ((lo & 0x0F) | (hi & 0xF0));
                            PendingCycles--;
                        } else {
                            FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
                            FlagC = temp >= 0;
                            A = (byte)temp;
                        }
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 7;
                        break;
                    case 0xE3: // TIA src, dest, len
                        if (InBlockTransfer == false)
                        {
                            InBlockTransfer = true;
                            btFrom = ReadWord(PC); PC += 2;
                            btTo = ReadWord(PC); PC += 2;
                            btLen = ReadWord(PC); PC += 2;
                            btAlternator = 0;
                            PendingCycles -= 14;
                            PC -= 7;
                            break;
                        }

                        if (btLen-- != 0)
                        {
                            WriteMemory((ushort)(btTo+btAlternator), ReadMemory(btFrom++));
                            btAlternator ^= 1;
                            PendingCycles -= 6;
                            PC--;
                            break;
                        }

                        InBlockTransfer = false;
                        PendingCycles -= 3;
                        PC += 6;
                        break;
                    case 0xE4: // CPX zp
                        value8 = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        value16 = (ushort) (X - value8);
                        FlagC = (X >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 4;
                        break;
                    case 0xE5: // SBC zp
                        value8 = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        temp = A - value8 - (FlagC ? 0 : 1);
                        if ((P & 0x08) != 0) {
                            lo = (A & 0x0F) - (value8 & 0x0F) - (FlagC ? 0 : 1);
                            hi = (A & 0xF0) - (value8 & 0xF0);
                            if ((lo & 0xF0) != 0) lo -= 0x06;
                            if ((lo & 0x80) != 0) hi -= 0x10;
                            if ((hi & 0x0F00) != 0) hi -= 0x60;
                            FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
                            FlagC = (hi & 0xFF00) == 0;
                            A = (byte) ((lo & 0x0F) | (hi & 0xF0));
                            PendingCycles--;
                        } else {
                            FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
                            FlagC = temp >= 0;
                            A = (byte)temp;
                        }
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4;
                        break;
                    case 0xE6: // INC zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        value8 = (byte)(ReadMemory(value16) + 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 6;
                        break;
                    case 0xE7: // SMB6 zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        value8 = ReadMemory(value16);
                        value8 |= 0x40;
                        WriteMemory(value16, value8);
                        PendingCycles -= 7;
                        break;
                    case 0xE8: // INX
                        P = (byte)((P & 0x7D) | TableNZ[++X]);
                        PendingCycles -= 2;
                        break;
                    case 0xE9: // SBC #nn
                        value8 = ReadMemory(PC++);
                        temp = A - value8 - (FlagC ? 0 : 1);
                        if ((P & 0x08) != 0) {
                            lo = (A & 0x0F) - (value8 & 0x0F) - (FlagC ? 0 : 1);
                            hi = (A & 0xF0) - (value8 & 0xF0);
                            if ((lo & 0xF0) != 0) lo -= 0x06;
                            if ((lo & 0x80) != 0) hi -= 0x10;
                            if ((hi & 0x0F00) != 0) hi -= 0x60;
                            FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
                            FlagC = (hi & 0xFF00) == 0;
                            A = (byte) ((lo & 0x0F) | (hi & 0xF0));
                            PendingCycles--;
                        } else {
                            FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
                            FlagC = temp >= 0;
                            A = (byte)temp;
                        }
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 2;
                        break;
                    case 0xEA: // NOP
                        PendingCycles -= 2;
                        break;
                    case 0xEC: // CPX addr
                        value8 = ReadMemory(ReadWord(PC)); PC += 2;
                        value16 = (ushort) (X - value8);
                        FlagC = (X >= value8);
                        P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
                        PendingCycles -= 5;
                        break;
                    case 0xED: // SBC addr
                        value8 = ReadMemory(ReadWord(PC)); PC += 2;
                        temp = A - value8 - (FlagC ? 0 : 1);
                        if ((P & 0x08) != 0) {
                            lo = (A & 0x0F) - (value8 & 0x0F) - (FlagC ? 0 : 1);
                            hi = (A & 0xF0) - (value8 & 0xF0);
                            if ((lo & 0xF0) != 0) lo -= 0x06;
                            if ((lo & 0x80) != 0) hi -= 0x10;
                            if ((hi & 0x0F00) != 0) hi -= 0x60;
                            FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
                            FlagC = (hi & 0xFF00) == 0;
                            A = (byte) ((lo & 0x0F) | (hi & 0xF0));
                            PendingCycles--;
                        } else {
                            FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
                            FlagC = temp >= 0;
                            A = (byte)temp;
                        }
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 5;
                        break;
                    case 0xEE: // INC addr
                        value16 = ReadWord(PC); PC += 2;
                        value8 = (byte)(ReadMemory(value16) + 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 7;
                        break;
                    case 0xEF: // BBS6
                        value8 = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        rel8 = (sbyte) ReadMemory(PC++);
                        if ((value8 & 0x40) != 0) {
                            PendingCycles -= 2;
                            PC = (ushort)(PC+rel8);
                        }
                        PendingCycles -= 6;
                        break;
                    case 0xF0: // BEQ +/-rel
                        rel8 = (sbyte)ReadMemory(PC++);
                        value16 = (ushort)(PC+rel8);
                        if (FlagZ == true) {
                            PendingCycles -= 2;
                            PC = value16;
                        }
                        PendingCycles -= 2;
                        break;
                    case 0xF1: // SBC (addr),Y
                        temp16 = ReadWordPageWrap((ushort)(ReadMemory(PC++)+0x2000));
                        value8 = ReadMemory((ushort)(temp16+Y));
                        temp = A - value8 - (FlagC ? 0 : 1);
                        if ((P & 0x08) != 0) {
                            lo = (A & 0x0F) - (value8 & 0x0F) - (FlagC ? 0 : 1);
                            hi = (A & 0xF0) - (value8 & 0xF0);
                            if ((lo & 0xF0) != 0) lo -= 0x06;
                            if ((lo & 0x80) != 0) hi -= 0x10;
                            if ((hi & 0x0F00) != 0) hi -= 0x60;
                            FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
                            FlagC = (hi & 0xFF00) == 0;
                            A = (byte) ((lo & 0x0F) | (hi & 0xF0));
                            PendingCycles--;
                        } else {
                            FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
                            FlagC = temp >= 0;
                            A = (byte)temp;
                        }
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 7;
                        break;
                    case 0xF2: // SBC (addr)
                        value8 = ReadMemory(ReadWordPageWrap((ushort)(ReadMemory(PC++)+0x2000)));
                        temp = A - value8 - (FlagC ? 0 : 1);
                        if ((P & 0x08) != 0) {
                            lo = (A & 0x0F) - (value8 & 0x0F) - (FlagC ? 0 : 1);
                            hi = (A & 0xF0) - (value8 & 0xF0);
                            if ((lo & 0xF0) != 0) lo -= 0x06;
                            if ((lo & 0x80) != 0) hi -= 0x10;
                            if ((hi & 0x0F00) != 0) hi -= 0x60;
                            FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
                            FlagC = (hi & 0xFF00) == 0;
                            A = (byte) ((lo & 0x0F) | (hi & 0xF0));
                            PendingCycles--;
                        } else {
                            FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
                            FlagC = temp >= 0;
                            A = (byte)temp;
                        }
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 7;
                        break;
                    case 0xF3: // TAI src, dest, len
                        if (InBlockTransfer == false)
                        {
                            InBlockTransfer = true;
                            btFrom = ReadWord(PC); PC += 2;
                            btTo = ReadWord(PC); PC += 2;
                            btLen = ReadWord(PC); PC += 2;
                            btAlternator = 0;
                            PendingCycles -= 14;
                            PC -= 7;
                            break;
                        }

                        if (btLen-- != 0)
                        {
                            WriteMemory(btTo++, ReadMemory((ushort)(btFrom + btAlternator)));
                            btAlternator ^= 1;
                            PendingCycles -= 6;
                            PC--;
                            break;
                        }

                        InBlockTransfer = false;
                        PendingCycles -= 3;
                        PC += 6;
                        break;
                    case 0xF4: // SET
                        int a; // TODO remove these extra checks
                        string b = Disassemble(PC, out a);
                        if (b.StartsWith("ADC") == false && b.StartsWith("EOR") == false && b.StartsWith("AND") == false && b.StartsWith("ORA") == false)
                            Console.WriteLine("SETTING T FLAG, NEXT INSTRUCTION IS UNHANDLED:  {0}", b);
                        FlagT = true;
                        PendingCycles -= 2;
                        goto AfterClearTFlag;
                    case 0xF5: // SBC zp,X
                        value8 = ReadMemory((ushort)(((ReadMemory(PC++)+X)&0xFF)+0x2000));
                        temp = A - value8 - (FlagC ? 0 : 1);
                        if ((P & 0x08) != 0) {
                            lo = (A & 0x0F) - (value8 & 0x0F) - (FlagC ? 0 : 1);
                            hi = (A & 0xF0) - (value8 & 0xF0);
                            if ((lo & 0xF0) != 0) lo -= 0x06;
                            if ((lo & 0x80) != 0) hi -= 0x10;
                            if ((hi & 0x0F00) != 0) hi -= 0x60;
                            FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
                            FlagC = (hi & 0xFF00) == 0;
                            A = (byte) ((lo & 0x0F) | (hi & 0xF0));
                            PendingCycles--;
                        } else {
                            FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
                            FlagC = temp >= 0;
                            A = (byte)temp;
                        }
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 4;
                        break;
                    case 0xF6: // INC zp,X
                        value16 = (ushort)(((ReadMemory(PC++)+X)&0xFF)+0x2000);
                        value8 = (byte)(ReadMemory(value16) + 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 6;
                        break;
                    case 0xF7: // SMB7 zp
                        value16 = (ushort)(ReadMemory(PC++)+0x2000);
                        value8 = ReadMemory(value16);
                        value8 |= 0x80;
                        WriteMemory(value16, value8);
                        PendingCycles -= 7;
                        break;
                    case 0xF8: // SED
                        FlagD = true;
                        PendingCycles -= 2;
                        break;
                    case 0xF9: // SBC addr,Y
                        value8 = ReadMemory((ushort)(ReadWord(PC)+Y));
                        PC += 2;
                        temp = A - value8 - (FlagC ? 0 : 1);
                        if ((P & 0x08) != 0) {
                            lo = (A & 0x0F) - (value8 & 0x0F) - (FlagC ? 0 : 1);
                            hi = (A & 0xF0) - (value8 & 0xF0);
                            if ((lo & 0xF0) != 0) lo -= 0x06;
                            if ((lo & 0x80) != 0) hi -= 0x10;
                            if ((hi & 0x0F00) != 0) hi -= 0x60;
                            FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
                            FlagC = (hi & 0xFF00) == 0;
                            A = (byte) ((lo & 0x0F) | (hi & 0xF0));
                            PendingCycles--;
                        } else {
                            FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
                            FlagC = temp >= 0;
                            A = (byte)temp;
                        }
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 5;
                        break;
                    case 0xFA: // PLX
                        X = ReadMemory((ushort)(++S + 0x2100));
                        P = (byte)((P & 0x7D) | TableNZ[X]);
                        PendingCycles -= 4;
                        break;
                    case 0xFD: // SBC addr,X
                        value8 = ReadMemory((ushort)(ReadWord(PC)+X));
                        PC += 2;
                        temp = A - value8 - (FlagC ? 0 : 1);
                        if ((P & 0x08) != 0) {
                            lo = (A & 0x0F) - (value8 & 0x0F) - (FlagC ? 0 : 1);
                            hi = (A & 0xF0) - (value8 & 0xF0);
                            if ((lo & 0xF0) != 0) lo -= 0x06;
                            if ((lo & 0x80) != 0) hi -= 0x10;
                            if ((hi & 0x0F00) != 0) hi -= 0x60;
                            FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
                            FlagC = (hi & 0xFF00) == 0;
                            A = (byte) ((lo & 0x0F) | (hi & 0xF0));
                            PendingCycles--;
                        } else {
                            FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
                            FlagC = temp >= 0;
                            A = (byte)temp;
                        }
                        P = (byte)((P & 0x7D) | TableNZ[A]);
                        PendingCycles -= 5;
                        break;
                    case 0xFE: // INC addr,X
                        value16 = (ushort)(ReadWord(PC)+X);
                        PC += 2;
                        value8 = (byte)(ReadMemory(value16) + 1);
                        WriteMemory(value16, value8);
                        P = (byte)((P & 0x7D) | TableNZ[value8]);
                        PendingCycles -= 7;
                        break;
                    case 0xFF: // BBS7
                        value8 = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));
                        rel8 = (sbyte) ReadMemory(PC++);
                        if ((value8 & 0x80) != 0) {
                            PendingCycles -= 2;
                            PC = (ushort)(PC+rel8);
                        }
                        PendingCycles -= 6;
                        break;
                    default:
                        Console.WriteLine("Unhandled opcode: {0:X2}", opcode);
                        break;
                }

                P &= 0xDF; // Clear T flag
            AfterClearTFlag: // SET command jumps here
                int delta = lastCycles - PendingCycles;
                if (LowSpeed)
                {
                    delta *= 4;
                    PendingCycles = lastCycles - delta;
                }
                TotalExecutedCycles += delta;

                if (TimerEnabled)
                {
                    TimerTickCounter += delta;
                    while (TimerTickCounter >= 1024)
                    {
                        TimerValue--;
                        TimerTickCounter -= 1024;
                        if (TimerValue == 0xFF)
                        {
                            TimerValue = TimerReloadValue;
                            TimerAssert = true;
                        }
                    }
                }
                ThinkAction(delta);
            }
        }
    }
}
