//http://nesdev.parodius.com/6502_cpu.txt

#include <stdint.h>

#include "UopTable.cpp"

typedef int8_t sbyte;
typedef uint8_t byte;
typedef uint16_t ushort;

#include "TableNZ.h"

const ushort NMIVector = 0xFFFA;
const ushort ResetVector = 0xFFFC;
const ushort BRKVector = 0xFFFE;
const ushort IRQVector = 0xFFFE;

#ifdef __GNUC__
#define INL __attribute__((always_inline))
#else
#define INL
#endif

template<int index> bool Bit(int b)
{
	return (b & (1 << index)) != 0;
}

template<int index> bool Bit(byte b)
{
	return (b & (1 << index)) != 0;
}

struct CPU
{
	int _anchor;
	int _land0;
	int _land1;
	int _land2;

	// interface
	void *_ReadMemory_Managed;
	void *_DummyReadMemory_Managed;
	void *_PeekMemory_Managed;
	void *_WriteMemory_Managed;
	void *_OnExecFetch_Managed; // this only calls when the first byte of an instruction is fetched.
	void *_TraceCallback_Managed; // TODO

	byte (*ReadMemory)(ushort addr);
	byte (*DummyReadMemory)(ushort addr);
	byte (*PeekMemory)(ushort addr);
	void (*WriteMemory)(ushort addr, byte val);
	void (*OnExecFetch)(ushort addr);

	// config
	bool BCD_Enabled;
	bool debug;

	// state
	byte A;
	byte X;
	byte Y;
	//byte P;
	/// <summary>Carry Flag</summary>
	bool FlagC;
	/// <summary>Zero Flag</summary>
	bool FlagZ;
	/// <summary>Interrupt Disable Flag</summary>
	bool FlagI;
	/// <summary>Decimal Mode Flag</summary>
	bool FlagD;
	/// <summary>Break Flag</summary>
	bool FlagB;
	/// <summary>T... Flag</summary>
	bool FlagT;
	/// <summary>Overflow Flag</summary>
	bool FlagV;
	/// <summary>Negative Flag</summary>
	bool FlagN;

	ushort PC;
	byte S;

	bool IRQ;
	bool NMI;
	bool RDY;

	int TotalExecutedCycles;

	//opcode bytes.. theoretically redundant with the temp variables? who knows.
	int opcode;
	byte opcode2, opcode3;

	int ea, alu_temp; //cpu internal temp variables
	int mi; //microcode index
	bool iflag_pending; //iflag must be stored after it is checked in some cases (CLI and SEI).
	bool rdy_freeze; //true if the CPU must be frozen

	//tracks whether an interrupt condition has popped up recently.
	//not sure if this is real or not but it helps with the branch_irq_hack
	bool interrupt_pending;
	bool branch_irq_hack; //see Uop.RelBranch_Stage3 for more details

	// transient state
	byte value8, temp8;
	ushort value16;
	bool branch_taken;
	bool my_iflag;
	bool booltemp;
	int tempint;
	int lo, hi;


	INL byte GetP()
	{
				byte ret = 0;
				if (FlagC) ret |= 1;
				if (FlagZ) ret |= 2;
				if (FlagI) ret |= 4;
				if (FlagD) ret |= 8;
				if (FlagB) ret |= 16;
				if (FlagT) ret |= 32;
				if (FlagV) ret |= 64;
				if (FlagN) ret |= 128;
				return ret;
	}


	INL void SetP(byte value)
	{
				FlagC = (value & 1) != 0;
				FlagZ = (value & 2) != 0;
				FlagI = (value & 4) != 0;
				FlagD = (value & 8) != 0;
				FlagB = (value & 16) != 0;
				FlagT = (value & 32) != 0;
				FlagV = (value & 64) != 0;
				FlagN = (value & 128) != 0;
	}

	INL void NZ_V(byte value)
	{
		FlagZ = value == 0;
		FlagN = (value & 0x80) != 0;
	}


	/*
	void InitOpcodeHandlers()
	{
	//delegates arent faster than the switch. pretty sure. dont use it.
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
	//  End,End_ISpecial,End_BranchSpecial,End_SuppressInterrupt,
	//};
	}
	*/
	static const int VOP_Fetch1 = 256;
	static const int VOP_RelativeStuff = 257;
	static const int VOP_RelativeStuff2 = 258;
	static const int VOP_RelativeStuff3 = 259;
	static const int VOP_NMI = 260;
	static const int VOP_IRQ = 261;
	static const int VOP_RESET = 262;
	static const int VOP_Fetch1_NoInterrupt = 263;
	static const int VOP_NUM = 264;

	/*
	bool Interrupted
	{
	get
	{
	return NMI || (IRQ && !FlagI);
	}
	}
	*/

	INL void FetchDummy()
	{
		DummyReadMemory(PC);
	}

	/*
	void Execute(int cycles)
	{
		for (int i = 0; i < cycles; i++)
		{
			ExecuteOne();
		}
	}*/

	void Fetch1()
	{
		my_iflag = FlagI;
		FlagI = iflag_pending;
		if (!branch_irq_hack)
		{
			interrupt_pending = false;
			if (NMI)
			{
				//if (TraceCallback != nullptr)
				//	TraceCallback("====NMI====");
				ea = NMIVector;
				opcode = VOP_NMI;
				NMI = false;
				mi = 0;
				ExecuteOneRetry();
				return;
			}
			else if (IRQ && !my_iflag)
			{
				//if (TraceCallback != nullptr)
				//	TraceCallback("====IRQ====");
				ea = IRQVector;
				opcode = VOP_IRQ;
				mi = 0;
				ExecuteOneRetry();
				return;
			}
		}
		Fetch1_Real();
	}

