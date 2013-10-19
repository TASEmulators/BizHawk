using System;
using System.IO;

namespace M6502
{
    public enum AddrMode
    {
        Implicit,
        Accumulator,
        Immediate,
        ZeroPage,
        ZeroPageX,
        ZeroPageY,
        Absolute,
        AbsoluteX,
        AbsoluteX_P, //* page-crossing penalty
        AbsoluteY,
        AbsoluteY_P, //* page-crossing penalty
        Indirect,
        IndirectX,
        IndirectY,
        IndirectY_P, //* page-crossing penalty
        Relative
    }

    public class OpcodeInfo
    {
        public string Instruction;
        public AddrMode AddressMode;
        public int Cycles;

        public int Size
        {
            get
            {
                switch (AddressMode)
                {
                    case AddrMode.Implicit:    return 1;
                    case AddrMode.Accumulator: return 1;
                    case AddrMode.Immediate:   return 2;
                    case AddrMode.ZeroPage:    return 2;
                    case AddrMode.ZeroPageX:   return 2;
                    case AddrMode.ZeroPageY:   return 2;
                    case AddrMode.Absolute:    return 3;
                    case AddrMode.AbsoluteX:   return 3;
                    case AddrMode.AbsoluteX_P: return 3;
                    case AddrMode.AbsoluteY:   return 3;
                    case AddrMode.AbsoluteY_P: return 3;
                    case AddrMode.Indirect:    return 3;
                    case AddrMode.IndirectX:   return 2;
                    case AddrMode.IndirectY:   return 2;
                    case AddrMode.IndirectY_P: return 2;
                    case AddrMode.Relative:    return 2;
                    default:
                        return -1;    
                }
            }
        }

        public override string ToString()
        {
            switch (AddressMode)
            {
                case AddrMode.Implicit:     return Instruction;
                case AddrMode.Accumulator:  return Instruction+" A";
                case AddrMode.Immediate:    return Instruction+" #nn";
                case AddrMode.ZeroPage:     return Instruction+" zp";
                case AddrMode.ZeroPageX:    return Instruction + " zp,X";
                case AddrMode.ZeroPageY:    return Instruction + " zp,Y";
                case AddrMode.Absolute:     return Instruction + " addr";
                case AddrMode.AbsoluteX:    return Instruction + " addr,X";
                case AddrMode.AbsoluteX_P:  return Instruction + " addr,X*";
                case AddrMode.AbsoluteY:    return Instruction + " addr,Y";
                case AddrMode.AbsoluteY_P:  return Instruction + " addr,Y*";
                case AddrMode.Indirect:     return Instruction + " (addr)";
                case AddrMode.IndirectX:    return Instruction + " (addr,X)";
                case AddrMode.IndirectY:    return Instruction + " (addr),Y";
                case AddrMode.IndirectY_P:  return Instruction + " (addr),Y*";
                case AddrMode.Relative:     return Instruction + " +/-rel";
                default: return Instruction;
            }
        }
    }

    public partial class CoreGenerator
    {
        public OpcodeInfo[] Opcodes = new OpcodeInfo[256];
        
        // NOTE: page is 256 bytes.

