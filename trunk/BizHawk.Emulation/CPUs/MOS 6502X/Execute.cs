//http://nesdev.parodius.com/6502_cpu.txt
//TODO - correct brk/irq/nmi interrupting and prioritization
//TODO - rename unofficial NOPs as DOPs? (see immediate instr tests)
using System;

namespace BizHawk.Emulation.CPUs.M6502
{
	public partial class MOS6502X
	{
		static Uop[][] Microcode = new Uop[][]{
			//0x00
			/*BRK [implied]*/ new Uop[] { Uop.Fetch2, Uop.PushPCH, Uop.PushPCL, Uop.PushP_BRK, Uop.FetchPCLVector, Uop.FetchPCHVector, Uop.End },
			/*ORA (addr,X) [indexed indirect READ]*/ new Uop[] { Uop.Fetch2, Uop.IdxInd_Stage3, Uop.IdxInd_Stage4, Uop.IdxInd_Stage5, Uop.IdxInd_Stage6_READ_ORA, Uop.End },
			/*JAM*/ new Uop[] { Uop.End },
			/*SLO* (addr,X) [indexed indirect RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.End },
			/*NOP zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_NOP, Uop.End },
			/*ORA zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_ORA, Uop.End },
			/*ASL zp [zero page RMW]*/ new Uop[] { Uop.Fetch2, Uop.ZP_RMW_Stage3, Uop.ZP_RMW_ASL, Uop.ZP_RMW_Stage5, Uop.End },
			/*SLO* zp [zero page] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.Unsupported, Uop.End },
			/*PHP [implied]*/ new Uop[] { Uop.FetchDummy, Uop.PushP, Uop.End },
			/*ORA #nn [immediate]*/ new Uop[] { Uop.Imm_ORA, Uop.End },
			/*ASL A [accumulator]*/ new Uop[] { Uop.Imp_ASL_A, Uop.End },
			/*ANC** [immediate] [unofficial]*/ new Uop[] { Uop.Imm_Unsupported, Uop.End },
			/*NOP addr [absolute READ]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_NOP, Uop.End },
			/*ORA addr [absolute READ]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_ORA, Uop.End },
			/*ASL addr [absolute RMW]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_RMW_Stage4, Uop.Abs_RMW_Stage5_ASL, Uop.Abs_RMW_Stage6, Uop.End },
			/*SLO* addr [absolute RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_RMW_Stage4, Uop.Abs_RMW_Stage5_Unofficial, Uop.Abs_RMW_Stage6, Uop.End },
			//0x10
			/*BPL +/-rel*/ new Uop[] { Uop.RelBranch_Stage2_BPL, Uop.End },
			/*ORA (addr),Y* [indirect indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.IndIdx_Stage3, Uop.IndIdx_Stage4, Uop.IndIdx_READ_Stage5, Uop.IndIdx_READ_Stage6_ORA, Uop.End },
			/*JAM*/ new Uop[] { Uop.End },
			/*RLA (addr),Y* [indirect indexed RMW] [unofficial] */ new Uop[] { Uop.Fetch2, Uop.End },
			/*NOP zp,X [zero page indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_NOP, Uop.End },
			/*ORA zp,X [zero page indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_ORA, Uop.End },
			/*ASL zp,X [zero page indexed RMW]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZpIdx_RMW_Stage4, Uop.ZP_RMW_ASL, Uop.ZpIdx_RMW_Stage6, Uop.End },
			/*SLO* zp,X [zero page indexed RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZpIdx_RMW_Stage4, Uop.ZP_RMW_UNOFFICIAL, Uop.ZpIdx_RMW_Stage6, Uop.End },
			/*CLC [implied]*/ new Uop[] { Uop.Imp_CLC, Uop.End },
			/*ORA addr,Y* [absolute indexed READ Y]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_ORA, Uop.End },
			/*NOP 1A*/ new Uop[] { Uop.FetchDummy, Uop.End },
			/*SLO* addr,Y [absolute indexed RMW Y] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_Unofficial, Uop.AbsIdx_RMW_Stage7, Uop.End },
			/*NOP addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_NOP, Uop.End },
			/*ORA addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_ORA, Uop.End },
			/*ASL addr,X [absolute indexed RMW X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_ASL, Uop.AbsIdx_RMW_Stage7, Uop.End },
			/*SLO* addr,X [absolute indexed RMW X] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_Unofficial, Uop.AbsIdx_RMW_Stage7, Uop.End },
			//0x20
			/*JSR*/ new Uop[] { Uop.Fetch2, Uop.NOP, Uop.PushPCH, Uop.PushPCL, Uop.JSR, Uop.End },
			/*AND (addr,X) [indexed indirect READ]*/ new Uop[] { Uop.Fetch2, Uop.IdxInd_Stage3, Uop.IdxInd_Stage4, Uop.IdxInd_Stage5, Uop.IdxInd_Stage6_READ_AND, Uop.End },
			/*JAM*/ new Uop[] { Uop.End },
			/*RLA* (addr,X) [indexed indirect RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.End },
			/*BIT zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_BIT, Uop.End },
			/*AND zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_AND, Uop.End },
			/*ROL zp [zero page RMW]*/ new Uop[] { Uop.Fetch2, Uop.ZP_RMW_Stage3, Uop.ZP_RMW_ROL, Uop.ZP_RMW_Stage5, Uop.End },
			/*RLA* zp [zero page RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.ZP_RMW_Stage3, Uop.ZP_RMW_UNOFFICIAL, Uop.ZP_RMW_Stage5, Uop.End },
			/*PLP [implied] */ new Uop[] { Uop.FetchDummy,  Uop.IncS, Uop.PullP_NoInc, Uop.End_ISpecial },
			/*AND #nn [immediate]*/ new Uop[] { Uop.Imm_AND, Uop.End },
			/*ROL A [accumulator]*/ new Uop[] { Uop.Imp_ROL_A, Uop.End },
			/*ANC** [immediate] [unofficial]*/ new Uop[] { Uop.Imm_Unsupported, Uop.End },
			/*BIT addr [absolute]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_BIT, Uop.End },
			/*AND addr [absolute READ]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_AND, Uop.End },
			/*ROL addr [absolute RMW]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_RMW_Stage4, Uop.Abs_RMW_Stage5_ROL, Uop.Abs_RMW_Stage6, Uop.End },
			/*RLA* addr [absolute RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_RMW_Stage4, Uop.Abs_RMW_Stage5_Unofficial, Uop.Abs_RMW_Stage6, Uop.End },
			//0x30
			/*BMI +/-rel [relative]*/ new Uop[] { Uop.RelBranch_Stage2_BMI, Uop.End },
			/*AND (addr),Y* [indirect indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.IndIdx_Stage3, Uop.IndIdx_Stage4, Uop.IndIdx_READ_Stage5, Uop.IndIdx_READ_Stage6_AND, Uop.End },
			/*JAM*/ new Uop[] { Uop.End },
			/*RLA* (addr),Y* [indirect indexed RMW] [unofficial] */ new Uop[] { Uop.Fetch2, Uop.End },
			/*NOP zp,X [zero page indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_NOP, Uop.End },
			/*AND zp,X [zero page indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_AND, Uop.End },
			/*ROL zp,X [zero page indexed RMW]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZpIdx_RMW_Stage4, Uop.ZP_RMW_ROL, Uop.ZpIdx_RMW_Stage6, Uop.End },
			/*RLA* zp,X [zero page indexed RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZpIdx_RMW_Stage4, Uop.ZP_RMW_UNOFFICIAL, Uop.ZpIdx_RMW_Stage6, Uop.End },
			/*SEC [implied]*/ new Uop[] { Uop.Imp_SEC, Uop.End },
			/*AND addr,Y* [absolute indexed READ Y]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_AND, Uop.End },
			/*NOP 3A [implied]*/ new Uop[] { Uop.FetchDummy, Uop.End },
			/*RLA* addr,Y [absolute indexed RMW Y] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_Unofficial, Uop.AbsIdx_RMW_Stage7, Uop.End },
			/*NOP addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_NOP, Uop.End },
			/*AND addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_AND, Uop.End },
			/*ROL addr,X [absolute indexed RMW X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_ROL, Uop.AbsIdx_RMW_Stage7, Uop.End },
			/*RLA* addr,X [absolute indexed RMW X] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_Unofficial, Uop.AbsIdx_RMW_Stage7, Uop.End },
			//0x40
			/*RTI*/ new Uop[] { Uop.FetchDummy, Uop.IncS, Uop.PullP, Uop.PullPCL, Uop.PullPCH_NoInc, Uop.End },
			/*EOR (addr,X) [indexed indirect READ]*/ new Uop[] { Uop.Fetch2, Uop.IdxInd_Stage3, Uop.IdxInd_Stage4, Uop.IdxInd_Stage5, Uop.IdxInd_Stage6_READ_EOR, Uop.End },
			/*JAM*/ new Uop[] { Uop.End },
			/*SRE* (addr,X) [indexed indirect RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.End },
			/*NOP zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_NOP, Uop.End },
			/*EOR zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_EOR, Uop.End },
			/*LSR zp [zero page RMW]*/ new Uop[] { Uop.Fetch2, Uop.ZP_RMW_Stage3, Uop.ZP_RMW_LSR, Uop.ZP_RMW_Stage5, Uop.End },
			/*SRE* zp [zero page RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.ZP_RMW_Stage3, Uop.ZP_RMW_UNOFFICIAL, Uop.ZP_RMW_Stage5, Uop.End },
			/*PHA [implied]*/ new Uop[] { Uop.FetchDummy, Uop.PushA, Uop.End },
			/*EOR #nn [immediate]*/ new Uop[] { Uop.Imm_EOR, Uop.End },
			/*LSR A [accumulator]*/ new Uop[] { Uop.Imp_LSR_A, Uop.End },
			/*ASR** [immediate] [unofficial]*/ new Uop[] { Uop.Imm_Unsupported, Uop.End },
			/*JMP addr [absolute]*/ new Uop[] { Uop.Fetch2, Uop.JMP_abs, Uop.End },
			/*EOR addr [absolute READ]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_EOR, Uop.End },
			/*LSR addr [absolute RMW]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_RMW_Stage4, Uop.Abs_RMW_Stage5_LSR, Uop.Abs_RMW_Stage6, Uop.End },
			/*SRE* addr [absolute RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_RMW_Stage4, Uop.Abs_RMW_Stage5_Unofficial, Uop.Abs_RMW_Stage6, Uop.End },
			//0x50
			/*BVC +/-rel [relative]*/ new Uop[] { Uop.RelBranch_Stage2_BVC, Uop.End },
			/*EOR (addr),Y* [indirect indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.IndIdx_Stage3, Uop.IndIdx_Stage4, Uop.IndIdx_READ_Stage5, Uop.IndIdx_READ_Stage6_EOR, Uop.End },
			/*JAM*/ new Uop[] { Uop.End },
			/*SRE* (addr),Y* [indirect indexed RMW] [unofficial] */ new Uop[] { Uop.Fetch2, Uop.End },
			/*NOP zp,X [zero page indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_NOP, Uop.End },
			/*EOR zp,X [zero page indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_EOR, Uop.End },
			/*LSR zp,X [zero page indexed RMW]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZpIdx_RMW_Stage4, Uop.ZP_RMW_LSR, Uop.ZpIdx_RMW_Stage6, Uop.End },
			/*SRE* zp,X [zero page indexed RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZpIdx_RMW_Stage4, Uop.ZP_RMW_UNOFFICIAL, Uop.ZpIdx_RMW_Stage6, Uop.End },
			/*CLI [implied]*/ new Uop[] { Uop.Imp_CLI, Uop.End_ISpecial },
			/*EOR addr,Y* [absolute indexed READ Y]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_EOR, Uop.End },
			/*NOP 5A [implied]*/ new Uop[] { Uop.FetchDummy, Uop.End },
			/*SRE* addr,Y [absolute indexed RMW Y] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_Unofficial, Uop.AbsIdx_RMW_Stage7, Uop.End },
			/*NOP addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_NOP, Uop.End },
			/*EOR addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_EOR, Uop.End },
			/*LSR addr,X [absolute indexed RMW X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_LSR, Uop.AbsIdx_RMW_Stage7, Uop.End },
			/*SRE* addr,X [absolute indexed RMW X] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_Unofficial, Uop.AbsIdx_RMW_Stage7, Uop.End },
			//0x60
			/*RTS*/ new Uop[] { Uop.FetchDummy, Uop.IncS, Uop.PullPCL, Uop.PullPCH_NoInc, Uop.IncPC, Uop.End }, //can't fetch here because the PC isnt ready until the end of the last clock
			/*ADC (addr,X) [indexed indirect READ]*/ new Uop[] { Uop.Fetch2, Uop.IdxInd_Stage3, Uop.IdxInd_Stage4, Uop.IdxInd_Stage5, Uop.IdxInd_Stage6_READ_ADC, Uop.End },
			/*JAM*/ new Uop[] { Uop.End },
			/*RRA* (addr,X) [indexed indirect RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.End },
			/*NOP zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_NOP, Uop.End },
			/*ADC zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_ADC, Uop.End },
			/*ROR zp [zero page RMW]*/ new Uop[] { Uop.Fetch2, Uop.ZP_RMW_Stage3, Uop.ZP_RMW_ROR, Uop.ZP_RMW_Stage5, Uop.End },
			/*RRA* zp [zero page RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.ZP_RMW_Stage3, Uop.ZP_RMW_UNOFFICIAL, Uop.ZP_RMW_Stage5, Uop.End },
			/*PLA [implied]*/ new Uop[] { Uop.FetchDummy, Uop.IncS, Uop.PullA_NoInc, Uop.End },
			/*ADC #nn [immediate]*/ new Uop[] { Uop.Imm_ADC, Uop.End },
			/*ROR A [accumulator]*/ new Uop[] { Uop.Imp_ROR_A, Uop.End },
			/*ARR** [immediate] [unofficial]*/ new Uop[] { Uop.Imm_Unsupported, Uop.End },
			/*JMP (addr) [absolute indirect JMP]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.AbsInd_JMP_Stage4, Uop.AbsInd_JMP_Stage5, Uop.End },
			/*ADC addr [absolute READ]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_ADC, Uop.End },
			/*ROR addr [absolute RMW]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_RMW_Stage4, Uop.Abs_RMW_Stage5_ROR, Uop.Abs_RMW_Stage6, Uop.End },
			/*RRA* addr [absolute RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_RMW_Stage4, Uop.Abs_RMW_Stage5_Unofficial, Uop.Abs_RMW_Stage6, Uop.End },
			//0x70
			/*BVS +/-rel [relative]*/ new Uop[] { Uop.RelBranch_Stage2_BVS, Uop.End },
			/*ADC (addr),Y [indirect indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.IndIdx_Stage3, Uop.IndIdx_Stage4, Uop.IndIdx_READ_Stage5, Uop.IndIdx_READ_Stage6_ADC, Uop.End },
			/*JAM*/ new Uop[] { Uop.End },
			/*RRA* (addr),Y [indirect indexed RMW Y] [unofficial] */ new Uop[] { Uop.Fetch2, Uop.End },
			/*NOP zp,X [zero page indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_NOP, Uop.End },
			/*ADC zp,X [zero page indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_ADC, Uop.End },
			/*ROR zp,X [zero page indexed RMW]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZpIdx_RMW_Stage4, Uop.ZP_RMW_ROR, Uop.ZpIdx_RMW_Stage6, Uop.End },
			/*RRA* zp,X [zero page indexed RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZpIdx_RMW_Stage4, Uop.ZP_RMW_UNOFFICIAL, Uop.ZpIdx_RMW_Stage6, Uop.End },
			/*SEI [implied]*/ new Uop[] { Uop.Imp_SEI, Uop.End_ISpecial },
			/*ADC addr,Y* [absolute indexed READ Y]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_ADC, Uop.End },
			/*NOP 7A [implied]*/ new Uop[] { Uop.FetchDummy, Uop.End },
			/*RRA* addr,Y [absolute indexed RMW Y] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_Unofficial, Uop.AbsIdx_RMW_Stage7, Uop.End },
			/*NOP addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_NOP, Uop.End },
			/*ADC addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_ADC, Uop.End },
			/*ROR addr,X [absolute indexed RMW X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_ROR, Uop.AbsIdx_RMW_Stage7, Uop.End },
			/*RRA* addr,X [absolute indexed RMW X] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_Unofficial, Uop.AbsIdx_RMW_Stage7, Uop.End },
			//0x80
			/*NOP #nn [immediate]*/ new Uop[] { Uop.Imm_Unsupported, Uop.End },
			/*STA (addr,X) [indexed indirect WRITE]*/ new Uop[] { Uop.Fetch2, Uop.IdxInd_Stage3, Uop.IdxInd_Stage4, Uop.IdxInd_Stage5, Uop.IdxInd_Stage6_WRITE_STA, Uop.End },
			/*NOP #nn [immediate]*/ new Uop[] { Uop.Imm_Unsupported, Uop.End }, //jams very rarely
			/*SAX* (addr,X) [indexed indirect WRITE] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.End },
			/*STY zp [zero page WRITE]*/ new Uop[] { Uop.Fetch2, Uop.ZP_WRITE_STY, Uop.End },
			/*STA zp [zero page WRITE]*/ new Uop[] { Uop.Fetch2, Uop.ZP_WRITE_STA, Uop.End },
			/*STX zp [zero page WRITE]*/ new Uop[] { Uop.Fetch2, Uop.ZP_WRITE_STX, Uop.End },
			/*SAX* zp [zero page WRITE] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.ZP_WRITE_SAX, Uop.End },
			/*DEY [implied]*/ new Uop[] { Uop.Imp_DEY, Uop.End },
			/*NOP #nn [immediate]*/ new Uop[] { Uop.Imm_Unsupported, Uop.End },
			/*TXA [implied]*/ new Uop[] { Uop.Imp_TXA, Uop.End },
			/*ANE** [immediate] [unofficial]*/ new Uop[] { Uop.Imm_Unsupported, Uop.End },
			/*STY addr [absolute WRITE]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_WRITE_STY, Uop.End },
			/*STA addr [absolute WRITE]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_WRITE_STA, Uop.End },
			/*STX addr [absolute WRITE]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_WRITE_STX, Uop.End },
			/*SAX* addr [absolute WRITE] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_WRITE_Unofficial, Uop.End },
			//0x90
			/*BCC +/-rel [relative]*/ new Uop[] { Uop.RelBranch_Stage2_BCC, Uop.End },
			/*STA (addr),Y [indirect indexed WRITE]*/ new Uop[] { Uop.Fetch2, Uop.IndIdx_Stage3, Uop.IndIdx_Stage4, Uop.IndIdx_WRITE_Stage5, Uop.IndIdx_WRITE_Stage6_STA, Uop.End },
			/*JAM*/ new Uop[] { Uop.End },
			/*SHA** [indirect indexed RMW Y] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.End },
			/*STY zp,X [zero page indexed WRITE X]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_WRITE_STY, Uop.End },
			/*STA zp,X [zero page indexed WRITE X]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_WRITE_STA, Uop.End },
			/*STX zp,Y [zero page indexed WRITE Y]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_Y, Uop.ZP_WRITE_STX, Uop.End },
			/*SAX* zp,Y [zero page indexed WRITE Y] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_Y, Uop.ZP_WRITE_UNOFFICIAL, Uop.End },
			/*TYA [implied]*/ new Uop[] { Uop.Imp_TYA, Uop.End },
			/*STA addr,Y [absolute indexed WRITE]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_Stage4, Uop.AbsIdx_WRITE_Stage5_STA, Uop.End },
			/*TXS [implied]*/ new Uop[] { Uop.Imp_TXS, Uop.End },
			/*SHS* addr,X [absolute indexed READ? X] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.End },
			/*SHY** [absolute indexed READ?] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.End },
			/*STA addr,X [absolute indexed WRITE]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_Stage4, Uop.AbsIdx_WRITE_Stage5_STA, Uop.End },
			/*SHX* addr,Y [absolute indexed WRITE? Y] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_Stage4, Uop.AbsIdx_WRITE_Stage5_Unofficial, Uop.End },
			/*SHA* addr,Y [absolute indexed WRITE? Y] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_Stage4, Uop.AbsIdx_WRITE_Stage5_Unofficial, Uop.End },
			//0xA0
			/*LDY #nn [immediate]*/ new Uop[] { Uop.Imm_LDY, Uop.End },
			/*LDA (addr,X) [indexed indirect READ]*/ new Uop[] { Uop.Fetch2, Uop.IdxInd_Stage3, Uop.IdxInd_Stage4, Uop.IdxInd_Stage5, Uop.IdxInd_Stage6_READ_LDA, Uop.End },
			/*LDX #nn [immediate]*/ new Uop[] { Uop.Imm_LDX, Uop.End },
			/*LAX* (addr,X) [indexed indirect READ] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.End },
			/*LDY zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_LDY, Uop.End },
			/*LDA zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_LDA, Uop.End },
			/*LDX zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_LDX, Uop.End },
			/*LAX* zp [zero page READ] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_LAX, Uop.End },
			/*TAY [implied]*/ new Uop[] { Uop.Imp_TAY, Uop.End },
			/*LDA #nn [immediate]*/ new Uop[] { Uop.Imm_LDA, Uop.End },
			/*TAX [implied]*/ new Uop[] { Uop.Imp_TAX, Uop.End },
			/*LXA** [immediate] [unofficial]*/ new Uop[] { Uop.Imm_Unsupported, Uop.End },
			/*LDY addr [absolute READ]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_LDY, Uop.End },
			/*LDA addr [absolute READ]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_LDA, Uop.End },
			/*LDX addr [absolute READ]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_LDX, Uop.End },
			/*LAX* addr [absolute READ] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_Unofficial, Uop.End },
			//0xB0
			/*BCS +/-rel [relative]*/ new Uop[] { Uop.RelBranch_Stage2_BCS, Uop.End },
			/*LDA (addr),Y* [indirect indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.IndIdx_Stage3, Uop.IndIdx_Stage4, Uop.IndIdx_READ_Stage5, Uop.IndIdx_READ_Stage6_LDA, Uop.End },
			/*JAM*/ new Uop[] { Uop.End },
			/*LAX* (addr),Y* [indirect indexed READ Y] [unofficial] */ new Uop[] { Uop.Fetch2, Uop.End },
			/*LDY zp,X [zero page indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_LDY, Uop.End },
			/*LDA zp,X [zero page indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_LDA, Uop.End },
			/*LDX zp,Y [zero page indexed READ Y]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_Y, Uop.ZP_READ_LDX, Uop.End },
			/*LAX* zp,Y [zero page indexed READ] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_UNOFFICIAL, Uop.End },
			/*CLV [implied]*/ new Uop[] { Uop.Imp_CLV, Uop.End },
			/*LDA addr,Y* [absolute indexed READ Y]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_LDA, Uop.End },
			/*TSX [implied]*/ new Uop[] { Uop.Imp_TSX, Uop.End },
			/*LAS* addr,X [absolute indexed READ? X] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.End },
			/*LDY addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_LDY, Uop.End },
			/*LDA addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_LDA, Uop.End },
			/*LDX addr,Y* [absolute indexed READ Y]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_LDX, Uop.End },
			/*LAX* addr,Y [absolute indexed READ?] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.End },
			//0xC0
			/*CPY #nn [immediate]*/ new Uop[] { Uop.Imm_CPY, Uop.End },
			/*CMP (addr,X) [indexed indirect READ]*/ new Uop[] { Uop.Fetch2, Uop.IdxInd_Stage3, Uop.IdxInd_Stage4, Uop.IdxInd_Stage5, Uop.IdxInd_Stage6_READ_CMP, Uop.End },
			/*NOP #nn [immediate]*/ new Uop[] { Uop.Imm_Unsupported, Uop.End }, //jams very rarely
			/*DCP* (addr,X) [indexed indirect RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.End },
			/*CPY zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_CPY, Uop.End },
			/*CMP zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_CMP, Uop.End },
			/*DEC zp [zero page RMW]*/ new Uop[] { Uop.Fetch2, Uop.ZP_RMW_Stage3, Uop.ZP_RMW_DEC, Uop.ZP_RMW_Stage5, Uop.End },
			/*DCP* zp [zero page RMW] [unofficial]*/ new Uop[]  { Uop.Fetch2, Uop.ZP_RMW_Stage3, Uop.ZP_RMW_UNOFFICIAL, Uop.ZP_RMW_Stage5, Uop.End },
			/*INY [implied]*/ new Uop[] { Uop.Imp_INY, Uop.End },
			/*CMP #nn [immediate]*/ new Uop[] { Uop.Imm_CMP, Uop.End },
			/*DEX  [implied]*/ new Uop[] { Uop.Imp_DEX, Uop.End },
			/*SBX** [immediate] [unofficial]*/ new Uop[] { Uop.Imm_Unsupported, Uop.End },
			/*CPY addr [absolute READ]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_CPY, Uop.End },
			/*CMP addr [absolute READ]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_CMP, Uop.End },
			/*DEC addr [absolute RMW]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_RMW_Stage4, Uop.Abs_RMW_Stage5_DEC, Uop.Abs_RMW_Stage6, Uop.End },
			/*DCP* addr [absolute RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_RMW_Stage4, Uop.Abs_RMW_Stage5_Unofficial, Uop.Abs_RMW_Stage6, Uop.End },
			//0xD0
			/*BNE +/-rel [relative]*/ new Uop[] { Uop.RelBranch_Stage2_BNE, Uop.End },
			/*CMP (addr),Y* [indirect indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.IndIdx_Stage3, Uop.IndIdx_Stage4, Uop.IndIdx_READ_Stage5, Uop.IndIdx_READ_Stage6_CMP, Uop.End },
			/*JAM*/ new Uop[] { Uop.End },
			/*DCP* (addr),Y* [indirect indexed RMW Y] [unofficial] */ new Uop[] { Uop.Fetch2, Uop.End },
			/*NOP zp,X [zero page indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_NOP, Uop.End },
			/*CMP zp,X [zero page indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_CMP, Uop.End },
			/*DEC zp,X [zero page indexed RMW X]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZpIdx_RMW_Stage4, Uop.ZP_RMW_DEC, Uop.ZpIdx_RMW_Stage6, Uop.End },
			/*DCP* zp,X [zero page indexed RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZpIdx_RMW_Stage4, Uop.ZP_RMW_UNOFFICIAL, Uop.ZpIdx_RMW_Stage6, Uop.End },
			/*CLD [implied]*/ new Uop[] { Uop.Imp_CLD, Uop.End },
			/*CMP addr,Y* [absolute indexed READ Y]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_CMP, Uop.End },
			/*NOP DA [implied]*/ new Uop[] { Uop.FetchDummy, Uop.End },
			/*DCP* addr,Y [absolute indexed RMW Y] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_Unofficial, Uop.AbsIdx_RMW_Stage7, Uop.End },
			/*NOP addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_NOP, Uop.End },
			/*CMP addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_CMP, Uop.End },
			/*DEC addr,X [absolute indexed RMW X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_DEC, Uop.AbsIdx_RMW_Stage7, Uop.End },
			/*DCP* addr,X [absolute indexed RMW X] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_Unofficial, Uop.AbsIdx_RMW_Stage7, Uop.End },
			//0xE0
			/*CPX #nn [immediate]*/ new Uop[] { Uop.Imm_CPX, Uop.End },
			/*SBC (addr,X) [indirect indexed]*/ new Uop[] { Uop.Fetch2, Uop.IdxInd_Stage3, Uop.IdxInd_Stage4, Uop.IdxInd_Stage5, Uop.IdxInd_Stage6_READ_SBC, Uop.End },
			/*NOP #nn [immediate]*/ new Uop[] { Uop.Imm_Unsupported, Uop.End }, //jams very rarely
			/*ISB* (addr,X) [indexed indirect RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.End },
			/*CPX zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_CPX, Uop.End },
			/*SBC zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_SBC, Uop.End },
			/*INC zp [zero page RMW]*/ new Uop[] { Uop.Fetch2, Uop.ZP_RMW_Stage3, Uop.ZP_RMW_INC, Uop.ZP_RMW_Stage5, Uop.End },
			/*ISB* zp [zero page RMW] [unofficial]*/ new Uop[]  { Uop.Fetch2, Uop.ZP_RMW_Stage3, Uop.ZP_RMW_UNOFFICIAL, Uop.ZP_RMW_Stage5, Uop.End },
			/*INX [implied]*/ new Uop[] { Uop.Imp_INX, Uop.End },
			/*SBC #nn [immediate READ]*/ new Uop[] { Uop.Imm_SBC, Uop.End },
			/*NOP EA [implied]*/ new Uop[] { Uop.FetchDummy, Uop.End }, //nothing happened here.. but the last thing to happen was a fetch, so we can't pipeline the next fetch
			/*ISB #nn [immediate READ]*/ new Uop[] { Uop.Imm_SBC, Uop.End },
			/*CPX addr [absolute READ]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_CPX, Uop.End },
			/*SBC addr [absolute READ]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_SBC, Uop.End },
			/*INC addr [absolute RMW]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_RMW_Stage4, Uop.Abs_RMW_Stage5_INC, Uop.Abs_RMW_Stage6, Uop.End },
			/*ISB* addr [absolute RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_RMW_Stage4, Uop.Abs_RMW_Stage5_Unofficial, Uop.Abs_RMW_Stage6, Uop.End },
			//0xF0
			/*BEQ +/-rel [relative]*/ new Uop[] { Uop.RelBranch_Stage2_BEQ, Uop.End },
			/*SBC (addr),Y* [indirect indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.IndIdx_Stage3, Uop.IndIdx_Stage4, Uop.IndIdx_READ_Stage5, Uop.IndIdx_READ_Stage6_SBC, Uop.End },
			/*JAM*/ new Uop[] { Uop.End },
			/*ISB* (addr),Y* [indirect indexed RMW Y] [unofficial] */ new Uop[] { Uop.Fetch2, Uop.End },
			/*NOP zp,X [zero page indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_NOP, Uop.End },
			/*SBC zp,X [zero page indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_SBC, Uop.End },
			/*INC zp,X [zero page indexed RMW X]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZpIdx_RMW_Stage4, Uop.ZP_RMW_INC, Uop.ZpIdx_RMW_Stage6, Uop.End },
			/*ISB* zp,X [zero page indexed RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZpIdx_RMW_Stage4, Uop.ZP_RMW_UNOFFICIAL, Uop.ZpIdx_RMW_Stage6, Uop.End },
			/*SED [implied]*/ new Uop[] { Uop.Imp_SED, Uop.End },
			/*SBC addr,Y* [absolute indexed READ Y]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_SBC, Uop.End },
			/*NOP FA [implied]*/ new Uop[] { Uop.FetchDummy, Uop.End },
			/*ISB* addr,Y [absolute indexed RMW Y] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_Unofficial, Uop.AbsIdx_RMW_Stage7, Uop.End },
			/*NOP addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_NOP, Uop.End },
			/*SBC addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_SBC, Uop.End },
			/*INC addr,X [absolute indexed RMW X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_INC, Uop.AbsIdx_RMW_Stage7, Uop.End },
			/*ISB* addr,X [absolute indexed RMW X] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_Unofficial, Uop.AbsIdx_RMW_Stage7, Uop.End },
			//0x100
			/*VOP_Fetch1*/ new Uop[] { Uop.Fetch1 },
			/*VOP_RelativeStuff*/ new Uop[] { Uop.RelBranch_Stage3, Uop.End },
			/*VOP_RelativeStuff2*/ new Uop[] { Uop.RelBranch_Stage4, Uop.End },
			//i assume these are dummy fetches.... maybe theyre just nops? supposedly these take 7 cycles so thats the only way i can make sense of it
			//one of them might be the next instruction's fetch, and whatever fetch follows it.
			//the interrupt would then take place if necessary, using a cached PC. but im not so sure about that.
			/*VOP_NMI*/ new Uop[] { Uop.FetchDummy, Uop.FetchDummy, Uop.PushPCH, Uop.PushPCL, Uop.PushP_NMI, Uop.FetchPCLVector, Uop.FetchPCHVector, Uop.End },
			/*VOP_IRQ*/ new Uop[] { Uop.FetchDummy, Uop.FetchDummy, Uop.PushPCH, Uop.PushPCL, Uop.PushP_IRQ, Uop.FetchPCLVector, Uop.FetchPCHVector, Uop.End },
			/*VOP_RESET*/ new Uop[] { Uop.FetchDummy, Uop.FetchDummy, Uop.PushDummy, Uop.PushDummy, Uop.PushP_Reset, Uop.FetchPCLVector, Uop.FetchPCHVector, Uop.End },
		};
		
