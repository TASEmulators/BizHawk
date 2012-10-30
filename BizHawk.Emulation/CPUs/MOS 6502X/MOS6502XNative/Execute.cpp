#include "MOS6502X.h"
#include "UopEnum.h"

const int VOP_Fetch1 = 256;
const int VOP_RelativeStuff = 257;
const int VOP_RelativeStuff2 = 258;
const int VOP_RelativeStuff3 = 259;
const int VOP_NMI = 260;
const int VOP_IRQ = 261;
const int VOP_RESET = 262;
const int VOP_Fetch1_NoInterrupt = 263;

const ushort NMIVector = 0xFFFA;
const ushort ResetVector = 0xFFFC;
const ushort BRKVector = 0xFFFE;
const ushort IRQVector = 0xFFFE;

const byte TableNZ[] = 
{ 
	0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
	0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
	0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
	0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
	0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
	0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
	0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
	0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
	0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
	0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
	0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
	0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
	0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
	0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
	0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
	0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80
};



#define BIT(x,b) (((x) & (1 << (b))) != 0)

#define GetFlagC (((P) & 0x01) != 0)
#define GetFlagZ (((P) & 0x02) != 0)
#define GetFlagI (((P) & 0x04) != 0)
#define GetFlagD (((P) & 0x08) != 0)
#define GetFlagB (((P) & 0x10) != 0)
#define GetFlagT (((P) & 0x20) != 0)
#define GetFlagV (((P) & 0x40) != 0)
#define GetFlagN (((P) & 0x80) != 0)

#define SetFlagC(value) {P = (P & ~0x01) | (value ? 0x01 : 0x00);} 
#define SetFlagZ(value) {P = (P & ~0x02) | (value ? 0x02 : 0x00);} 
#define SetFlagI(value) {P = (P & ~0x04) | (value ? 0x04 : 0x00);} 
#define SetFlagD(value) {P = (P & ~0x08) | (value ? 0x08 : 0x00);} 
#define SetFlagB(value) {P = (P & ~0x10) | (value ? 0x10 : 0x00);} 
#define SetFlagT(value) {P = (P & ~0x20) | (value ? 0x20 : 0x00);} 
#define SetFlagV(value) {P = (P & ~0x40) | (value ? 0x40 : 0x00);} 
#define SetFlagN(value) {P = (P & ~0x80) | (value ? 0x80 : 0x00);} 

void MOS6502X::NESSoftReset()
{
	opcode = VOP_RESET;
	mi = 0;
	iflag_pending = true;
	SetFlagI(true);
}