        public void InitOpcodeTable()
        {
            // Add with Carry
            Set(0x69, "ADC", AddrMode.Immediate, 2);
            Set(0x65, "ADC", AddrMode.ZeroPage , 3);
            Set(0x75, "ADC", AddrMode.ZeroPageX, 4);
            Set(0x6D, "ADC", AddrMode.Absolute , 4);
            Set(0x7D, "ADC", AddrMode.AbsoluteX_P, 4);
            Set(0x79, "ADC", AddrMode.AbsoluteY_P, 4);
            Set(0x61, "ADC", AddrMode.IndirectX, 6);
            Set(0x71, "ADC", AddrMode.IndirectY_P, 5);

            // AND
            Set(0x29, "AND", AddrMode.Immediate, 2);
            Set(0x25, "AND", AddrMode.ZeroPage , 3);
            Set(0x35, "AND", AddrMode.ZeroPageX, 4);
            Set(0x2D, "AND", AddrMode.Absolute , 4);
            Set(0x3D, "AND", AddrMode.AbsoluteX_P, 4);
            Set(0x39, "AND", AddrMode.AbsoluteY_P, 4);
            Set(0x21, "AND", AddrMode.IndirectX, 6);
            Set(0x31, "AND", AddrMode.IndirectY_P, 5);

            // Arithmatic Shift Left
            Set(0x0A, "ASL", AddrMode.Accumulator, 2);
            Set(0x06, "ASL", AddrMode.ZeroPage , 5);
            Set(0x16, "ASL", AddrMode.ZeroPageX, 6);
            Set(0x0E, "ASL", AddrMode.Absolute , 6);
            Set(0x1E, "ASL", AddrMode.AbsoluteX, 7);

            // BIT
            Set(0x24, "BIT", AddrMode.ZeroPage, 3);
            Set(0x2C, "BIT", AddrMode.Absolute, 4);

            // Branch instructions
            Set(0x10, "BPL", AddrMode.Relative, 2); // Branch on Plus
            Set(0x30, "BMI", AddrMode.Relative, 2); // Branch on Minus
            Set(0x50, "BVC", AddrMode.Relative, 2); // Branch on Overflow Clear
            Set(0x70, "BVS", AddrMode.Relative, 2); // Branch on Overflow Set
            Set(0x90, "BCC", AddrMode.Relative, 2); // Branch on Carry Clear
            Set(0xB0, "BCS", AddrMode.Relative, 2); // Branch on Carry Set
            Set(0xD0, "BNE", AddrMode.Relative, 2); // Branch on Not Equal
            Set(0xF0, "BEQ", AddrMode.Relative, 2); // Branch on Equal

            // CPU Break
            Set(0x00, "BRK", AddrMode.Implicit, 7);

            // Compare accumulator
            Set(0xC9, "CMP", AddrMode.Immediate, 2);
            Set(0xC5, "CMP", AddrMode.ZeroPage , 3);
            Set(0xD5, "CMP", AddrMode.ZeroPageX, 4);
            Set(0xCD, "CMP", AddrMode.Absolute , 4);
            Set(0xDD, "CMP", AddrMode.AbsoluteX_P, 4);
            Set(0xD9, "CMP", AddrMode.AbsoluteY_P, 4);
            Set(0xC1, "CMP", AddrMode.IndirectX, 6);
            Set(0xD1, "CMP", AddrMode.IndirectY_P, 5);

            // Compare X register
            Set(0xE0, "CPX", AddrMode.Immediate, 2);
            Set(0xE4, "CPX", AddrMode.ZeroPage , 3);
            Set(0xEC, "CPX", AddrMode.Absolute , 4);

            // Compare Y register
            Set(0xC0, "CPY", AddrMode.Immediate, 2);
            Set(0xC4, "CPY", AddrMode.ZeroPage , 3);
            Set(0xCC, "CPY", AddrMode.Absolute , 4);

            // DEC
            Set(0xC6, "DEC", AddrMode.ZeroPage , 5);
            Set(0xD6, "DEC", AddrMode.ZeroPageX, 6);
            Set(0xCE, "DEC", AddrMode.Absolute , 6);
            Set(0xDE, "DEC", AddrMode.AbsoluteX, 7);

            // Exclusive OR
            Set(0x49, "EOR", AddrMode.Immediate, 2);
            Set(0x45, "EOR", AddrMode.ZeroPage , 3);
            Set(0x55, "EOR", AddrMode.ZeroPageX, 4);
            Set(0x4D, "EOR", AddrMode.Absolute , 4);
            Set(0x5D, "EOR", AddrMode.AbsoluteX_P, 4);
            Set(0x59, "EOR", AddrMode.AbsoluteY_P, 4);
            Set(0x41, "EOR", AddrMode.IndirectX, 6);
            Set(0x51, "EOR", AddrMode.IndirectY_P, 5);
            
            // Flag Instructions
            Set(0x18, "CLC", AddrMode.Implicit, 2); // Clear Carry
            Set(0x38, "SEC", AddrMode.Implicit, 2); // Set Carry
            Set(0x58, "CLI", AddrMode.Implicit, 2); // Clear Interrupt
            Set(0x78, "SEI", AddrMode.Implicit, 2); // Set Interrupt
            Set(0xB8, "CLV", AddrMode.Implicit, 2); // Clear Overflow
            Set(0xD8, "CLD", AddrMode.Implicit, 2); // Clear Decimal
            Set(0xF8, "SED", AddrMode.Implicit, 2); // Set Decimal

            // INC
            Set(0xE6, "INC", AddrMode.ZeroPage , 5);
            Set(0xF6, "INC", AddrMode.ZeroPageX, 6);
            Set(0xEE, "INC", AddrMode.Absolute , 6);
            Set(0xFE, "INC", AddrMode.AbsoluteX, 7);

            // Jump
            Set(0x4C, "JMP", AddrMode.Absolute, 3);
            Set(0x6C, "JMP", AddrMode.Indirect, 5);

            // Jump to Subroutine
            Set(0x20, "JSR", AddrMode.Absolute, 6);

            // Load Accumulator
            Set(0xA9, "LDA", AddrMode.Immediate, 2);
            Set(0xA5, "LDA", AddrMode.ZeroPage , 3);
            Set(0xB5, "LDA", AddrMode.ZeroPageX, 4);
            Set(0xAD, "LDA", AddrMode.Absolute , 4);
            Set(0xBD, "LDA", AddrMode.AbsoluteX_P, 4);
            Set(0xB9, "LDA", AddrMode.AbsoluteY_P, 4);
            Set(0xA1, "LDA", AddrMode.IndirectX, 6);
            Set(0xB1, "LDA", AddrMode.IndirectY_P, 5);

            // Load X register
            Set(0xA2, "LDX", AddrMode.Immediate, 2);
            Set(0xA6, "LDX", AddrMode.ZeroPage , 3);
            Set(0xB6, "LDX", AddrMode.ZeroPageY, 4);
            Set(0xAE, "LDX", AddrMode.Absolute , 4);
            Set(0xBE, "LDX", AddrMode.AbsoluteY_P, 4);

            // Load Y register
            Set(0xA0, "LDY", AddrMode.Immediate, 2);
            Set(0xA4, "LDY", AddrMode.ZeroPage , 3);
            Set(0xB4, "LDY", AddrMode.ZeroPageX, 4);
            Set(0xAC, "LDY", AddrMode.Absolute , 4);
            Set(0xBC, "LDY", AddrMode.AbsoluteX_P, 4);

            // Logical Shift Right
            Set(0x4A, "LSR", AddrMode.Accumulator, 2);
            Set(0x46, "LSR", AddrMode.ZeroPage , 5);
            Set(0x56, "LSR", AddrMode.ZeroPageX, 6);
            Set(0x4E, "LSR", AddrMode.Absolute , 6);
            Set(0x5E, "LSR", AddrMode.AbsoluteX, 7);

            // No Operation
            Set(0xEA, "NOP", AddrMode.Implicit, 2);
			
			// Illegal NOPs
			Set(0x1A, "NOP", AddrMode.Implicit, 2);
			Set(0x3A, "NOP", AddrMode.Implicit, 2);
			Set(0x5A, "NOP", AddrMode.Implicit, 2);
			Set(0x7A, "NOP", AddrMode.Implicit, 2);
			Set(0xDA, "NOP", AddrMode.Implicit, 2);
			Set(0xFA, "NOP", AddrMode.Implicit, 2);
			Set(0x80, "NOP", AddrMode.Immediate, 2);
			Set(0x82, "NOP", AddrMode.Immediate, 2);
			Set(0x89, "NOP", AddrMode.Immediate, 2);
			Set(0xC2, "NOP", AddrMode.Immediate, 2);
			Set(0xE2, "NOP", AddrMode.Immediate, 2);
			Set(0x04, "NOP", AddrMode.ZeroPage, 3); 
			Set(0x44, "NOP", AddrMode.ZeroPage, 3);
			Set(0x64, "NOP", AddrMode.ZeroPage, 3);
			Set(0x14, "NOP", AddrMode.ZeroPageX, 4);
			Set(0x34, "NOP", AddrMode.ZeroPageX, 4);
			Set(0x54, "NOP", AddrMode.ZeroPageX, 4);
			Set(0x74, "NOP", AddrMode.ZeroPageX, 4);
			Set(0xD4, "NOP", AddrMode.ZeroPageX, 4);
			Set(0xF4, "NOP", AddrMode.ZeroPageX, 4);
			//do the following issue a read or write? if so, we need to emulate with another instruction
			Set(0x0C, "NOP", AddrMode.Indirect, 4);
			Set(0x1C, "NOP", AddrMode.IndirectX, 4);
			Set(0x3C, "NOP", AddrMode.IndirectX, 4);
			Set(0x5C, "NOP", AddrMode.IndirectX, 4);
			Set(0x7C, "NOP", AddrMode.IndirectX, 4);
			Set(0xDC, "NOP", AddrMode.IndirectX, 4);
			Set(0xFC, "NOP", AddrMode.IndirectX, 4);

			//undocumented opcodes
			//RLA:
			//Set(0x23, "RLA", AddrMode.IndirectX, 8);

            // Bitwise OR with Accumulator
            Set(0x09, "ORA", AddrMode.Immediate, 2);
            Set(0x05, "ORA", AddrMode.ZeroPage , 3);
            Set(0x15, "ORA", AddrMode.ZeroPageX, 4);
            Set(0x0D, "ORA", AddrMode.Absolute , 4);
            Set(0x1D, "ORA", AddrMode.AbsoluteX_P, 4);
            Set(0x19, "ORA", AddrMode.AbsoluteY_P, 4);
            Set(0x01, "ORA", AddrMode.IndirectX, 6);
            Set(0x11, "ORA", AddrMode.IndirectY_P, 5);

            // Register instructions
            Set(0xAA, "TAX", AddrMode.Implicit, 2); // Transfer A to X
            Set(0x8A, "TXA", AddrMode.Implicit, 2); // Transfer X to A
            Set(0xCA, "DEX", AddrMode.Implicit, 2); // DEC X
            Set(0xE8, "INX", AddrMode.Implicit, 2); // INC X
            Set(0xA8, "TAY", AddrMode.Implicit, 2); // Transfer A to Y
            Set(0x98, "TYA", AddrMode.Implicit, 2); // Transfer Y to A
            Set(0x88, "DEY", AddrMode.Implicit, 2); // DEC Y
            Set(0xC8, "INY", AddrMode.Implicit, 2); // INC Y

            // Rotate Left
            Set(0x2A, "ROL", AddrMode.Accumulator, 2);
            Set(0x26, "ROL", AddrMode.ZeroPage , 5);
            Set(0x36, "ROL", AddrMode.ZeroPageX, 6);
            Set(0x2E, "ROL", AddrMode.Absolute , 6);
            Set(0x3E, "ROL", AddrMode.AbsoluteX, 7);

            // Rotate Right
            Set(0x6A, "ROR", AddrMode.Accumulator, 2);
            Set(0x66, "ROR", AddrMode.ZeroPage , 5);
            Set(0x76, "ROR", AddrMode.ZeroPageX, 6);
            Set(0x6E, "ROR", AddrMode.Absolute , 6);
            Set(0x7E, "ROR", AddrMode.AbsoluteX, 7);

            // Return from Interrupt
            Set(0x40, "RTI", AddrMode.Implicit, 6);

            // Return from Subroutine
            Set(0x60, "RTS", AddrMode.Implicit, 6);

            // Subtract with Carry
            Set(0xE9, "SBC", AddrMode.Immediate, 2);
            Set(0xE5, "SBC", AddrMode.ZeroPage , 3);
            Set(0xF5, "SBC", AddrMode.ZeroPageX, 4);
            Set(0xED, "SBC", AddrMode.Absolute , 4);
            Set(0xFD, "SBC", AddrMode.AbsoluteX_P, 4);
            Set(0xF9, "SBC", AddrMode.AbsoluteY_P, 4);
            Set(0xE1, "SBC", AddrMode.IndirectX, 6);
            Set(0xF1, "SBC", AddrMode.IndirectY_P, 5);

            // Store Accumulator
            Set(0x85, "STA", AddrMode.ZeroPage , 3);
            Set(0x95, "STA", AddrMode.ZeroPageX, 4);
            Set(0x8D, "STA", AddrMode.Absolute , 4);
            Set(0x9D, "STA", AddrMode.AbsoluteX, 5);
            Set(0x99, "STA", AddrMode.AbsoluteY, 5);
            Set(0x81, "STA", AddrMode.IndirectX, 6);
            Set(0x91, "STA", AddrMode.IndirectY, 6);

            // Stack instructions
            Set(0x9A, "TXS", AddrMode.Implicit, 2); // Transfer X to Stack
            Set(0xBA, "TSX", AddrMode.Implicit, 2); // Transfer Stack to X
            Set(0x48, "PHA", AddrMode.Implicit, 3); // Push A
            Set(0x68, "PLA", AddrMode.Implicit, 4); // Pull A
            Set(0x08, "PHP", AddrMode.Implicit, 3); // Push P
            Set(0x28, "PLP", AddrMode.Implicit, 4); // Pull P
            
            // Store X register
            Set(0x86, "STX", AddrMode.ZeroPage , 3);
            Set(0x96, "STX", AddrMode.ZeroPageY, 4);
            Set(0x8E, "STX", AddrMode.Absolute , 4);

            // Store Y register
            Set(0x84, "STY", AddrMode.ZeroPage , 3);
            Set(0x94, "STY", AddrMode.ZeroPageX, 4);
            Set(0x8C, "STY", AddrMode.Absolute , 4);
        }