		enum Uop
		{
			//sometimes i used this as a marker for unsupported instructions, but it is very inconsistent
			Unsupported,

			Fetch1, Fetch2, Fetch3, 
			//used by instructions with no second opcode byte (6502 fetches a byte anyway but won't increment PC for these)
			FetchDummy,

			NOP,

			JSR,
			IncPC, //from RTS

			//[absolute WRITE]
			Abs_WRITE_STA, Abs_WRITE_STX, Abs_WRITE_STY,
			Abs_WRITE_Unofficial,
			//[absolute READ]
			Abs_READ_BIT, Abs_READ_LDA, Abs_READ_LDY, Abs_READ_ORA, Abs_READ_LDX, Abs_READ_CMP, Abs_READ_ADC, Abs_READ_CPX, Abs_READ_SBC, Abs_READ_AND, Abs_READ_EOR, Abs_READ_CPY, Abs_READ_NOP,
			Abs_READ_Unofficial,
			//[absolute RMW]
			Abs_RMW_Stage4, Abs_RMW_Stage6,
			Abs_RMW_Stage5_INC, Abs_RMW_Stage5_DEC, Abs_RMW_Stage5_LSR, Abs_RMW_Stage5_ROL, Abs_RMW_Stage5_ASL, Abs_RMW_Stage5_ROR, 
			Abs_RMW_Stage5_Unofficial,

