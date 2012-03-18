using System;
using System.IO;

namespace HuC6280
{
    public enum AddrMode
    {
        Implicit,
        Accumulator,
        Immediate,
        ZeroPage,
        ZeroPageX,
        ZeroPageY,
        ZeroPageR,
        Absolute,
        AbsoluteX,
        AbsoluteY,
        AbsoluteIndirect,
        AbsoluteIndirectX,
        Indirect,
        IndirectX,
        IndirectY,
        Relative,
        BlockMove,
        ImmZeroPage,
        ImmZeroPageX,
        ImmAbsolute,
        ImmAbsoluteX
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
                    case AddrMode.ZeroPageR:   return 3;
                    case AddrMode.Absolute:    return 3;
                    case AddrMode.AbsoluteX:   return 3;
                    case AddrMode.AbsoluteY:   return 3;
                    case AddrMode.Indirect:    return 2;
                    case AddrMode.IndirectX:   return 2;
                    case AddrMode.IndirectY:   return 2;
                    case AddrMode.Relative:    return 2;
                    case AddrMode.BlockMove:   return 7;
                    case AddrMode.ImmZeroPage: return 3;
                    case AddrMode.ImmZeroPageX:return 3;
                    case AddrMode.ImmAbsolute: return 4;
                    case AddrMode.ImmAbsoluteX:return 4;
                    case AddrMode.AbsoluteIndirect: return 3;
                    case AddrMode.AbsoluteIndirectX: return 3;
                    default:
                        return -1;
                }
            }
        }

        public override string ToString()
        {
            switch (AddressMode)
            {
                case AddrMode.Implicit:    return Instruction;
                case AddrMode.Accumulator: return Instruction + " A";
                case AddrMode.Immediate:   return Instruction + " #nn";
                case AddrMode.ZeroPage:    return Instruction + " zp";
                case AddrMode.ZeroPageX:   return Instruction + " zp,X";
                case AddrMode.ZeroPageY:   return Instruction + " zp,Y";
                case AddrMode.Absolute:    return Instruction + " addr";
                case AddrMode.AbsoluteX:   return Instruction + " addr,X";
                case AddrMode.AbsoluteY:   return Instruction + " addr,Y";
                case AddrMode.Indirect:    return Instruction + " (addr)";
                case AddrMode.IndirectX:   return Instruction + " (addr,X)";
                case AddrMode.IndirectY:   return Instruction + " (addr),Y";
                case AddrMode.Relative:    return Instruction + " +/-rel";
                case AddrMode.BlockMove:   return Instruction + " src, dest, len";
                default: return Instruction;
            }
        }
    }

    public partial class CoreGenerator
    {
        public OpcodeInfo[] Opcodes = new OpcodeInfo[256];

        public void InitOpcodeTable()
        {
            // Add with Carry
            Set(0x69, "ADC", AddrMode.Immediate, 2);
            Set(0x65, "ADC", AddrMode.ZeroPage,  4);
            Set(0x75, "ADC", AddrMode.ZeroPageX, 4);
            Set(0x6D, "ADC", AddrMode.Absolute,  5);
            Set(0x7D, "ADC", AddrMode.AbsoluteX, 5);
            Set(0x79, "ADC", AddrMode.AbsoluteY, 5);
            Set(0x72, "ADC", AddrMode.Indirect,  7);
            Set(0x61, "ADC", AddrMode.IndirectX, 7);
            Set(0x71, "ADC", AddrMode.IndirectY, 7);

            // AND
            Set(0x29, "AND", AddrMode.Immediate, 2);
            Set(0x25, "AND", AddrMode.ZeroPage,  4);
            Set(0x35, "AND", AddrMode.ZeroPageX, 4);
            Set(0x2D, "AND", AddrMode.Absolute,  5);
            Set(0x3D, "AND", AddrMode.AbsoluteX, 5);
            Set(0x39, "AND", AddrMode.AbsoluteY, 5);
            Set(0x32, "AND", AddrMode.Indirect,  7);
            Set(0x21, "AND", AddrMode.IndirectX, 7);
            Set(0x31, "AND", AddrMode.IndirectY, 7);

            // Arithmatic Shift Left
            Set(0x06, "ASL", AddrMode.ZeroPage,  6);
            Set(0x16, "ASL", AddrMode.ZeroPageX, 6);
            Set(0x0E, "ASL", AddrMode.Absolute,  7);
            Set(0x1E, "ASL", AddrMode.AbsoluteX, 7);
            Set(0x0A, "ASL", AddrMode.Accumulator, 2);

            // Branch on Bit Reset
            Set(0x0F, "BBR0", AddrMode.ZeroPageR, 6);
            Set(0x1F, "BBR1", AddrMode.ZeroPageR, 6);
            Set(0x2F, "BBR2", AddrMode.ZeroPageR, 6);
            Set(0x3F, "BBR3", AddrMode.ZeroPageR, 6);
            Set(0x4F, "BBR4", AddrMode.ZeroPageR, 6);
            Set(0x5F, "BBR5", AddrMode.ZeroPageR, 6);
            Set(0x6F, "BBR6", AddrMode.ZeroPageR, 6);
            Set(0x7F, "BBR7", AddrMode.ZeroPageR, 6);

            // Branch on Bit Set 
            Set(0x8F, "BBS0", AddrMode.ZeroPageR, 6);
            Set(0x9F, "BBS1", AddrMode.ZeroPageR, 6);
            Set(0xAF, "BBS2", AddrMode.ZeroPageR, 6);
            Set(0xBF, "BBS3", AddrMode.ZeroPageR, 6);
            Set(0xCF, "BBS4", AddrMode.ZeroPageR, 6);
            Set(0xDF, "BBS5", AddrMode.ZeroPageR, 6);
            Set(0xEF, "BBS6", AddrMode.ZeroPageR, 6);
            Set(0xFF, "BBS7", AddrMode.ZeroPageR, 6);

            // BIT
            Set(0x89, "BIT", AddrMode.Immediate, 2);
            Set(0x24, "BIT", AddrMode.ZeroPage,  4);
            Set(0x34, "BIT", AddrMode.ZeroPageX, 4);
            Set(0x2C, "BIT", AddrMode.Absolute,  5);
            Set(0x3C, "BIT", AddrMode.AbsoluteX, 5);

            // Branch instructions
            Set(0x10, "BPL", AddrMode.Relative, 2); // Branch on Plus
            Set(0x30, "BMI", AddrMode.Relative, 2); // Branch on Minus
            Set(0x50, "BVC", AddrMode.Relative, 2); // Branch on Overflow Clear
            Set(0x70, "BVS", AddrMode.Relative, 2); // Branch on Overflow Set
            Set(0x90, "BCC", AddrMode.Relative, 2); // Branch on Carry Clear
            Set(0xB0, "BCS", AddrMode.Relative, 2); // Branch on Carry Set
            Set(0xD0, "BNE", AddrMode.Relative, 2); // Branch on Not Equal
            Set(0xF0, "BEQ", AddrMode.Relative, 2); // Branch on Equal
            Set(0x80, "BRA", AddrMode.Relative, 4); // Branch Always
            Set(0x44, "BSR", AddrMode.Relative, 8); // Branch to Subroutine

            // CPU Break
            Set(0x00, "BRK", AddrMode.Implicit, 8);

            // Compare accumulator
            Set(0xC9, "CMP", AddrMode.Immediate, 2);
            Set(0xC5, "CMP", AddrMode.ZeroPage,  4);
            Set(0xD5, "CMP", AddrMode.ZeroPageX, 4);
            Set(0xD2, "CMP", AddrMode.Indirect,  7);
            Set(0xC1, "CMP", AddrMode.IndirectX, 7);
            Set(0xD1, "CMP", AddrMode.IndirectY, 7);
            Set(0xCD, "CMP", AddrMode.Absolute,  5);
            Set(0xDD, "CMP", AddrMode.AbsoluteX, 5);
            Set(0xD9, "CMP", AddrMode.AbsoluteY, 5);

            // Compare X register
            Set(0xE0, "CPX", AddrMode.Immediate, 2);
            Set(0xE4, "CPX", AddrMode.ZeroPage,  4);
            Set(0xEC, "CPX", AddrMode.Absolute,  5);

            // Compare Y register
            Set(0xC0, "CPY", AddrMode.Immediate, 2);
            Set(0xC4, "CPY", AddrMode.ZeroPage,  4);
            Set(0xCC, "CPY", AddrMode.Absolute,  5);

            // DEC
            Set(0xC6, "DEC", AddrMode.ZeroPage,  6);
            Set(0xD6, "DEC", AddrMode.ZeroPageX, 6);
            Set(0xCE, "DEC", AddrMode.Absolute,  7);
            Set(0xDE, "DEC", AddrMode.AbsoluteX, 7);
            Set(0x3A, "DEC", AddrMode.Accumulator, 2);

            // Exclusive OR
            Set(0x49, "EOR", AddrMode.Immediate, 2);
            Set(0x45, "EOR", AddrMode.ZeroPage,  4);
            Set(0x55, "EOR", AddrMode.ZeroPageX, 4);
            Set(0x52, "EOR", AddrMode.Indirect,  7);
            Set(0x41, "EOR", AddrMode.IndirectX, 7);
            Set(0x51, "EOR", AddrMode.IndirectY, 7);
            Set(0x4D, "EOR", AddrMode.Absolute,  5);
            Set(0x5D, "EOR", AddrMode.AbsoluteX, 5);
            Set(0x59, "EOR", AddrMode.AbsoluteY, 5);

            // Flag Instructions
            Set(0x18, "CLC", AddrMode.Implicit, 2); // Clear Carry
            Set(0x38, "SEC", AddrMode.Implicit, 2); // Set Carry
            Set(0x58, "CLI", AddrMode.Implicit, 2); // Clear Interrupt
            Set(0x78, "SEI", AddrMode.Implicit, 2); // Set Interrupt
            Set(0xB8, "CLV", AddrMode.Implicit, 2); // Clear Overflow
            Set(0xD8, "CLD", AddrMode.Implicit, 2); // Clear Decimal
            Set(0xF8, "SED", AddrMode.Implicit, 2); // Set Decimal
            Set(0xF4, "SET", AddrMode.Implicit, 2); // Set T flag

            // Additional clear instructions
            Set(0x62, "CLA", AddrMode.Implicit, 2);
            Set(0x82, "CLX", AddrMode.Implicit, 2);
            Set(0xC2, "CLY", AddrMode.Implicit, 2);
            
            // INC
            Set(0xE6, "INC", AddrMode.ZeroPage,  6);
            Set(0xF6, "INC", AddrMode.ZeroPageX, 6);
            Set(0xEE, "INC", AddrMode.Absolute,  7);
            Set(0xFE, "INC", AddrMode.AbsoluteX, 7);
            Set(0x1A, "INC", AddrMode.Accumulator, 2);

            // Jump
            Set(0x4C, "JMP", AddrMode.Absolute, 4);
            Set(0x6C, "JMP", AddrMode.AbsoluteIndirect,  7);
            Set(0x7C, "JMP", AddrMode.AbsoluteIndirectX, 7);

            // Jump to Subroutine
            Set(0x20, "JSR", AddrMode.Absolute, 7);

            // Load Accumulator
            Set(0xA9, "LDA", AddrMode.Immediate, 2);
            Set(0xA5, "LDA", AddrMode.ZeroPage,  4);
            Set(0xB5, "LDA", AddrMode.ZeroPageX, 4);
            Set(0xB2, "LDA", AddrMode.Indirect,  7);
            Set(0xA1, "LDA", AddrMode.IndirectX, 7);
            Set(0xB1, "LDA", AddrMode.IndirectY, 7);
            Set(0xAD, "LDA", AddrMode.Absolute,  5);
            Set(0xBD, "LDA", AddrMode.AbsoluteX, 5);
            Set(0xB9, "LDA", AddrMode.AbsoluteY, 5);

            // Load X register
            Set(0xA2, "LDX", AddrMode.Immediate, 2);
            Set(0xA6, "LDX", AddrMode.ZeroPage,  4);
            Set(0xB6, "LDX", AddrMode.ZeroPageY, 4);
            Set(0xAE, "LDX", AddrMode.Absolute,  5);
            Set(0xBE, "LDX", AddrMode.AbsoluteY, 5);

            // Load Y register
            Set(0xA0, "LDY", AddrMode.Immediate, 2);
            Set(0xA4, "LDY", AddrMode.ZeroPage,  4);
            Set(0xB4, "LDY", AddrMode.ZeroPageX, 4);
            Set(0xAC, "LDY", AddrMode.Absolute,  5);
            Set(0xBC, "LDY", AddrMode.AbsoluteX, 5);

            // Logical Shift Right
            Set(0x46, "LSR", AddrMode.ZeroPage,  6);
            Set(0x56, "LSR", AddrMode.ZeroPageX, 6);
            Set(0x4E, "LSR", AddrMode.Absolute,  7);
            Set(0x5E, "LSR", AddrMode.AbsoluteX, 7);
            Set(0x4A, "LSR", AddrMode.Accumulator, 2);

            // No Operation
            Set(0xEA, "NOP", AddrMode.Implicit, 2);

            // Bitwise OR with Accumulator
            Set(0x09, "ORA", AddrMode.Immediate, 2);
            Set(0x05, "ORA", AddrMode.ZeroPage,  4);
            Set(0x15, "ORA", AddrMode.ZeroPageX, 4);
            Set(0x12, "ORA", AddrMode.Indirect,  7);
            Set(0x01, "ORA", AddrMode.IndirectX, 7);
            Set(0x11, "ORA", AddrMode.IndirectY, 7);
            Set(0x0D, "ORA", AddrMode.Absolute,  5);
            Set(0x1D, "ORA", AddrMode.AbsoluteX, 5);
            Set(0x19, "ORA", AddrMode.AbsoluteY, 5);

            // Register instructions
            Set(0xCA, "DEX", AddrMode.Implicit, 2); // DEC X
            Set(0x88, "DEY", AddrMode.Implicit, 2); // DEC Y
            Set(0xE8, "INX", AddrMode.Implicit, 2); // INC X
            Set(0xC8, "INY", AddrMode.Implicit, 2); // INC Y
            Set(0x22, "SAX", AddrMode.Implicit, 3); // Swap A and X
            Set(0x42, "SAY", AddrMode.Implicit, 3); // Swap A and Y
            Set(0x02, "SXY", AddrMode.Implicit, 3); // Swap X and Y
            Set(0xAA, "TAX", AddrMode.Implicit, 2); // Transfer A to X
            Set(0x8A, "TXA", AddrMode.Implicit, 2); // Transfer X to A
            Set(0xA8, "TAY", AddrMode.Implicit, 2); // Transfer A to Y
            Set(0x98, "TYA", AddrMode.Implicit, 2); // Transfer Y to A

            // Rotate Left
            Set(0x26, "ROL", AddrMode.ZeroPage,  6);
            Set(0x36, "ROL", AddrMode.ZeroPageX, 6);
            Set(0x2E, "ROL", AddrMode.Absolute,  7);
            Set(0x3E, "ROL", AddrMode.AbsoluteX, 7);
            Set(0x2A, "ROL", AddrMode.Accumulator, 2);

            // Rotate Right
            Set(0x66, "ROR", AddrMode.ZeroPage,  6);
            Set(0x76, "ROR", AddrMode.ZeroPageX, 6);
            Set(0x6E, "ROR", AddrMode.Absolute,  7);
            Set(0x7E, "ROR", AddrMode.AbsoluteX, 7);
            Set(0x6A, "ROR", AddrMode.Accumulator, 2);

            // Return from Interrupt
            Set(0x40, "RTI", AddrMode.Implicit, 7);

            // Return from Subroutine
            Set(0x60, "RTS", AddrMode.Implicit, 7);

            // Subtract with Carry
            Set(0xE9, "SBC", AddrMode.Immediate, 2);
            Set(0xE5, "SBC", AddrMode.ZeroPage,  4);
            Set(0xF5, "SBC", AddrMode.ZeroPageX, 4);
            Set(0xF2, "SBC", AddrMode.Indirect,  7);
            Set(0xE1, "SBC", AddrMode.IndirectX, 7);
            Set(0xF1, "SBC", AddrMode.IndirectY, 7);
            Set(0xED, "SBC", AddrMode.Absolute,  5);
            Set(0xFD, "SBC", AddrMode.AbsoluteX, 5);
            Set(0xF9, "SBC", AddrMode.AbsoluteY, 5);
            
            // Store Accumulator
            Set(0x85, "STA", AddrMode.ZeroPage,  4);
            Set(0x95, "STA", AddrMode.ZeroPageX, 4);
            Set(0x92, "STA", AddrMode.Indirect,  7);
            Set(0x81, "STA", AddrMode.IndirectX, 7);
            Set(0x91, "STA", AddrMode.IndirectY, 7);
            Set(0x8D, "STA", AddrMode.Absolute,  5);
            Set(0x9D, "STA", AddrMode.AbsoluteX, 5);
            Set(0x99, "STA", AddrMode.AbsoluteY, 5);

            // Stack instructions
            Set(0x9A, "TXS", AddrMode.Implicit, 2); // Transfer X to Stack
            Set(0xBA, "TSX", AddrMode.Implicit, 2); // Transfer Stack to X
            Set(0x48, "PHA", AddrMode.Implicit, 3); // Push A
            Set(0x68, "PLA", AddrMode.Implicit, 4); // Pull A
            Set(0x08, "PHP", AddrMode.Implicit, 3); // Push P
            Set(0x28, "PLP", AddrMode.Implicit, 4); // Pull P
            Set(0xDA, "PHX", AddrMode.Implicit, 3); // Push X
            Set(0xFA, "PLX", AddrMode.Implicit, 4); // Pull X
            Set(0x5A, "PHY", AddrMode.Implicit, 3); // Push Y
            Set(0x7A, "PLY", AddrMode.Implicit, 4); // Pull Y

            // Store X register
            Set(0x86, "STX", AddrMode.ZeroPage,  4);
            Set(0x96, "STX", AddrMode.ZeroPageY, 4);
            Set(0x8E, "STX", AddrMode.Absolute,  5);

            // Store Y register
            Set(0x84, "STY", AddrMode.ZeroPage,  4);
            Set(0x94, "STY", AddrMode.ZeroPageX, 4);
            Set(0x8C, "STY", AddrMode.Absolute,  5);

            // Memory Paging Register instructions
            Set(0x53, "TAM", AddrMode.Immediate, 5);
            Set(0x43, "TMA", AddrMode.Immediate, 4);

            // VDC I/O instructions
            Set(0x03, "ST0", AddrMode.Immediate, 4);
            Set(0x13, "ST1", AddrMode.Immediate, 4);
            Set(0x23, "ST2", AddrMode.Immediate, 4);

            // Store Memory To Zero 
            Set(0x64, "STZ", AddrMode.ZeroPage,  4);
            Set(0x74, "STZ", AddrMode.ZeroPageX, 4);
            Set(0x9C, "STZ", AddrMode.Absolute,  5);
            Set(0x9E, "STZ", AddrMode.AbsoluteX, 5);

            // Reset Memory Bit i
            Set(0x07, "RMB0", AddrMode.ZeroPage, 7);
            Set(0x17, "RMB1", AddrMode.ZeroPage, 7);
            Set(0x27, "RMB2", AddrMode.ZeroPage, 7);
            Set(0x37, "RMB3", AddrMode.ZeroPage, 7);
            Set(0x47, "RMB4", AddrMode.ZeroPage, 7);
            Set(0x57, "RMB5", AddrMode.ZeroPage, 7);
            Set(0x67, "RMB6", AddrMode.ZeroPage, 7);
            Set(0x77, "RMB7", AddrMode.ZeroPage, 7);

            // Set Memory Bit i
            Set(0x87, "SMB0", AddrMode.ZeroPage, 7);
            Set(0x97, "SMB1", AddrMode.ZeroPage, 7);
            Set(0xA7, "SMB2", AddrMode.ZeroPage, 7);
            Set(0xB7, "SMB3", AddrMode.ZeroPage, 7);
            Set(0xC7, "SMB4", AddrMode.ZeroPage, 7);
            Set(0xD7, "SMB5", AddrMode.ZeroPage, 7);
            Set(0xE7, "SMB6", AddrMode.ZeroPage, 7);
            Set(0xF7, "SMB7", AddrMode.ZeroPage, 7);

            // Test and Reset Memory Bit Against Accumulator
            Set(0x14, "TRB", AddrMode.ZeroPage, 6);
            Set(0x1C, "TRB", AddrMode.Absolute, 7);

            // Test and Set Memory Bit Against Accumulator
            Set(0x04, "TSB", AddrMode.ZeroPage, 6);
            Set(0x0C, "TSB", AddrMode.Absolute, 7);

            // Test and Reset Memory Bits
            Set(0x83, "TST", AddrMode.ImmZeroPage,  7);
            Set(0xA3, "TST", AddrMode.ImmZeroPageX, 7);
            Set(0x93, "TST", AddrMode.ImmAbsolute,  8);
            Set(0xB3, "TST", AddrMode.ImmAbsoluteX, 8);

            // Cpu Speed instructions
            Set(0xD4, "CSH", AddrMode.Implicit, 3);
            Set(0x54, "CSL", AddrMode.Implicit, 3);

            // Block Memory Transfer instructions
            Set(0xF3, "TAI", AddrMode.BlockMove, 17); // Transfer Alternate Increment
            Set(0xE3, "TIA", AddrMode.BlockMove, 17); // Transfer Increment Alternate
            Set(0x73, "TII", AddrMode.BlockMove, 17); // Transfer Increment Increment
            Set(0xD3, "TIN", AddrMode.BlockMove, 17); // Transfer Increment None
            Set(0xC3, "TDD", AddrMode.BlockMove, 17); // Transfer Decrement Decrement
        }

        private void Set(byte value, string instr, AddrMode addressMode, int cycles)
        {
            var op = new OpcodeInfo();
            op.Instruction = instr;
            op.AddressMode = addressMode;
            op.Cycles = cycles;
            if (Opcodes[value] != null)
                throw new Exception("opcode " + value + " already assigned");
            Opcodes[value] = op;
        }

        public void GenerateExecutor(string file)
        {
            var w = new StreamWriter(file, false);
            w.WriteLine("using System;");
            w.WriteLine("using BizHawk.Emulation.Consoles.TurboGrafx;");
            w.WriteLine();
            w.WriteLine("// Do not modify this file directly! This is GENERATED code.");
            w.WriteLine("// Please open the CpuCoreGenerator solution and make your modifications there.");
            w.WriteLine();
            w.WriteLine("namespace BizHawk.Emulation.CPUs.H6280");
            w.WriteLine("{");
            w.WriteLine("    public partial class HuC6280");
            w.WriteLine("    {");
            w.WriteLine("        public void Execute(int cycles)");
            w.WriteLine("        {");
            w.WriteLine("            sbyte rel8;");
            w.WriteLine("            byte value8, temp8, source8;");
            w.WriteLine("            ushort value16, temp16;");
            w.WriteLine("            int temp, lo, hi;");
            w.WriteLine();
            w.WriteLine("            PendingCycles += cycles;");
            w.WriteLine("            while (PendingCycles > 0)");
            w.WriteLine("            {");
            w.WriteLine("                int lastCycles = PendingCycles;");
            w.WriteLine();
            w.WriteLine("                if (IRQ1Assert && FlagI == false && LagIFlag == false && (IRQControlByte & IRQ1Selector) == 0 && InBlockTransfer == false)");
            w.WriteLine("                {");
            w.WriteLine("                    WriteMemory((ushort)(S-- + 0x2100), (byte)(PC >> 8));");
            w.WriteLine("                    WriteMemory((ushort)(S-- + 0x2100), (byte)PC);");
            w.WriteLine("                    WriteMemory((ushort)(S-- + 0x2100), (byte)(P & (~0x10)));");
            w.WriteLine("                    FlagD = false;");
            w.WriteLine("                    FlagI = true;");
            w.WriteLine("                    PC = ReadWord(IRQ1Vector);");
            w.WriteLine("                    PendingCycles -= 8;");
            w.WriteLine("                }");
            w.WriteLine();
            w.WriteLine("                if (TimerAssert && FlagI == false && LagIFlag == false && (IRQControlByte & TimerSelector) == 0 && InBlockTransfer == false)");
            w.WriteLine("                {");
            w.WriteLine("                    WriteMemory((ushort)(S-- + 0x2100), (byte)(PC >> 8));");
            w.WriteLine("                    WriteMemory((ushort)(S-- + 0x2100), (byte)PC);");
            w.WriteLine("                    WriteMemory((ushort)(S-- + 0x2100), (byte)(P & (~0x10)));");
            w.WriteLine("                    FlagD = false;");
            w.WriteLine("                    FlagI = true;");
            w.WriteLine("                    PC = ReadWord(TimerVector);");
            w.WriteLine("                    PendingCycles -= 8;");
            w.WriteLine("                }");
            w.WriteLine();
            w.WriteLine("                if (IRQ2Assert && FlagI == false && LagIFlag == false && (IRQControlByte & IRQ2Selector) == 0 && InBlockTransfer == false)");
            w.WriteLine("                {");
            w.WriteLine("                    WriteMemory((ushort)(S-- + 0x2100), (byte)(PC >> 8));");
            w.WriteLine("                    WriteMemory((ushort)(S-- + 0x2100), (byte)PC);");
            w.WriteLine("                    WriteMemory((ushort)(S-- + 0x2100), (byte)(P & (~0x10)));");
            w.WriteLine("                    FlagD = false;");
            w.WriteLine("                    FlagI = true;");
            w.WriteLine("                    PC = ReadWord(IRQ2Vector);");
            w.WriteLine("                    PendingCycles -= 8;");
            w.WriteLine("                }");
            w.WriteLine();
            w.WriteLine("                IRQControlByte = IRQNextControlByte;");
            w.WriteLine("                LagIFlag = FlagI;");
            w.WriteLine();

            w.WriteLine("                byte opcode = ReadMemory(PC++);");
            w.WriteLine("                switch (opcode)");
            w.WriteLine("                {");

            for (int i = 0; i < 256; i++)
            {
                if (Opcodes[i] != null)
                    EmulateOpcode(w, i);
            }

            w.WriteLine("                    default:");
            w.WriteLine("                        Console.WriteLine(\"Unhandled opcode: {0:X2}\", opcode);");
            w.WriteLine("                        break;");
            w.WriteLine("                }");
            w.WriteLine();
            w.WriteLine("                P &= 0xDF; // Clear T flag");
            w.WriteLine("            AfterClearTFlag: // SET command jumps here");
            w.WriteLine("                int delta = lastCycles - PendingCycles;");
            w.WriteLine("                if (LowSpeed)");
            w.WriteLine("                {");
            w.WriteLine("                    delta *= 4;");
            w.WriteLine("                    PendingCycles = lastCycles - delta;");
            w.WriteLine("                }");
            w.WriteLine("                TotalExecutedCycles += delta;");
            w.WriteLine();
            w.WriteLine("                if (TimerEnabled)");
            w.WriteLine("                {");
            w.WriteLine("                    TimerTickCounter += delta;");
            w.WriteLine("                    while (TimerTickCounter >= 1024)");
            w.WriteLine("                    {");
            w.WriteLine("                        TimerValue--;");
            w.WriteLine("                        TimerTickCounter -= 1024;");
            w.WriteLine("                        if (TimerValue == 0xFF)");
            w.WriteLine("                        {");
            w.WriteLine("                            TimerValue = TimerReloadValue;");
            w.WriteLine("                            TimerAssert = true;");
            w.WriteLine("                        }");
            w.WriteLine("                    }");
            w.WriteLine("                }");
            w.WriteLine("                ThinkAction(delta);");
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
                case "BBR0": BB(op, w, 0, false); break;
                case "BBR1": BB(op, w, 1, false); break;
                case "BBR2": BB(op, w, 2, false); break;
                case "BBR3": BB(op, w, 3, false); break;
                case "BBR4": BB(op, w, 4, false); break;
                case "BBR5": BB(op, w, 5, false); break;
                case "BBR6": BB(op, w, 6, false); break;
                case "BBR7": BB(op, w, 7, false); break;
                case "BBS0": BB(op, w, 0, true); break;
                case "BBS1": BB(op, w, 1, true); break;
                case "BBS2": BB(op, w, 2, true); break;
                case "BBS3": BB(op, w, 3, true); break;
                case "BBS4": BB(op, w, 4, true); break;
                case "BBS5": BB(op, w, 5, true); break;
                case "BBS6": BB(op, w, 6, true); break;
                case "BBS7": BB(op, w, 7, true); break;
                case "BCC": Branch(op, w, "C", false); break;
                case "BCS": Branch(op, w, "C", true); break;
                case "BEQ": Branch(op, w, "Z", true); break;
                case "BIT": BIT(op, w); break;
                case "BMI": Branch(op, w, "N", true); break;
                case "BNE": Branch(op, w, "Z", false); break;
                case "BPL": Branch(op, w, "N", false); break;
                case "BRA": BRA(op, w); break;
                case "BRK": BRK(op, w); break;
                case "BSR": BSR(op, w); break;
                case "BVC": Branch(op, w, "V", false); break;
                case "BVS": Branch(op, w, "V", true); break;
                case "CLA": CLreg(op, w, "A"); break;
                case "CLC": CLC(op, w); break;
                case "CLD": CLD(op, w); break;
                case "CLI": CLI(op, w); break;
                case "CLV": CLV(op, w); break;
                case "CLX": CLreg(op, w, "X"); break;
                case "CLY": CLreg(op, w, "Y"); break;
                case "CMP": CMP_reg(op, w, "A"); break;
                case "CPX": CMP_reg(op, w, "X"); break;
                case "CPY": CMP_reg(op, w, "Y"); break;
                case "CSH": CSH(op, w); break;
                case "CSL": CSL(op, w); break;
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
                case "PHA": PushReg(op, w, "A"); break;
                case "PHP": PushReg(op, w, "P"); break;
                case "PHX": PushReg(op, w, "X"); break;
                case "PHY": PushReg(op, w, "Y"); break;
                case "PLA": PullReg(op, w, "A"); break;
                case "PLP": PLP(op, w); break;
                case "PLX": PullReg(op, w, "X"); break;
                case "PLY": PullReg(op, w, "Y"); break;
                case "ROL": ROL(op, w); break;
                case "ROR": ROR(op, w); break;
                case "RTI": RTI(op, w); break;
                case "RTS": RTS(op, w); break;
                case "SAX": SAX(op, w); break;
                case "SAY": SAY(op, w); break;
                case "SBC": SBC(op, w); break;
                case "SEC": SEC(op, w); break;
                case "SED": SED(op, w); break;
                case "SEI": SEI(op, w); break;
                case "SET": SET(op, w); break;
                case "SXY": SXY(op, w); break;
                case "ST0": ST0(op, w); break;
                case "ST1": ST1(op, w); break;
                case "ST2": ST2(op, w); break;
                case "STA": STA(op, w); break;
                case "STX": STX(op, w); break;
                case "STY": STY(op, w); break;
                case "STZ": STZ(op, w); break;
                case "TAI": TAI(op, w); break;
                case "TAM": TAM(op, w); break;
                case "TAX": TAX(op, w); break;
                case "TAY": TAY(op, w); break;
                case "TDD": TDD(op, w); break;
                case "TIA": TIA(op, w); break;
                case "TII": TII(op, w); break;
                case "TIN": TIN(op, w); break;
                case "TMA": TMA(op, w); break;
                case "TRB": TRB(op, w); break;
                case "TSB": TSB(op, w); break;
                case "TST": TST(op, w); break;
                case "TSX": TSX(op, w); break;
                case "TXA": TXA(op, w); break;
                case "TYA": TYA(op, w); break;
                case "TXS": TXS(op, w); break;
                case "RMB0": RMB(op, w, 0); break;
                case "RMB1": RMB(op, w, 1); break;
                case "RMB2": RMB(op, w, 2); break;
                case "RMB3": RMB(op, w, 3); break;
                case "RMB4": RMB(op, w, 4); break;
                case "RMB5": RMB(op, w, 5); break;
                case "RMB6": RMB(op, w, 6); break;
                case "RMB7": RMB(op, w, 7); break;
                case "SMB0": SMB(op, w, 0); break;
                case "SMB1": SMB(op, w, 1); break;
                case "SMB2": SMB(op, w, 2); break;
                case "SMB3": SMB(op, w, 3); break;
                case "SMB4": SMB(op, w, 4); break;
                case "SMB5": SMB(op, w, 5); break;
                case "SMB6": SMB(op, w, 6); break;
                case "SMB7": SMB(op, w, 7); break;
                default:
                    w.WriteLine("throw new Exception(\"unsupported opcode {0:X2}\");",opcode);
                    break;
            }
            if (op.Instruction != "SET" && op.Instruction != "RTI" && op.Instruction != "PLP")
                w.WriteLine(Spaces + "break;");
        }

        private void GetValue8(OpcodeInfo op, TextWriter w, string dest)
        {
            switch (op.AddressMode)
            {
                case AddrMode.Immediate:
                    w.WriteLine(Spaces + dest + " = ReadMemory(PC++);"); break;
                case AddrMode.ImmZeroPage:
                case AddrMode.ZeroPage:
                    w.WriteLine(Spaces + dest + " = ReadMemory((ushort)(ReadMemory(PC++)+0x2000));"); break;
                case AddrMode.ImmZeroPageX:
                case AddrMode.ZeroPageX:
                    w.WriteLine(Spaces + dest + " = ReadMemory((ushort)(((ReadMemory(PC++)+X)&0xFF)+0x2000));"); break;
                case AddrMode.ZeroPageY:
                    w.WriteLine(Spaces + dest + " = ReadMemory((ushort)(((ReadMemory(PC++)+Y)&0xFF)+0x2000));"); break;
                case AddrMode.ImmAbsolute:
                case AddrMode.Absolute:
                    w.WriteLine(Spaces + dest + " = ReadMemory(ReadWord(PC)); PC += 2;"); break;
                case AddrMode.ImmAbsoluteX:
                case AddrMode.AbsoluteX:
                    w.WriteLine(Spaces + dest + " = ReadMemory((ushort)(ReadWord(PC)+X));");
                    w.WriteLine(Spaces + "PC += 2;");
                    break;
                case AddrMode.AbsoluteY:
                    w.WriteLine(Spaces + dest + " = ReadMemory((ushort)(ReadWord(PC)+Y));");
                    w.WriteLine(Spaces + "PC += 2;");
                    break;
                case AddrMode.Indirect:
                    w.WriteLine(Spaces + dest + " = ReadMemory(ReadWordPageWrap((ushort)(ReadMemory(PC++)+0x2000)));"); break;
                case AddrMode.IndirectX:
                    w.WriteLine(Spaces + dest + " = ReadMemory(ReadWordPageWrap((ushort)((byte)(ReadMemory(PC++)+X)+0x2000)));"); break;
                case AddrMode.IndirectY:
                    w.WriteLine(Spaces + "temp16 = ReadWordPageWrap((ushort)(ReadMemory(PC++)+0x2000));");
                    w.WriteLine(Spaces + dest + " = ReadMemory((ushort)(temp16+Y));");
                    break;
                default:
                    throw new Exception("p"+op.Instruction);
            }
        }

        private void GetAddress(OpcodeInfo op, TextWriter w, string dest)
        {
            switch (op.AddressMode)
            {
                case AddrMode.ZeroPage:
                    w.WriteLine(Spaces + dest + " = (ushort)(ReadMemory(PC++)+0x2000);"); break;
                case AddrMode.ZeroPageX:
                    w.WriteLine(Spaces + dest + " = (ushort)(((ReadMemory(PC++)+X)&0xFF)+0x2000);"); break;
                case AddrMode.ZeroPageY:
                    w.WriteLine(Spaces + dest + " = (ushort)(((ReadMemory(PC++)+Y)&0xFF)+0x2000);"); break;
                case AddrMode.Absolute:
                    w.WriteLine(Spaces + dest + " = ReadWord(PC); PC += 2;"); break;
                case AddrMode.AbsoluteX:
                    w.WriteLine(Spaces + dest + " = (ushort)(ReadWord(PC)+X);");
                    w.WriteLine(Spaces + "PC += 2;");
                    break;
                case AddrMode.AbsoluteY:
                    w.WriteLine(Spaces + dest + " = (ushort)(ReadWord(PC)+Y);");
                    w.WriteLine(Spaces + "PC += 2;");
                    break;
                case AddrMode.Indirect:
                    w.WriteLine(Spaces + dest + " = ReadWordPageWrap((ushort)(ReadMemory(PC++)+0x2000));"); break;
                case AddrMode.IndirectX:
                    w.WriteLine(Spaces + dest + " = ReadWordPageWrap((ushort)((byte)(ReadMemory(PC++)+X)+0x2000));"); break;
                case AddrMode.IndirectY:
                    w.WriteLine(Spaces + "temp16 = ReadWordPageWrap((ushort)(ReadMemory(PC++)+0x2000));");
                    w.WriteLine(Spaces + dest + " = (ushort)(temp16+Y);");
                    break;
                case AddrMode.Relative:
                    w.WriteLine(Spaces + "rel8 = (sbyte)ReadMemory(PC++);");
                    w.WriteLine(Spaces + dest + " = (ushort)(PC+rel8);");
                    break;
            }
        }

        public void GenerateDisassembler(string file)
        {
            var w = new StreamWriter(file, false);
            w.WriteLine("namespace BizHawk.Emulation.CPUs.H6280");
            w.WriteLine();
            w.WriteLine("// Do not modify this file directly! This is GENERATED code.");
            w.WriteLine("// Please open the CpuCoreGenerator solution and make your modifications there.");
            w.WriteLine();
            w.WriteLine("{");
            w.WriteLine("    public partial class HuC6280");
            w.WriteLine("    {");
            w.WriteLine("        public string Disassemble(ushort pc, out int bytesToAdvance)");
            w.WriteLine("        {");
            w.WriteLine("            byte op = ReadMemory(pc);");
            w.WriteLine("            switch (op)");
            w.WriteLine("            {");

            for (int i = 0; i < 256; i++)
            {
                if (Opcodes[i] != null)
                    DisassembleOpcode(w, i);
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
                case AddrMode.Implicit:          mstr = "\"" + op.Instruction + "\""; break;
                case AddrMode.Accumulator:       mstr = "\"" + op.Instruction + " A\""; break;
                case AddrMode.Immediate:         mstr = "string.Format(\"" + op.Instruction + " #${0:X2}\", ReadMemory(++pc))"; break;
                case AddrMode.ZeroPage:          mstr = "string.Format(\"" + op.Instruction + " ${0:X2}\", ReadMemory(++pc))"; break;
                case AddrMode.ZeroPageX:         mstr = "string.Format(\"" + op.Instruction + " ${0:X2},X\", ReadMemory(++pc))"; break;
                case AddrMode.ZeroPageY:         mstr = "string.Format(\"" + op.Instruction + " ${0:X2},Y\", ReadMemory(++pc))"; break;
                case AddrMode.ZeroPageR:         mstr = "string.Format(\"" + op.Instruction + " ${0:X2},{1}\", ReadMemory(++pc), (sbyte)ReadMemory(++pc))"; break;
                case AddrMode.Absolute:          mstr = "string.Format(\"" + op.Instruction + " ${0:X4}\", ReadWord(++pc))"; break;
                case AddrMode.AbsoluteX:         mstr = "string.Format(\"" + op.Instruction + " ${0:X4},X\", ReadWord(++pc))"; break;
                case AddrMode.AbsoluteY:         mstr = "string.Format(\"" + op.Instruction + " ${0:X4},Y\", ReadWord(++pc))"; break;
                case AddrMode.Indirect:          mstr = "string.Format(\"" + op.Instruction + " (${0:X2})\", ReadMemory(++pc))"; break;
                case AddrMode.IndirectX:         mstr = "string.Format(\"" + op.Instruction + " (${0:X2},X)\", ReadMemory(++pc))"; break;
                case AddrMode.IndirectY:         mstr = "string.Format(\"" + op.Instruction + " (${0:X2}),Y\", ReadMemory(++pc))"; break;
                case AddrMode.Relative:          mstr = "string.Format(\"" + op.Instruction + " {0}\", (sbyte)ReadMemory(++pc))"; break;
                case AddrMode.BlockMove:         mstr = "string.Format(\"" + op.Instruction + " {0:X4},{1:X4},{2:X4}\", ReadWord((ushort)(pc+1)),ReadWord((ushort)(pc+3)),ReadWord((ushort)(pc+5)))"; break;
                case AddrMode.ImmZeroPage:       mstr = "string.Format(\"" + op.Instruction + " #${0:X2}, ${1:X2}\", ReadMemory(++pc), ReadMemory(++pc))"; break;
                case AddrMode.ImmZeroPageX:      mstr = "string.Format(\"" + op.Instruction + " #${0:X2}, ${1:X2},X\", ReadMemory(++pc), ReadMemory(++pc))"; break;
                case AddrMode.ImmAbsolute:       mstr = "string.Format(\"" + op.Instruction + " #${0:X2}, ${1:X4}\", ReadMemory(++pc), ReadWord(++pc))"; break;
                case AddrMode.ImmAbsoluteX:      mstr = "string.Format(\"" + op.Instruction + " #${0:X2}, ${1:X4},X\", ReadMemory(++pc), ReadWord(++pc))"; break;
                case AddrMode.AbsoluteIndirect:  mstr = "string.Format(\"" + op.Instruction + " (${0:X4})\", ReadWord(++pc))"; break;
                case AddrMode.AbsoluteIndirectX: mstr = "string.Format(\"" + op.Instruction + " (${0:X4},X)\", ReadWord(++pc))"; break;
                default:                         mstr = @"""?"""; break;
            }

            w.Write("bytesToAdvance = {0}; ", op.Size);
            w.WriteLine("return " + mstr + ";");
        }
    }
}