        private void Set(byte value, string instr, AddrMode addressMode, int cycles)
        {
            var op = new OpcodeInfo();
            op.Instruction = instr;
            op.AddressMode = addressMode;
            op.Cycles = cycles;
            if (Opcodes[value] != null)
                throw new Exception("opcode "+value+" already assigned");
            Opcodes[value] = op;
        }

        public void GenerateExecutor(string file)
        {
            var w = new StreamWriter(file, false);
            w.WriteLine("using System;");
            w.WriteLine();
            w.WriteLine("// Do not modify this file directly! This is GENERATED code.");
            w.WriteLine("// Please open the CpuCoreGenerator solution and make your modifications there.");
            w.WriteLine();
            w.WriteLine("namespace BizHawk.Emulation.CPUs.M6502");
            w.WriteLine("{");
            w.WriteLine("    public partial class MOS6502");
            w.WriteLine("    {");
            w.WriteLine("        public void Execute(int cycles)");
            w.WriteLine("        {");
            w.WriteLine("            sbyte rel8;");
            w.WriteLine("            byte value8, temp8;");
            w.WriteLine("            ushort value16, temp16;");
            w.WriteLine("            int temp;");
            w.WriteLine();
            w.WriteLine("            PendingCycles += cycles;");
            w.WriteLine("            while (PendingCycles > 0)");
            w.WriteLine("            {");
            w.WriteLine("                if (NMI)");
            w.WriteLine("                {");
            w.WriteLine("                    TriggerException(ExceptionType.NMI);");
            w.WriteLine("                    NMI = false;");
            w.WriteLine("                }");
            w.WriteLine("                if (IRQ && !FlagI)");
            w.WriteLine("                {");
            w.WriteLine("                    if (SEI_Pending)");
            w.WriteLine("                        FlagI = true;");
            w.WriteLine("                    TriggerException(ExceptionType.IRQ);");
            w.WriteLine("                }");
            w.WriteLine("                if (CLI_Pending)");
            w.WriteLine("                {");
            w.WriteLine("                    FlagI = false;");
            w.WriteLine("                    CLI_Pending = false;");
            w.WriteLine("                }");
            w.WriteLine("                if (SEI_Pending)");
            w.WriteLine("                {");
            w.WriteLine("                    FlagI = true;");
            w.WriteLine("                    SEI_Pending = false;");
            w.WriteLine("                }");

            w.WriteLine("                if(debug) Console.WriteLine(State());");
            w.WriteLine("");

            w.WriteLine("                ushort this_pc = PC;");
            w.WriteLine("                byte opcode = ReadMemory(PC++);");
            w.WriteLine("                switch (opcode)");
            w.WriteLine("                {");

            for (int i = 0; i < 256; i++)
            {
                if (Opcodes[i] != null)
                {
                    EmulateOpcode(w, i);
                }
            }

            w.WriteLine("                   default:");
            w.WriteLine("                       if(throw_unhandled)");
            w.WriteLine("                           throw new Exception(String.Format(\"Unhandled opcode: {0:X2}\", opcode));");
            w.WriteLine("                      break;");
            w.WriteLine("                }");
            w.WriteLine("            }");
            w.WriteLine("        }");
            w.WriteLine("    }");
            w.WriteLine("}");
            w.Close();
        }