			//[absolute JUMP]
			JMP_abs,

			//[zero page misc]
			ZpIdx_Stage3_X, ZpIdx_Stage3_Y,
			ZpIdx_RMW_Stage4, ZpIdx_RMW_Stage6,
			//[zero page WRITE]
			ZP_WRITE_STA, ZP_WRITE_STX, ZP_WRITE_STY, ZP_WRITE_SAX,
			ZP_WRITE_UNOFFICIAL,
			//[zero page RMW]
			ZP_RMW_Stage3, ZP_RMW_Stage5,
			ZP_RMW_DEC, ZP_RMW_INC, ZP_RMW_ASL, ZP_RMW_LSR, ZP_RMW_ROR, ZP_RMW_ROL,
			ZP_RMW_UNOFFICIAL,
			//[zero page READ]
			ZP_READ_EOR, ZP_READ_BIT, ZP_READ_ORA, ZP_READ_LDA, ZP_READ_LDY, ZP_READ_LDX, ZP_READ_CPX, ZP_READ_SBC, ZP_READ_CPY, ZP_READ_NOP, ZP_READ_ADC, ZP_READ_AND, ZP_READ_CMP, ZP_READ_LAX,
			ZP_READ_UNOFFICIAL,

			//[indexed indirect READ] (addr,X)
			//[indexed indirect WRITE] (addr,X)
			IdxInd_Stage3, IdxInd_Stage4, IdxInd_Stage5,
			IdxInd_Stage6_READ_ORA, IdxInd_Stage6_READ_SBC, IdxInd_Stage6_READ_LDA, IdxInd_Stage6_READ_EOR, IdxInd_Stage6_READ_CMP, IdxInd_Stage6_READ_ADC, IdxInd_Stage6_READ_AND, 
			IdxInd_Stage6_WRITE_STA,