	INL void Fetch1_Real()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			//if (debug) Console.WriteLine(State());
			branch_irq_hack = false;
			if (OnExecFetch != nullptr) OnExecFetch(PC);
			//if (TraceCallback != nullptr)
			//	TraceCallback(State());
			opcode = ReadMemory(PC++);
			mi = -1;
		}
	}
	INL void Fetch2()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			opcode2 = ReadMemory(PC++);
		}
	}
	INL void Fetch3()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			opcode3 = ReadMemory(PC++);
		}
	}
	INL void PushPCH()
	{
		WriteMemory((ushort)(S-- + 0x100), (byte)(PC >> 8));
	}
	INL void PushPCL()
	{
		WriteMemory((ushort)(S-- + 0x100), (byte)PC);
	}
	INL void PushP_BRK()
	{
		FlagB = true;
		WriteMemory((ushort)(S-- + 0x100), GetP());
		FlagI = true;
		ea = BRKVector;

	}
	INL void PushP_IRQ()
	{
		FlagB = false;
		WriteMemory((ushort)(S-- + 0x100), GetP());
		FlagI = true;
		ea = IRQVector;

	}
	INL void PushP_NMI()
	{
		FlagB = false;
		WriteMemory((ushort)(S-- + 0x100), GetP());
		FlagI = true; //is this right?
		ea = NMIVector;

	}
	INL void PushP_Reset()
	{
		ea = ResetVector;
		S--;
		FlagI = true;

	}
	INL void PushDummy()
	{
		S--;

	}
	INL void FetchPCLVector()
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
			alu_temp = ReadMemory((ushort)ea);
		}

	}
	INL void FetchPCHVector()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp += ReadMemory((ushort)(ea + 1)) << 8;
			PC = (ushort)alu_temp;
		}

	}
	INL void Imp_INY()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			FetchDummy(); Y++; NZ_Y();
		}
	}
	INL void Imp_DEY()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			FetchDummy(); Y--; NZ_Y();
		}
	}
	INL void Imp_INX()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			FetchDummy(); X++; NZ_X();
		}
	}
	INL void Imp_DEX()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			FetchDummy(); X--; NZ_X();
		}
	}
	INL void NZ_A()
	{
		FlagZ = A == 0;
		FlagN = (A & 0x80) != 0;
	}
	INL void NZ_X()
	{
		FlagZ = X == 0;
		FlagN = (X & 0x80) != 0;
	}
	INL void NZ_Y()
	{
		FlagZ = Y == 0;
		FlagN = (Y & 0x80) != 0;
	}
	INL void Imp_TSX()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			FetchDummy(); X = S; NZ_X();
		}
	}
	INL void Imp_TXS()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			FetchDummy(); S = X;
		}
	}
	INL void Imp_TAX()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			FetchDummy(); X = A; NZ_X();
		}
	}
	INL void Imp_TAY()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			FetchDummy(); Y = A; NZ_Y();
		}
	}
	INL void Imp_TYA()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			FetchDummy(); A = Y; NZ_A();
		}
	}
	INL void Imp_TXA()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			FetchDummy(); A = X; NZ_A();
		}

	}
	INL void Imp_SEI()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			FetchDummy(); iflag_pending = true;
		}
	}
	INL void Imp_CLI()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			FetchDummy(); iflag_pending = false;
		}
	}
	INL void Imp_SEC()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			FetchDummy(); FlagC = true;
		}
	}
	INL void Imp_CLC()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			FetchDummy(); FlagC = false;
		}
	}
	INL void Imp_SED()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			FetchDummy(); FlagD = true;
		}
	}
	INL void Imp_CLD()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			FetchDummy(); FlagD = false;
		}
	}
	INL void Imp_CLV()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			FetchDummy(); FlagV = false;
		}

	}
	INL void Abs_WRITE_STA()
	{
		WriteMemory((ushort)((opcode3 << 8) + opcode2), A);
	}
	INL void Abs_WRITE_STX()
	{
		WriteMemory((ushort)((opcode3 << 8) + opcode2), X);
	}
	INL void Abs_WRITE_STY()
	{
		WriteMemory((ushort)((opcode3 << 8) + opcode2), Y);
	}
	INL void Abs_WRITE_SAX()
	{
		WriteMemory((ushort)((opcode3 << 8) + opcode2), (byte)(X & A));

	}
	INL void ZP_WRITE_STA()
	{
		WriteMemory(opcode2, A);
	}
	INL void ZP_WRITE_STY()
	{
		WriteMemory(opcode2, Y);
	}
	INL void ZP_WRITE_STX()
	{
		WriteMemory(opcode2, X);
	}
	INL void ZP_WRITE_SAX()
	{
		WriteMemory(opcode2, (byte)(X & A));

	}
	INL void IndIdx_Stage3()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			ea = ReadMemory(opcode2);
		}

	}
	INL void IndIdx_Stage4()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ea + Y;
			ea = (ReadMemory((byte)(opcode2 + 1)) << 8)
				| ((alu_temp & 0xFF));
		}

	}
	INL void IndIdx_WRITE_Stage5()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			ReadMemory((ushort)ea);
			ea += (alu_temp >> 8) << 8;
		}

	}
	void IndIdx_READ_Stage5()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			if (!Bit<8>(alu_temp))
			{
				mi++;
				ExecuteOneRetry();
				return;
			}
			else
			{
				ReadMemory((ushort)ea);
				ea = (ushort)(ea + 0x100);
			}
		}
	}
	INL void IndIdx_RMW_Stage5()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			if (Bit<8>(alu_temp))
				ea = (ushort)(ea + 0x100);
			ReadMemory((ushort)ea);
		}

	}
	INL void IndIdx_WRITE_Stage6_STA()
	{
		WriteMemory((ushort)ea, A);

	}
	INL void IndIdx_WRITE_Stage6_SHA()
	{
		WriteMemory((ushort)ea, (byte)(A & X & 7));

	}
	INL void IndIdx_READ_Stage6_LDA()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			A = ReadMemory((ushort)ea);
			NZ_A();
		}
	}
	INL void IndIdx_READ_Stage6_CMP()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)ea);
			_Cmp();
		}
	}
	INL void IndIdx_READ_Stage6_AND()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)ea);
			_And();
		}
	}
	INL void IndIdx_READ_Stage6_EOR()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)ea);
			_Eor();
		}
	}
	INL void IndIdx_READ_Stage6_LAX()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			A = X = ReadMemory((ushort)ea);
			NZ_A();
		}
	}
	INL void IndIdx_READ_Stage6_ADC()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)ea);
			_Adc();
		}
	}
	INL void IndIdx_READ_Stage6_SBC()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)ea);
			_Sbc();
		}
	}
	INL void IndIdx_READ_Stage6_ORA()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)ea);
			_Ora();
		}
	}
	INL void IndIdx_RMW_Stage6()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)ea);
		}

	}
	INL void IndIdx_RMW_Stage7_SLO()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = (byte)alu_temp;
		FlagC = (value8 & 0x80) != 0;
		alu_temp = value8 = (byte)((value8 << 1));
		A |= value8;
		NZ_A();
	}
	INL void IndIdx_RMW_Stage7_SRE()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = (byte)alu_temp;
		FlagC = (value8 & 1) != 0;
		alu_temp = value8 = (byte)(value8 >> 1);
		A ^= value8;
		NZ_A();
	}
	INL void IndIdx_RMW_Stage7_RRA()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = temp8 = (byte)alu_temp;
		alu_temp = value8 = (byte)((value8 >> 1) | ((FlagC) << 7));
		FlagC = (temp8 & 1) != 0;
		_Adc();
	}
	INL void IndIdx_RMW_Stage7_ISC()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = temp8 = (byte)alu_temp;
		alu_temp = value8 = (byte)(value8 + 1);
		_Sbc();
	}
	INL void IndIdx_RMW_Stage7_DCP()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = temp8 = (byte)alu_temp;
		alu_temp = value8 = (byte)(value8 - 1);
		FlagC = (temp8 & 1) != 0;
		_Cmp();
	}
	INL void IndIdx_RMW_Stage7_RLA()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = temp8 = (byte)alu_temp;
		alu_temp = value8 = (byte)((value8 << 1) | (FlagC));
		FlagC = (temp8 & 0x80) != 0;
		A &= value8;
		NZ_A();
	}
	INL void IndIdx_RMW_Stage8()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);


	}
	INL void RelBranch_Stage2_BVS()
	{
		branch_taken = FlagV == true;
		RelBranch_Stage2();
	}
	INL void RelBranch_Stage2_BVC()
	{
		branch_taken = FlagV == false;
		RelBranch_Stage2();
	}
	INL void RelBranch_Stage2_BMI()
	{
		branch_taken = FlagN == true;
		RelBranch_Stage2();
	}
	INL void RelBranch_Stage2_BPL()
	{
		branch_taken = FlagN == false;
		RelBranch_Stage2();
	}
	INL void RelBranch_Stage2_BCS()
	{
		branch_taken = FlagC == true;
		RelBranch_Stage2();
	}
	INL void RelBranch_Stage2_BCC()
	{
		branch_taken = FlagC == false;
		RelBranch_Stage2();
	}
	INL void RelBranch_Stage2_BEQ()
	{
		branch_taken = FlagZ == true;
		RelBranch_Stage2();
	}
	INL void RelBranch_Stage2_BNE()
	{
		branch_taken = FlagZ == false;
		RelBranch_Stage2();

	}
	INL void RelBranch_Stage2()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			opcode2 = ReadMemory(PC++);
			if (branch_taken)
			{
				branch_taken = false;
				//if the branch is taken, we enter a different bit of microcode to calculate the PC and complete the branch
				opcode = VOP_RelativeStuff;
				mi = -1;
			}
		}

	}
	INL void RelBranch_Stage3()
	{
		FetchDummy();
		alu_temp = (byte)PC + (int)(sbyte)opcode2;
		PC &= 0xFF00;
		PC |= (ushort)((alu_temp & 0xFF));
		if (Bit<8>(alu_temp))
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
	INL void RelBranch_Stage4()
	{
		FetchDummy();
		if (Bit<31>(alu_temp))
			PC = (ushort)(PC - 0x100);
		else PC = (ushort)(PC + 0x100);


	}
	INL void NOP()
	{
	}
	INL void DecS()
	{
		S--;
	}
	INL void IncS()
	{
		S++;
	}
	INL void JSR()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			PC = (ushort)((ReadMemory((ushort)(PC)) << 8) + opcode2);
		}
	}
	INL void PullP()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			SetP(ReadMemory((ushort)(S++ + 0x100)));
			FlagT = true; //force T always to remain true
		}

	}
	INL void PullPCL()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			PC &= 0xFF00;
			PC |= ReadMemory((ushort)(S++ + 0x100));
		}

	}
	INL void PullPCH_NoInc()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			PC &= 0xFF;
			PC |= (ushort)(ReadMemory((ushort)(S + 0x100)) << 8);
		}

	}
	INL void Abs_READ_LDA()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			A = ReadMemory((ushort)((opcode3 << 8) + opcode2));
			NZ_A();
		}
	}
	INL void Abs_READ_LDY()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			Y = ReadMemory((ushort)((opcode3 << 8) + opcode2));
			NZ_Y();
		}
	}
	INL void Abs_READ_LDX()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			X = ReadMemory((ushort)((opcode3 << 8) + opcode2));
			NZ_X();
		}
	}
	INL void Abs_READ_BIT()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
			_Bit();
		}
	}
	INL void Abs_READ_LAX()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
			A = ReadMemory((ushort)((opcode3 << 8) + opcode2));
			X = A;
			NZ_A();
		}
	}
	INL void Abs_READ_AND()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
			_And();
		}
	}
	INL void Abs_READ_EOR()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
			_Eor();
		}
	}
	INL void Abs_READ_ORA()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
			_Ora();
		}
	}
	INL void Abs_READ_ADC()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
			_Adc();
		}
	}
	INL void Abs_READ_CMP()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
			_Cmp();
		}
	}
	INL void Abs_READ_CPY()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
			_Cpy();
		}
	}
	INL void Abs_READ_NOP()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
		}

	}
	INL void Abs_READ_CPX()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
			_Cpx();
		}
	}
	INL void Abs_READ_SBC()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
			_Sbc();
		}

	}
	INL void ZpIdx_Stage3_X()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			ReadMemory(opcode2);
			opcode2 = (byte)(opcode2 + X); //a bit sneaky to shove this into opcode2... but we can reuse all the zero page uops if we do that
		}

	}
	INL void ZpIdx_Stage3_Y()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			ReadMemory(opcode2);
			opcode2 = (byte)(opcode2 + Y); //a bit sneaky to shove this into opcode2... but we can reuse all the zero page uops if we do that
		}

	}
	INL void ZpIdx_RMW_Stage4()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory(opcode2);
		}

	}
	INL void ZpIdx_RMW_Stage6()
	{
		WriteMemory(opcode2, (byte)alu_temp);


	}
	INL void ZP_READ_EOR()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory(opcode2);
			_Eor();
		}
	}
	INL void ZP_READ_BIT()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory(opcode2);
			_Bit();
		}
	}
	INL void ZP_READ_LDA()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			A = ReadMemory(opcode2);
			NZ_A();
		}
	}
	INL void ZP_READ_LDY()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			Y = ReadMemory(opcode2);
			NZ_Y();
		}
	}
	INL void ZP_READ_LDX()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			X = ReadMemory(opcode2);
			NZ_X();
		}
	}
	INL void ZP_READ_LAX()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			//?? is this right??
			X = ReadMemory(opcode2);
			A = X;
			NZ_A();
		}
	}
	INL void ZP_READ_CPY()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory(opcode2);
			_Cpy();
		}
	}
	INL void ZP_READ_CMP()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory(opcode2);
			_Cmp();
		}
	}
	INL void ZP_READ_CPX()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory(opcode2);
			_Cpx();
		}
	}
	INL void ZP_READ_ORA()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory(opcode2);
			_Ora();
		}
	}
	INL void ZP_READ_NOP()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			ReadMemory(opcode2); //just a dummy
		}

	}
	INL void ZP_READ_SBC()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory(opcode2);
			_Sbc();
		}
	}
	INL void ZP_READ_ADC()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory(opcode2);
			_Adc();
		}
	}
	INL void ZP_READ_AND()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory(opcode2);
			_And();
		}

	}
	INL void _Cpx()
	{
		value8 = (byte)alu_temp;
		value16 = (ushort)(X - value8);
		FlagC = (X >= value8);
		NZ_V((byte)value16);

	}
	INL void _Cpy()
	{
		value8 = (byte)alu_temp;
		value16 = (ushort)(Y - value8);
		FlagC = (Y >= value8);
		NZ_V((byte)value16);

	}
	INL void _Cmp()
	{
		value8 = (byte)alu_temp;
		value16 = (ushort)(A - value8);
		FlagC = (A >= value8);
		NZ_V((byte)value16);

	}
	INL void _Bit()
	{
		FlagN = (alu_temp & 0x80) != 0;
		FlagV = (alu_temp & 0x40) != 0;
		FlagZ = (A & alu_temp) == 0;

	}
	INL void _Eor()
	{
		A ^= (byte)alu_temp;
		NZ_A();
	}
	INL void _And()
	{
		A &= (byte)alu_temp;
		NZ_A();
	}
	INL void _Ora()
	{
		A |= (byte)alu_temp;
		NZ_A();
	}
	INL void _Anc()
	{
		A &= (byte)alu_temp;
		FlagC = Bit<7>(A);
		NZ_A();
	}
	INL void _Asr()
	{
		A &= (byte)alu_temp;
		FlagC = Bit<0>(A);
		A >>= 1;
		NZ_A();
	}
	INL void _Axs()
	{
		X &= A;
		alu_temp = X - (byte)alu_temp;
		X = (byte)alu_temp;
		FlagC = !Bit<8>(alu_temp);
		NZ_X();
	}
	INL void _Arr()
	{
		{
			A &= (byte)alu_temp;
			booltemp = Bit<0>(A);
			A = (byte)((A >> 1) | (FlagC ? 0x80 : 0x00));
			FlagC = booltemp;
			if (Bit<5>(A))
				if (Bit<6>(A))
				{ FlagC = true; FlagV = false; }
				else { FlagV = true; FlagC = false; }
			else if (Bit<6>(A))
			{ FlagV = true; FlagC = true; }
			else { FlagV = false; FlagC = false; }
			FlagZ = (A == 0);

		}
	}
	INL void _Lxa()
	{
		A |= 0xFF; //there is some debate about what this should be. it may depend on the 6502 variant. this is suggested by qeed's doc for the nes and passes blargg's instruction test
		A &= (byte)alu_temp;
		X = A;
		NZ_A();
	}
	INL void _Sbc()
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
	INL void _Adc()
	{
		{
			//TODO - an extra cycle penalty?
			value8 = (byte)alu_temp;
			if (FlagD && BCD_Enabled)
			{
				lo = (A & 0x0F) + (value8 & 0x0F) + (FlagC ? 1 : 0);
				hi = (A & 0xF0) + (value8 & 0xF0);
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
				tempint = value8 + A + (FlagC ? 1 : 0);
				FlagV = (~(A ^ value8) & (A ^ tempint) & 0x80) != 0;
				FlagC = tempint > 0xFF;
				A = (byte)tempint;
			}
			NZ_A();
		}

	}
	INL void Unsupported()
	{


	}
	INL void Imm_EOR()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory(PC++);
			_Eor();
		}
	}
	INL void Imm_ANC()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory(PC++);
			_Anc();
		}
	}
	INL void Imm_ASR()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory(PC++);
			_Asr();
		}
	}
	INL void Imm_AXS()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory(PC++);
			_Axs();
		}
	}
	INL void Imm_ARR()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory(PC++);
			_Arr();
		}
	}
	INL void Imm_LXA()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory(PC++);
			_Lxa();
		}
	}
	INL void Imm_ORA()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory(PC++);
			_Ora();
		}
	}
	INL void Imm_CPY()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory(PC++);
			_Cpy();
		}
	}
	INL void Imm_CPX()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory(PC++);
			_Cpx();
		}
	}
	INL void Imm_CMP()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory(PC++);
			_Cmp();
		}
	}
	INL void Imm_SBC()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory(PC++);
			_Sbc();
		}
	}
	INL void Imm_AND()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory(PC++);
			_And();
		}
	}
	INL void Imm_ADC()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory(PC++);
			_Adc();
		}
	}
	INL void Imm_LDA()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			A = ReadMemory(PC++);
			NZ_A();
		}
	}
	INL void Imm_LDX()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			X = ReadMemory(PC++);
			NZ_X();
		}
	}
	INL void Imm_LDY()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			Y = ReadMemory(PC++);
			NZ_Y();
		}
	}
	INL void Imm_Unsupported()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			ReadMemory(PC++);
		}

	}
	INL void IdxInd_Stage3()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			ReadMemory(opcode2); //dummy?
			alu_temp = (opcode2 + X) & 0xFF;
		}

	}
	INL void IdxInd_Stage4()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			ea = ReadMemory((ushort)alu_temp);
		}

	}
	INL void IdxInd_Stage5()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			ea += (ReadMemory((byte)(alu_temp + 1)) << 8);
		}

	}
	INL void IdxInd_Stage6_READ_LDA()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			//TODO make uniform with others
			A = ReadMemory((ushort)ea);
			NZ_A();
		}
	}
	INL void IdxInd_Stage6_READ_ORA()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)ea);
			_Ora();
		}
	}
	INL void IdxInd_Stage6_READ_LAX()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			A = X = ReadMemory((ushort)ea);
			NZ_A();
		}
	}
	INL void IdxInd_Stage6_READ_CMP()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)ea);
			_Cmp();
		}
	}
	INL void IdxInd_Stage6_READ_ADC()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)ea);
			_Adc();
		}
	}
	INL void IdxInd_Stage6_READ_AND()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)ea);
			_And();
		}
	}
	INL void IdxInd_Stage6_READ_EOR()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)ea);
			_Eor();
		}
	}
	INL void IdxInd_Stage6_READ_SBC()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)ea);
			_Sbc();
		}
	}
	INL void IdxInd_Stage6_WRITE_STA()
	{
		WriteMemory((ushort)ea, A);

	}
	INL void IdxInd_Stage6_WRITE_SAX()
	{
		alu_temp = A & X;
		WriteMemory((ushort)ea, (byte)alu_temp);
		//flag writing skipped on purpose

	}
	INL void IdxInd_Stage6_RMW()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)ea);
		}

	}
	INL void IdxInd_Stage7_RMW_SLO()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = (byte)alu_temp;
		FlagC = (value8 & 0x80) != 0;
		alu_temp = value8 = (byte)((value8 << 1));
		A |= value8;
		NZ_A();
	}
	INL void IdxInd_Stage7_RMW_ISC()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = (byte)alu_temp;
		alu_temp = value8 = (byte)(value8 + 1);
		_Sbc();
	}
	INL void IdxInd_Stage7_RMW_DCP()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = temp8 = (byte)alu_temp;
		alu_temp = value8 = (byte)(value8 - 1);
		FlagC = (temp8 & 1) != 0;
		_Cmp();
	}
	INL void IdxInd_Stage7_RMW_SRE()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = (byte)alu_temp;
		FlagC = (value8 & 1) != 0;
		alu_temp = value8 = (byte)(value8 >> 1);
		A ^= value8;
		NZ_A();
	}
	INL void IdxInd_Stage7_RMW_RRA()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = (byte)alu_temp;
		value8 = temp8 = (byte)alu_temp;
		alu_temp = value8 = (byte)((value8 >> 1) | ((FlagC) << 7));
		FlagC = (temp8 & 1) != 0;
		_Adc();
	}
	INL void IdxInd_Stage7_RMW_RLA()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = temp8 = (byte)alu_temp;
		alu_temp = value8 = (byte)((value8 << 1) | (FlagC));
		FlagC = (temp8 & 0x80) != 0;
		A &= value8;
		NZ_A();
	}
	INL void IdxInd_Stage8_RMW()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);


	}
	INL void PushP()
	{
		FlagB = true;
		WriteMemory((ushort)(S-- + 0x100), GetP());

	}
	INL void PushA()
	{
		WriteMemory((ushort)(S-- + 0x100), A);
	}
	INL void PullA_NoInc()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			A = ReadMemory((ushort)(S + 0x100));
			NZ_A();
		}
	}
	INL void PullP_NoInc()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			my_iflag = FlagI;
			SetP(ReadMemory((ushort)(S + 0x100)));
			iflag_pending = FlagI;
			FlagI = my_iflag;
			FlagT = true; //force T always to remain true

		}

	}
	INL void Imp_ASL_A()
	{
		FetchDummy();
		FlagC = (A & 0x80) != 0;
		A = (byte)(A << 1);
		NZ_A();
	}
	INL void Imp_ROL_A()
	{
		FetchDummy();
		temp8 = A;
		A = (byte)((A << 1) | (FlagC));
		FlagC = (temp8 & 0x80) != 0;
		NZ_A();
	}
	INL void Imp_ROR_A()
	{
		FetchDummy();
		temp8 = A;
		A = (byte)((A >> 1) | ((FlagC) << 7));
		FlagC = (temp8 & 1) != 0;
		NZ_A();
	}
	INL void Imp_LSR_A()
	{
		FetchDummy();
		FlagC = (A & 1) != 0;
		A = (byte)(A >> 1);
		NZ_A();

	}
	INL void JMP_abs()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			PC = (ushort)((ReadMemory(PC) << 8) + opcode2);
		}

	}
	INL void IncPC()
	{
		PC++;


	}
	INL void ZP_RMW_Stage3()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory(opcode2);
		}

	}
	INL void ZP_RMW_Stage5()
	{
		WriteMemory(opcode2, (byte)alu_temp);

	}
	INL void ZP_RMW_INC()
	{
		WriteMemory(opcode2, (byte)alu_temp);
		alu_temp = (byte)((alu_temp + 1) & 0xFF);
		NZ_V((byte)alu_temp);

	}
	INL void ZP_RMW_DEC()
	{
		WriteMemory(opcode2, (byte)alu_temp);
		alu_temp = (byte)((alu_temp - 1) & 0xFF);
		NZ_V((byte)alu_temp);

	}
	INL void ZP_RMW_ASL()
	{
		WriteMemory(opcode2, (byte)alu_temp);
		value8 = (byte)alu_temp;
		FlagC = (value8 & 0x80) != 0;
		alu_temp = value8 = (byte)(value8 << 1);
		NZ_V((byte)value8);

	}
	INL void ZP_RMW_SRE()
	{
		WriteMemory(opcode2, (byte)alu_temp);
		value8 = (byte)alu_temp;
		FlagC = (value8 & 1) != 0;
		alu_temp = value8 = (byte)(value8 >> 1);
		A ^= value8;
		NZ_A();
	}
	INL void ZP_RMW_RRA()
	{
		WriteMemory(opcode2, (byte)alu_temp);
		value8 = temp8 = (byte)alu_temp;
		alu_temp = value8 = (byte)((value8 >> 1) | ((FlagC) << 7));
		FlagC = (temp8 & 1) != 0;
		_Adc();
	}
	INL void ZP_RMW_DCP()
	{
		WriteMemory(opcode2, (byte)alu_temp);
		value8 = temp8 = (byte)alu_temp;
		alu_temp = value8 = (byte)(value8 - 1);
		FlagC = (temp8 & 1) != 0;
		_Cmp();
	}
	INL void ZP_RMW_LSR()
	{
		WriteMemory(opcode2, (byte)alu_temp);
		value8 = (byte)alu_temp;
		FlagC = (value8 & 1) != 0;
		alu_temp = value8 = (byte)(value8 >> 1);
		NZ_V((byte)value8);

	}
	INL void ZP_RMW_ROR()
	{
		WriteMemory(opcode2, (byte)alu_temp);
		value8 = temp8 = (byte)alu_temp;
		alu_temp = value8 = (byte)((value8 >> 1) | ((FlagC) << 7));
		FlagC = (temp8 & 1) != 0;
		NZ_V((byte)value8);

	}
	INL void ZP_RMW_ROL()
	{
		WriteMemory(opcode2, (byte)alu_temp);
		value8 = temp8 = (byte)alu_temp;
		alu_temp = value8 = (byte)((value8 << 1) | (FlagC));
		FlagC = (temp8 & 0x80) != 0;
		NZ_V((byte)value8);

	}
	INL void ZP_RMW_SLO()
	{
		WriteMemory(opcode2, (byte)alu_temp);
		value8 = (byte)alu_temp;
		FlagC = (value8 & 0x80) != 0;
		alu_temp = value8 = (byte)((value8 << 1));
		A |= value8;
		NZ_A();
	}
	INL void ZP_RMW_ISC()
	{
		WriteMemory(opcode2, (byte)alu_temp);
		value8 = (byte)alu_temp;
		alu_temp = value8 = (byte)(value8 + 1);
		_Sbc();
	}
	INL void ZP_RMW_RLA()
	{
		WriteMemory(opcode2, (byte)alu_temp);
		value8 = temp8 = (byte)alu_temp;
		alu_temp = value8 = (byte)((value8 << 1) | (FlagC));
		FlagC = (temp8 & 0x80) != 0;
		A &= value8;
		NZ_A();

	}
	INL void AbsIdx_Stage3_Y()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			opcode3 = ReadMemory(PC++);
			alu_temp = opcode2 + Y;
			ea = (opcode3 << 8) + (alu_temp & 0xFF);

			//new Uop[] { Uop.Fetch2, Uop.AbsIdx_Stage3_Y, Uop.AbsIdx_Stage4, Uop.AbsIdx_WRITE_Stage5_STA, Uop.End },
		}
	}
	INL void AbsIdx_Stage3_X()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			opcode3 = ReadMemory(PC++);
			alu_temp = opcode2 + X;
			ea = (opcode3 << 8) + (alu_temp & 0xFF);
		}

	}
	void AbsIdx_READ_Stage4()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			if (!Bit<8>(alu_temp))
			{
				mi++;
				ExecuteOneRetry();
				return;
			}
			else
			{
				alu_temp = ReadMemory((ushort)ea);
				ea = (ushort)(ea + 0x100);
			}
		}

	}
	INL void AbsIdx_Stage4()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			//bleh.. redundant code to make sure we dont clobber alu_temp before using it to decide whether to change ea
			if (Bit<8>(alu_temp))
			{
				alu_temp = ReadMemory((ushort)ea);
				ea = (ushort)(ea + 0x100);
			}
			else alu_temp = ReadMemory((ushort)ea);
		}

	}
	INL void AbsIdx_WRITE_Stage5_STA()
	{
		WriteMemory((ushort)ea, A);

	}
	INL void AbsIdx_WRITE_Stage5_SHY()
	{
		alu_temp = Y & (ea >> 8);
		ea = (ea & 0xFF) | (alu_temp << 8); //"(the bank where the value is stored may be equal to the value stored)" -- more like IS.
		WriteMemory((ushort)ea, (byte)alu_temp);

	}
	INL void AbsIdx_WRITE_Stage5_SHX()
	{
		alu_temp = X & (ea >> 8);
		ea = (ea & 0xFF) | (alu_temp << 8); //"(the bank where the value is stored may be equal to the value stored)" -- more like IS.
		WriteMemory((ushort)ea, (byte)alu_temp);

	}
	INL void AbsIdx_WRITE_Stage5_ERROR()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)ea);
			//throw new InvalidOperationException("UNSUPPORTED OPCODE [probably SHS] PLEASE REPORT");
		}

	}
	INL void AbsIdx_RMW_Stage5()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)ea);
		}

	}
	INL void AbsIdx_RMW_Stage7()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);

	}
	INL void AbsIdx_RMW_Stage6_DEC()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		alu_temp = value8 = (byte)(alu_temp - 1);
		NZ_V((byte)value8);

	}
	INL void AbsIdx_RMW_Stage6_DCP()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		alu_temp = value8 = (byte)(alu_temp - 1);
		_Cmp();
	}
	INL void AbsIdx_RMW_Stage6_ISC()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		alu_temp = value8 = (byte)(alu_temp + 1);
		_Sbc();
	}
	INL void AbsIdx_RMW_Stage6_INC()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		alu_temp = value8 = (byte)(alu_temp + 1);
		NZ_V((byte)value8);

	}
	INL void AbsIdx_RMW_Stage6_ROL()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = temp8 = (byte)alu_temp;
		alu_temp = value8 = (byte)((value8 << 1) | (FlagC));
		FlagC = (temp8 & 0x80) != 0;
		NZ_V((byte)value8);

	}
	INL void AbsIdx_RMW_Stage6_LSR()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = (byte)alu_temp;
		FlagC = (value8 & 1) != 0;
		alu_temp = value8 = (byte)(value8 >> 1);
		NZ_V((byte)value8);

	}
	INL void AbsIdx_RMW_Stage6_SLO()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = (byte)alu_temp;
		FlagC = (value8 & 0x80) != 0;
		alu_temp = value8 = (byte)(value8 << 1);
		A |= value8;
		NZ_A();
	}
	INL void AbsIdx_RMW_Stage6_SRE()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = (byte)alu_temp;
		FlagC = (value8 & 1) != 0;
		alu_temp = value8 = (byte)(value8 >> 1);
		A ^= value8;
		NZ_A();
	}
	INL void AbsIdx_RMW_Stage6_RRA()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = temp8 = (byte)alu_temp;
		alu_temp = value8 = (byte)((value8 >> 1) | ((FlagC) << 7));
		FlagC = (temp8 & 1) != 0;
		_Adc();
	}
	INL void AbsIdx_RMW_Stage6_RLA()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = temp8 = (byte)alu_temp;
		alu_temp = value8 = (byte)((value8 << 1) | (FlagC));
		FlagC = (temp8 & 0x80) != 0;
		A &= value8;
		NZ_A();
	}
	INL void AbsIdx_RMW_Stage6_ASL()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = (byte)alu_temp;
		FlagC = (value8 & 0x80) != 0;
		alu_temp = value8 = (byte)(value8 << 1);
		NZ_V((byte)value8);

	}
	INL void AbsIdx_RMW_Stage6_ROR()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = temp8 = (byte)alu_temp;
		alu_temp = value8 = (byte)((value8 >> 1) | ((FlagC) << 7));
		FlagC = (temp8 & 1) != 0;
		NZ_V((byte)value8);


	}
	INL void AbsIdx_READ_Stage5_LDA()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			A = ReadMemory((ushort)ea);
			NZ_A();
		}
	}
	INL void AbsIdx_READ_Stage5_LDX()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			X = ReadMemory((ushort)ea);
			NZ_X();
		}
	}
	INL void AbsIdx_READ_Stage5_LAX()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			A = ReadMemory((ushort)ea);
			X = A;
			NZ_A();
		}
	}
	INL void AbsIdx_READ_Stage5_LDY()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			Y = ReadMemory((ushort)ea);
			NZ_Y();
		}
	}
	INL void AbsIdx_READ_Stage5_ORA()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)ea);
			_Ora();
		}
	}
	INL void AbsIdx_READ_Stage5_NOP()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)ea);
		}

	}
	INL void AbsIdx_READ_Stage5_CMP()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)ea);
			_Cmp();
		}
	}
	INL void AbsIdx_READ_Stage5_SBC()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)ea);
			_Sbc();
		}
	}
	INL void AbsIdx_READ_Stage5_ADC()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)ea);
			_Adc();
		}
	}
	INL void AbsIdx_READ_Stage5_EOR()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)ea);
			_Eor();
		}
	}
	INL void AbsIdx_READ_Stage5_AND()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)ea);
			_And();
		}
	}
	INL void AbsIdx_READ_Stage5_ERROR()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			alu_temp = ReadMemory((ushort)ea);
			//throw new InvalidOperationException("UNSUPPORTED OPCODE [probably LAS] PLEASE REPORT");
		}

	}
	INL void AbsInd_JMP_Stage4()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			ea = (opcode3 << 8) + opcode2;
			alu_temp = ReadMemory((ushort)ea);
		}
	}
	INL void AbsInd_JMP_Stage5()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			ea = (opcode3 << 8) + (byte)(opcode2 + 1);
			alu_temp += ReadMemory((ushort)ea) << 8;
			PC = (ushort)alu_temp;
		}

	}
	INL void Abs_RMW_Stage4()
	{
		rdy_freeze = !RDY;
		if (RDY)
		{
			ea = (opcode3 << 8) + opcode2;
			alu_temp = ReadMemory((ushort)ea);
		}

	}
	INL void Abs_RMW_Stage5_INC()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = (byte)(alu_temp + 1);
		alu_temp = value8;
		NZ_V((byte)value8);

	}
	INL void Abs_RMW_Stage5_DEC()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = (byte)(alu_temp - 1);
		alu_temp = value8;
		NZ_V((byte)value8);

	}
	INL void Abs_RMW_Stage5_DCP()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = (byte)(alu_temp - 1);
		alu_temp = value8;
		_Cmp();
	}
	INL void Abs_RMW_Stage5_ISC()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = (byte)(alu_temp + 1);
		alu_temp = value8;
		_Sbc();
	}
	INL void Abs_RMW_Stage5_ASL()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = (byte)alu_temp;
		FlagC = (value8 & 0x80) != 0;
		alu_temp = value8 = (byte)(value8 << 1);
		NZ_V((byte)value8);

	}
	INL void Abs_RMW_Stage5_ROR()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = temp8 = (byte)alu_temp;
		alu_temp = value8 = (byte)((value8 >> 1) | ((FlagC) << 7));
		FlagC = (temp8 & 1) != 0;
		NZ_V((byte)value8);

	}
	INL void Abs_RMW_Stage5_SLO()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = (byte)alu_temp;
		FlagC = (value8 & 0x80) != 0;
		alu_temp = value8 = (byte)(value8 << 1);
		A |= value8;
		NZ_A();
	}
	INL void Abs_RMW_Stage5_RLA()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = temp8 = (byte)alu_temp;
		alu_temp = value8 = (byte)((value8 << 1) | (FlagC));
		FlagC = (temp8 & 0x80) != 0;
		A &= value8;
		NZ_A();
	}
	INL void Abs_RMW_Stage5_SRE()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = (byte)alu_temp;
		FlagC = (value8 & 1) != 0;
		alu_temp = value8 = (byte)(value8 >> 1);
		A ^= value8;
		NZ_A();
	}
	INL void Abs_RMW_Stage5_RRA()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = temp8 = (byte)alu_temp;
		alu_temp = value8 = (byte)((value8 >> 1) | ((FlagC) << 7));
		FlagC = (temp8 & 1) != 0;
		_Adc();
	}
	INL void Abs_RMW_Stage5_ROL()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = temp8 = (byte)alu_temp;
		alu_temp = value8 = (byte)((value8 << 1) | (FlagC));
		FlagC = (temp8 & 0x80) != 0;
		NZ_V((byte)value8);

	}
	INL void Abs_RMW_Stage5_LSR()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);
		value8 = (byte)alu_temp;
		FlagC = (value8 & 1) != 0;
		alu_temp = value8 = (byte)(value8 >> 1);
		NZ_V((byte)value8);


	}
	INL void Abs_RMW_Stage6()
	{
		WriteMemory((ushort)ea, (byte)alu_temp);


	}
	void End_ISpecial()
	{
		opcode = VOP_Fetch1;
		mi = 0;
		ExecuteOneRetry();
		return;

	}
	void End_SuppressInterrupt()
	{
		opcode = VOP_Fetch1_NoInterrupt;
		mi = 0;
		ExecuteOneRetry();
		return;

	}
	void End()
	{
		opcode = VOP_Fetch1;
		mi = 0;
		iflag_pending = FlagI;
		ExecuteOneRetry();
		return;
	}
	void End_BranchSpecial()
	{
		End();
	}


	void ExecuteOneRetry()
	{
		//dont know whether this system is any faster. hard to get benchmarks someone else try it?
		//Uop uop = (Uop)CompiledMicrocode[MicrocodeIndex[opcode] + mi];
		Uop uop = Microcode[opcode][mi];
		switch (uop)
		{
		case Uop_Fetch1: Fetch1(); break;
		case Uop_Fetch1_Real: Fetch1_Real(); break;
		case Uop_Fetch2: Fetch2(); break;
		case Uop_Fetch3: Fetch3(); break;
		case Uop_FetchDummy: FetchDummy(); break;
		case Uop_PushPCH: PushPCH(); break;
		case Uop_PushPCL: PushPCL(); break;
		case Uop_PushP_BRK: PushP_BRK(); break;
		case Uop_PushP_IRQ: PushP_IRQ(); break;
		case Uop_PushP_NMI: PushP_NMI(); break;
		case Uop_PushP_Reset: PushP_Reset(); break;
		case Uop_PushDummy: PushDummy(); break;
		case Uop_FetchPCLVector: FetchPCLVector(); break;
		case Uop_FetchPCHVector: FetchPCHVector(); break;
		case Uop_Imp_INY: Imp_INY(); break;
		case Uop_Imp_DEY: Imp_DEY(); break;
		case Uop_Imp_INX: Imp_INX(); break;
		case Uop_Imp_DEX: Imp_DEX(); break;
		case Uop_NZ_A: NZ_A(); break;
		case Uop_NZ_X: NZ_X(); break;
		case Uop_NZ_Y: NZ_Y(); break;
		case Uop_Imp_TSX: Imp_TSX(); break;
		case Uop_Imp_TXS: Imp_TXS(); break;
		case Uop_Imp_TAX: Imp_TAX(); break;
		case Uop_Imp_TAY: Imp_TAY(); break;
		case Uop_Imp_TYA: Imp_TYA(); break;
		case Uop_Imp_TXA: Imp_TXA(); break;
		case Uop_Imp_SEI: Imp_SEI(); break;
		case Uop_Imp_CLI: Imp_CLI(); break;
		case Uop_Imp_SEC: Imp_SEC(); break;
		case Uop_Imp_CLC: Imp_CLC(); break;
		case Uop_Imp_SED: Imp_SED(); break;
		case Uop_Imp_CLD: Imp_CLD(); break;
		case Uop_Imp_CLV: Imp_CLV(); break;
		case Uop_Abs_WRITE_STA: Abs_WRITE_STA(); break;
		case Uop_Abs_WRITE_STX: Abs_WRITE_STX(); break;
		case Uop_Abs_WRITE_STY: Abs_WRITE_STY(); break;
		case Uop_Abs_WRITE_SAX: Abs_WRITE_SAX(); break;
		case Uop_ZP_WRITE_STA: ZP_WRITE_STA(); break;
		case Uop_ZP_WRITE_STY: ZP_WRITE_STY(); break;
		case Uop_ZP_WRITE_STX: ZP_WRITE_STX(); break;
		case Uop_ZP_WRITE_SAX: ZP_WRITE_SAX(); break;
		case Uop_IndIdx_Stage3: IndIdx_Stage3(); break;
		case Uop_IndIdx_Stage4: IndIdx_Stage4(); break;
		case Uop_IndIdx_WRITE_Stage5: IndIdx_WRITE_Stage5(); break;
		case Uop_IndIdx_READ_Stage5: IndIdx_READ_Stage5(); break;
		case Uop_IndIdx_RMW_Stage5: IndIdx_RMW_Stage5(); break;
		case Uop_IndIdx_WRITE_Stage6_STA: IndIdx_WRITE_Stage6_STA(); break;
		case Uop_IndIdx_WRITE_Stage6_SHA: IndIdx_WRITE_Stage6_SHA(); break;
		case Uop_IndIdx_READ_Stage6_LDA: IndIdx_READ_Stage6_LDA(); break;
		case Uop_IndIdx_READ_Stage6_CMP: IndIdx_READ_Stage6_CMP(); break;
		case Uop_IndIdx_READ_Stage6_AND: IndIdx_READ_Stage6_AND(); break;
		case Uop_IndIdx_READ_Stage6_EOR: IndIdx_READ_Stage6_EOR(); break;
		case Uop_IndIdx_READ_Stage6_LAX: IndIdx_READ_Stage6_LAX(); break;
		case Uop_IndIdx_READ_Stage6_ADC: IndIdx_READ_Stage6_ADC(); break;
		case Uop_IndIdx_READ_Stage6_SBC: IndIdx_READ_Stage6_SBC(); break;
		case Uop_IndIdx_READ_Stage6_ORA: IndIdx_READ_Stage6_ORA(); break;
		case Uop_IndIdx_RMW_Stage6: IndIdx_RMW_Stage6(); break;
		case Uop_IndIdx_RMW_Stage7_SLO: IndIdx_RMW_Stage7_SLO(); break;
		case Uop_IndIdx_RMW_Stage7_SRE: IndIdx_RMW_Stage7_SRE(); break;
		case Uop_IndIdx_RMW_Stage7_RRA: IndIdx_RMW_Stage7_RRA(); break;
		case Uop_IndIdx_RMW_Stage7_ISC: IndIdx_RMW_Stage7_ISC(); break;
		case Uop_IndIdx_RMW_Stage7_DCP: IndIdx_RMW_Stage7_DCP(); break;
		case Uop_IndIdx_RMW_Stage7_RLA: IndIdx_RMW_Stage7_RLA(); break;
		case Uop_IndIdx_RMW_Stage8: IndIdx_RMW_Stage8(); break;
		case Uop_RelBranch_Stage2_BVS: RelBranch_Stage2_BVS(); break;
		case Uop_RelBranch_Stage2_BVC: RelBranch_Stage2_BVC(); break;
		case Uop_RelBranch_Stage2_BMI: RelBranch_Stage2_BMI(); break;
		case Uop_RelBranch_Stage2_BPL: RelBranch_Stage2_BPL(); break;
		case Uop_RelBranch_Stage2_BCS: RelBranch_Stage2_BCS(); break;
		case Uop_RelBranch_Stage2_BCC: RelBranch_Stage2_BCC(); break;
		case Uop_RelBranch_Stage2_BEQ: RelBranch_Stage2_BEQ(); break;
		case Uop_RelBranch_Stage2_BNE: RelBranch_Stage2_BNE(); break;
		case Uop_RelBranch_Stage2: RelBranch_Stage2(); break;
		case Uop_RelBranch_Stage3: RelBranch_Stage3(); break;
		case Uop_RelBranch_Stage4: RelBranch_Stage4(); break;
		case Uop_NOP: NOP(); break;
		case Uop_DecS: DecS(); break;
		case Uop_IncS: IncS(); break;
		case Uop_JSR: JSR(); break;
		case Uop_PullP: PullP(); break;
		case Uop_PullPCL: PullPCL(); break;
		case Uop_PullPCH_NoInc: PullPCH_NoInc(); break;
		case Uop_Abs_READ_LDA: Abs_READ_LDA(); break;
		case Uop_Abs_READ_LDY: Abs_READ_LDY(); break;
		case Uop_Abs_READ_LDX: Abs_READ_LDX(); break;
		case Uop_Abs_READ_BIT: Abs_READ_BIT(); break;
		case Uop_Abs_READ_LAX: Abs_READ_LAX(); break;
		case Uop_Abs_READ_AND: Abs_READ_AND(); break;
		case Uop_Abs_READ_EOR: Abs_READ_EOR(); break;
		case Uop_Abs_READ_ORA: Abs_READ_ORA(); break;
		case Uop_Abs_READ_ADC: Abs_READ_ADC(); break;
		case Uop_Abs_READ_CMP: Abs_READ_CMP(); break;
		case Uop_Abs_READ_CPY: Abs_READ_CPY(); break;
		case Uop_Abs_READ_NOP: Abs_READ_NOP(); break;
		case Uop_Abs_READ_CPX: Abs_READ_CPX(); break;
		case Uop_Abs_READ_SBC: Abs_READ_SBC(); break;
		case Uop_ZpIdx_Stage3_X: ZpIdx_Stage3_X(); break;
		case Uop_ZpIdx_Stage3_Y: ZpIdx_Stage3_Y(); break;
		case Uop_ZpIdx_RMW_Stage4: ZpIdx_RMW_Stage4(); break;
		case Uop_ZpIdx_RMW_Stage6: ZpIdx_RMW_Stage6(); break;
		case Uop_ZP_READ_EOR: ZP_READ_EOR(); break;
		case Uop_ZP_READ_BIT: ZP_READ_BIT(); break;
		case Uop_ZP_READ_LDA: ZP_READ_LDA(); break;
		case Uop_ZP_READ_LDY: ZP_READ_LDY(); break;
		case Uop_ZP_READ_LDX: ZP_READ_LDX(); break;
		case Uop_ZP_READ_LAX: ZP_READ_LAX(); break;
		case Uop_ZP_READ_CPY: ZP_READ_CPY(); break;
		case Uop_ZP_READ_CMP: ZP_READ_CMP(); break;
		case Uop_ZP_READ_CPX: ZP_READ_CPX(); break;
		case Uop_ZP_READ_ORA: ZP_READ_ORA(); break;
		case Uop_ZP_READ_NOP: ZP_READ_NOP(); break;
		case Uop_ZP_READ_SBC: ZP_READ_SBC(); break;
		case Uop_ZP_READ_ADC: ZP_READ_ADC(); break;
		case Uop_ZP_READ_AND: ZP_READ_AND(); break;
		case Uop__Cpx: _Cpx(); break;
		case Uop__Cpy: _Cpy(); break;
		case Uop__Cmp: _Cmp(); break;
		case Uop__Eor: _Eor(); break;
		case Uop__And: _And(); break;
		case Uop__Ora: _Ora(); break;
		case Uop__Anc: _Anc(); break;
		case Uop__Asr: _Asr(); break;
		case Uop__Axs: _Axs(); break;
		case Uop__Arr: _Arr(); break;
		case Uop__Lxa: _Lxa(); break;
		case Uop__Sbc: _Sbc(); break;
		case Uop__Adc: _Adc(); break;
		case Uop_Unsupported: Unsupported(); break;
		case Uop_Imm_EOR: Imm_EOR(); break;
		case Uop_Imm_ANC: Imm_ANC(); break;
		case Uop_Imm_ASR: Imm_ASR(); break;
		case Uop_Imm_AXS: Imm_AXS(); break;
		case Uop_Imm_ARR: Imm_ARR(); break;
		case Uop_Imm_LXA: Imm_LXA(); break;
		case Uop_Imm_ORA: Imm_ORA(); break;
		case Uop_Imm_CPY: Imm_CPY(); break;
		case Uop_Imm_CPX: Imm_CPX(); break;
		case Uop_Imm_CMP: Imm_CMP(); break;
		case Uop_Imm_SBC: Imm_SBC(); break;
		case Uop_Imm_AND: Imm_AND(); break;
		case Uop_Imm_ADC: Imm_ADC(); break;
		case Uop_Imm_LDA: Imm_LDA(); break;
		case Uop_Imm_LDX: Imm_LDX(); break;
		case Uop_Imm_LDY: Imm_LDY(); break;
		case Uop_Imm_Unsupported: Imm_Unsupported(); break;
		case Uop_IdxInd_Stage3: IdxInd_Stage3(); break;
		case Uop_IdxInd_Stage4: IdxInd_Stage4(); break;
		case Uop_IdxInd_Stage5: IdxInd_Stage5(); break;
		case Uop_IdxInd_Stage6_READ_LDA: IdxInd_Stage6_READ_LDA(); break;
		case Uop_IdxInd_Stage6_READ_ORA: IdxInd_Stage6_READ_ORA(); break;
		case Uop_IdxInd_Stage6_READ_LAX: IdxInd_Stage6_READ_LAX(); break;
		case Uop_IdxInd_Stage6_READ_CMP: IdxInd_Stage6_READ_CMP(); break;
		case Uop_IdxInd_Stage6_READ_ADC: IdxInd_Stage6_READ_ADC(); break;
		case Uop_IdxInd_Stage6_READ_AND: IdxInd_Stage6_READ_AND(); break;
		case Uop_IdxInd_Stage6_READ_EOR: IdxInd_Stage6_READ_EOR(); break;
		case Uop_IdxInd_Stage6_READ_SBC: IdxInd_Stage6_READ_SBC(); break;
		case Uop_IdxInd_Stage6_WRITE_STA: IdxInd_Stage6_WRITE_STA(); break;
		case Uop_IdxInd_Stage6_WRITE_SAX: IdxInd_Stage6_WRITE_SAX(); break;
		case Uop_IdxInd_Stage6_RMW: IdxInd_Stage6_RMW(); break;
		case Uop_IdxInd_Stage7_RMW_SLO: IdxInd_Stage7_RMW_SLO(); break;
		case Uop_IdxInd_Stage7_RMW_ISC: IdxInd_Stage7_RMW_ISC(); break;
		case Uop_IdxInd_Stage7_RMW_DCP: IdxInd_Stage7_RMW_DCP(); break;
		case Uop_IdxInd_Stage7_RMW_SRE: IdxInd_Stage7_RMW_SRE(); break;
		case Uop_IdxInd_Stage7_RMW_RRA: IdxInd_Stage7_RMW_RRA(); break;
		case Uop_IdxInd_Stage7_RMW_RLA: IdxInd_Stage7_RMW_RLA(); break;
		case Uop_IdxInd_Stage8_RMW: IdxInd_Stage8_RMW(); break;
		case Uop_PushP: PushP(); break;
		case Uop_PushA: PushA(); break;
		case Uop_PullA_NoInc: PullA_NoInc(); break;
		case Uop_PullP_NoInc: PullP_NoInc(); break;
		case Uop_Imp_ASL_A: Imp_ASL_A(); break;
		case Uop_Imp_ROL_A: Imp_ROL_A(); break;
		case Uop_Imp_ROR_A: Imp_ROR_A(); break;
		case Uop_Imp_LSR_A: Imp_LSR_A(); break;
		case Uop_JMP_abs: JMP_abs(); break;
		case Uop_IncPC: IncPC(); break;
		case Uop_ZP_RMW_Stage3: ZP_RMW_Stage3(); break;
		case Uop_ZP_RMW_Stage5: ZP_RMW_Stage5(); break;
		case Uop_ZP_RMW_INC: ZP_RMW_INC(); break;
		case Uop_ZP_RMW_DEC: ZP_RMW_DEC(); break;
		case Uop_ZP_RMW_ASL: ZP_RMW_ASL(); break;
		case Uop_ZP_RMW_SRE: ZP_RMW_SRE(); break;
		case Uop_ZP_RMW_RRA: ZP_RMW_RRA(); break;
		case Uop_ZP_RMW_DCP: ZP_RMW_DCP(); break;
		case Uop_ZP_RMW_LSR: ZP_RMW_LSR(); break;
		case Uop_ZP_RMW_ROR: ZP_RMW_ROR(); break;
		case Uop_ZP_RMW_ROL: ZP_RMW_ROL(); break;
		case Uop_ZP_RMW_SLO: ZP_RMW_SLO(); break;
		case Uop_ZP_RMW_ISC: ZP_RMW_ISC(); break;
		case Uop_ZP_RMW_RLA: ZP_RMW_RLA(); break;
		case Uop_AbsIdx_Stage3_Y: AbsIdx_Stage3_Y(); break;
		case Uop_AbsIdx_Stage3_X: AbsIdx_Stage3_X(); break;
		case Uop_AbsIdx_READ_Stage4: AbsIdx_READ_Stage4(); break;
		case Uop_AbsIdx_Stage4: AbsIdx_Stage4(); break;
		case Uop_AbsIdx_WRITE_Stage5_STA: AbsIdx_WRITE_Stage5_STA(); break;
		case Uop_AbsIdx_WRITE_Stage5_SHY: AbsIdx_WRITE_Stage5_SHY(); break;
		case Uop_AbsIdx_WRITE_Stage5_SHX: AbsIdx_WRITE_Stage5_SHX(); break;
		case Uop_AbsIdx_WRITE_Stage5_ERROR: AbsIdx_WRITE_Stage5_ERROR(); break;
		case Uop_AbsIdx_RMW_Stage5: AbsIdx_RMW_Stage5(); break;
		case Uop_AbsIdx_RMW_Stage7: AbsIdx_RMW_Stage7(); break;
		case Uop_AbsIdx_RMW_Stage6_DEC: AbsIdx_RMW_Stage6_DEC(); break;
		case Uop_AbsIdx_RMW_Stage6_DCP: AbsIdx_RMW_Stage6_DCP(); break;
		case Uop_AbsIdx_RMW_Stage6_ISC: AbsIdx_RMW_Stage6_ISC(); break;
		case Uop_AbsIdx_RMW_Stage6_INC: AbsIdx_RMW_Stage6_INC(); break;
		case Uop_AbsIdx_RMW_Stage6_ROL: AbsIdx_RMW_Stage6_ROL(); break;
		case Uop_AbsIdx_RMW_Stage6_LSR: AbsIdx_RMW_Stage6_LSR(); break;
		case Uop_AbsIdx_RMW_Stage6_SLO: AbsIdx_RMW_Stage6_SLO(); break;
		case Uop_AbsIdx_RMW_Stage6_SRE: AbsIdx_RMW_Stage6_SRE(); break;
		case Uop_AbsIdx_RMW_Stage6_RRA: AbsIdx_RMW_Stage6_RRA(); break;
		case Uop_AbsIdx_RMW_Stage6_RLA: AbsIdx_RMW_Stage6_RLA(); break;
		case Uop_AbsIdx_RMW_Stage6_ASL: AbsIdx_RMW_Stage6_ASL(); break;
		case Uop_AbsIdx_RMW_Stage6_ROR: AbsIdx_RMW_Stage6_ROR(); break;
		case Uop_AbsIdx_READ_Stage5_LDA: AbsIdx_READ_Stage5_LDA(); break;
		case Uop_AbsIdx_READ_Stage5_LDX: AbsIdx_READ_Stage5_LDX(); break;
		case Uop_AbsIdx_READ_Stage5_LAX: AbsIdx_READ_Stage5_LAX(); break;
		case Uop_AbsIdx_READ_Stage5_LDY: AbsIdx_READ_Stage5_LDY(); break;
		case Uop_AbsIdx_READ_Stage5_ORA: AbsIdx_READ_Stage5_ORA(); break;
		case Uop_AbsIdx_READ_Stage5_NOP: AbsIdx_READ_Stage5_NOP(); break;
		case Uop_AbsIdx_READ_Stage5_CMP: AbsIdx_READ_Stage5_CMP(); break;
		case Uop_AbsIdx_READ_Stage5_SBC: AbsIdx_READ_Stage5_SBC(); break;
		case Uop_AbsIdx_READ_Stage5_ADC: AbsIdx_READ_Stage5_ADC(); break;
		case Uop_AbsIdx_READ_Stage5_EOR: AbsIdx_READ_Stage5_EOR(); break;
		case Uop_AbsIdx_READ_Stage5_AND: AbsIdx_READ_Stage5_AND(); break;
		case Uop_AbsIdx_READ_Stage5_ERROR: AbsIdx_READ_Stage5_ERROR(); break;
		case Uop_AbsInd_JMP_Stage4: AbsInd_JMP_Stage4(); break;
		case Uop_AbsInd_JMP_Stage5: AbsInd_JMP_Stage5(); break;
		case Uop_Abs_RMW_Stage4: Abs_RMW_Stage4(); break;
		case Uop_Abs_RMW_Stage5_INC: Abs_RMW_Stage5_INC(); break;
		case Uop_Abs_RMW_Stage5_DEC: Abs_RMW_Stage5_DEC(); break;
		case Uop_Abs_RMW_Stage5_DCP: Abs_RMW_Stage5_DCP(); break;
		case Uop_Abs_RMW_Stage5_ISC: Abs_RMW_Stage5_ISC(); break;
		case Uop_Abs_RMW_Stage5_ASL: Abs_RMW_Stage5_ASL(); break;
		case Uop_Abs_RMW_Stage5_ROR: Abs_RMW_Stage5_ROR(); break;
		case Uop_Abs_RMW_Stage5_SLO: Abs_RMW_Stage5_SLO(); break;
		case Uop_Abs_RMW_Stage5_RLA: Abs_RMW_Stage5_RLA(); break;
		case Uop_Abs_RMW_Stage5_SRE: Abs_RMW_Stage5_SRE(); break;
		case Uop_Abs_RMW_Stage5_RRA: Abs_RMW_Stage5_RRA(); break;
		case Uop_Abs_RMW_Stage5_ROL: Abs_RMW_Stage5_ROL(); break;
		case Uop_Abs_RMW_Stage5_LSR: Abs_RMW_Stage5_LSR(); break;
		case Uop_Abs_RMW_Stage6: Abs_RMW_Stage6(); break;
		case Uop_End_ISpecial: End_ISpecial(); break;
		case Uop_End_SuppressInterrupt: End_SuppressInterrupt(); break;
		case Uop_End: End(); break;
		case Uop_End_BranchSpecial: End_BranchSpecial(); break;
		}
	}

	__declspec(dllexport) void ExecuteOne()
	{
		if (!rdy_freeze)
		{
			TotalExecutedCycles++;

			interrupt_pending |= NMI || (IRQ && !FlagI);
		}
		rdy_freeze = false;

		//i tried making ExecuteOneRetry not re-entrant by having it set a flag instead, then exit from the call below, check the flag, and GOTO if it was flagged, but it wasnt faster
		ExecuteOneRetry();

		if (!rdy_freeze)
			mi++;
	} //ExecuteOne

}; // struct CPU
