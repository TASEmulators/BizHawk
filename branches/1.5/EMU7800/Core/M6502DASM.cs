/*
 * M6502DASM.cs
 * 
 * Provides disassembly services.
 * 
 * Copyright © 2003, 2004 Mike Murphy
 * 
 */
using System;
using System.Text;

namespace EMU7800.Core
{
    public static class M6502DASM
    {
        // Instruction Mnemonics
        enum m : uint
        {
            ADC = 1, AND, ASL,
            BIT, BCC, BCS, BEQ, BMI, BNE, BPL, BRK, BVC, BVS,
            CLC, CLD, CLI, CLV, CMP, CPX, CPY,
            DEC, DEX, DEY,
            EOR,
            INC, INX, INY,
            JMP, JSR,
            LDA, LDX, LDY, LSR,
            NOP,
            ORA,
            PLA, PLP, PHA, PHP,
            ROL, ROR, RTI, RTS,
            SEC, SEI, STA, SBC, SED, STX, STY,
            TAX, TAY, TSX, TXA, TXS, TYA,

            // Illegal/undefined opcodes
            isb,
            kil,
            lax,
            rla,
            sax,
            top
        }

        // Addressing Modes
        enum a : uint
        {
            REL,    // Relative: $aa (branch instructions only)
            ZPG,    // Zero Page: $aa
            ZPX,    // Zero Page Indexed X: $aa,X
            ZPY,    // Zero Page Indexed Y: $aa,Y
            ABS,    // Absolute: $aaaa
            ABX,    // Absolute Indexed X: $aaaa,X
            ABY,    // Absolute Indexed Y: $aaaa,Y
            IDX,    // Indexed Indirect: ($aa,X)
            IDY,    // Indirect Indexed: ($aa),Y
            IND,    // Indirect Absolute: ($aaaa) (JMP only)
            IMM,    // Immediate: #aa
            IMP,    // Implied
            ACC     // Accumulator
        }

        static readonly m[] MnemonicMatrix = {
//        0      1      2      3      4      5      6      7      8      9      A  B      C      D      E      F
/*0*/ m.BRK, m.ORA, m.kil,     0,     0, m.ORA, m.ASL,     0, m.PHP, m.ORA, m.ASL, 0, m.top, m.ORA, m.ASL,     0,/*0*/
/*1*/ m.BPL, m.ORA, m.kil,     0,     0, m.ORA, m.ASL,     0, m.CLC, m.ORA,     0, 0, m.top, m.ORA, m.ASL,     0,/*1*/
/*2*/ m.JSR, m.AND, m.kil,     0, m.BIT, m.AND, m.ROL,     0, m.PLP, m.AND, m.ROL, 0, m.BIT, m.AND, m.ROL,     0,/*2*/
/*3*/ m.BMI, m.AND, m.kil,     0,     0, m.AND, m.ROL,     0, m.SEC, m.AND,     0, 0, m.top, m.AND, m.ROL, m.rla,/*3*/
/*4*/ m.RTI, m.EOR, m.kil,     0,     0, m.EOR, m.LSR,     0, m.PHA, m.EOR, m.LSR, 0, m.JMP, m.EOR, m.LSR,     0,/*4*/
/*5*/ m.BVC, m.EOR, m.kil,     0,     0, m.EOR, m.LSR,     0, m.CLI, m.EOR,     0, 0, m.top, m.EOR, m.LSR,     0,/*5*/
/*6*/ m.RTS, m.ADC, m.kil,     0,     0, m.ADC, m.ROR,     0, m.PLA, m.ADC, m.ROR, 0, m.JMP, m.ADC, m.ROR,     0,/*6*/
/*7*/ m.BVS, m.ADC, m.kil,     0,     0, m.ADC, m.ROR,     0, m.SEI, m.ADC,     0, 0, m.top, m.ADC, m.ROR,     0,/*7*/
/*8*/     0, m.STA,     0, m.sax, m.STY, m.STA, m.STX, m.sax, m.DEY,     0, m.TXA, 0, m.STY, m.STA, m.STX, m.sax,/*8*/
/*9*/ m.BCC, m.STA, m.kil,     0, m.STY, m.STA, m.STX, m.sax, m.TYA, m.STA, m.TXS, 0, m.top, m.STA,     0,     0,/*9*/
/*A*/ m.LDY, m.LDA, m.LDX, m.lax, m.LDY, m.LDA, m.LDX, m.lax, m.TAY, m.LDA, m.TAX, 0, m.LDY, m.LDA, m.LDX, m.lax,/*A*/
/*B*/ m.BCS, m.LDA, m.kil, m.lax, m.LDY, m.LDA, m.LDX, m.lax, m.CLV, m.LDA, m.TSX, 0, m.LDY, m.LDA, m.LDX, m.lax,/*B*/
/*C*/ m.CPY, m.CMP,     0,     0, m.CPY, m.CMP, m.DEC,     0, m.INY, m.CMP, m.DEX, 0, m.CPY, m.CMP, m.DEC,     0,/*C*/
/*D*/ m.BNE, m.CMP, m.kil,     0,     0, m.CMP, m.DEC,     0, m.CLD, m.CMP,     0, 0, m.top, m.CMP, m.DEC,     0,/*D*/
/*E*/ m.CPX, m.SBC,     0,     0, m.CPX, m.SBC, m.INC,     0, m.INX, m.SBC, m.NOP, 0, m.CPX, m.SBC, m.INC, m.isb,/*E*/
/*F*/ m.BEQ, m.SBC, m.kil,     0,     0, m.SBC, m.INC,     0, m.SED, m.SBC,     0, 0, m.top, m.SBC, m.INC, m.isb /*F*/
};