        const string Spaces = "                        ";

        private void EmulateOpcode(TextWriter w, int opcode)
        {
            var op = Opcodes[opcode];
            w.WriteLine("                    case 0x{0:X2}: // {1}", opcode, op);
            switch (op.Instruction)
            {
                case "ADC": ADC(op, w); break;
                case "AND": AND(op, w); break;
                case "ASL": ASL(op, w); break;
                case "BCC": Branch(op, w, "C", false); break;
                case "BCS": Branch(op, w, "C", true); break;
                case "BEQ": Branch(op, w, "Z", true); break;
                case "BIT": BIT(op, w);  break;
                case "BMI": Branch(op, w, "N", true); break;
                case "BNE": Branch(op, w, "Z", false); break;
                case "BPL": Branch(op, w, "N", false); break;
                case "BRK": w.WriteLine(Spaces + "TriggerException(ExceptionType.BRK);"); break;
                case "BVC": Branch(op, w, "V", false); break;
                case "BVS": Branch(op, w, "V", true); break;
                case "CLC": CLC(op, w); break;
                case "CLD": CLD(op, w); break;
                case "CLI": CLI(op, w); break;
                case "CLV": CLV(op, w); break;
                case "CMP": CMP_reg(op, w, "A"); break;
                case "CPX": CMP_reg(op, w, "X"); break;
                case "CPY": CMP_reg(op, w, "Y"); break;
                case "DEC": DEC(op, w); break;
                case "DEX": DEX(op, w); break;
                case "DEY": DEY(op, w); break;
                case "EOR": EOR(op, w); break;
                case "INC": INC(op, w); break;
                case "INX": INX(op, w); break;
                case "INY": INY(op, w); break;
                case "JMP": JMP(op, w); break;
                case "JSR": JSR(op, w); break;
                case "LDA": LDA(op, w); break;
                case "LDX": LDX(op, w); break;
                case "LDY": LDY(op, w); break;
                case "LSR": LSR(op, w); break;
                case "NOP": NOP(op, w); break;
                case "ORA": ORA(op, w); break;
                case "PHA": PHA(op, w); break;
                case "PHP": PHP(op, w); break;
                case "PLA": PLA(op, w); break;
                case "PLP": PLP(op, w); break;
                case "ROL": ROL(op, w); break;
                case "ROR": ROR(op, w); break;
                case "RTI": RTI(op, w); break;
                case "RTS": RTS(op, w); break;
                case "SBC": SBC(op, w); break;
                case "SEC": SEC(op, w); break;
                case "SED": SED(op, w); break;
                case "SEI": SEI(op, w); break;
                case "STA": STA(op, w); break;
                case "STX": STX(op, w); break;
                case "STY": STY(op, w); break;
                case "TAX": TAX(op, w); break;
                case "TAY": TAY(op, w); break;
                case "TSX": TSX(op, w); break;
                case "TXA": TXA(op, w); break;
                case "TYA": TYA(op, w); break;
                case "TXS": TXS(op, w); break;
            }
            w.WriteLine(Spaces+"break;");
        }

