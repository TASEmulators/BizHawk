//http://nesdev.parodius.com/6502_cpu.txt

using System.Runtime.CompilerServices;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Components.M6502
{
	public partial class MOS6502X<TLink>
	{
		//don't know whether this system is any faster. hard to get benchmarks someone else try it?
		//static ShortBuffer CompiledMicrocode;
		//static ShortBuffer MicrocodeIndex;
		static MOS6502X()
		{
			//  int index = 0;
			//  MicrocodeIndex = new ShortBuffer(VOP_NUM);
			//  List<Uop> temp = new List<Uop>();
			//  for (int i = 0; i < VOP_NUM; i++)
			//  {
			//    MicrocodeIndex[i] = (short)index;
			//    int numUops = Microcode[i].Length;
			//    for (int j = 0; j < numUops; j++)
			//      temp.Add(Microcode[i][j]);
			//    index += numUops;
			//  }
			//  CompiledMicrocode = new ShortBuffer(temp.Count);
			//  for (int i = 0; i < temp.Count; i++)
			//    CompiledMicrocode[i] = (short)temp[i];
		}

		private static readonly Uop[][] Microcode =
		{
			//0x00
			/*BRK [implied]*/ new Uop[] { Uop.Fetch2, Uop.PushPCH, Uop.PushPCL, Uop.PushP_BRK, Uop.FetchPCLVector, Uop.FetchPCHVector, Uop.End_SuppressInterrupt },
			/*ORA (addr,X) [indexed indirect READ]*/ new Uop[] { Uop.Fetch2, Uop.IdxInd_Stage3, Uop.IdxInd_Stage4, Uop.IdxInd_Stage5, Uop.IdxInd_Stage6_READ_ORA, Uop.End },
			/*JAM*/ new Uop[] { Uop.Jam },
			/*SLO* (addr,X) [indexed indirect RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.IdxInd_Stage3, Uop.IdxInd_Stage4, Uop.IdxInd_Stage5, Uop.IdxInd_Stage6_RMW, Uop.IdxInd_Stage7_RMW_SLO, Uop.IdxInd_Stage8_RMW, Uop.End },
			/*NOP zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_NOP, Uop.End },
			/*ORA zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_ORA, Uop.End },
			/*ASL zp [zero page RMW]*/ new Uop[] { Uop.Fetch2, Uop.ZP_RMW_Stage3, Uop.ZP_RMW_ASL, Uop.ZP_RMW_Stage5, Uop.End },
			/*SLO* zp [zero page RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.ZP_RMW_Stage3, Uop.ZP_RMW_SLO, Uop.ZP_RMW_Stage5, Uop.End },
			/*PHP [implied]*/ new Uop[] { Uop.FetchDummy, Uop.PushP, Uop.End },
			/*ORA #nn [immediate]*/ new Uop[] { Uop.Imm_ORA, Uop.End },
			/*ASL A [accumulator]*/ new Uop[] { Uop.Imp_ASL_A, Uop.End },
			/*ANC** [immediate] [unofficial]*/ new Uop[] { Uop.Imm_ANC, Uop.End },
			/*NOP addr [absolute READ]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_NOP, Uop.End },
			/*ORA addr [absolute READ]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_ORA, Uop.End },
			/*ASL addr [absolute RMW]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_RMW_Stage4, Uop.Abs_RMW_Stage5_ASL, Uop.Abs_RMW_Stage6, Uop.End },
			/*SLO* addr [absolute RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_RMW_Stage4, Uop.Abs_RMW_Stage5_SLO, Uop.Abs_RMW_Stage6, Uop.End },
			//0x10
			/*BPL +/-rel*/ new Uop[] { Uop.RelBranch_Stage2_BPL, Uop.End },
			/*ORA (addr),Y* [indirect indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.IndIdx_Stage3, Uop.IndIdx_Stage4, Uop.IndIdx_READ_Stage5, Uop.IndIdx_READ_Stage6_ORA, Uop.End },
			/*JAM*/ new Uop[] { Uop.Jam },
			/*SLO (addr),Y* [indirect indexed RMW] [unofficial] */ new Uop[] { Uop.Fetch2, Uop.IndIdx_Stage3, Uop.IndIdx_Stage4, Uop.IndIdx_RMW_Stage5, Uop.IndIdx_RMW_Stage6, Uop.IndIdx_RMW_Stage7_SLO, Uop.IndIdx_RMW_Stage8, Uop.End },
			/*NOP zp,X [zero page indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_NOP, Uop.End },
			/*ORA zp,X [zero page indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_ORA, Uop.End },
			/*ASL zp,X [zero page indexed RMW]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZpIdx_RMW_Stage4, Uop.ZP_RMW_ASL, Uop.ZpIdx_RMW_Stage6, Uop.End },
			/*SLO* zp,X [zero page indexed RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZpIdx_RMW_Stage4, Uop.ZP_RMW_SLO, Uop.ZpIdx_RMW_Stage6, Uop.End },
			/*CLC [implied]*/ new Uop[] { Uop.Imp_CLC, Uop.End },
			/*ORA addr,Y* [absolute indexed READ Y]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_ORA, Uop.End },
			/*NOP 1A*/ new Uop[] { Uop.FetchDummy, Uop.End },
			/*SLO* addr,Y [absolute indexed RMW Y] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_SLO, Uop.AbsIdx_RMW_Stage7, Uop.End },
			/*NOP addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_NOP, Uop.End },
			/*ORA addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_ORA, Uop.End },
			/*ASL addr,X [absolute indexed RMW X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_ASL, Uop.AbsIdx_RMW_Stage7, Uop.End },
			/*SLO* addr,X [absolute indexed RMW X] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_SLO, Uop.AbsIdx_RMW_Stage7, Uop.End },
			//0x20
			/*JSR*/ new Uop[] { Uop.Fetch2, Uop.NOP, Uop.PushPCH, Uop.PushPCL, Uop.JSR, Uop.End },
			/*AND (addr,X) [indexed indirect READ]*/ new Uop[] { Uop.Fetch2, Uop.IdxInd_Stage3, Uop.IdxInd_Stage4, Uop.IdxInd_Stage5, Uop.IdxInd_Stage6_READ_AND, Uop.End },
			/*JAM*/ new Uop[] { Uop.Jam },
			/*RLA* (addr,X) [indexed indirect RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.IdxInd_Stage3, Uop.IdxInd_Stage4, Uop.IdxInd_Stage5, Uop.IdxInd_Stage6_RMW, Uop.IdxInd_Stage7_RMW_RLA, Uop.IdxInd_Stage8_RMW, Uop.End },
			/*BIT zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_BIT, Uop.End },
			/*AND zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_AND, Uop.End },
			/*ROL zp [zero page RMW]*/ new Uop[] { Uop.Fetch2, Uop.ZP_RMW_Stage3, Uop.ZP_RMW_ROL, Uop.ZP_RMW_Stage5, Uop.End },
			/*RLA* zp [zero page RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.ZP_RMW_Stage3, Uop.ZP_RMW_RLA, Uop.ZP_RMW_Stage5, Uop.End },
			/*PLP [implied] */ new Uop[] { Uop.FetchDummy,  Uop.IncS, Uop.PullP_NoInc, Uop.End_ISpecial },
			/*AND #nn [immediate]*/ new Uop[] { Uop.Imm_AND, Uop.End },
			/*ROL A [accumulator]*/ new Uop[] { Uop.Imp_ROL_A, Uop.End },
			/*ANC** [immediate] [unofficial]*/ new Uop[] { Uop.Imm_ANC, Uop.End },
			/*BIT addr [absolute]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_BIT, Uop.End },
			/*AND addr [absolute READ]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_AND, Uop.End },
			/*ROL addr [absolute RMW]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_RMW_Stage4, Uop.Abs_RMW_Stage5_ROL, Uop.Abs_RMW_Stage6, Uop.End },
			/*RLA* addr [absolute RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_RMW_Stage4, Uop.Abs_RMW_Stage5_RLA, Uop.Abs_RMW_Stage6, Uop.End },
			//0x30
			/*BMI +/-rel [relative]*/ new Uop[] { Uop.RelBranch_Stage2_BMI, Uop.End },
			/*AND (addr),Y* [indirect indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.IndIdx_Stage3, Uop.IndIdx_Stage4, Uop.IndIdx_READ_Stage5, Uop.IndIdx_READ_Stage6_AND, Uop.End },
			/*JAM*/ new Uop[] { Uop.Jam },
			/*RLA* (addr),Y* [indirect indexed RMW] [unofficial] */ new Uop[] { Uop.Fetch2, Uop.IndIdx_Stage3, Uop.IndIdx_Stage4, Uop.IndIdx_RMW_Stage5, Uop.IndIdx_RMW_Stage6, Uop.IndIdx_RMW_Stage7_RLA, Uop.IndIdx_RMW_Stage8, Uop.End },
			/*NOP zp,X [zero page indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_NOP, Uop.End },
			/*AND zp,X [zero page indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_AND, Uop.End },
			/*ROL zp,X [zero page indexed RMW]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZpIdx_RMW_Stage4, Uop.ZP_RMW_ROL, Uop.ZpIdx_RMW_Stage6, Uop.End },
			/*RLA* zp,X [zero page indexed RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZpIdx_RMW_Stage4, Uop.ZP_RMW_RLA, Uop.ZpIdx_RMW_Stage6, Uop.End },
			/*SEC [implied]*/ new Uop[] { Uop.Imp_SEC, Uop.End },
			/*AND addr,Y* [absolute indexed READ Y]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_AND, Uop.End },
			/*NOP 3A [implied]*/ new Uop[] { Uop.FetchDummy, Uop.End },
			/*RLA* addr,Y [absolute indexed RMW Y] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_RLA, Uop.AbsIdx_RMW_Stage7, Uop.End },
			/*NOP addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_NOP, Uop.End },
			/*AND addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_AND, Uop.End },
			/*ROL addr,X [absolute indexed RMW X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_ROL, Uop.AbsIdx_RMW_Stage7, Uop.End },
			/*RLA* addr,X [absolute indexed RMW X] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_RLA, Uop.AbsIdx_RMW_Stage7, Uop.End },
			//0x40
			/*RTI*/ new Uop[] { Uop.FetchDummy, Uop.IncS, Uop.PullP, Uop.PullPCL, Uop.PullPCH_NoInc, Uop.End },
			/*EOR (addr,X) [indexed indirect READ]*/ new Uop[] { Uop.Fetch2, Uop.IdxInd_Stage3, Uop.IdxInd_Stage4, Uop.IdxInd_Stage5, Uop.IdxInd_Stage6_READ_EOR, Uop.End },
			/*JAM*/ new Uop[] { Uop.Jam },
			/*SRE* (addr,X) [indexed indirect RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.IdxInd_Stage3, Uop.IdxInd_Stage4, Uop.IdxInd_Stage5, Uop.IdxInd_Stage6_RMW, Uop.IdxInd_Stage7_RMW_SRE, Uop.IdxInd_Stage8_RMW, Uop.End },
			/*NOP zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_NOP, Uop.End },
			/*EOR zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_EOR, Uop.End },
			/*LSR zp [zero page RMW]*/ new Uop[] { Uop.Fetch2, Uop.ZP_RMW_Stage3, Uop.ZP_RMW_LSR, Uop.ZP_RMW_Stage5, Uop.End },
			/*SRE* zp [zero page RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.ZP_RMW_Stage3, Uop.ZP_RMW_SRE, Uop.ZP_RMW_Stage5, Uop.End },
			/*PHA [implied]*/ new Uop[] { Uop.FetchDummy, Uop.PushA, Uop.End },
			/*EOR #nn [immediate]*/ new Uop[] { Uop.Imm_EOR, Uop.End },
			/*LSR A [accumulator]*/ new Uop[] { Uop.Imp_LSR_A, Uop.End },
			/*ASR** [immediate] [unofficial]*/ new Uop[] { Uop.Imm_ASR, Uop.End },
			/*JMP addr [absolute]*/ new Uop[] { Uop.Fetch2, Uop.JMP_abs, Uop.End },
			/*EOR addr [absolute READ]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_EOR, Uop.End },
			/*LSR addr [absolute RMW]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_RMW_Stage4, Uop.Abs_RMW_Stage5_LSR, Uop.Abs_RMW_Stage6, Uop.End },
			/*SRE* addr [absolute RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_RMW_Stage4, Uop.Abs_RMW_Stage5_SRE, Uop.Abs_RMW_Stage6, Uop.End },
			//0x50
			/*BVC +/-rel [relative]*/ new Uop[] { Uop.RelBranch_Stage2_BVC, Uop.End },
			/*EOR (addr),Y* [indirect indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.IndIdx_Stage3, Uop.IndIdx_Stage4, Uop.IndIdx_READ_Stage5, Uop.IndIdx_READ_Stage6_EOR, Uop.End },
			/*JAM*/ new Uop[] { Uop.Jam },
			/*SRE* (addr),Y* [indirect indexed RMW] [unofficial] */ new Uop[] { Uop.Fetch2, Uop.IndIdx_Stage3, Uop.IndIdx_Stage4, Uop.IndIdx_RMW_Stage5, Uop.IndIdx_RMW_Stage6, Uop.IndIdx_RMW_Stage7_SRE, Uop.IndIdx_RMW_Stage8, Uop.End },
			/*NOP zp,X [zero page indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_NOP, Uop.End },
			/*EOR zp,X [zero page indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_EOR, Uop.End },
			/*LSR zp,X [zero page indexed RMW]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZpIdx_RMW_Stage4, Uop.ZP_RMW_LSR, Uop.ZpIdx_RMW_Stage6, Uop.End },
			/*SRE* zp,X [zero page indexed RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZpIdx_RMW_Stage4, Uop.ZP_RMW_SRE, Uop.ZpIdx_RMW_Stage6, Uop.End },
			/*CLI [implied]*/ new Uop[] { Uop.Imp_CLI, Uop.End_ISpecial },
			/*EOR addr,Y* [absolute indexed READ Y]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_EOR, Uop.End },
			/*NOP 5A [implied]*/ new Uop[] { Uop.FetchDummy, Uop.End },
			/*SRE* addr,Y [absolute indexed RMW Y] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_SRE, Uop.AbsIdx_RMW_Stage7, Uop.End },
			/*NOP addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_NOP, Uop.End },
			/*EOR addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_EOR, Uop.End },
			/*LSR addr,X [absolute indexed RMW X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_LSR, Uop.AbsIdx_RMW_Stage7, Uop.End },
			/*SRE* addr,X [absolute indexed RMW X] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_SRE, Uop.AbsIdx_RMW_Stage7, Uop.End },
			//0x60
			/*RTS*/ new Uop[] { Uop.FetchDummy, Uop.IncS, Uop.PullPCL, Uop.PullPCH_NoInc, Uop.IncPC, Uop.End }, //can't fetch here because the PC isnt ready until the end of the last clock
			/*ADC (addr,X) [indexed indirect READ]*/ new Uop[] { Uop.Fetch2, Uop.IdxInd_Stage3, Uop.IdxInd_Stage4, Uop.IdxInd_Stage5, Uop.IdxInd_Stage6_READ_ADC, Uop.End },
			/*JAM*/ new Uop[] { Uop.Jam },
			/*RRA* (addr,X) [indexed indirect RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.IdxInd_Stage3, Uop.IdxInd_Stage4, Uop.IdxInd_Stage5, Uop.IdxInd_Stage6_RMW, Uop.IdxInd_Stage7_RMW_RRA, Uop.IdxInd_Stage8_RMW, Uop.End },
			/*NOP zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_NOP, Uop.End },
			/*ADC zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_ADC, Uop.End },
			/*ROR zp [zero page RMW]*/ new Uop[] { Uop.Fetch2, Uop.ZP_RMW_Stage3, Uop.ZP_RMW_ROR, Uop.ZP_RMW_Stage5, Uop.End },
			/*RRA* zp [zero page RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.ZP_RMW_Stage3, Uop.ZP_RMW_RRA, Uop.ZP_RMW_Stage5, Uop.End },
			/*PLA [implied]*/ new Uop[] { Uop.FetchDummy, Uop.IncS, Uop.PullA_NoInc, Uop.End },
			/*ADC #nn [immediate]*/ new Uop[] { Uop.Imm_ADC, Uop.End },
			/*ROR A [accumulator]*/ new Uop[] { Uop.Imp_ROR_A, Uop.End },
			/*ARR** [immediate] [unofficial]*/ new Uop[] { Uop.Imm_ARR, Uop.End },
			/*JMP (addr) [absolute indirect JMP]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.AbsInd_JMP_Stage4, Uop.AbsInd_JMP_Stage5, Uop.End },
			/*ADC addr [absolute READ]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_ADC, Uop.End },
			/*ROR addr [absolute RMW]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_RMW_Stage4, Uop.Abs_RMW_Stage5_ROR, Uop.Abs_RMW_Stage6, Uop.End },
			/*RRA* addr [absolute RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_RMW_Stage4, Uop.Abs_RMW_Stage5_RRA, Uop.Abs_RMW_Stage6, Uop.End },
			//0x70
			/*BVS +/-rel [relative]*/ new Uop[] { Uop.RelBranch_Stage2_BVS, Uop.End },
			/*ADC (addr),Y [indirect indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.IndIdx_Stage3, Uop.IndIdx_Stage4, Uop.IndIdx_READ_Stage5, Uop.IndIdx_READ_Stage6_ADC, Uop.End },
			/*JAM*/ new Uop[] { Uop.Jam },
			/*RRA* (addr),Y [indirect indexed RMW Y] [unofficial] */ new Uop[] { Uop.Fetch2, Uop.IndIdx_Stage3, Uop.IndIdx_Stage4, Uop.IndIdx_RMW_Stage5, Uop.IndIdx_RMW_Stage6, Uop.IndIdx_RMW_Stage7_RRA, Uop.IndIdx_RMW_Stage8, Uop.End },
			/*NOP zp,X [zero page indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_NOP, Uop.End },
			/*ADC zp,X [zero page indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_ADC, Uop.End },
			/*ROR zp,X [zero page indexed RMW]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZpIdx_RMW_Stage4, Uop.ZP_RMW_ROR, Uop.ZpIdx_RMW_Stage6, Uop.End },
			/*RRA* zp,X [zero page indexed RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZpIdx_RMW_Stage4, Uop.ZP_RMW_RRA, Uop.ZpIdx_RMW_Stage6, Uop.End },
			/*SEI [implied]*/ new Uop[] { Uop.Imp_SEI, Uop.End_ISpecial },
			/*ADC addr,Y* [absolute indexed READ Y]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_ADC, Uop.End },
			/*NOP 7A [implied]*/ new Uop[] { Uop.FetchDummy, Uop.End },
			/*RRA* addr,Y [absolute indexed RMW Y] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_RRA, Uop.AbsIdx_RMW_Stage7, Uop.End },
			/*NOP addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_NOP, Uop.End },
			/*ADC addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_ADC, Uop.End },
			/*ROR addr,X [absolute indexed RMW X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_ROR, Uop.AbsIdx_RMW_Stage7, Uop.End },
			/*RRA* addr,X [absolute indexed RMW X] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_RRA, Uop.AbsIdx_RMW_Stage7, Uop.End },
			//0x80
			/*NOP #nn [immediate]*/ new Uop[] { Uop.Imm_Unsupported, Uop.End },
			/*STA (addr,X) [indexed indirect WRITE]*/ new Uop[] { Uop.Fetch2, Uop.IdxInd_Stage3, Uop.IdxInd_Stage4, Uop.IdxInd_Stage5, Uop.IdxInd_Stage6_WRITE_STA, Uop.End },
			/*NOP #nn [immediate]*/ new Uop[] { Uop.Imm_Unsupported, Uop.End }, //jams very rarely
			/*SAX* (addr,X) [indexed indirect WRITE] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.IdxInd_Stage3, Uop.IdxInd_Stage4, Uop.IdxInd_Stage5, Uop.IdxInd_Stage6_WRITE_SAX, Uop.End },
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
			/*SAX* addr [absolute WRITE] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_WRITE_SAX, Uop.End },
			//0x90
			/*BCC +/-rel [relative]*/ new Uop[] { Uop.RelBranch_Stage2_BCC, Uop.End },
			/*STA (addr),Y [indirect indexed WRITE]*/ new Uop[] { Uop.Fetch2, Uop.IndIdx_Stage3, Uop.IndIdx_Stage4, Uop.IndIdx_WRITE_Stage5, Uop.IndIdx_WRITE_Stage6_STA, Uop.End },
			/*JAM*/ new Uop[] { Uop.Jam },
			/*SHA** [indirect indexed WRITE] [unofficial] [not tested by blargg's instruction tests]*/ new Uop[] { Uop.Fetch2, Uop.IndIdx_Stage3, Uop.IndIdx_Stage4, Uop.IndIdx_WRITE_Stage5, Uop.IndIdx_WRITE_Stage6_SHA, Uop.End },
			/*STY zp,X [zero page indexed WRITE X]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_WRITE_STY, Uop.End },
			/*STA zp,X [zero page indexed WRITE X]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_WRITE_STA, Uop.End },
			/*STX zp,Y [zero page indexed WRITE Y]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_Y, Uop.ZP_WRITE_STX, Uop.End },
			/*SAX* zp,Y [zero page indexed WRITE Y] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_Y, Uop.ZP_WRITE_SAX, Uop.End },
			/*TYA [implied]*/ new Uop[] { Uop.Imp_TYA, Uop.End },
			/*STA addr,Y [absolute indexed WRITE]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_Stage4, Uop.AbsIdx_WRITE_Stage5_STA, Uop.End },
			/*TXS [implied]*/ new Uop[] { Uop.Imp_TXS, Uop.End },
			/*SHS* addr,Y [absolute indexed WRITE Y] [unofficial] */ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_Stage4, Uop.AbsIdx_WRITE_Stage5_ERROR, Uop.End },
			/*SHY** [absolute indexed WRITE] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_Stage4, Uop.AbsIdx_WRITE_Stage5_SHY, Uop.End },
			/*STA addr,X [absolute indexed WRITE]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_Stage4, Uop.AbsIdx_WRITE_Stage5_STA, Uop.End },
			/*SHX* addr,Y [absolute indexed WRITE Y] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_Stage4, Uop.AbsIdx_WRITE_Stage5_SHX, Uop.End },
			/*SHA* addr,Y [absolute indexed WRITE Y] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_Stage4, Uop.AbsIdx_WRITE_Stage5_SHY, Uop.End },
			//0xA0
			/*LDY #nn [immediate]*/ new Uop[] { Uop.Imm_LDY, Uop.End },
			/*LDA (addr,X) [indexed indirect READ]*/ new Uop[] { Uop.Fetch2, Uop.IdxInd_Stage3, Uop.IdxInd_Stage4, Uop.IdxInd_Stage5, Uop.IdxInd_Stage6_READ_LDA, Uop.End },
			/*LDX #nn [immediate]*/ new Uop[] { Uop.Imm_LDX, Uop.End },
			/*LAX* (addr,X) [indexed indirect READ] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.IdxInd_Stage3, Uop.IdxInd_Stage4, Uop.IdxInd_Stage5, Uop.IdxInd_Stage6_READ_LAX, Uop.End },
			/*LDY zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_LDY, Uop.End },
			/*LDA zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_LDA, Uop.End },
			/*LDX zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_LDX, Uop.End },
			/*LAX* zp [zero page READ] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_LAX, Uop.End },
			/*TAY [implied]*/ new Uop[] { Uop.Imp_TAY, Uop.End },
			/*LDA #nn [immediate]*/ new Uop[] { Uop.Imm_LDA, Uop.End },
			/*TAX [implied]*/ new Uop[] { Uop.Imp_TAX, Uop.End },
			/*LXA** (ATX) [immediate] [unofficial]*/ new Uop[] { Uop.Imm_LXA, Uop.End },
			/*LDY addr [absolute READ]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_LDY, Uop.End },
			/*LDA addr [absolute READ]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_LDA, Uop.End },
			/*LDX addr [absolute READ]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_LDX, Uop.End },
			/*LAX* addr [absolute READ] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_LAX, Uop.End },
			//0xB0
			/*BCS +/-rel [relative]*/ new Uop[] { Uop.RelBranch_Stage2_BCS, Uop.End },
			/*LDA (addr),Y* [indirect indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.IndIdx_Stage3, Uop.IndIdx_Stage4, Uop.IndIdx_READ_Stage5, Uop.IndIdx_READ_Stage6_LDA, Uop.End },
			/*JAM*/ new Uop[] { Uop.Jam },
			/*LAX* (addr),Y* [indirect indexed READ] [unofficial] */ new Uop[] { Uop.Fetch2, Uop.IndIdx_Stage3, Uop.IndIdx_Stage4, Uop.IndIdx_READ_Stage5, Uop.IndIdx_READ_Stage6_LAX, Uop.End },
			/*LDY zp,X [zero page indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_LDY, Uop.End },
			/*LDA zp,X [zero page indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_LDA, Uop.End },
			/*LDX zp,Y [zero page indexed READ Y]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_Y, Uop.ZP_READ_LDX, Uop.End },
			/*LAX* zp,Y [zero page indexed READ] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_Y, Uop.ZP_READ_LAX, Uop.End },
			/*CLV [implied]*/ new Uop[] { Uop.Imp_CLV, Uop.End },
			/*LDA addr,Y* [absolute indexed READ Y]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_LDA, Uop.End },
			/*TSX [implied]*/ new Uop[] { Uop.Imp_TSX, Uop.End },
			/*LAS* addr,Y [absolute indexed READ Y] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_ERROR, Uop.End },
			/*LDY addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_LDY, Uop.End },
			/*LDA addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_LDA, Uop.End },
			/*LDX addr,Y* [absolute indexed READ Y]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_LDX, Uop.End },
			/*LAX* addr,Y [absolute indexed READ Y] [unofficial]*/  new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_LAX, Uop.End },
			//0xC0
			/*CPY #nn [immediate]*/ new Uop[] { Uop.Imm_CPY, Uop.End },
			/*CMP (addr,X) [indexed indirect READ]*/ new Uop[] { Uop.Fetch2, Uop.IdxInd_Stage3, Uop.IdxInd_Stage4, Uop.IdxInd_Stage5, Uop.IdxInd_Stage6_READ_CMP, Uop.End },
			/*NOP #nn [immediate]*/ new Uop[] { Uop.Imm_Unsupported, Uop.End }, //jams very rarely
			/*DCP* (addr,X) [indexed indirect RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.IdxInd_Stage3, Uop.IdxInd_Stage4, Uop.IdxInd_Stage5, Uop.IdxInd_Stage6_RMW, Uop.IdxInd_Stage7_RMW_DCP, Uop.IdxInd_Stage8_RMW, Uop.End },
			/*CPY zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_CPY, Uop.End },
			/*CMP zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_CMP, Uop.End },
			/*DEC zp [zero page RMW]*/ new Uop[] { Uop.Fetch2, Uop.ZP_RMW_Stage3, Uop.ZP_RMW_DEC, Uop.ZP_RMW_Stage5, Uop.End },
			/*DCP* zp [zero page RMW] [unofficial]*/ new Uop[]  { Uop.Fetch2, Uop.ZP_RMW_Stage3, Uop.ZP_RMW_DCP, Uop.ZP_RMW_Stage5, Uop.End },
			/*INY [implied]*/ new Uop[] { Uop.Imp_INY, Uop.End },
			/*CMP #nn [immediate]*/ new Uop[] { Uop.Imm_CMP, Uop.End },
			/*DEX  [implied]*/ new Uop[] { Uop.Imp_DEX, Uop.End },
			/*AXS** [immediate] [unofficial]*/ new Uop[] { Uop.Imm_AXS, Uop.End },
			/*CPY addr [absolute READ]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_CPY, Uop.End },
			/*CMP addr [absolute READ]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_CMP, Uop.End },
			/*DEC addr [absolute RMW]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_RMW_Stage4, Uop.Abs_RMW_Stage5_DEC, Uop.Abs_RMW_Stage6, Uop.End },
			/*DCP* addr [absolute RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_RMW_Stage4, Uop.Abs_RMW_Stage5_DCP, Uop.Abs_RMW_Stage6, Uop.End },
			//0xD0
			/*BNE +/-rel [relative]*/ new Uop[] { Uop.RelBranch_Stage2_BNE, Uop.End },
			/*CMP (addr),Y* [indirect indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.IndIdx_Stage3, Uop.IndIdx_Stage4, Uop.IndIdx_READ_Stage5, Uop.IndIdx_READ_Stage6_CMP, Uop.End },
			/*JAM*/ new Uop[] { Uop.Jam },
			/*DCP* (addr),Y* [indirect indexed RMW Y] [unofficial] */ new Uop[] { Uop.Fetch2, Uop.IndIdx_Stage3, Uop.IndIdx_Stage4, Uop.IndIdx_RMW_Stage5, Uop.IndIdx_RMW_Stage6, Uop.IndIdx_RMW_Stage7_DCP, Uop.IndIdx_RMW_Stage8, Uop.End },
			/*NOP zp,X [zero page indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_NOP, Uop.End },
			/*CMP zp,X [zero page indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_CMP, Uop.End },
			/*DEC zp,X [zero page indexed RMW X]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZpIdx_RMW_Stage4, Uop.ZP_RMW_DEC, Uop.ZpIdx_RMW_Stage6, Uop.End },
			/*DCP* zp,X [zero page indexed RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZpIdx_RMW_Stage4, Uop.ZP_RMW_DCP, Uop.ZpIdx_RMW_Stage6, Uop.End },
			/*CLD [implied]*/ new Uop[] { Uop.Imp_CLD, Uop.End },
			/*CMP addr,Y* [absolute indexed READ Y]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_CMP, Uop.End },
			/*NOP DA [implied]*/ new Uop[] { Uop.FetchDummy, Uop.End },
			/*DCP* addr,Y [absolute indexed RMW Y] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_DCP, Uop.AbsIdx_RMW_Stage7, Uop.End },
			/*NOP addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_NOP, Uop.End },
			/*CMP addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_CMP, Uop.End },
			/*DEC addr,X [absolute indexed RMW X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_DEC, Uop.AbsIdx_RMW_Stage7, Uop.End },
			/*DCP* addr,X [absolute indexed RMW X] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_DCP, Uop.AbsIdx_RMW_Stage7, Uop.End },
			//0xE0
			/*CPX #nn [immediate]*/ new Uop[] { Uop.Imm_CPX, Uop.End },
			/*SBC (addr,X) [indirect indexed]*/ new Uop[] { Uop.Fetch2, Uop.IdxInd_Stage3, Uop.IdxInd_Stage4, Uop.IdxInd_Stage5, Uop.IdxInd_Stage6_READ_SBC, Uop.End },
			/*NOP #nn [immediate]*/ new Uop[] { Uop.Imm_Unsupported, Uop.End }, //jams very rarely
			/*ISC* (addr,X) [indexed indirect RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.IdxInd_Stage3, Uop.IdxInd_Stage4, Uop.IdxInd_Stage5, Uop.IdxInd_Stage6_RMW, Uop.IdxInd_Stage7_RMW_ISC, Uop.IdxInd_Stage8_RMW, Uop.End },
			/*CPX zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_CPX, Uop.End },
			/*SBC zp [zero page READ]*/ new Uop[] { Uop.Fetch2, Uop.ZP_READ_SBC, Uop.End },
			/*INC zp [zero page RMW]*/ new Uop[] { Uop.Fetch2, Uop.ZP_RMW_Stage3, Uop.ZP_RMW_INC, Uop.ZP_RMW_Stage5, Uop.End },
			/*ISB* zp [zero page RMW] [unofficial]*/ new Uop[]  { Uop.Fetch2, Uop.ZP_RMW_Stage3, Uop.ZP_RMW_ISC, Uop.ZP_RMW_Stage5, Uop.End },
			/*INX [implied]*/ new Uop[] { Uop.Imp_INX, Uop.End },
			/*SBC #nn [immediate READ]*/ new Uop[] { Uop.Imm_SBC, Uop.End },
			/*NOP EA [implied]*/ new Uop[] { Uop.FetchDummy, Uop.End }, //nothing happened here.. but the last thing to happen was a fetch, so we can't pipeline the next fetch
			/*ISB #nn [immediate READ]*/ new Uop[] { Uop.Imm_SBC, Uop.End },
			/*CPX addr [absolute READ]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_CPX, Uop.End },
			/*SBC addr [absolute READ]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_READ_SBC, Uop.End },
			/*INC addr [absolute RMW]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_RMW_Stage4, Uop.Abs_RMW_Stage5_INC, Uop.Abs_RMW_Stage6, Uop.End },
			/*ISC* addr [absolute RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.Fetch3, Uop.Abs_RMW_Stage4, Uop.Abs_RMW_Stage5_ISC, Uop.Abs_RMW_Stage6, Uop.End },
			//0xF0
			/*BEQ +/-rel [relative]*/ new Uop[] { Uop.RelBranch_Stage2_BEQ, Uop.End },
			/*SBC (addr),Y* [indirect indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.IndIdx_Stage3, Uop.IndIdx_Stage4, Uop.IndIdx_READ_Stage5, Uop.IndIdx_READ_Stage6_SBC, Uop.End },
			/*JAM*/ new Uop[] { Uop.Jam },
			/*ISC* (addr),Y* [indirect indexed RMW Y] [unofficial] */ new Uop[] { Uop.Fetch2, Uop.IndIdx_Stage3, Uop.IndIdx_Stage4, Uop.IndIdx_RMW_Stage5, Uop.IndIdx_RMW_Stage6, Uop.IndIdx_RMW_Stage7_ISC, Uop.IndIdx_RMW_Stage8, Uop.End },
			/*NOP zp,X [zero page indexed READ]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_NOP, Uop.End },
			/*SBC zp,X [zero page indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZP_READ_SBC, Uop.End },
			/*INC zp,X [zero page indexed RMW X]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZpIdx_RMW_Stage4, Uop.ZP_RMW_INC, Uop.ZpIdx_RMW_Stage6, Uop.End },
			/*ISC* zp,X [zero page indexed RMW] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.ZpIdx_Stage3_X, Uop.ZpIdx_RMW_Stage4, Uop.ZP_RMW_ISC, Uop.ZpIdx_RMW_Stage6, Uop.End },
			/*SED [implied]*/ new Uop[] { Uop.Imp_SED, Uop.End },
			/*SBC addr,Y* [absolute indexed READ Y]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_SBC, Uop.End },
			/*NOP FA [implied]*/ new Uop[] { Uop.FetchDummy, Uop.End },
			/*ISC* addr,Y [absolute indexed RMW Y] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_ISC, Uop.AbsIdx_RMW_Stage7, Uop.End },
			/*NOP addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_NOP, Uop.End },
			/*SBC addr,X* [absolute indexed READ X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X, Uop.AbsIdx_READ_Stage4, Uop.AbsIdx_READ_Stage5_SBC, Uop.End },
			/*INC addr,X [absolute indexed RMW X]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_INC, Uop.AbsIdx_RMW_Stage7, Uop.End },
			/*ISC* addr,X [absolute indexed RMW X] [unofficial]*/ new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_X,  Uop.AbsIdx_Stage4, Uop.AbsIdx_RMW_Stage5, Uop.AbsIdx_RMW_Stage6_ISC, Uop.AbsIdx_RMW_Stage7, Uop.End },
			//0x100
			/*VOP_Fetch1*/ new Uop[] { Uop.Fetch1 },
			/*VOP_RelativeStuff*/ new Uop[] { Uop.RelBranch_Stage3, Uop.End },
			/*VOP_RelativeStuff2*/ new Uop[] { Uop.RelBranch_Stage4, Uop.End },
			/*VOP_RelativeStuff3*/ new Uop[] { Uop.End_SuppressInterrupt },
			//i assume these are dummy fetches.... maybe theyre just nops? supposedly these take 7 cycles so that's the only way i can make sense of it
			//one of them might be the next instruction's fetch, and whatever fetch follows it.
			//the interrupt would then take place if necessary, using a cached PC. but im not so sure about that.
			/*VOP_NMI*/ new Uop[] { Uop.FetchDummy, Uop.FetchDummy, Uop.PushPCH, Uop.PushPCL, Uop.PushP_NMI, Uop.FetchPCLVector, Uop.FetchPCHVector, Uop.End_SuppressInterrupt },
			/*VOP_IRQ*/ new Uop[] { Uop.FetchDummy, Uop.FetchDummy, Uop.PushPCH, Uop.PushPCL, Uop.PushP_IRQ, Uop.FetchPCLVector, Uop.FetchPCHVector, Uop.End_SuppressInterrupt },
			/*VOP_RESET*/ new Uop[] { Uop.FetchDummy, /*Uop.FetchDummy,*/ Uop.FetchDummy, Uop.PushDummy, Uop.PushDummy, Uop.PushP_Reset, Uop.FetchPCLVector, Uop.FetchPCHVector, Uop.End_SuppressInterrupt },
			/*VOP_Fetch1_NoInterrupt*/  new Uop[] { Uop.Fetch1_Real },
		};

		/*
		static MOS6502X()
		{
			using (System.IO.StreamWriter sw = new System.IO.StreamWriter("UopEnum.h"))
			{
				sw.WriteLine("// AUTOGENERATED");
				sw.WriteLine("#ifndef UOPENUM_H");
				sw.WriteLine("#define UOPENUM_H");
				sw.WriteLine("enum Uop {");
				foreach (var v in Enum.GetValues(typeof(Uop)))
				{
					//sw.WriteLine("#define Uop_{0} {1}", (Uop)v, (int)v);
					sw.WriteLine("\tUop_{0}, ", (Uop)v);
				}
				sw.WriteLine("};");
				sw.WriteLine("#endif // UOPENUM_H");
			}
			using (System.IO.StreamWriter sw = new System.IO.StreamWriter("UopTable.cpp"))
			{
				sw.WriteLine("// AUTOGENERATED");
				sw.WriteLine("#include \"UopEnum.h\"");

				int max = 0;
				foreach (var a in Microcode)
					if (a.Length > max)
						max = a.Length;

				sw.WriteLine("const Uop Microcode[{0}][{1}] = {{", Microcode.Length, max);
				for (int i = 0; i < Microcode.Length; i++)
				{
					sw.Write("\t{");
					for (int j = 0; j < Microcode[i].Length; j++)
					{
						sw.Write("Uop_{0}", Microcode[i][j]);
						if (j < Microcode[i].Length - 1)
							sw.Write(", ");
					}
					sw.WriteLine("},");
				}
				sw.WriteLine("};");
			}
		}
		*/

		private enum Uop
		{
			//sometimes i used this as a marker for unsupported instructions, but it is very inconsistent
			Unsupported,

			Fetch1, Fetch1_Real, Fetch2, Fetch3,
			//used by instructions with no second opcode byte (6502 fetches a byte anyway but won't increment PC for these)
			FetchDummy,

			NOP,

			JSR,
			IncPC, //from RTS

			//[absolute WRITE]
			Abs_WRITE_STA, Abs_WRITE_STX, Abs_WRITE_STY,
			Abs_WRITE_SAX, //unofficials
			//[absolute READ]
			Abs_READ_BIT, Abs_READ_LDA, Abs_READ_LDY, Abs_READ_ORA, Abs_READ_LDX, Abs_READ_CMP, Abs_READ_ADC, Abs_READ_CPX, Abs_READ_SBC, Abs_READ_AND, Abs_READ_EOR, Abs_READ_CPY, Abs_READ_NOP,
			Abs_READ_LAX, //unofficials
			//[absolute RMW]
			Abs_RMW_Stage4, Abs_RMW_Stage6,
			Abs_RMW_Stage5_INC, Abs_RMW_Stage5_DEC, Abs_RMW_Stage5_LSR, Abs_RMW_Stage5_ROL, Abs_RMW_Stage5_ASL, Abs_RMW_Stage5_ROR,
			Abs_RMW_Stage5_SLO, Abs_RMW_Stage5_RLA, Abs_RMW_Stage5_SRE, Abs_RMW_Stage5_RRA, Abs_RMW_Stage5_DCP, Abs_RMW_Stage5_ISC, //unofficials

			//[absolute JUMP]
			JMP_abs,

			//[zero page misc]
			ZpIdx_Stage3_X, ZpIdx_Stage3_Y,
			ZpIdx_RMW_Stage4, ZpIdx_RMW_Stage6,
			//[zero page WRITE]
			ZP_WRITE_STA, ZP_WRITE_STX, ZP_WRITE_STY, ZP_WRITE_SAX,
			//[zero page RMW]
			ZP_RMW_Stage3, ZP_RMW_Stage5,
			ZP_RMW_DEC, ZP_RMW_INC, ZP_RMW_ASL, ZP_RMW_LSR, ZP_RMW_ROR, ZP_RMW_ROL,
			ZP_RMW_SLO, ZP_RMW_RLA, ZP_RMW_SRE, ZP_RMW_RRA, ZP_RMW_DCP, ZP_RMW_ISC,
			//[zero page READ]
			ZP_READ_EOR, ZP_READ_BIT, ZP_READ_ORA, ZP_READ_LDA, ZP_READ_LDY, ZP_READ_LDX, ZP_READ_CPX, ZP_READ_SBC, ZP_READ_CPY, ZP_READ_NOP, ZP_READ_ADC, ZP_READ_AND, ZP_READ_CMP, ZP_READ_LAX,

			//[indexed indirect READ] (addr,X)
			//[indexed indirect WRITE] (addr,X)
			IdxInd_Stage3, IdxInd_Stage4, IdxInd_Stage5,
			IdxInd_Stage6_READ_ORA, IdxInd_Stage6_READ_SBC, IdxInd_Stage6_READ_LDA, IdxInd_Stage6_READ_EOR, IdxInd_Stage6_READ_CMP, IdxInd_Stage6_READ_ADC, IdxInd_Stage6_READ_AND,
			IdxInd_Stage6_READ_LAX,
			IdxInd_Stage6_WRITE_STA, IdxInd_Stage6_WRITE_SAX,
			IdxInd_Stage6_RMW, //work happens in stage 7
			IdxInd_Stage7_RMW_SLO, IdxInd_Stage7_RMW_RLA, IdxInd_Stage7_RMW_SRE, IdxInd_Stage7_RMW_RRA, IdxInd_Stage7_RMW_ISC, IdxInd_Stage7_RMW_DCP, //unofficials
			IdxInd_Stage8_RMW,

			//[absolute indexed]
			AbsIdx_Stage3_X, AbsIdx_Stage3_Y, AbsIdx_Stage4,
			//[absolute indexed WRITE]
			AbsIdx_WRITE_Stage5_STA,
			AbsIdx_WRITE_Stage5_SHY, AbsIdx_WRITE_Stage5_SHX, //unofficials
			AbsIdx_WRITE_Stage5_ERROR,
			//[absolute indexed READ]
			AbsIdx_READ_Stage4,
			AbsIdx_READ_Stage5_LDA, AbsIdx_READ_Stage5_CMP, AbsIdx_READ_Stage5_SBC, AbsIdx_READ_Stage5_ADC, AbsIdx_READ_Stage5_EOR, AbsIdx_READ_Stage5_LDX, AbsIdx_READ_Stage5_AND, AbsIdx_READ_Stage5_ORA, AbsIdx_READ_Stage5_LDY, AbsIdx_READ_Stage5_NOP,
			AbsIdx_READ_Stage5_LAX, //unofficials
			AbsIdx_READ_Stage5_ERROR,
			//[absolute indexed RMW]
			AbsIdx_RMW_Stage5, AbsIdx_RMW_Stage7,
			AbsIdx_RMW_Stage6_ROR, AbsIdx_RMW_Stage6_DEC, AbsIdx_RMW_Stage6_INC, AbsIdx_RMW_Stage6_ASL, AbsIdx_RMW_Stage6_LSR, AbsIdx_RMW_Stage6_ROL,
			AbsIdx_RMW_Stage6_SLO, AbsIdx_RMW_Stage6_RLA, AbsIdx_RMW_Stage6_SRE, AbsIdx_RMW_Stage6_RRA, AbsIdx_RMW_Stage6_DCP, AbsIdx_RMW_Stage6_ISC, //unofficials

			IncS, DecS,
			PushPCL, PushPCH, PushP, PullP, PullPCL, PullPCH_NoInc, PushA, PullA_NoInc, PullP_NoInc,
			PushP_BRK, PushP_NMI, PushP_IRQ, PushP_Reset, PushDummy,
			FetchPCLVector, FetchPCHVector, //todo - may not need these ?? can reuse fetch2 and fetch3?

			//[implied] and [accumulator]
			Imp_ASL_A, Imp_ROL_A, Imp_ROR_A, Imp_LSR_A,
			Imp_SEC, Imp_CLI, Imp_SEI, Imp_CLD, Imp_CLC, Imp_CLV, Imp_SED,
			Imp_INY, Imp_DEY, Imp_INX, Imp_DEX,
			Imp_TSX, Imp_TXS, Imp_TAX, Imp_TAY, Imp_TYA, Imp_TXA,

			//[immediate]
			Imm_CMP, Imm_ADC, Imm_AND, Imm_SBC, Imm_ORA, Imm_EOR, Imm_CPY, Imm_CPX, Imm_ANC, Imm_ASR, Imm_ARR, Imm_LXA, Imm_AXS,
			Imm_LDA, Imm_LDX, Imm_LDY,
			Imm_Unsupported,

			//sub-ops
			NZ_X, NZ_Y, NZ_A,
			RelBranch_Stage2_BNE, RelBranch_Stage2_BPL, RelBranch_Stage2_BCC, RelBranch_Stage2_BCS, RelBranch_Stage2_BEQ, RelBranch_Stage2_BMI, RelBranch_Stage2_BVC, RelBranch_Stage2_BVS,
			RelBranch_Stage2, RelBranch_Stage3, RelBranch_Stage4,
			_Eor, _Bit, _Cpx, _Cpy, _Cmp, _Adc, _Sbc, _Ora, _And, _Anc, _Asr, _Arr, _Lxa, _Axs, //alu-related sub-ops

			//JMP (addr) 0x6C
			AbsInd_JMP_Stage4, AbsInd_JMP_Stage5,

			//[indirect indexed] (i.e. LDA (addr),Y	)
			IndIdx_Stage3, IndIdx_Stage4, IndIdx_READ_Stage5, IndIdx_WRITE_Stage5,
			IndIdx_WRITE_Stage6_STA, IndIdx_WRITE_Stage6_SHA,
			IndIdx_READ_Stage6_LDA, IndIdx_READ_Stage6_CMP, IndIdx_READ_Stage6_ORA, IndIdx_READ_Stage6_SBC, IndIdx_READ_Stage6_ADC, IndIdx_READ_Stage6_AND, IndIdx_READ_Stage6_EOR,
			IndIdx_READ_Stage6_LAX,
			IndIdx_RMW_Stage5,
			IndIdx_RMW_Stage6, //just reads from effective address
			IndIdx_RMW_Stage7_SLO, IndIdx_RMW_Stage7_RLA, IndIdx_RMW_Stage7_SRE, IndIdx_RMW_Stage7_RRA, IndIdx_RMW_Stage7_ISC, IndIdx_RMW_Stage7_DCP, //unofficials
			IndIdx_RMW_Stage8,

			End,
			End_ISpecial, //same as end, but preserves the iflag set by the instruction
			End_SuppressInterrupt,

			Jam
		}

		private void InitOpcodeHandlers()
		{
			//delegates arent faster than the switch. pretty sure. don't use it.
			//opcodeHandlers = new Action[] {
			//  Unsupported,Fetch1, Fetch1_Real, Fetch2, Fetch3,FetchDummy,
			//  NOP,JSR,IncPC,
			//  Abs_WRITE_STA, Abs_WRITE_STX, Abs_WRITE_STY,Abs_WRITE_SAX,Abs_READ_BIT, Abs_READ_LDA, Abs_READ_LDY, Abs_READ_ORA, Abs_READ_LDX, Abs_READ_CMP, Abs_READ_ADC, Abs_READ_CPX, Abs_READ_SBC, Abs_READ_AND, Abs_READ_EOR, Abs_READ_CPY, Abs_READ_NOP,
			//  Abs_READ_LAX,Abs_RMW_Stage4, Abs_RMW_Stage6,Abs_RMW_Stage5_INC, Abs_RMW_Stage5_DEC, Abs_RMW_Stage5_LSR, Abs_RMW_Stage5_ROL, Abs_RMW_Stage5_ASL, Abs_RMW_Stage5_ROR,Abs_RMW_Stage5_SLO, Abs_RMW_Stage5_RLA, Abs_RMW_Stage5_SRE, Abs_RMW_Stage5_RRA, Abs_RMW_Stage5_DCP, Abs_RMW_Stage5_ISC, 
			//  JMP_abs,ZpIdx_Stage3_X, ZpIdx_Stage3_Y,ZpIdx_RMW_Stage4, ZpIdx_RMW_Stage6,ZP_WRITE_STA, ZP_WRITE_STX, ZP_WRITE_STY, ZP_WRITE_SAX,ZP_RMW_Stage3, ZP_RMW_Stage5,
			//  ZP_RMW_DEC, ZP_RMW_INC, ZP_RMW_ASL, ZP_RMW_LSR, ZP_RMW_ROR, ZP_RMW_ROL,ZP_RMW_SLO, ZP_RMW_RLA, ZP_RMW_SRE, ZP_RMW_RRA, ZP_RMW_DCP, ZP_RMW_ISC,
			//  ZP_READ_EOR, ZP_READ_BIT, ZP_READ_ORA, ZP_READ_LDA, ZP_READ_LDY, ZP_READ_LDX, ZP_READ_CPX, ZP_READ_SBC, ZP_READ_CPY, ZP_READ_NOP, ZP_READ_ADC, ZP_READ_AND, ZP_READ_CMP, ZP_READ_LAX,
			//  IdxInd_Stage3, IdxInd_Stage4, IdxInd_Stage5,IdxInd_Stage6_READ_ORA, IdxInd_Stage6_READ_SBC, IdxInd_Stage6_READ_LDA, IdxInd_Stage6_READ_EOR, IdxInd_Stage6_READ_CMP, IdxInd_Stage6_READ_ADC, IdxInd_Stage6_READ_AND,
			//  IdxInd_Stage6_READ_LAX,IdxInd_Stage6_WRITE_STA, IdxInd_Stage6_WRITE_SAX,IdxInd_Stage6_RMW,IdxInd_Stage7_RMW_SLO, IdxInd_Stage7_RMW_RLA, IdxInd_Stage7_RMW_SRE, IdxInd_Stage7_RMW_RRA, IdxInd_Stage7_RMW_ISC, IdxInd_Stage7_RMW_DCP,
			//  IdxInd_Stage8_RMW,AbsIdx_Stage3_X, AbsIdx_Stage3_Y, AbsIdx_Stage4,AbsIdx_WRITE_Stage5_STA,AbsIdx_WRITE_Stage5_SHY, AbsIdx_WRITE_Stage5_SHX,AbsIdx_WRITE_Stage5_ERROR,AbsIdx_READ_Stage4,
			//  AbsIdx_READ_Stage5_LDA, AbsIdx_READ_Stage5_CMP, AbsIdx_READ_Stage5_SBC, AbsIdx_READ_Stage5_ADC, AbsIdx_READ_Stage5_EOR, AbsIdx_READ_Stage5_LDX, AbsIdx_READ_Stage5_AND, AbsIdx_READ_Stage5_ORA, AbsIdx_READ_Stage5_LDY, AbsIdx_READ_Stage5_NOP,
			//  AbsIdx_READ_Stage5_LAX,AbsIdx_READ_Stage5_ERROR,AbsIdx_RMW_Stage5, AbsIdx_RMW_Stage7,AbsIdx_RMW_Stage6_ROR, AbsIdx_RMW_Stage6_DEC, AbsIdx_RMW_Stage6_INC, AbsIdx_RMW_Stage6_ASL, AbsIdx_RMW_Stage6_LSR, AbsIdx_RMW_Stage6_ROL,
			//  AbsIdx_RMW_Stage6_SLO, AbsIdx_RMW_Stage6_RLA, AbsIdx_RMW_Stage6_SRE, AbsIdx_RMW_Stage6_RRA, AbsIdx_RMW_Stage6_DCP, AbsIdx_RMW_Stage6_ISC,IncS, DecS,
			//  PushPCL, PushPCH, PushP, PullP, PullPCL, PullPCH_NoInc, PushA, PullA_NoInc, PullP_NoInc,PushP_BRK, PushP_NMI, PushP_IRQ, PushP_Reset, PushDummy,FetchPCLVector, FetchPCHVector,
			//  Imp_ASL_A, Imp_ROL_A, Imp_ROR_A, Imp_LSR_A,Imp_SEC, Imp_CLI, Imp_SEI, Imp_CLD, Imp_CLC, Imp_CLV, Imp_SED,Imp_INY, Imp_DEY, Imp_INX, Imp_DEX,Imp_TSX, Imp_TXS, Imp_TAX, Imp_TAY, Imp_TYA, Imp_TXA,
			//  Imm_CMP, Imm_ADC, Imm_AND, Imm_SBC, Imm_ORA, Imm_EOR, Imm_CPY, Imm_CPX, Imm_ANC, Imm_ASR, Imm_ARR, Imm_LXA, Imm_AXS,Imm_LDA, Imm_LDX, Imm_LDY,
			//  Imm_Unsupported,NZ_X, NZ_Y, NZ_A,RelBranch_Stage2_BNE, RelBranch_Stage2_BPL, RelBranch_Stage2_BCC, RelBranch_Stage2_BCS, RelBranch_Stage2_BEQ, RelBranch_Stage2_BMI, RelBranch_Stage2_BVC, RelBranch_Stage2_BVS,
			//  RelBranch_Stage2, RelBranch_Stage3, RelBranch_Stage4,_Eor, _Bit, _Cpx, _Cpy, _Cmp, _Adc, _Sbc, _Ora, _And, _Anc, _Asr, _Arr, _Lxa, _Axs,
			//  AbsInd_JMP_Stage4, AbsInd_JMP_Stage5,IndIdx_Stage3, IndIdx_Stage4, IndIdx_READ_Stage5, IndIdx_WRITE_Stage5,
			//  IndIdx_WRITE_Stage6_STA, IndIdx_WRITE_Stage6_SHA,IndIdx_READ_Stage6_LDA, IndIdx_READ_Stage6_CMP, IndIdx_READ_Stage6_ORA, IndIdx_READ_Stage6_SBC, IndIdx_READ_Stage6_ADC, IndIdx_READ_Stage6_AND, IndIdx_READ_Stage6_EOR,
			//  IndIdx_READ_Stage6_LAX,IndIdx_RMW_Stage5,IndIdx_RMW_Stage6, IndIdx_RMW_Stage7_SLO, IndIdx_RMW_Stage7_RLA, IndIdx_RMW_Stage7_SRE, IndIdx_RMW_Stage7_RRA, IndIdx_RMW_Stage7_ISC, IndIdx_RMW_Stage7_DCP,IndIdx_RMW_Stage8,
			//  End,End_ISpecial,End_SuppressInterrupt,
			//};
		}

		private const int VOP_Fetch1 = 256;
		private const int VOP_RelativeStuff = 257;
		private const int VOP_RelativeStuff2 = 258;
		private const int VOP_RelativeStuff3 = 259;
		private const int VOP_NMI = 260;
		private const int VOP_IRQ = 261;
		private const int VOP_RESET = 262;
		private const int VOP_Fetch1_NoInterrupt = 263;
		private const int VOP_NUM = 264;

		//opcode bytes.. theoretically redundant with the temp variables? who knows.
		public int opcode;
		private byte opcode2, opcode3;

		private int ea, alu_temp; //cpu internal temp variables
		public int mi; //microcode index
		private bool iflag_pending; //iflag must be stored after it is checked in some cases (CLI and SEI).
		public bool rdy_freeze; //true if the CPU must be frozen

		//tracks whether an interrupt condition has popped up recently.
		//not sure if this is real or not but it helps with the branch_irq_hack
		private bool interrupt_pending;
		private bool branch_irq_hack; //see Uop.RelBranch_Stage3 for more details

		private bool Interrupted => NMI || (IRQ && !FlagI);

		private void FetchDummy()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory(PC);
			}
		}

		public void Execute(int cycles)
		{
			for (int i = 0; i < cycles; i++)
			{
				ExecuteOne();
			}
		}

		private byte value8, temp8;
		private ushort value16;
		private bool branch_taken = false;
		private bool my_iflag;
		private bool booltemp;
		private int tempint;
		private int lo, hi;
		public Action<TraceInfo> TraceCallback;

		private void Fetch1()
		{
			my_iflag = FlagI;
			FlagI = iflag_pending;
			if (!branch_irq_hack)
			{
				interrupt_pending = false;
				if (NMI)
				{
					TraceCallback?.Invoke(new(disassembly: "====NMI====", registerInfo: string.Empty));
					ea = NMIVector;
					opcode = VOP_NMI;
					NMI = false;
					mi = 0;
					ExecuteOneRetry();
					return;
				}

				if (IRQ && !my_iflag)
				{
					TraceCallback?.Invoke(new(disassembly: "====IRQ====", registerInfo: string.Empty));
					ea = IRQVector;
					opcode = VOP_IRQ;
					mi = 0;
					ExecuteOneRetry();
					return;
				}
			}

			mi = 0;
			opcode = VOP_Fetch1_NoInterrupt;

			Fetch1_Real();			
		}

		private void Fetch1_Real()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				if (debug) Console.WriteLine(State());
				branch_irq_hack = false;
				_link.OnExecFetch(PC);
				TraceCallback?.Invoke(State());
				opcode = _link.ReadMemory(PC++);
				mi = -1;
			}
		}

		private void Fetch2()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				opcode2 = _link.ReadMemory(PC++);
			}
		}

		private void Fetch3()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				opcode3 = _link.ReadMemory(PC++);
			}
		}

		private void PushPCH()
		{
			_link.WriteMemory((ushort)(S-- + 0x100), (byte)(PC >> 8));
		}

		private void PushPCL()
		{
			_link.WriteMemory((ushort)(S-- + 0x100), (byte)PC);
		}

		private void PushP_BRK()
		{
			FlagB = true;
			_link.WriteMemory((ushort)(S-- + 0x100), P);
			FlagI = true;
			ea = BRKVector;
		}

		private void PushP_IRQ()
		{
			FlagB = false;
			_link.WriteMemory((ushort)(S-- + 0x100), P);
			FlagI = true;
			ea = IRQVector;
		}

		private void PushP_NMI()
		{
			FlagB = false;
			_link.WriteMemory((ushort)(S-- + 0x100), P);
			FlagI = true; //is this right?
			ea = NMIVector;
		}

		private void PushP_Reset()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				ea = ResetVector;
				_link.DummyReadMemory((ushort)(S-- + 0x100));
				FlagI = true;
			}
		}

		private void PushDummy()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory((ushort)(S-- + 0x100));
			}
		}

		private void FetchPCLVector()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				if (ea == BRKVector && FlagB && NMI)
				{
					NMI = false;
					ea = NMIVector;
				}
				if (ea == IRQVector && !FlagB && NMI)
				{
					NMI = false;
					ea = NMIVector;
				}
				alu_temp = _link.ReadMemory((ushort)ea);
			}
		}

		private void FetchPCHVector()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp += _link.ReadMemory((ushort)(ea + 1)) << 8;
				PC = (ushort)alu_temp;
			}
		}

		private void Imp_INY()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory(PC); Y++; NZ_Y();
			}
		}

		private void Imp_DEY()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory(PC); Y--; NZ_Y();
			}
		}

		private void Imp_INX()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory(PC); X++; NZ_X();
			}
		}

		private void Imp_DEX()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory(PC); X--; NZ_X();
			}
		}

		private void NZ_A()
		{
			P = (byte)((P & 0x7D) | TableNZ[A]);
		}

		private void NZ_X()
		{
			P = (byte)((P & 0x7D) | TableNZ[X]);
		}

		private void NZ_Y()
		{
			P = (byte)((P & 0x7D) | TableNZ[Y]);

		}

		private void Imp_TSX()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory(PC); X = S; NZ_X();
			}
		}

		private void Imp_TXS()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory(PC); S = X;
			}
		}

		private void Imp_TAX()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory(PC); X = A; NZ_X();
			}
		}

		private void Imp_TAY()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory(PC); Y = A; NZ_Y();
			}
		}

		private void Imp_TYA()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory(PC); A = Y; NZ_A();
			}
		}

		private void Imp_TXA()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory(PC); A = X; NZ_A();
			}
		}

		private void Imp_SEI()
		{
			// not affected by RDY
			iflag_pending = true;

			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory(PC);
			}
		}

		private void Imp_CLI()
		{
			// not affected by RDY
			iflag_pending = false;

			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory(PC);
			}
		}

		private void Imp_SEC()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory(PC); FlagC = true;
			}
		}

		private void Imp_CLC()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory(PC); FlagC = false;
			}
		}

		private void Imp_SED()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory(PC); FlagD = true;
			}
		}

		private void Imp_CLD()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory(PC); FlagD = false;
			}
		}

		private void Imp_CLV()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory(PC); FlagV = false;
			}
		}

		private void Abs_WRITE_STA()
		{
			_link.WriteMemory((ushort)((opcode3 << 8) + opcode2), A);
		}

		private void Abs_WRITE_STX()
		{
			_link.WriteMemory((ushort)((opcode3 << 8) + opcode2), X);
		}

		private void Abs_WRITE_STY()
		{
			_link.WriteMemory((ushort)((opcode3 << 8) + opcode2), Y);
		}

		private void Abs_WRITE_SAX()
		{
			_link.WriteMemory((ushort)((opcode3 << 8) + opcode2), (byte)(X & A));

		}

		private void ZP_WRITE_STA()
		{
			_link.WriteMemory(opcode2, A);
		}

		private void ZP_WRITE_STY()
		{
			_link.WriteMemory(opcode2, Y);
		}

		private void ZP_WRITE_STX()
		{
			_link.WriteMemory(opcode2, X);
		}

		private void ZP_WRITE_SAX()
		{
			_link.WriteMemory(opcode2, (byte)(X & A));
		}

		private void IndIdx_Stage3()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				ea = _link.ReadMemory(opcode2);
			}
		}

		private void IndIdx_Stage4()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = ea + Y;
				ea = (_link.ReadMemory((byte)(opcode2 + 1)) << 8)
					| ((alu_temp & 0xFF));
			}
		}

		private void IndIdx_WRITE_Stage5()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.ReadMemory((ushort)ea);
				ea += (alu_temp >> 8) << 8;
			}
		}

		private void IndIdx_READ_Stage5()
		{
			if (!alu_temp.Bit(8))
			{
				mi++;
				ExecuteOneRetry();
				return;
			}
			else
			{
				rdy_freeze = !RDY;
				if (RDY)
				{
					_link.ReadMemory((ushort) ea);
					ea = (ushort) (ea + 0x100);
				}
			}
		}

		private void IndIdx_RMW_Stage5()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.ReadMemory((ushort)ea);
				if (alu_temp.Bit(8))
					ea = (ushort)(ea + 0x100);
				
			}
		}

		private void IndIdx_WRITE_Stage6_STA()
		{
			_link.WriteMemory((ushort)ea, A);
		}

		private void IndIdx_WRITE_Stage6_SHA()
		{
			_link.WriteMemory((ushort)ea, (byte)(A & X & 7));
		}

		private void IndIdx_READ_Stage6_LDA()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				A = _link.ReadMemory((ushort)ea);
				NZ_A();
			}
		}

		private void IndIdx_READ_Stage6_CMP()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)ea);
				_Cmp();
			}
		}

		private void IndIdx_READ_Stage6_AND()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)ea);
				_And();
			}
		}

		private void IndIdx_READ_Stage6_EOR()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)ea);
				_Eor();
			}
		}

		private void IndIdx_READ_Stage6_LAX()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				A = X = _link.ReadMemory((ushort)ea);
				NZ_A();
			}
		}

		private void IndIdx_READ_Stage6_ADC()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)ea);
				_Adc();
			}
		}

		private void IndIdx_READ_Stage6_SBC()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)ea);
				_Sbc();
			}
		}

		private void IndIdx_READ_Stage6_ORA()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)ea);
				_Ora();
			}
		}

		private void IndIdx_RMW_Stage6()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)ea);
			}
		}

		private void IndIdx_RMW_Stage7_SLO()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = (byte)alu_temp;
			FlagC = (value8 & 0x80) != 0;
			alu_temp = value8 = (byte)((value8 << 1));
			A |= value8;
			NZ_A();
		}

		private void IndIdx_RMW_Stage7_SRE()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = (byte)alu_temp;
			FlagC = (value8 & 1) != 0;
			alu_temp = value8 = (byte)(value8 >> 1);
			A ^= value8;
			NZ_A();
		}

		private void IndIdx_RMW_Stage7_RRA()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = temp8 = (byte)alu_temp;
			alu_temp = value8 = (byte)((value8 >> 1) | ((P & 1) << 7));
			FlagC = (temp8 & 1) != 0;
			_Adc();
		}

		private void IndIdx_RMW_Stage7_ISC()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = temp8 = (byte)alu_temp;
			alu_temp = value8 = (byte)(value8 + 1);
			_Sbc();
		}

		private void IndIdx_RMW_Stage7_DCP()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = temp8 = (byte)alu_temp;
			alu_temp = value8 = (byte)(value8 - 1);
			FlagC = (temp8 & 1) != 0;
			_Cmp();
		}

		private void IndIdx_RMW_Stage7_RLA()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = temp8 = (byte)alu_temp;
			alu_temp = value8 = (byte)((value8 << 1) | (P & 1));
			FlagC = (temp8 & 0x80) != 0;
			A &= value8;
			NZ_A();
		}

		private void IndIdx_RMW_Stage8()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
		}

		private void RelBranch_Stage2_BVS()
		{
			branch_taken = FlagV;
			RelBranch_Stage2();
		}

		private void RelBranch_Stage2_BVC()
		{
			branch_taken = !FlagV;
			RelBranch_Stage2();
		}

		private void RelBranch_Stage2_BMI()
		{
			branch_taken = FlagN;
			RelBranch_Stage2();
		}

		private void RelBranch_Stage2_BPL()
		{
			branch_taken = !FlagN;
			RelBranch_Stage2();
		}

		private void RelBranch_Stage2_BCS()
		{
			branch_taken = FlagC;
			RelBranch_Stage2();
		}

		private void RelBranch_Stage2_BCC()
		{
			branch_taken = !FlagC;
			RelBranch_Stage2();
		}

		private void RelBranch_Stage2_BEQ()
		{
			branch_taken = FlagZ;
			RelBranch_Stage2();
		}

		private void RelBranch_Stage2_BNE()
		{
			branch_taken = !FlagZ;
			RelBranch_Stage2();

		}

		private void RelBranch_Stage2()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				opcode2 = _link.ReadMemory(PC++);
				if (branch_taken)
				{
					branch_taken = false;
					//if the branch is taken, we enter a different bit of microcode to calculate the PC and complete the branch
					opcode = VOP_RelativeStuff;
					mi = -1;
				}
			}
		}

		private void RelBranch_Stage3()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory(PC);
				alu_temp = (byte)PC + (sbyte)opcode2;
				PC &= 0xFF00;
				PC |= (ushort)(alu_temp & 0xFF);

				if (alu_temp.Bit(8))
				{
					//we need to carry the add, and then we'll be ready to fetch the next instruction
					opcode = VOP_RelativeStuff2;
					mi = -1;
				}
				else
				{
					//to pass cpu_interrupts_v2/5-branch_delays_irq we need to handle a quirk here
					//if we decide to interrupt in the next cycle, this condition will cause it to get deferred by one instruction
					if (!interrupt_pending)
						branch_irq_hack = true;
				}
			}
		}

		private void RelBranch_Stage4()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory(PC);
				if (alu_temp.Bit(31))
					PC = (ushort)(PC - 0x100);
				else PC = (ushort)(PC + 0x100);
			}
		}

		private void NOP()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory(PC);
			}
		}

		private void DecS()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory((ushort) (0x100 | --S));
			}
		}

		private void IncS()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory((ushort) (0x100 | S++));
			}
		}

		private void JSR()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				PC = (ushort)((_link.ReadMemory(PC) << 8) + opcode2);
			}
		}

		private void PullP()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				P = _link.ReadMemory((ushort)(S++ + 0x100));
				FlagT = true; //force T always to remain true
			}
		}

		private void PullPCL()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				PC &= 0xFF00;
				PC |= _link.ReadMemory((ushort)(S++ + 0x100));
			}
		}

		private void PullPCH_NoInc()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				PC &= 0xFF;
				PC |= (ushort)(_link.ReadMemory((ushort)(S + 0x100)) << 8);
			}
		}

		private void Abs_READ_LDA()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				A = _link.ReadMemory((ushort)((opcode3 << 8) + opcode2));
				NZ_A();
			}
		}

		private void Abs_READ_LDY()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				Y = _link.ReadMemory((ushort)((opcode3 << 8) + opcode2));
				NZ_Y();
			}
		}

		private void Abs_READ_LDX()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				X = _link.ReadMemory((ushort)((opcode3 << 8) + opcode2));
				NZ_X();
			}
		}

		private void Abs_READ_BIT()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)((opcode3 << 8) + opcode2));
				_Bit();
			}
		}

		private void Abs_READ_LAX()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)((opcode3 << 8) + opcode2));
				A = _link.ReadMemory((ushort)((opcode3 << 8) + opcode2));
				X = A;
				NZ_A();
			}
		}

		private void Abs_READ_AND()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)((opcode3 << 8) + opcode2));
				_And();
			}
		}

		private void Abs_READ_EOR()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)((opcode3 << 8) + opcode2));
				_Eor();
			}
		}

		private void Abs_READ_ORA()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)((opcode3 << 8) + opcode2));
				_Ora();
			}
		}

		private void Abs_READ_ADC()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)((opcode3 << 8) + opcode2));
				_Adc();
			}
		}

		private void Abs_READ_CMP()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)((opcode3 << 8) + opcode2));
				_Cmp();
			}
		}

		private void Abs_READ_CPY()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)((opcode3 << 8) + opcode2));
				_Cpy();
			}
		}

		private void Abs_READ_NOP()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)((opcode3 << 8) + opcode2));
			}
		}

		private void Abs_READ_CPX()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)((opcode3 << 8) + opcode2));
				_Cpx();
			}
		}

		private void Abs_READ_SBC()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)((opcode3 << 8) + opcode2));
				_Sbc();
			}
		}

		private void ZpIdx_Stage3_X()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.ReadMemory(opcode2);
				opcode2 = (byte)(opcode2 + X); //a bit sneaky to shove this into opcode2... but we can reuse all the zero page uops if we do that
			}
		}

		private void ZpIdx_Stage3_Y()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.ReadMemory(opcode2);
				opcode2 = (byte)(opcode2 + Y); //a bit sneaky to shove this into opcode2... but we can reuse all the zero page uops if we do that
			}
		}

		private void ZpIdx_RMW_Stage4()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory(opcode2);
			}
		}

		private void ZpIdx_RMW_Stage6()
		{
			_link.WriteMemory(opcode2, (byte)alu_temp);
		}

		private void ZP_READ_EOR()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory(opcode2);
				_Eor();
			}
		}

		private void ZP_READ_BIT()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory(opcode2);
				_Bit();
			}
		}

		private void ZP_READ_LDA()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				A = _link.ReadMemory(opcode2);
				NZ_A();
			}
		}

		private void ZP_READ_LDY()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				Y = _link.ReadMemory(opcode2);
				NZ_Y();
			}
		}

		private void ZP_READ_LDX()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				X = _link.ReadMemory(opcode2);
				NZ_X();
			}
		}

		private void ZP_READ_LAX()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				//?? is this right??
				X = _link.ReadMemory(opcode2);
				A = X;
				NZ_A();
			}
		}

		private void ZP_READ_CPY()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory(opcode2);
				_Cpy();
			}
		}

		private void ZP_READ_CMP()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory(opcode2);
				_Cmp();
			}
		}

		private void ZP_READ_CPX()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory(opcode2);
				_Cpx();
			}
		}

		private void ZP_READ_ORA()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory(opcode2);
				_Ora();
			}
		}

		private void ZP_READ_NOP()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.ReadMemory(opcode2); //just a dummy
			}
		}

		private void ZP_READ_SBC()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory(opcode2);
				_Sbc();
			}
		}

		private void ZP_READ_ADC()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory(opcode2);
				_Adc();
			}
		}

		private void ZP_READ_AND()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory(opcode2);
				_And();
			}
		}

		private void _Cpx()
		{
			value8 = (byte)alu_temp;
			value16 = (ushort)(X - value8);
			FlagC = (X >= value8);
			P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
		}

		private void _Cpy()
		{
			value8 = (byte)alu_temp;
			value16 = (ushort)(Y - value8);
			FlagC = (Y >= value8);
			P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
		}

		private void _Cmp()
		{
			value8 = (byte)alu_temp;
			value16 = (ushort)(A - value8);
			FlagC = (A >= value8);
			P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
		}

		private void _Bit()
		{
			FlagN = (alu_temp & 0x80) != 0;
			FlagV = (alu_temp & 0x40) != 0;
			FlagZ = (A & alu_temp) == 0;
		}

		private void _Eor()
		{
			A ^= (byte)alu_temp;
			NZ_A();
		}

		private void _And()
		{
			A &= (byte)alu_temp;
			NZ_A();
		}

		private void _Ora()
		{
			A |= (byte)alu_temp;
			NZ_A();
		}

		private void _Anc()
		{
			A &= (byte)alu_temp;
			FlagC = A.Bit(7);
			NZ_A();
		}

		private void _Asr()
		{
			A &= (byte)alu_temp;
			FlagC = A.Bit(0);
			A >>= 1;
			NZ_A();
		}

		private void _Axs()
		{
			X &= A;
			alu_temp = X - (byte)alu_temp;
			X = (byte)alu_temp;
			FlagC = !alu_temp.Bit(8);
			NZ_X();
		}

		private void _Arr()
		{
			{
				A &= (byte)alu_temp;
				booltemp = A.Bit(0);
				A = (byte)((A >> 1) | (FlagC ? 0x80 : 0x00));
				FlagC = booltemp;
				if (A.Bit(5))
					if (A.Bit(6))
					{ FlagC = true; FlagV = false; }
					else { FlagV = true; FlagC = false; }
				else if (A.Bit(6))
				{ FlagV = true; FlagC = true; }
				else { FlagV = false; FlagC = false; }
				FlagZ = (A == 0);
			}
		}

		private void _Lxa()
		{
			A |= 0xFF; //there is some debate about what this should be. it may depend on the 6502 variant. this is suggested by qeed's doc for the nes and passes blargg's instruction test
			A &= (byte)alu_temp;
			X = A;
			NZ_A();
		}

		private void _Sbc()
		{
			{
				value8 = (byte)alu_temp;
				tempint = A - value8 - (FlagC ? 0 : 1);
				if (FlagD && BCD_Enabled)
				{
					lo = (A & 0x0F) - (value8 & 0x0F) - (FlagC ? 0 : 1);
					hi = (A & 0xF0) - (value8 & 0xF0);
					if ((lo & 0xF0) != 0) lo -= 0x06;
					if ((lo & 0x80) != 0) hi -= 0x10;
					if ((hi & 0x0F00) != 0) hi -= 0x60;
					FlagV = ((A ^ value8) & (A ^ tempint) & 0x80) != 0;
					FlagC = (hi & 0xFF00) == 0;
					A = (byte)((lo & 0x0F) | (hi & 0xF0));
				}
				else
				{
					FlagV = ((A ^ value8) & (A ^ tempint) & 0x80) != 0;
					FlagC = tempint >= 0;
					A = (byte)tempint;
				}
				NZ_A();
			}
		}

		private void _Adc()
		{
			//TODO - an extra cycle penalty on 65C02 only
			value8 = (byte)alu_temp;
			if (FlagD && BCD_Enabled)
			{
				tempint = (A & 0x0F) + (value8 & 0x0F) + (FlagC ? 0x01 : 0x00);
				if (tempint > 0x09)
					tempint += 0x06;
				tempint = (tempint & 0x0F) + (A & 0xF0) + (value8 & 0xF0) + (tempint > 0x0F ? 0x10 : 0x00);
				FlagV = (~(A ^ value8) & (A ^ tempint) & 0x80) != 0;
				FlagZ = ((A + value8 + (FlagC ? 1 : 0)) & 0xFF) == 0;
				FlagN = (tempint & 0x80) != 0;
				if ((tempint & 0x1F0) > 0x090)
					tempint += 0x060;
				FlagC = tempint > 0xFF;
				A = (byte)(tempint & 0xFF);
			}
			else
			{
				tempint = value8 + A + (FlagC ? 1 : 0);
				FlagV = (~(A ^ value8) & (A ^ tempint) & 0x80) != 0;
				FlagC = tempint > 0xFF;
				A = (byte)tempint;
				NZ_A();
			}
		}

		private void Unsupported()
		{


		}

		private void Imm_EOR()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory(PC++);
				_Eor();
			}
		}

		private void Imm_ANC()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory(PC++);
				_Anc();
			}
		}

		private void Imm_ASR()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory(PC++);
				_Asr();
			}
		}

		private void Imm_AXS()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory(PC++);
				_Axs();
			}
		}

		private void Imm_ARR()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory(PC++);
				_Arr();
			}
		}

		private void Imm_LXA()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory(PC++);
				_Lxa();
			}
		}

		private void Imm_ORA()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory(PC++);
				_Ora();
			}
		}

		private void Imm_CPY()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory(PC++);
				_Cpy();
			}
		}

		private void Imm_CPX()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory(PC++);
				_Cpx();
			}
		}

		private void Imm_CMP()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory(PC++);
				_Cmp();
			}
		}

		private void Imm_SBC()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory(PC++);
				_Sbc();
			}
		}

		private void Imm_AND()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory(PC++);
				_And();
			}
		}

		private void Imm_ADC()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory(PC++);
				_Adc();
			}
		}

		private void Imm_LDA()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				A = _link.ReadMemory(PC++);
				NZ_A();
			}
		}

		private void Imm_LDX()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				X = _link.ReadMemory(PC++);
				NZ_X();
			}
		}

		private void Imm_LDY()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				Y = _link.ReadMemory(PC++);
				NZ_Y();
			}
		}

		private void Imm_Unsupported()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.ReadMemory(PC++);
			}
		}

		private void IdxInd_Stage3()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory(opcode2);
				alu_temp = (opcode2 + X) & 0xFF;
			}
		}

		private void IdxInd_Stage4()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				ea = _link.ReadMemory((ushort)alu_temp);
			}
		}

		private void IdxInd_Stage5()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				ea += (_link.ReadMemory((byte)(alu_temp + 1)) << 8);
			}
		}

		private void IdxInd_Stage6_READ_LDA()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				//TODO make uniform with others
				A = _link.ReadMemory((ushort)ea);
				NZ_A();
			}
		}

		private void IdxInd_Stage6_READ_ORA()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)ea);
				_Ora();
			}
		}

		private void IdxInd_Stage6_READ_LAX()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				A = X = _link.ReadMemory((ushort)ea);
				NZ_A();
			}
		}

		private void IdxInd_Stage6_READ_CMP()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)ea);
				_Cmp();
			}
		}

		private void IdxInd_Stage6_READ_ADC()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)ea);
				_Adc();
			}
		}

		private void IdxInd_Stage6_READ_AND()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)ea);
				_And();
			}
		}

		private void IdxInd_Stage6_READ_EOR()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)ea);
				_Eor();
			}
		}

		private void IdxInd_Stage6_READ_SBC()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)ea);
				_Sbc();
			}
		}

		private void IdxInd_Stage6_WRITE_STA()
		{
			_link.WriteMemory((ushort)ea, A);
		}

		private void IdxInd_Stage6_WRITE_SAX()
		{
			alu_temp = A & X;
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			//flag writing skipped on purpose
		}

		private void IdxInd_Stage6_RMW()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)ea);
			}
		}

		private void IdxInd_Stage7_RMW_SLO()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = (byte)alu_temp;
			FlagC = (value8 & 0x80) != 0;
			alu_temp = value8 = (byte)((value8 << 1));
			A |= value8;
			NZ_A();
		}

		private void IdxInd_Stage7_RMW_ISC()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = (byte)alu_temp;
			alu_temp = value8 = (byte)(value8 + 1);
			_Sbc();
		}

		private void IdxInd_Stage7_RMW_DCP()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = temp8 = (byte)alu_temp;
			alu_temp = value8 = (byte)(value8 - 1);
			FlagC = (temp8 & 1) != 0;
			_Cmp();
		}

		private void IdxInd_Stage7_RMW_SRE()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = (byte)alu_temp;
			FlagC = (value8 & 1) != 0;
			alu_temp = value8 = (byte)(value8 >> 1);
			A ^= value8;
			NZ_A();
		}

		private void IdxInd_Stage7_RMW_RRA()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = (byte)alu_temp;
			value8 = temp8 = (byte)alu_temp;
			alu_temp = value8 = (byte)((value8 >> 1) | ((P & 1) << 7));
			FlagC = (temp8 & 1) != 0;
			_Adc();
		}

		private void IdxInd_Stage7_RMW_RLA()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = temp8 = (byte)alu_temp;
			alu_temp = value8 = (byte)((value8 << 1) | (P & 1));
			FlagC = (temp8 & 0x80) != 0;
			A &= value8;
			NZ_A();
		}

		private void IdxInd_Stage8_RMW()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
		}

		private void PushP()
		{
			FlagB = true;
			_link.WriteMemory((ushort)(S-- + 0x100), P);
		}

		private void PushA()
		{
			_link.WriteMemory((ushort)(S-- + 0x100), A);
		}

		private void PullA_NoInc()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				A = _link.ReadMemory((ushort)(S + 0x100));
				NZ_A();
			}
		}

		private void PullP_NoInc()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				my_iflag = FlagI;
				P = _link.ReadMemory((ushort)(S + 0x100));
				iflag_pending = FlagI;
				FlagI = my_iflag;
				FlagT = true; //force T always to remain true
			}
		}

		private void Imp_ASL_A()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory(PC);
				FlagC = (A & 0x80) != 0;
				A = (byte)(A << 1);
				NZ_A();
			}
		}

		private void Imp_ROL_A()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory(PC);
				temp8 = A;
				A = (byte)((A << 1) | (P & 1));
				FlagC = (temp8 & 0x80) != 0;
				NZ_A();
			}
		}

		private void Imp_ROR_A()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory(PC);
				temp8 = A;
				A = (byte)((A >> 1) | ((P & 1) << 7));
				FlagC = (temp8 & 1) != 0;
				NZ_A();
			}
		}

		private void Imp_LSR_A()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.DummyReadMemory(PC);
				FlagC = (A & 1) != 0;
				A = (byte)(A >> 1);
				NZ_A();
			}
		}

		private void JMP_abs()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				PC = (ushort)((_link.ReadMemory(PC) << 8) + opcode2);
			}
		}

		private void IncPC()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				_link.ReadMemory(PC);
				PC++;
			}
		}

		private void ZP_RMW_Stage3()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory(opcode2);
			}
		}

		private void ZP_RMW_Stage5()
		{
			_link.WriteMemory(opcode2, (byte)alu_temp);
		}

		private void ZP_RMW_INC()
		{
			_link.WriteMemory(opcode2, (byte)alu_temp);
			alu_temp = (byte)((alu_temp + 1) & 0xFF);
			P = (byte)((P & 0x7D) | TableNZ[alu_temp]);
		}

		private void ZP_RMW_DEC()
		{
			_link.WriteMemory(opcode2, (byte)alu_temp);
			alu_temp = (byte)((alu_temp - 1) & 0xFF);
			P = (byte)((P & 0x7D) | TableNZ[alu_temp]);
		}

		private void ZP_RMW_ASL()
		{
			_link.WriteMemory(opcode2, (byte)alu_temp);
			value8 = (byte)alu_temp;
			FlagC = (value8 & 0x80) != 0;
			alu_temp = value8 = (byte)(value8 << 1);
			P = (byte)((P & 0x7D) | TableNZ[value8]);
		}

		private void ZP_RMW_SRE()
		{
			_link.WriteMemory(opcode2, (byte)alu_temp);
			value8 = (byte)alu_temp;
			FlagC = (value8 & 1) != 0;
			alu_temp = value8 = (byte)(value8 >> 1);
			A ^= value8;
			NZ_A();
		}

		private void ZP_RMW_RRA()
		{
			_link.WriteMemory(opcode2, (byte)alu_temp);
			value8 = temp8 = (byte)alu_temp;
			alu_temp = value8 = (byte)((value8 >> 1) | ((P & 1) << 7));
			FlagC = (temp8 & 1) != 0;
			_Adc();
		}

		private void ZP_RMW_DCP()
		{
			_link.WriteMemory(opcode2, (byte)alu_temp);
			value8 = temp8 = (byte)alu_temp;
			alu_temp = value8 = (byte)(value8 - 1);
			FlagC = (temp8 & 1) != 0;
			_Cmp();
		}

		private void ZP_RMW_LSR()
		{
			_link.WriteMemory(opcode2, (byte)alu_temp);
			value8 = (byte)alu_temp;
			FlagC = (value8 & 1) != 0;
			alu_temp = value8 = (byte)(value8 >> 1);
			P = (byte)((P & 0x7D) | TableNZ[value8]);

		}

		private void ZP_RMW_ROR()
		{
			_link.WriteMemory(opcode2, (byte)alu_temp);
			value8 = temp8 = (byte)alu_temp;
			alu_temp = value8 = (byte)((value8 >> 1) | ((P & 1) << 7));
			FlagC = (temp8 & 1) != 0;
			P = (byte)((P & 0x7D) | TableNZ[value8]);
		}

		private void ZP_RMW_ROL()
		{
			_link.WriteMemory(opcode2, (byte)alu_temp);
			value8 = temp8 = (byte)alu_temp;
			alu_temp = value8 = (byte)((value8 << 1) | (P & 1));
			FlagC = (temp8 & 0x80) != 0;
			P = (byte)((P & 0x7D) | TableNZ[value8]);
		}

		private void ZP_RMW_SLO()
		{
			_link.WriteMemory(opcode2, (byte)alu_temp);
			value8 = (byte)alu_temp;
			FlagC = (value8 & 0x80) != 0;
			alu_temp = value8 = (byte)((value8 << 1));
			A |= value8;
			NZ_A();
		}

		private void ZP_RMW_ISC()
		{
			_link.WriteMemory(opcode2, (byte)alu_temp);
			value8 = (byte)alu_temp;
			alu_temp = value8 = (byte)(value8 + 1);
			_Sbc();
		}

		private void ZP_RMW_RLA()
		{
			_link.WriteMemory(opcode2, (byte)alu_temp);
			value8 = temp8 = (byte)alu_temp;
			alu_temp = value8 = (byte)((value8 << 1) | (P & 1));
			FlagC = (temp8 & 0x80) != 0;
			A &= value8;
			NZ_A();
		}

		private void AbsIdx_Stage3_Y()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				opcode3 = _link.ReadMemory(PC++);
				alu_temp = opcode2 + Y;
				ea = (opcode3 << 8) + (alu_temp & 0xFF);

				//new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_Stage4, Uop.AbsIdx_WRITE_Stage5_STA, Uop.End },
			}
		}

		private void AbsIdx_Stage3_X()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				opcode3 = _link.ReadMemory(PC++);
				alu_temp = opcode2 + X;
				ea = (opcode3 << 8) + (alu_temp & 0xFF);
			}
		}

		private void AbsIdx_READ_Stage4()
		{
			if (!alu_temp.Bit(8))
			{
				mi++;
				ExecuteOneRetry();
			}
			else
			{
				rdy_freeze = !RDY;
				if (RDY)
				{
					alu_temp = _link.ReadMemory((ushort) ea);
					ea = (ushort) (ea + 0x100);
				}
			}
		}

		private void AbsIdx_Stage4()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				//bleh.. redundant code to make sure we don't clobber alu_temp before using it to decide whether to change ea

				if (alu_temp.Bit(8))
				{
					alu_temp = _link.ReadMemory((ushort)ea);
					ea = (ushort)(ea + 0x100);
				}
				else alu_temp = _link.ReadMemory((ushort)ea);
			}
		}

		private void AbsIdx_WRITE_Stage5_STA()
		{
			_link.WriteMemory((ushort)ea, A);
		}

		private void AbsIdx_WRITE_Stage5_SHY()
		{
			alu_temp = Y & (ea >> 8);
			ea = (ea & 0xFF) | (alu_temp << 8); //"(the bank where the value is stored may be equal to the value stored)" -- more like IS.
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
		}

		private void AbsIdx_WRITE_Stage5_SHX()
		{
			alu_temp = X & (ea >> 8);
			ea = (ea & 0xFF) | (alu_temp << 8); //"(the bank where the value is stored may be equal to the value stored)" -- more like IS.
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
		}

		private void AbsIdx_WRITE_Stage5_ERROR()
		{
			S = (byte)(X & A);
			_link.WriteMemory((ushort)ea, (byte)(S & (opcode3+1)));
		}

		private void AbsIdx_RMW_Stage5()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)ea);
			}
		}

		private void AbsIdx_RMW_Stage7()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
		}

		private void AbsIdx_RMW_Stage6_DEC()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			alu_temp = value8 = (byte)(alu_temp - 1);
			P = (byte)((P & 0x7D) | TableNZ[value8]);
		}

		private void AbsIdx_RMW_Stage6_DCP()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			alu_temp = value8 = (byte)(alu_temp - 1);
			_Cmp();
		}

		private void AbsIdx_RMW_Stage6_ISC()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			alu_temp = value8 = (byte)(alu_temp + 1);
			_Sbc();
		}

		private void AbsIdx_RMW_Stage6_INC()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			alu_temp = value8 = (byte)(alu_temp + 1);
			P = (byte)((P & 0x7D) | TableNZ[value8]);

		}

		private void AbsIdx_RMW_Stage6_ROL()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = temp8 = (byte)alu_temp;
			alu_temp = value8 = (byte)((value8 << 1) | (P & 1));
			FlagC = (temp8 & 0x80) != 0;
			P = (byte)((P & 0x7D) | TableNZ[value8]);
		}

		private void AbsIdx_RMW_Stage6_LSR()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = (byte)alu_temp;
			FlagC = (value8 & 1) != 0;
			alu_temp = value8 = (byte)(value8 >> 1);
			P = (byte)((P & 0x7D) | TableNZ[value8]);
		}

		private void AbsIdx_RMW_Stage6_SLO()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = (byte)alu_temp;
			FlagC = (value8 & 0x80) != 0;
			alu_temp = value8 = (byte)(value8 << 1);
			A |= value8;
			NZ_A();
		}

		private void AbsIdx_RMW_Stage6_SRE()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = (byte)alu_temp;
			FlagC = (value8 & 1) != 0;
			alu_temp = value8 = (byte)(value8 >> 1);
			A ^= value8;
			NZ_A();
		}

		private void AbsIdx_RMW_Stage6_RRA()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = temp8 = (byte)alu_temp;
			alu_temp = value8 = (byte)((value8 >> 1) | ((P & 1) << 7));
			FlagC = (temp8 & 1) != 0;
			_Adc();
		}

		private void AbsIdx_RMW_Stage6_RLA()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = temp8 = (byte)alu_temp;
			alu_temp = value8 = (byte)((value8 << 1) | (P & 1));
			FlagC = (temp8 & 0x80) != 0;
			A &= value8;
			NZ_A();
		}

		private void AbsIdx_RMW_Stage6_ASL()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = (byte)alu_temp;
			FlagC = (value8 & 0x80) != 0;
			alu_temp = value8 = (byte)(value8 << 1);
			P = (byte)((P & 0x7D) | TableNZ[value8]);
		}

		private void AbsIdx_RMW_Stage6_ROR()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = temp8 = (byte)alu_temp;
			alu_temp = value8 = (byte)((value8 >> 1) | ((P & 1) << 7));
			FlagC = (temp8 & 1) != 0;
			P = (byte)((P & 0x7D) | TableNZ[value8]);
		}

		private void AbsIdx_READ_Stage5_LDA()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				A = _link.ReadMemory((ushort)ea);
				NZ_A();
			}
		}

		private void AbsIdx_READ_Stage5_LDX()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				X = _link.ReadMemory((ushort)ea);
				NZ_X();
			}
		}

		private void AbsIdx_READ_Stage5_LAX()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				A = _link.ReadMemory((ushort)ea);
				X = A;
				NZ_A();
			}
		}

		private void AbsIdx_READ_Stage5_LDY()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				Y = _link.ReadMemory((ushort)ea);
				NZ_Y();
			}
		}

		private void AbsIdx_READ_Stage5_ORA()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)ea);
				_Ora();
			}
		}

		private void AbsIdx_READ_Stage5_NOP()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)ea);
			}
		}

		private void AbsIdx_READ_Stage5_CMP()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)ea);
				_Cmp();
			}
		}

		private void AbsIdx_READ_Stage5_SBC()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)ea);
				_Sbc();
			}
		}

		private void AbsIdx_READ_Stage5_ADC()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)ea);
				_Adc();
			}
		}

		private void AbsIdx_READ_Stage5_EOR()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)ea);
				_Eor();
			}
		}

		private void AbsIdx_READ_Stage5_AND()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)ea);
				_And();
			}
		}

		private void AbsIdx_READ_Stage5_ERROR()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				alu_temp = _link.ReadMemory((ushort)ea);
				S &= (byte)alu_temp;
				X = S;
				A = S;
				P = (byte)((P & 0x7D) | TableNZ[S]);
			}
		}

		private void AbsInd_JMP_Stage4()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				ea = (opcode3 << 8) + opcode2;
				alu_temp = _link.ReadMemory((ushort)ea);
			}
		}

		private void AbsInd_JMP_Stage5()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				ea = (opcode3 << 8) + (byte)(opcode2 + 1);
				alu_temp += _link.ReadMemory((ushort)ea) << 8;
				PC = (ushort)alu_temp;
			}
		}

		private void Abs_RMW_Stage4()
		{
			rdy_freeze = !RDY;
			if (RDY)
			{
				ea = (opcode3 << 8) + opcode2;
				alu_temp = _link.ReadMemory((ushort)ea);
			}
		}

		private void Abs_RMW_Stage5_INC()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = (byte)(alu_temp + 1);
			alu_temp = value8;
			P = (byte)((P & 0x7D) | TableNZ[value8]);
		}

		private void Abs_RMW_Stage5_DEC()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = (byte)(alu_temp - 1);
			alu_temp = value8;
			P = (byte)((P & 0x7D) | TableNZ[value8]);
		}

		private void Abs_RMW_Stage5_DCP()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = (byte)(alu_temp - 1);
			alu_temp = value8;
			_Cmp();
		}

		private void Abs_RMW_Stage5_ISC()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = (byte)(alu_temp + 1);
			alu_temp = value8;
			_Sbc();
		}

		private void Abs_RMW_Stage5_ASL()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = (byte)alu_temp;
			FlagC = (value8 & 0x80) != 0;
			alu_temp = value8 = (byte)(value8 << 1);
			P = (byte)((P & 0x7D) | TableNZ[value8]);
		}

		private void Abs_RMW_Stage5_ROR()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = temp8 = (byte)alu_temp;
			alu_temp = value8 = (byte)((value8 >> 1) | ((P & 1) << 7));
			FlagC = (temp8 & 1) != 0;
			P = (byte)((P & 0x7D) | TableNZ[value8]);
		}

		private void Abs_RMW_Stage5_SLO()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = (byte)alu_temp;
			FlagC = (value8 & 0x80) != 0;
			alu_temp = value8 = (byte)(value8 << 1);
			A |= value8;
			NZ_A();
		}

		private void Abs_RMW_Stage5_RLA()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = temp8 = (byte)alu_temp;
			alu_temp = value8 = (byte)((value8 << 1) | (P & 1));
			FlagC = (temp8 & 0x80) != 0;
			A &= value8;
			NZ_A();
		}

		private void Abs_RMW_Stage5_SRE()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = (byte)alu_temp;
			FlagC = (value8 & 1) != 0;
			alu_temp = value8 = (byte)(value8 >> 1);
			A ^= value8;
			NZ_A();
		}

		private void Abs_RMW_Stage5_RRA()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = temp8 = (byte)alu_temp;
			alu_temp = value8 = (byte)((value8 >> 1) | ((P & 1) << 7));
			FlagC = (temp8 & 1) != 0;
			_Adc();
		}

		private void Abs_RMW_Stage5_ROL()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = temp8 = (byte)alu_temp;
			alu_temp = value8 = (byte)((value8 << 1) | (P & 1));
			FlagC = (temp8 & 0x80) != 0;
			P = (byte)((P & 0x7D) | TableNZ[value8]);
		}

		private void Abs_RMW_Stage5_LSR()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
			value8 = (byte)alu_temp;
			FlagC = (value8 & 1) != 0;
			alu_temp = value8 = (byte)(value8 >> 1);
			P = (byte)((P & 0x7D) | TableNZ[value8]);
		}

		private void Abs_RMW_Stage6()
		{
			_link.WriteMemory((ushort)ea, (byte)alu_temp);
		}

		private void End_ISpecial()
		{
			opcode = VOP_Fetch1;
			mi = 0;
			ExecuteOneRetry();
			return;
		}

		private void End_SuppressInterrupt()
		{
			opcode = VOP_Fetch1_NoInterrupt;
			mi = 0;
			ExecuteOneRetry();
			return;
		}

		private void End()
		{
			opcode = VOP_Fetch1;
			mi = 0;
			iflag_pending = FlagI;
			ExecuteOneRetry();
		}

		private void End_BranchSpecial()
		{
			End();
		}

		private void Jam()
		{
			rdy_freeze = true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ExecuteOneRetry()
		{
			//don't know whether this system is any faster. hard to get benchmarks someone else try it?
			//Uop uop = (Uop)CompiledMicrocode[MicrocodeIndex[opcode] + mi];
			Uop uop = Microcode[opcode][mi];
			switch (uop)
			{
				default: throw new InvalidOperationException();
				case Uop.Fetch1: Fetch1(); break;
				case Uop.Fetch1_Real: Fetch1_Real(); break;
				case Uop.Fetch2: Fetch2(); break;
				case Uop.Fetch3: Fetch3(); break;
				case Uop.FetchDummy: FetchDummy(); break;
				case Uop.PushPCH: PushPCH(); break;
				case Uop.PushPCL: PushPCL(); break;
				case Uop.PushP_BRK: PushP_BRK(); break;
				case Uop.PushP_IRQ: PushP_IRQ(); break;
				case Uop.PushP_NMI: PushP_NMI(); break;
				case Uop.PushP_Reset: PushP_Reset(); break;
				case Uop.PushDummy: PushDummy(); break;
				case Uop.FetchPCLVector: FetchPCLVector(); break;
				case Uop.FetchPCHVector: FetchPCHVector(); break;
				case Uop.Imp_INY: Imp_INY(); break;
				case Uop.Imp_DEY: Imp_DEY(); break;
				case Uop.Imp_INX: Imp_INX(); break;
				case Uop.Imp_DEX: Imp_DEX(); break;
				case Uop.NZ_A: NZ_A(); break;
				case Uop.NZ_X: NZ_X(); break;
				case Uop.NZ_Y: NZ_Y(); break;
				case Uop.Imp_TSX: Imp_TSX(); break;
				case Uop.Imp_TXS: Imp_TXS(); break;
				case Uop.Imp_TAX: Imp_TAX(); break;
				case Uop.Imp_TAY: Imp_TAY(); break;
				case Uop.Imp_TYA: Imp_TYA(); break;
				case Uop.Imp_TXA: Imp_TXA(); break;
				case Uop.Imp_SEI: Imp_SEI(); break;
				case Uop.Imp_CLI: Imp_CLI(); break;
				case Uop.Imp_SEC: Imp_SEC(); break;
				case Uop.Imp_CLC: Imp_CLC(); break;
				case Uop.Imp_SED: Imp_SED(); break;
				case Uop.Imp_CLD: Imp_CLD(); break;
				case Uop.Imp_CLV: Imp_CLV(); break;
				case Uop.Abs_WRITE_STA: Abs_WRITE_STA(); break;
				case Uop.Abs_WRITE_STX: Abs_WRITE_STX(); break;
				case Uop.Abs_WRITE_STY: Abs_WRITE_STY(); break;
				case Uop.Abs_WRITE_SAX: Abs_WRITE_SAX(); break;
				case Uop.ZP_WRITE_STA: ZP_WRITE_STA(); break;
				case Uop.ZP_WRITE_STY: ZP_WRITE_STY(); break;
				case Uop.ZP_WRITE_STX: ZP_WRITE_STX(); break;
				case Uop.ZP_WRITE_SAX: ZP_WRITE_SAX(); break;
				case Uop.IndIdx_Stage3: IndIdx_Stage3(); break;
				case Uop.IndIdx_Stage4: IndIdx_Stage4(); break;
				case Uop.IndIdx_WRITE_Stage5: IndIdx_WRITE_Stage5(); break;
				case Uop.IndIdx_READ_Stage5: IndIdx_READ_Stage5(); break;
				case Uop.IndIdx_RMW_Stage5: IndIdx_RMW_Stage5(); break;
				case Uop.IndIdx_WRITE_Stage6_STA: IndIdx_WRITE_Stage6_STA(); break;
				case Uop.IndIdx_WRITE_Stage6_SHA: IndIdx_WRITE_Stage6_SHA(); break;
				case Uop.IndIdx_READ_Stage6_LDA: IndIdx_READ_Stage6_LDA(); break;
				case Uop.IndIdx_READ_Stage6_CMP: IndIdx_READ_Stage6_CMP(); break;
				case Uop.IndIdx_READ_Stage6_AND: IndIdx_READ_Stage6_AND(); break;
				case Uop.IndIdx_READ_Stage6_EOR: IndIdx_READ_Stage6_EOR(); break;
				case Uop.IndIdx_READ_Stage6_LAX: IndIdx_READ_Stage6_LAX(); break;
				case Uop.IndIdx_READ_Stage6_ADC: IndIdx_READ_Stage6_ADC(); break;
				case Uop.IndIdx_READ_Stage6_SBC: IndIdx_READ_Stage6_SBC(); break;
				case Uop.IndIdx_READ_Stage6_ORA: IndIdx_READ_Stage6_ORA(); break;
				case Uop.IndIdx_RMW_Stage6: IndIdx_RMW_Stage6(); break;
				case Uop.IndIdx_RMW_Stage7_SLO: IndIdx_RMW_Stage7_SLO(); break;
				case Uop.IndIdx_RMW_Stage7_SRE: IndIdx_RMW_Stage7_SRE(); break;
				case Uop.IndIdx_RMW_Stage7_RRA: IndIdx_RMW_Stage7_RRA(); break;
				case Uop.IndIdx_RMW_Stage7_ISC: IndIdx_RMW_Stage7_ISC(); break;
				case Uop.IndIdx_RMW_Stage7_DCP: IndIdx_RMW_Stage7_DCP(); break;
				case Uop.IndIdx_RMW_Stage7_RLA: IndIdx_RMW_Stage7_RLA(); break;
				case Uop.IndIdx_RMW_Stage8: IndIdx_RMW_Stage8(); break;
				case Uop.RelBranch_Stage2_BVS: RelBranch_Stage2_BVS(); break;
				case Uop.RelBranch_Stage2_BVC: RelBranch_Stage2_BVC(); break;
				case Uop.RelBranch_Stage2_BMI: RelBranch_Stage2_BMI(); break;
				case Uop.RelBranch_Stage2_BPL: RelBranch_Stage2_BPL(); break;
				case Uop.RelBranch_Stage2_BCS: RelBranch_Stage2_BCS(); break;
				case Uop.RelBranch_Stage2_BCC: RelBranch_Stage2_BCC(); break;
				case Uop.RelBranch_Stage2_BEQ: RelBranch_Stage2_BEQ(); break;
				case Uop.RelBranch_Stage2_BNE: RelBranch_Stage2_BNE(); break;
				case Uop.RelBranch_Stage2: RelBranch_Stage2(); break;
				case Uop.RelBranch_Stage3: RelBranch_Stage3(); break;
				case Uop.RelBranch_Stage4: RelBranch_Stage4(); break;
				case Uop.NOP: NOP(); break;
				case Uop.DecS: DecS(); break;
				case Uop.IncS: IncS(); break;
				case Uop.JSR: JSR(); break;
				case Uop.PullP: PullP(); break;
				case Uop.PullPCL: PullPCL(); break;
				case Uop.PullPCH_NoInc: PullPCH_NoInc(); break;
				case Uop.Abs_READ_LDA: Abs_READ_LDA(); break;
				case Uop.Abs_READ_LDY: Abs_READ_LDY(); break;
				case Uop.Abs_READ_LDX: Abs_READ_LDX(); break;
				case Uop.Abs_READ_BIT: Abs_READ_BIT(); break;
				case Uop.Abs_READ_LAX: Abs_READ_LAX(); break;
				case Uop.Abs_READ_AND: Abs_READ_AND(); break;
				case Uop.Abs_READ_EOR: Abs_READ_EOR(); break;
				case Uop.Abs_READ_ORA: Abs_READ_ORA(); break;
				case Uop.Abs_READ_ADC: Abs_READ_ADC(); break;
				case Uop.Abs_READ_CMP: Abs_READ_CMP(); break;
				case Uop.Abs_READ_CPY: Abs_READ_CPY(); break;
				case Uop.Abs_READ_NOP: Abs_READ_NOP(); break;
				case Uop.Abs_READ_CPX: Abs_READ_CPX(); break;
				case Uop.Abs_READ_SBC: Abs_READ_SBC(); break;
				case Uop.ZpIdx_Stage3_X: ZpIdx_Stage3_X(); break;
				case Uop.ZpIdx_Stage3_Y: ZpIdx_Stage3_Y(); break;
				case Uop.ZpIdx_RMW_Stage4: ZpIdx_RMW_Stage4(); break;
				case Uop.ZpIdx_RMW_Stage6: ZpIdx_RMW_Stage6(); break;
				case Uop.ZP_READ_EOR: ZP_READ_EOR(); break;
				case Uop.ZP_READ_BIT: ZP_READ_BIT(); break;
				case Uop.ZP_READ_LDA: ZP_READ_LDA(); break;
				case Uop.ZP_READ_LDY: ZP_READ_LDY(); break;
				case Uop.ZP_READ_LDX: ZP_READ_LDX(); break;
				case Uop.ZP_READ_LAX: ZP_READ_LAX(); break;
				case Uop.ZP_READ_CPY: ZP_READ_CPY(); break;
				case Uop.ZP_READ_CMP: ZP_READ_CMP(); break;
				case Uop.ZP_READ_CPX: ZP_READ_CPX(); break;
				case Uop.ZP_READ_ORA: ZP_READ_ORA(); break;
				case Uop.ZP_READ_NOP: ZP_READ_NOP(); break;
				case Uop.ZP_READ_SBC: ZP_READ_SBC(); break;
				case Uop.ZP_READ_ADC: ZP_READ_ADC(); break;
				case Uop.ZP_READ_AND: ZP_READ_AND(); break;
				case Uop._Cpx: _Cpx(); break;
				case Uop._Cpy: _Cpy(); break;
				case Uop._Cmp: _Cmp(); break;
				case Uop._Eor: _Eor(); break;
				case Uop._And: _And(); break;
				case Uop._Ora: _Ora(); break;
				case Uop._Anc: _Anc(); break;
				case Uop._Asr: _Asr(); break;
				case Uop._Axs: _Axs(); break;
				case Uop._Arr: _Arr(); break;
				case Uop._Lxa: _Lxa(); break;
				case Uop._Sbc: _Sbc(); break;
				case Uop._Adc: _Adc(); break;
				case Uop.Unsupported: Unsupported(); break;
				case Uop.Imm_EOR: Imm_EOR(); break;
				case Uop.Imm_ANC: Imm_ANC(); break;
				case Uop.Imm_ASR: Imm_ASR(); break;
				case Uop.Imm_AXS: Imm_AXS(); break;
				case Uop.Imm_ARR: Imm_ARR(); break;
				case Uop.Imm_LXA: Imm_LXA(); break;
				case Uop.Imm_ORA: Imm_ORA(); break;
				case Uop.Imm_CPY: Imm_CPY(); break;
				case Uop.Imm_CPX: Imm_CPX(); break;
				case Uop.Imm_CMP: Imm_CMP(); break;
				case Uop.Imm_SBC: Imm_SBC(); break;
				case Uop.Imm_AND: Imm_AND(); break;
				case Uop.Imm_ADC: Imm_ADC(); break;
				case Uop.Imm_LDA: Imm_LDA(); break;
				case Uop.Imm_LDX: Imm_LDX(); break;
				case Uop.Imm_LDY: Imm_LDY(); break;
				case Uop.Imm_Unsupported: Imm_Unsupported(); break;
				case Uop.IdxInd_Stage3: IdxInd_Stage3(); break;
				case Uop.IdxInd_Stage4: IdxInd_Stage4(); break;
				case Uop.IdxInd_Stage5: IdxInd_Stage5(); break;
				case Uop.IdxInd_Stage6_READ_LDA: IdxInd_Stage6_READ_LDA(); break;
				case Uop.IdxInd_Stage6_READ_ORA: IdxInd_Stage6_READ_ORA(); break;
				case Uop.IdxInd_Stage6_READ_LAX: IdxInd_Stage6_READ_LAX(); break;
				case Uop.IdxInd_Stage6_READ_CMP: IdxInd_Stage6_READ_CMP(); break;
				case Uop.IdxInd_Stage6_READ_ADC: IdxInd_Stage6_READ_ADC(); break;
				case Uop.IdxInd_Stage6_READ_AND: IdxInd_Stage6_READ_AND(); break;
				case Uop.IdxInd_Stage6_READ_EOR: IdxInd_Stage6_READ_EOR(); break;
				case Uop.IdxInd_Stage6_READ_SBC: IdxInd_Stage6_READ_SBC(); break;
				case Uop.IdxInd_Stage6_WRITE_STA: IdxInd_Stage6_WRITE_STA(); break;
				case Uop.IdxInd_Stage6_WRITE_SAX: IdxInd_Stage6_WRITE_SAX(); break;
				case Uop.IdxInd_Stage6_RMW: IdxInd_Stage6_RMW(); break;
				case Uop.IdxInd_Stage7_RMW_SLO: IdxInd_Stage7_RMW_SLO(); break;
				case Uop.IdxInd_Stage7_RMW_ISC: IdxInd_Stage7_RMW_ISC(); break;
				case Uop.IdxInd_Stage7_RMW_DCP: IdxInd_Stage7_RMW_DCP(); break;
				case Uop.IdxInd_Stage7_RMW_SRE: IdxInd_Stage7_RMW_SRE(); break;
				case Uop.IdxInd_Stage7_RMW_RRA: IdxInd_Stage7_RMW_RRA(); break;
				case Uop.IdxInd_Stage7_RMW_RLA: IdxInd_Stage7_RMW_RLA(); break;
				case Uop.IdxInd_Stage8_RMW: IdxInd_Stage8_RMW(); break;
				case Uop.PushP: PushP(); break;
				case Uop.PushA: PushA(); break;
				case Uop.PullA_NoInc: PullA_NoInc(); break;
				case Uop.PullP_NoInc: PullP_NoInc(); break;
				case Uop.Imp_ASL_A: Imp_ASL_A(); break;
				case Uop.Imp_ROL_A: Imp_ROL_A(); break;
				case Uop.Imp_ROR_A: Imp_ROR_A(); break;
				case Uop.Imp_LSR_A: Imp_LSR_A(); break;
				case Uop.JMP_abs: JMP_abs(); break;
				case Uop.IncPC: IncPC(); break;
				case Uop.ZP_RMW_Stage3: ZP_RMW_Stage3(); break;
				case Uop.ZP_RMW_Stage5: ZP_RMW_Stage5(); break;
				case Uop.ZP_RMW_INC: ZP_RMW_INC(); break;
				case Uop.ZP_RMW_DEC: ZP_RMW_DEC(); break;
				case Uop.ZP_RMW_ASL: ZP_RMW_ASL(); break;
				case Uop.ZP_RMW_SRE: ZP_RMW_SRE(); break;
				case Uop.ZP_RMW_RRA: ZP_RMW_RRA(); break;
				case Uop.ZP_RMW_DCP: ZP_RMW_DCP(); break;
				case Uop.ZP_RMW_LSR: ZP_RMW_LSR(); break;
				case Uop.ZP_RMW_ROR: ZP_RMW_ROR(); break;
				case Uop.ZP_RMW_ROL: ZP_RMW_ROL(); break;
				case Uop.ZP_RMW_SLO: ZP_RMW_SLO(); break;
				case Uop.ZP_RMW_ISC: ZP_RMW_ISC(); break;
				case Uop.ZP_RMW_RLA: ZP_RMW_RLA(); break;
				case Uop.AbsIdx_Stage3_Y: AbsIdx_Stage3_Y(); break;
				case Uop.AbsIdx_Stage3_X: AbsIdx_Stage3_X(); break;
				case Uop.AbsIdx_READ_Stage4: AbsIdx_READ_Stage4(); break;
				case Uop.AbsIdx_Stage4: AbsIdx_Stage4(); break;
				case Uop.AbsIdx_WRITE_Stage5_STA: AbsIdx_WRITE_Stage5_STA(); break;
				case Uop.AbsIdx_WRITE_Stage5_SHY: AbsIdx_WRITE_Stage5_SHY(); break;
				case Uop.AbsIdx_WRITE_Stage5_SHX: AbsIdx_WRITE_Stage5_SHX(); break;
				case Uop.AbsIdx_WRITE_Stage5_ERROR: AbsIdx_WRITE_Stage5_ERROR(); break;
				case Uop.AbsIdx_RMW_Stage5: AbsIdx_RMW_Stage5(); break;
				case Uop.AbsIdx_RMW_Stage7: AbsIdx_RMW_Stage7(); break;
				case Uop.AbsIdx_RMW_Stage6_DEC: AbsIdx_RMW_Stage6_DEC(); break;
				case Uop.AbsIdx_RMW_Stage6_DCP: AbsIdx_RMW_Stage6_DCP(); break;
				case Uop.AbsIdx_RMW_Stage6_ISC: AbsIdx_RMW_Stage6_ISC(); break;
				case Uop.AbsIdx_RMW_Stage6_INC: AbsIdx_RMW_Stage6_INC(); break;
				case Uop.AbsIdx_RMW_Stage6_ROL: AbsIdx_RMW_Stage6_ROL(); break;
				case Uop.AbsIdx_RMW_Stage6_LSR: AbsIdx_RMW_Stage6_LSR(); break;
				case Uop.AbsIdx_RMW_Stage6_SLO: AbsIdx_RMW_Stage6_SLO(); break;
				case Uop.AbsIdx_RMW_Stage6_SRE: AbsIdx_RMW_Stage6_SRE(); break;
				case Uop.AbsIdx_RMW_Stage6_RRA: AbsIdx_RMW_Stage6_RRA(); break;
				case Uop.AbsIdx_RMW_Stage6_RLA: AbsIdx_RMW_Stage6_RLA(); break;
				case Uop.AbsIdx_RMW_Stage6_ASL: AbsIdx_RMW_Stage6_ASL(); break;
				case Uop.AbsIdx_RMW_Stage6_ROR: AbsIdx_RMW_Stage6_ROR(); break;
				case Uop.AbsIdx_READ_Stage5_LDA: AbsIdx_READ_Stage5_LDA(); break;
				case Uop.AbsIdx_READ_Stage5_LDX: AbsIdx_READ_Stage5_LDX(); break;
				case Uop.AbsIdx_READ_Stage5_LAX: AbsIdx_READ_Stage5_LAX(); break;
				case Uop.AbsIdx_READ_Stage5_LDY: AbsIdx_READ_Stage5_LDY(); break;
				case Uop.AbsIdx_READ_Stage5_ORA: AbsIdx_READ_Stage5_ORA(); break;
				case Uop.AbsIdx_READ_Stage5_NOP: AbsIdx_READ_Stage5_NOP(); break;
				case Uop.AbsIdx_READ_Stage5_CMP: AbsIdx_READ_Stage5_CMP(); break;
				case Uop.AbsIdx_READ_Stage5_SBC: AbsIdx_READ_Stage5_SBC(); break;
				case Uop.AbsIdx_READ_Stage5_ADC: AbsIdx_READ_Stage5_ADC(); break;
				case Uop.AbsIdx_READ_Stage5_EOR: AbsIdx_READ_Stage5_EOR(); break;
				case Uop.AbsIdx_READ_Stage5_AND: AbsIdx_READ_Stage5_AND(); break;
				case Uop.AbsIdx_READ_Stage5_ERROR: AbsIdx_READ_Stage5_ERROR(); break;
				case Uop.AbsInd_JMP_Stage4: AbsInd_JMP_Stage4(); break;
				case Uop.AbsInd_JMP_Stage5: AbsInd_JMP_Stage5(); break;
				case Uop.Abs_RMW_Stage4: Abs_RMW_Stage4(); break;
				case Uop.Abs_RMW_Stage5_INC: Abs_RMW_Stage5_INC(); break;
				case Uop.Abs_RMW_Stage5_DEC: Abs_RMW_Stage5_DEC(); break;
				case Uop.Abs_RMW_Stage5_DCP: Abs_RMW_Stage5_DCP(); break;
				case Uop.Abs_RMW_Stage5_ISC: Abs_RMW_Stage5_ISC(); break;
				case Uop.Abs_RMW_Stage5_ASL: Abs_RMW_Stage5_ASL(); break;
				case Uop.Abs_RMW_Stage5_ROR: Abs_RMW_Stage5_ROR(); break;
				case Uop.Abs_RMW_Stage5_SLO: Abs_RMW_Stage5_SLO(); break;
				case Uop.Abs_RMW_Stage5_RLA: Abs_RMW_Stage5_RLA(); break;
				case Uop.Abs_RMW_Stage5_SRE: Abs_RMW_Stage5_SRE(); break;
				case Uop.Abs_RMW_Stage5_RRA: Abs_RMW_Stage5_RRA(); break;
				case Uop.Abs_RMW_Stage5_ROL: Abs_RMW_Stage5_ROL(); break;
				case Uop.Abs_RMW_Stage5_LSR: Abs_RMW_Stage5_LSR(); break;
				case Uop.Abs_RMW_Stage6: Abs_RMW_Stage6(); break;
				case Uop.End_ISpecial: End_ISpecial(); break;
				case Uop.End_SuppressInterrupt: End_SuppressInterrupt(); break;
				case Uop.End: End(); break;
				case Uop.Jam: Jam(); break;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ExecuteOne()
		{
			// total cycles now increments every time a cycle is called to accurately count during RDY
			TotalExecutedCycles++;
			interrupt_pending |= Interrupted;
			rdy_freeze = false;

			//i tried making ExecuteOneRetry not re-entrant by having it set a flag instead, then exit from the call below, check the flag, and GOTO if it was flagged, but it wasnt faster
			ExecuteOneRetry();

			if (!rdy_freeze)
				mi++;
		}

		public bool AtInstructionStart()
		{
			return Microcode[opcode][mi] >= Uop.End;
		}
	}
}