        static readonly a[] AddressingModeMatrix = {
//        0      1      2      3      4      5      6      7      8      9      A  B      C      D      E      F
/*0*/ a.IMP, a.IDX, a.IMP,     0,     0, a.ZPG, a.ZPG,     0, a.IMP, a.IMM, a.ACC, 0, a.ABS, a.ABS, a.ABS,     0,/*0*/
/*1*/ a.REL, a.IDY, a.IMP,     0,     0, a.ZPG, a.ZPG,     0, a.IMP, a.ABY,     0, 0, a.ABS, a.ABX, a.ABX,     0,/*1*/
/*2*/ a.ABS, a.IDX, a.IMP,     0, a.ZPG, a.ZPG, a.ZPG,     0, a.IMP, a.IMM, a.ACC, 0, a.ABS, a.ABS, a.ABS,     0,/*2*/
/*3*/ a.REL, a.IDY, a.IMP,     0,     0, a.ZPG, a.ZPG,     0, a.IMP, a.ABY,     0, 0, a.ABS, a.ABX, a.ABX, a.ABX,/*3*/
/*4*/ a.IMP, a.IDY, a.IMP,     0,     0, a.ZPG, a.ZPG,     0, a.IMP, a.IMM, a.ACC, 0, a.ABS, a.ABS, a.ABS,     0,/*4*/
/*5*/ a.REL, a.IDY, a.IMP,     0,     0, a.ZPG, a.ZPG,     0, a.IMP, a.ABY,     0, 0, a.ABS, a.ABX, a.ABX,     0,/*5*/
/*6*/ a.IMP, a.IDX, a.IMP,     0,     0, a.ZPG, a.ZPG,     0, a.IMP, a.IMM, a.ACC, 0, a.IND, a.ABS, a.ABS,     0,/*6*/
/*7*/ a.REL, a.IDY, a.IMP,     0,     0, a.ZPX, a.ZPX,     0, a.IMP, a.ABY,     0, 0, a.ABS, a.ABX, a.ABX,     0,/*7*/
/*8*/     0, a.IDY,     0, a.IDX, a.ZPG, a.ZPG, a.ZPG, a.ZPG, a.IMP,     0, a.IMP, 0, a.ABS, a.ABS, a.ABS, a.ABS,/*8*/
/*9*/ a.REL, a.IDY, a.IMP,     0, a.ZPX, a.ZPX, a.ZPY, a.ZPY, a.IMP, a.ABY, a.IMP, 0, a.ABS, a.ABX,     0,     0,/*9*/
/*A*/ a.IMM, a.IND, a.IMM, a.IDX, a.ZPG, a.ZPG, a.ZPG, a.ZPX, a.IMP, a.IMM, a.IMP, 0, a.ABS, a.ABS, a.ABS, a.ABS,/*A*/
/*B*/ a.REL, a.IDY, a.IMP, a.IDY, a.ZPX, a.ZPX, a.ZPY, a.ZPY, a.IMP, a.ABY, a.IMP, 0, a.ABX, a.ABX, a.ABY, a.ABY,/*B*/
/*C*/ a.IMM, a.IDX,     0,     0, a.ZPG, a.ZPG, a.ZPG,     0, a.IMP, a.IMM, a.IMP, 0, a.ABS, a.ABS, a.ABS,     0,/*C*/
/*D*/ a.REL, a.IDY, a.IMP,     0,     0, a.ZPX, a.ZPX,     0, a.IMP, a.ABY,     0, 0, a.ABS, a.ABX, a.ABX,     0,/*D*/
/*E*/ a.IMM, a.IDX,     0,     0, a.ZPG, a.ZPG, a.ZPG,     0, a.IMP, a.IMM, a.IMP, 0, a.ABS, a.ABS, a.ABS, a.ABS,/*E*/
/*F*/ a.REL, a.IDY, a.IMP,     0,     0, a.ZPX, a.ZPX,     0, a.IMP, a.ABY,     0, 0, a.ABS, a.ABX, a.ABX, a.ABX /*F*/
};