			//[absolute indexed]
			AbsIdx_Stage3_X, AbsIdx_Stage3_Y, AbsIdx_Stage4,
			//[absolute indexed WRITE]
			AbsIdx_WRITE_Stage5_STA, 
			AbsIdx_WRITE_Stage5_Unofficial,
			//[absolute indexed READ]
			AbsIdx_READ_Stage4,
			AbsIdx_READ_Stage5_LDA, AbsIdx_READ_Stage5_CMP, AbsIdx_READ_Stage5_SBC, AbsIdx_READ_Stage5_ADC, AbsIdx_READ_Stage5_EOR, AbsIdx_READ_Stage5_LDX, AbsIdx_READ_Stage5_AND, AbsIdx_READ_Stage5_ORA, AbsIdx_READ_Stage5_LDY, AbsIdx_READ_Stage5_NOP,
			//[absolute indexed RMW]
			AbsIdx_RMW_Stage5, AbsIdx_RMW_Stage7,
			AbsIdx_RMW_Stage6_ROR, AbsIdx_RMW_Stage6_DEC, AbsIdx_RMW_Stage6_INC, AbsIdx_RMW_Stage6_ASL, AbsIdx_RMW_Stage6_LSR, AbsIdx_RMW_Stage6_ROL,
			AbsIdx_RMW_Stage6_Unofficial,