        private void GetValue8(OpcodeInfo op, TextWriter w, string dest)
        {
            switch (op.AddressMode)
            {
                case AddrMode.Immediate:
                    w.WriteLine(Spaces + dest + " = ReadMemory(PC++);"); break;
                case AddrMode.ZeroPage:
                    w.WriteLine(Spaces + dest + " = ReadMemory(ReadMemory(PC++));"); break;
                case AddrMode.ZeroPageX:
                    w.WriteLine(Spaces + dest + " = ReadMemory((byte)(ReadMemory(PC++)+X));"); break;
                case AddrMode.ZeroPageY:
                    w.WriteLine(Spaces + dest + " = ReadMemory((byte)(ReadMemory(PC++)+Y));"); break;
                case AddrMode.Absolute:
                    w.WriteLine(Spaces + dest + " = ReadMemory(ReadWord(PC)); PC += 2;"); break;
                case AddrMode.AbsoluteX_P:
                    w.WriteLine(Spaces + "temp16 = ReadWord(PC);");
                    w.WriteLine(Spaces + dest + " = ReadMemory((ushort)(temp16+X));");
                    w.WriteLine(Spaces + "if ((temp16 & 0xFF00) != ((temp16 + X) & 0xFF00)) ");
                    w.WriteLine(Spaces + "    { PendingCycles--; TotalExecutedCycles++; }");
                    w.WriteLine(Spaces + "PC += 2;");
                    break;
                case AddrMode.AbsoluteX:
                    w.WriteLine(Spaces + dest + " = ReadMemory((ushort)(ReadWord(PC)+X));");
                    w.WriteLine(Spaces + "PC += 2;");
                    break;
                case AddrMode.AbsoluteY_P:
                    w.WriteLine(Spaces + "temp16 = ReadWord(PC);");
                    w.WriteLine(Spaces + dest + " = ReadMemory((ushort)(temp16+Y));");
                    w.WriteLine(Spaces + "if ((temp16 & 0xFF00) != ((temp16 + Y) & 0xFF00)) ");
                    w.WriteLine(Spaces + "    { PendingCycles--; TotalExecutedCycles++; }");
                    w.WriteLine(Spaces + "PC += 2;");
                    break;
                case AddrMode.AbsoluteY:
                    w.WriteLine(Spaces + dest + " = ReadMemory((ushort)(ReadWord(PC)+Y));");
                    w.WriteLine(Spaces + "PC += 2;");
                    break;
                case AddrMode.IndirectX:
                    w.WriteLine(Spaces + dest + " = ReadMemory(ReadWordPageWrap((byte)(ReadMemory(PC++)+X)));"); break;
                case AddrMode.IndirectY:
                    w.WriteLine(Spaces + dest + " = ReadMemory(ReadWordPageWrap((byte)(ReadMemory(PC++)+Y)));"); break;
                case AddrMode.IndirectY_P:
                    w.WriteLine(Spaces + "temp16 = ReadWordPageWrap(ReadMemory(PC++));");
                    w.WriteLine(Spaces + dest + " = ReadMemory((ushort)(temp16+Y));");
                    w.WriteLine(Spaces + "if ((temp16 & 0xFF00) != ((temp16+Y) & 0xFF00)) ");
                    w.WriteLine(Spaces + "    { PendingCycles--; TotalExecutedCycles++; }");
                    break;
            }
        }

