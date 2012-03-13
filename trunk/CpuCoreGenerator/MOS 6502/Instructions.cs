using System.IO;

namespace M6502
{
    public partial class CoreGenerator
    {
        private string SetNZ(string val)
        {
            return "P = (byte)((P & 0x7D) | TableNZ[" + val + "]);";    // NES version
            //return "P = (byte)((P & 0x5D) | TableNZ[" + val + "]);";    // PCE version
        }

        private void ADC(OpcodeInfo op, TextWriter w)
        {
            GetValue8(op, w, "value8");
            w.WriteLine(Spaces + "temp = value8 + A + (FlagC ? 1 : 0);");
            w.WriteLine(Spaces + "FlagV = (~(A ^ value8) & (A ^ temp) & 0x80) != 0;");
            w.WriteLine(Spaces + "FlagC = temp > 0xFF;");
            w.WriteLine(Spaces + "A = (byte)temp;");
            w.WriteLine(Spaces + SetNZ("A"));
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void AND(OpcodeInfo op, TextWriter w)
        {
            GetValue8(op, w, "value8");
            w.WriteLine(Spaces + "A &= value8;");
            w.WriteLine(Spaces + SetNZ("A"));
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void ASL(OpcodeInfo op, TextWriter w)
        {
            if (op.AddressMode == AddrMode.Accumulator)
            {
                w.WriteLine(Spaces + "FlagC = (A & 0x80) != 0;");
                w.WriteLine(Spaces + "A = (byte) (A << 1);");
                w.WriteLine(Spaces + SetNZ("A"));
            } else {
                GetAddress(op, w, "value16");
                w.WriteLine(Spaces + "value8 = ReadMemory(value16);");
                w.WriteLine(Spaces + "FlagC = (value8 & 0x80) != 0;");
                w.WriteLine(Spaces + "value8 = (byte)(value8 << 1);");
                w.WriteLine(Spaces + "WriteMemory(value16, value8);");
                w.WriteLine(Spaces + SetNZ("value8"));
            }
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void Branch(OpcodeInfo op, TextWriter w, string flag, bool cond)
        {
            GetAddress(op, w, "value16");
            w.WriteLine(Spaces + "if (Flag"+flag+" == "+cond.ToString().ToLower()+") {");
            w.WriteLine(Spaces + "    PendingCycles--; TotalExecutedCycles++;");
            w.WriteLine(Spaces + "    if ((PC & 0xFF00) != (value16 & 0xFF00)) ");
            w.WriteLine(Spaces + "        { PendingCycles--; TotalExecutedCycles++; }");
            w.WriteLine(Spaces + "    PC = value16;");
            w.WriteLine(Spaces + "}"); 
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void BIT(OpcodeInfo op, TextWriter w)
        {
            GetValue8(op, w, "value8");
            w.WriteLine(Spaces + "FlagN = (value8 & 0x80) != 0;");
            w.WriteLine(Spaces + "FlagV = (value8 & 0x40) != 0;");
            w.WriteLine(Spaces + "FlagZ = (A & value8) == 0;");
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void CLC(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "FlagC = false;");
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void CLD(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "FlagD = false;");
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void CLI(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "//FlagI = false;");
            w.WriteLine(Spaces + "CLI_Pending = true;");
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void CLV(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "FlagV = false;");
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void CMP_reg(OpcodeInfo op, TextWriter w, string reg)
        {
            GetValue8(op, w, "value8");
            w.WriteLine(Spaces + "value16 = (ushort) (" + reg + " - value8);");
            w.WriteLine(Spaces + "FlagC = (" + reg + " >= value8);");
            w.WriteLine(Spaces + SetNZ("(byte)value16"));
            w.WriteLine(Spaces + "PendingCycles -= {0};  TotalExecutedCycles += {0};", op.Cycles);
        }

        private void DEC(OpcodeInfo op, TextWriter w)
        {
            GetAddress(op, w, "value16");
            w.WriteLine(Spaces + "value8 = (byte)(ReadMemory(value16) - 1);");
            w.WriteLine(Spaces + "WriteMemory(value16, value8);");
            w.WriteLine(Spaces + SetNZ("value8"));
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void DEX(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + SetNZ("--X"));
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void DEY(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + SetNZ("--Y"));
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void EOR(OpcodeInfo op, TextWriter w)
        {
            GetValue8(op, w, "value8");
            w.WriteLine(Spaces + "A ^= value8;");
            w.WriteLine(Spaces + SetNZ("A"));
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void INC(OpcodeInfo op, TextWriter w)
        {
            GetAddress(op, w, "value16");
            w.WriteLine(Spaces + "value8 = (byte)(ReadMemory(value16) + 1);");
            w.WriteLine(Spaces + "WriteMemory(value16, value8);");
            w.WriteLine(Spaces + SetNZ("value8"));
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void INX(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + SetNZ("++X"));
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void INY(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + SetNZ("++Y"));
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void JMP(OpcodeInfo op, TextWriter w)
        {
            switch (op.AddressMode)
            {
                case AddrMode.Absolute: w.WriteLine(Spaces+"PC = ReadWord(PC);"); break;
                case AddrMode.Indirect: w.WriteLine(Spaces+"PC = ReadWordPageWrap(ReadWord(PC));"); break;
            }
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void JSR(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "temp16 = (ushort)(PC+1);");
            w.WriteLine(Spaces + "WriteMemory((ushort)(S-- + 0x100), (byte)(temp16 >> 8));");
            w.WriteLine(Spaces + "WriteMemory((ushort)(S-- + 0x100), (byte)temp16);");
            w.WriteLine(Spaces + "PC = ReadWord(PC);");
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void LDA(OpcodeInfo op, TextWriter w)
        {
            GetValue8(op, w, "A");
            w.WriteLine(Spaces + SetNZ("A"));
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void LDX(OpcodeInfo op, TextWriter w)
        {
            GetValue8(op, w, "X");
            w.WriteLine(Spaces + SetNZ("X"));
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void LDY(OpcodeInfo op, TextWriter w)
        {
            GetValue8(op, w, "Y");
            w.WriteLine(Spaces + SetNZ("Y"));
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void LSR(OpcodeInfo op, TextWriter w)
        {
            if (op.AddressMode == AddrMode.Accumulator)
            {
                w.WriteLine(Spaces + "FlagC = (A & 1) != 0;");
                w.WriteLine(Spaces + "A = (byte) (A >> 1);");
                w.WriteLine(Spaces + SetNZ("A"));
            } else {
                GetAddress(op, w, "value16");
                w.WriteLine(Spaces + "value8 = ReadMemory(value16);");
                w.WriteLine(Spaces + "FlagC = (value8 & 1) != 0;");
                w.WriteLine(Spaces + "value8 = (byte)(value8 >> 1);");
                w.WriteLine(Spaces + "WriteMemory(value16, value8);");
                w.WriteLine(Spaces + SetNZ("value8"));
            }
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void NOP(OpcodeInfo op, TextWriter w)
        {
            // This code is quite insufficient, but at least, we have to increment program counter appropriately.
            // For immediate addressing mode, it will be correct, and it will fix desyncs of "Puzznic (J)" and "Puzznic (U)".
            // For other addressing modes, I don't know whether they access memory, so further investigation will be needed.
            if (op.Size > 1)
                w.WriteLine(Spaces+"PC += {0};", op.Size-1);
            w.WriteLine(Spaces+"PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void ORA(OpcodeInfo op, TextWriter w)
        {
            GetValue8(op, w, "value8");
            w.WriteLine(Spaces + "A |= value8;");
            w.WriteLine(Spaces + SetNZ("A"));
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void PHA(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "WriteMemory((ushort)(S-- + 0x100), A);");
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void PHP(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "FlagB = true; //why would it do this?? how weird");
            w.WriteLine(Spaces + "WriteMemory((ushort)(S-- + 0x100), P);");
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void PLA(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "A = ReadMemory((ushort)(++S + 0x100));");
            w.WriteLine(Spaces + SetNZ("A"));
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void PLP(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "//handle I flag differently. sort of a sloppy way to do the job, but it does finish it off.");
            w.WriteLine(Spaces + "value8 = ReadMemory((ushort)(++S + 0x100));");
            w.WriteLine(Spaces + "if ((value8 & 0x04) != 0 && !FlagI)");
            w.WriteLine(Spaces + "\tSEI_Pending = true;");
            w.WriteLine(Spaces + "if ((value8 & 0x04) == 0 && FlagI)");
            w.WriteLine(Spaces + "\tCLI_Pending = true;");
            w.WriteLine(Spaces + "value8 &= unchecked((byte)~0x04);");
            w.WriteLine(Spaces + "P &= 0x04;");
            w.WriteLine(Spaces + "P |= value8;");
w.WriteLine("FlagT = true;//this seems wrong");//this seems wrong
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
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
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
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
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void RTI(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "P = ReadMemory((ushort)(++S + 0x100));");
w.WriteLine("FlagT = true;// this seems wrong");//this seems wrong
            w.WriteLine(Spaces + "PC = ReadMemory((ushort)(++S + 0x100));");
            w.WriteLine(Spaces + "PC |= (ushort)(ReadMemory((ushort)(++S + 0x100)) << 8);");
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void RTS(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "PC = ReadMemory((ushort)(++S + 0x100));");
            w.WriteLine(Spaces + "PC |= (ushort)(ReadMemory((ushort)(++S + 0x100)) << 8);");
            w.WriteLine(Spaces + "PC++;");
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void SBC(OpcodeInfo op, TextWriter w)
        {
            GetValue8(op, w, "value8");
            w.WriteLine(Spaces + "temp = A - value8 - (FlagC?0:1);");
            w.WriteLine(Spaces + "FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;");
            w.WriteLine(Spaces + "FlagC = temp >= 0;");
            w.WriteLine(Spaces + "A = (byte)temp;");
            w.WriteLine(Spaces + SetNZ("A"));
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void SEC(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "FlagC = true;");
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void SED(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "FlagD = true;");
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void SEI(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "//FlagI = true;");
            w.WriteLine(Spaces + "SEI_Pending = true;");
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void STA(OpcodeInfo op, TextWriter w)
        {
            GetAddress(op, w, "value16");
            w.WriteLine(Spaces + "WriteMemory(value16, A);");
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void STX(OpcodeInfo op, TextWriter w)
        {
            GetAddress(op, w, "value16");
            w.WriteLine(Spaces + "WriteMemory(value16, X);");
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void STY(OpcodeInfo op, TextWriter w)
        {
            GetAddress(op, w, "value16");
            w.WriteLine(Spaces + "WriteMemory(value16, Y);");
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void TAX(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "X = A;");
            w.WriteLine(Spaces + SetNZ("X"));
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void TAY(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "Y = A;");
            w.WriteLine(Spaces + SetNZ("Y"));
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void TSX(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "X = S;");
            w.WriteLine(Spaces + SetNZ("X"));
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void TXA(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "A = X;");
            w.WriteLine(Spaces + SetNZ("A"));
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void TXS(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "S = X;");
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }

        private void TYA(OpcodeInfo op, TextWriter w)
        {
            w.WriteLine(Spaces + "A = Y;");
            w.WriteLine(Spaces + SetNZ("A"));
            w.WriteLine(Spaces + "PendingCycles -= {0}; TotalExecutedCycles += {0};", op.Cycles);
        }
    }
}