			IncS, DecS,
			PushPCL, PushPCH, PushPCH_B, PushP, PullP, PullPCL, PullPCH_NoInc, PushA, PullA_NoInc, PullP_NoInc,
			PushP_BRK, PushP_NMI, PushP_IRQ, PushP_Reset, PushDummy,
			FetchPCLVector, FetchPCHVector, //todo - may not need these ?? can reuse fetch2 and fetch3?

			//[implied] and [accumulator]
			Imp_ASL_A, Imp_ROL_A, Imp_ROR_A, Imp_LSR_A,
			Imp_SEC, Imp_CLI, Imp_SEI, Imp_CLD, Imp_CLC, Imp_CLV, Imp_SED,
			Imp_INY, Imp_DEY, Imp_INX, Imp_DEX,
			Imp_TSX, Imp_TXS, Imp_TAX, Imp_TAY, Imp_TYA, Imp_TXA, 
			
			//[immediate]
			Imm_CMP, Imm_ADC, Imm_AND, Imm_SBC, Imm_ORA, Imm_EOR, Imm_CPY, Imm_CPX,
			Imm_LDA, Imm_LDX, Imm_LDY,
			Imm_Unsupported,

			//sub-ops
			NZ_X, NZ_Y, NZ_A,
			RelBranch_Stage2_BNE, RelBranch_Stage2_BPL, RelBranch_Stage2_BCC, RelBranch_Stage2_BCS, RelBranch_Stage2_BEQ, RelBranch_Stage2_BMI, RelBranch_Stage2_BVC, RelBranch_Stage2_BVS,
			RelBranch_Stage2, RelBranch_Stage3, RelBranch_Stage4,
			_Eor, _Bit, _Cpx, _Cpy, _Cmp, _Adc, _Sbc, _Ora, _And, //alu-related sub-ops

			//JMP (addr) 0x6C
			AbsInd_JMP_Stage4, AbsInd_JMP_Stage5,

			//[indirect indexed] (i.e. LDA (addr),Y	
			IndIdx_Stage3, IndIdx_Stage4, IndIdx_READ_Stage5, IndIdx_WRITE_Stage5,
			IndIdx_WRITE_Stage6_STA,
			IndIdx_READ_Stage6_LDA, IndIdx_READ_Stage6_CMP, IndIdx_READ_Stage6_ORA, IndIdx_READ_Stage6_SBC, IndIdx_READ_Stage6_ADC, IndIdx_READ_Stage6_AND, IndIdx_READ_Stage6_EOR,

			End,
			End_ISpecial, //same as end, but preserves the iflag set by the instruction
		}

		const int VOP_Fetch1 = 256;
		const int VOP_RelativeStuff = 257;
		const int VOP_RelativeStuff2 = 258;
		const int VOP_NMI = 259;
		const int VOP_IRQ = 260;
		const int VOP_RESET = 261;

		int opcode;
		byte opcode2, opcode3; //opcode bytes.. theoretically redundant with the temp variables? who knows.
		int ea, alu_temp; //cpu internal temp variables
		int mi; //microcode index
		//bool branch_taken; //only needed for the timing debug
		bool iflag_pending; //iflag must be stored after it is checked in some cases (CLI and SEI).

		void FetchDummy()
		{
			DummyReadMemory(PC);
		}

		////timing debug
		//int ctr = 0;
		//int realOpcode = 0;
		//public static byte[] CycTable = new byte[]
		//{                             
		///*0x00*/ 7,6,2,8,3,3,5,5,3,2,2,2,4,4,6,6,
		///*0x10*/ 2,5,2,8,4,4,6,6,2,4,2,7,4,4,7,7,
		///*0x20*/ 6,6,2,8,3,3,5,5,4,2,2,2,4,4,6,6,
		///*0x30*/ 2,5,2,8,4,4,6,6,2,4,2,7,4,4,7,7,
		///*0x40*/ 6,6,2,8,3,3,5,5,3,2,2,2,3,4,6,6,
		///*0x50*/ 2,5,2,8,4,4,6,6,2,4,2,7,4,4,7,7,
		///*0x60*/ 6,6,2,8,3,3,5,5,4,2,2,2,5,4,6,6,
		///*0x70*/ 2,5,2,8,4,4,6,6,2,4,2,7,4,4,7,7,
		///*0x80*/ 2,6,2,6,3,3,3,3,2,2,2,2,4,4,4,4,
		///*0x90*/ 2,6,2,6,4,4,4,4,2,5,2,5,5,5,5,5,
		///*0xA0*/ 2,6,2,6,3,3,3,3,2,2,2,2,4,4,4,4,
		///*0xB0*/ 2,5,2,5,4,4,4,4,2,4,2,4,4,4,4,4,
		///*0xC0*/ 2,6,2,8,3,3,5,5,2,2,2,2,4,4,6,6,
		///*0xD0*/ 2,5,2,8,4,4,6,6,2,4,2,7,4,4,7,7,
		///*0xE0*/ 2,6,3,8,3,3,5,5,2,2,2,2,4,4,6,6,
		///*0xF0*/ 2,5,2,8,4,4,6,6,2,4,2,7,4,4,7,7,
		//};

		public void Execute(int cycles)
		{
			for (int i = 0; i < cycles; i++)
			{
				ExecuteOne();
			}
		}