        private void GetAddress(OpcodeInfo op, TextWriter w, string dest)
        {
            // TODO it APPEARS that the +1 opcode penalty applies to all AbsoluteX, AbsoluteY, and IndirectY
            // but this is not completely clear. the doc has some exceptions, but are they real?
            switch (op.AddressMode)
            {
                case AddrMode.ZeroPage:
                    w.WriteLine(Spaces + dest + " = ReadMemory(PC++);"); break;
                case AddrMode.ZeroPageX:
                    w.WriteLine(Spaces + dest + " = (byte)(ReadMemory(PC++)+X);"); break;
                case AddrMode.ZeroPageY:
                    w.WriteLine(Spaces + dest + " = (byte)(ReadMemory(PC++)+Y);"); break;
                case AddrMode.Absolute:
                    w.WriteLine(Spaces + dest + " = ReadWord(PC); PC += 2;"); break;
                case AddrMode.AbsoluteX:
                    w.WriteLine(Spaces + dest + " = (ushort)(ReadWord(PC)+X);");
                    w.WriteLine(Spaces + "PC += 2;");
                    break;
                case AddrMode.AbsoluteX_P:
                    w.WriteLine(Spaces + "temp16 = ReadWord(PC);");
                    w.WriteLine(Spaces + dest + " = (ushort)(temp16+X);");
                    w.WriteLine(Spaces + "if ((temp16 & 0xFF00) != ((temp16 + X) & 0xFF00)) ");
                    w.WriteLine(Spaces + "    { PendingCycles--; TotalExecutedCycles++; }");
                    w.WriteLine(Spaces + "PC += 2;");
                    break;
                case AddrMode.AbsoluteY:
                    w.WriteLine(Spaces + dest + " = (ushort)(ReadWord(PC)+Y);");
                    w.WriteLine(Spaces + "PC += 2;");
                    break;
                case AddrMode.AbsoluteY_P:
                    w.WriteLine(Spaces + "temp16 = ReadWord(PC);");
                    w.WriteLine(Spaces + dest + " = (ushort)(temp16+Y);");
                    w.WriteLine(Spaces + "if ((temp16 & 0xFF00) != ((temp16 + Y) & 0xFF00)) ");
                    w.WriteLine(Spaces + "    { PendingCycles--; TotalExecutedCycles++; }");
                    w.WriteLine(Spaces + "PC += 2;");
                    break;
                case AddrMode.IndirectX:
                    w.WriteLine(Spaces + "temp8 = (byte)(ReadMemory(PC++) + X);");
                    w.WriteLine(Spaces + dest + " = ReadWordPageWrap(temp8);");
                    break;
                case AddrMode.IndirectY:
                    w.WriteLine(Spaces + "temp16 = ReadWordPageWrap(ReadMemory(PC++));");
                    w.WriteLine(Spaces + dest + " = (ushort)(temp16+Y);");
                    break;
                case AddrMode.IndirectY_P:
                    w.WriteLine(Spaces + "temp16 = ReadWordPageWrap(ReadMemory(PC++));");
                    w.WriteLine(Spaces + dest + " = (ushort)(temp16+Y);");
                    w.WriteLine(Spaces + "if ((temp16 & 0xFF00) != ((temp16+Y) & 0xFF00)) ");
                    w.WriteLine(Spaces + "    { PendingCycles--; TotalExecutedCycles++; }");
                    break;
                case AddrMode.Relative:
                    w.WriteLine(Spaces + "rel8 = (sbyte)ReadMemory(PC++);"); 
                    w.WriteLine(Spaces + dest +" = (ushort)(PC+rel8);");
                    break;
            }
        }