void MOS6502X::ExecuteOne()
{
	byte value8, temp8;
	ushort value16;
	bool branch_taken = false;

	TotalExecutedCycles++;

	interrupt_pending |= NMI || (IRQ && !GetFlagI);

RETRY:
	Uop uop = Microcode[opcode][mi];
	switch (uop)
	{
		//default: throw new InvalidOperationException();
	case Uop_Fetch1:
		{
			bool my_iflag = GetFlagI;
			SetFlagI(iflag_pending);
			if (!branch_irq_hack)
			{
				interrupt_pending = false;
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
			}
			goto case_Uop_Fetch1_Real;
		}

case_Uop_Fetch1_Real: case Uop_Fetch1_Real:
		//if (debug) Console.WriteLine(State());
		branch_irq_hack = false;
		opcode = ReadMemory(PC++);
		mi = -1;
		break;

case Uop_Fetch2: opcode2 = ReadMemory(PC++); break;
case Uop_Fetch3: opcode3 = ReadMemory(PC++); break;
case Uop_FetchDummy: FetchDummy(); break;

case Uop_PushPCH: WriteMemory((ushort)(S-- + 0x100), (byte)(PC >> 8)); break;
case Uop_PushPCL: WriteMemory((ushort)(S-- + 0x100), (byte)PC); break;
case Uop_PushP_BRK:
	SetFlagB(true);
	WriteMemory((ushort)(S-- + 0x100), P);
	SetFlagI(true);
	ea = BRKVector;
	break;
case Uop_PushP_IRQ:
	SetFlagB(false);
	WriteMemory((ushort)(S-- + 0x100), P);
	SetFlagI(true);
	ea = IRQVector;
	break;
case Uop_PushP_NMI:
	SetFlagB(false);
	WriteMemory((ushort)(S-- + 0x100), P);
	SetFlagI(true); //is this right?
	ea = NMIVector;
	break;
case Uop_PushP_Reset:
	ea = ResetVector;
	S--;
	SetFlagI(true);
	break;
case Uop_PushDummy:
	S--;
	break;
case Uop_FetchPCLVector:
	if (ea == BRKVector && GetFlagB && NMI)
	{
		NMI = false;
		ea = NMIVector;
	}
	if(ea == IRQVector && !GetFlagB && NMI)
	{
		NMI = false;
		ea = NMIVector;
	}
	alu_temp = ReadMemory((ushort)ea);
	break;
case Uop_FetchPCHVector:
	alu_temp += ReadMemory((ushort)(ea + 1)) << 8;
	PC = (ushort)alu_temp;
	break;


case Uop_Imp_INY: FetchDummy(); Y++; goto case_Uop_NZ_Y;
case Uop_Imp_DEY: FetchDummy(); Y--; goto case_Uop_NZ_Y;
case Uop_Imp_INX: FetchDummy(); X++; goto case_Uop_NZ_X;
case Uop_Imp_DEX: FetchDummy(); X--; goto case_Uop_NZ_X;

case_Uop_NZ_A: case Uop_NZ_A: P = (byte)((P & 0x7D) | TableNZ[A]); break;
case_Uop_NZ_X: case Uop_NZ_X: P = (byte)((P & 0x7D) | TableNZ[X]); break;
case_Uop_NZ_Y: case Uop_NZ_Y: P = (byte)((P & 0x7D) | TableNZ[Y]); break;

case Uop_Imp_TSX: FetchDummy(); X = S; goto case_Uop_NZ_X;
case Uop_Imp_TXS: FetchDummy(); S = X; break;
case Uop_Imp_TAX: FetchDummy(); X = A; goto case_Uop_NZ_X;
case Uop_Imp_TAY: FetchDummy(); Y = A; goto case_Uop_NZ_Y;
case Uop_Imp_TYA: FetchDummy(); A = Y; goto case_Uop_NZ_A;
case Uop_Imp_TXA: FetchDummy(); A = X; goto case_Uop_NZ_A;

case Uop_Imp_SEI: FetchDummy(); iflag_pending = true; break;
case Uop_Imp_CLI: FetchDummy(); iflag_pending = false; break;
case Uop_Imp_SEC: FetchDummy(); SetFlagC(true); break;
case Uop_Imp_CLC: FetchDummy(); SetFlagC(false); break;
case Uop_Imp_SED: FetchDummy(); SetFlagD(true); break;
case Uop_Imp_CLD: FetchDummy(); SetFlagD(false); break;
case Uop_Imp_CLV: FetchDummy(); SetFlagV(false); break;

case Uop_Abs_WRITE_STA: WriteMemory((ushort)((opcode3 << 8) + opcode2), A); break;
case Uop_Abs_WRITE_STX: WriteMemory((ushort)((opcode3 << 8) + opcode2), X); break;
case Uop_Abs_WRITE_STY: WriteMemory((ushort)((opcode3 << 8) + opcode2), Y); break;
case Uop_Abs_WRITE_SAX: WriteMemory((ushort)((opcode3 << 8) + opcode2), (byte)(X & A)); break;

case Uop_ZP_WRITE_STA: WriteMemory(opcode2, A); break;
case Uop_ZP_WRITE_STY: WriteMemory(opcode2, Y); break;
case Uop_ZP_WRITE_STX: WriteMemory(opcode2, X); break;
case Uop_ZP_WRITE_SAX: WriteMemory(opcode2, (byte)(X & A)); break;

case Uop_IndIdx_Stage3:
	ea = ReadMemory(opcode2);
	break;
case Uop_IndIdx_Stage4:
	alu_temp = ea + Y;
	ea = (ReadMemory((byte)(opcode2+1))<<8) 
		| ((alu_temp&0xFF));
	break;
case Uop_IndIdx_WRITE_Stage5:
	ReadMemory((ushort)ea);
	ea += (alu_temp >> 8) << 8;
	break;
case Uop_IndIdx_READ_Stage5:
	if (!BIT(alu_temp,8))
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
case Uop_IndIdx_RMW_Stage5:
	if (BIT(alu_temp,8))
		ea = (ushort)(ea + 0x100);
	ReadMemory((ushort)ea);
	break;
case Uop_IndIdx_WRITE_Stage6_STA:
	WriteMemory((ushort)ea, A);
	break;
case Uop_IndIdx_WRITE_Stage6_SHA:
	WriteMemory((ushort)ea, (byte)(A&X&7));
	break;
case Uop_IndIdx_READ_Stage6_LDA:
	A = ReadMemory((ushort)ea);
	goto case_Uop_NZ_A;
case Uop_IndIdx_READ_Stage6_CMP:
	alu_temp = ReadMemory((ushort)ea);
	goto case_Uop__Cmp;
case Uop_IndIdx_READ_Stage6_AND:
	alu_temp = ReadMemory((ushort)ea);
	goto case_Uop__And;
case Uop_IndIdx_READ_Stage6_EOR:
	alu_temp = ReadMemory((ushort)ea);
	goto case_Uop__Eor;
case Uop_IndIdx_READ_Stage6_LAX:
	A = X = ReadMemory((ushort)ea);
	goto case_Uop_NZ_A;
case Uop_IndIdx_READ_Stage6_ADC:
	alu_temp = ReadMemory((ushort)ea);
	goto case_Uop__Adc;
case Uop_IndIdx_READ_Stage6_SBC:
	alu_temp = ReadMemory((ushort)ea);
	goto case_Uop__Sbc;
case Uop_IndIdx_READ_Stage6_ORA:
	alu_temp = ReadMemory((ushort)ea);
	goto case_Uop__Ora;
case Uop_IndIdx_RMW_Stage6:
	alu_temp = ReadMemory((ushort)ea);
	break;
case Uop_IndIdx_RMW_Stage7_SLO:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = (byte)alu_temp;
	SetFlagC((value8 & 0x80) != 0);
	alu_temp = value8 = (byte)((value8 << 1));
	A |= value8;
	goto case_Uop_NZ_A;
case Uop_IndIdx_RMW_Stage7_SRE:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = (byte)alu_temp;
	SetFlagC((value8 & 1) != 0);
	alu_temp = value8 = (byte)(value8 >> 1);
	A ^= value8;
	goto case_Uop_NZ_A;
case Uop_IndIdx_RMW_Stage7_RRA:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = temp8 = (byte)alu_temp;
	alu_temp = value8 = (byte)((value8 >> 1) | ((P & 1) << 7));
	SetFlagC((temp8 & 1) != 0);
	goto case_Uop__Adc;
case Uop_IndIdx_RMW_Stage7_ISC:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = temp8 = (byte)alu_temp;
	alu_temp = value8 = (byte)(value8 + 1);
	goto case_Uop__Sbc;
case Uop_IndIdx_RMW_Stage7_DCP:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = temp8 = (byte)alu_temp;
	alu_temp = value8 = (byte)(value8 - 1);
	SetFlagC((temp8 & 1) != 0);
	goto case_Uop__Cmp;
case Uop_IndIdx_RMW_Stage7_RLA:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = temp8 = (byte)alu_temp;
	alu_temp = value8 = (byte)((value8 << 1) | (P & 1));
	SetFlagC((temp8 & 0x80) != 0);
	A &= value8;
	goto case_Uop_NZ_A;
case Uop_IndIdx_RMW_Stage8:
	WriteMemory((ushort)ea, (byte)alu_temp);
	break;

case Uop_RelBranch_Stage2_BVS:
	branch_taken = GetFlagV == true;
	goto case_Uop_RelBranch_Stage2;
case Uop_RelBranch_Stage2_BVC:
	branch_taken = GetFlagV == false;
	goto case_Uop_RelBranch_Stage2;
case Uop_RelBranch_Stage2_BMI:
	branch_taken = GetFlagN == true;
	goto case_Uop_RelBranch_Stage2;
case Uop_RelBranch_Stage2_BPL:
	branch_taken = GetFlagN == false;
	goto case_Uop_RelBranch_Stage2;
case Uop_RelBranch_Stage2_BCS:
	branch_taken = GetFlagC == true;
	goto case_Uop_RelBranch_Stage2;
case Uop_RelBranch_Stage2_BCC:
	branch_taken = GetFlagC == false;
	goto case_Uop_RelBranch_Stage2;
case Uop_RelBranch_Stage2_BEQ:
	branch_taken = GetFlagZ == true;
	goto case_Uop_RelBranch_Stage2;
case Uop_RelBranch_Stage2_BNE:
	branch_taken = GetFlagZ == false;
	goto case_Uop_RelBranch_Stage2;

case_Uop_RelBranch_Stage2: case Uop_RelBranch_Stage2:
	opcode2 = ReadMemory(PC++);
	if (branch_taken)
	{
		//if the branch is taken, we enter a different bit of microcode to calculate the PC and complete the branch
		opcode = VOP_RelativeStuff;
		mi = -1;
	}

	break;
case Uop_RelBranch_Stage3:
	FetchDummy();
	alu_temp = (byte)PC + (int)(sbyte)opcode2;
	PC &= 0xFF00;
	PC |= (ushort)((alu_temp&0xFF));
	if (BIT(alu_temp,8))
	{
		//we need to carry the add, and then we'll be ready to fetch the next instruction
		opcode = VOP_RelativeStuff2;
		mi = -1;
	}
	else
	{
		//to pass cpu_interrupts_v2/5-branch_delays_irq we need to handle a quirk here
		//if we decide to interrupt in the next cycle, this condition will cause it to get deferred by one instruction
		if(!interrupt_pending)
			branch_irq_hack = true;
	}
	break;
case Uop_RelBranch_Stage4:
	FetchDummy();
	if (BIT(alu_temp,31))
		PC = (ushort)(PC - 0x100);
	else PC = (ushort)(PC + 0x100);
	break;

case Uop_NOP: break;
case Uop_DecS: S--; break;
case Uop_IncS: S++; break;
case Uop_JSR: PC = (ushort)((ReadMemory((ushort)(PC)) << 8) + opcode2); break;
case Uop_PullP: 
	P = ReadMemory((ushort)(S++ + 0x100));
	SetFlagT(true); //force T always to remain true
	break;
case Uop_PullPCL:
	PC &= 0xFF00;
	PC |= ReadMemory((ushort)(S++ + 0x100));
	break;
case Uop_PullPCH_NoInc:
	PC &= 0xFF;
	PC |= (ushort)(ReadMemory((ushort)(S + 0x100)) << 8);
	break;

case Uop_Abs_READ_LDA:
	A = ReadMemory((ushort)((opcode3 << 8) + opcode2));
	goto case_Uop_NZ_A;
case Uop_Abs_READ_LDY:
	Y = ReadMemory((ushort)((opcode3 << 8) + opcode2));
	goto case_Uop_NZ_Y;
case Uop_Abs_READ_LDX:
	X = ReadMemory((ushort)((opcode3 << 8) + opcode2));
	goto case_Uop_NZ_X;
case Uop_Abs_READ_BIT:
	alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
	goto case_Uop__Bit;
case Uop_Abs_READ_LAX:
	alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
	A = ReadMemory((ushort)((opcode3 << 8) + opcode2));
	X = A;
	goto case_Uop_NZ_A;
case Uop_Abs_READ_AND:
	alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
	goto case_Uop__And;
case Uop_Abs_READ_EOR:
	alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
	goto case_Uop__Eor;
case Uop_Abs_READ_ORA:
	alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
	goto case_Uop__Ora;
case Uop_Abs_READ_ADC:
	alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
	goto case_Uop__Adc;
case Uop_Abs_READ_CMP:
	alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
	goto case_Uop__Cmp;
case Uop_Abs_READ_CPY:
	alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
	goto case_Uop__Cpy;
case Uop_Abs_READ_NOP:
	alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
	break;
case Uop_Abs_READ_CPX:
	alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
	goto case_Uop__Cpx;
case Uop_Abs_READ_SBC:
	alu_temp = ReadMemory((ushort)((opcode3 << 8) + opcode2));
	goto case_Uop__Sbc;

case Uop_ZpIdx_Stage3_X:
	ReadMemory(opcode2);
	opcode2 = (byte)(opcode2 + X); //a bit sneaky to shove this into opcode2... but we can reuse all the zero page uops if we do that
	break;
case Uop_ZpIdx_Stage3_Y:
	ReadMemory(opcode2);
	opcode2 = (byte)(opcode2 + Y); //a bit sneaky to shove this into opcode2... but we can reuse all the zero page uops if we do that
	break;
case Uop_ZpIdx_RMW_Stage4:
	alu_temp = ReadMemory(opcode2);
	break;
case Uop_ZpIdx_RMW_Stage6:
	WriteMemory(opcode2, (byte)alu_temp);
	break;

case Uop_ZP_READ_EOR:
	alu_temp = ReadMemory(opcode2);
	goto case_Uop__Eor;
case Uop_ZP_READ_BIT:
	alu_temp = ReadMemory(opcode2);
	goto case_Uop__Bit;
case Uop_ZP_READ_LDA:
	A = ReadMemory(opcode2);
	goto case_Uop_NZ_A;
case Uop_ZP_READ_LDY:
	Y = ReadMemory(opcode2);
	goto case_Uop_NZ_Y;
case Uop_ZP_READ_LDX:
	X = ReadMemory(opcode2);
	goto case_Uop_NZ_X;
case Uop_ZP_READ_LAX:
	//?? is this right??
	X = ReadMemory(opcode2);
	A = X;
	goto case_Uop_NZ_A;
case Uop_ZP_READ_CPY:
	alu_temp = ReadMemory(opcode2);
	goto case_Uop__Cpy;
case Uop_ZP_READ_CMP:
	alu_temp = ReadMemory(opcode2);
	goto case_Uop__Cmp;
case Uop_ZP_READ_CPX:
	alu_temp = ReadMemory(opcode2);
	goto case_Uop__Cpx;
case Uop_ZP_READ_ORA:
	alu_temp = ReadMemory(opcode2);
	goto case_Uop__Ora;
case Uop_ZP_READ_NOP:
	ReadMemory(opcode2); //just a dummy
	break;
case Uop_ZP_READ_SBC:
	alu_temp = ReadMemory(opcode2);
	goto case_Uop__Sbc;
case Uop_ZP_READ_ADC:
	alu_temp = ReadMemory(opcode2);
	goto case_Uop__Adc;
case Uop_ZP_READ_AND:
	alu_temp = ReadMemory(opcode2);
	goto case_Uop__And;

case_Uop__Cpx: case Uop__Cpx:
	value8 = (byte)alu_temp;
	value16 = (ushort)(X - value8);
	SetFlagC(X >= value8);
	P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
	break;
case_Uop__Cpy: case Uop__Cpy:
	value8 = (byte)alu_temp;
	value16 = (ushort)(Y - value8);
	SetFlagC(Y >= value8);
	P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
	break;
case_Uop__Cmp: case Uop__Cmp:
	value8 = (byte)alu_temp;
	value16 = (ushort)(A - value8);
	SetFlagC(A >= value8);
	P = (byte)((P & 0x7D) | TableNZ[(byte)value16]);
	break;
case_Uop__Bit: case Uop__Bit:
	SetFlagN((alu_temp & 0x80) != 0);
	SetFlagV((alu_temp & 0x40) != 0);
	SetFlagZ((A & alu_temp) == 0);
	break;
case_Uop__Eor: case Uop__Eor:
	A ^= (byte)alu_temp;
	goto case_Uop_NZ_A;
case_Uop__And: case Uop__And:
	A &= (byte)alu_temp;
	goto case_Uop_NZ_A;
case_Uop__Ora: case Uop__Ora:
	A |= (byte)alu_temp;
	goto case_Uop_NZ_A;
case_Uop__Anc: case Uop__Anc:
	A &= (byte)alu_temp;
	SetFlagC(BIT(A,7));
	goto case_Uop_NZ_A;
case_Uop__Asr: case Uop__Asr:
	A &= (byte)alu_temp;
	SetFlagC(BIT(A,0));
	A >>= 1;
	goto case_Uop_NZ_A;
case_Uop__Axs: case Uop__Axs:
	X &= A;
	alu_temp = X - (byte)alu_temp;
	X = (byte)alu_temp;
	SetFlagC(!BIT(alu_temp,8));
	goto case_Uop_NZ_X;
case_Uop__Arr: case Uop__Arr:
	{
		A &= (byte)alu_temp;
		bool temp = BIT(A,0);
		A = (byte)((A >> 1) | (GetFlagC ? 0x80 : 0x00));
		SetFlagC(temp);
		if (BIT(A,5))
			if (BIT(A,6))
			{ SetFlagC(true); SetFlagV(false); }
			else { SetFlagV(true); SetFlagC(false); }
		else if (BIT(A,6))
		{ SetFlagV(true); SetFlagC(true); }
		else { SetFlagV(false); SetFlagC(false); }
		SetFlagZ(A == 0);
		break;
	}
case_Uop__Lxa: case Uop__Lxa:
	A |= 0xFF; //there is some debate about what this should be. it may depend on the 6502 variant. this is suggested by qeed's doc for the nes and passes blargg's instruction test
	A &= (byte)alu_temp;
	X = A;
	goto case_Uop_NZ_A;
case_Uop__Sbc: case Uop__Sbc:
	{
		value8 = (byte)alu_temp;
		int temp = A - value8 - (GetFlagC ? 0 : 1);
		if (GetFlagD && BCD_Enabled)
		{
			int lo = (A & 0x0F) - (value8 & 0x0F) - (GetFlagC ? 0 : 1);
			int hi = (A & 0xF0) - (value8 & 0xF0);
			if ((lo & 0xF0) != 0) lo -= 0x06;
			if ((lo & 0x80) != 0) hi -= 0x10;
			if ((hi & 0x0F00) != 0) hi -= 0x60;
			SetFlagV(((A ^ value8) & (A ^ temp) & 0x80) != 0);
			SetFlagC((hi & 0xFF00) == 0);
			A = (byte)((lo & 0x0F) | (hi & 0xF0));
		}
		else
		{
			SetFlagV(((A ^ value8) & (A ^ temp) & 0x80) != 0);
			SetFlagC(temp >= 0);
			A = (byte)temp;
		}
		goto case_Uop_NZ_A;
	}
case_Uop__Adc: case Uop__Adc:
	{
		//TODO - an extra cycle penalty?
		value8 = (byte)alu_temp;
		if (GetFlagD && BCD_Enabled)
		{
			int lo = (A & 0x0F) + (value8 & 0x0F) + (GetFlagC ? 1 : 0);
			int hi = (A & 0xF0) + (value8 & 0xF0);
			if (lo > 0x09)
			{
				hi += 0x10;
				lo += 0x06;
			}
			if (hi > 0x90) hi += 0x60;
			SetFlagV((~(A ^ value8) & (A ^ hi) & 0x80) != 0);
			SetFlagC(hi > 0xFF);
			A = (byte)((lo & 0x0F) | (hi & 0xF0));
		}
		else
		{
			int temp = value8 + A + (GetFlagC ? 1 : 0);
			SetFlagV((~(A ^ value8) & (A ^ temp) & 0x80) != 0);
			SetFlagC(temp > 0xFF);
			A = (byte)temp;
		}
		goto case_Uop_NZ_A;
	}

case Uop_Unsupported:
	break;

case Uop_Imm_EOR:
	alu_temp = ReadMemory(PC++);
	goto case_Uop__Eor;
case Uop_Imm_ANC:
	alu_temp = ReadMemory(PC++);
	goto case_Uop__Anc;
case Uop_Imm_ASR:
	alu_temp = ReadMemory(PC++);
	goto case_Uop__Asr;
case Uop_Imm_AXS:
	alu_temp = ReadMemory(PC++);
	goto case_Uop__Axs;
case Uop_Imm_ARR:
	alu_temp = ReadMemory(PC++);
	goto case_Uop__Arr;
case Uop_Imm_LXA:
	alu_temp = ReadMemory(PC++);
	goto case_Uop__Lxa;
case Uop_Imm_ORA:
	alu_temp = ReadMemory(PC++);
	goto case_Uop__Ora;
case Uop_Imm_CPY:
	alu_temp = ReadMemory(PC++);
	goto case_Uop__Cpy;
case Uop_Imm_CPX:
	alu_temp = ReadMemory(PC++);
	goto case_Uop__Cpx;
case Uop_Imm_CMP:
	alu_temp = ReadMemory(PC++);
	goto case_Uop__Cmp;
case Uop_Imm_SBC:
	alu_temp = ReadMemory(PC++);
	goto case_Uop__Sbc;
case Uop_Imm_AND:
	alu_temp = ReadMemory(PC++);
	goto case_Uop__And;
case Uop_Imm_ADC:
	alu_temp = ReadMemory(PC++);
	goto case_Uop__Adc;
case Uop_Imm_LDA:
	A = ReadMemory(PC++);
	goto case_Uop_NZ_A;
case Uop_Imm_LDX:
	X = ReadMemory(PC++);
	goto case_Uop_NZ_X;
case Uop_Imm_LDY:
	Y = ReadMemory(PC++);
	goto case_Uop_NZ_Y;
case Uop_Imm_Unsupported:
	ReadMemory(PC++);
	break;

case Uop_IdxInd_Stage3:
	ReadMemory(opcode2); //dummy?
	alu_temp = (opcode2 + X) & 0xFF;
	break;
case Uop_IdxInd_Stage4:
	ea = ReadMemory((ushort)alu_temp); 
	break;
case Uop_IdxInd_Stage5:
	ea += (ReadMemory((byte)(alu_temp + 1)) << 8);
	break;
case Uop_IdxInd_Stage6_READ_LDA:
	//TODO make uniform with others
	A = ReadMemory((ushort)ea);
	goto case_Uop_NZ_A;
case Uop_IdxInd_Stage6_READ_ORA:
	alu_temp = ReadMemory((ushort)ea);
	goto case_Uop__Ora;
case Uop_IdxInd_Stage6_READ_LAX:
	A = X = ReadMemory((ushort)ea);
	goto case_Uop_NZ_A;
case Uop_IdxInd_Stage6_READ_CMP:
	alu_temp = ReadMemory((ushort)ea);
	goto case_Uop__Cmp;
case Uop_IdxInd_Stage6_READ_ADC:
	alu_temp = ReadMemory((ushort)ea);
	goto case_Uop__Adc;
case Uop_IdxInd_Stage6_READ_AND:
	alu_temp = ReadMemory((ushort)ea);
	goto case_Uop__And;
case Uop_IdxInd_Stage6_READ_EOR:
	alu_temp = ReadMemory((ushort)ea);
	goto case_Uop__Eor;
case Uop_IdxInd_Stage6_READ_SBC:
	alu_temp = ReadMemory((ushort)ea);
	goto case_Uop__Sbc;
case Uop_IdxInd_Stage6_WRITE_STA:
	WriteMemory((ushort)ea, A);
	break;
case Uop_IdxInd_Stage6_WRITE_SAX:
	alu_temp = A & X;
	WriteMemory((ushort)ea, (byte)alu_temp);
	//flag writing skipped on purpose
	break;
case Uop_IdxInd_Stage6_RMW:
	alu_temp = ReadMemory((ushort)ea);
	break;
case Uop_IdxInd_Stage7_RMW_SLO:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = (byte)alu_temp;
	SetFlagC((value8 & 0x80) != 0);
	alu_temp = value8 = (byte)((value8 << 1));
	A |= value8;
	goto case_Uop_NZ_A;
case Uop_IdxInd_Stage7_RMW_ISC:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = (byte)alu_temp;
	alu_temp = value8 = (byte)(value8 + 1);
	goto case_Uop__Sbc;
case Uop_IdxInd_Stage7_RMW_DCP:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = temp8 = (byte)alu_temp;
	alu_temp = value8 = (byte)(value8 - 1);
	SetFlagC((temp8 & 1) != 0);
	goto case_Uop__Cmp;
case Uop_IdxInd_Stage7_RMW_SRE:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = (byte)alu_temp;
	SetFlagC((value8 & 1) != 0);
	alu_temp = value8 = (byte)(value8 >> 1);
	A ^= value8;
	goto case_Uop_NZ_A;
case Uop_IdxInd_Stage7_RMW_RRA:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = (byte)alu_temp;
	value8 = temp8 = (byte)alu_temp;
	alu_temp = value8 = (byte)((value8 >> 1) | ((P & 1) << 7));
	SetFlagC((temp8 & 1) != 0);
	goto case_Uop__Adc;
case Uop_IdxInd_Stage7_RMW_RLA:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = temp8 = (byte)alu_temp;
	alu_temp = value8 = (byte)((value8 << 1) | (P & 1));
	SetFlagC((temp8 & 0x80) != 0);
	A &= value8;
	goto case_Uop_NZ_A;
case Uop_IdxInd_Stage8_RMW:
	WriteMemory((ushort)ea, (byte)alu_temp);
	break;

case Uop_PushP:
	SetFlagB(true);
	WriteMemory((ushort)(S-- + 0x100), P); 
	break;
case Uop_PushA: WriteMemory((ushort)(S-- + 0x100), A); break;
case Uop_PullA_NoInc: 
	A = ReadMemory((ushort)(S + 0x100));
	goto case_Uop_NZ_A;
case Uop_PullP_NoInc:
	{
		bool my_iflag = GetFlagI;
		P = ReadMemory((ushort)(S + 0x100));
		iflag_pending = GetFlagI;
		SetFlagI(my_iflag);
		SetFlagT(true); //force T always to remain true
		break;
	}

case Uop_Imp_ASL_A:
	FetchDummy();
	SetFlagC((A & 0x80) != 0);
	A = (byte)(A << 1);
	goto case_Uop_NZ_A;
case Uop_Imp_ROL_A:
	FetchDummy();
	temp8 = A;
	A = (byte)((A << 1) | (P & 1));
	SetFlagC((temp8 & 0x80) != 0);
	goto case_Uop_NZ_A;
case Uop_Imp_ROR_A:
	FetchDummy();
	temp8 = A;
	A = (byte)((A >> 1) | ((P & 1) << 7));
	SetFlagC((temp8 & 1) != 0);
	goto case_Uop_NZ_A;
case Uop_Imp_LSR_A:
	FetchDummy();
	SetFlagC((A & 1) != 0);
	A = (byte)(A >> 1);
	goto case_Uop_NZ_A;

case Uop_JMP_abs:
	PC = (ushort)((ReadMemory(PC) << 8) + opcode2);
	break;
case Uop_IncPC: 
	PC++; 
	break;

case Uop_ZP_RMW_Stage3:
	alu_temp = ReadMemory(opcode2);
	break;
case Uop_ZP_RMW_Stage5:
	WriteMemory(opcode2,(byte)alu_temp);
	break;
case Uop_ZP_RMW_INC:
	WriteMemory(opcode2, (byte)alu_temp);
	alu_temp = (byte)((alu_temp+1)&0xFF);
	P = (byte)((P & 0x7D) | TableNZ[alu_temp]);
	break;
case Uop_ZP_RMW_DEC:
	WriteMemory(opcode2, (byte)alu_temp);
	alu_temp = (byte)((alu_temp - 1) & 0xFF);
	P = (byte)((P & 0x7D) | TableNZ[alu_temp]);
	break;
case Uop_ZP_RMW_ASL:
	WriteMemory(opcode2, (byte)alu_temp);
	value8 = (byte)alu_temp;
	SetFlagC((value8 & 0x80) != 0);
	alu_temp = value8 = (byte)(value8 << 1);
	P = (byte)((P & 0x7D) | TableNZ[value8]);
	break;
case Uop_ZP_RMW_SRE:
	WriteMemory(opcode2, (byte)alu_temp);
	value8 = (byte)alu_temp;
	SetFlagC((value8 & 1) != 0);
	alu_temp = value8 = (byte)(value8 >> 1);
	A ^= value8;
	goto case_Uop_NZ_A;
case Uop_ZP_RMW_RRA:
	WriteMemory(opcode2, (byte)alu_temp);
	value8 = temp8 = (byte)alu_temp;
	alu_temp = value8 = (byte)((value8 >> 1) | ((P & 1) << 7));
	SetFlagC((temp8 & 1) != 0);
	goto case_Uop__Adc;
case Uop_ZP_RMW_DCP:
	WriteMemory(opcode2, (byte)alu_temp);
	value8 = temp8 = (byte)alu_temp;
	alu_temp = value8 = (byte)(value8 - 1);
	SetFlagC((temp8 & 1) != 0);
	goto case_Uop__Cmp;
case Uop_ZP_RMW_LSR:
	WriteMemory(opcode2, (byte)alu_temp);
	value8 = (byte)alu_temp;
	SetFlagC((value8 & 1) != 0);
	alu_temp = value8 = (byte)(value8 >> 1);
	P = (byte)((P & 0x7D) | TableNZ[value8]);
	break;
case Uop_ZP_RMW_ROR:
	WriteMemory(opcode2, (byte)alu_temp);
	value8 = temp8 = (byte)alu_temp;
	alu_temp = value8 = (byte)((value8 >> 1) | ((P & 1) << 7));
	SetFlagC((temp8 & 1) != 0);
	P = (byte)((P & 0x7D) | TableNZ[value8]);
	break;
case Uop_ZP_RMW_ROL:
	WriteMemory(opcode2, (byte)alu_temp);
	value8 = temp8 = (byte)alu_temp;
	alu_temp = value8 = (byte)((value8 << 1) | (P & 1));
	SetFlagC((temp8 & 0x80) != 0);
	P = (byte)((P & 0x7D) | TableNZ[value8]);
	break;
case Uop_ZP_RMW_SLO:
	WriteMemory(opcode2, (byte)alu_temp);
	value8 = (byte)alu_temp;
	SetFlagC((value8 & 0x80) != 0);
	alu_temp = value8 = (byte)((value8 << 1));
	A |= value8;
	goto case_Uop_NZ_A;
case Uop_ZP_RMW_ISC:
	WriteMemory(opcode2, (byte)alu_temp);
	value8 = (byte)alu_temp;
	alu_temp = value8 = (byte)(value8 + 1);
	goto case_Uop__Sbc;
case Uop_ZP_RMW_RLA:
	WriteMemory(opcode2, (byte)alu_temp);
	value8 = temp8 = (byte)alu_temp;
	alu_temp = value8 = (byte)((value8 << 1) | (P & 1));
	SetFlagC((temp8 & 0x80) != 0);
	A &= value8;
	goto case_Uop_NZ_A;

case Uop_AbsIdx_Stage3_Y:
	opcode3 = ReadMemory(PC++);
	alu_temp = opcode2 + Y;
	ea = (opcode3 << 8) + (alu_temp & 0xFF);
	break;
	//new Uop[] { Uop_Fetch2, Uop_AbsIdx_Stage3_Y, Uop_AbsIdx_Stage4, Uop_AbsIdx_WRITE_Stage5_STA, Uop_End },
case Uop_AbsIdx_Stage3_X:
	opcode3 = ReadMemory(PC++);
	alu_temp = opcode2 + X;
	ea = (opcode3 << 8) + (alu_temp & 0xFF);
	break;
case Uop_AbsIdx_READ_Stage4:
	if (!BIT(alu_temp,8))
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
case Uop_AbsIdx_Stage4:
	//bleh.. redundant code to make sure we dont clobber alu_temp before using it to decide whether to change ea
	if (BIT(alu_temp,8))
	{
		alu_temp = ReadMemory((ushort)ea);
		ea = (ushort)(ea + 0x100);
	} 
	else alu_temp = ReadMemory((ushort)ea);
	break;

case Uop_AbsIdx_WRITE_Stage5_STA: 
	WriteMemory((ushort)ea, A); 
	break;
case Uop_AbsIdx_WRITE_Stage5_SHY:
	alu_temp = Y & (ea>>8);
	ea = (ea & 0xFF) | (alu_temp << 8); //"(the bank where the value is stored may be equal to the value stored)" -- more like IS.
	WriteMemory((ushort)ea, (byte)alu_temp);
	break;
case Uop_AbsIdx_WRITE_Stage5_SHX:
	alu_temp = X & (ea >> 8);
	ea = (ea & 0xFF) | (alu_temp << 8); //"(the bank where the value is stored may be equal to the value stored)" -- more like IS.
	WriteMemory((ushort)ea, (byte)alu_temp);
	break;
case Uop_AbsIdx_WRITE_Stage5_ERROR:
	alu_temp = ReadMemory((ushort)ea);
	//throw new InvalidOperationException("UNSUPPORTED OPCODE [probably SHS] PLEASE REPORT");
	break;

case Uop_AbsIdx_RMW_Stage5:
	alu_temp = ReadMemory((ushort)ea);
	break;
case Uop_AbsIdx_RMW_Stage7:
	WriteMemory((ushort)ea, (byte)alu_temp);
	break;
case Uop_AbsIdx_RMW_Stage6_DEC:
	WriteMemory((ushort)ea, (byte)alu_temp);
	alu_temp = value8 = (byte)(alu_temp - 1);
	P = (byte)((P & 0x7D) | TableNZ[value8]);
	break;
case Uop_AbsIdx_RMW_Stage6_DCP:
	WriteMemory((ushort)ea, (byte)alu_temp);
	alu_temp = value8 = (byte)(alu_temp - 1);
	goto case_Uop__Cmp;
case Uop_AbsIdx_RMW_Stage6_ISC:
	WriteMemory((ushort)ea, (byte)alu_temp);
	alu_temp = value8 = (byte)(alu_temp + 1);
	goto case_Uop__Sbc;
case Uop_AbsIdx_RMW_Stage6_INC:
	WriteMemory((ushort)ea, (byte)alu_temp);
	alu_temp = value8 = (byte)(alu_temp + 1);
	P = (byte)((P & 0x7D) | TableNZ[value8]);
	break;
case Uop_AbsIdx_RMW_Stage6_ROL:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = temp8 = (byte)alu_temp;
	alu_temp = value8 = (byte)((value8 << 1) | (P & 1));
	SetFlagC((temp8 & 0x80) != 0);
	P = (byte)((P & 0x7D) | TableNZ[value8]);
	break;
case Uop_AbsIdx_RMW_Stage6_LSR:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = (byte)alu_temp;
	SetFlagC((value8 & 1) != 0);
	alu_temp = value8 = (byte)(value8 >> 1);
	P = (byte)((P & 0x7D) | TableNZ[value8]);
	break;
case Uop_AbsIdx_RMW_Stage6_SLO:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = (byte)alu_temp;
	SetFlagC((value8 & 0x80) != 0);
	alu_temp = value8 = (byte)(value8 << 1);
	A |= value8;
	goto case_Uop_NZ_A;
case Uop_AbsIdx_RMW_Stage6_SRE:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = (byte)alu_temp;
	SetFlagC((value8 & 1) != 0);
	alu_temp = value8 = (byte)(value8 >> 1);
	A ^= value8;
	goto case_Uop_NZ_A;
case Uop_AbsIdx_RMW_Stage6_RRA:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = temp8 = (byte)alu_temp;
	alu_temp = value8 = (byte)((value8 >> 1) | ((P & 1) << 7));
	SetFlagC((temp8 & 1) != 0);
	goto case_Uop__Adc;
case Uop_AbsIdx_RMW_Stage6_RLA:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = temp8 = (byte)alu_temp;
	alu_temp = value8 = (byte)((value8 << 1) | (P & 1));
	SetFlagC((temp8 & 0x80) != 0);
	A &= value8;
	goto case_Uop_NZ_A;
case Uop_AbsIdx_RMW_Stage6_ASL:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = (byte)alu_temp;
	SetFlagC((value8 & 0x80) != 0);
	alu_temp = value8 = (byte)(value8 << 1);
	P = (byte)((P & 0x7D) | TableNZ[value8]);
	break;
case Uop_AbsIdx_RMW_Stage6_ROR:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = temp8 = (byte)alu_temp;
	alu_temp = value8 = (byte)((value8 >> 1) | ((P & 1) << 7));
	SetFlagC((temp8 & 1) != 0);
	P = (byte)((P & 0x7D) | TableNZ[value8]);
	break;

case Uop_AbsIdx_READ_Stage5_LDA:
	A = ReadMemory((ushort)ea);
	goto case_Uop_NZ_A;
case Uop_AbsIdx_READ_Stage5_LDX:
	X = ReadMemory((ushort)ea);
	goto case_Uop_NZ_X;
case Uop_AbsIdx_READ_Stage5_LAX:
	A = ReadMemory((ushort)ea);
	X = A;
	goto case_Uop_NZ_A;
case Uop_AbsIdx_READ_Stage5_LDY:
	Y = ReadMemory((ushort)ea);
	goto case_Uop_NZ_Y;
case Uop_AbsIdx_READ_Stage5_ORA:
	alu_temp = ReadMemory((ushort)ea);
	goto case_Uop__Ora;
case Uop_AbsIdx_READ_Stage5_NOP:
	alu_temp = ReadMemory((ushort)ea);
	break;
case Uop_AbsIdx_READ_Stage5_CMP:
	alu_temp = ReadMemory((ushort)ea);
	goto case_Uop__Cmp;
case Uop_AbsIdx_READ_Stage5_SBC:
	alu_temp = ReadMemory((ushort)ea);
	goto case_Uop__Sbc;
case Uop_AbsIdx_READ_Stage5_ADC:
	alu_temp = ReadMemory((ushort)ea);
	goto case_Uop__Adc;
case Uop_AbsIdx_READ_Stage5_EOR:
	alu_temp = ReadMemory((ushort)ea);
	goto case_Uop__Eor;
case Uop_AbsIdx_READ_Stage5_AND:
	alu_temp = ReadMemory((ushort)ea);
	goto case_Uop__And;
case Uop_AbsIdx_READ_Stage5_ERROR:
	alu_temp = ReadMemory((ushort)ea);
	//throw new InvalidOperationException("UNSUPPORTED OPCODE [probably LAS] PLEASE REPORT");
	break;

case Uop_AbsInd_JMP_Stage4:
	ea = (opcode3<<8)+opcode2;
	alu_temp = ReadMemory((ushort)ea);
	break;
case Uop_AbsInd_JMP_Stage5:
	ea = (opcode3<<8)+(byte)(opcode2+1);
	alu_temp += ReadMemory((ushort)ea) << 8;
	PC = (ushort)alu_temp;
	break;

case Uop_Abs_RMW_Stage4:
	ea = (opcode3<<8)+opcode2;
	alu_temp = ReadMemory((ushort)ea);
	break;
case Uop_Abs_RMW_Stage5_INC:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = (byte)(alu_temp + 1);
	alu_temp = value8;
	P = (byte)((P & 0x7D) | TableNZ[value8]);
	break;
case Uop_Abs_RMW_Stage5_DEC:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = (byte)(alu_temp - 1);
	alu_temp = value8;
	P = (byte)((P & 0x7D) | TableNZ[value8]);
	break;
case Uop_Abs_RMW_Stage5_DCP:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = (byte)(alu_temp - 1);
	alu_temp = value8;
	goto case_Uop__Cmp;
case Uop_Abs_RMW_Stage5_ISC:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = (byte)(alu_temp + 1);
	alu_temp = value8;
	goto case_Uop__Sbc;
case Uop_Abs_RMW_Stage5_ASL:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = (byte)alu_temp;
	SetFlagC((value8 & 0x80) != 0);
	alu_temp = value8 = (byte)(value8 << 1);
	P = (byte)((P & 0x7D) | TableNZ[value8]);
	break;
case Uop_Abs_RMW_Stage5_ROR:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = temp8 = (byte)alu_temp;
	alu_temp = value8 = (byte)((value8 >> 1) | ((P & 1) << 7));
	SetFlagC((temp8 & 1) != 0);
	P = (byte)((P & 0x7D) | TableNZ[value8]);
	break;
case Uop_Abs_RMW_Stage5_SLO:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = (byte)alu_temp;
	SetFlagC((value8 & 0x80) != 0);
	alu_temp = value8 = (byte)(value8 << 1);
	A |= value8;
	goto case_Uop_NZ_A;
case Uop_Abs_RMW_Stage5_RLA:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = temp8 = (byte)alu_temp;
	alu_temp = value8 = (byte)((value8 << 1) | (P & 1));
	SetFlagC((temp8 & 0x80) != 0);
	A &= value8;
	goto case_Uop_NZ_A;
case Uop_Abs_RMW_Stage5_SRE:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = (byte)alu_temp;
	SetFlagC((value8 & 1) != 0);
	alu_temp = value8 = (byte)(value8 >> 1);
	A ^= value8;
	goto case_Uop_NZ_A;
case Uop_Abs_RMW_Stage5_RRA:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = temp8 = (byte)alu_temp;
	alu_temp = value8 = (byte)((value8 >> 1) | ((P & 1) << 7));
	SetFlagC((temp8 & 1) != 0);
	goto case_Uop__Adc;
case Uop_Abs_RMW_Stage5_ROL:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = temp8 = (byte)alu_temp;
	alu_temp = value8 = (byte)((value8 << 1) | (P & 1));
	SetFlagC((temp8 & 0x80) != 0);
	P = (byte)((P & 0x7D) | TableNZ[value8]);
	break;
case Uop_Abs_RMW_Stage5_LSR:
	WriteMemory((ushort)ea, (byte)alu_temp);
	value8 = (byte)alu_temp;
	SetFlagC((value8 & 1) != 0);
	alu_temp = value8 = (byte)(value8 >> 1);
	P = (byte)((P & 0x7D) | TableNZ[value8]);
	break;

case Uop_Abs_RMW_Stage6:
	WriteMemory((ushort)ea, (byte)alu_temp);
	break;

case Uop_End_ISpecial:
	opcode = VOP_Fetch1;
	mi = 0;
	goto RETRY;

case Uop_End_SuppressInterrupt:
	opcode = VOP_Fetch1_NoInterrupt;
	mi = 0;
	goto RETRY;

case_Uop_End: case Uop_End:
	opcode = VOP_Fetch1;
	mi = 0;
	iflag_pending = GetFlagI;
	goto RETRY;
case Uop_End_BranchSpecial:
	goto case_Uop_End;
	}

	mi++;

}

