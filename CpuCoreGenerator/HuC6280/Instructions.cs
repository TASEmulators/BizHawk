using System.IO;

namespace HuC6280
{
    public partial class CoreGenerator
    {
        private string SetNZ(string val)
        {
            return "P = (byte)((P & 0x7D) | TableNZ[" + val + "]);";
        }

        private void ADC(OpcodeInfo op, TextWriter w)
        {
            GetValue8(op, w, "value8");
            w.WriteLine(Spaces + "source8 = FlagT ? ReadMemory((ushort)(0x2000 + X)) : A;");
            w.WriteLine();
            w.WriteLine(Spaces + "if ((P & 0x08) != 0) {");
            w.WriteLine(Spaces + "    lo = (source8 & 0x0F) + (value8 & 0x0F) + (FlagC ? 1 : 0);");
            w.WriteLine(Spaces + "    hi = (source8 & 0xF0) + (value8 & 0xF0);");
            w.WriteLine(Spaces + "    if (lo > 0x09) {");
            w.WriteLine(Spaces + "        hi += 0x10;");
            w.WriteLine(Spaces + "        lo += 0x06;");
            w.WriteLine(Spaces + "    }");
            w.WriteLine(Spaces + "    if (hi > 0x90) hi += 0x60;");
            w.WriteLine(Spaces + "    FlagV = (~(source8^value8) & (source8^hi) & 0x80) != 0;");
            w.WriteLine(Spaces + "    FlagC = (hi & 0xFF00) != 0;");
            w.WriteLine(Spaces + "    source8 = (byte) ((lo & 0x0F) | (hi & 0xF0));");
            w.WriteLine(Spaces + "    PendingCycles--;");
            w.WriteLine(Spaces + "} else {");
            w.WriteLine(Spaces + "    temp = value8 + source8 + (FlagC ? 1 : 0);");
            w.WriteLine(Spaces + "    FlagV = (~(source8 ^ value8) & (source8 ^ temp) & 0x80) != 0;");
            w.WriteLine(Spaces + "    FlagC = temp > 0xFF;");
            w.WriteLine(Spaces + "    source8 = (byte)temp;");
            w.WriteLine(Spaces + "}");

            w.WriteLine(Spaces + "if (FlagT == false)");
            w.WriteLine(Spaces + "    A = source8;");
            w.WriteLine(Spaces + "else { ");
            w.WriteLine(Spaces + "    WriteMemory((ushort)(0x2000 + X), source8);");
            w.WriteLine(Spaces + "    PendingCycles -= 3;");
            w.WriteLine(Spaces + "}");
            w.WriteLine(Spaces + SetNZ("source8"));
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void AND(OpcodeInfo op, TextWriter w)
        {
            GetValue8(op, w, "value8");
            w.WriteLine(Spaces + "if (FlagT == false) { ");
            w.WriteLine(Spaces + "    A &= value8;");
            w.WriteLine(Spaces + "    " + SetNZ("A"));
            w.WriteLine(Spaces + "    PendingCycles -= {0};", op.Cycles);
            w.WriteLine(Spaces + "} else {");
            w.WriteLine(Spaces + "    temp8 = ReadMemory((ushort)(0x2000 + X));");
            w.WriteLine(Spaces + "    temp8 &= value8;");
            w.WriteLine(Spaces + "    " + SetNZ("temp8"));
            w.WriteLine(Spaces + "    WriteMemory((ushort)(0x2000 + X), temp8);");
            w.WriteLine(Spaces + "    PendingCycles -= {0};", op.Cycles + 3);
            w.WriteLine(Spaces + "}");
        }

        private void ASL(OpcodeInfo op, TextWriter w)
        {
            if (op.AddressMode == AddrMode.Accumulator)
            {
                w.WriteLine(Spaces + "FlagC = (A & 0x80) != 0;");
                w.WriteLine(Spaces + "A = (byte) (A << 1);");
                w.WriteLine(Spaces + SetNZ("A"));
            }
            else
            {
                GetAddress(op, w, "value16");
                w.WriteLine(Spaces + "value8 = ReadMemory(value16);");
                w.WriteLine(Spaces + "FlagC = (value8 & 0x80) != 0;");
                w.WriteLine(Spaces + "value8 = (byte)(value8 << 1);");
                w.WriteLine(Spaces + "WriteMemory(value16, value8);");
                w.WriteLine(Spaces + SetNZ("value8"));
            }
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void BB(OpcodeInfo op, TextWriter w, int bit, bool set)
        {
            string filter = "";
            switch (bit)
            {
                case 0: filter = "0x01"; break;
                case 1: filter = "0x02"; break;
                case 2: filter = "0x04"; break;
                case 3: filter = "0x08"; break;
                case 4: filter = "0x10"; break;
                case 5: filter = "0x20"; break;
                case 6: filter = "0x40"; break;
                case 7: filter = "0x80"; break;
            }
            string cond = set ? "!=" : "==";

            w.WriteLine(Spaces + "value8 = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));");
            w.WriteLine(Spaces + "rel8 = (sbyte) ReadMemory(PC++);");
            w.WriteLine(Spaces + "if ((value8 & "+filter+") "+cond+" 0) {");
            w.WriteLine(Spaces + "    PendingCycles -= 2;");
            w.WriteLine(Spaces + "    PC = (ushort)(PC+rel8);");
            w.WriteLine(Spaces + "}");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void Branch(OpcodeInfo op, TextWriter w, string flag, bool cond)
        {
            GetAddress(op, w, "value16");
            w.WriteLine(Spaces + "if (Flag" + flag + " == " + cond.ToString().ToLower() + ") {");
            w.WriteLine(Spaces + "    PendingCycles -= 2;");
            w.WriteLine(Spaces + "    PC = value16;");
            w.WriteLine(Spaces + "}");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void BRA(OpcodeInfo op, TextWriter w)
        {
            GetAddress(op, w, "PC");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void BRK(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "Console.WriteLine(\"EXEC BRK\");");
            w.WriteLine(Spaces + "PC++;");
            w.WriteLine(Spaces + "WriteMemory((ushort)(S-- + 0x2100), (byte)(PC >> 8));");
            w.WriteLine(Spaces + "WriteMemory((ushort)(S-- + 0x2100), (byte)PC);");
            w.WriteLine(Spaces + "WriteMemory((ushort)(S-- + 0x2100), (byte)(P & (~0x10)));");
            w.WriteLine(Spaces + "FlagT = false;");
            w.WriteLine(Spaces + "FlagB = true;");
            w.WriteLine(Spaces + "FlagD = false;");
            w.WriteLine(Spaces + "FlagI = true;");
            w.WriteLine(Spaces + "PC = ReadWord(IRQ2Vector);");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void BSR(OpcodeInfo op, TextWriter w)
        {
            GetAddress(op, w, "value16");
            w.WriteLine(Spaces + "temp16 = (ushort)(PC-1);");
            w.WriteLine(Spaces + "WriteMemory((ushort)(S-- + 0x2100), (byte)(temp16 >> 8));");
            w.WriteLine(Spaces + "WriteMemory((ushort)(S-- + 0x2100), (byte)temp16);");
            w.WriteLine(Spaces + "PC = value16;");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void BIT(OpcodeInfo op, TextWriter w)
        {
            GetValue8(op, w, "value8");
            w.WriteLine(Spaces + "FlagN = (value8 & 0x80) != 0;");
            w.WriteLine(Spaces + "FlagV = (value8 & 0x40) != 0;");
            w.WriteLine(Spaces + "FlagZ = (A & value8) == 0;");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void CLC(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "FlagC = false;");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void CLD(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "FlagD = false;");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void CLI(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "FlagI = false;");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void CLV(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "FlagV = false;");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void CLreg(OpcodeInfo op, TextWriter w, string reg)
        {
            w.WriteLine(Spaces + "{0} = 0;",reg);
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void CMP_reg(OpcodeInfo op, TextWriter w, string reg)
        {
            GetValue8(op, w, "value8");
            w.WriteLine(Spaces + "value16 = (ushort) (" + reg + " - value8);");
            w.WriteLine(Spaces + "FlagC = (" + reg + " >= value8);");
            w.WriteLine(Spaces + SetNZ("(byte)value16"));
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void CSH(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "LowSpeed = false;");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void CSL(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "LowSpeed = true;");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void DEC(OpcodeInfo op, TextWriter w)
        {
            if (op.AddressMode != AddrMode.Accumulator)
            {
                GetAddress(op, w, "value16");
                w.WriteLine(Spaces + "value8 = (byte)(ReadMemory(value16) - 1);");
                w.WriteLine(Spaces + "WriteMemory(value16, value8);");
                w.WriteLine(Spaces + SetNZ("value8"));
            } else {
                w.WriteLine(Spaces + SetNZ("--A"));
            }
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void DEX(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + SetNZ("--X"));
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void DEY(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + SetNZ("--Y"));
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void EOR(OpcodeInfo op, TextWriter w)
        {
            GetValue8(op, w, "value8");
            w.WriteLine(Spaces + "if (FlagT == false) { ");
            w.WriteLine(Spaces + "    A ^= value8;");
            w.WriteLine(Spaces + "    "+SetNZ("A"));
            w.WriteLine(Spaces + "    PendingCycles -= {0};", op.Cycles);
            w.WriteLine(Spaces + "} else {");
            w.WriteLine(Spaces + "    temp8 = ReadMemory((ushort)(0x2000 + X));");
            w.WriteLine(Spaces + "    temp8 ^= value8;");
            w.WriteLine(Spaces + "    " + SetNZ("temp8"));
            w.WriteLine(Spaces + "    WriteMemory((ushort)(0x2000 + X), temp8);");
            w.WriteLine(Spaces + "    PendingCycles -= {0};", op.Cycles+3);
            w.WriteLine(Spaces + "}");
        }

        private void INC(OpcodeInfo op, TextWriter w)
        {
            if (op.AddressMode != AddrMode.Accumulator)
            {
                GetAddress(op, w, "value16");
                w.WriteLine(Spaces + "value8 = (byte)(ReadMemory(value16) + 1);");
                w.WriteLine(Spaces + "WriteMemory(value16, value8);");
                w.WriteLine(Spaces + SetNZ("value8"));
            } else {
                w.WriteLine(Spaces + SetNZ("++A"));
            }
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void INX(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + SetNZ("++X"));
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void INY(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + SetNZ("++Y"));
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void JMP(OpcodeInfo op, TextWriter w)
        {
            switch (op.AddressMode)
            {
                case AddrMode.Absolute:          w.WriteLine(Spaces + "PC = ReadWord(PC);"); break;
                case AddrMode.AbsoluteIndirect:  w.WriteLine(Spaces + "PC = ReadWord(ReadWord(PC));"); break;
                case AddrMode.AbsoluteIndirectX: w.WriteLine(Spaces + "PC = ReadWord((ushort)(ReadWord(PC)+X));"); break;
            }
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void JSR(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "temp16 = (ushort)(PC+1);");
            w.WriteLine(Spaces + "WriteMemory((ushort)(S-- + 0x2100), (byte)(temp16 >> 8));");
            w.WriteLine(Spaces + "WriteMemory((ushort)(S-- + 0x2100), (byte)temp16);");
            w.WriteLine(Spaces + "PC = ReadWord(PC);");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void LDA(OpcodeInfo op, TextWriter w)
        {
            GetValue8(op, w, "A");
            w.WriteLine(Spaces + SetNZ("A"));
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void LDX(OpcodeInfo op, TextWriter w)
        {
            GetValue8(op, w, "X");
            w.WriteLine(Spaces + SetNZ("X"));
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void LDY(OpcodeInfo op, TextWriter w)
        {
            GetValue8(op, w, "Y");
            w.WriteLine(Spaces + SetNZ("Y"));
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void LSR(OpcodeInfo op, TextWriter w)
        {
            if (op.AddressMode == AddrMode.Accumulator)
            {
                w.WriteLine(Spaces + "FlagC = (A & 1) != 0;");
                w.WriteLine(Spaces + "A = (byte) (A >> 1);");
                w.WriteLine(Spaces + SetNZ("A"));
            }
            else
            {
                GetAddress(op, w, "value16");
                w.WriteLine(Spaces + "value8 = ReadMemory(value16);");
                w.WriteLine(Spaces + "FlagC = (value8 & 1) != 0;");
                w.WriteLine(Spaces + "value8 = (byte)(value8 >> 1);");
                w.WriteLine(Spaces + "WriteMemory(value16, value8);");
                w.WriteLine(Spaces + SetNZ("value8"));
            }
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void NOP(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void ORA(OpcodeInfo op, TextWriter w)
        {
            GetValue8(op, w, "value8");

            w.WriteLine(Spaces + "if (FlagT == false)");
            w.WriteLine(Spaces + "{");
            w.WriteLine(Spaces + "    A |= value8;");
            w.WriteLine(Spaces + "    "+SetNZ("A"));
            w.WriteLine(Spaces + "    PendingCycles -= {0};", op.Cycles);
            w.WriteLine(Spaces + "} else {");
            w.WriteLine(Spaces + "    source8 = ReadMemory((ushort)(0x2000 + X));");
            w.WriteLine(Spaces + "    source8 |= value8;");
            w.WriteLine(Spaces + "    " + SetNZ("source8"));
            w.WriteLine(Spaces + "    WriteMemory((ushort)(0x2000 + X), source8);");
            w.WriteLine(Spaces + "    PendingCycles -= {0};", op.Cycles+3);
            w.WriteLine(Spaces + "}");
        }

        private void PushReg(OpcodeInfo op, TextWriter w, string reg)
        {
            w.WriteLine(Spaces + "WriteMemory((ushort)(S-- + 0x2100), {0});", reg);
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void PullReg(OpcodeInfo op, TextWriter w, string reg)
        {
            w.WriteLine(Spaces + "{0} = ReadMemory((ushort)(++S + 0x2100));", reg);
            w.WriteLine(Spaces + SetNZ(reg));
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }


        private void PLP(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "P = ReadMemory((ushort)(++S + 0x2100));");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
            w.WriteLine(Spaces + "goto AfterClearTFlag;");
        }

        private void ROL(OpcodeInfo op, TextWriter w)
        {
            if (op.AddressMode == AddrMode.Accumulator)
            {
                w.WriteLine(Spaces + "temp8 = A;");
                w.WriteLine(Spaces + "A = (byte)((A << 1) | (P & 1));");
                w.WriteLine(Spaces + "FlagC = (temp8 & 0x80) != 0;");
                w.WriteLine(Spaces + SetNZ("A"));
            }
            else
            {
                GetAddress(op, w, "value16");
                w.WriteLine(Spaces + "value8 = temp8 = ReadMemory(value16);");
                w.WriteLine(Spaces + "value8 = (byte)((value8 << 1) | (P & 1));");
                w.WriteLine(Spaces + "WriteMemory(value16, value8);");
                w.WriteLine(Spaces + "FlagC = (temp8 & 0x80) != 0;");
                w.WriteLine(Spaces + SetNZ("value8"));
            }
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void ROR(OpcodeInfo op, TextWriter w)
        {
            if (op.AddressMode == AddrMode.Accumulator)
            {
                w.WriteLine(Spaces + "temp8 = A;");
                w.WriteLine(Spaces + "A = (byte)((A >> 1) | ((P & 1)<<7));");
                w.WriteLine(Spaces + "FlagC = (temp8 & 1) != 0;");
                w.WriteLine(Spaces + SetNZ("A"));
            }
            else
            {
                GetAddress(op, w, "value16");
                w.WriteLine(Spaces + "value8 = temp8 = ReadMemory(value16);");
                w.WriteLine(Spaces + "value8 = (byte)((value8 >> 1) | ((P & 1)<<7));");
                w.WriteLine(Spaces + "WriteMemory(value16, value8);");
                w.WriteLine(Spaces + "FlagC = (temp8 & 1) != 0;");
                w.WriteLine(Spaces + SetNZ("value8"));
            }
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void RTI(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "P = ReadMemory((ushort)(++S + 0x2100));");
            w.WriteLine(Spaces + "PC = ReadMemory((ushort)(++S + 0x2100));");
            w.WriteLine(Spaces + "PC |= (ushort)(ReadMemory((ushort)(++S + 0x2100)) << 8);");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
            w.WriteLine(Spaces + "goto AfterClearTFlag;");
        }

        private void RTS(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "PC = ReadMemory((ushort)(++S + 0x2100));");
            w.WriteLine(Spaces + "PC |= (ushort)(ReadMemory((ushort)(++S + 0x2100)) << 8);");
            w.WriteLine(Spaces + "PC++;");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void SAX(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "temp8 = A;");
            w.WriteLine(Spaces + "A = X;");
            w.WriteLine(Spaces + "X = temp8;");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void SAY(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "temp8 = A;");
            w.WriteLine(Spaces + "A = Y;");
            w.WriteLine(Spaces + "Y = temp8;");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void SXY(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "temp8 = X;");
            w.WriteLine(Spaces + "X = Y;");
            w.WriteLine(Spaces + "Y = temp8;");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void SBC(OpcodeInfo op, TextWriter w)
        {
            GetValue8(op, w, "value8");
            w.WriteLine(Spaces + "temp = A - value8 - (FlagC ? 0 : 1);");
            w.WriteLine(Spaces + "if ((P & 0x08) != 0) {");
            w.WriteLine(Spaces + "    lo = (A & 0x0F) - (value8 & 0x0F) - (FlagC ? 0 : 1);");
            w.WriteLine(Spaces + "    hi = (A & 0xF0) - (value8 & 0xF0);");
            w.WriteLine(Spaces + "    if ((lo & 0xF0) != 0) lo -= 0x06;");
            w.WriteLine(Spaces + "    if ((lo & 0x80) != 0) hi -= 0x10;");
            w.WriteLine(Spaces + "    if ((hi & 0x0F00) != 0) hi -= 0x60;");
            w.WriteLine(Spaces + "    FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;");
            w.WriteLine(Spaces + "    FlagC = (hi & 0xFF00) == 0;");
            w.WriteLine(Spaces + "    A = (byte) ((lo & 0x0F) | (hi & 0xF0));");
            w.WriteLine(Spaces + "    PendingCycles--;");
            w.WriteLine(Spaces + "} else {");
            w.WriteLine(Spaces + "    FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;");
            w.WriteLine(Spaces + "    FlagC = temp >= 0;");
            w.WriteLine(Spaces + "    A = (byte)temp;");
            w.WriteLine(Spaces + "}");
            w.WriteLine(Spaces + SetNZ("A"));
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void SEC(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "FlagC = true;");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void SED(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "FlagD = true;");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void SEI(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "FlagI = true;");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void SET(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine("                        int a; // TODO remove these extra checks"); // TODO remove these extra checks
            w.WriteLine("                        string b = Disassemble(PC, out a);");
            w.WriteLine("                        if (b.StartsWith(\"ADC\") == false && b.StartsWith(\"EOR\") == false && b.StartsWith(\"AND\") == false && b.StartsWith(\"ORA\") == false)");
            w.WriteLine("                            Console.WriteLine(\"SETTING T FLAG, NEXT INSTRUCTION IS UNHANDLED:  {0}\", b);");
            w.WriteLine(Spaces + "FlagT = true;");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
            w.WriteLine(Spaces + "goto AfterClearTFlag;");
        }

        private void ST0(OpcodeInfo op, TextWriter w)
        {
            GetValue8(op, w, "value8");
            w.WriteLine(Spaces + "WriteVDC(0,value8);");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void ST1(OpcodeInfo op, TextWriter w)
        {
            GetValue8(op, w, "value8");
            w.WriteLine(Spaces + "WriteVDC(2,value8);");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void ST2(OpcodeInfo op, TextWriter w)
        {
            GetValue8(op, w, "value8");
            w.WriteLine(Spaces + "WriteVDC(3,value8);");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void STA(OpcodeInfo op, TextWriter w)
        {
            GetAddress(op, w, "value16");
            w.WriteLine(Spaces + "WriteMemory(value16, A);");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void STX(OpcodeInfo op, TextWriter w)
        {
            GetAddress(op, w, "value16");
            w.WriteLine(Spaces + "WriteMemory(value16, X);");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void STY(OpcodeInfo op, TextWriter w)
        {
            GetAddress(op, w, "value16");
            w.WriteLine(Spaces + "WriteMemory(value16, Y);");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void STZ(OpcodeInfo op, TextWriter w)
        {
            GetAddress(op, w, "value16");
            w.WriteLine(Spaces + "WriteMemory(value16, 0);");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void TAM(OpcodeInfo op, TextWriter w)
        {
            GetValue8(op, w, "value8");
            w.WriteLine(Spaces + "for (byte reg=0; reg<8; reg++)");
            w.WriteLine(Spaces + "{");
            w.WriteLine(Spaces + "    if ((value8 & (1 << reg)) != 0)");
            w.WriteLine(Spaces + "        MPR[reg] = A;");
            w.WriteLine(Spaces + "}");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void TAX(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "X = A;");
            w.WriteLine(Spaces + SetNZ("X"));
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void TAY(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "Y = A;");
            w.WriteLine(Spaces + SetNZ("Y"));
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void TMA(OpcodeInfo op, TextWriter w)
        {
            GetValue8(op, w, "value8");
            w.WriteLine(Spaces + "     if ((value8 & 0x01) != 0) A = MPR[0];");
            w.WriteLine(Spaces + "else if ((value8 & 0x02) != 0) A = MPR[1];");
            w.WriteLine(Spaces + "else if ((value8 & 0x04) != 0) A = MPR[2];");
            w.WriteLine(Spaces + "else if ((value8 & 0x08) != 0) A = MPR[3];");
            w.WriteLine(Spaces + "else if ((value8 & 0x10) != 0) A = MPR[4];");
            w.WriteLine(Spaces + "else if ((value8 & 0x20) != 0) A = MPR[5];");
            w.WriteLine(Spaces + "else if ((value8 & 0x40) != 0) A = MPR[6];");
            w.WriteLine(Spaces + "else if ((value8 & 0x80) != 0) A = MPR[7];");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void TRB(OpcodeInfo op, TextWriter w)
        {
            GetAddress(op, w, "value16");
            w.WriteLine(Spaces + "value8 = ReadMemory(value16);");
            w.WriteLine(Spaces + "WriteMemory(value16, (byte)(value8 & ~A));");
            w.WriteLine(Spaces + "FlagN = (value8 & 0x80) != 0;");
            w.WriteLine(Spaces + "FlagV = (value8 & 0x40) != 0;");
            w.WriteLine(Spaces + "FlagZ = (A & value8) == 0;");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void TSB(OpcodeInfo op, TextWriter w)
        {
            GetAddress(op, w, "value16");
            w.WriteLine(Spaces + "value8 = ReadMemory(value16);");
            w.WriteLine(Spaces + "WriteMemory(value16, (byte)(value8 | A));");
            w.WriteLine(Spaces + "FlagN = (value8 & 0x80) != 0;");
            w.WriteLine(Spaces + "FlagV = (value8 & 0x40) != 0;");
            w.WriteLine(Spaces + "FlagZ = (A | value8) == 0;");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void TST(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "value8 = ReadMemory(PC++);");
            GetValue8(op, w, "temp8");
            w.WriteLine(Spaces + "FlagN = (temp8 & 0x80) != 0;");
            w.WriteLine(Spaces + "FlagV = (temp8 & 0x40) != 0;");
            w.WriteLine(Spaces + "FlagZ = (temp8 & value8) == 0;");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void TSX(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "X = S;");
            w.WriteLine(Spaces + SetNZ("X"));
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void TXA(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "A = X;");
            w.WriteLine(Spaces + SetNZ("A"));
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void TXS(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "S = X;");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void TYA(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "A = Y;");
            w.WriteLine(Spaces + SetNZ("A"));
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void RMB(OpcodeInfo op, TextWriter w, int bit)
        {
            GetAddress(op, w, "value16");
            w.WriteLine(Spaces + "value8 = ReadMemory(value16);");
            w.WriteLine(Spaces + "value8 &= 0x{0:X2};", (byte)(~(1 << bit)));
            w.WriteLine(Spaces + "WriteMemory(value16, value8);");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void SMB(OpcodeInfo op, TextWriter w, int bit)
        {
            GetAddress(op, w, "value16");
            w.WriteLine(Spaces + "value8 = ReadMemory(value16);");
            w.WriteLine(Spaces + "value8 |= 0x{0:X2};",(1<<bit));
            w.WriteLine(Spaces + "WriteMemory(value16, value8);");
            w.WriteLine(Spaces + "PendingCycles -= {0};", op.Cycles);
        }

        private void TAI(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "if (InBlockTransfer == false)");
            w.WriteLine(Spaces + "{");
            w.WriteLine(Spaces + "    InBlockTransfer = true;");
            w.WriteLine(Spaces + "    btFrom = ReadWord(PC); PC += 2;");
            w.WriteLine(Spaces + "    btTo = ReadWord(PC); PC += 2;");
            w.WriteLine(Spaces + "    btLen = ReadWord(PC); PC += 2;");
            w.WriteLine(Spaces + "    btAlternator = 0;");
            w.WriteLine(Spaces + "    PendingCycles -= 14;");
            w.WriteLine(Spaces + "    PC -= 7;");
            w.WriteLine(Spaces + "    break;");
            w.WriteLine(Spaces + "}");
            w.WriteLine();
            w.WriteLine(Spaces + "if (btLen-- != 0)");
            w.WriteLine(Spaces + "{");
            w.WriteLine(Spaces + "    WriteMemory(btTo++, ReadMemory((ushort)(btFrom + btAlternator)));");
            w.WriteLine(Spaces + "    btAlternator ^= 1;");
            w.WriteLine(Spaces + "    PendingCycles -= 6;");
            w.WriteLine(Spaces + "    PC--;");
            w.WriteLine(Spaces + "    break;");
            w.WriteLine(Spaces + "}");
            w.WriteLine();
            w.WriteLine(Spaces + "InBlockTransfer = false;");
            w.WriteLine(Spaces + "PendingCycles -= 3;");
            w.WriteLine(Spaces + "PC += 6;");
        }

        private void TIA(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "if (InBlockTransfer == false)");
            w.WriteLine(Spaces + "{");
            w.WriteLine(Spaces + "    InBlockTransfer = true;");
            w.WriteLine(Spaces + "    btFrom = ReadWord(PC); PC += 2;");
            w.WriteLine(Spaces + "    btTo = ReadWord(PC); PC += 2;");
            w.WriteLine(Spaces + "    btLen = ReadWord(PC); PC += 2;");
            w.WriteLine(Spaces + "    btAlternator = 0;");
            w.WriteLine(Spaces + "    PendingCycles -= 14;");
            w.WriteLine(Spaces + "    PC -= 7;");
            w.WriteLine(Spaces + "    break;");
            w.WriteLine(Spaces + "}");
            w.WriteLine();
            w.WriteLine(Spaces + "if (btLen-- != 0)");
            w.WriteLine(Spaces + "{");
            w.WriteLine(Spaces + "    WriteMemory((ushort)(btTo+btAlternator), ReadMemory(btFrom++));");
            w.WriteLine(Spaces + "    btAlternator ^= 1;");
            w.WriteLine(Spaces + "    PendingCycles -= 6;");
            w.WriteLine(Spaces + "    PC--;");
            w.WriteLine(Spaces + "    break;");
            w.WriteLine(Spaces + "}");
            w.WriteLine();
            w.WriteLine(Spaces + "InBlockTransfer = false;");
            w.WriteLine(Spaces + "PendingCycles -= 3;");
            w.WriteLine(Spaces + "PC += 6;");
        }

        private void TII(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "if (InBlockTransfer == false)");
            w.WriteLine(Spaces + "{");
            w.WriteLine(Spaces + "    InBlockTransfer = true;");
            w.WriteLine(Spaces + "    btFrom = ReadWord(PC); PC += 2;");
            w.WriteLine(Spaces + "    btTo = ReadWord(PC); PC += 2;");
            w.WriteLine(Spaces + "    btLen = ReadWord(PC); PC += 2;");
            w.WriteLine(Spaces + "    PendingCycles -= 14;");
            w.WriteLine(Spaces + "    PC -= 7;");
            w.WriteLine(Spaces + "    break;");
            w.WriteLine(Spaces + "}");
            w.WriteLine();
            w.WriteLine(Spaces + "if (btLen-- != 0)");
            w.WriteLine(Spaces + "{");
            w.WriteLine(Spaces + "    WriteMemory(btTo++, ReadMemory(btFrom++));");
            w.WriteLine(Spaces + "    PendingCycles -= 6;");
            w.WriteLine(Spaces + "    PC--;");
            w.WriteLine(Spaces + "    break;");
            w.WriteLine(Spaces + "}");
            w.WriteLine();
            w.WriteLine(Spaces + "InBlockTransfer = false;");
            w.WriteLine(Spaces + "PendingCycles -= 3;");
            w.WriteLine(Spaces + "PC += 6;");
        }

        private void TIN(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "if (InBlockTransfer == false)");
            w.WriteLine(Spaces + "{");
            w.WriteLine(Spaces + "    InBlockTransfer = true;");
            w.WriteLine(Spaces + "    btFrom = ReadWord(PC); PC += 2;");
            w.WriteLine(Spaces + "    btTo = ReadWord(PC); PC += 2;");
            w.WriteLine(Spaces + "    btLen = ReadWord(PC); PC += 2;");
            w.WriteLine(Spaces + "    PendingCycles -= 14;");
            w.WriteLine(Spaces + "    PC -= 7;");
            w.WriteLine(Spaces + "    break;");
            w.WriteLine(Spaces + "}");
            w.WriteLine();
            w.WriteLine(Spaces + "if (btLen-- != 0)");
            w.WriteLine(Spaces + "{");
            w.WriteLine(Spaces + "    WriteMemory(btTo, ReadMemory(btFrom++));");
            w.WriteLine(Spaces + "    PendingCycles -= 6;");
            w.WriteLine(Spaces + "    PC--;");
            w.WriteLine(Spaces + "    break;");
            w.WriteLine(Spaces + "}");
            w.WriteLine();
            w.WriteLine(Spaces + "InBlockTransfer = false;");
            w.WriteLine(Spaces + "PendingCycles -= 3;");
            w.WriteLine(Spaces + "PC += 6;");
        }

        private void TDD(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "if (InBlockTransfer == false)");
            w.WriteLine(Spaces + "{");
            w.WriteLine(Spaces + "    InBlockTransfer = true;");
            w.WriteLine(Spaces + "    btFrom = ReadWord(PC); PC += 2;");
            w.WriteLine(Spaces + "    btTo = ReadWord(PC); PC += 2;");
            w.WriteLine(Spaces + "    btLen = ReadWord(PC); PC += 2;");
            w.WriteLine(Spaces + "    PendingCycles -= 14;");
            w.WriteLine(Spaces + "    PC -= 7;");
            w.WriteLine(Spaces + "    break;");
            w.WriteLine(Spaces + "}");
            w.WriteLine();
            w.WriteLine(Spaces + "if (btLen-- != 0)");
            w.WriteLine(Spaces + "{");
            w.WriteLine(Spaces + "    WriteMemory(btTo--, ReadMemory(btFrom--));");
            w.WriteLine(Spaces + "    PendingCycles -= 6;");
            w.WriteLine(Spaces + "    PC--;");
            w.WriteLine(Spaces + "    break;");
            w.WriteLine(Spaces + "}");
            w.WriteLine();
            w.WriteLine(Spaces + "InBlockTransfer = false;");
            w.WriteLine(Spaces + "PendingCycles -= 3;");
            w.WriteLine(Spaces + "PC += 6;");
        }
    }
}