        public void GenerateDisassembler(string file)
        {
            var w = new StreamWriter(file, false);
            w.WriteLine("using System;");
            w.WriteLine();
            w.WriteLine("// Do not modify this file directly! This is GENERATED code.");
            w.WriteLine("// Please open the CpuCoreGenerator solution and make your modifications there.");
            w.WriteLine();
            w.WriteLine("namespace BizHawk.Emulation.CPUs.M6502");
            w.WriteLine("{");
            w.WriteLine("    public partial class MOS6502");
            w.WriteLine("    {");
            w.WriteLine("        public string Disassemble(ushort pc, out int bytesToAdvance)");
            w.WriteLine("        {");
            w.WriteLine("            byte op = ReadMemory(pc);");
            w.WriteLine("            switch (op)");
            w.WriteLine("            {");

            for (int i = 0; i < 256; i++)
            {
                if (Opcodes[i] != null)
                    DisassembleOpcode(w,i);
            }

            w.WriteLine("            }");
            w.WriteLine("            bytesToAdvance = 1;");
            w.WriteLine("            return \"???\";");
            w.WriteLine("        }");
            w.WriteLine("    }");
            w.WriteLine("}");
            w.Close();
        }

        private void DisassembleOpcode(TextWriter w, int i)
        {
            var op = Opcodes[i];
            w.Write("                case 0x{0:X2}: ", i);

            string mstr;
            switch (op.AddressMode)
            {
                case AddrMode.Implicit:    mstr = "\""+op.Instruction+"\""; break;
                case AddrMode.Accumulator: mstr = "\"" + op.Instruction + " A\""; break;
                case AddrMode.Immediate:   mstr = "string.Format(\""+op.Instruction+" #${0:X2}\", ReadMemory(++pc))"; break;
                case AddrMode.ZeroPage:    mstr = "string.Format(\"" + op.Instruction + " ${0:X2}\", ReadMemory(++pc))"; break;
                case AddrMode.ZeroPageX:   mstr = "string.Format(\"" + op.Instruction + " ${0:X2},X\", ReadMemory(++pc))"; break;
                case AddrMode.ZeroPageY:   mstr = "string.Format(\"" + op.Instruction + " ${0:X2},Y\", ReadMemory(++pc))"; break;
                case AddrMode.Absolute:    mstr = "string.Format(\"" + op.Instruction + " ${0:X4}\", ReadWord(++pc))"; break;
                case AddrMode.AbsoluteX:   mstr = "string.Format(\"" + op.Instruction + " ${0:X4},X\", ReadWord(++pc))"; break;
                case AddrMode.AbsoluteX_P: mstr = "string.Format(\"" + op.Instruction + " ${0:X4},X *\", ReadWord(++pc))"; break;
                case AddrMode.AbsoluteY:   mstr = "string.Format(\"" + op.Instruction + " ${0:X4},Y\", ReadWord(++pc))"; break;
                case AddrMode.AbsoluteY_P: mstr = "string.Format(\"" + op.Instruction + " ${0:X4},Y *\", ReadWord(++pc))"; break;    
                case AddrMode.Indirect:    mstr = "string.Format(\"" + op.Instruction + " (${0:X4})\", ReadWord(++pc))"; break;
                case AddrMode.IndirectX:   mstr = "string.Format(\"" + op.Instruction + " (${0:X2},X)\", ReadMemory(++pc))"; break;
                case AddrMode.IndirectY:   mstr = "string.Format(\"" + op.Instruction + " (${0:X2}),Y\", ReadMemory(++pc))"; break;
                case AddrMode.IndirectY_P: mstr = "string.Format(\"" + op.Instruction + " (${0:X2}),Y *\", ReadMemory(++pc))"; break;    
                case AddrMode.Relative:    mstr = "string.Format(\"" + op.Instruction + " ${0:X4}\", pc+2+(sbyte)ReadMemory(++pc))"; break;
                default:                   mstr = @"""?"""; break;
            }

            // BRK is 2-byte, but it is rarely used. So I don't care about it.
            w.Write("bytesToAdvance = {0}; ", op.Size);
            w.WriteLine("return " + mstr + ";");
        }
    }
}