        public static string GetRegisters(M6502 cpu)
        {
            var dSB = new StringBuilder();
            dSB.Append(String.Format(
                "PC:{0:x4} A:{1:x2} X:{2:x2} Y:{3:x2} S:{4:x2} P:",
                cpu.PC, cpu.A, cpu.X, cpu.Y, cpu.S));

            const string flags = "nv0bdizcNV1BDIZC";

            for (var i = 0; i < 8; i++)
            {
                dSB.Append(((cpu.P & (1 << (7 - i))) == 0) ? flags[i] : flags[i + 8]);
            }
            return dSB.ToString();
        }

        public static string Disassemble(AddressSpace addrSpace, ushort atAddr, ushort untilAddr)
        {
            var dSB = new StringBuilder();
            var dPC = atAddr;
            while (atAddr < untilAddr)
            {
                dSB.AppendFormat("{0:x4}: ", dPC);
                var len = GetInstructionLength(addrSpace, dPC);
                for (var i = 0; i < 3; i++)
                {
                    if (i < len)
                    {
                        dSB.AppendFormat("{0:x2} ", addrSpace[atAddr++]);
                    }
                    else
                    {
                        dSB.Append("   ");
                    }
                }
                dSB.AppendFormat("{0,-15}{1}", RenderOpCode(addrSpace, dPC), Environment.NewLine);
                dPC += (ushort)len;
            }
            if (dSB.Length > 0)
            {
                dSB.Length--;  // Trim trailing newline
            }
            return dSB.ToString();
        }

        public static string MemDump(AddressSpace addrSpace, ushort atAddr, ushort untilAddr)
        {
            var dSB = new StringBuilder();
            var len = untilAddr - atAddr;
            while (len-- >= 0)
            {
                dSB.AppendFormat("{0:x4}: ", atAddr);
                for (var i = 0; i < 8; i++)
                {
                    dSB.AppendFormat("{0:x2} ", addrSpace[atAddr++]);
                    if (i == 3)
                    {
                        dSB.Append(" ");
                    }
                }
                dSB.Append("\n");
            }
            if (dSB.Length > 0)
            {
                dSB.Length--;  // Trim trailing newline
            }
            return dSB.ToString();
        }

        public static string RenderOpCode(AddressSpace addrSpace, ushort PC)
        {
            var num_operands = GetInstructionLength(addrSpace, PC) - 1;
            var PC1 = (ushort)(PC + 1);
            string addrmodeStr;

            switch (AddressingModeMatrix[addrSpace[PC]])
            {
                case a.REL:
                    addrmodeStr = String.Format("${0:x4}", (ushort)(PC + (sbyte)(addrSpace[PC1]) + 2));
                    break;
                case a.ZPG:
                case a.ABS:
                    addrmodeStr = RenderEA(addrSpace, PC1, num_operands);
                    break;
                case a.ZPX:
                case a.ABX:
                    addrmodeStr = RenderEA(addrSpace, PC1, num_operands) + ",X";
                    break;
                case a.ZPY:
                case a.ABY:
                    addrmodeStr = RenderEA(addrSpace, PC1, num_operands) + ",Y";
                    break;
                case a.IDX:
                    addrmodeStr = "(" + RenderEA(addrSpace, PC1, num_operands) + ",X)";
                    break;
                case a.IDY:
                    addrmodeStr = "(" + RenderEA(addrSpace, PC1, num_operands) + "),Y";
                    break;
                case a.IND:
                    addrmodeStr = "(" + RenderEA(addrSpace, PC1, num_operands) + ")";
                    break;
                case a.IMM:
                    addrmodeStr = "#" + RenderEA(addrSpace, PC1, num_operands);
                    break;
                default:
                    // a.IMP, a.ACC
                    addrmodeStr = string.Empty;
                    break;
            }

            return string.Format("{0} {1}", MnemonicMatrix[addrSpace[PC]], addrmodeStr);
        }

        static int GetInstructionLength(AddressSpace addrSpace, ushort PC)
        {
            switch (AddressingModeMatrix[addrSpace[PC]])
            {
                case a.ACC:
                case a.IMP:
                    return 1;
                case a.REL:
                case a.ZPG:
                case a.ZPX:
                case a.ZPY:
                case a.IDX:
                case a.IDY:
                case a.IMM:
                    return 2;
                default:
                    return 3;
            }
        }

        static string RenderEA(AddressSpace addrSpace, ushort PC, int bytes)
        {
            var lsb = addrSpace[PC];
            var msb = (bytes == 2) ? addrSpace[(ushort)(PC + 1)] : (byte)0;
            var ea = (ushort)(lsb | (msb << 8));
            return string.Format((bytes == 1) ? "${0:x2}" : "${0:x4}", ea);
        }
    }
}