		public void ExecuteOne()
		{
			byte value8, temp8;
			ushort value16;
			bool branch_taken = false;

			TotalExecutedCycles++;

		RETRY:
			Uop uop = Microcode[opcode][mi];
			switch (uop)
			{
				default: throw new InvalidOperationException();
				case Uop.Fetch1:
					{
						bool my_iflag = FlagI;
						FlagI = iflag_pending;
						if (NMI)
						{
							ea = NMIVector;
							opcode = VOP_NMI;
							NMI = false;
							mi = 0;
							goto RETRY;
						}
						else if (IRQ && !my_iflag)
						{
							ea = IRQVector;
							opcode = VOP_IRQ;
							mi = 0;
							goto RETRY;
						}
#if TIMINGDEBUG
						if (debug)
						{
							int ideal = CycTable[realOpcode] + (branch_taken ? 1 : 0);
							int actual = TotalExecutedCycles - ctr;
							Console.Write(" | ideal={0}", ideal);
							Console.Write(" actual={0}", actual);
							if (actual != ideal) Console.WriteLine(" !!!"); else Console.WriteLine();
							Console.Write(State());
						}
						branch_taken = false;
						opcode = ReadMemory(PC++);
						realOpcode = opcode;
						mi = -1;
						ctr = TotalExecutedCycles;
						break;
#else
						if(debug) Console.WriteLine(State());
						opcode = ReadMemory(PC++);
						mi = -1;
						break;
#endif
					}
					
				case Uop.Fetch2: opcode2 = ReadMemory(PC++); break;
				case Uop.Fetch3: opcode3 = ReadMemory(PC++); break;
				case Uop.FetchDummy: FetchDummy(); break;

				case Uop.PushPCH: WriteMemory((ushort)(S-- + 0x100), (byte)(PC >> 8)); break;
				case Uop.PushPCL: WriteMemory((ushort)(S-- + 0x100), (byte)PC); break;
				case Uop.PushP_BRK:
					FlagB = true;
					WriteMemory((ushort)(S-- + 0x100), P);
					FlagI = true;
					ea = BRKVector;
					break;
				case Uop.PushP_IRQ:
					FlagB = false;
					WriteMemory((ushort)(S-- + 0x100), P);
					FlagI = true;
					ea = IRQVector;
					break;
				case Uop.PushP_NMI:
					FlagB = false;
					WriteMemory((ushort)(S-- + 0x100), P);
					FlagI = true; //is this right?
					ea = NMIVector;
					break;
				case Uop.PushP_Reset:
					ea = ResetVector;
					S--;
					FlagI = true;
					break;
				case Uop.PushDummy:
					S--;
					break;
				case Uop.FetchPCLVector:
					alu_temp = ReadMemory((ushort)ea);
					break;
				case Uop.FetchPCHVector:
					alu_temp += ReadMemory((ushort)(ea + 1)) << 8;
					PC = (ushort)alu_temp;
					break;


				case Uop.Imp_INY: FetchDummy(); Y++; goto case Uop.NZ_Y;
				case Uop.Imp_DEY: FetchDummy(); Y--; goto case Uop.NZ_Y;
				case Uop.Imp_INX: FetchDummy(); X++; goto case Uop.NZ_X;
				case Uop.Imp_DEX: FetchDummy(); X--; goto case Uop.NZ_X;

				case Uop.NZ_A: P = (byte)((P & 0x7D) | TableNZ[A]); break;
				case Uop.NZ_X: P = (byte)((P & 0x7D) | TableNZ[X]); break;
				case Uop.NZ_Y: P = (byte)((P & 0x7D) | TableNZ[Y]); break;

				case Uop.Imp_TSX: FetchDummy(); X = S; goto case Uop.NZ_X;
				case Uop.Imp_TXS: FetchDummy(); S = X; break;
				case Uop.Imp_TAX: FetchDummy(); X = A; goto case Uop.NZ_X;
				case Uop.Imp_TAY: FetchDummy(); Y = A; goto case Uop.NZ_Y;
				case Uop.Imp_TYA: FetchDummy(); A = Y; goto case Uop.NZ_A;
				case Uop.Imp_TXA: FetchDummy(); A = X; goto case Uop.NZ_A;

				case Uop.Imp_SEI: FetchDummy(); iflag_pending = true; break;
				case Uop.Imp_CLI: FetchDummy(); iflag_pending = false; break;
				case Uop.Imp_SEC: FetchDummy(); FlagC = true; break;
				case Uop.Imp_CLC: FetchDummy(); FlagC = false; break;
				case Uop.Imp_SED: FetchDummy(); FlagD = true; break;
				case Uop.Imp_CLD: FetchDummy(); FlagD = false; break;
				case Uop.Imp_CLV: FetchDummy(); FlagV = false; break;

				case Uop.Abs_WRITE_STA: WriteMemory((ushort)((opcode3 << 8) + opcode2), A); break;
				case Uop.Abs_WRITE_STX: WriteMemory((ushort)((opcode3 << 8) + opcode2), X); break;
				case Uop.Abs_WRITE_STY: WriteMemory((ushort)((opcode3 << 8) + opcode2), Y); break;
				case Uop.Abs_WRITE_Unofficial: 
					WriteMemory((ushort)((opcode3 << 8) + opcode2), 0); 
					break;

				case Uop.ZP_WRITE_STA: WriteMemory(opcode2, A); break;
				case Uop.ZP_WRITE_STY: WriteMemory(opcode2, Y); break;
				case Uop.ZP_WRITE_STX: WriteMemory(opcode2, X); break;
				case Uop.ZP_WRITE_SAX: WriteMemory(opcode2, (byte)(X & A)); break;

				case Uop.IndIdx_Stage3:
					ea = ReadMemory(opcode2);
					break;
				case Uop.IndIdx_Stage4:
					alu_temp = ea + Y;
					ea = (ReadMemory((byte)(opcode2+1))<<8) 
						| ((alu_temp&0xFF));
					break;
				case Uop.IndIdx_WRITE_Stage5:
					ReadMemory((ushort)ea);
					ea += (alu_temp >> 8) << 8;
					break;
				case Uop.IndIdx_READ_Stage5:
					if (!alu_temp.Bit(8))
					{
						mi++;
						goto RETRY;
					}
					else
					{
						ReadMemory((ushort)ea);
						ea = (ushort)(ea + 0x100);
					}
					break;
				case Uop.IndIdx_WRITE_Stage6_STA:
					WriteMemory((ushort)ea, A);
					break;
				case Uop.IndIdx_READ_Stage6_LDA:
					A = ReadMemory((ushort)ea);
					goto case Uop.NZ_A;
				case Uop.IndIdx_READ_Stage6_CMP:
					alu_temp = ReadMemory((ushort)ea);
					goto case Uop._Cmp;
				case Uop.IndIdx_READ_Stage6_AND:
					alu_temp = ReadMemory((ushort)ea);
					goto case Uop._And;
				case Uop.IndIdx_READ_Stage6_EOR:
					alu_temp = ReadMemory((ushort)ea);
					goto case Uop._Eor;
				case Uop.IndIdx_READ_Stage6_ADC:
					alu_temp = ReadMemory((ushort)ea);
					goto case Uop._Adc;
				case Uop.IndIdx_READ_Stage6_SBC:
					alu_temp = ReadMemory((ushort)ea);
					goto case Uop._Sbc;
				case Uop.IndIdx_READ_Stage6_ORA:
					alu_temp = ReadMemory((ushort)ea);
					goto case Uop._Ora;

				case Uop.RelBranch_Stage2_BVS:
					branch_taken = FlagV == true;
					goto case Uop.RelBranch_Stage2;
				case Uop.RelBranch_Stage2_BVC:
					branch_taken = FlagV == false;
					goto case Uop.RelBranch_Stage2;
				case Uop.RelBranch_Stage2_BMI:
					branch_taken = FlagN == true;
					goto case Uop.RelBranch_Stage2;
				case Uop.RelBranch_Stage2_BPL:
					branch_taken = FlagN == false;
					goto case Uop.RelBranch_Stage2;
				case Uop.RelBranch_Stage2_BCS:
					branch_taken = FlagC == true;
					goto case Uop.RelBranch_Stage2;
				case Uop.RelBranch_Stage2_BCC:
					branch_taken = FlagC == false;
					goto case Uop.RelBranch_Stage2;
				case Uop.RelBranch_Stage2_BEQ:
					branch_taken = FlagZ == true;
					goto case Uop.RelBranch_Stage2;
				case Uop.RelBranch_Stage2_BNE:
					branch_taken = FlagZ == false;
					goto case Uop.RelBranch_Stage2;
					
				case Uop.RelBranch_Stage2:
					opcode2 = ReadMemory(PC++);
					if (branch_taken)
					{
						//if the branch is taken, we enter a different bit of microcode to calculate the PC and complete the branch
						opcode = VOP_RelativeStuff;
						mi = -1;
					}
					break;
				case Uop.RelBranch_Stage3:
					FetchDummy();
					alu_temp = (byte)PC + (int)(sbyte)opcode2;
					PC &= 0xFF00;
					PC |= (ushort)((alu_temp&0xFF));
					if(alu_temp.Bit(8))
					{
						//we need to carry the add, and then we'll be ready to fetch the next instruction
						opcode = VOP_RelativeStuff2;
						mi = -1;
					}
					break;
				case Uop.RelBranch_Stage4:
					FetchDummy();
					if (alu_temp.Bit(31))
						PC = (ushort)(PC - 0x100);
					else PC = (ushort)(PC + 0x100);
					break;

				case Uop.NOP: break;
				case Uop.DecS: S--; break;
				case Uop.IncS: S++; break;
				case Uop.JSR: PC = (ushort)((ReadMemory((ushort)(PC)) << 8) + opcode2); break;
				case Uop.PullP: 
					P = ReadMemory((ushort)(S++ + 0x100));
					FlagT = true;
					break;
				case Uop.PullPCL:
					PC &= 0xFF00;
					PC |= ReadMemory((ushort)(S++ + 0x100));
					break;
				case Uop.PullPCH_NoInc:
					PC &= 0xFF;
					PC |= (ushort)(ReadMemory((ushort)(S + 0x100)) << 8);
					break;

				case Uop.Abs_READ_LDA:
					A = ReadMemory((ushort)((opcode3 << 8) + opcode2));
					goto case Uop.NZ_A;
				case Uop.Abs_READ_LDY:
					Y = ReadMemory((ushort)((opcode3 << 8) + opcode2));
					goto case Uop.NZ_Y;
				case Uop.Abs_READ_LDX:
					X = ReadMemory((ushort)((opcode3 << 8) + opcode2));
					goto case Uop.NZ_X;
				case Uop.Abs_READ_BIT:
					alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
					goto case Uop._Bit;
				case Uop.Abs_READ_AND:
					alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
					goto case Uop._And;
				case Uop.Abs_READ_EOR:
					alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
					goto case Uop._Eor;
				case Uop.Abs_READ_ORA:
					alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
					goto case Uop._Ora;
				case Uop.Abs_READ_ADC:
					alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
					goto case Uop._Adc;
				case Uop.Abs_READ_CMP:
					alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
					goto case Uop._Cmp;
				case Uop.Abs_READ_CPY:
					alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
					goto case Uop._Cpy;
				case Uop.Abs_READ_NOP:
					alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
					break;
				case Uop.Abs_READ_Unofficial:
					alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
					break;
				case Uop.Abs_READ_CPX:
					alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
					goto case Uop._Cpx;
				case Uop.Abs_READ_SBC:
					alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
					goto case Uop._Sbc;

				case Uop.ZpIdx_Stage3_X:
					ReadMemory(opcode2);
					opcode2 = (byte)(opcode2 + X); //a bit sneaky to shove this into opcode2... but we can reuse all the zero page uops if we do that
					break;
				case Uop.ZpIdx_Stage3_Y:
					ReadMemory(opcode2);
					opcode2 = (byte)(opcode2 + Y); //a bit sneaky to shove this into opcode2... but we can reuse all the zero page uops if we do that
					break;
				case Uop.ZpIdx_RMW_Stage4:
					alu_temp = ReadMemory(opcode2);
					break;
				case Uop.ZpIdx_RMW_Stage6:
					WriteMemory(opcode2, (byte)alu_temp);
					break;

				case Uop.ZP_READ_UNOFFICIAL:
					alu_temp = ReadMemory(opcode2);
					break;
				case Uop.ZP_READ_EOR:
					alu_temp = ReadMemory(opcode2);
					goto case Uop._Eor;
				case Uop.ZP_READ_BIT:
					alu_temp = ReadMemory(opcode2);
					goto case Uop._Bit;
				case Uop.ZP_READ_LDA:
					A = ReadMemory(opcode2);
					goto case Uop.NZ_A;
				case Uop.ZP_READ_LDY:
					Y = ReadMemory(opcode2);
					goto case Uop.NZ_Y;
				case Uop.ZP_READ_LDX:
					X = ReadMemory(opcode2);
					goto case Uop.NZ_X;
				case Uop.ZP_READ_LAX:
					//?? is this right??
					X = ReadMemory(opcode2);
					A = X;
					goto case Uop.NZ_A;
				case Uop.ZP_READ_CPY:
					alu_temp = ReadMemory(opcode2);
					goto case Uop._Cpy;
				case Uop.ZP_READ_CMP:
					alu_temp = ReadMemory(opcode2);
					goto case Uop._Cmp;
				case Uop.ZP_READ_CPX:
					alu_temp = ReadMemory(opcode2);
					goto case Uop._Cpx;
				case Uop.ZP_READ_ORA:
					alu_temp = ReadMemory(opcode2);
					goto case Uop._Ora;
				case Uop.ZP_READ_NOP:
					ReadMemory(opcode2); //just a dummy
					break;
				case Uop.ZP_READ_SBC:
					alu_temp = ReadMemory(opcode2);
					goto case Uop._Sbc;
				case Uop.ZP_READ_ADC:
					alu_temp = ReadMemory(opcode2);
					goto case Uop._Adc;
				case Uop.ZP_READ_AND:
					alu_temp = ReadMemory(opcode2);
					goto case Uop._And;

				case Uop._Cpx:
					value8 = (byte)alu_temp;
					value16 = (ushort)(X - value8);
					FlagC = (X >= value8);
					P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
					break;
				case Uop._Cpy:
					value8 = (byte)alu_temp;
					value16 = (ushort)(Y - value8);
					FlagC = (Y >= value8);
					P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
					break;
				case Uop._Cmp:
					value8 = (byte)alu_temp;
					value16 = (ushort)(A - value8);
					FlagC = (A >= value8);
					P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
					break;
				case Uop._Bit:
					FlagN = (alu_temp & 0x80) != 0;
					FlagV = (alu_temp & 0x40) != 0;
					FlagZ = (A & alu_temp) == 0;
					break;
				case Uop._Eor:
					A ^= (byte)alu_temp;
					goto case Uop.NZ_A;
				case Uop._And:
					A &= (byte)alu_temp;
					goto case Uop.NZ_A;
				case Uop._Ora:
					A |= (byte)alu_temp;
					goto case Uop.NZ_A;
				case Uop._Sbc:
					{
						value8 = (byte)alu_temp;
						int temp = A - value8 - (FlagC ? 0 : 1);
						if (FlagD && BCD_Enabled)
						{
							int lo = (A & 0x0F) - (value8 & 0x0F) - (FlagC ? 0 : 1);
							int hi = (A & 0xF0) - (value8 & 0xF0);
							if ((lo & 0xF0) != 0) lo -= 0x06;
							if ((lo & 0x80) != 0) hi -= 0x10;
							if ((hi & 0x0F00) != 0) hi -= 0x60;
							FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
							FlagC = (hi & 0xFF00) == 0;
							A = (byte)((lo & 0x0F) | (hi & 0xF0));
						}
						else
						{
							FlagV = ((A ^ value8) & (A ^ temp) & 0x80) != 0;
							FlagC = temp >= 0;
							A = (byte)temp;
						}
						goto case Uop.NZ_A;
					}
				case Uop._Adc:
					{
						//TODO - an extra cycle penalty?
						value8 = (byte)alu_temp;
						if (FlagD && BCD_Enabled)
						{
							int lo = (A & 0x0F) + (value8 & 0x0F) + (FlagC ? 1 : 0);
							int hi = (A & 0xF0) + (value8 & 0xF0);
							if (lo > 0x09)
							{
								hi += 0x10;
								lo += 0x06;
							}
							if (hi > 0x90) hi += 0x60;
							FlagV = (~(A ^ value8) & (A ^ hi) & 0x80) != 0;
							FlagC = hi > 0xFF;
							A = (byte)((lo & 0x0F) | (hi & 0xF0));
						}
						else
						{
							int temp = value8 + A + (FlagC ? 1 : 0);
							FlagV = (~(A ^ value8) & (A ^ temp) & 0x80) != 0;
							FlagC = temp > 0xFF;
							A = (byte)temp;
						}
						goto case Uop.NZ_A;
					}

				case Uop.Unsupported:
					break;

				case Uop.Imm_EOR:
					alu_temp = ReadMemory(PC++);
					goto case Uop._Eor;
				case Uop.Imm_ORA:
					alu_temp = ReadMemory(PC++);
					goto case Uop._Ora;
				case Uop.Imm_CPY:
					alu_temp = ReadMemory(PC++);
					goto case Uop._Cpy;
				case Uop.Imm_CPX:
					alu_temp = ReadMemory(PC++);
					goto case Uop._Cpx;
				case Uop.Imm_CMP:
					alu_temp = ReadMemory(PC++);
					goto case Uop._Cmp;
				case Uop.Imm_SBC:
					alu_temp = ReadMemory(PC++);
					goto case Uop._Sbc;
				case Uop.Imm_AND:
					alu_temp = ReadMemory(PC++);
					goto case Uop._And;
				case Uop.Imm_ADC:
					alu_temp = ReadMemory(PC++);
					goto case Uop._Adc;
				case Uop.Imm_LDA:
					A = ReadMemory(PC++);
					goto case Uop.NZ_A;
				case Uop.Imm_LDX:
					X = ReadMemory(PC++);
					goto case Uop.NZ_X;
				case Uop.Imm_LDY:
					Y = ReadMemory(PC++);
					goto case Uop.NZ_Y;
				case Uop.Imm_Unsupported:
					ReadMemory(PC++);
					break;

				case Uop.IdxInd_Stage3:
					ReadMemory(opcode2); //dummy?
					alu_temp = (opcode2 + X) & 0xFF;
					break;
				case Uop.IdxInd_Stage4:
					ea = ReadMemory((ushort)alu_temp); 
					break;
				case Uop.IdxInd_Stage5:
					ea += (ReadMemory((byte)(alu_temp + 1)) << 8);
					break;
				case Uop.IdxInd_Stage6_READ_LDA:
					//TODO make uniform with others
					A = ReadMemory((ushort)ea);
					goto case Uop.NZ_A;
				case Uop.IdxInd_Stage6_READ_ORA:
					alu_temp = ReadMemory((ushort)ea);
					goto case Uop._Ora;
				case Uop.IdxInd_Stage6_READ_CMP:
					alu_temp = ReadMemory((ushort)ea);
					goto case Uop._Cmp;
				case Uop.IdxInd_Stage6_READ_ADC:
					alu_temp = ReadMemory((ushort)ea);
					goto case Uop._Adc;
				case Uop.IdxInd_Stage6_READ_AND:
					alu_temp = ReadMemory((ushort)ea);
					goto case Uop._And;
				case Uop.IdxInd_Stage6_READ_EOR:
					alu_temp = ReadMemory((ushort)ea);
					goto case Uop._Eor;
				case Uop.IdxInd_Stage6_READ_SBC:
					alu_temp = ReadMemory((ushort)ea);
					goto case Uop._Sbc;
				case Uop.IdxInd_Stage6_WRITE_STA:
					WriteMemory((ushort)ea, A);
					break;

				case Uop.PushP:
					FlagB = true;
					WriteMemory((ushort)(S-- + 0x100), P); 
					break;
				case Uop.PushA: WriteMemory((ushort)(S-- + 0x100), A); break;
				case Uop.PullA_NoInc: 
					A = ReadMemory((ushort)(S + 0x100));
					goto case Uop.NZ_A;
				case Uop.PullP_NoInc:
					{
						bool my_iflag = FlagI;
						P = ReadMemory((ushort)(S + 0x100));
						iflag_pending = FlagI;
						FlagI = my_iflag;
						FlagT = true; //why?
						break;
					}

				case Uop.Imp_ASL_A:
					FetchDummy();
					FlagC = (A & 0x80) != 0;
					A = (byte)(A << 1);
					goto case Uop.NZ_A;
				case Uop.Imp_ROL_A:
					FetchDummy();
					temp8 = A;
					A = (byte)((A << 1) | (P & 1));
					FlagC = (temp8 & 0x80) != 0;
					goto case Uop.NZ_A;
				case Uop.Imp_ROR_A:
					FetchDummy();
					temp8 = A;
					A = (byte)((A >> 1) | ((P & 1) << 7));
					FlagC = (temp8 & 1) != 0;
					goto case Uop.NZ_A;
				case Uop.Imp_LSR_A:
					FetchDummy();
					FlagC = (A & 1) != 0;
					A = (byte)(A >> 1);
					goto case Uop.NZ_A;

				case Uop.JMP_abs:
					PC = (ushort)((ReadMemory(PC) << 8) + opcode2);
					break;
				case Uop.IncPC: 
					PC++; 
					break;

				case Uop.ZP_RMW_Stage3:
					alu_temp = ReadMemory(opcode2);
					break;
				case Uop.ZP_RMW_Stage5:
					WriteMemory(opcode2,(byte)alu_temp);
					break;
				case Uop.ZP_RMW_INC:
					WriteMemory(opcode2, (byte)alu_temp);
					alu_temp = (byte)((alu_temp+1)&0xFF);
					P = (byte)((P & 0x7D) | TableNZ[alu_temp]);
					break;
				case Uop.ZP_RMW_DEC:
					WriteMemory(opcode2, (byte)alu_temp);
					alu_temp = (byte)((alu_temp - 1) & 0xFF);
					P = (byte)((P & 0x7D) | TableNZ[alu_temp]);
					break;
				case Uop.ZP_RMW_ASL:
					WriteMemory(opcode2, (byte)alu_temp);
					value8 = (byte)alu_temp;
					FlagC = (value8 & 0x80) != 0;
					alu_temp = value8 = (byte)(value8 << 1);
					P = (byte)((P & 0x7D) | TableNZ[value8]);
					break;
				case Uop.ZP_RMW_LSR:
					WriteMemory(opcode2, (byte)alu_temp);
					value8 = (byte)alu_temp;
					FlagC = (value8 & 1) != 0;
					alu_temp = value8 = (byte)(value8 >> 1);
					P = (byte)((P & 0x7D) | TableNZ[value8]);
					break;
				case Uop.ZP_RMW_ROR:
					WriteMemory(opcode2, (byte)alu_temp);
					value8 = temp8 = (byte)alu_temp;
					alu_temp = value8 = (byte)((value8 >> 1) | ((P & 1) << 7));
					FlagC = (temp8 & 1) != 0;
					P = (byte)((P & 0x7D) | TableNZ[value8]);
					break;
				case Uop.ZP_RMW_ROL:
					WriteMemory(opcode2, (byte)alu_temp);
					value8 = temp8 = (byte)alu_temp;
					alu_temp = value8 = (byte)((value8 << 1) | (P & 1));
					FlagC = (temp8 & 0x80) != 0;
					P = (byte)((P & 0x7D) | TableNZ[value8]);
					break;
				case Uop.ZP_RMW_UNOFFICIAL:
					WriteMemory(opcode2, (byte)alu_temp);
					break;
				case Uop.ZP_WRITE_UNOFFICIAL:
					WriteMemory(opcode2, 0); //???
					break;

				case Uop.AbsIdx_Stage3_Y:
					opcode3 = ReadMemory(PC++);
					alu_temp = opcode2 + Y;
					ea = (opcode3 << 8) + (alu_temp & 0xFF);
					break;
					//new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_Stage4, Uop.AbsIdx_WRITE_Stage5_STA, Uop.End },
				case Uop.AbsIdx_Stage3_X:
					opcode3 = ReadMemory(PC++);
					alu_temp = opcode2 + X;
					ea = (opcode3 << 8) + (alu_temp & 0xFF);
					break;
				case Uop.AbsIdx_READ_Stage4:
					if (!alu_temp.Bit(8))
					{
						mi++;
						goto RETRY;
					}
					else
					{
						alu_temp = ReadMemory((ushort)ea);
						ea = (ushort)(ea + 0x100);
					}
					break;
				case Uop.AbsIdx_Stage4:
					//bleh.. redundant code to make sure we dont clobber alu_temp before using it to decide whether to change ea
					if (alu_temp.Bit(8))
					{
						alu_temp = ReadMemory((ushort)ea);
						ea = (ushort)(ea + 0x100);
					} 
					else alu_temp = ReadMemory((ushort)ea);
					break;

				case Uop.AbsIdx_WRITE_Stage5_STA: 
					WriteMemory((ushort)ea, A); 
					break;
				case Uop.AbsIdx_WRITE_Stage5_Unofficial:
					WriteMemory((ushort)ea, 0);
					break;

				case Uop.AbsIdx_RMW_Stage5:
					alu_temp = ReadMemory((ushort)ea);
					break;
				case Uop.AbsIdx_RMW_Stage7:
					WriteMemory((ushort)ea, (byte)alu_temp);
					break;
				case Uop.AbsIdx_RMW_Stage6_DEC:
					WriteMemory((ushort)ea, (byte)alu_temp);
					alu_temp = value8 = (byte)(alu_temp - 1);
					P = (byte)((P & 0x7D) | TableNZ[value8]);
					break;
				case Uop.AbsIdx_RMW_Stage6_INC:
					WriteMemory((ushort)ea, (byte)alu_temp);
					alu_temp = value8 = (byte)(alu_temp + 1);
					P = (byte)((P & 0x7D) | TableNZ[value8]);
					break;
				case Uop.AbsIdx_RMW_Stage6_ROL:
					WriteMemory((ushort)ea, (byte)alu_temp);
					value8 = temp8 = (byte)alu_temp;
					alu_temp = value8 = (byte)((value8 << 1) | (P & 1));
					FlagC = (temp8 & 0x80) != 0;
					P = (byte)((P & 0x7D) | TableNZ[value8]);
					break;
				case Uop.AbsIdx_RMW_Stage6_LSR:
					WriteMemory((ushort)ea, (byte)alu_temp);
					value8 = (byte)alu_temp;
					FlagC = (value8 & 1) != 0;
					alu_temp = value8 = (byte)(value8 >> 1);
					P = (byte)((P & 0x7D) | TableNZ[value8]);
					break;
				case Uop.AbsIdx_RMW_Stage6_Unofficial:
					WriteMemory((ushort)ea, (byte)alu_temp);
					break;
				case Uop.AbsIdx_RMW_Stage6_ASL:
					WriteMemory((ushort)ea, (byte)alu_temp);
					value8 = (byte)alu_temp;
					FlagC = (value8 & 0x80) != 0;
					alu_temp = value8 = (byte)(value8 << 1);
					P = (byte)((P & 0x7D) | TableNZ[value8]);
					break;
				case Uop.AbsIdx_RMW_Stage6_ROR:
					WriteMemory((ushort)ea, (byte)alu_temp);
					value8 = temp8 = (byte)alu_temp;
					alu_temp = value8 = (byte)((value8 >> 1) | ((P & 1) << 7));
					FlagC = (temp8 & 1) != 0;
					P = (byte)((P & 0x7D) | TableNZ[value8]);
					break;

				case Uop.AbsIdx_READ_Stage5_LDA:
					A = ReadMemory((ushort)ea);
					goto case Uop.NZ_A;
				case Uop.AbsIdx_READ_Stage5_LDX:
					X = ReadMemory((ushort)ea);
					goto case Uop.NZ_X;
				case Uop.AbsIdx_READ_Stage5_LDY:
					Y = ReadMemory((ushort)ea);
					goto case Uop.NZ_Y;
				case Uop.AbsIdx_READ_Stage5_ORA:
					alu_temp = ReadMemory((ushort)ea);
					goto case Uop._Ora;
				case Uop.AbsIdx_READ_Stage5_NOP:
					alu_temp = ReadMemory((ushort)ea);
					break;
				case Uop.AbsIdx_READ_Stage5_CMP:
					alu_temp = ReadMemory((ushort)ea);
					goto case Uop._Cmp;
				case Uop.AbsIdx_READ_Stage5_SBC:
					alu_temp = ReadMemory((ushort)ea);
					goto case Uop._Sbc;
				case Uop.AbsIdx_READ_Stage5_ADC:
					alu_temp = ReadMemory((ushort)ea);
					goto case Uop._Adc;
				case Uop.AbsIdx_READ_Stage5_EOR:
					alu_temp = ReadMemory((ushort)ea);
					goto case Uop._Eor;
				case Uop.AbsIdx_READ_Stage5_AND:
					alu_temp = ReadMemory((ushort)ea);
					goto case Uop._And;

				case Uop.AbsInd_JMP_Stage4:
					ea = (opcode3<<8)+opcode2;
					alu_temp = ReadMemory((ushort)ea);
					break;
				case Uop.AbsInd_JMP_Stage5:
					ea = (opcode3<<8)+(byte)(opcode2+1);
					alu_temp += ReadMemory((ushort)ea) << 8;
					PC = (ushort)alu_temp;
					break;

				case Uop.Abs_RMW_Stage4:
					ea = (opcode3<<8)+opcode2;
					alu_temp = ReadMemory((ushort)ea);
					break;
				case Uop.Abs_RMW_Stage5_INC:
					WriteMemory((ushort)ea, (byte)alu_temp);
					value8 = (byte)(alu_temp + 1);
					alu_temp = value8;
					P = (byte)((P & 0x7D) | TableNZ[value8]);
					break;
				case Uop.Abs_RMW_Stage5_DEC:
					WriteMemory((ushort)ea, (byte)alu_temp);
					value8 = (byte)(alu_temp - 1);
					alu_temp = value8;
					P = (byte)((P & 0x7D) | TableNZ[value8]);
					break;
				case Uop.Abs_RMW_Stage5_ASL:
					WriteMemory((ushort)ea, (byte)alu_temp);
					value8 = (byte)alu_temp;
					FlagC = (value8 & 0x80) != 0;
					alu_temp = value8 = (byte)(value8 << 1);
					P = (byte)((P & 0x7D) | TableNZ[value8]);
					break;
				case Uop.Abs_RMW_Stage5_ROR:
					WriteMemory((ushort)ea, (byte)alu_temp);
					value8 = temp8 = (byte)alu_temp;
					alu_temp = value8 = (byte)((value8 >> 1) | ((P & 1) << 7));
					FlagC = (temp8 & 1) != 0;
					P = (byte)((P & 0x7D) | TableNZ[value8]);
					break;
				case Uop.Abs_RMW_Stage5_Unofficial:
					WriteMemory((ushort)ea, (byte)alu_temp);
					break;
				case Uop.Abs_RMW_Stage5_ROL:
					WriteMemory((ushort)ea, (byte)alu_temp);
					value8 = temp8 = (byte)alu_temp;
					alu_temp = value8 = (byte)((value8 << 1) | (P & 1));
					FlagC = (temp8 & 0x80) != 0;
					P = (byte)((P & 0x7D) | TableNZ[value8]);
					break;
				case Uop.Abs_RMW_Stage5_LSR:
					WriteMemory((ushort)ea, (byte)alu_temp);
					value8 = (byte)alu_temp;
					FlagC = (value8 & 1) != 0;
					alu_temp = value8 = (byte)(value8 >> 1);
					P = (byte)((P & 0x7D) | TableNZ[value8]);
					break;

				case Uop.Abs_RMW_Stage6:
					WriteMemory((ushort)ea, (byte)alu_temp);
					break;

				case Uop.End_ISpecial:
					opcode = VOP_Fetch1;
					mi = 0;
					goto RETRY;

				case Uop.End:
					opcode = VOP_Fetch1;
					mi = 0;
					iflag_pending = FlagI;
					goto RETRY;
			}

			mi++;

		} //ExecuteOne
	}
}
