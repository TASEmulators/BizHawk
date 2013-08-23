using System.Text;

namespace BizHawk.Emulation.CPUs.M68000
{
    public sealed class DisassemblyInfo
    {
        public int PC;
        public string Mnemonic;
        public string Args;
        public string RawBytes;
        public int Length;

        public override string ToString()
        {
            return string.Format("{0:X6}: {3,-20}  {1,-8} {2}", PC, Mnemonic, Args, RawBytes);
        }
    }

    partial class MC68000
    {
        public DisassemblyInfo Disassemble(int pc)
        {
            var info = new DisassemblyInfo { Mnemonic = "UNKNOWN", PC = pc, Length = 2 };
            op = (ushort)ReadWord(pc);

                 if (Opcodes[op] == MOVE)   MOVE_Disasm(info);
            else if (Opcodes[op] == MOVEA)  MOVEA_Disasm(info);
            else if (Opcodes[op] == MOVEQ)  MOVEQ_Disasm(info);
            else if (Opcodes[op] == MOVEM0) MOVEM0_Disasm(info);
            else if (Opcodes[op] == MOVEM1) MOVEM1_Disasm(info);
            else if (Opcodes[op] == LEA)    LEA_Disasm(info);
            else if (Opcodes[op] == CLR)    CLR_Disasm(info);
            else if (Opcodes[op] == EXT)    EXT_Disasm(info);
            else if (Opcodes[op] == PEA)    PEA_Disasm(info);

            else if (Opcodes[op] == ANDI)   ANDI_Disasm(info);
            else if (Opcodes[op] == EORI)   EORI_Disasm(info);
            else if (Opcodes[op] == ORI)    ORI_Disasm(info);
            else if (Opcodes[op] == ASLd)   ASLd_Disasm(info);
            else if (Opcodes[op] == ASRd)   ASRd_Disasm(info);
            else if (Opcodes[op] == LSLd)   LSLd_Disasm(info);
            else if (Opcodes[op] == LSRd)   LSRd_Disasm(info);
            else if (Opcodes[op] == ROXLd)  ROXLd_Disasm(info);
            else if (Opcodes[op] == ROXRd)  ROXRd_Disasm(info);            
            else if (Opcodes[op] == ROLd)   ROLd_Disasm(info);
            else if (Opcodes[op] == RORd)   RORd_Disasm(info);
            else if (Opcodes[op] == SWAP)   SWAP_Disasm(info);
            else if (Opcodes[op] == AND0)   AND0_Disasm(info);
            else if (Opcodes[op] == AND1)   AND1_Disasm(info);
            else if (Opcodes[op] == EOR)    EOR_Disasm(info);
            else if (Opcodes[op] == OR0)    OR0_Disasm(info);
            else if (Opcodes[op] == OR1)    OR1_Disasm(info);
            else if (Opcodes[op] == NOT)    NOT_Disasm(info);
            else if (Opcodes[op] == NEG)    NEG_Disasm(info);

            else if (Opcodes[op] == JMP)    JMP_Disasm(info);
            else if (Opcodes[op] == JSR)    JSR_Disasm(info);
            else if (Opcodes[op] == Bcc)    Bcc_Disasm(info);
            else if (Opcodes[op] == BRA)    BRA_Disasm(info);
            else if (Opcodes[op] == BSR)    BSR_Disasm(info);
            else if (Opcodes[op] == DBcc)   DBcc_Disasm(info);
            else if (Opcodes[op] == Scc)    Scc_Disasm(info);
            else if (Opcodes[op] == RTE)    RTE_Disasm(info);
            else if (Opcodes[op] == RTS)    RTS_Disasm(info);
            else if (Opcodes[op] == RTR)    RTR_Disasm(info);
            else if (Opcodes[op] == TST)    TST_Disasm(info);
            else if (Opcodes[op] == BTSTi)  BTSTi_Disasm(info);
            else if (Opcodes[op] == BTSTr)  BTSTr_Disasm(info);
            else if (Opcodes[op] == BCHGi)  BCHGi_Disasm(info);
            else if (Opcodes[op] == BCHGr)  BCHGr_Disasm(info);
            else if (Opcodes[op] == BCLRi)  BCLRi_Disasm(info);
            else if (Opcodes[op] == BCLRr)  BCLRr_Disasm(info);
            else if (Opcodes[op] == BSETi)  BSETi_Disasm(info);
            else if (Opcodes[op] == BSETr)  BSETr_Disasm(info);
            else if (Opcodes[op] == LINK)   LINK_Disasm(info);
            else if (Opcodes[op] == UNLK)   UNLK_Disasm(info);
            else if (Opcodes[op] == NOP)    NOP_Disasm(info);

            else if (Opcodes[op] == ADD0)   ADD_Disasm(info);
            else if (Opcodes[op] == ADD1)   ADD_Disasm(info);
            else if (Opcodes[op] == ADDA)   ADDA_Disasm(info);
            else if (Opcodes[op] == ADDI)   ADDI_Disasm(info);
            else if (Opcodes[op] == ADDQ)   ADDQ_Disasm(info);
            else if (Opcodes[op] == SUB0)   SUB_Disasm(info);
            else if (Opcodes[op] == SUB1)   SUB_Disasm(info);
            else if (Opcodes[op] == SUBA)   SUBA_Disasm(info);
            else if (Opcodes[op] == SUBI)   SUBI_Disasm(info);
            else if (Opcodes[op] == SUBQ)   SUBQ_Disasm(info);
            else if (Opcodes[op] == CMP)    CMP_Disasm(info);
            else if (Opcodes[op] == CMPM)   CMPM_Disasm(info);
            else if (Opcodes[op] == CMPA)   CMPA_Disasm(info);

            else if (Opcodes[op] == CMPI)   CMPI_Disasm(info);
            else if (Opcodes[op] == MULU)   MULU_Disasm(info);
            else if (Opcodes[op] == MULS)   MULS_Disasm(info);
            else if (Opcodes[op] == DIVU)   DIVU_Disasm(info);
            else if (Opcodes[op] == DIVS)   DIVS_Disasm(info);

            else if (Opcodes[op] == MOVEtSR) MOVEtSR_Disasm(info);
            else if (Opcodes[op] == MOVEfSR) MOVEfSR_Disasm(info);
            else if (Opcodes[op] == MOVEUSP) MOVEUSP_Disasm(info);
            else if (Opcodes[op] == ANDI_SR) ANDI_SR_Disasm(info);
            else if (Opcodes[op] == EORI_SR) EORI_SR_Disasm(info);
            else if (Opcodes[op] == ORI_SR)  ORI_SR_Disasm(info);
            else if (Opcodes[op] == MOVECCR) MOVECCR_Disasm(info);
            else if (Opcodes[op] == TRAP)    TRAP_Disasm(info);

            var sb = new StringBuilder();
            for (int p = info.PC; p < info.PC + info.Length; p++)
                sb.AppendFormat("{0:X2}", ReadByte(p));
            info.RawBytes = sb.ToString();

            return info;
        }
    }
}
