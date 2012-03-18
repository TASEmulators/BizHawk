using System;

namespace BizHawk.Emulation.CPUs.M68000
{
    partial class MC68000
    {
        void MOVEtSR()
        {
            if (S == false)
                throw new Exception("Write to SR when not in supervisor mode. supposed to trap or something...");

            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;
            SR = ReadValueW(mode, reg);
            PendingCycles -= 12 + EACyclesBW[mode, reg];
        }

        void MOVEtSR_Disasm(DisassemblyInfo info)
        {
            int pc = info.PC + 2;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;
            info.Mnemonic = "move";
            info.Args = DisassembleValue(mode, reg, 2, ref pc) + ", SR";
            info.Length = pc - info.PC;
        }

        void MOVEfSR()
        {
            int mode = (op >> 3) & 7;
            int reg = (op >> 0) & 7;
            WriteValueW(mode, reg, (short) SR);
            PendingCycles -= (mode == 0) ? 6 : 8 + EACyclesBW[mode, reg];
        }

        void MOVEfSR_Disasm(DisassemblyInfo info)
        {
            int pc = info.PC + 2;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;
            info.Mnemonic = "move";
            info.Args = "SR, " + DisassembleValue(mode, reg, 2, ref pc);
            info.Length = pc - info.PC;
        }

        void MOVEUSP()
        {
            if (S == false)
                throw new Exception("MOVE to USP when not supervisor. needs to trap");

            int dir = (op >> 3) & 1;
            int reg = op & 7;

            if (dir == 0) usp = A[reg].s32;
            else A[reg].s32 = usp;

            PendingCycles -= 4;
        }

        void MOVEUSP_Disasm(DisassemblyInfo info)
        {
            int pc = info.PC + 2;
            int dir = (op >> 3) & 1;
            int reg = op & 7;
            info.Mnemonic = "move";
            info.Args = (dir == 0) ? ("A" + reg + ", USP") : ("USP, A" + reg);
            info.Length = pc - info.PC;
        }

        void ANDI_SR()
        {
            if (S == false)
                throw new Exception("trap!");
            SR &= ReadWord(PC); PC += 2;
            PendingCycles -= 20;
        }

        void ANDI_SR_Disasm(DisassemblyInfo info)
        {
            int pc = info.PC + 2;
            info.Mnemonic = "andi";
            info.Args = DisassembleImmediate(2, ref pc) + ", SR";
            info.Length = pc - info.PC;
        }

        void EORI_SR()
        {
            if (S == false)
                throw new Exception("trap!");
            SR ^= ReadWord(PC); PC += 2;
            PendingCycles -= 20;
        }

        void EORI_SR_Disasm(DisassemblyInfo info)
        {
            int pc = info.PC + 2;
            info.Mnemonic = "eori";
            info.Args = DisassembleImmediate(2, ref pc) + ", SR";
            info.Length = pc - info.PC;
        }

        void ORI_SR()
        {
            if (S == false)
                throw new Exception("trap!");
            SR |= ReadWord(PC); PC += 2;
            PendingCycles -= 20;
        }

        void ORI_SR_Disasm(DisassemblyInfo info)
        {
            int pc = info.PC + 2;
            info.Mnemonic = "ori";
            info.Args = DisassembleImmediate(2, ref pc) + ", SR";
            info.Length = pc - info.PC;
        }

        void MOVECCR()
        {
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;

            ushort sr = (ushort) (SR & 0xFF00);
            sr |= (byte)ReadValueB(mode, reg);
            SR = (short)sr;
            PendingCycles -= 12 + EACyclesBW[mode, reg];
        }

        void MOVECCR_Disasm(DisassemblyInfo info)
        {
            int pc   = info.PC + 2;
            int mode = (op >> 3) & 7;
            int reg  = (op >> 0) & 7;
            info.Mnemonic = "move";
            info.Args = DisassembleValue(mode, reg, 2, ref pc) + ", CCR";
            info.Length = pc - info.PC;
        }

        void TRAP()
        {
            int vector = 32 + (op & 0x0F);
            TrapVector(vector);
            PendingCycles -= 26;
        }

        void TRAP_Disasm(DisassemblyInfo info)
        {
            info.Mnemonic = "trap";
            info.Args = string.Format("${0:X}", op & 0xF);
        }

        void TrapVector(int vector)
        {
            short sr = (short)SR;        // capture current SR.
            S = true;                    // switch to supervisor mode, if not already in it.
            A[7].s32 -= 4;               // Push PC on stack
            WriteLong(A[7].s32, PC);
            A[7].s32 -= 2;               // Push SR on stack
            WriteWord(A[7].s32, sr);
            PC = ReadLong(vector * 4);   // Jump to vector
        }
    }
}