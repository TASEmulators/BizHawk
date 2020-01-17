#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

using namespace std;

namespace MSXHawk
{
	class MemoryManager;
	
	class Z80A
	{
	public:

		#pragma region Variable Declarations

		// pointer to controlling memory manager goes here
		// this will be iplementation dependent
		MemoryManager* mem_ctrl;
		
		// Memory is usually mostly static, so it is efficient to access it with a pointer and write mask
		// the size of the pointer matrix and masks is system dependent.
		// This also assumes a simple relationship between bank and write mask
		// some systems might require more detailed mask, maybe even the same size as read
		const uint32_t low_mask = 0x3FF;
		const uint32_t high_mask = 0x3F;
		const uint32_t bank_shift = 10;
		
		uint32_t bank_num;
		uint32_t bank_offset;
		uint8_t* MemoryMap[64];
		uint8_t MemoryMapMask[64];

		// Port action is highly variable based on application, typically this will not suitable for a static mapping
		// uint8_t* HWMemoryMap;
		// uint8_t* HWMemoryMapMask;
		void HW_Write(uint32_t, uint8_t);
		uint8_t HW_Read(uint32_t);
		void Memory_Write(uint32_t, uint8_t);

		// when connected devices do not output a value on the BUS, they are responsible for determining open bus behaviour and returning it
		uint32_t ExternalDB;

		long TotalExecutedCycles;
		uint32_t PRE_SRC;
		uint32_t EI_pending;
		// variables for executing instructions
		uint32_t instr_pntr = 0;
		uint32_t bus_pntr = 0;
		uint32_t mem_pntr = 0;
		uint32_t irq_pntr = 0;
		uint32_t cur_instr [38] = {};		// fixed size - do not change at runtime
		uint32_t BUSRQ [19] = {};         // fixed size - do not change at runtime
		uint32_t MEMRQ [19] = {};         // fixed size - do not change at runtime
		uint32_t IRQS;
		bool NO_prefix, CB_prefix, IX_prefix, EXTD_prefix, IY_prefix, IXCB_prefix, IYCB_prefix;
		bool halted;
		bool I_skip;
		bool FlagI;
		bool FlagW; // wait flag, when set to true reads / writes will be delayed

		uint8_t opcode;
		uint8_t temp_R;
		uint8_t Regs[36] = {};

		// non-state variables
		uint32_t Ztemp1, Ztemp2, Ztemp3, Ztemp4;
		uint32_t Reg16_d, Reg16_s, ans, temp, carry, dest_t, src_t;


		inline bool FlagCget() { return (Regs[5] & 0x01) != 0; };
		inline void FlagCset(bool value) { Regs[5] = (uint32_t)((Regs[5] & ~0x01) | (value ? 0x01 : 0x00)); }

		inline bool FlagNget() { return (Regs[5] & 0x02) != 0; };
		inline void FlagNset(bool value) { Regs[5] = (uint32_t)((Regs[5] & ~0x02) | (value ? 0x02 : 0x00)); }

		inline bool FlagPget() { return (Regs[5] & 0x04) != 0; };
		inline void FlagPset(bool value) { Regs[5] = (uint32_t)((Regs[5] & ~0x04) | (value ? 0x04 : 0x00)); }

		inline bool Flag3get() { return (Regs[5] & 0x08) != 0; };
		inline void Flag3set(bool value) { Regs[5] = (uint32_t)((Regs[5] & ~0x08) | (value ? 0x08 : 0x00)); }

		inline bool FlagHget() { return (Regs[5] & 0x10) != 0; };
		inline void FlagHset(bool value) { Regs[5] = (uint32_t)((Regs[5] & ~0x10) | (value ? 0x10 : 0x00)); }

		inline bool Flag5get() { return (Regs[5] & 0x20) != 0; };
		inline void Flag5set(bool value) { Regs[5] = (uint32_t)((Regs[5] & ~0x20) | (value ? 0x20 : 0x00)); }

		inline bool FlagZget() { return (Regs[5] & 0x40) != 0; };
		inline void FlagZset(bool value) { Regs[5] = (uint32_t)((Regs[5] & ~0x40) | (value ? 0x40 : 0x00)); }

		inline bool FlagSget() { return (Regs[5] & 0x80) != 0; };
		inline void FlagSset(bool value) { Regs[5] = (uint32_t)((Regs[5] & ~0x80) | (value ? 0x80 : 0x00)); }

		inline uint32_t RegPCget() { return (uint32_t)(Regs[0] | (Regs[1] << 8)); }
		inline void RegPCset(uint32_t value) { Regs[0] = (uint32_t)(value & 0xFF); Regs[1] = (uint32_t)((value >> 8) & 0xFF); }

		bool TableParity [256] = {};
		bool IFF1;
		bool IFF2;
		bool nonMaskableInterrupt;
		bool nonMaskableInterruptPending;

		inline bool NonMaskableInterruptget() { return nonMaskableInterrupt; };
		inline void NonMaskableInterruptset(bool value)
		{ 
			if (value && !nonMaskableInterrupt) nonMaskableInterruptPending = true; 
			nonMaskableInterrupt = value; 
		}

		uint32_t interruptMode;

		inline uint32_t InterruptModeget() { return interruptMode; };
		inline void InterruptModeset(uint32_t value)
		{
			if (value < 0 || value > 2) { /* add exception here */ }
			interruptMode = value;
		}

		#pragma endregion

		#pragma region Constant Declarations
		// prefix related
		const static uint32_t CBpre = 0;
		const static uint32_t EXTDpre = 1;
		const static uint32_t IXpre = 2;
		const static uint32_t IYpre = 3;
		const static uint32_t IXCBpre = 4;
		const static uint32_t IYCBpre = 5;
		const static uint32_t IXYprefetch = 6;

		// operations that can take place in an instruction
		const static uint32_t IDLE = 0;
		const static uint32_t OP = 1;
		const static uint32_t OP_F = 2; // used for repeating operations
		const static uint32_t HALT = 3;
		const static uint32_t RD = 4;
		const static uint32_t WR = 5;
		const static uint32_t RD_INC = 6; // read and increment
		const static uint32_t WR_INC = 7; // write and increment
		const static uint32_t WR_DEC = 8; // write and increment (for stack pointer)
		const static uint32_t TR = 9;
		const static uint32_t TR16 = 10;
		const static uint32_t ADD16 = 11;
		const static uint32_t ADD8 = 12;
		const static uint32_t SUB8 = 13;
		const static uint32_t ADC8 = 14;
		const static uint32_t SBC8 = 15;
		const static uint32_t SBC16 = 16;
		const static uint32_t ADC16 = 17;
		const static uint32_t INC16 = 18;
		const static uint32_t INC8 = 19;
		const static uint32_t DEC16 = 20;
		const static uint32_t DEC8 = 21;
		const static uint32_t RLC = 22;
		const static uint32_t RL = 23;
		const static uint32_t RRC = 24;
		const static uint32_t RR = 25;
		const static uint32_t CPL = 26;
		const static uint32_t DA = 27;
		const static uint32_t SCF = 28;
		const static uint32_t CCF = 29;
		const static uint32_t AND8 = 30;
		const static uint32_t XOR8 = 31;
		const static uint32_t OR8 = 32;
		const static uint32_t CP8 = 33;
		const static uint32_t SLA = 34;
		const static uint32_t SRA = 35;
		const static uint32_t SRL = 36;
		const static uint32_t SLL = 37;
		const static uint32_t BIT = 38;
		const static uint32_t RES = 39;
		const static uint32_t SET = 40;
		const static uint32_t EI = 41;
		const static uint32_t DI = 42;
		const static uint32_t EXCH = 43;
		const static uint32_t EXX = 44;
		const static uint32_t EXCH_16 = 45;
		const static uint32_t PREFIX = 46;
		const static uint32_t PREFETCH = 47;
		const static uint32_t ASGN = 48;
		const static uint32_t ADDS = 49; // signed 16 bit operation used in 2 instructions
		const static uint32_t INT_MODE = 50;
		const static uint32_t EI_RETN = 51;
		const static uint32_t EI_RETI = 52; // reti has no delay in interrupt enable
		const static uint32_t OUT = 53;
		const static uint32_t IN = 54;
		const static uint32_t NEG = 55;
		const static uint32_t RRD = 56;
		const static uint32_t RLD = 57;
		const static uint32_t SET_FL_LD_R = 58;
		const static uint32_t SET_FL_CP_R = 59;
		const static uint32_t SET_FL_IR = 60;
		const static uint32_t I_BIT = 61;
		const static uint32_t HL_BIT = 62;
		const static uint32_t FTCH_DB = 63;
		const static uint32_t WAIT = 64; // enterred when reading or writing and FlagW is true
		const static uint32_t RST = 65;
		const static uint32_t REP_OP_I = 66;
		const static uint32_t REP_OP_O = 67;
		const static uint32_t IN_A_N_INC = 68;
		const static uint32_t RD_INC_TR_PC = 69; // transfer WZ to PC after read
		const static uint32_t WR_TR_PC = 70; // transfer WZ to PC after write
		const static uint32_t OUT_INC = 71;
		const static uint32_t IN_INC = 72;
		const static uint32_t WR_INC_WA = 73; // A -> W after WR_INC
		const static uint32_t RD_OP = 74;
		const static uint32_t IORQ = 75;

		// registers
		const static uint32_t PCl = 0;
		const static uint32_t PCh = 1;
		const static uint32_t SPl = 2;
		const static uint32_t SPh = 3;
		const static uint32_t A = 4;
		const static uint32_t F = 5;
		const static uint32_t B = 6;
		const static uint32_t C = 7;
		const static uint32_t D = 8;
		const static uint32_t E = 9;
		const static uint32_t H = 10;
		const static uint32_t L = 11;
		const static uint32_t W = 12;
		const static uint32_t Z = 13;
		const static uint32_t Aim = 14; // use this indicator for RLCA etc., since the Z flag is reset on those
		const static uint32_t Ixl = 15;
		const static uint32_t Ixh = 16;
		const static uint32_t Iyl = 17;
		const static uint32_t Iyh = 18;
		const static uint32_t INT = 19;
		const static uint32_t R = 20;
		const static uint32_t I = 21;
		const static uint32_t ZERO = 22; // it is convenient to have a register that is always zero, to reuse instructions
		const static uint32_t ALU = 23; // This will be temporary arthimatic storage
		// shadow registers
		const static uint32_t A_s = 24;
		const static uint32_t F_s = 25;
		const static uint32_t B_s = 26;
		const static uint32_t C_s = 27;
		const static uint32_t D_s = 28;
		const static uint32_t E_s = 29;
		const static uint32_t H_s = 30;
		const static uint32_t L_s = 31;
		const static uint32_t DB = 32;
		const static uint32_t scratch = 33;
		const static uint32_t IRQ_V = 34; // IRQ mode 1 vector
		const static uint32_t NMI_V = 35; // NMI vector

		// IO Contention Constants. Need to distinguish port access and normal memory accesses for zx spectrum
		const static uint32_t BIO1 = 100;
		const static uint32_t BIO2 = 101;
		const static uint32_t BIO3 = 102;
		const static uint32_t BIO4 = 103;

		const static uint32_t WIO1 = 105;
		const static uint32_t WIO2 = 106;
		const static uint32_t WIO3 = 107;
		const static uint32_t WIO4 = 108;
		#pragma endregion

		#pragma region Z80 functions
		
		Z80A()
		{
			Reset();
			InitTableParity();
		}
		
		void Reset()
		{
			ResetRegisters();
			ResetInterrupts();
			TotalExecutedCycles = 0;

			PopulateCURINSTR(IDLE,
							DEC16, F, A,
							DEC16, SPl, SPh);

			PopulateBUSRQ(0, 0, 0);
			PopulateMEMRQ(0, 0, 0);
			IRQS = 3;
			instr_pntr = mem_pntr = bus_pntr = irq_pntr = 0;
			NO_prefix = true;
		}

		// Memory Access 

		// Data Bus
		// Interrupting Devices are responsible for putting a value onto the data bus
		// for as long as the interrupt is valid

		//this only calls when the first byte of an instruction is fetched.

		// Execute instructions
		void ExecuteOne()
		{
			bus_pntr++; mem_pntr++;
			switch (cur_instr[instr_pntr++])
			{
			case IDLE:
				// do nothing
				break;
			case OP:
				// should never reach here

				break;
			case OP_F:
				// Read the opcode of the next instruction	
				//if (OnExecFetch != null) OnExecFetch(RegPC);

				if (TraceCallback) { TraceCallback(0); }

				bank_num = bank_offset = RegPCget();
				bank_offset &= low_mask;
				bank_num = (bank_num >> bank_shift)& high_mask;
				opcode = MemoryMap[bank_num][bank_offset];
				RegPCset(RegPCget() + 1);
				FetchInstruction();

				temp_R = (Regs[R] & 0x7F);
				temp_R++;
				temp_R &= 0x7F;
				Regs[R] = ((Regs[R] & 0x80) | temp_R);

				instr_pntr = bus_pntr = mem_pntr = irq_pntr = 0;
				I_skip = true;
				break;
			case HALT:
				halted = true;
				// NOTE: Check how halt state effects the DB
				Regs[DB] = 0xFF;

				temp_R = (Regs[R] & 0x7F);
				temp_R++;
				temp_R &= 0x7F;
				Regs[R] = ((Regs[R] & 0x80) | temp_R);
				break;
			case RD:
				Read_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2]);
				instr_pntr += 3;
				break;
			case WR:
				Write_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2]);
				instr_pntr += 3;
				break;
			case RD_INC:
				Read_INC_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2]);
				instr_pntr += 3;
				break;
			case RD_INC_TR_PC:
				Read_INC_TR_PC_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2], cur_instr[instr_pntr + 3]);
				instr_pntr += 4;
				break;
			case RD_OP:
				if (cur_instr[instr_pntr++] == 1) { Read_INC_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2]); }
				else { Read_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2]); }
				instr_pntr += 3;
				switch (cur_instr[instr_pntr])
				{
				case ADD8:
					ADD8_Func(cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2]);
					break;
				case ADC8:
					ADC8_Func(cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2]);
					break;
				case SUB8:
					SUB8_Func(cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2]);
					break;
				case SBC8:
					SBC8_Func(cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2]);
					break;
				case AND8:
					AND8_Func(cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2]);
					break;
				case XOR8:
					XOR8_Func(cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2]);
					break;
				case OR8:
					OR8_Func(cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2]);
					break;
				case CP8:
					CP8_Func(cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2]);
					break;
				case TR:
					TR_Func(cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2]);
					break;
				}
				instr_pntr += 3;
				break;
			case WR_INC:
				Write_INC_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2]);
				instr_pntr += 3;
				break;
			case WR_DEC:
				Write_DEC_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2]);
				instr_pntr += 3;
				break;
			case WR_TR_PC:
				Write_TR_PC_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2]);
				instr_pntr += 3;
				break;
			case WR_INC_WA:
				Write_INC_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2]);
				instr_pntr += 3;
				Regs[W] = Regs[A];
				break;
			case TR:
				TR_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1]);
				instr_pntr += 2;
				break;
			case TR16:
				TR16_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2], cur_instr[instr_pntr + 3]);
				instr_pntr += 4;
				break;
			case ADD16:
				ADD16_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2], cur_instr[instr_pntr + 3]);
				instr_pntr += 4;
				break;
			case ADD8:
				ADD8_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1]);
				instr_pntr += 2;
				break;
			case SUB8:
				SUB8_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1]);
				instr_pntr += 2;
				break;
			case ADC8:
				ADC8_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1]);
				instr_pntr += 2;
				break;
			case ADC16:
				ADC_16_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2], cur_instr[instr_pntr + 3]);
				instr_pntr += 4;
				break;
			case SBC8:
				SBC8_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1]);
				instr_pntr += 2;
				break;
			case SBC16:
				SBC_16_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2], cur_instr[instr_pntr + 3]);
				instr_pntr += 4;
				break;
			case INC16:
				INC16_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1]);
				instr_pntr += 2;
				break;
			case INC8:
				INC8_Func(cur_instr[instr_pntr]);
				instr_pntr += 1;
				break;
			case DEC16:
				DEC16_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1]);
				instr_pntr += 2;
				break;
			case DEC8:
				DEC8_Func(cur_instr[instr_pntr]);
				instr_pntr += 1;
				break;
			case RLC:
				RLC_Func(cur_instr[instr_pntr]);
				instr_pntr += 1;
				break;
			case RL:
				RL_Func(cur_instr[instr_pntr]);
				instr_pntr += 1;
				break;
			case RRC:
				RRC_Func(cur_instr[instr_pntr]);
				instr_pntr += 1;
				break;
			case RR:
				RR_Func(cur_instr[instr_pntr]);
				instr_pntr += 1;
				break;
			case CPL:
				CPL_Func(cur_instr[instr_pntr]);
				instr_pntr += 1;
				break;
			case DA:
				DA_Func(cur_instr[instr_pntr]);
				instr_pntr += 1;
				break;
			case SCF:
				SCF_Func(cur_instr[instr_pntr]);
				instr_pntr += 1;
				break;
			case CCF:
				CCF_Func(cur_instr[instr_pntr]);
				instr_pntr += 1;
				break;
			case AND8:
				AND8_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1]);
				instr_pntr += 2;
				break;
			case XOR8:
				XOR8_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1]);
				instr_pntr += 2;
				break;
			case OR8:
				OR8_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1]);
				instr_pntr += 2;
				break;
			case CP8:
				CP8_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1]);
				instr_pntr += 2;
				break;
			case SLA:
				SLA_Func(cur_instr[instr_pntr]);
				instr_pntr += 1;
				break;
			case SRA:
				SRA_Func(cur_instr[instr_pntr]);
				instr_pntr += 1;
				break;
			case SRL:
				SRL_Func(cur_instr[instr_pntr]);
				instr_pntr += 1;
				break;
			case SLL:
				SLL_Func(cur_instr[instr_pntr]);
				instr_pntr += 1;
				break;
			case BIT:
				BIT_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1]);
				instr_pntr += 2;
				break;
			case I_BIT:
				I_BIT_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1]);
				instr_pntr += 2;
				break;
			case RES:
				RES_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1]);
				instr_pntr += 2;
				break;
			case SET:
				SET_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1]);
				instr_pntr += 2;
				break;
			case EI:
				EI_pending = 2;
				break;
			case DI:
				IFF1 = IFF2 = false;
				break;
			case EXCH:
				EXCH_16_Func(F_s, A_s, F, A);
				break;
			case EXX:
				EXCH_16_Func(C_s, B_s, C, B);
				EXCH_16_Func(E_s, D_s, E, D);
				EXCH_16_Func(L_s, H_s, L, H);
				break;
			case EXCH_16:
				EXCH_16_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2], cur_instr[instr_pntr + 3]);
				instr_pntr += 4;
				break;
			case PREFIX:
				src_t = PRE_SRC;

				NO_prefix = false;
				if (PRE_SRC == CBpre) { CB_prefix = true; }
				if (PRE_SRC == EXTDpre) { EXTD_prefix = true; }
				if (PRE_SRC == IXpre) { IX_prefix = true; }
				if (PRE_SRC == IYpre) { IY_prefix = true; }
				if (PRE_SRC == IXCBpre) { IXCB_prefix = true; }
				if (PRE_SRC == IYCBpre) { IYCB_prefix = true; }

				// only the first prefix in a double prefix increases R, although I don't know how / why
				if (PRE_SRC < 4)
				{
					temp_R = (Regs[R] & 0x7F);
					temp_R++;
					temp_R &= 0x7F;
					Regs[R] = ((Regs[R] & 0x80) | temp_R);
				}

				bank_num = bank_offset = RegPCget();
				bank_offset &= low_mask;
				bank_num = (bank_num >> bank_shift)& high_mask;
				opcode = MemoryMap[bank_num][bank_offset];
				RegPCset(RegPCget() + 1);
				FetchInstruction();
				instr_pntr = bus_pntr = mem_pntr = irq_pntr = 0;
				I_skip = true;

				// for prefetched case, the PC stays on the BUS one cycle longer
				if ((src_t == IXCBpre) || (src_t == IYCBpre)) { BUSRQ[0] = PCh; }

				break;
			case ASGN:
				ASGN_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1]);
				instr_pntr += 2;
				break;
			case ADDS:
				ADDS_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2], cur_instr[instr_pntr + 3]);
				instr_pntr += 4;
				break;
			case EI_RETI:
				// NOTE: This is needed for systems using multiple interrupt sources, it triggers the next interrupt
				// Not currently implemented here
				IFF1 = IFF2;
				break;
			case EI_RETN:
				IFF1 = IFF2;
				break;
			case OUT:
				OUT_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2]);
				instr_pntr += 3;
				break;
			case OUT_INC:
				OUT_INC_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2]);
				instr_pntr += 3;
				break;
			case IN:
				IN_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2]);
				instr_pntr += 3;
				break;
			case IN_INC:
				IN_INC_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2]);
				instr_pntr += 3;
				break;
			case IN_A_N_INC:
				IN_A_N_INC_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2]);
				instr_pntr += 3;
				break;
			case NEG:
				NEG_8_Func(cur_instr[instr_pntr]);
				instr_pntr += 1;
				break;
			case INT_MODE:
				interruptMode = cur_instr[instr_pntr];
				instr_pntr += 1;
				break;
			case RRD:
				RRD_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1]);
				instr_pntr += 2;
				break;
			case RLD:
				RLD_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1]);
				instr_pntr += 2;
				break;
			case SET_FL_LD_R:
				DEC16_Func(C, B);
				SET_FL_LD_Func();

				Ztemp1 = cur_instr[instr_pntr++];
				Ztemp2 = cur_instr[instr_pntr++];
				Ztemp3 = cur_instr[instr_pntr++];

				if (((Regs[C] | (Regs[B] << 8)) != 0) && (Ztemp3 > 0))
				{
					PopulateCURINSTR(DEC16, PCl, PCh,
									DEC16, PCl, PCh,
									TR16, Z, W, PCl, PCh,
									INC16, Z, W,
									Ztemp2, E, D);

					PopulateBUSRQ(D, D, D, D, D);
					PopulateMEMRQ(0, 0, 0, 0, 0);
					IRQS = 5;

					instr_pntr = mem_pntr = bus_pntr = irq_pntr = 0;
					I_skip = true;
				}
				else
				{
					if (Ztemp2 == INC16) { INC16_Func(E, D); }
					else { DEC16_Func(E, D); }
				}
				break;
			case SET_FL_CP_R:
				SET_FL_CP_Func();

				Ztemp1 = cur_instr[instr_pntr++];
				Ztemp2 = cur_instr[instr_pntr++];
				Ztemp3 = cur_instr[instr_pntr++];

				if (((Regs[C] | (Regs[B] << 8)) != 0) && (Ztemp3 > 0) && !FlagZget())
				{

					PopulateCURINSTR(DEC16, PCl, PCh,
									DEC16, PCl, PCh,
									TR16, Z, W, PCl, PCh,
									INC16, Z, W,
									Ztemp2, L, H);

					PopulateBUSRQ(H, H, H, H, H);
					PopulateMEMRQ(0, 0, 0, 0, 0);
					IRQS = 5;

					instr_pntr = mem_pntr = bus_pntr = irq_pntr = 0;
					I_skip = true;
				}
				else
				{
					if (Ztemp2 == INC16) { INC16_Func(L, H); }
					else { DEC16_Func(L, H); }
				}
				break;
			case SET_FL_IR:
				dest_t = cur_instr[instr_pntr++];
				TR_Func(dest_t, cur_instr[instr_pntr++]);
				SET_FL_IR_Func(dest_t);
				break;
			case FTCH_DB:
				FTCH_DB_Func();
				break;
			case WAIT:
				if (FlagW)
				{
					instr_pntr--; bus_pntr--; mem_pntr--;
					I_skip = true;
				}
				break;
			case RST:
				Regs[Z] = cur_instr[instr_pntr++];
				Regs[W] = 0;
				break;
			case REP_OP_I:
				Write_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2]);
				instr_pntr += 3;

				Ztemp4 = cur_instr[instr_pntr++];
				if (Ztemp4 == DEC16)
				{
					TR16_Func(Z, W, C, B);
					DEC16_Func(Z, W);
					DEC8_Func(B);

					// take care of other flags
					// taken from 'undocumented z80 documented' and Fuse
					FlagNset((Regs[ALU] & 0x80) > 0);
					FlagHset(((Regs[ALU] + Regs[C] - 1) & 0xFF) < Regs[ALU]);
					FlagCset(((Regs[ALU] + Regs[C] - 1) & 0xFF) < Regs[ALU]);
					FlagPset(TableParity[((Regs[ALU] + Regs[C] - 1) & 7) ^ Regs[B]]);
				}
				else
				{
					TR16_Func(Z, W, C, B);
					INC16_Func(Z, W);
					DEC8_Func(B);

					// take care of other flags
					// taken from 'undocumented z80 documented' and Fuse
					FlagNset((Regs[ALU] & 0x80) > 0);
					FlagHset(((Regs[ALU] + Regs[C] + 1) & 0xFF) < Regs[ALU]);
					FlagCset(((Regs[ALU] + Regs[C] + 1) & 0xFF) < Regs[ALU]);
					FlagPset(TableParity[((Regs[ALU] + Regs[C] + 1) & 7) ^ Regs[B]]);
				}

				Ztemp1 = cur_instr[instr_pntr++];
				Ztemp2 = cur_instr[instr_pntr++];
				Ztemp3 = cur_instr[instr_pntr++];

				if ((Regs[B] != 0) && (Ztemp3 > 0))
				{
					PopulateCURINSTR(IDLE,
									IDLE,
									DEC16, PCl, PCh,
									DEC16, PCl, PCh,
									Ztemp2, L, H);

					PopulateBUSRQ(H, H, H, H, H);
					PopulateMEMRQ(0, 0, 0, 0, 0);
					IRQS = 5;

					instr_pntr = mem_pntr = bus_pntr = irq_pntr = 0;
					I_skip = true;
				}
				else
				{
					if (Ztemp2 == INC16) { INC16_Func(L, H); }
					else { DEC16_Func(L, H); }
				}
				break;
			case REP_OP_O:
				OUT_Func(cur_instr[instr_pntr], cur_instr[instr_pntr + 1], cur_instr[instr_pntr + 2]);
				instr_pntr += 3;

				Ztemp4 = cur_instr[instr_pntr++];
				if (Ztemp4 == DEC16)
				{
					DEC16_Func(L, H);
					DEC8_Func(B);
					TR16_Func(Z, W, C, B);
					DEC16_Func(Z, W);
				}
				else
				{
					INC16_Func(L, H);
					DEC8_Func(B);
					TR16_Func(Z, W, C, B);
					INC16_Func(Z, W);
				}

				// take care of other flags
				// taken from 'undocumented z80 documented'
				FlagNset((Regs[ALU] & 0x80) > 0);
				FlagHset((Regs[ALU] + Regs[L]) > 0xFF);
				FlagCset((Regs[ALU] + Regs[L]) > 0xFF);
				FlagPset(TableParity[((Regs[ALU] + Regs[L]) & 7) ^ (Regs[B])]);

				Ztemp1 = cur_instr[instr_pntr++];
				Ztemp2 = cur_instr[instr_pntr++];
				Ztemp3 = cur_instr[instr_pntr++];

				if ((Regs[B] != 0) && (Ztemp3 > 0))
				{
					PopulateCURINSTR
					(IDLE,
						IDLE,
						DEC16, PCl, PCh,
						DEC16, PCl, PCh,
						IDLE);

					PopulateBUSRQ(B, B, B, B, B);
					PopulateMEMRQ(0, 0, 0, 0, 0);
					IRQS = 5;

					instr_pntr = mem_pntr = bus_pntr = irq_pntr = 0;
					I_skip = true;
				}
				break;

			case IORQ:
				//IRQACKCallback();
				break;
			}

			if (I_skip)
			{
				I_skip = false;
			}
			else if (++irq_pntr == IRQS)
			{
				if (EI_pending > 0)
				{
					EI_pending--;
					if (EI_pending == 0) { IFF1 = IFF2 = true; }
				}

				// NMI has priority
				if (nonMaskableInterruptPending)
				{
					nonMaskableInterruptPending = false;

					if (TraceCallback) { TraceCallback(1); }

					IFF2 = IFF1;
					IFF1 = false;
					NMI_();
					//NMICallback();
					instr_pntr = mem_pntr = bus_pntr = irq_pntr = 0;

					temp_R = (Regs[R] & 0x7F);
					temp_R++;
					temp_R &= 0x7F;
					Regs[R] = ((Regs[R] & 0x80) | temp_R);

					halted = false;
				}
				// if we are processing an interrrupt, we need to modify the instruction vector
				else if (IFF1 && FlagI)
				{
					IFF1 = IFF2 = false;
					EI_pending = 0;

					if (TraceCallback) { TraceCallback(2); }

					switch (interruptMode)
					{
					case 0:
						// Requires something to be pushed onto the data bus
						// we'll assume it's a zero for now
						INTERRUPT_0(0);
						break;
					case 1:
						INTERRUPT_1();
						break;
					case 2:
						INTERRUPT_2();
						break;
					}
					//IRQCallback();
					instr_pntr = mem_pntr = bus_pntr = irq_pntr = 0;

					temp_R = (Regs[R] & 0x7F);
					temp_R++;
					temp_R &= 0x7F;
					Regs[R] = ((Regs[R] & 0x80) | temp_R);

					halted = false;
				}
				// otherwise start a new normal access
				else if (!halted)
				{
					PopulateCURINSTR
					(IDLE,
						WAIT,
						OP_F,
						OP);

					PopulateBUSRQ(PCh, 0, 0, 0);
					PopulateMEMRQ(PCh, 0, 0, 0);
					IRQS = 4;

					instr_pntr = mem_pntr = bus_pntr = irq_pntr = 0;
				}
				else
				{
					instr_pntr = mem_pntr = bus_pntr = irq_pntr = 0;
				}
			}

			TotalExecutedCycles++;
		}

		/// <summary>
		/// Optimization method to set BUSRQ
		/// </summary>		
		void PopulateBUSRQ(uint32_t d0 = 0, uint32_t d1 = 0, uint32_t d2 = 0, uint32_t d3 = 0, uint32_t d4 = 0, uint32_t d5 = 0, uint32_t d6 = 0, uint32_t d7 = 0, uint32_t d8 = 0,
			uint32_t d9 = 0, uint32_t d10 = 0, uint32_t d11 = 0, uint32_t d12 = 0, uint32_t d13 = 0, uint32_t d14 = 0, uint32_t d15 = 0, uint32_t d16 = 0, uint32_t d17 = 0, uint32_t d18 = 0)
		{
			BUSRQ[0] = d0; BUSRQ[1] = d1; BUSRQ[2] = d2;
			BUSRQ[3] = d3; BUSRQ[4] = d4; BUSRQ[5] = d5;
			BUSRQ[6] = d6; BUSRQ[7] = d7; BUSRQ[8] = d8;
			BUSRQ[9] = d9; BUSRQ[10] = d10; BUSRQ[11] = d11;
			BUSRQ[12] = d12; BUSRQ[13] = d13; BUSRQ[14] = d14;
			BUSRQ[15] = d15; BUSRQ[16] = d16; BUSRQ[17] = d17;
			BUSRQ[18] = d18;
		}

		/// <summary>
		/// Optimization method to set MEMRQ
		/// </summary>	
		void PopulateMEMRQ(uint32_t d0 = 0, uint32_t d1 = 0, uint32_t d2 = 0, uint32_t d3 = 0, uint32_t d4 = 0, uint32_t d5 = 0, uint32_t d6 = 0, uint32_t d7 = 0, uint32_t d8 = 0,
			uint32_t d9 = 0, uint32_t d10 = 0, uint32_t d11 = 0, uint32_t d12 = 0, uint32_t d13 = 0, uint32_t d14 = 0, uint32_t d15 = 0, uint32_t d16 = 0, uint32_t d17 = 0, uint32_t d18 = 0)
		{
			MEMRQ[0] = d0; MEMRQ[1] = d1; MEMRQ[2] = d2;
			MEMRQ[3] = d3; MEMRQ[4] = d4; MEMRQ[5] = d5;
			MEMRQ[6] = d6; MEMRQ[7] = d7; MEMRQ[8] = d8;
			MEMRQ[9] = d9; MEMRQ[10] = d10; MEMRQ[11] = d11;
			MEMRQ[12] = d12; MEMRQ[13] = d13; MEMRQ[14] = d14;
			MEMRQ[15] = d15; MEMRQ[16] = d16; MEMRQ[17] = d17;
			MEMRQ[18] = d18;
		}

		/// <summary>
		/// Optimization method to set cur_instr
		/// </summary>	
		void PopulateCURINSTR(uint32_t d0 = 0, uint32_t d1 = 0, uint32_t d2 = 0, uint32_t d3 = 0, uint32_t d4 = 0, uint32_t d5 = 0, uint32_t d6 = 0, uint32_t d7 = 0, uint32_t d8 = 0,
			uint32_t d9 = 0, uint32_t d10 = 0, uint32_t d11 = 0, uint32_t d12 = 0, uint32_t d13 = 0, uint32_t d14 = 0, uint32_t d15 = 0, uint32_t d16 = 0, uint32_t d17 = 0, uint32_t d18 = 0,
			uint32_t d19 = 0, uint32_t d20 = 0, uint32_t d21 = 0, uint32_t d22 = 0, uint32_t d23 = 0, uint32_t d24 = 0, uint32_t d25 = 0, uint32_t d26 = 0, uint32_t d27 = 0, uint32_t d28 = 0,
			uint32_t d29 = 0, uint32_t d30 = 0, uint32_t d31 = 0, uint32_t d32 = 0, uint32_t d33 = 0, uint32_t d34 = 0, uint32_t d35 = 0, uint32_t d36 = 0, uint32_t d37 = 0)
		{
			cur_instr[0] = d0; cur_instr[1] = d1; cur_instr[2] = d2;
			cur_instr[3] = d3; cur_instr[4] = d4; cur_instr[5] = d5;
			cur_instr[6] = d6; cur_instr[7] = d7; cur_instr[8] = d8;
			cur_instr[9] = d9; cur_instr[10] = d10; cur_instr[11] = d11;
			cur_instr[12] = d12; cur_instr[13] = d13; cur_instr[14] = d14;
			cur_instr[15] = d15; cur_instr[16] = d16; cur_instr[17] = d17;
			cur_instr[18] = d18; cur_instr[19] = d19; cur_instr[20] = d20;
			cur_instr[21] = d21; cur_instr[22] = d22; cur_instr[23] = d23;
			cur_instr[24] = d24; cur_instr[25] = d25; cur_instr[26] = d26;
			cur_instr[27] = d27; cur_instr[28] = d28; cur_instr[29] = d29;
			cur_instr[30] = d30; cur_instr[31] = d31; cur_instr[32] = d32;
			cur_instr[33] = d33; cur_instr[34] = d34; cur_instr[35] = d35;
			cur_instr[36] = d36; cur_instr[37] = d37;
		}
		/*
		// State Save/Load
		void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(Z80A));
			ser.Sync(nameof(Regs), ref Regs, false);
			ser.Sync("NMI", ref nonMaskableInterrupt);
			ser.Sync("NMIPending", ref nonMaskableInterruptPending);
			ser.Sync("IM", ref interruptMode);
			ser.Sync("IFF1", ref iff1);
			ser.Sync("IFF2", ref iff2);
			ser.Sync("Halted", ref halted);
			ser.Sync(nameof(I_skip), ref I_skip);
			ser.Sync("ExecutedCycles", ref TotalExecutedCycles);
			ser.Sync(nameof(EI_pending), ref EI_pending);

			ser.Sync(nameof(instr_pntr), ref instr_pntr);
			ser.Sync(nameof(bus_pntr), ref bus_pntr);
			ser.Sync(nameof(mem_pntr), ref mem_pntr);
			ser.Sync(nameof(irq_pntr), ref irq_pntr);
			ser.Sync(nameof(cur_instr), ref cur_instr, false);
			ser.Sync(nameof(BUSRQ), ref BUSRQ, false);
			ser.Sync(nameof(IRQS), ref IRQS);
			ser.Sync(nameof(MEMRQ), ref MEMRQ, false);
			ser.Sync(nameof(opcode), ref opcode);
			ser.Sync(nameof(FlagI), ref FlagI);
			ser.Sync(nameof(FlagW), ref FlagW);

			ser.Sync(nameof(NO_prefix), ref NO_prefix);
			ser.Sync(nameof(CB_prefix), ref CB_prefix);
			ser.Sync(nameof(IX_prefix), ref IX_prefix);
			ser.Sync(nameof(IY_prefix), ref IY_prefix);
			ser.Sync(nameof(IXCB_prefix), ref IXCB_prefix);
			ser.Sync(nameof(IYCB_prefix), ref IYCB_prefix);
			ser.Sync(nameof(EXTD_prefix), ref EXTD_prefix);
			ser.Sync(nameof(PRE_SRC), ref PRE_SRC);

			ser.EndSection();
		}
		*/

		void InitTableParity()
		{
			for (uint32_t i = 0; i < 256; ++i)
			{
				uint32_t Bits = 0;
				for (uint32_t j = 0; j < 8; ++j)
				{
					Bits += (i >> j) & 1;
				}
				TableParity[i] = (Bits & 1) == 0;
			}
		}

		void ResetRegisters()
		{
			for (uint32_t i = 0; i < 36; i++)
			{
				Regs[i] = 0;
			}

			// the IRQ1 vector is 0x38
			Regs[IRQ_V] = 0x38;
			// The NMI vector is constant 0x66
			Regs[NMI_V] = 0x66;

			FlagI = false;
			FlagW = false;
		}

		#pragma endregion

		#pragma region Interrupts
		void NMI_()
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							IDLE,
							DEC16, SPl, SPh,
							TR, ALU, PCl,
							WAIT,
							WR_DEC, SPl, SPh, PCh,
							TR16, PCl, PCh, NMI_V, ZERO,
							WAIT,
							WR, SPl, SPh, ALU);

			PopulateBUSRQ(0, 0, 0, 0, 0, SPh, 0, 0, SPh, 0, 0);
			PopulateMEMRQ(0, 0, 0, 0, 0, SPh, 0, 0, SPh, 0, 0);
			IRQS = 11;
		}

		// Mode 0 interrupts only take effect if a CALL or RST is on the data bus
		// Otherwise operation just continues as normal
		// For now assume a NOP is on the data bus, in which case no stack operations occur

		//NOTE: TODO: When a CALL is present on the data bus, adjust WZ accordingly 
		void INTERRUPT_0(uint32_t src)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IORQ,
							WAIT,
							IDLE,
							WAIT,
							RD_INC, ALU, PCl, PCh);

			PopulateBUSRQ(0, 0, 0, 0, PCh, 0, 0);
			PopulateMEMRQ(0, 0, 0, 0, PCh, 0, 0);
			IRQS = 7;
		}

		// Just jump to $0038
		void INTERRUPT_1()
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IORQ,
							WAIT,
							IDLE,
							TR, ALU, PCl,
							DEC16, SPl, SPh,
							IDLE,
							WAIT,
							WR_DEC, SPl, SPh, PCh,
							TR16, PCl, PCh, IRQ_V, ZERO,
							WAIT,
							WR, SPl, SPh, ALU);

			PopulateBUSRQ(0, 0, 0, 0, I, 0, 0, SPh, 0, 0, SPh, 0, 0);
			PopulateMEMRQ(0, 0, 0, 0, I, 0, 0, SPh, 0, 0, SPh, 0, 0);
			IRQS = 13;
		}

		// Interrupt mode 2 uses the I vector combined with a byte on the data bus
		void INTERRUPT_2()
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IORQ,
							WAIT,
							FTCH_DB,
							IDLE,
							DEC16, SPl, SPh,
							TR16, Z, W, DB, I,
							WAIT,
							WR_DEC, SPl, SPh, PCh,
							IDLE,
							WAIT,
							WR, SPl, SPh, PCl,
							IDLE,
							WAIT,
							RD_INC, PCl, Z, W,
							IDLE,
							WAIT,
							RD, PCh, Z, W);

			PopulateBUSRQ(0, 0, 0, 0, I, 0, 0, SPh, 0, 0, SPh, 0, 0, W, 0, 0, W, 0, 0);
			PopulateMEMRQ(0, 0, 0, 0, I, 0, 0, SPh, 0, 0, SPh, 0, 0, W, 0, 0, W, 0, 0);
			IRQS = 19;
		}

		void ResetInterrupts()
		{
			IFF1 = false;
			IFF2 = false;
			nonMaskableInterrupt = false;
			nonMaskableInterruptPending = false;
			FlagI = false;
			InterruptModeset(1);
		}
		#pragma endregion

		#pragma region Indirect Ops

		// this contains the vectors of instrcution operations
		// NOTE: This list is NOT confirmed accurate for each individual cycle

		void INT_OP_IND(uint32_t operation, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD, ALU, src_l, src_h,
							IDLE,
							operation, ALU,
							WAIT,
							WR, src_l, src_h, ALU);

			PopulateBUSRQ(0, src_h, 0, 0, src_h, src_h, 0, 0);
			PopulateMEMRQ(0, src_h, 0, 0, 0, src_h, 0, 0);
			IRQS = 8;
		};

		void BIT_OP_IND(uint32_t operation, uint32_t bit, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD, ALU, src_l, src_h,
							operation, bit, ALU,
							IDLE,
							WAIT,
							WR, src_l, src_h, ALU);

			PopulateBUSRQ(0, src_h, 0, 0, src_h, src_h, 0, 0);
			PopulateMEMRQ(0, src_h, 0, 0, 0, src_h, 0, 0);
			IRQS = 8;
		};

		// Note that this operation uses I_BIT, same as indexed BIT.
		// This is where the strange behaviour in Flag bits 3 and 5 come from.
		// normally WZ contain I* + n when doing I_BIT ops, but here we use that code path 
		// even though WZ is not assigned to, letting it's value from other operations show through
		void BIT_TE_IND(uint32_t operation, uint32_t bit, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD, ALU, src_l, src_h,
							I_BIT, bit, ALU);

			PopulateBUSRQ(0, src_h, 0, 0, src_h);
			PopulateMEMRQ(0, src_h, 0, 0, 0);
			IRQS = 5;
		};

		void REG_OP_IND_INC(uint32_t operation, uint32_t dest, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD_OP, 1, ALU, src_l, src_h, operation, dest, ALU);

			PopulateBUSRQ(0, src_h, 0, 0);
			PopulateMEMRQ(0, src_h, 0, 0);
			IRQS = 4;
		};

		void REG_OP_IND(uint32_t operation, uint32_t dest, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(IDLE,
							TR16, Z, W, src_l, src_h,
							WAIT,
							RD_OP, 1, ALU, Z, W, operation, dest, ALU);

			PopulateBUSRQ(0, src_h, 0, 0);
			PopulateMEMRQ(0, src_h, 0, 0);
			IRQS = 4;
		};

		// different because HL doesn't effect WZ
		void REG_OP_IND_HL(uint32_t operation, uint32_t dest)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD_OP, 0, ALU, L, H, operation, dest, ALU);

			PopulateBUSRQ(0, H, 0, 0);
			PopulateMEMRQ(0, H, 0, 0);
			IRQS = 4;
		};

		void LD_16_IND_nn(uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD_INC, Z, PCl, PCh,
							IDLE,
							WAIT,
							RD_INC, W, PCl, PCh,
							IDLE,
							WAIT,
							WR_INC, Z, W, src_l,
							IDLE,
							WAIT,
							WR, Z, W, src_h);

			PopulateBUSRQ(0, PCh, 0, 0, PCh, 0, 0, W, 0, 0, W, 0, 0);
			PopulateMEMRQ(0, PCh, 0, 0, PCh, 0, 0, W, 0, 0, W, 0, 0);
			IRQS = 13;
		};

		void LD_IND_16_nn(uint32_t dest_l, uint32_t dest_h)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD_INC, Z, PCl, PCh,
							IDLE,
							WAIT,
							RD_INC, W, PCl, PCh,
							IDLE,
							WAIT,
							RD_INC, dest_l, Z, W,
							IDLE,
							WAIT,
							RD, dest_h, Z, W);

			PopulateBUSRQ(0, PCh, 0, 0, PCh, 0, 0, W, 0, 0, W, 0, 0);
			PopulateMEMRQ(0, PCh, 0, 0, PCh, 0, 0, W, 0, 0, W, 0, 0);
			IRQS = 13;
		};

		void LD_8_IND_nn(uint32_t src)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD_INC, Z, PCl, PCh,
							IDLE,
							WAIT,
							RD_INC, W, PCl, PCh,
							IDLE,
							WAIT,
							WR_INC_WA, Z, W, src);

			PopulateBUSRQ(0, PCh, 0, 0, PCh, 0, 0, W, 0, 0);
			PopulateMEMRQ(0, PCh, 0, 0, PCh, 0, 0, W, 0, 0);
			IRQS = 10;
		};

		void LD_IND_8_nn(uint32_t dest)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD_INC, Z, PCl, PCh,
							IDLE,
							WAIT,
							RD_INC, W, PCl, PCh,
							IDLE,
							WAIT,
							RD_INC, dest, Z, W);

			PopulateBUSRQ(0, PCh, 0, 0, PCh, 0, 0, W, 0, 0);
			PopulateMEMRQ(0, PCh, 0, 0, PCh, 0, 0, W, 0, 0);
			IRQS = 10;
		};

		void LD_8_IND(uint32_t dest_l, uint32_t dest_h, uint32_t src)
		{
			PopulateCURINSTR(IDLE,
							TR16, Z, W, dest_l, dest_h,
							WAIT,
							WR_INC_WA, Z, W, src);

			PopulateBUSRQ(0, dest_h, 0, 0);
			PopulateMEMRQ(0, dest_h, 0, 0);
			IRQS = 4;
		};

		// seperate HL needed since it doesn't effect the WZ pair
		void LD_8_IND_HL(uint32_t src)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							WR, L, H, src);

			PopulateBUSRQ(0, H, 0, 0);
			PopulateMEMRQ(0, H, 0, 0);
			IRQS = 4;
		};

		void LD_8_IND_IND(uint32_t dest_l, uint32_t dest_h, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD_INC, ALU, src_l, src_h,
							IDLE,
							WAIT,
							WR, dest_l, dest_h, ALU);

			PopulateBUSRQ(0, src_h, 0, 0, dest_h, 0, 0);
			PopulateMEMRQ(0, src_h, 0, 0, dest_h, 0, 0);
			IRQS = 7;
		};

		void LD_IND_8_INC(uint32_t dest, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD_INC, dest, src_l, src_h);

			PopulateBUSRQ(0, src_h, 0, 0);
			PopulateMEMRQ(0, src_h, 0, 0);
			IRQS = 4;
		};

		void LD_IND_16(uint32_t dest_l, uint32_t dest_h, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD_INC, dest_l, src_l, src_h,
							IDLE,
							WAIT,
							RD_INC, dest_h, src_l, src_h);

			PopulateBUSRQ(0, src_h, 0, 0, src_h, 0, 0);
			PopulateMEMRQ(0, src_h, 0, 0, src_h, 0, 0);
			IRQS = 7;
		};

		void INC_8_IND(uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD, ALU, src_l, src_h,
							INC8, ALU,
							IDLE,
							WAIT,
							WR, src_l, src_h, ALU);

			PopulateBUSRQ(0, src_h, 0, 0, src_h, src_h, 0, 0);
			PopulateMEMRQ(0, src_h, 0, 0, 0, src_h, 0, 0);
			IRQS = 8;
		};

		void DEC_8_IND(uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD, ALU, src_l, src_h,
							DEC8, ALU,
							IDLE,
							WAIT,
							WR, src_l, src_h, ALU);

			PopulateBUSRQ(0, src_h, 0, 0, src_h, src_h, 0, 0);
			PopulateMEMRQ(0, src_h, 0, 0, 0, src_h, 0, 0);
			IRQS = 8;
		};

		// NOTE: WZ implied for the wollowing 3 functions
		void I_INT_OP(uint32_t operation, uint32_t dest)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD, ALU, Z, W,
							operation, ALU,
							TR, dest, ALU,
							WAIT,
							WR, Z, W, ALU);

			PopulateBUSRQ(0, W, 0, 0, W, W, 0, 0);
			PopulateMEMRQ(0, W, 0, 0, 0, W, 0, 0);
			IRQS = 8;
		};

		void I_BIT_OP(uint32_t operation, uint32_t bit, uint32_t dest)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD, ALU, Z, W,
							operation, bit, ALU,
							TR, dest, ALU,
							WAIT,
							WR, Z, W, ALU);

			PopulateBUSRQ(0, W, 0, 0, W, W, 0, 0);
			PopulateMEMRQ(0, W, 0, 0, 0, W, 0, 0);
			IRQS = 8;
		};

		void I_BIT_TE(uint32_t bit)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD, ALU, Z, W,
							I_BIT, bit, ALU);

			PopulateBUSRQ(0, W, 0, 0, W);
			PopulateMEMRQ(0, W, 0, 0, 0);
			IRQS = 5;
		};

		void I_OP_n(uint32_t operation, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD, ALU, PCl, PCh,
							IDLE,
							IDLE,
							TR16, Z, W, src_l, src_h,
							ADDS, Z, W, ALU, ZERO,
							IDLE,
							INC16, PCl, PCh,
							WAIT,
							RD, ALU, Z, W,
							operation, ALU,
							IDLE,
							WAIT,
							WR, Z, W, ALU);

			PopulateBUSRQ(0, PCh, 0, 0, PCh, PCh, PCh, PCh, PCh, W, 0, 0, W, W, 0, 0);
			PopulateMEMRQ(0, PCh, 0, 0, 0, 0, 0, 0, 0, W, 0, 0, 0, W, 0, 0);
			IRQS = 16;
		};

		void I_OP_n_n(uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(IDLE,
							TR16, Z, W, src_l, src_h,
							WAIT,
							RD_INC, ALU, PCl, PCh,
							ADDS, Z, W, ALU, ZERO,
							WAIT,
							RD, ALU, PCl, PCh,
							IDLE,
							IDLE,
							INC16, PCl, PCh,
							WAIT,
							WR, Z, W, ALU);

			PopulateBUSRQ(0, PCh, 0, 0, PCh, 0, 0, PCh, PCh, W, 0, 0);
			PopulateMEMRQ(0, PCh, 0, 0, PCh, 0, 0, 0, 0, W, 0, 0);
			IRQS = 12;
		};

		void I_REG_OP_IND_n(uint32_t operation, uint32_t dest, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD, ALU, PCl, PCh,
							IDLE,
							TR16, Z, W, src_l, src_h,
							IDLE,
							ADDS, Z, W, ALU, ZERO,
							IDLE,
							INC16, PCl, PCh,
							WAIT,
							RD_OP, 0, ALU, Z, W, operation, dest, ALU);

			PopulateBUSRQ(0, PCh, 0, 0, PCh, PCh, PCh, PCh, PCh, W, 0, 0);
			PopulateMEMRQ(0, PCh, 0, 0, 0, 0, 0, 0, 0, W, 0, 0);
			IRQS = 12;
		};

		void I_LD_8_IND_n(uint32_t dest_l, uint32_t dest_h, uint32_t src)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD, ALU, PCl, PCh,
							IDLE,
							IDLE,
							TR16, Z, W, dest_l, dest_h,
							ADDS, Z, W, ALU, ZERO,
							IDLE,
							INC16, PCl, PCh,
							WAIT,
							WR, Z, W, src);

			PopulateBUSRQ(0, PCh, 0, 0, PCh, PCh, PCh, PCh, PCh, Z, 0, 0);
			PopulateMEMRQ(0, PCh, 0, 0, 0, 0, 0, 0, 0, Z, 0, 0);
			IRQS = 12;
		};

		void LD_OP_R(uint32_t operation, uint32_t repeat_instr)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD, ALU, L, H,
							operation, L, H,
							WAIT,
							WR, E, D, ALU,
							IDLE,
							SET_FL_LD_R, 0, operation, repeat_instr);

			PopulateBUSRQ(0, H, 0, 0, D, 0, 0, D, D);
			PopulateMEMRQ(0, H, 0, 0, D, 0, 0, 0, 0);
			IRQS = 9;
		};

		void CP_OP_R(uint32_t operation, uint32_t repeat_instr)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD, ALU, L, H,
							IDLE,
							DEC16, C, B,
							operation, Z, W,
							IDLE,
							SET_FL_CP_R, 1, operation, repeat_instr);

			PopulateBUSRQ(0, H, 0, 0, H, H, H, H, H);
			PopulateMEMRQ(0, H, 0, 0, 0, 0, 0, 0, 0);
			IRQS = 9;
		};

		void IN_OP_R(uint32_t operation, uint32_t repeat_instr)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							WAIT,
							WAIT,
							IN, ALU, C, B,
							IDLE,
							WAIT,
							REP_OP_I, L, H, ALU, operation, 2, operation, repeat_instr);

			PopulateBUSRQ(0, I, BIO1, BIO2, BIO3, BIO4, H, 0, 0);
			PopulateMEMRQ(0, 0, BIO1, BIO2, BIO3, BIO4, H, 0, 0);
			IRQS = 9;
		};

		void OUT_OP_R(uint32_t operation, uint32_t repeat_instr)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							WAIT,
							RD, ALU, L, H,
							IDLE,
							WAIT,
							WAIT,
							REP_OP_O, C, B, ALU, operation, 3, operation, repeat_instr);

			PopulateBUSRQ(0, I, H, 0, 0, BIO1, BIO2, BIO3, BIO4);
			PopulateMEMRQ(0, 0, H, 0, 0, BIO1, BIO2, BIO3, BIO4);
			IRQS = 9;
		};

		// this is an indirect change of a a 16 bit register with memory
		void EXCH_16_IND_(uint32_t dest_l, uint32_t dest_h, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD_INC, Z, dest_l, dest_h,
							IDLE,
							WAIT,
							RD, W, dest_l, dest_h,
							IDLE,
							IDLE,
							WAIT,
							WR_DEC, dest_l, dest_h, src_h,
							IDLE,
							WAIT,
							WR, dest_l, dest_h, src_l,
							IDLE,
							TR16, src_l, src_h, Z, W);

			PopulateBUSRQ(0, dest_h, 0, 0, dest_h, 0, 0, dest_h, dest_h, 0, 0, dest_h, 0, 0, dest_h, dest_h);
			PopulateMEMRQ(0, dest_h, 0, 0, dest_h, 0, 0, 0, dest_h, 0, 0, dest_h, 0, 0, 0, 0);
			IRQS = 16;
		};
		#pragma endregion

		#pragma region Direct Ops

		void NOP_()
		{
			PopulateCURINSTR(IDLE);

			PopulateBUSRQ(0);
			PopulateMEMRQ(0);
			IRQS = 1;
		};

		// NOTE: In a real Z80, this operation just flips a switch to choose between 2 registers
		// but it's simpler to emulate just by exchanging the register with it's shadow
		void EXCH_()
		{
			PopulateCURINSTR(EXCH);

			PopulateBUSRQ(0);
			PopulateMEMRQ(0);
			IRQS = 1;
		};

		void EXX_()
		{
			PopulateCURINSTR(EXX);

			PopulateBUSRQ(0);
			PopulateMEMRQ(0);
			IRQS = 1;
		};

		// this exchanges 2 16 bit registers
		void EXCH_16_(uint32_t dest_l, uint32_t dest_h, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(EXCH_16, dest_l, dest_h, src_l, src_h);

			PopulateBUSRQ(0);
			PopulateMEMRQ(0);
			IRQS = 1;
		};

		void INC_16(uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(INC16, src_l, src_h,
							IDLE,
							IDLE);

			PopulateBUSRQ(0, I, I);
			PopulateMEMRQ(0, 0, 0);
			IRQS = 3;
		};


		void DEC_16(uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(DEC16, src_l, src_h,
							IDLE,
							IDLE);

			PopulateBUSRQ(0, I, I);
			PopulateMEMRQ(0, 0, 0);
			IRQS = 3;
		};

		// this is done in two steps technically, but the flags don't work out using existing funcitons
		// so let's use a different function since it's an internal operation anyway
		void ADD_16(uint32_t dest_l, uint32_t dest_h, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(IDLE,
							TR16, Z, W, dest_l, dest_h,
							IDLE,
							INC16, Z, W,
							IDLE,
							ADD16, dest_l, dest_h, src_l, src_h,
							IDLE,
							IDLE);

			PopulateBUSRQ(0, I, I, I, I, I, I, I);
			PopulateMEMRQ(0, 0, 0, 0, 0, 0, 0, 0);
			IRQS = 8;
		};

		void REG_OP(uint32_t operation, uint32_t dest, uint32_t src)
		{
			PopulateCURINSTR(operation, dest, src);

			PopulateBUSRQ(0);
			PopulateMEMRQ(0);
			IRQS = 1;
		};

		// Operations using the I and R registers take one T-cycle longer
		void REG_OP_IR(uint32_t operation, uint32_t dest, uint32_t src)
		{
			PopulateCURINSTR(IDLE,
							SET_FL_IR, dest, src);

			PopulateBUSRQ(0, I);
			PopulateMEMRQ(0, 0);
			IRQS = 2;
		};

		// note: do not use DEC here since no flags are affected by this operation
		void DJNZ_()
		{
			if ((Regs[B] - 1) != 0)
			{
				PopulateCURINSTR(IDLE,
								IDLE,
								ASGN, B, (uint32_t)((Regs[B] - 1) & 0xFF),
								WAIT,
								RD_INC, Z, PCl, PCh,
								IDLE,
								IDLE,
								ASGN, W, 0,
								ADDS, PCl, PCh, Z, W,
								TR16, Z, W, PCl, PCh);

				PopulateBUSRQ(0, I, PCh, 0, 0, PCh, PCh, PCh, PCh, PCh);
				PopulateMEMRQ(0, 0, PCh, 0, 0, 0, 0, 0, 0, 0);
				IRQS = 10;
			}
			else
			{
				PopulateCURINSTR(IDLE,
								IDLE,
								ASGN, B, (uint32_t)((Regs[B] - 1) & 0xFF),
								WAIT,
								RD_INC, ALU, PCl, PCh);

				PopulateBUSRQ(0, I, PCh, 0, 0);
				PopulateMEMRQ(0, 0, PCh, 0, 0);
				IRQS = 5;
			};
		};

		void HALT_()
		{
			PopulateCURINSTR(HALT);

			PopulateBUSRQ(0);
			PopulateMEMRQ(0);
			IRQS = 1;
		};

		void JR_COND(bool cond)
		{
			if (cond)
			{
				PopulateCURINSTR(IDLE,
								IDLE,
								WAIT,
								RD_INC, Z, PCl, PCh,
								IDLE,
								ASGN, W, 0,
								IDLE,
								ADDS, PCl, PCh, Z, W,
								TR16, Z, W, PCl, PCh);

				PopulateBUSRQ(0, PCh, 0, 0, PCh, PCh, PCh, PCh, PCh);
				PopulateMEMRQ(0, PCh, 0, 0, 0, 0, 0, 0, 0);
				IRQS = 9;
			}
			else
			{
				PopulateCURINSTR
				(IDLE,
					IDLE,
					WAIT,
					RD_INC, ALU, PCl, PCh);

				PopulateBUSRQ(0, PCh, 0, 0);
				PopulateMEMRQ(0, PCh, 0, 0);
				IRQS = 4;
			};
		};

		void JP_COND(bool cond)
		{
			if (cond)
			{
				PopulateCURINSTR(IDLE,
								IDLE,
								WAIT,
								RD_INC, Z, PCl, PCh,
								IDLE,
								WAIT,
								RD_INC_TR_PC, Z, W, PCl, PCh);

				PopulateBUSRQ(0, PCh, 0, 0, PCh, 0, 0);
				PopulateMEMRQ(0, PCh, 0, 0, PCh, 0, 0);
				IRQS = 7;
			}
			else
			{
				PopulateCURINSTR(IDLE,
								IDLE,
								WAIT,
								RD_INC, Z, PCl, PCh,
								IDLE,
								WAIT,
								RD_INC, W, PCl, PCh);

				PopulateBUSRQ(0, PCh, 0, 0, PCh, 0, 0);
				PopulateMEMRQ(0, PCh, 0, 0, PCh, 0, 0);
				IRQS = 7;
			};
		};

		void RET_()
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD_INC, Z, SPl, SPh,
							IDLE,
							WAIT,
							RD_INC_TR_PC, Z, W, SPl, SPh);

			PopulateBUSRQ(0, SPh, 0, 0, SPh, 0, 0);
			PopulateMEMRQ(0, SPh, 0, 0, SPh, 0, 0);
			IRQS = 7;
		};

		void RETI_()
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD_INC, Z, SPl, SPh,
							IDLE,
							WAIT,
							RD_INC_TR_PC, Z, W, SPl, SPh);

			PopulateBUSRQ(0, SPh, 0, 0, SPh, 0, 0);
			PopulateMEMRQ(0, SPh, 0, 0, SPh, 0, 0);
			IRQS = 7;
		};

		void RETN_()
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD_INC, Z, SPl, SPh,
							EI_RETN,
							WAIT,
							RD_INC_TR_PC, Z, W, SPl, SPh);

			PopulateBUSRQ(0, SPh, 0, 0, SPh, 0, 0);
			PopulateMEMRQ(0, SPh, 0, 0, SPh, 0, 0);
			IRQS = 7;
		};


		void RET_COND(bool cond)
		{
			if (cond)
			{
				PopulateCURINSTR(IDLE,
								IDLE,
								IDLE,
								WAIT,
								RD_INC, Z, SPl, SPh,
								IDLE,
								WAIT,
								RD_INC_TR_PC, Z, W, SPl, SPh);

				PopulateBUSRQ(0, I, SPh, 0, 0, SPh, 0, 0);
				PopulateMEMRQ(0, 0, SPh, 0, 0, SPh, 0, 0);
				IRQS = 8;
			}
			else
			{
				PopulateCURINSTR(IDLE,
								IDLE);

				PopulateBUSRQ(0, I);
				PopulateMEMRQ(0, 0);
				IRQS = 2;
			};
		};

		void CALL_COND(bool cond)
		{
			if (cond)
			{
				PopulateCURINSTR(IDLE,
								IDLE,
								WAIT,
								RD_INC, Z, PCl, PCh,
								IDLE,
								WAIT,
								RD, W, PCl, PCh,
								INC16, PCl, PCh,
								DEC16, SPl, SPh,
								WAIT,
								WR_DEC, SPl, SPh, PCh,
								IDLE,
								WAIT,
								WR_TR_PC, SPl, SPh, PCl);

				PopulateBUSRQ(0, PCh, 0, 0, PCh, 0, 0, PCh, SPh, 0, 0, SPh, 0, 0);
				PopulateMEMRQ(0, PCh, 0, 0, PCh, 0, 0, 0, SPh, 0, 0, SPh, 0, 0);
				IRQS = 14;
			}
			else
			{
				PopulateCURINSTR(IDLE,
								IDLE,
								WAIT,
								RD_INC, Z, PCl, PCh,
								IDLE,
								WAIT,
								RD_INC, W, PCl, PCh);

				PopulateBUSRQ(0, PCh, 0, 0, PCh, 0, 0);
				PopulateMEMRQ(0, PCh, 0, 0, PCh, 0, 0);
				IRQS = 7;
			};
		};

		void INT_OP(uint32_t operation, uint32_t src)
		{
			PopulateCURINSTR(operation, src);

			PopulateBUSRQ(0);
			PopulateMEMRQ(0);
			IRQS = 1;
		};

		void BIT_OP(uint32_t operation, uint32_t bit, uint32_t src)
		{
			PopulateCURINSTR(operation, bit, src);

			PopulateBUSRQ(0);
			PopulateMEMRQ(0);
			IRQS = 1;
		};

		void PUSH_(uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(IDLE,
							DEC16, SPl, SPh,
							IDLE,
							WAIT,
							WR_DEC, SPl, SPh, src_h,
							IDLE,
							WAIT,
							WR, SPl, SPh, src_l);

			PopulateBUSRQ(0, I, SPh, 0, 0, SPh, 0, 0);
			PopulateMEMRQ(0, 0, SPh, 0, 0, SPh, 0, 0);
			IRQS = 8;
		};


		void POP_(uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD_INC, src_l, SPl, SPh,
							IDLE,
							WAIT,
							RD_INC, src_h, SPl, SPh);

			PopulateBUSRQ(0, SPh, 0, 0, SPh, 0, 0);
			PopulateMEMRQ(0, SPh, 0, 0, SPh, 0, 0);
			IRQS = 7;
		};

		void RST_(uint32_t n)
		{
			PopulateCURINSTR(IDLE,
							DEC16, SPl, SPh,
							IDLE,
							WAIT,
							WR_DEC, SPl, SPh, PCh,
							RST, n,
							WAIT,
							WR_TR_PC, SPl, SPh, PCl);

			PopulateBUSRQ(0, I, SPh, 0, 0, SPh, 0, 0);
			PopulateMEMRQ(0, 0, SPh, 0, 0, SPh, 0, 0);
			IRQS = 8;
		};

		void PREFIX_(uint32_t src)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							PREFIX);

			PRE_SRC = src;

			PopulateBUSRQ(0, PCh, 0, 0);
			PopulateMEMRQ(0, PCh, 0, 0);
			IRQS = -1; // prefix does not get interrupted
		};

		void PREFETCH_(uint32_t src)
		{
			if (src == IXCBpre)
			{
				Regs[W] = Regs[Ixh];
				Regs[Z] = Regs[Ixl];
			}
			else
			{
				Regs[W] = Regs[Iyh];
				Regs[Z] = Regs[Iyl];
			};

			PopulateCURINSTR(IDLE,
							IDLE,
							WAIT,
							RD_INC, ALU, PCl, PCh,
							ADDS, Z, W, ALU, ZERO,
							WAIT,
							IDLE,
							PREFIX);

			PRE_SRC = src;

			//Console.WriteLine(TotalExecutedCycles);

			PopulateBUSRQ(0, PCh, 0, 0, PCh, 0, 0, PCh);
			PopulateMEMRQ(0, PCh, 0, 0, PCh, 0, 0, 0);
			IRQS = -1; // prefetch does not get interrupted
		};

		void DI_()
		{
			PopulateCURINSTR(DI);

			PopulateBUSRQ(0);
			PopulateMEMRQ(0);
			IRQS = 1;
		};

		void EI_()
		{
			PopulateCURINSTR(EI);

			PopulateBUSRQ(0);
			PopulateMEMRQ(0);
			IRQS = 1;
		};

		void JP_16(uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(TR16, PCl, PCh, src_l, src_h);

			PopulateBUSRQ(0);
			PopulateMEMRQ(0);
			IRQS = 1;
		};

		void LD_SP_16(uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							TR16, SPl, SPh, src_l, src_h);

			PopulateBUSRQ(0, I, I);
			PopulateMEMRQ(0, 0, 0);
			IRQS = 3;
		};

		void OUT_()
		{
			PopulateCURINSTR(IDLE,
							TR, W, A,
							WAIT,
							RD_INC, Z, PCl, PCh,
							TR, ALU, A,
							WAIT,
							WAIT,
							OUT_INC, Z, ALU, A);

			PopulateBUSRQ(0, PCh, 0, 0, WIO1, WIO2, WIO3, WIO4);
			PopulateMEMRQ(0, PCh, 0, 0, WIO1, WIO2, WIO3, WIO4);
			IRQS = 8;
		};

		void OUT_REG_(uint32_t dest, uint32_t src)
		{
			PopulateCURINSTR(IDLE,
							TR16, Z, W, C, B,
							IDLE,
							IDLE,
							OUT_INC, Z, W, src);

			PopulateBUSRQ(0, BIO1, BIO2, BIO3, BIO4);
			PopulateMEMRQ(0, BIO1, BIO2, BIO3, BIO4);
			IRQS = 5;
		};

		void IN_()
		{
			PopulateCURINSTR(IDLE,
							TR, W, A,
							WAIT,
							RD_INC, Z, PCl, PCh,
							IDLE,
							WAIT,
							WAIT,
							IN_A_N_INC, A, Z, W);

			PopulateBUSRQ(0, PCh, 0, 0, WIO1, WIO2, WIO3, WIO4);
			PopulateMEMRQ(0, PCh, 0, 0, WIO1, WIO2, WIO3, WIO4);
			IRQS = 8;
		};

		void IN_REG_(uint32_t dest, uint32_t src)
		{
			PopulateCURINSTR(IDLE,
							TR16, Z, W, C, B,
							WAIT,
							WAIT,
							IN_INC, dest, Z, W);

			PopulateBUSRQ(0, BIO1, BIO2, BIO3, BIO4);
			PopulateMEMRQ(0, BIO1, BIO2, BIO3, BIO4);
			IRQS = 5;
		};

		void REG_OP_16_(uint32_t op, uint32_t dest_l, uint32_t dest_h, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							TR16, Z, W, dest_l, dest_h,
							INC16, Z, W,
							IDLE,
							IDLE,
							op, dest_l, dest_h, src_l, src_h);

			PopulateBUSRQ(0, I, I, I, I, I, I, I);
			PopulateMEMRQ(0, 0, 0, 0, 0, 0, 0, 0);
			IRQS = 8;
		};

		void INT_MODE_(uint32_t src)
		{
			PopulateCURINSTR(INT_MODE, src);

			PopulateBUSRQ(0);
			PopulateMEMRQ(0);
			IRQS = 1;
		};

		void RRD_()
		{
			PopulateCURINSTR(IDLE,
							TR16, Z, W, L, H,
							WAIT,
							RD, ALU, Z, W,
							IDLE,
							RRD, ALU, A,
							IDLE,
							IDLE,
							IDLE,
							WAIT,
							WR_INC, Z, W, ALU);

			PopulateBUSRQ(0, H, 0, 0, H, H, H, H, W, 0, 0);
			PopulateMEMRQ(0, H, 0, 0, 0, 0, 0, 0, W, 0, 0);
			IRQS = 11;
		};

		void RLD_()
		{
			PopulateCURINSTR(IDLE,
							TR16, Z, W, L, H,
							WAIT,
							RD, ALU, Z, W,
							IDLE,
							RLD, ALU, A,
							IDLE,
							IDLE,
							IDLE,
							WAIT,
							WR_INC, Z, W, ALU);

			PopulateBUSRQ(0, H, 0, 0, H, H, H, H, W, 0, 0);
			PopulateMEMRQ(0, H, 0, 0, 0, 0, 0, 0, W, 0, 0);
			IRQS = 11;
		};

		#pragma endregion

		#pragma region Decode

		void FetchInstruction()
		{
			if (NO_prefix)
			{
				switch (opcode)
				{
				case 0x00: NOP_();									break; // NOP
				case 0x01: LD_IND_16(C, B, PCl, PCh);				break; // LD BC, nn
				case 0x02: LD_8_IND(C, B, A);						break; // LD (BC), A
				case 0x03: INC_16(C, B);							break; // INC BC
				case 0x04: INT_OP(INC8, B);							break; // INC B
				case 0x05: INT_OP(DEC8, B);							break; // DEC B
				case 0x06: LD_IND_8_INC(B, PCl, PCh);				break; // LD B, n
				case 0x07: INT_OP(RLC, Aim);						break; // RLCA
				case 0x08: EXCH_();									break; // EXCH AF, AF'
				case 0x09: ADD_16(L, H, C, B);						break; // ADD HL, BC
				case 0x0A: REG_OP_IND(TR, A, C, B);					break; // LD A, (BC)
				case 0x0B: DEC_16(C, B);							break; // DEC BC
				case 0x0C: INT_OP(INC8, C);							break; // INC C
				case 0x0D: INT_OP(DEC8, C);							break; // DEC C
				case 0x0E: LD_IND_8_INC(C, PCl, PCh);				break; // LD C, n
				case 0x0F: INT_OP(RRC, Aim);						break; // RRCA
				case 0x10: DJNZ_();									break; // DJNZ B
				case 0x11: LD_IND_16(E, D, PCl, PCh);				break; // LD DE, nn
				case 0x12: LD_8_IND(E, D, A);						break; // LD (DE), A
				case 0x13: INC_16(E, D);							break; // INC DE
				case 0x14: INT_OP(INC8, D);							break; // INC D
				case 0x15: INT_OP(DEC8, D);							break; // DEC D
				case 0x16: LD_IND_8_INC(D, PCl, PCh);				break; // LD D, n
				case 0x17: INT_OP(RL, Aim);							break; // RLA
				case 0x18: JR_COND(true);							break; // JR, r8
				case 0x19: ADD_16(L, H, E, D);						break; // ADD HL, DE
				case 0x1A: REG_OP_IND(TR, A, E, D);					break; // LD A, (DE)
				case 0x1B: DEC_16(E, D);							break; // DEC DE
				case 0x1C: INT_OP(INC8, E);							break; // INC E
				case 0x1D: INT_OP(DEC8, E);							break; // DEC E
				case 0x1E: LD_IND_8_INC(E, PCl, PCh);				break; // LD E, n
				case 0x1F: INT_OP(RR, Aim);							break; // RRA
				case 0x20: JR_COND(!FlagZget());					break; // JR NZ, r8
				case 0x21: LD_IND_16(L, H, PCl, PCh);				break; // LD HL, nn
				case 0x22: LD_16_IND_nn(L, H);						break; // LD (nn), HL
				case 0x23: INC_16(L, H);							break; // INC HL
				case 0x24: INT_OP(INC8, H);							break; // INC H
				case 0x25: INT_OP(DEC8, H);							break; // DEC H
				case 0x26: LD_IND_8_INC(H, PCl, PCh);				break; // LD H, n
				case 0x27: INT_OP(DA, A);							break; // DAA
				case 0x28: JR_COND(FlagZget());						break; // JR Z, r8
				case 0x29: ADD_16(L, H, L, H);						break; // ADD HL, HL
				case 0x2A: LD_IND_16_nn(L, H);						break; // LD HL, (nn)
				case 0x2B: DEC_16(L, H);							break; // DEC HL
				case 0x2C: INT_OP(INC8, L);							break; // INC L
				case 0x2D: INT_OP(DEC8, L);							break; // DEC L
				case 0x2E: LD_IND_8_INC(L, PCl, PCh);				break; // LD L, n
				case 0x2F: INT_OP(CPL, A);							break; // CPL
				case 0x30: JR_COND(!FlagCget());					break; // JR NC, r8
				case 0x31: LD_IND_16(SPl, SPh, PCl, PCh);			break; // LD SP, nn
				case 0x32: LD_8_IND_nn(A);							break; // LD (nn), A
				case 0x33: INC_16(SPl, SPh);						break; // INC SP
				case 0x34: INC_8_IND(L, H);							break; // INC (HL)
				case 0x35: DEC_8_IND(L, H);							break; // DEC (HL)
				case 0x36: LD_8_IND_IND(L, H, PCl, PCh);			break; // LD (HL), n
				case 0x37: INT_OP(SCF, A);							break; // SCF
				case 0x38: JR_COND(FlagCget());						break; // JR C, r8
				case 0x39: ADD_16(L, H, SPl, SPh);					break; // ADD HL, SP
				case 0x3A: LD_IND_8_nn(A);							break; // LD A, (nn)
				case 0x3B: DEC_16(SPl, SPh);						break; // DEC SP
				case 0x3C: INT_OP(INC8, A);							break; // INC A
				case 0x3D: INT_OP(DEC8, A);							break; // DEC A
				case 0x3E: LD_IND_8_INC(A, PCl, PCh);				break; // LD A, n
				case 0x3F: INT_OP(CCF, A);							break; // CCF
				case 0x40: REG_OP(TR, B, B);						break; // LD B, B
				case 0x41: REG_OP(TR, B, C);						break; // LD B, C
				case 0x42: REG_OP(TR, B, D);						break; // LD B, D
				case 0x43: REG_OP(TR, B, E);						break; // LD B, E
				case 0x44: REG_OP(TR, B, H);						break; // LD B, H
				case 0x45: REG_OP(TR, B, L);						break; // LD B, L
				case 0x46: REG_OP_IND_HL(TR, B);					break; // LD B, (HL)
				case 0x47: REG_OP(TR, B, A);						break; // LD B, A
				case 0x48: REG_OP(TR, C, B);						break; // LD C, B
				case 0x49: REG_OP(TR, C, C);						break; // LD C, C
				case 0x4A: REG_OP(TR, C, D);						break; // LD C, D
				case 0x4B: REG_OP(TR, C, E);						break; // LD C, E
				case 0x4C: REG_OP(TR, C, H);						break; // LD C, H
				case 0x4D: REG_OP(TR, C, L);						break; // LD C, L
				case 0x4E: REG_OP_IND_HL(TR, C);					break; // LD C, (HL)
				case 0x4F: REG_OP(TR, C, A);						break; // LD C, A
				case 0x50: REG_OP(TR, D, B);						break; // LD D, B
				case 0x51: REG_OP(TR, D, C);						break; // LD D, C
				case 0x52: REG_OP(TR, D, D);						break; // LD D, D
				case 0x53: REG_OP(TR, D, E);						break; // LD D, E
				case 0x54: REG_OP(TR, D, H);						break; // LD D, H
				case 0x55: REG_OP(TR, D, L);						break; // LD D, L
				case 0x56: REG_OP_IND_HL(TR, D);					break; // LD D, (HL)
				case 0x57: REG_OP(TR, D, A);						break; // LD D, A
				case 0x58: REG_OP(TR, E, B);						break; // LD E, B
				case 0x59: REG_OP(TR, E, C);						break; // LD E, C
				case 0x5A: REG_OP(TR, E, D);						break; // LD E, D
				case 0x5B: REG_OP(TR, E, E);						break; // LD E, E
				case 0x5C: REG_OP(TR, E, H);						break; // LD E, H
				case 0x5D: REG_OP(TR, E, L);						break; // LD E, L
				case 0x5E: REG_OP_IND_HL(TR, E);					break; // LD E, (HL)
				case 0x5F: REG_OP(TR, E, A);						break; // LD E, A
				case 0x60: REG_OP(TR, H, B);						break; // LD H, B
				case 0x61: REG_OP(TR, H, C);						break; // LD H, C
				case 0x62: REG_OP(TR, H, D);						break; // LD H, D
				case 0x63: REG_OP(TR, H, E);						break; // LD H, E
				case 0x64: REG_OP(TR, H, H);						break; // LD H, H
				case 0x65: REG_OP(TR, H, L);						break; // LD H, L
				case 0x66: REG_OP_IND_HL(TR, H);					break; // LD H, (HL)
				case 0x67: REG_OP(TR, H, A);						break; // LD H, A
				case 0x68: REG_OP(TR, L, B);						break; // LD L, B
				case 0x69: REG_OP(TR, L, C);						break; // LD L, C
				case 0x6A: REG_OP(TR, L, D);						break; // LD L, D
				case 0x6B: REG_OP(TR, L, E);						break; // LD L, E
				case 0x6C: REG_OP(TR, L, H);						break; // LD L, H
				case 0x6D: REG_OP(TR, L, L);						break; // LD L, L
				case 0x6E: REG_OP_IND_HL(TR, L);					break; // LD L, (HL)
				case 0x6F: REG_OP(TR, L, A);						break; // LD L, A
				case 0x70: LD_8_IND_HL(B);							break; // LD (HL), B
				case 0x71: LD_8_IND_HL(C);							break; // LD (HL), C
				case 0x72: LD_8_IND_HL(D);							break; // LD (HL), D
				case 0x73: LD_8_IND_HL(E);							break; // LD (HL), E
				case 0x74: LD_8_IND_HL(H);							break; // LD (HL), H
				case 0x75: LD_8_IND_HL(L);							break; // LD (HL), L
				case 0x76: HALT_();									break; // HALT
				case 0x77: LD_8_IND_HL(A);							break; // LD (HL), A
				case 0x78: REG_OP(TR, A, B);						break; // LD A, B
				case 0x79: REG_OP(TR, A, C);						break; // LD A, C
				case 0x7A: REG_OP(TR, A, D);						break; // LD A, D
				case 0x7B: REG_OP(TR, A, E);						break; // LD A, E
				case 0x7C: REG_OP(TR, A, H);						break; // LD A, H
				case 0x7D: REG_OP(TR, A, L);						break; // LD A, L
				case 0x7E: REG_OP_IND_HL(TR, A);					break; // LD A, (HL)
				case 0x7F: REG_OP(TR, A, A);						break; // LD A, A
				case 0x80: REG_OP(ADD8, A, B);						break; // ADD A, B
				case 0x81: REG_OP(ADD8, A, C);						break; // ADD A, C
				case 0x82: REG_OP(ADD8, A, D);						break; // ADD A, D
				case 0x83: REG_OP(ADD8, A, E);						break; // ADD A, E
				case 0x84: REG_OP(ADD8, A, H);						break; // ADD A, H
				case 0x85: REG_OP(ADD8, A, L);						break; // ADD A, L
				case 0x86: REG_OP_IND(ADD8, A, L, H);				break; // ADD A, (HL)
				case 0x87: REG_OP(ADD8, A, A);						break; // ADD A, A
				case 0x88: REG_OP(ADC8, A, B);						break; // ADC A, B
				case 0x89: REG_OP(ADC8, A, C);						break; // ADC A, C
				case 0x8A: REG_OP(ADC8, A, D);						break; // ADC A, D
				case 0x8B: REG_OP(ADC8, A, E);						break; // ADC A, E
				case 0x8C: REG_OP(ADC8, A, H);						break; // ADC A, H
				case 0x8D: REG_OP(ADC8, A, L);						break; // ADC A, L
				case 0x8E: REG_OP_IND(ADC8, A, L, H);				break; // ADC A, (HL)
				case 0x8F: REG_OP(ADC8, A, A);						break; // ADC A, A
				case 0x90: REG_OP(SUB8, A, B);						break; // SUB A, B
				case 0x91: REG_OP(SUB8, A, C);						break; // SUB A, C
				case 0x92: REG_OP(SUB8, A, D);						break; // SUB A, D
				case 0x93: REG_OP(SUB8, A, E);						break; // SUB A, E
				case 0x94: REG_OP(SUB8, A, H);						break; // SUB A, H
				case 0x95: REG_OP(SUB8, A, L);						break; // SUB A, L
				case 0x96: REG_OP_IND(SUB8, A, L, H);				break; // SUB A, (HL)
				case 0x97: REG_OP(SUB8, A, A);						break; // SUB A, A
				case 0x98: REG_OP(SBC8, A, B);						break; // SBC A, B
				case 0x99: REG_OP(SBC8, A, C);						break; // SBC A, C
				case 0x9A: REG_OP(SBC8, A, D);						break; // SBC A, D
				case 0x9B: REG_OP(SBC8, A, E);						break; // SBC A, E
				case 0x9C: REG_OP(SBC8, A, H);						break; // SBC A, H
				case 0x9D: REG_OP(SBC8, A, L);						break; // SBC A, L
				case 0x9E: REG_OP_IND(SBC8, A, L, H);				break; // SBC A, (HL)
				case 0x9F: REG_OP(SBC8, A, A);						break; // SBC A, A
				case 0xA0: REG_OP(AND8, A, B);						break; // AND A, B
				case 0xA1: REG_OP(AND8, A, C);						break; // AND A, C
				case 0xA2: REG_OP(AND8, A, D);						break; // AND A, D
				case 0xA3: REG_OP(AND8, A, E);						break; // AND A, E
				case 0xA4: REG_OP(AND8, A, H);						break; // AND A, H
				case 0xA5: REG_OP(AND8, A, L);						break; // AND A, L
				case 0xA6: REG_OP_IND(AND8, A, L, H);				break; // AND A, (HL)
				case 0xA7: REG_OP(AND8, A, A);						break; // AND A, A
				case 0xA8: REG_OP(XOR8, A, B);						break; // XOR A, B
				case 0xA9: REG_OP(XOR8, A, C);						break; // XOR A, C
				case 0xAA: REG_OP(XOR8, A, D);						break; // XOR A, D
				case 0xAB: REG_OP(XOR8, A, E);						break; // XOR A, E
				case 0xAC: REG_OP(XOR8, A, H);						break; // XOR A, H
				case 0xAD: REG_OP(XOR8, A, L);						break; // XOR A, L
				case 0xAE: REG_OP_IND(XOR8, A, L, H);				break; // XOR A, (HL)
				case 0xAF: REG_OP(XOR8, A, A);						break; // XOR A, A
				case 0xB0: REG_OP(OR8, A, B);						break; // OR A, B
				case 0xB1: REG_OP(OR8, A, C);						break; // OR A, C
				case 0xB2: REG_OP(OR8, A, D);						break; // OR A, D
				case 0xB3: REG_OP(OR8, A, E);						break; // OR A, E
				case 0xB4: REG_OP(OR8, A, H);						break; // OR A, H
				case 0xB5: REG_OP(OR8, A, L);						break; // OR A, L
				case 0xB6: REG_OP_IND(OR8, A, L, H);				break; // OR A, (HL)
				case 0xB7: REG_OP(OR8, A, A);						break; // OR A, A
				case 0xB8: REG_OP(CP8, A, B);						break; // CP A, B
				case 0xB9: REG_OP(CP8, A, C);						break; // CP A, C
				case 0xBA: REG_OP(CP8, A, D);						break; // CP A, D
				case 0xBB: REG_OP(CP8, A, E);						break; // CP A, E
				case 0xBC: REG_OP(CP8, A, H);						break; // CP A, H
				case 0xBD: REG_OP(CP8, A, L);						break; // CP A, L
				case 0xBE: REG_OP_IND(CP8, A, L, H);				break; // CP A, (HL)
				case 0xBF: REG_OP(CP8, A, A);						break; // CP A, A
				case 0xC0: RET_COND(!FlagZget());					break; // Ret NZ
				case 0xC1: POP_(C, B);								break; // POP BC
				case 0xC2: JP_COND(!FlagZget());					break; // JP NZ
				case 0xC3: JP_COND(true);							break; // JP
				case 0xC4: CALL_COND(!FlagZget());					break; // CALL NZ
				case 0xC5: PUSH_(C, B);								break; // PUSH BC
				case 0xC6: REG_OP_IND_INC(ADD8, A, PCl, PCh);		break; // ADD A, n
				case 0xC7: RST_(0);									break; // RST 0
				case 0xC8: RET_COND(FlagZget());					break; // RET Z
				case 0xC9: RET_();									break; // RET
				case 0xCA: JP_COND(FlagZget());						break; // JP Z
				case 0xCB: PREFIX_(CBpre);							break; // PREFIX CB
				case 0xCC: CALL_COND(FlagZget());					break; // CALL Z
				case 0xCD: CALL_COND(true);							break; // CALL
				case 0xCE: REG_OP_IND_INC(ADC8, A, PCl, PCh);		break; // ADC A, n
				case 0xCF: RST_(0x08);								break; // RST 0x08
				case 0xD0: RET_COND(!FlagCget());					break; // Ret NC
				case 0xD1: POP_(E, D);								break; // POP DE
				case 0xD2: JP_COND(!FlagCget());					break; // JP NC
				case 0xD3: OUT_();									break; // OUT A
				case 0xD4: CALL_COND(!FlagCget());					break; // CALL NC
				case 0xD5: PUSH_(E, D);								break; // PUSH DE
				case 0xD6: REG_OP_IND_INC(SUB8, A, PCl, PCh);		break; // SUB A, n
				case 0xD7: RST_(0x10);								break; // RST 0x10
				case 0xD8: RET_COND(FlagCget());					break; // RET C
				case 0xD9: EXX_();									break; // EXX
				case 0xDA: JP_COND(FlagCget());						break; // JP C
				case 0xDB: IN_();									break; // IN A
				case 0xDC: CALL_COND(FlagCget());					break; // CALL C
				case 0xDD: PREFIX_(IXpre);							break; // PREFIX IX
				case 0xDE: REG_OP_IND_INC(SBC8, A, PCl, PCh);		break; // SBC A, n
				case 0xDF: RST_(0x18);								break; // RST 0x18
				case 0xE0: RET_COND(!FlagPget());					break; // RET Po
				case 0xE1: POP_(L, H);								break; // POP HL
				case 0xE2: JP_COND(!FlagPget());					break; // JP Po
				case 0xE3: EXCH_16_IND_(SPl, SPh, L, H);			break; // ex (SP), HL
				case 0xE4: CALL_COND(!FlagPget());					break; // CALL Po
				case 0xE5: PUSH_(L, H);								break; // PUSH HL
				case 0xE6: REG_OP_IND_INC(AND8, A, PCl, PCh);		break; // AND A, n
				case 0xE7: RST_(0x20);								break; // RST 0x20
				case 0xE8: RET_COND(FlagPget());					break; // RET Pe
				case 0xE9: JP_16(L, H);								break; // JP (HL)
				case 0xEA: JP_COND(FlagPget());						break; // JP Pe
				case 0xEB: EXCH_16_(E, D, L, H);					break; // ex DE, HL
				case 0xEC: CALL_COND(FlagPget());					break; // CALL Pe
				case 0xED: PREFIX_(EXTDpre);						break; // PREFIX EXTD
				case 0xEE: REG_OP_IND_INC(XOR8, A, PCl, PCh);		break; // XOR A, n
				case 0xEF: RST_(0x28);								break; // RST 0x28
				case 0xF0: RET_COND(!FlagSget());					break; // RET p
				case 0xF1: POP_(F, A);								break; // POP AF
				case 0xF2: JP_COND(!FlagSget());					break; // JP p
				case 0xF3: DI_();									break; // DI
				case 0xF4: CALL_COND(!FlagSget());					break; // CALL p
				case 0xF5: PUSH_(F, A);								break; // PUSH AF
				case 0xF6: REG_OP_IND_INC(OR8, A, PCl, PCh);		break; // OR A, n
				case 0xF7: RST_(0x30);								break; // RST 0x30
				case 0xF8: RET_COND(FlagSget());					break; // RET M
				case 0xF9: LD_SP_16(L, H);							break; // LD SP, HL
				case 0xFA: JP_COND(FlagSget());						break; // JP M
				case 0xFB: EI_();									break; // EI
				case 0xFC: CALL_COND(FlagSget());					break; // CALL M
				case 0xFD: PREFIX_(IYpre);							break; // PREFIX IY
				case 0xFE: REG_OP_IND_INC(CP8, A, PCl, PCh);		break; // CP A, n
				case 0xFF: RST_(0x38);								break; // RST 0x38
				}
			}
			else if (CB_prefix)
			{
				CB_prefix = false;
				NO_prefix = true;
				switch (opcode)
				{
				case 0x00: INT_OP(RLC, B);							break; // RLC B
				case 0x01: INT_OP(RLC, C);							break; // RLC C
				case 0x02: INT_OP(RLC, D);							break; // RLC D
				case 0x03: INT_OP(RLC, E);							break; // RLC E
				case 0x04: INT_OP(RLC, H);							break; // RLC H
				case 0x05: INT_OP(RLC, L);							break; // RLC L
				case 0x06: INT_OP_IND(RLC, L, H);					break; // RLC (HL)
				case 0x07: INT_OP(RLC, A);							break; // RLC A
				case 0x08: INT_OP(RRC, B);							break; // RRC B
				case 0x09: INT_OP(RRC, C);							break; // RRC C
				case 0x0A: INT_OP(RRC, D);							break; // RRC D
				case 0x0B: INT_OP(RRC, E);							break; // RRC E
				case 0x0C: INT_OP(RRC, H);							break; // RRC H
				case 0x0D: INT_OP(RRC, L);							break; // RRC L
				case 0x0E: INT_OP_IND(RRC, L, H);					break; // RRC (HL)
				case 0x0F: INT_OP(RRC, A);							break; // RRC A
				case 0x10: INT_OP(RL, B);							break; // RL B
				case 0x11: INT_OP(RL, C);							break; // RL C
				case 0x12: INT_OP(RL, D);							break; // RL D
				case 0x13: INT_OP(RL, E);							break; // RL E
				case 0x14: INT_OP(RL, H);							break; // RL H
				case 0x15: INT_OP(RL, L);							break; // RL L
				case 0x16: INT_OP_IND(RL, L, H);					break; // RL (HL)
				case 0x17: INT_OP(RL, A);							break; // RL A
				case 0x18: INT_OP(RR, B);							break; // RR B
				case 0x19: INT_OP(RR, C);							break; // RR C
				case 0x1A: INT_OP(RR, D);							break; // RR D
				case 0x1B: INT_OP(RR, E);							break; // RR E
				case 0x1C: INT_OP(RR, H);							break; // RR H
				case 0x1D: INT_OP(RR, L);							break; // RR L
				case 0x1E: INT_OP_IND(RR, L, H);					break; // RR (HL)
				case 0x1F: INT_OP(RR, A);							break; // RR A
				case 0x20: INT_OP(SLA, B);							break; // SLA B
				case 0x21: INT_OP(SLA, C);							break; // SLA C
				case 0x22: INT_OP(SLA, D);							break; // SLA D
				case 0x23: INT_OP(SLA, E);							break; // SLA E
				case 0x24: INT_OP(SLA, H);							break; // SLA H
				case 0x25: INT_OP(SLA, L);							break; // SLA L
				case 0x26: INT_OP_IND(SLA, L, H);					break; // SLA (HL)
				case 0x27: INT_OP(SLA, A);							break; // SLA A
				case 0x28: INT_OP(SRA, B);							break; // SRA B
				case 0x29: INT_OP(SRA, C);							break; // SRA C
				case 0x2A: INT_OP(SRA, D);							break; // SRA D
				case 0x2B: INT_OP(SRA, E);							break; // SRA E
				case 0x2C: INT_OP(SRA, H);							break; // SRA H
				case 0x2D: INT_OP(SRA, L);							break; // SRA L
				case 0x2E: INT_OP_IND(SRA, L, H);					break; // SRA (HL)
				case 0x2F: INT_OP(SRA, A);							break; // SRA A
				case 0x30: INT_OP(SLL, B);							break; // SLL B
				case 0x31: INT_OP(SLL, C);							break; // SLL C
				case 0x32: INT_OP(SLL, D);							break; // SLL D
				case 0x33: INT_OP(SLL, E);							break; // SLL E
				case 0x34: INT_OP(SLL, H);							break; // SLL H
				case 0x35: INT_OP(SLL, L);							break; // SLL L
				case 0x36: INT_OP_IND(SLL, L, H);					break; // SLL (HL)
				case 0x37: INT_OP(SLL, A);							break; // SLL A
				case 0x38: INT_OP(SRL, B);							break; // SRL B
				case 0x39: INT_OP(SRL, C);							break; // SRL C
				case 0x3A: INT_OP(SRL, D);							break; // SRL D
				case 0x3B: INT_OP(SRL, E);							break; // SRL E
				case 0x3C: INT_OP(SRL, H);							break; // SRL H
				case 0x3D: INT_OP(SRL, L);							break; // SRL L
				case 0x3E: INT_OP_IND(SRL, L, H);					break; // SRL (HL)
				case 0x3F: INT_OP(SRL, A);							break; // SRL A
				case 0x40: BIT_OP(BIT, 0, B);						break; // BIT 0, B
				case 0x41: BIT_OP(BIT, 0, C);						break; // BIT 0, C
				case 0x42: BIT_OP(BIT, 0, D);						break; // BIT 0, D
				case 0x43: BIT_OP(BIT, 0, E);						break; // BIT 0, E
				case 0x44: BIT_OP(BIT, 0, H);						break; // BIT 0, H
				case 0x45: BIT_OP(BIT, 0, L);						break; // BIT 0, L
				case 0x46: BIT_TE_IND(BIT, 0, L, H);				break; // BIT 0, (HL)
				case 0x47: BIT_OP(BIT, 0, A);						break; // BIT 0, A
				case 0x48: BIT_OP(BIT, 1, B);						break; // BIT 1, B
				case 0x49: BIT_OP(BIT, 1, C);						break; // BIT 1, C
				case 0x4A: BIT_OP(BIT, 1, D);						break; // BIT 1, D
				case 0x4B: BIT_OP(BIT, 1, E);						break; // BIT 1, E
				case 0x4C: BIT_OP(BIT, 1, H);						break; // BIT 1, H
				case 0x4D: BIT_OP(BIT, 1, L);						break; // BIT 1, L
				case 0x4E: BIT_TE_IND(BIT, 1, L, H);				break; // BIT 1, (HL)
				case 0x4F: BIT_OP(BIT, 1, A);						break; // BIT 1, A
				case 0x50: BIT_OP(BIT, 2, B);						break; // BIT 2, B
				case 0x51: BIT_OP(BIT, 2, C);						break; // BIT 2, C
				case 0x52: BIT_OP(BIT, 2, D);						break; // BIT 2, D
				case 0x53: BIT_OP(BIT, 2, E);						break; // BIT 2, E
				case 0x54: BIT_OP(BIT, 2, H);						break; // BIT 2, H
				case 0x55: BIT_OP(BIT, 2, L);						break; // BIT 2, L
				case 0x56: BIT_TE_IND(BIT, 2, L, H);				break; // BIT 2, (HL)
				case 0x57: BIT_OP(BIT, 2, A);						break; // BIT 2, A
				case 0x58: BIT_OP(BIT, 3, B);						break; // BIT 3, B
				case 0x59: BIT_OP(BIT, 3, C);						break; // BIT 3, C
				case 0x5A: BIT_OP(BIT, 3, D);						break; // BIT 3, D
				case 0x5B: BIT_OP(BIT, 3, E);						break; // BIT 3, E
				case 0x5C: BIT_OP(BIT, 3, H);						break; // BIT 3, H
				case 0x5D: BIT_OP(BIT, 3, L);						break; // BIT 3, L
				case 0x5E: BIT_TE_IND(BIT, 3, L, H);				break; // BIT 3, (HL)
				case 0x5F: BIT_OP(BIT, 3, A);						break; // BIT 3, A
				case 0x60: BIT_OP(BIT, 4, B);						break; // BIT 4, B
				case 0x61: BIT_OP(BIT, 4, C);						break; // BIT 4, C
				case 0x62: BIT_OP(BIT, 4, D);						break; // BIT 4, D
				case 0x63: BIT_OP(BIT, 4, E);						break; // BIT 4, E
				case 0x64: BIT_OP(BIT, 4, H);						break; // BIT 4, H
				case 0x65: BIT_OP(BIT, 4, L);						break; // BIT 4, L
				case 0x66: BIT_TE_IND(BIT, 4, L, H);				break; // BIT 4, (HL)
				case 0x67: BIT_OP(BIT, 4, A);						break; // BIT 4, A
				case 0x68: BIT_OP(BIT, 5, B);						break; // BIT 5, B
				case 0x69: BIT_OP(BIT, 5, C);						break; // BIT 5, C
				case 0x6A: BIT_OP(BIT, 5, D);						break; // BIT 5, D
				case 0x6B: BIT_OP(BIT, 5, E);						break; // BIT 5, E
				case 0x6C: BIT_OP(BIT, 5, H);						break; // BIT 5, H
				case 0x6D: BIT_OP(BIT, 5, L);						break; // BIT 5, L
				case 0x6E: BIT_TE_IND(BIT, 5, L, H);				break; // BIT 5, (HL)
				case 0x6F: BIT_OP(BIT, 5, A);						break; // BIT 5, A
				case 0x70: BIT_OP(BIT, 6, B);						break; // BIT 6, B
				case 0x71: BIT_OP(BIT, 6, C);						break; // BIT 6, C
				case 0x72: BIT_OP(BIT, 6, D);						break; // BIT 6, D
				case 0x73: BIT_OP(BIT, 6, E);						break; // BIT 6, E
				case 0x74: BIT_OP(BIT, 6, H);						break; // BIT 6, H
				case 0x75: BIT_OP(BIT, 6, L);						break; // BIT 6, L
				case 0x76: BIT_TE_IND(BIT, 6, L, H);				break; // BIT 6, (HL)
				case 0x77: BIT_OP(BIT, 6, A);						break; // BIT 6, A
				case 0x78: BIT_OP(BIT, 7, B);						break; // BIT 7, B
				case 0x79: BIT_OP(BIT, 7, C);						break; // BIT 7, C
				case 0x7A: BIT_OP(BIT, 7, D);						break; // BIT 7, D
				case 0x7B: BIT_OP(BIT, 7, E);						break; // BIT 7, E
				case 0x7C: BIT_OP(BIT, 7, H);						break; // BIT 7, H
				case 0x7D: BIT_OP(BIT, 7, L);						break; // BIT 7, L
				case 0x7E: BIT_TE_IND(BIT, 7, L, H);				break; // BIT 7, (HL)
				case 0x7F: BIT_OP(BIT, 7, A);						break; // BIT 7, A
				case 0x80: BIT_OP(RES, 0, B);						break; // RES 0, B
				case 0x81: BIT_OP(RES, 0, C);						break; // RES 0, C
				case 0x82: BIT_OP(RES, 0, D);						break; // RES 0, D
				case 0x83: BIT_OP(RES, 0, E);						break; // RES 0, E
				case 0x84: BIT_OP(RES, 0, H);						break; // RES 0, H
				case 0x85: BIT_OP(RES, 0, L);						break; // RES 0, L
				case 0x86: BIT_OP_IND(RES, 0, L, H);				break; // RES 0, (HL)
				case 0x87: BIT_OP(RES, 0, A);						break; // RES 0, A
				case 0x88: BIT_OP(RES, 1, B);						break; // RES 1, B
				case 0x89: BIT_OP(RES, 1, C);						break; // RES 1, C
				case 0x8A: BIT_OP(RES, 1, D);						break; // RES 1, D
				case 0x8B: BIT_OP(RES, 1, E);						break; // RES 1, E
				case 0x8C: BIT_OP(RES, 1, H);						break; // RES 1, H
				case 0x8D: BIT_OP(RES, 1, L);						break; // RES 1, L
				case 0x8E: BIT_OP_IND(RES, 1, L, H);				break; // RES 1, (HL)
				case 0x8F: BIT_OP(RES, 1, A);						break; // RES 1, A
				case 0x90: BIT_OP(RES, 2, B);						break; // RES 2, B
				case 0x91: BIT_OP(RES, 2, C);						break; // RES 2, C
				case 0x92: BIT_OP(RES, 2, D);						break; // RES 2, D
				case 0x93: BIT_OP(RES, 2, E);						break; // RES 2, E
				case 0x94: BIT_OP(RES, 2, H);						break; // RES 2, H
				case 0x95: BIT_OP(RES, 2, L);						break; // RES 2, L
				case 0x96: BIT_OP_IND(RES, 2, L, H);				break; // RES 2, (HL)
				case 0x97: BIT_OP(RES, 2, A);						break; // RES 2, A
				case 0x98: BIT_OP(RES, 3, B);						break; // RES 3, B
				case 0x99: BIT_OP(RES, 3, C);						break; // RES 3, C
				case 0x9A: BIT_OP(RES, 3, D);						break; // RES 3, D
				case 0x9B: BIT_OP(RES, 3, E);						break; // RES 3, E
				case 0x9C: BIT_OP(RES, 3, H);						break; // RES 3, H
				case 0x9D: BIT_OP(RES, 3, L);						break; // RES 3, L
				case 0x9E: BIT_OP_IND(RES, 3, L, H);				break; // RES 3, (HL)
				case 0x9F: BIT_OP(RES, 3, A);						break; // RES 3, A
				case 0xA0: BIT_OP(RES, 4, B);						break; // RES 4, B
				case 0xA1: BIT_OP(RES, 4, C);						break; // RES 4, C
				case 0xA2: BIT_OP(RES, 4, D);						break; // RES 4, D
				case 0xA3: BIT_OP(RES, 4, E);						break; // RES 4, E
				case 0xA4: BIT_OP(RES, 4, H);						break; // RES 4, H
				case 0xA5: BIT_OP(RES, 4, L);						break; // RES 4, L
				case 0xA6: BIT_OP_IND(RES, 4, L, H);				break; // RES 4, (HL)
				case 0xA7: BIT_OP(RES, 4, A);						break; // RES 4, A
				case 0xA8: BIT_OP(RES, 5, B);						break; // RES 5, B
				case 0xA9: BIT_OP(RES, 5, C);						break; // RES 5, C
				case 0xAA: BIT_OP(RES, 5, D);						break; // RES 5, D
				case 0xAB: BIT_OP(RES, 5, E);						break; // RES 5, E
				case 0xAC: BIT_OP(RES, 5, H);						break; // RES 5, H
				case 0xAD: BIT_OP(RES, 5, L);						break; // RES 5, L
				case 0xAE: BIT_OP_IND(RES, 5, L, H);				break; // RES 5, (HL)
				case 0xAF: BIT_OP(RES, 5, A);						break; // RES 5, A
				case 0xB0: BIT_OP(RES, 6, B);						break; // RES 6, B
				case 0xB1: BIT_OP(RES, 6, C);						break; // RES 6, C
				case 0xB2: BIT_OP(RES, 6, D);						break; // RES 6, D
				case 0xB3: BIT_OP(RES, 6, E);						break; // RES 6, E
				case 0xB4: BIT_OP(RES, 6, H);						break; // RES 6, H
				case 0xB5: BIT_OP(RES, 6, L);						break; // RES 6, L
				case 0xB6: BIT_OP_IND(RES, 6, L, H);				break; // RES 6, (HL)
				case 0xB7: BIT_OP(RES, 6, A);						break; // RES 6, A
				case 0xB8: BIT_OP(RES, 7, B);						break; // RES 7, B
				case 0xB9: BIT_OP(RES, 7, C);						break; // RES 7, C
				case 0xBA: BIT_OP(RES, 7, D);						break; // RES 7, D
				case 0xBB: BIT_OP(RES, 7, E);						break; // RES 7, E
				case 0xBC: BIT_OP(RES, 7, H);						break; // RES 7, H
				case 0xBD: BIT_OP(RES, 7, L);						break; // RES 7, L
				case 0xBE: BIT_OP_IND(RES, 7, L, H);				break; // RES 7, (HL)
				case 0xBF: BIT_OP(RES, 7, A);						break; // RES 7, A
				case 0xC0: BIT_OP(SET, 0, B);						break; // SET 0, B
				case 0xC1: BIT_OP(SET, 0, C);						break; // SET 0, C
				case 0xC2: BIT_OP(SET, 0, D);						break; // SET 0, D
				case 0xC3: BIT_OP(SET, 0, E);						break; // SET 0, E
				case 0xC4: BIT_OP(SET, 0, H);						break; // SET 0, H
				case 0xC5: BIT_OP(SET, 0, L);						break; // SET 0, L
				case 0xC6: BIT_OP_IND(SET, 0, L, H);				break; // SET 0, (HL)
				case 0xC7: BIT_OP(SET, 0, A);						break; // SET 0, A
				case 0xC8: BIT_OP(SET, 1, B);						break; // SET 1, B
				case 0xC9: BIT_OP(SET, 1, C);						break; // SET 1, C
				case 0xCA: BIT_OP(SET, 1, D);						break; // SET 1, D
				case 0xCB: BIT_OP(SET, 1, E);						break; // SET 1, E
				case 0xCC: BIT_OP(SET, 1, H);						break; // SET 1, H
				case 0xCD: BIT_OP(SET, 1, L);						break; // SET 1, L
				case 0xCE: BIT_OP_IND(SET, 1, L, H);				break; // SET 1, (HL)
				case 0xCF: BIT_OP(SET, 1, A);						break; // SET 1, A
				case 0xD0: BIT_OP(SET, 2, B);						break; // SET 2, B
				case 0xD1: BIT_OP(SET, 2, C);						break; // SET 2, C
				case 0xD2: BIT_OP(SET, 2, D);						break; // SET 2, D
				case 0xD3: BIT_OP(SET, 2, E);						break; // SET 2, E
				case 0xD4: BIT_OP(SET, 2, H);						break; // SET 2, H
				case 0xD5: BIT_OP(SET, 2, L);						break; // SET 2, L
				case 0xD6: BIT_OP_IND(SET, 2, L, H);				break; // SET 2, (HL)
				case 0xD7: BIT_OP(SET, 2, A);						break; // SET 2, A
				case 0xD8: BIT_OP(SET, 3, B);						break; // SET 3, B
				case 0xD9: BIT_OP(SET, 3, C);						break; // SET 3, C
				case 0xDA: BIT_OP(SET, 3, D);						break; // SET 3, D
				case 0xDB: BIT_OP(SET, 3, E);						break; // SET 3, E
				case 0xDC: BIT_OP(SET, 3, H);						break; // SET 3, H
				case 0xDD: BIT_OP(SET, 3, L);						break; // SET 3, L
				case 0xDE: BIT_OP_IND(SET, 3, L, H);				break; // SET 3, (HL)
				case 0xDF: BIT_OP(SET, 3, A);						break; // SET 3, A
				case 0xE0: BIT_OP(SET, 4, B);						break; // SET 4, B
				case 0xE1: BIT_OP(SET, 4, C);						break; // SET 4, C
				case 0xE2: BIT_OP(SET, 4, D);						break; // SET 4, D
				case 0xE3: BIT_OP(SET, 4, E);						break; // SET 4, E
				case 0xE4: BIT_OP(SET, 4, H);						break; // SET 4, H
				case 0xE5: BIT_OP(SET, 4, L);						break; // SET 4, L
				case 0xE6: BIT_OP_IND(SET, 4, L, H);				break; // SET 4, (HL)
				case 0xE7: BIT_OP(SET, 4, A);						break; // SET 4, A
				case 0xE8: BIT_OP(SET, 5, B);						break; // SET 5, B
				case 0xE9: BIT_OP(SET, 5, C);						break; // SET 5, C
				case 0xEA: BIT_OP(SET, 5, D);						break; // SET 5, D
				case 0xEB: BIT_OP(SET, 5, E);						break; // SET 5, E
				case 0xEC: BIT_OP(SET, 5, H);						break; // SET 5, H
				case 0xED: BIT_OP(SET, 5, L);						break; // SET 5, L
				case 0xEE: BIT_OP_IND(SET, 5, L, H);				break; // SET 5, (HL)
				case 0xEF: BIT_OP(SET, 5, A);						break; // SET 5, A
				case 0xF0: BIT_OP(SET, 6, B);						break; // SET 6, B
				case 0xF1: BIT_OP(SET, 6, C);						break; // SET 6, C
				case 0xF2: BIT_OP(SET, 6, D);						break; // SET 6, D
				case 0xF3: BIT_OP(SET, 6, E);						break; // SET 6, E
				case 0xF4: BIT_OP(SET, 6, H);						break; // SET 6, H
				case 0xF5: BIT_OP(SET, 6, L);						break; // SET 6, L
				case 0xF6: BIT_OP_IND(SET, 6, L, H);				break; // SET 6, (HL)
				case 0xF7: BIT_OP(SET, 6, A);						break; // SET 6, A
				case 0xF8: BIT_OP(SET, 7, B);						break; // SET 7, B
				case 0xF9: BIT_OP(SET, 7, C);						break; // SET 7, C
				case 0xFA: BIT_OP(SET, 7, D);						break; // SET 7, D
				case 0xFB: BIT_OP(SET, 7, E);						break; // SET 7, E
				case 0xFC: BIT_OP(SET, 7, H);						break; // SET 7, H
				case 0xFD: BIT_OP(SET, 7, L);						break; // SET 7, L
				case 0xFE: BIT_OP_IND(SET, 7, L, H);				break; // SET 7, (HL)
				case 0xFF: BIT_OP(SET, 7, A);						break; // SET 7, A
				}
			}
			else if (EXTD_prefix)
			{
				// NOTE: Much of EXTD is empty
				EXTD_prefix = false;
				NO_prefix = true;

				switch (opcode)
				{
				case 0x40: IN_REG_(B, C);							break; // IN B, (C)
				case 0x41: OUT_REG_(C, B);							break; // OUT (C), B
				case 0x42: REG_OP_16_(SBC16, L, H, C, B);			break; // SBC HL, BC
				case 0x43: LD_16_IND_nn(C, B);						break; // LD (nn), BC
				case 0x44: INT_OP(NEG, A);							break; // NEG
				case 0x45: RETN_();									break; // RETN
				case 0x46: INT_MODE_(0);							break; // IM $0
				case 0x47: REG_OP_IR(TR, I, A);						break; // LD I, A
				case 0x48: IN_REG_(C, C);							break; // IN C, (C)
				case 0x49: OUT_REG_(C, C);							break; // OUT (C), C
				case 0x4A: REG_OP_16_(ADC16, L, H, C, B);			break; // ADC HL, BC
				case 0x4B: LD_IND_16_nn(C, B);						break; // LD BC, (nn)
				case 0x4C: INT_OP(NEG, A);							break; // NEG
				case 0x4D: RETI_();									break; // RETI
				case 0x4E: INT_MODE_(0);							break; // IM $0
				case 0x4F: REG_OP_IR(TR, R, A);						break; // LD R, A
				case 0x50: IN_REG_(D, C);							break; // IN D, (C)
				case 0x51: OUT_REG_(C, D);							break; // OUT (C), D
				case 0x52: REG_OP_16_(SBC16, L, H, E, D);			break; // SBC HL, DE
				case 0x53: LD_16_IND_nn(E, D);						break; // LD (nn), DE
				case 0x54: INT_OP(NEG, A);							break; // NEG
				case 0x55: RETN_();									break; // RETN
				case 0x56: INT_MODE_(1); 							break; // IM $1
				case 0x57: REG_OP_IR(TR, A, I);						break; // LD A, I
				case 0x58: IN_REG_(E, C);							break; // IN E, (C)
				case 0x59: OUT_REG_(C, E);							break; // OUT (C), E
				case 0x5A: REG_OP_16_(ADC16, L, H, E, D);			break; // ADC HL, DE
				case 0x5B: LD_IND_16_nn(E, D);						break; // LD DE, (nn)
				case 0x5C: INT_OP(NEG, A);							break; // NEG
				case 0x5D: RETN_();									break; // RETI
				case 0x5E: INT_MODE_(2);							break; // IM $0
				case 0x5F: REG_OP_IR(TR, A, R);						break; // LD A, R
				case 0x60: IN_REG_(H, C);							break; // IN H, (C)
				case 0x61: OUT_REG_(C, H);							break; // OUT (C), H
				case 0x62: REG_OP_16_(SBC16, L, H, L, H);			break; // SBC HL, HL
				case 0x63: LD_16_IND_nn(L, H);						break; // LD (nn), HL
				case 0x64: INT_OP(NEG, A);							break; // NEG
				case 0x65: RETN_();									break; // RETN
				case 0x66: INT_MODE_(0);							break; // IM $0
				case 0x67: RRD_();									break; // RRD
				case 0x68: IN_REG_(L, C);							break; // IN L, (C)
				case 0x69: OUT_REG_(C, L);							break; // OUT (C), L
				case 0x6A: REG_OP_16_(ADC16, L, H, L, H);			break; // ADC HL, HL
				case 0x6B: LD_IND_16_nn(L, H);						break; // LD HL, (nn)
				case 0x6C: INT_OP(NEG, A);							break; // NEG
				case 0x6D: RETN_();									break; // RETI
				case 0x6E: INT_MODE_(0);							break; // IM $0
				case 0x6F: RLD_();									break; // LD R, A
				case 0x70: IN_REG_(ALU, C);							break; // IN 0, (C)
				case 0x71: OUT_REG_(C, ZERO);						break; // OUT (C), 0
				case 0x72: REG_OP_16_(SBC16, L, H, SPl, SPh);		break; // SBC HL, SP
				case 0x73: LD_16_IND_nn(SPl, SPh);					break; // LD (nn), SP
				case 0x74: INT_OP(NEG, A);							break; // NEG
				case 0x75: RETN_();									break; // RETN
				case 0x76: INT_MODE_(1);							break; // IM $1
				case 0x77: NOP_();									break; // NOP
				case 0x78: IN_REG_(A, C);							break; // IN A, (C)
				case 0x79: OUT_REG_(C, A);							break; // OUT (C), A
				case 0x7A: REG_OP_16_(ADC16, L, H, SPl, SPh);		break; // ADC HL, SP
				case 0x7B: LD_IND_16_nn(SPl, SPh);					break; // LD SP, (nn)
				case 0x7C: INT_OP(NEG, A);							break; // NEG
				case 0x7D: RETN_();									break; // RETI
				case 0x7E: INT_MODE_(2);							break; // IM $2
				case 0x7F: NOP_();									break; // NOP
				case 0xA0: LD_OP_R(INC16, 0);						break; // LDI
				case 0xA1: CP_OP_R(INC16, 0);						break; // CPI
				case 0xA2: IN_OP_R(INC16, 0);						break; // INI
				case 0xA3: OUT_OP_R(INC16, 0);						break; // OUTI
				case 0xA8: LD_OP_R(DEC16, 0);						break; // LDD
				case 0xA9: CP_OP_R(DEC16, 0);						break; // CPD
				case 0xAA: IN_OP_R(DEC16, 0);						break; // IND
				case 0xAB: OUT_OP_R(DEC16, 0);						break; // OUTD
				case 0xB0: LD_OP_R(INC16, 1);						break; // LDIR
				case 0xB1: CP_OP_R(INC16, 1);						break; // CPIR
				case 0xB2: IN_OP_R(INC16, 1);						break; // INIR
				case 0xB3: OUT_OP_R(INC16, 1);						break; // OTIR
				case 0xB8: LD_OP_R(DEC16, 1);						break; // LDDR
				case 0xB9: CP_OP_R(DEC16, 1);						break; // CPDR
				case 0xBA: IN_OP_R(DEC16, 1);						break; // INDR
				case 0xBB: OUT_OP_R(DEC16, 1);						break; // OTDR
				default: NOP_();									break; // NOP

				}
			}
			else if (IX_prefix)
			{
				IX_prefix = false;
				NO_prefix = true;

				switch (opcode)
				{
				case 0x00: NOP_();									break; // NOP
				case 0x01: LD_IND_16(C, B, PCl, PCh);				break; // LD BC, nn
				case 0x02: LD_8_IND(C, B, A);						break; // LD (BC), A
				case 0x03: INC_16(C, B);							break; // INC BC
				case 0x04: INT_OP(INC8, B);							break; // INC B
				case 0x05: INT_OP(DEC8, B);							break; // DEC B
				case 0x06: LD_IND_8_INC(B, PCl, PCh);				break; // LD B, n
				case 0x07: INT_OP(RLC, Aim);						break; // RLCA
				case 0x08: EXCH_();									break; // EXCH AF, AF'
				case 0x09: ADD_16(Ixl, Ixh, C, B);					break; // ADD Ix, BC
				case 0x0A: REG_OP_IND(TR, A, C, B);					break; // LD A, (BC)
				case 0x0B: DEC_16(C, B);							break; // DEC BC
				case 0x0C: INT_OP(INC8, C);							break; // INC C
				case 0x0D: INT_OP(DEC8, C);							break; // DEC C
				case 0x0E: LD_IND_8_INC(C, PCl, PCh);				break; // LD C, n
				case 0x0F: INT_OP(RRC, Aim);						break; // RRCA
				case 0x10: DJNZ_();									break; // DJNZ B
				case 0x11: LD_IND_16(E, D, PCl, PCh);				break; // LD DE, nn
				case 0x12: LD_8_IND(E, D, A);						break; // LD (DE), A
				case 0x13: INC_16(E, D);							break; // INC DE
				case 0x14: INT_OP(INC8, D);							break; // INC D
				case 0x15: INT_OP(DEC8, D);							break; // DEC D
				case 0x16: LD_IND_8_INC(D, PCl, PCh);				break; // LD D, n
				case 0x17: INT_OP(RL, Aim);							break; // RLA
				case 0x18: JR_COND(true);							break; // JR, r8
				case 0x19: ADD_16(Ixl, Ixh, E, D);					break; // ADD Ix, DE
				case 0x1A: REG_OP_IND(TR, A, E, D);					break; // LD A, (DE)
				case 0x1B: DEC_16(E, D);							break; // DEC DE
				case 0x1C: INT_OP(INC8, E);							break; // INC E
				case 0x1D: INT_OP(DEC8, E);							break; // DEC E
				case 0x1E: LD_IND_8_INC(E, PCl, PCh);				break; // LD E, n
				case 0x1F: INT_OP(RR, Aim);							break; // RRA
				case 0x20: JR_COND(!FlagZget());					break; // JR NZ, r8
				case 0x21: LD_IND_16(Ixl, Ixh, PCl, PCh);			break; // LD Ix, nn
				case 0x22: LD_16_IND_nn(Ixl, Ixh);					break; // LD (nn), Ix
				case 0x23: INC_16(Ixl, Ixh);						break; // INC Ix
				case 0x24: INT_OP(INC8, Ixh);						break; // INC Ixh
				case 0x25: INT_OP(DEC8, Ixh);						break; // DEC Ixh
				case 0x26: LD_IND_8_INC(Ixh, PCl, PCh);				break; // LD Ixh, n
				case 0x27: INT_OP(DA, A);							break; // DAA
				case 0x28: JR_COND(FlagZget());						break; // JR Z, r8
				case 0x29: ADD_16(Ixl, Ixh, Ixl, Ixh);				break; // ADD Ix, Ix
				case 0x2A: LD_IND_16_nn(Ixl, Ixh);					break; // LD Ix, (nn)
				case 0x2B: DEC_16(Ixl, Ixh);						break; // DEC Ix
				case 0x2C: INT_OP(INC8, Ixl);						break; // INC Ixl
				case 0x2D: INT_OP(DEC8, Ixl);						break; // DEC Ixl
				case 0x2E: LD_IND_8_INC(Ixl, PCl, PCh);				break; // LD Ixl, n
				case 0x2F: INT_OP(CPL, A);							break; // CPL
				case 0x30: JR_COND(!FlagCget());					break; // JR NC, r8
				case 0x31: LD_IND_16(SPl, SPh, PCl, PCh);			break; // LD SP, nn
				case 0x32: LD_8_IND_nn(A);							break; // LD (nn), A
				case 0x33: INC_16(SPl, SPh);						break; // INC SP
				case 0x34: I_OP_n(INC8, Ixl, Ixh);					break; // INC (Ix + n)
				case 0x35: I_OP_n(DEC8, Ixl, Ixh);					break; // DEC (Ix + n)
				case 0x36: I_OP_n_n(Ixl, Ixh);						break; // LD (Ix + n), n
				case 0x37: INT_OP(SCF, A);							break; // SCF
				case 0x38: JR_COND(FlagCget());						break; // JR C, r8
				case 0x39: ADD_16(Ixl, Ixh, SPl, SPh);				break; // ADD Ix, SP
				case 0x3A: LD_IND_8_nn(A);							break; // LD A, (nn)
				case 0x3B: DEC_16(SPl, SPh);						break; // DEC SP
				case 0x3C: INT_OP(INC8, A);							break; // INC A
				case 0x3D: INT_OP(DEC8, A);							break; // DEC A
				case 0x3E: LD_IND_8_INC(A, PCl, PCh);				break; // LD A, n
				case 0x3F: INT_OP(CCF, A);							break; // CCF
				case 0x40: REG_OP(TR, B, B);						break; // LD B, B
				case 0x41: REG_OP(TR, B, C);						break; // LD B, C
				case 0x42: REG_OP(TR, B, D);						break; // LD B, D
				case 0x43: REG_OP(TR, B, E);						break; // LD B, E
				case 0x44: REG_OP(TR, B, Ixh);						break; // LD B, Ixh
				case 0x45: REG_OP(TR, B, Ixl);						break; // LD B, Ixl
				case 0x46: I_REG_OP_IND_n(TR, B, Ixl, Ixh);			break; // LD B, (Ix + n)
				case 0x47: REG_OP(TR, B, A);						break; // LD B, A
				case 0x48: REG_OP(TR, C, B);						break; // LD C, B
				case 0x49: REG_OP(TR, C, C);						break; // LD C, C
				case 0x4A: REG_OP(TR, C, D);						break; // LD C, D
				case 0x4B: REG_OP(TR, C, E);						break; // LD C, E
				case 0x4C: REG_OP(TR, C, Ixh);						break; // LD C, Ixh
				case 0x4D: REG_OP(TR, C, Ixl);						break; // LD C, Ixl
				case 0x4E: I_REG_OP_IND_n(TR, C, Ixl, Ixh);			break; // LD C, (Ix + n)
				case 0x4F: REG_OP(TR, C, A);						break; // LD C, A
				case 0x50: REG_OP(TR, D, B);						break; // LD D, B
				case 0x51: REG_OP(TR, D, C);						break; // LD D, C
				case 0x52: REG_OP(TR, D, D);						break; // LD D, D
				case 0x53: REG_OP(TR, D, E);						break; // LD D, E
				case 0x54: REG_OP(TR, D, Ixh);						break; // LD D, Ixh
				case 0x55: REG_OP(TR, D, Ixl);						break; // LD D, Ixl
				case 0x56: I_REG_OP_IND_n(TR, D, Ixl, Ixh);			break; // LD D, (Ix + n)
				case 0x57: REG_OP(TR, D, A);						break; // LD D, A
				case 0x58: REG_OP(TR, E, B);						break; // LD E, B
				case 0x59: REG_OP(TR, E, C);						break; // LD E, C
				case 0x5A: REG_OP(TR, E, D);						break; // LD E, D
				case 0x5B: REG_OP(TR, E, E);						break; // LD E, E
				case 0x5C: REG_OP(TR, E, Ixh);						break; // LD E, Ixh
				case 0x5D: REG_OP(TR, E, Ixl);						break; // LD E, Ixl
				case 0x5E: I_REG_OP_IND_n(TR, E, Ixl, Ixh);			break; // LD E, (Ix + n)
				case 0x5F: REG_OP(TR, E, A);						break; // LD E, A
				case 0x60: REG_OP(TR, Ixh, B);						break; // LD Ixh, B
				case 0x61: REG_OP(TR, Ixh, C);						break; // LD Ixh, C
				case 0x62: REG_OP(TR, Ixh, D);						break; // LD Ixh, D
				case 0x63: REG_OP(TR, Ixh, E);						break; // LD Ixh, E
				case 0x64: REG_OP(TR, Ixh, Ixh);					break; // LD Ixh, Ixh
				case 0x65: REG_OP(TR, Ixh, Ixl);					break; // LD Ixh, Ixl
				case 0x66: I_REG_OP_IND_n(TR, H, Ixl, Ixh);			break; // LD H, (Ix + n)
				case 0x67: REG_OP(TR, Ixh, A);						break; // LD Ixh, A
				case 0x68: REG_OP(TR, Ixl, B);						break; // LD Ixl, B
				case 0x69: REG_OP(TR, Ixl, C);						break; // LD Ixl, C
				case 0x6A: REG_OP(TR, Ixl, D);						break; // LD Ixl, D
				case 0x6B: REG_OP(TR, Ixl, E);						break; // LD Ixl, E
				case 0x6C: REG_OP(TR, Ixl, Ixh);					break; // LD Ixl, Ixh
				case 0x6D: REG_OP(TR, Ixl, Ixl);					break; // LD Ixl, Ixl
				case 0x6E: I_REG_OP_IND_n(TR, L, Ixl, Ixh);			break; // LD L, (Ix + n)
				case 0x6F: REG_OP(TR, Ixl, A);						break; // LD Ixl, A
				case 0x70: I_LD_8_IND_n(Ixl, Ixh, B);				break; // LD (Ix + n), B
				case 0x71: I_LD_8_IND_n(Ixl, Ixh, C);				break; // LD (Ix + n), C
				case 0x72: I_LD_8_IND_n(Ixl, Ixh, D);				break; // LD (Ix + n), D
				case 0x73: I_LD_8_IND_n(Ixl, Ixh, E);				break; // LD (Ix + n), E
				case 0x74: I_LD_8_IND_n(Ixl, Ixh, H);				break; // LD (Ix + n), H
				case 0x75: I_LD_8_IND_n(Ixl, Ixh, L);				break; // LD (Ix + n), L
				case 0x76: HALT_();									break; // HALT
				case 0x77: I_LD_8_IND_n(Ixl, Ixh, A);				break; // LD (Ix + n), A
				case 0x78: REG_OP(TR, A, B);						break; // LD A, B
				case 0x79: REG_OP(TR, A, C);						break; // LD A, C
				case 0x7A: REG_OP(TR, A, D);						break; // LD A, D
				case 0x7B: REG_OP(TR, A, E);						break; // LD A, E
				case 0x7C: REG_OP(TR, A, Ixh);						break; // LD A, Ixh
				case 0x7D: REG_OP(TR, A, Ixl);						break; // LD A, Ixl
				case 0x7E: I_REG_OP_IND_n(TR, A, Ixl, Ixh);			break; // LD A, (Ix + n)
				case 0x7F: REG_OP(TR, A, A);						break; // LD A, A
				case 0x80: REG_OP(ADD8, A, B);						break; // ADD A, B
				case 0x81: REG_OP(ADD8, A, C);						break; // ADD A, C
				case 0x82: REG_OP(ADD8, A, D);						break; // ADD A, D
				case 0x83: REG_OP(ADD8, A, E);						break; // ADD A, E
				case 0x84: REG_OP(ADD8, A, Ixh);					break; // ADD A, Ixh
				case 0x85: REG_OP(ADD8, A, Ixl);					break; // ADD A, Ixl
				case 0x86: I_REG_OP_IND_n(ADD8, A, Ixl, Ixh);		break; // ADD A, (Ix + n)
				case 0x87: REG_OP(ADD8, A, A);						break; // ADD A, A
				case 0x88: REG_OP(ADC8, A, B);						break; // ADC A, B
				case 0x89: REG_OP(ADC8, A, C);						break; // ADC A, C
				case 0x8A: REG_OP(ADC8, A, D);						break; // ADC A, D
				case 0x8B: REG_OP(ADC8, A, E);						break; // ADC A, E
				case 0x8C: REG_OP(ADC8, A, Ixh);					break; // ADC A, Ixh
				case 0x8D: REG_OP(ADC8, A, Ixl);					break; // ADC A, Ixl
				case 0x8E: I_REG_OP_IND_n(ADC8, A, Ixl, Ixh);		break; // ADC A, (Ix + n)
				case 0x8F: REG_OP(ADC8, A, A);						break; // ADC A, A
				case 0x90: REG_OP(SUB8, A, B);						break; // SUB A, B
				case 0x91: REG_OP(SUB8, A, C);						break; // SUB A, C
				case 0x92: REG_OP(SUB8, A, D);						break; // SUB A, D
				case 0x93: REG_OP(SUB8, A, E);						break; // SUB A, E
				case 0x94: REG_OP(SUB8, A, Ixh);					break; // SUB A, Ixh
				case 0x95: REG_OP(SUB8, A, Ixl);					break; // SUB A, Ixl
				case 0x96: I_REG_OP_IND_n(SUB8, A, Ixl, Ixh);		break; // SUB A, (Ix + n)
				case 0x97: REG_OP(SUB8, A, A);						break; // SUB A, A
				case 0x98: REG_OP(SBC8, A, B);						break; // SBC A, B
				case 0x99: REG_OP(SBC8, A, C);						break; // SBC A, C
				case 0x9A: REG_OP(SBC8, A, D);						break; // SBC A, D
				case 0x9B: REG_OP(SBC8, A, E);						break; // SBC A, E
				case 0x9C: REG_OP(SBC8, A, Ixh);					break; // SBC A, Ixh
				case 0x9D: REG_OP(SBC8, A, Ixl);					break; // SBC A, Ixl
				case 0x9E: I_REG_OP_IND_n(SBC8, A, Ixl, Ixh);		break; // SBC A, (Ix + n)
				case 0x9F: REG_OP(SBC8, A, A);						break; // SBC A, A
				case 0xA0: REG_OP(AND8, A, B);						break; // AND A, B
				case 0xA1: REG_OP(AND8, A, C);						break; // AND A, C
				case 0xA2: REG_OP(AND8, A, D);						break; // AND A, D
				case 0xA3: REG_OP(AND8, A, E);						break; // AND A, E
				case 0xA4: REG_OP(AND8, A, Ixh);					break; // AND A, Ixh
				case 0xA5: REG_OP(AND8, A, Ixl);					break; // AND A, Ixl
				case 0xA6: I_REG_OP_IND_n(AND8, A, Ixl, Ixh);		break; // AND A, (Ix + n)
				case 0xA7: REG_OP(AND8, A, A);						break; // AND A, A
				case 0xA8: REG_OP(XOR8, A, B);						break; // XOR A, B
				case 0xA9: REG_OP(XOR8, A, C);						break; // XOR A, C
				case 0xAA: REG_OP(XOR8, A, D);						break; // XOR A, D
				case 0xAB: REG_OP(XOR8, A, E);						break; // XOR A, E
				case 0xAC: REG_OP(XOR8, A, Ixh);					break; // XOR A, Ixh
				case 0xAD: REG_OP(XOR8, A, Ixl);					break; // XOR A, Ixl
				case 0xAE: I_REG_OP_IND_n(XOR8, A, Ixl, Ixh);		break; // XOR A, (Ix + n)
				case 0xAF: REG_OP(XOR8, A, A);						break; // XOR A, A
				case 0xB0: REG_OP(OR8, A, B);						break; // OR A, B
				case 0xB1: REG_OP(OR8, A, C);						break; // OR A, C
				case 0xB2: REG_OP(OR8, A, D);						break; // OR A, D
				case 0xB3: REG_OP(OR8, A, E);						break; // OR A, E
				case 0xB4: REG_OP(OR8, A, Ixh);						break; // OR A, Ixh
				case 0xB5: REG_OP(OR8, A, Ixl);						break; // OR A, Ixl
				case 0xB6: I_REG_OP_IND_n(OR8, A, Ixl, Ixh);		break; // OR A, (Ix + n)
				case 0xB7: REG_OP(OR8, A, A);						break; // OR A, A
				case 0xB8: REG_OP(CP8, A, B);						break; // CP A, B
				case 0xB9: REG_OP(CP8, A, C);						break; // CP A, C
				case 0xBA: REG_OP(CP8, A, D);						break; // CP A, D
				case 0xBB: REG_OP(CP8, A, E);						break; // CP A, E
				case 0xBC: REG_OP(CP8, A, Ixh);						break; // CP A, Ixh
				case 0xBD: REG_OP(CP8, A, Ixl);						break; // CP A, Ixl
				case 0xBE: I_REG_OP_IND_n(CP8, A, Ixl, Ixh);		break; // CP A, (Ix + n)
				case 0xBF: REG_OP(CP8, A, A);						break; // CP A, A
				case 0xC0: RET_COND(!FlagZget());					break; // Ret NZ
				case 0xC1: POP_(C, B);								break; // POP BC
				case 0xC2: JP_COND(!FlagZget());					break; // JP NZ
				case 0xC3: JP_COND(true);							break; // JP
				case 0xC4: CALL_COND(!FlagZget());					break; // CALL NZ
				case 0xC5: PUSH_(C, B);								break; // PUSH BC
				case 0xC6: REG_OP_IND_INC(ADD8, A, PCl, PCh);		break; // ADD A, n
				case 0xC7: RST_(0);									break; // RST 0
				case 0xC8: RET_COND(FlagZget());					break; // RET Z
				case 0xC9: RET_();									break; // RET
				case 0xCA: JP_COND(FlagZget());						break; // JP Z
				case 0xCB: PREFETCH_(IXCBpre);						break; // PREFIX IXCB
				case 0xCC: CALL_COND(FlagZget());					break; // CALL Z
				case 0xCD: CALL_COND(true);							break; // CALL
				case 0xCE: REG_OP_IND_INC(ADC8, A, PCl, PCh);		break; // ADC A, n
				case 0xCF: RST_(0x08);								break; // RST 0x08
				case 0xD0: RET_COND(!FlagCget());					break; // Ret NC
				case 0xD1: POP_(E, D);								break; // POP DE
				case 0xD2: JP_COND(!FlagCget());					break; // JP NC
				case 0xD3: OUT_();									break; // OUT A
				case 0xD4: CALL_COND(!FlagCget());					break; // CALL NC
				case 0xD5: PUSH_(E, D);								break; // PUSH DE
				case 0xD6: REG_OP_IND_INC(SUB8, A, PCl, PCh);		break; // SUB A, n
				case 0xD7: RST_(0x10);								break; // RST 0x10
				case 0xD8: RET_COND(FlagCget());					break; // RET C
				case 0xD9: EXX_();									break; // EXX
				case 0xDA: JP_COND(FlagCget());						break; // JP C
				case 0xDB: IN_();									break; // IN A
				case 0xDC: CALL_COND(FlagCget());					break; // CALL C
				case 0xDD: PREFIX_(IXpre);							break; // IX Prefix
				case 0xDE: REG_OP_IND_INC(SBC8, A, PCl, PCh);		break; // SBC A, n
				case 0xDF: RST_(0x18);								break; // RST 0x18
				case 0xE0: RET_COND(!FlagPget());					break; // RET Po
				case 0xE1: POP_(Ixl, Ixh);							break; // POP Ix
				case 0xE2: JP_COND(!FlagPget());					break; // JP Po
				case 0xE3: EXCH_16_IND_(SPl, SPh, Ixl, Ixh);		break; // ex (SP), Ix
				case 0xE4: CALL_COND(!FlagPget());					break; // CALL Po
				case 0xE5: PUSH_(Ixl, Ixh);							break; // PUSH Ix
				case 0xE6: REG_OP_IND_INC(AND8, A, PCl, PCh);		break; // AND A, n
				case 0xE7: RST_(0x20);								break; // RST 0x20
				case 0xE8: RET_COND(FlagPget());					break; // RET Pe
				case 0xE9: JP_16(Ixl, Ixh);							break; // JP (Ix)
				case 0xEA: JP_COND(FlagPget());						break; // JP Pe
				case 0xEB: EXCH_16_(E, D, L, H);					break; // ex DE, HL
				case 0xEC: CALL_COND(FlagPget());					break; // CALL Pe
				case 0xED: PREFIX_(EXTDpre);						break; // EXTD Prefix
				case 0xEE: REG_OP_IND_INC(XOR8, A, PCl, PCh);		break; // XOR A, n
				case 0xEF: RST_(0x28);								break; // RST 0x28
				case 0xF0: RET_COND(!FlagSget());					break; // RET p
				case 0xF1: POP_(F, A);								break; // POP AF
				case 0xF2: JP_COND(!FlagSget());					break; // JP p
				case 0xF3: DI_();									break; // DI
				case 0xF4: CALL_COND(!FlagSget());					break; // CALL p
				case 0xF5: PUSH_(F, A);								break; // PUSH AF
				case 0xF6: REG_OP_IND_INC(OR8, A, PCl, PCh);		break; // OR A, n
				case 0xF7: RST_(0x30);								break; // RST 0x30
				case 0xF8: RET_COND(FlagSget());					break; // RET M
				case 0xF9: LD_SP_16(Ixl, Ixh);						break; // LD SP, Ix
				case 0xFA: JP_COND(FlagSget());						break; // JP M
				case 0xFB: EI_();									break; // EI
				case 0xFC: CALL_COND(FlagSget());					break; // CALL M
				case 0xFD: PREFIX_(IYpre);							break; // IY Prefix
				case 0xFE: REG_OP_IND_INC(CP8, A, PCl, PCh);		break; // CP A, n
				case 0xFF: RST_(0x38);								break; // RST $38
				}
			}
			else if (IY_prefix)
			{
				IY_prefix = false;
				NO_prefix = true;

				switch (opcode)
				{
				case 0x00: NOP_();									break; // NOP
				case 0x01: LD_IND_16(C, B, PCl, PCh);				break; // LD BC, nn
				case 0x02: LD_8_IND(C, B, A);						break; // LD (BC), A
				case 0x03: INC_16(C, B);							break; // INC BC
				case 0x04: INT_OP(INC8, B);							break; // INC B
				case 0x05: INT_OP(DEC8, B);							break; // DEC B
				case 0x06: LD_IND_8_INC(B, PCl, PCh);				break; // LD B, n
				case 0x07: INT_OP(RLC, Aim);						break; // RLCA
				case 0x08: EXCH_();									break; // EXCH AF, AF'
				case 0x09: ADD_16(Iyl, Iyh, C, B);					break; // ADD Iy, BC
				case 0x0A: REG_OP_IND(TR, A, C, B);					break; // LD A, (BC)
				case 0x0B: DEC_16(C, B);							break; // DEC BC
				case 0x0C: INT_OP(INC8, C);							break; // INC C
				case 0x0D: INT_OP(DEC8, C);							break; // DEC C
				case 0x0E: LD_IND_8_INC(C, PCl, PCh);				break; // LD C, n
				case 0x0F: INT_OP(RRC, Aim);						break; // RRCA
				case 0x10: DJNZ_();									break; // DJNZ B
				case 0x11: LD_IND_16(E, D, PCl, PCh);				break; // LD DE, nn
				case 0x12: LD_8_IND(E, D, A);						break; // LD (DE), A
				case 0x13: INC_16(E, D);							break; // INC DE
				case 0x14: INT_OP(INC8, D);							break; // INC D
				case 0x15: INT_OP(DEC8, D);							break; // DEC D
				case 0x16: LD_IND_8_INC(D, PCl, PCh);				break; // LD D, n
				case 0x17: INT_OP(RL, Aim);							break; // RLA
				case 0x18: JR_COND(true);							break; // JR, r8
				case 0x19: ADD_16(Iyl, Iyh, E, D);					break; // ADD Iy, DE
				case 0x1A: REG_OP_IND(TR, A, E, D);					break; // LD A, (DE)
				case 0x1B: DEC_16(E, D);							break; // DEC DE
				case 0x1C: INT_OP(INC8, E);							break; // INC E
				case 0x1D: INT_OP(DEC8, E);							break; // DEC E
				case 0x1E: LD_IND_8_INC(E, PCl, PCh);				break; // LD E, n
				case 0x1F: INT_OP(RR, Aim);							break; // RRA
				case 0x20: JR_COND(!FlagZget());					break; // JR NZ, r8
				case 0x21: LD_IND_16(Iyl, Iyh, PCl, PCh);			break; // LD Iy, nn
				case 0x22: LD_16_IND_nn(Iyl, Iyh);					break; // LD (nn), Iy
				case 0x23: INC_16(Iyl, Iyh);						break; // INC Iy
				case 0x24: INT_OP(INC8, Iyh);						break; // INC Iyh
				case 0x25: INT_OP(DEC8, Iyh);						break; // DEC Iyh
				case 0x26: LD_IND_8_INC(Iyh, PCl, PCh);				break; // LD Iyh, n
				case 0x27: INT_OP(DA, A);							break; // DAA
				case 0x28: JR_COND(FlagZget());						break; // JR Z, r8
				case 0x29: ADD_16(Iyl, Iyh, Iyl, Iyh);				break; // ADD Iy, Iy
				case 0x2A: LD_IND_16_nn(Iyl, Iyh);					break; // LD Iy, (nn)
				case 0x2B: DEC_16(Iyl, Iyh);						break; // DEC Iy
				case 0x2C: INT_OP(INC8, Iyl);						break; // INC Iyl
				case 0x2D: INT_OP(DEC8, Iyl);						break; // DEC Iyl
				case 0x2E: LD_IND_8_INC(Iyl, PCl, PCh);				break; // LD Iyl, n
				case 0x2F: INT_OP(CPL, A);							break; // CPL
				case 0x30: JR_COND(!FlagCget());					break; // JR NC, r8
				case 0x31: LD_IND_16(SPl, SPh, PCl, PCh);			break; // LD SP, nn
				case 0x32: LD_8_IND_nn(A);							break; // LD (nn), A
				case 0x33: INC_16(SPl, SPh);						break; // INC SP
				case 0x34: I_OP_n(INC8, Iyl, Iyh);					break; // INC (Iy + n)
				case 0x35: I_OP_n(DEC8, Iyl, Iyh);					break; // DEC (Iy + n)
				case 0x36: I_OP_n_n(Iyl, Iyh);						break; // LD (Iy + n), n
				case 0x37: INT_OP(SCF, A);							break; // SCF
				case 0x38: JR_COND(FlagCget());						break; // JR C, r8
				case 0x39: ADD_16(Iyl, Iyh, SPl, SPh);				break; // ADD Iy, SP
				case 0x3A: LD_IND_8_nn(A);							break; // LD A, (nn)
				case 0x3B: DEC_16(SPl, SPh);						break; // DEC SP
				case 0x3C: INT_OP(INC8, A);							break; // INC A
				case 0x3D: INT_OP(DEC8, A);							break; // DEC A
				case 0x3E: LD_IND_8_INC(A, PCl, PCh);				break; // LD A, n
				case 0x3F: INT_OP(CCF, A);							break; // CCF
				case 0x40: REG_OP(TR, B, B);						break; // LD B, B
				case 0x41: REG_OP(TR, B, C);						break; // LD B, C
				case 0x42: REG_OP(TR, B, D);						break; // LD B, D
				case 0x43: REG_OP(TR, B, E);						break; // LD B, E
				case 0x44: REG_OP(TR, B, Iyh);						break; // LD B, Iyh
				case 0x45: REG_OP(TR, B, Iyl);						break; // LD B, Iyl
				case 0x46: I_REG_OP_IND_n(TR, B, Iyl, Iyh);			break; // LD B, (Iy + n)
				case 0x47: REG_OP(TR, B, A);						break; // LD B, A
				case 0x48: REG_OP(TR, C, B);						break; // LD C, B
				case 0x49: REG_OP(TR, C, C);						break; // LD C, C
				case 0x4A: REG_OP(TR, C, D);						break; // LD C, D
				case 0x4B: REG_OP(TR, C, E);						break; // LD C, E
				case 0x4C: REG_OP(TR, C, Iyh);						break; // LD C, Iyh
				case 0x4D: REG_OP(TR, C, Iyl);						break; // LD C, Iyl
				case 0x4E: I_REG_OP_IND_n(TR, C, Iyl, Iyh);			break; // LD C, (Iy + n)
				case 0x4F: REG_OP(TR, C, A);						break; // LD C, A
				case 0x50: REG_OP(TR, D, B);						break; // LD D, B
				case 0x51: REG_OP(TR, D, C);						break; // LD D, C
				case 0x52: REG_OP(TR, D, D);						break; // LD D, D
				case 0x53: REG_OP(TR, D, E);						break; // LD D, E
				case 0x54: REG_OP(TR, D, Iyh);						break; // LD D, Iyh
				case 0x55: REG_OP(TR, D, Iyl);						break; // LD D, Iyl
				case 0x56: I_REG_OP_IND_n(TR, D, Iyl, Iyh);			break; // LD D, (Iy + n)
				case 0x57: REG_OP(TR, D, A);						break; // LD D, A
				case 0x58: REG_OP(TR, E, B);						break; // LD E, B
				case 0x59: REG_OP(TR, E, C);						break; // LD E, C
				case 0x5A: REG_OP(TR, E, D);						break; // LD E, D
				case 0x5B: REG_OP(TR, E, E);						break; // LD E, E
				case 0x5C: REG_OP(TR, E, Iyh);						break; // LD E, Iyh
				case 0x5D: REG_OP(TR, E, Iyl);						break; // LD E, Iyl
				case 0x5E: I_REG_OP_IND_n(TR, E, Iyl, Iyh);			break; // LD E, (Iy + n)
				case 0x5F: REG_OP(TR, E, A);						break; // LD E, A
				case 0x60: REG_OP(TR, Iyh, B);						break; // LD Iyh, B
				case 0x61: REG_OP(TR, Iyh, C);						break; // LD Iyh, C
				case 0x62: REG_OP(TR, Iyh, D);						break; // LD Iyh, D
				case 0x63: REG_OP(TR, Iyh, E);						break; // LD Iyh, E
				case 0x64: REG_OP(TR, Iyh, Iyh);					break; // LD Iyh, Iyh
				case 0x65: REG_OP(TR, Iyh, Iyl);					break; // LD Iyh, Iyl
				case 0x66: I_REG_OP_IND_n(TR, H, Iyl, Iyh);			break; // LD H, (Iy + n)
				case 0x67: REG_OP(TR, Iyh, A);						break; // LD Iyh, A
				case 0x68: REG_OP(TR, Iyl, B);						break; // LD Iyl, B
				case 0x69: REG_OP(TR, Iyl, C);						break; // LD Iyl, C
				case 0x6A: REG_OP(TR, Iyl, D);						break; // LD Iyl, D
				case 0x6B: REG_OP(TR, Iyl, E);						break; // LD Iyl, E
				case 0x6C: REG_OP(TR, Iyl, Iyh);					break; // LD Iyl, Iyh
				case 0x6D: REG_OP(TR, Iyl, Iyl);					break; // LD Iyl, Iyl
				case 0x6E: I_REG_OP_IND_n(TR, L, Iyl, Iyh);			break; // LD L, (Iy + n)
				case 0x6F: REG_OP(TR, Iyl, A);						break; // LD Iyl, A
				case 0x70: I_LD_8_IND_n(Iyl, Iyh, B);				break; // LD (Iy + n), B
				case 0x71: I_LD_8_IND_n(Iyl, Iyh, C);				break; // LD (Iy + n), C
				case 0x72: I_LD_8_IND_n(Iyl, Iyh, D);				break; // LD (Iy + n), D
				case 0x73: I_LD_8_IND_n(Iyl, Iyh, E);				break; // LD (Iy + n), E
				case 0x74: I_LD_8_IND_n(Iyl, Iyh, H);				break; // LD (Iy + n), H
				case 0x75: I_LD_8_IND_n(Iyl, Iyh, L);				break; // LD (Iy + n), L
				case 0x76: HALT_();									break; // HALT
				case 0x77: I_LD_8_IND_n(Iyl, Iyh, A);				break; // LD (Iy + n), A
				case 0x78: REG_OP(TR, A, B);						break; // LD A, B
				case 0x79: REG_OP(TR, A, C);						break; // LD A, C
				case 0x7A: REG_OP(TR, A, D);						break; // LD A, D
				case 0x7B: REG_OP(TR, A, E);						break; // LD A, E
				case 0x7C: REG_OP(TR, A, Iyh);						break; // LD A, Iyh
				case 0x7D: REG_OP(TR, A, Iyl);						break; // LD A, Iyl
				case 0x7E: I_REG_OP_IND_n(TR, A, Iyl, Iyh);			break; // LD A, (Iy + n)
				case 0x7F: REG_OP(TR, A, A);						break; // LD A, A
				case 0x80: REG_OP(ADD8, A, B);						break; // ADD A, B
				case 0x81: REG_OP(ADD8, A, C);						break; // ADD A, C
				case 0x82: REG_OP(ADD8, A, D);						break; // ADD A, D
				case 0x83: REG_OP(ADD8, A, E);						break; // ADD A, E
				case 0x84: REG_OP(ADD8, A, Iyh);					break; // ADD A, Iyh
				case 0x85: REG_OP(ADD8, A, Iyl);					break; // ADD A, Iyl
				case 0x86: I_REG_OP_IND_n(ADD8, A, Iyl, Iyh);		break; // ADD A, (Iy + n)
				case 0x87: REG_OP(ADD8, A, A);						break; // ADD A, A
				case 0x88: REG_OP(ADC8, A, B);						break; // ADC A, B
				case 0x89: REG_OP(ADC8, A, C);						break; // ADC A, C
				case 0x8A: REG_OP(ADC8, A, D);						break; // ADC A, D
				case 0x8B: REG_OP(ADC8, A, E);						break; // ADC A, E
				case 0x8C: REG_OP(ADC8, A, Iyh);					break; // ADC A, Iyh
				case 0x8D: REG_OP(ADC8, A, Iyl);					break; // ADC A, Iyl
				case 0x8E: I_REG_OP_IND_n(ADC8, A, Iyl, Iyh);		break; // ADC A, (Iy + n)
				case 0x8F: REG_OP(ADC8, A, A);						break; // ADC A, A
				case 0x90: REG_OP(SUB8, A, B);						break; // SUB A, B
				case 0x91: REG_OP(SUB8, A, C);						break; // SUB A, C
				case 0x92: REG_OP(SUB8, A, D);						break; // SUB A, D
				case 0x93: REG_OP(SUB8, A, E);						break; // SUB A, E
				case 0x94: REG_OP(SUB8, A, Iyh);					break; // SUB A, Iyh
				case 0x95: REG_OP(SUB8, A, Iyl);					break; // SUB A, Iyl
				case 0x96: I_REG_OP_IND_n(SUB8, A, Iyl, Iyh);		break; // SUB A, (Iy + n)
				case 0x97: REG_OP(SUB8, A, A);						break; // SUB A, A
				case 0x98: REG_OP(SBC8, A, B);						break; // SBC A, B
				case 0x99: REG_OP(SBC8, A, C);						break; // SBC A, C
				case 0x9A: REG_OP(SBC8, A, D);						break; // SBC A, D
				case 0x9B: REG_OP(SBC8, A, E);						break; // SBC A, E
				case 0x9C: REG_OP(SBC8, A, Iyh);					break; // SBC A, Iyh
				case 0x9D: REG_OP(SBC8, A, Iyl);					break; // SBC A, Iyl
				case 0x9E: I_REG_OP_IND_n(SBC8, A, Iyl, Iyh);		break; // SBC A, (Iy + n)
				case 0x9F: REG_OP(SBC8, A, A);						break; // SBC A, A
				case 0xA0: REG_OP(AND8, A, B);						break; // AND A, B
				case 0xA1: REG_OP(AND8, A, C);						break; // AND A, C
				case 0xA2: REG_OP(AND8, A, D);						break; // AND A, D
				case 0xA3: REG_OP(AND8, A, E);						break; // AND A, E
				case 0xA4: REG_OP(AND8, A, Iyh);					break; // AND A, Iyh
				case 0xA5: REG_OP(AND8, A, Iyl);					break; // AND A, Iyl
				case 0xA6: I_REG_OP_IND_n(AND8, A, Iyl, Iyh);		break; // AND A, (Iy + n)
				case 0xA7: REG_OP(AND8, A, A);						break; // AND A, A
				case 0xA8: REG_OP(XOR8, A, B);						break; // XOR A, B
				case 0xA9: REG_OP(XOR8, A, C);						break; // XOR A, C
				case 0xAA: REG_OP(XOR8, A, D);						break; // XOR A, D
				case 0xAB: REG_OP(XOR8, A, E);						break; // XOR A, E
				case 0xAC: REG_OP(XOR8, A, Iyh);					break; // XOR A, Iyh
				case 0xAD: REG_OP(XOR8, A, Iyl);					break; // XOR A, Iyl
				case 0xAE: I_REG_OP_IND_n(XOR8, A, Iyl, Iyh);		break; // XOR A, (Iy + n)
				case 0xAF: REG_OP(XOR8, A, A);						break; // XOR A, A
				case 0xB0: REG_OP(OR8, A, B);						break; // OR A, B
				case 0xB1: REG_OP(OR8, A, C);						break; // OR A, C
				case 0xB2: REG_OP(OR8, A, D);						break; // OR A, D
				case 0xB3: REG_OP(OR8, A, E);						break; // OR A, E
				case 0xB4: REG_OP(OR8, A, Iyh);						break; // OR A, Iyh
				case 0xB5: REG_OP(OR8, A, Iyl);						break; // OR A, Iyl
				case 0xB6: I_REG_OP_IND_n(OR8, A, Iyl, Iyh);		break; // OR A, (Iy + n)
				case 0xB7: REG_OP(OR8, A, A);						break; // OR A, A
				case 0xB8: REG_OP(CP8, A, B);						break; // CP A, B
				case 0xB9: REG_OP(CP8, A, C);						break; // CP A, C
				case 0xBA: REG_OP(CP8, A, D);						break; // CP A, D
				case 0xBB: REG_OP(CP8, A, E);						break; // CP A, E
				case 0xBC: REG_OP(CP8, A, Iyh);						break; // CP A, Iyh
				case 0xBD: REG_OP(CP8, A, Iyl);						break; // CP A, Iyl
				case 0xBE: I_REG_OP_IND_n(CP8, A, Iyl, Iyh);		break; // CP A, (Iy + n)
				case 0xBF: REG_OP(CP8, A, A);						break; // CP A, A
				case 0xC0: RET_COND(!FlagZget());					break; // Ret NZ
				case 0xC1: POP_(C, B);								break; // POP BC
				case 0xC2: JP_COND(!FlagZget());					break; // JP NZ
				case 0xC3: JP_COND(true);							break; // JP
				case 0xC4: CALL_COND(!FlagZget());					break; // CALL NZ
				case 0xC5: PUSH_(C, B);								break; // PUSH BC
				case 0xC6: REG_OP_IND_INC(ADD8, A, PCl, PCh);		break; // ADD A, n
				case 0xC7: RST_(0);									break; // RST 0
				case 0xC8: RET_COND(FlagZget());					break; // RET Z
				case 0xC9: RET_();									break; // RET
				case 0xCA: JP_COND(FlagZget());						break; // JP Z
				case 0xCB: PREFETCH_(IYCBpre);						break; // PREFIX IyCB
				case 0xCC: CALL_COND(FlagZget());					break; // CALL Z
				case 0xCD: CALL_COND(true);							break; // CALL
				case 0xCE: REG_OP_IND_INC(ADC8, A, PCl, PCh);		break; // ADC A, n
				case 0xCF: RST_(0x08);								break; // RST 0x08
				case 0xD0: RET_COND(!FlagCget());					break; // Ret NC
				case 0xD1: POP_(E, D);								break; // POP DE
				case 0xD2: JP_COND(!FlagCget());					break; // JP NC
				case 0xD3: OUT_();									break; // OUT A
				case 0xD4: CALL_COND(!FlagCget());					break; // CALL NC
				case 0xD5: PUSH_(E, D);								break; // PUSH DE
				case 0xD6: REG_OP_IND_INC(SUB8, A, PCl, PCh);		break; // SUB A, n
				case 0xD7: RST_(0x10);								break; // RST 0x10
				case 0xD8: RET_COND(FlagCget());					break; // RET C
				case 0xD9: EXX_();									break; // EXX
				case 0xDA: JP_COND(FlagCget());						break; // JP C
				case 0xDB: IN_();									break; // IN A
				case 0xDC: CALL_COND(FlagCget());					break; // CALL C
				case 0xDD: PREFIX_(IXpre);							break; // IX Prefix
				case 0xDE: REG_OP_IND_INC(SBC8, A, PCl, PCh);		break; // SBC A, n
				case 0xDF: RST_(0x18);								break; // RST 0x18
				case 0xE0: RET_COND(!FlagPget());					break; // RET Po
				case 0xE1: POP_(Iyl, Iyh);							break; // POP Iy
				case 0xE2: JP_COND(!FlagPget());					break; // JP Po
				case 0xE3: EXCH_16_IND_(SPl, SPh, Iyl, Iyh);		break; // ex (SP), Iy
				case 0xE4: CALL_COND(!FlagPget());					break; // CALL Po
				case 0xE5: PUSH_(Iyl, Iyh);							break; // PUSH Iy
				case 0xE6: REG_OP_IND_INC(AND8, A, PCl, PCh);		break; // AND A, n
				case 0xE7: RST_(0x20);								break; // RST 0x20
				case 0xE8: RET_COND(FlagPget());					break; // RET Pe
				case 0xE9: JP_16(Iyl, Iyh);							break; // JP (Iy)
				case 0xEA: JP_COND(FlagPget());						break; // JP Pe
				case 0xEB: EXCH_16_(E, D, L, H);					break; // ex DE, HL
				case 0xEC: CALL_COND(FlagPget());					break; // CALL Pe
				case 0xED: PREFIX_(EXTDpre);						break; // EXTD Prefix
				case 0xEE: REG_OP_IND_INC(XOR8, A, PCl, PCh);		break; // XOR A, n
				case 0xEF: RST_(0x28);								break; // RST 0x28
				case 0xF0: RET_COND(!FlagSget());					break; // RET p
				case 0xF1: POP_(F, A);								break; // POP AF
				case 0xF2: JP_COND(!FlagSget());					break; // JP p
				case 0xF3: DI_();									break; // DI
				case 0xF4: CALL_COND(!FlagSget());					break; // CALL p
				case 0xF5: PUSH_(F, A);								break; // PUSH AF
				case 0xF6: REG_OP_IND_INC(OR8, A, PCl, PCh);		break; // OR A, n
				case 0xF7: RST_(0x30);								break; // RST 0x30
				case 0xF8: RET_COND(FlagSget());					break; // RET M
				case 0xF9: LD_SP_16(Iyl, Iyh);						break; // LD SP, Iy
				case 0xFA: JP_COND(FlagSget());						break; // JP M
				case 0xFB: EI_();									break; // EI
				case 0xFC: CALL_COND(FlagSget());					break; // CALL M
				case 0xFD: PREFIX_(IYpre);							break; // IY Prefix
				case 0xFE: REG_OP_IND_INC(CP8, A, PCl, PCh);		break; // CP A, n
				case 0xFF: RST_(0x38);								break; // RST $38
				}
			}
			else if (IXCB_prefix || IYCB_prefix)
			{
				// the first byte fetched is the prefetch value to use with the instruction
				// we pick Ix or Iy here, the indexed value is stored in WZ
				// In this way, we don't need to pass them as an argument to the I_Funcs.
				IXCB_prefix = false;
				IYCB_prefix = false;
				NO_prefix = true;

				switch (opcode)
				{
				case 0x00: I_INT_OP(RLC, B);						break; // RLC (I* + n) -> B
				case 0x01: I_INT_OP(RLC, C);						break; // RLC (I* + n) -> C
				case 0x02: I_INT_OP(RLC, D);						break; // RLC (I* + n) -> D
				case 0x03: I_INT_OP(RLC, E);						break; // RLC (I* + n) -> E
				case 0x04: I_INT_OP(RLC, H);						break; // RLC (I* + n) -> H
				case 0x05: I_INT_OP(RLC, L);						break; // RLC (I* + n) -> L
				case 0x06: I_INT_OP(RLC, ALU);						break; // RLC (I* + n)
				case 0x07: I_INT_OP(RLC, A);						break; // RLC (I* + n) -> A
				case 0x08: I_INT_OP(RRC, B);						break; // RRC (I* + n) -> B
				case 0x09: I_INT_OP(RRC, C);						break; // RRC (I* + n) -> C
				case 0x0A: I_INT_OP(RRC, D);						break; // RRC (I* + n) -> D
				case 0x0B: I_INT_OP(RRC, E);						break; // RRC (I* + n) -> E
				case 0x0C: I_INT_OP(RRC, H);						break; // RRC (I* + n) -> H
				case 0x0D: I_INT_OP(RRC, L);						break; // RRC (I* + n) -> L
				case 0x0E: I_INT_OP(RRC, ALU);						break; // RRC (I* + n)
				case 0x0F: I_INT_OP(RRC, A);						break; // RRC (I* + n) -> A
				case 0x10: I_INT_OP(RL, B);							break; // RL (I* + n) -> B
				case 0x11: I_INT_OP(RL, C);							break; // RL (I* + n) -> C
				case 0x12: I_INT_OP(RL, D);							break; // RL (I* + n) -> D
				case 0x13: I_INT_OP(RL, E);							break; // RL (I* + n) -> E
				case 0x14: I_INT_OP(RL, H);							break; // RL (I* + n) -> H
				case 0x15: I_INT_OP(RL, L);							break; // RL (I* + n) -> L
				case 0x16: I_INT_OP(RL, ALU);						break; // RL (I* + n)
				case 0x17: I_INT_OP(RL, A);							break; // RL (I* + n) -> A
				case 0x18: I_INT_OP(RR, B);							break; // RR (I* + n) -> B
				case 0x19: I_INT_OP(RR, C);							break; // RR (I* + n) -> C
				case 0x1A: I_INT_OP(RR, D);							break; // RR (I* + n) -> D
				case 0x1B: I_INT_OP(RR, E);							break; // RR (I* + n) -> E
				case 0x1C: I_INT_OP(RR, H);							break; // RR (I* + n) -> H
				case 0x1D: I_INT_OP(RR, L);							break; // RR (I* + n) -> L
				case 0x1E: I_INT_OP(RR, ALU);						break; // RR (I* + n)
				case 0x1F: I_INT_OP(RR, A);							break; // RR (I* + n) -> A
				case 0x20: I_INT_OP(SLA, B);						break; // SLA (I* + n) -> B
				case 0x21: I_INT_OP(SLA, C);						break; // SLA (I* + n) -> C
				case 0x22: I_INT_OP(SLA, D);						break; // SLA (I* + n) -> D
				case 0x23: I_INT_OP(SLA, E);						break; // SLA (I* + n) -> E
				case 0x24: I_INT_OP(SLA, H);						break; // SLA (I* + n) -> H
				case 0x25: I_INT_OP(SLA, L);						break; // SLA (I* + n) -> L
				case 0x26: I_INT_OP(SLA, ALU);						break; // SLA (I* + n)
				case 0x27: I_INT_OP(SLA, A);						break; // SLA (I* + n) -> A
				case 0x28: I_INT_OP(SRA, B);						break; // SRA (I* + n) -> B
				case 0x29: I_INT_OP(SRA, C);						break; // SRA (I* + n) -> C
				case 0x2A: I_INT_OP(SRA, D);						break; // SRA (I* + n) -> D
				case 0x2B: I_INT_OP(SRA, E);						break; // SRA (I* + n) -> E
				case 0x2C: I_INT_OP(SRA, H);						break; // SRA (I* + n) -> H
				case 0x2D: I_INT_OP(SRA, L);						break; // SRA (I* + n) -> L
				case 0x2E: I_INT_OP(SRA, ALU);						break; // SRA (I* + n)
				case 0x2F: I_INT_OP(SRA, A);						break; // SRA (I* + n) -> A
				case 0x30: I_INT_OP(SLL, B);						break; // SLL (I* + n) -> B
				case 0x31: I_INT_OP(SLL, C);						break; // SLL (I* + n) -> C
				case 0x32: I_INT_OP(SLL, D);						break; // SLL (I* + n) -> D
				case 0x33: I_INT_OP(SLL, E);						break; // SLL (I* + n) -> E
				case 0x34: I_INT_OP(SLL, H);						break; // SLL (I* + n) -> H
				case 0x35: I_INT_OP(SLL, L);						break; // SLL (I* + n) -> L
				case 0x36: I_INT_OP(SLL, ALU);						break; // SLL (I* + n)
				case 0x37: I_INT_OP(SLL, A);						break; // SLL (I* + n) -> A
				case 0x38: I_INT_OP(SRL, B);						break; // SRL (I* + n) -> B
				case 0x39: I_INT_OP(SRL, C);						break; // SRL (I* + n) -> C
				case 0x3A: I_INT_OP(SRL, D);						break; // SRL (I* + n) -> D
				case 0x3B: I_INT_OP(SRL, E);						break; // SRL (I* + n) -> E
				case 0x3C: I_INT_OP(SRL, H);						break; // SRL (I* + n) -> H
				case 0x3D: I_INT_OP(SRL, L);						break; // SRL (I* + n) -> L
				case 0x3E: I_INT_OP(SRL, ALU);						break; // SRL (I* + n)
				case 0x3F: I_INT_OP(SRL, A);						break; // SRL (I* + n) -> A
				case 0x40: I_BIT_TE(0);								break; // BIT 0, (I* + n)
				case 0x41: I_BIT_TE(0);								break; // BIT 0, (I* + n)
				case 0x42: I_BIT_TE(0);								break; // BIT 0, (I* + n)
				case 0x43: I_BIT_TE(0);								break; // BIT 0, (I* + n)
				case 0x44: I_BIT_TE(0);								break; // BIT 0, (I* + n)
				case 0x45: I_BIT_TE(0);								break; // BIT 0, (I* + n)
				case 0x46: I_BIT_TE(0);								break; // BIT 0, (I* + n)
				case 0x47: I_BIT_TE(0);								break; // BIT 0, (I* + n)
				case 0x48: I_BIT_TE(1);								break; // BIT 1, (I* + n)
				case 0x49: I_BIT_TE(1);								break; // BIT 1, (I* + n)
				case 0x4A: I_BIT_TE(1);								break; // BIT 1, (I* + n)
				case 0x4B: I_BIT_TE(1);								break; // BIT 1, (I* + n)
				case 0x4C: I_BIT_TE(1);								break; // BIT 1, (I* + n)
				case 0x4D: I_BIT_TE(1);								break; // BIT 1, (I* + n)
				case 0x4E: I_BIT_TE(1);								break; // BIT 1, (I* + n)
				case 0x4F: I_BIT_TE(1);								break; // BIT 1, (I* + n)
				case 0x50: I_BIT_TE(2);								break; // BIT 2, (I* + n)
				case 0x51: I_BIT_TE(2);								break; // BIT 2, (I* + n)
				case 0x52: I_BIT_TE(2);								break; // BIT 2, (I* + n)
				case 0x53: I_BIT_TE(2);								break; // BIT 2, (I* + n)
				case 0x54: I_BIT_TE(2);								break; // BIT 2, (I* + n)
				case 0x55: I_BIT_TE(2);								break; // BIT 2, (I* + n)
				case 0x56: I_BIT_TE(2);								break; // BIT 2, (I* + n)
				case 0x57: I_BIT_TE(2);								break; // BIT 2, (I* + n)
				case 0x58: I_BIT_TE(3);								break; // BIT 3, (I* + n)
				case 0x59: I_BIT_TE(3);								break; // BIT 3, (I* + n)
				case 0x5A: I_BIT_TE(3);								break; // BIT 3, (I* + n)
				case 0x5B: I_BIT_TE(3);								break; // BIT 3, (I* + n)
				case 0x5C: I_BIT_TE(3);								break; // BIT 3, (I* + n)
				case 0x5D: I_BIT_TE(3);								break; // BIT 3, (I* + n)
				case 0x5E: I_BIT_TE(3);								break; // BIT 3, (I* + n)
				case 0x5F: I_BIT_TE(3);								break; // BIT 3, (I* + n)
				case 0x60: I_BIT_TE(4);								break; // BIT 4, (I* + n)
				case 0x61: I_BIT_TE(4);								break; // BIT 4, (I* + n)
				case 0x62: I_BIT_TE(4);								break; // BIT 4, (I* + n)
				case 0x63: I_BIT_TE(4);								break; // BIT 4, (I* + n)
				case 0x64: I_BIT_TE(4);								break; // BIT 4, (I* + n)
				case 0x65: I_BIT_TE(4);								break; // BIT 4, (I* + n)
				case 0x66: I_BIT_TE(4);								break; // BIT 4, (I* + n)
				case 0x67: I_BIT_TE(4);								break; // BIT 4, (I* + n)
				case 0x68: I_BIT_TE(5);								break; // BIT 5, (I* + n)
				case 0x69: I_BIT_TE(5);								break; // BIT 5, (I* + n)
				case 0x6A: I_BIT_TE(5);								break; // BIT 5, (I* + n)
				case 0x6B: I_BIT_TE(5);								break; // BIT 5, (I* + n)
				case 0x6C: I_BIT_TE(5);								break; // BIT 5, (I* + n)
				case 0x6D: I_BIT_TE(5);								break; // BIT 5, (I* + n)
				case 0x6E: I_BIT_TE(5);								break; // BIT 5, (I* + n)
				case 0x6F: I_BIT_TE(5);								break; // BIT 5, (I* + n)
				case 0x70: I_BIT_TE(6);								break; // BIT 6, (I* + n)
				case 0x71: I_BIT_TE(6);								break; // BIT 6, (I* + n)
				case 0x72: I_BIT_TE(6);								break; // BIT 6, (I* + n)
				case 0x73: I_BIT_TE(6);								break; // BIT 6, (I* + n)
				case 0x74: I_BIT_TE(6);								break; // BIT 6, (I* + n)
				case 0x75: I_BIT_TE(6);								break; // BIT 6, (I* + n)
				case 0x76: I_BIT_TE(6);								break; // BIT 6, (I* + n)
				case 0x77: I_BIT_TE(6);								break; // BIT 6, (I* + n)
				case 0x78: I_BIT_TE(7);								break; // BIT 7, (I* + n)
				case 0x79: I_BIT_TE(7);								break; // BIT 7, (I* + n)
				case 0x7A: I_BIT_TE(7);								break; // BIT 7, (I* + n)
				case 0x7B: I_BIT_TE(7);								break; // BIT 7, (I* + n)
				case 0x7C: I_BIT_TE(7);								break; // BIT 7, (I* + n)
				case 0x7D: I_BIT_TE(7);								break; // BIT 7, (I* + n)
				case 0x7E: I_BIT_TE(7);								break; // BIT 7, (I* + n)
				case 0x7F: I_BIT_TE(7);								break; // BIT 7, (I* + n)
				case 0x80: I_BIT_OP(RES, 0, B);						break; // RES 0, (I* + n) -> B
				case 0x81: I_BIT_OP(RES, 0, C);						break; // RES 0, (I* + n) -> C
				case 0x82: I_BIT_OP(RES, 0, D);						break; // RES 0, (I* + n) -> D
				case 0x83: I_BIT_OP(RES, 0, E);						break; // RES 0, (I* + n) -> E
				case 0x84: I_BIT_OP(RES, 0, H);						break; // RES 0, (I* + n) -> H
				case 0x85: I_BIT_OP(RES, 0, L);						break; // RES 0, (I* + n) -> L
				case 0x86: I_BIT_OP(RES, 0, ALU);					break; // RES 0, (I* + n)
				case 0x87: I_BIT_OP(RES, 0, A);						break; // RES 0, (I* + n) -> A
				case 0x88: I_BIT_OP(RES, 1, B);						break; // RES 1, (I* + n) -> B
				case 0x89: I_BIT_OP(RES, 1, C);						break; // RES 1, (I* + n) -> C
				case 0x8A: I_BIT_OP(RES, 1, D);						break; // RES 1, (I* + n) -> D
				case 0x8B: I_BIT_OP(RES, 1, E);						break; // RES 1, (I* + n) -> E
				case 0x8C: I_BIT_OP(RES, 1, H);						break; // RES 1, (I* + n) -> H
				case 0x8D: I_BIT_OP(RES, 1, L);						break; // RES 1, (I* + n) -> L
				case 0x8E: I_BIT_OP(RES, 1, ALU);					break; // RES 1, (I* + n)
				case 0x8F: I_BIT_OP(RES, 1, A);						break; // RES 1, (I* + n) -> A
				case 0x90: I_BIT_OP(RES, 2, B);						break; // RES 2, (I* + n) -> B
				case 0x91: I_BIT_OP(RES, 2, C);						break; // RES 2, (I* + n) -> C
				case 0x92: I_BIT_OP(RES, 2, D);						break; // RES 2, (I* + n) -> D
				case 0x93: I_BIT_OP(RES, 2, E);						break; // RES 2, (I* + n) -> E
				case 0x94: I_BIT_OP(RES, 2, H);						break; // RES 2, (I* + n) -> H
				case 0x95: I_BIT_OP(RES, 2, L);						break; // RES 2, (I* + n) -> L
				case 0x96: I_BIT_OP(RES, 2, ALU);					break; // RES 2, (I* + n)
				case 0x97: I_BIT_OP(RES, 2, A);						break; // RES 2, (I* + n) -> A
				case 0x98: I_BIT_OP(RES, 3, B);						break; // RES 3, (I* + n) -> B
				case 0x99: I_BIT_OP(RES, 3, C);						break; // RES 3, (I* + n) -> C
				case 0x9A: I_BIT_OP(RES, 3, D);						break; // RES 3, (I* + n) -> D
				case 0x9B: I_BIT_OP(RES, 3, E);						break; // RES 3, (I* + n) -> E
				case 0x9C: I_BIT_OP(RES, 3, H);						break; // RES 3, (I* + n) -> H
				case 0x9D: I_BIT_OP(RES, 3, L);						break; // RES 3, (I* + n) -> L
				case 0x9E: I_BIT_OP(RES, 3, ALU);					break; // RES 3, (I* + n)
				case 0x9F: I_BIT_OP(RES, 3, A);						break; // RES 3, (I* + n) -> A
				case 0xA0: I_BIT_OP(RES, 4, B);						break; // RES 4, (I* + n) -> B
				case 0xA1: I_BIT_OP(RES, 4, C);						break; // RES 4, (I* + n) -> C
				case 0xA2: I_BIT_OP(RES, 4, D);						break; // RES 4, (I* + n) -> D
				case 0xA3: I_BIT_OP(RES, 4, E);						break; // RES 4, (I* + n) -> E
				case 0xA4: I_BIT_OP(RES, 4, H);						break; // RES 4, (I* + n) -> H 
				case 0xA5: I_BIT_OP(RES, 4, L);						break; // RES 4, (I* + n) -> L
				case 0xA6: I_BIT_OP(RES, 4, ALU);					break; // RES 4, (I* + n)
				case 0xA7: I_BIT_OP(RES, 4, A);						break; // RES 4, (I* + n) -> A
				case 0xA8: I_BIT_OP(RES, 5, B);						break; // RES 5, (I* + n) -> B
				case 0xA9: I_BIT_OP(RES, 5, C);						break; // RES 5, (I* + n) -> C
				case 0xAA: I_BIT_OP(RES, 5, D);						break; // RES 5, (I* + n) -> D
				case 0xAB: I_BIT_OP(RES, 5, E);						break; // RES 5, (I* + n) -> E
				case 0xAC: I_BIT_OP(RES, 5, H);						break; // RES 5, (I* + n) -> H
				case 0xAD: I_BIT_OP(RES, 5, L);						break; // RES 5, (I* + n) -> L
				case 0xAE: I_BIT_OP(RES, 5, ALU);					break; // RES 5, (I* + n)
				case 0xAF: I_BIT_OP(RES, 5, A);						break; // RES 5, (I* + n) -> A
				case 0xB0: I_BIT_OP(RES, 6, B);						break; // RES 6, (I* + n) -> B
				case 0xB1: I_BIT_OP(RES, 6, C);						break; // RES 6, (I* + n) -> C
				case 0xB2: I_BIT_OP(RES, 6, D);						break; // RES 6, (I* + n) -> D
				case 0xB3: I_BIT_OP(RES, 6, E);						break; // RES 6, (I* + n) -> E
				case 0xB4: I_BIT_OP(RES, 6, H);						break; // RES 6, (I* + n) -> H
				case 0xB5: I_BIT_OP(RES, 6, L);						break; // RES 6, (I* + n) -> L
				case 0xB6: I_BIT_OP(RES, 6, ALU);					break; // RES 6, (I* + n)
				case 0xB7: I_BIT_OP(RES, 6, A);						break; // RES 6, (I* + n) -> A
				case 0xB8: I_BIT_OP(RES, 7, B);						break; // RES 7, (I* + n) -> B
				case 0xB9: I_BIT_OP(RES, 7, C);						break; // RES 7, (I* + n) -> C
				case 0xBA: I_BIT_OP(RES, 7, D);						break; // RES 7, (I* + n) -> D
				case 0xBB: I_BIT_OP(RES, 7, E);						break; // RES 7, (I* + n) -> E
				case 0xBC: I_BIT_OP(RES, 7, H);						break; // RES 7, (I* + n) -> H
				case 0xBD: I_BIT_OP(RES, 7, L);						break; // RES 7, (I* + n) -> L
				case 0xBE: I_BIT_OP(RES, 7, ALU);					break; // RES 7, (I* + n)
				case 0xBF: I_BIT_OP(RES, 7, A);						break; // RES 7, (I* + n) -> A
				case 0xC0: I_BIT_OP(SET, 0, B);						break; // SET 0, (I* + n) -> B
				case 0xC1: I_BIT_OP(SET, 0, C);						break; // SET 0, (I* + n) -> C
				case 0xC2: I_BIT_OP(SET, 0, D);						break; // SET 0, (I* + n) -> D
				case 0xC3: I_BIT_OP(SET, 0, E);						break; // SET 0, (I* + n) -> E
				case 0xC4: I_BIT_OP(SET, 0, H);						break; // SET 0, (I* + n) -> H
				case 0xC5: I_BIT_OP(SET, 0, L);						break; // SET 0, (I* + n) -> L
				case 0xC6: I_BIT_OP(SET, 0, ALU);					break; // SET 0, (I* + n)
				case 0xC7: I_BIT_OP(SET, 0, A);						break; // SET 0, (I* + n) -> A
				case 0xC8: I_BIT_OP(SET, 1, B);						break; // SET 1, (I* + n) -> B
				case 0xC9: I_BIT_OP(SET, 1, C);						break; // SET 1, (I* + n) -> C
				case 0xCA: I_BIT_OP(SET, 1, D);						break; // SET 1, (I* + n) -> D
				case 0xCB: I_BIT_OP(SET, 1, E);						break; // SET 1, (I* + n) -> E
				case 0xCC: I_BIT_OP(SET, 1, H);						break; // SET 1, (I* + n) -> H
				case 0xCD: I_BIT_OP(SET, 1, L);						break; // SET 1, (I* + n) -> L
				case 0xCE: I_BIT_OP(SET, 1, ALU);					break; // SET 1, (I* + n)
				case 0xCF: I_BIT_OP(SET, 1, A);						break; // SET 1, (I* + n) -> A
				case 0xD0: I_BIT_OP(SET, 2, B);						break; // SET 2, (I* + n) -> B
				case 0xD1: I_BIT_OP(SET, 2, C);						break; // SET 2, (I* + n) -> C
				case 0xD2: I_BIT_OP(SET, 2, D);						break; // SET 2, (I* + n) -> D
				case 0xD3: I_BIT_OP(SET, 2, E);						break; // SET 2, (I* + n) -> E
				case 0xD4: I_BIT_OP(SET, 2, H);						break; // SET 2, (I* + n) -> H
				case 0xD5: I_BIT_OP(SET, 2, L);						break; // SET 2, (I* + n) -> L
				case 0xD6: I_BIT_OP(SET, 2, ALU);					break; // SET 2, (I* + n)
				case 0xD7: I_BIT_OP(SET, 2, A);						break; // SET 2, (I* + n) -> A
				case 0xD8: I_BIT_OP(SET, 3, B);						break; // SET 3, (I* + n) -> B
				case 0xD9: I_BIT_OP(SET, 3, C);						break; // SET 3, (I* + n) -> C
				case 0xDA: I_BIT_OP(SET, 3, D);						break; // SET 3, (I* + n) -> D
				case 0xDB: I_BIT_OP(SET, 3, E);						break; // SET 3, (I* + n) -> E
				case 0xDC: I_BIT_OP(SET, 3, H);						break; // SET 3, (I* + n) -> H
				case 0xDD: I_BIT_OP(SET, 3, L);						break; // SET 3, (I* + n) -> L
				case 0xDE: I_BIT_OP(SET, 3, ALU);					break; // SET 3, (I* + n)
				case 0xDF: I_BIT_OP(SET, 3, A);						break; // SET 3, (I* + n) -> A
				case 0xE0: I_BIT_OP(SET, 4, B);						break; // SET 4, (I* + n) -> B
				case 0xE1: I_BIT_OP(SET, 4, C);						break; // SET 4, (I* + n) -> C
				case 0xE2: I_BIT_OP(SET, 4, D);						break; // SET 4, (I* + n) -> D
				case 0xE3: I_BIT_OP(SET, 4, E);						break; // SET 4, (I* + n) -> E
				case 0xE4: I_BIT_OP(SET, 4, H);						break; // SET 4, (I* + n) -> H
				case 0xE5: I_BIT_OP(SET, 4, L);						break; // SET 4, (I* + n) -> L
				case 0xE6: I_BIT_OP(SET, 4, ALU);					break; // SET 4, (I* + n)
				case 0xE7: I_BIT_OP(SET, 4, A);						break; // SET 4, (I* + n) -> A
				case 0xE8: I_BIT_OP(SET, 5, B);						break; // SET 5, (I* + n) -> B
				case 0xE9: I_BIT_OP(SET, 5, C);						break; // SET 5, (I* + n) -> C
				case 0xEA: I_BIT_OP(SET, 5, D);						break; // SET 5, (I* + n) -> D
				case 0xEB: I_BIT_OP(SET, 5, E);						break; // SET 5, (I* + n) -> E
				case 0xEC: I_BIT_OP(SET, 5, H);						break; // SET 5, (I* + n) -> H
				case 0xED: I_BIT_OP(SET, 5, L);						break; // SET 5, (I* + n) -> L
				case 0xEE: I_BIT_OP(SET, 5, ALU);					break; // SET 5, (I* + n)
				case 0xEF: I_BIT_OP(SET, 5, A);						break; // SET 5, (I* + n) -> A
				case 0xF0: I_BIT_OP(SET, 6, B);						break; // SET 6, (I* + n) -> B
				case 0xF1: I_BIT_OP(SET, 6, C);						break; // SET 6, (I* + n) -> C
				case 0xF2: I_BIT_OP(SET, 6, D);						break; // SET 6, (I* + n) -> D
				case 0xF3: I_BIT_OP(SET, 6, E);						break; // SET 6, (I* + n) -> E
				case 0xF4: I_BIT_OP(SET, 6, H);						break; // SET 6, (I* + n) -> H
				case 0xF5: I_BIT_OP(SET, 6, L);						break; // SET 6, (I* + n) -> L
				case 0xF6: I_BIT_OP(SET, 6, ALU);					break; // SET 6, (I* + n)
				case 0xF7: I_BIT_OP(SET, 6, A);						break; // SET 6, (I* + n) -> A
				case 0xF8: I_BIT_OP(SET, 7, B);						break; // SET 7, (I* + n) -> B
				case 0xF9: I_BIT_OP(SET, 7, C);						break; // SET 7, (I* + n) -> C
				case 0xFA: I_BIT_OP(SET, 7, D);						break; // SET 7, (I* + n) -> D
				case 0xFB: I_BIT_OP(SET, 7, E);						break; // SET 7, (I* + n) -> E
				case 0xFC: I_BIT_OP(SET, 7, H);						break; // SET 7, (I* + n) -> H
				case 0xFD: I_BIT_OP(SET, 7, L);						break; // SET 7, (I* + n) -> L
				case 0xFE: I_BIT_OP(SET, 7, ALU);					break; // SET 7, (I* + n)
				case 0xFF: I_BIT_OP(SET, 7, A);						break; // SET 7, (I* + n) -> A
				}
			}
		}

		#pragma endregion

		#pragma region Operations

		void Read_Func(uint32_t dest, uint32_t src_l, uint32_t src_h)
		{
			bank_num = bank_offset = (uint32_t)(Regs[src_l] | (Regs[src_h]) << 8);
			bank_offset &= low_mask;
			bank_num = (bank_num >> bank_shift)& high_mask;
			Regs[dest] = MemoryMap[bank_num][bank_offset];

			Regs[DB] = Regs[dest];
		}

		void Read_INC_Func(uint32_t dest, uint32_t src_l, uint32_t src_h)
		{
			bank_num = bank_offset = (uint32_t)(Regs[src_l] | (Regs[src_h]) << 8);
			bank_offset &= low_mask;
			bank_num = (bank_num >> bank_shift)& high_mask;
			Regs[dest] = MemoryMap[bank_num][bank_offset];

			Regs[DB] = Regs[dest];
			INC16_Func(src_l, src_h);
		}

		void Read_INC_TR_PC_Func(uint32_t dest_l, uint32_t dest_h, uint32_t src_l, uint32_t src_h)
		{
			bank_num = bank_offset = (uint32_t)(Regs[src_l] | (Regs[src_h]) << 8);
			bank_offset &= low_mask;
			bank_num = (bank_num >> bank_shift)& high_mask;
			Regs[dest_h] = MemoryMap[bank_num][bank_offset];

			Regs[DB] = Regs[dest_h];
			INC16_Func(src_l, src_h);
			TR16_Func(PCl, PCh, dest_l, dest_h);
		}

		void Write_Func(uint32_t dest_l, uint32_t dest_h, uint32_t src)
		{
			Regs[DB] = Regs[src];

			bank_num = bank_offset = (uint32_t)(Regs[dest_l] | (Regs[dest_h]) << 8);
			bank_offset &= low_mask;
			bank_num = (bank_num >> bank_shift)& high_mask;
			MemoryMap[bank_num][bank_offset] = MemoryMapMask[bank_num] & (Regs[src] & 0xFF);

			Memory_Write((uint32_t)(Regs[dest_l] | (Regs[dest_h] << 8)), (uint8_t)(Regs[src] & 0xFF));
		}

		void Write_INC_Func(uint32_t dest_l, uint32_t dest_h, uint32_t src)
		{
			Regs[DB] = Regs[src];

			bank_num = bank_offset = (uint32_t)(Regs[dest_l] | (Regs[dest_h]) << 8);
			bank_offset &= low_mask;
			bank_num = (bank_num >> bank_shift)& high_mask;
			MemoryMap[bank_num][bank_offset] = MemoryMapMask[bank_num] & (Regs[src] & 0xFF);

			Memory_Write((uint32_t)(Regs[dest_l] | (Regs[dest_h] << 8)), (uint8_t)(Regs[src] & 0xFF));

			INC16_Func(dest_l, dest_h);
		}

		void Write_DEC_Func(uint32_t dest_l, uint32_t dest_h, uint32_t src)
		{
			Regs[DB] = Regs[src];

			bank_num = bank_offset = (uint32_t)(Regs[dest_l] | (Regs[dest_h]) << 8);
			bank_offset &= low_mask;
			bank_num = (bank_num >> bank_shift)& high_mask;
			MemoryMap[bank_num][bank_offset] = MemoryMapMask[bank_num] & (Regs[src] & 0xFF);

			Memory_Write((uint32_t)(Regs[dest_l] | (Regs[dest_h] << 8)), (uint8_t)(Regs[src] & 0xFF));

			DEC16_Func(dest_l, dest_h);
		}

		void Write_TR_PC_Func(uint32_t dest_l, uint32_t dest_h, uint32_t src)
		{
			Regs[DB] = Regs[src];

			bank_num = bank_offset = (uint32_t)(Regs[dest_l] | (Regs[dest_h]) << 8);
			bank_offset &= low_mask;
			bank_num = (bank_num >> bank_shift)& high_mask;
			MemoryMap[bank_num][bank_offset] = MemoryMapMask[bank_num] & (Regs[src] & 0xFF);

			Memory_Write((uint32_t)(Regs[dest_l] | (Regs[dest_h] << 8)), (uint8_t)(Regs[src] & 0xFF));

			TR16_Func(PCl, PCh, Z, W);
		}

		void OUT_Func(uint32_t dest_l, uint32_t dest_h, uint32_t src)
		{
			Regs[DB] = Regs[src];
			HW_Write((uint32_t)(Regs[dest_l] | (Regs[dest_h] << 8)), (uint8_t)(Regs[src] & 0xFF));
		}

		void OUT_INC_Func(uint32_t dest_l, uint32_t dest_h, uint32_t src)
		{
			Regs[DB] = Regs[src];
			HW_Write((uint32_t)(Regs[dest_l] | (Regs[dest_h] << 8)), (uint8_t)(Regs[src] & 0xFF));
			INC16_Func(dest_l, dest_h);
		}

		void IN_Func(uint32_t dest, uint32_t src_l, uint32_t src_h)
		{
			Regs[dest] = HW_Read((uint32_t)(Regs[src_l] | (Regs[src_h]) << 8));
			Regs[DB] = Regs[dest];

			FlagZset(Regs[dest] == 0);
			FlagPset(TableParity[Regs[dest]]);
			FlagHset(false);
			FlagNset(false);
			FlagSset((Regs[dest] & 0x80) > 0);
			Flag5set((Regs[dest] & 0x20) > 0);
			Flag3set((Regs[dest] & 0x08) > 0);
		}

		void IN_INC_Func(uint32_t dest, uint32_t src_l, uint32_t src_h)
		{
			Regs[dest] = HW_Read((uint32_t)(Regs[src_l] | (Regs[src_h]) << 8));
			Regs[DB] = Regs[dest];

			FlagZset(Regs[dest] == 0);
			FlagPset(TableParity[Regs[dest]]);
			FlagHset(false);
			FlagNset(false);
			FlagSset((Regs[dest] & 0x80) > 0);
			Flag5set((Regs[dest] & 0x20) > 0);
			Flag3set((Regs[dest] & 0x08) > 0);

			INC16_Func(src_l, src_h);
		}

		void IN_A_N_INC_Func(uint32_t dest, uint32_t src_l, uint32_t src_h)
		{
			Regs[dest] = HW_Read((uint32_t)(Regs[src_l] | (Regs[src_h]) << 8));
			Regs[DB] = Regs[dest];
			INC16_Func(src_l, src_h);
		}

		void TR_Func(uint32_t dest, uint32_t src)
		{
			Regs[dest] = Regs[src];
		}

		void TR16_Func(uint32_t dest_l, uint32_t dest_h, uint32_t src_l, uint32_t src_h)
		{
			Regs[dest_l] = Regs[src_l];
			Regs[dest_h] = Regs[src_h];
		}

		void ADD16_Func(uint32_t dest_l, uint32_t dest_h, uint32_t src_l, uint32_t src_h)
		{
			Reg16_d = Regs[dest_l] | (Regs[dest_h] << 8);
			Reg16_s = Regs[src_l] | (Regs[src_h] << 8);
			temp = Reg16_d + Reg16_s;

			FlagCset((temp & 0x10000) > 0);
			FlagHset(((Reg16_d & 0xFFF) + (Reg16_s & 0xFFF)) > 0xFFF);
			FlagNset(false);
			Flag3set((temp & 0x0800) != 0);
			Flag5set((temp & 0x2000) != 0);

			Regs[dest_l] = (uint8_t)(temp & 0xFF);
			Regs[dest_h] = (uint8_t)((temp & 0xFF00) >> 8);
		}

		void ADD8_Func(uint32_t dest, uint32_t src)
		{
			Reg16_d = Regs[dest];
			Reg16_d += Regs[src];

			FlagCset((Reg16_d & 0x100) > 0);
			FlagZset((Reg16_d & 0xFF) == 0);

			ans = (Reg16_d & 0xFF);

			// redo for half carry flag
			Reg16_d = Regs[dest] & 0xF;
			Reg16_d += (Regs[src] & 0xF);

			FlagHset((Reg16_d & 0x10) > 0);
			FlagNset(false);
			Flag3set((ans & 0x08) != 0);
			Flag5set((ans & 0x20) != 0);
			FlagPset(((Regs[dest] & 0x80) == (Regs[src] & 0x80)) && ((Regs[dest] & 0x80) != (ans & 0x80)));
			FlagSset(ans > 127);

			Regs[dest] = (uint8_t)ans;
		}

		void SUB8_Func(uint32_t dest, uint32_t src)
		{
			Reg16_d = Regs[dest];
			Reg16_d -= Regs[src];

			FlagCset((Reg16_d & 0x100) > 0);
			FlagZset((Reg16_d & 0xFF) == 0);

			ans = (Reg16_d & 0xFF);

			// redo for half carry flag
			Reg16_d = Regs[dest] & 0xF;
			Reg16_d -= (Regs[src] & 0xF);

			FlagHset((Reg16_d & 0x10) > 0);
			FlagNset(true);
			Flag3set((ans & 0x08) != 0);
			Flag5set((ans & 0x20) != 0);
			FlagPset(((Regs[dest] & 0x80) != (Regs[src] & 0x80)) && ((Regs[dest] & 0x80) != (ans & 0x80)));
			FlagSset(ans > 127);

			Regs[dest] = (uint8_t)ans;
		}

		void BIT_Func(uint32_t bit, uint32_t src)
		{
			FlagZset(!((Regs[src] & (1 << bit)) > 0));
			FlagPset(FlagZget()); // special case
			FlagHset(true);
			FlagNset(false);
			FlagSset((bit == 7) && (((Regs[src] & (1 << bit)) > 0)));
			Flag5set((Regs[src] & 0x20) > 0);
			Flag3set((Regs[src] & 0x08) > 0);
		}

		// When doing I* + n bit tests, flags 3 and 5 come from I* + n
		// This cooresponds to the high byte of WZ
		// This is the same for the (HL) bit tests, except that WZ were not assigned to before the test occurs
		void I_BIT_Func(uint32_t bit, uint32_t src)
		{
			FlagZset(!((Regs[src] & (1 << bit)) > 0));
			FlagPset(FlagZget()); // special case
			FlagHset(true);
			FlagNset(false);
			FlagSset((bit == 7) && (((Regs[src] & (1 << bit)) > 0)));
			Flag5set((Regs[W] & 0x20) > 0);
			Flag3set((Regs[W] & 0x08) > 0);
		}

		void SET_Func(uint32_t bit, uint32_t src)
		{
			Regs[src] |= (uint32_t)(1 << bit);
		}

		void RES_Func(uint32_t bit, uint32_t src)
		{
			Regs[src] &= (uint32_t)(0xFF - (1 << bit));
		}

		void ASGN_Func(uint32_t src, uint32_t val)
		{
			Regs[src] = val;
		}

		void SLL_Func(uint32_t src)
		{
			FlagCset((Regs[src] & 0x80) > 0);

			Regs[src] = (uint32_t)(((Regs[src] << 1) & 0xFF) | 0x1);

			FlagSset((Regs[src] & 0x80) > 0);
			FlagZset(Regs[src] == 0);
			FlagPset(TableParity[Regs[src]]);
			Flag3set((Regs[src] & 0x08) != 0);
			Flag5set((Regs[src] & 0x20) != 0);
			FlagHset(false);
			FlagNset(false);
		}

		void SLA_Func(uint32_t src)
		{
			FlagCset((Regs[src] & 0x80) > 0);

			Regs[src] = (uint32_t)((Regs[src] << 1) & 0xFF);

			FlagSset((Regs[src] & 0x80) > 0);
			FlagZset(Regs[src] == 0);
			FlagPset(TableParity[Regs[src]]);
			Flag3set((Regs[src] & 0x08) != 0);
			Flag5set((Regs[src] & 0x20) != 0);
			FlagHset(false);
			FlagNset(false);
		}

		void SRA_Func(uint32_t src)
		{
			FlagCset((Regs[src] & 0x01) > 0);

			temp = (uint32_t)(Regs[src] & 0x80); // MSB doesn't change in this operation

			Regs[src] = (uint8_t)((Regs[src] >> 1) | temp);

			FlagSset((Regs[src] & 0x80) > 0);
			FlagZset(Regs[src] == 0);
			FlagPset(TableParity[Regs[src]]);
			Flag3set((Regs[src] & 0x08) != 0);
			Flag5set((Regs[src] & 0x20) != 0);
			FlagHset(false);
			FlagNset(false);
		}

		void SRL_Func(uint32_t src)
		{
			FlagCset((Regs[src] & 0x01) > 0);

			Regs[src] = (uint8_t)(Regs[src] >> 1);

			FlagSset((Regs[src] & 0x80) > 0);
			FlagZset(Regs[src] == 0);
			FlagPset(TableParity[Regs[src]]);
			Flag3set((Regs[src] & 0x08) != 0);
			Flag5set((Regs[src] & 0x20) != 0);
			FlagHset(false);
			FlagNset(false);
		}

		void CPL_Func(uint32_t src)
		{
			Regs[src] = (uint8_t)((~Regs[src]) & 0xFF);

			FlagHset(true);
			FlagNset(true);
			Flag3set((Regs[src] & 0x08) != 0);
			Flag5set((Regs[src] & 0x20) != 0);
		}

		void CCF_Func(uint32_t src)
		{
			FlagHset(FlagCget());
			FlagCset(!FlagCget());
			FlagNset(false);
			Flag3set((Regs[src] & 0x08) != 0);
			Flag5set((Regs[src] & 0x20) != 0);
		}

		void SCF_Func(uint32_t src)
		{
			FlagCset(true);
			FlagHset(false);
			FlagNset(false);
			Flag3set((Regs[src] & 0x08) != 0);
			Flag5set((Regs[src] & 0x20) != 0);
		}

		void AND8_Func(uint32_t dest, uint32_t src)
		{
			Regs[dest] = (uint8_t)(Regs[dest] & Regs[src]);

			FlagZset(Regs[dest] == 0);
			FlagCset(false);
			FlagHset(true);
			FlagNset(false);
			Flag3set((Regs[dest] & 0x08) != 0);
			Flag5set((Regs[dest] & 0x20) != 0);
			FlagSset(Regs[dest] > 127);
			FlagPset(TableParity[Regs[dest]]);
		}

		void OR8_Func(uint32_t dest, uint32_t src)
		{
			Regs[dest] = (uint8_t)(Regs[dest] | Regs[src]);

			FlagZset(Regs[dest] == 0);
			FlagCset(false);
			FlagHset(false);
			FlagNset(false);
			Flag3set((Regs[dest] & 0x08) != 0);
			Flag5set((Regs[dest] & 0x20) != 0);
			FlagSset(Regs[dest] > 127);
			FlagPset(TableParity[Regs[dest]]);
		}

		void XOR8_Func(uint32_t dest, uint32_t src)
		{
			Regs[dest] = (uint8_t)((Regs[dest] ^ Regs[src]));

			FlagZset(Regs[dest] == 0);
			FlagCset(false);
			FlagHset(false);
			FlagNset(false);
			Flag3set((Regs[dest] & 0x08) != 0);
			Flag5set((Regs[dest] & 0x20) != 0);
			FlagSset(Regs[dest] > 127);
			FlagPset(TableParity[Regs[dest]]);
		}

		void CP8_Func(uint32_t dest, uint32_t src)
		{
			Reg16_d = Regs[dest];
			Reg16_d -= Regs[src];

			FlagCset((Reg16_d & 0x100) > 0);
			FlagZset((Reg16_d & 0xFF) == 0);

			ans = (uint32_t)(Reg16_d & 0xFF);

			// redo for half carry flag
			Reg16_d = Regs[dest] & 0xF;
			Reg16_d -= (Regs[src] & 0xF);

			FlagHset((Reg16_d & 0x10) > 0);
			FlagNset(true);
			Flag3set((Regs[src] & 0x08) != 0);
			Flag5set((Regs[src] & 0x20) != 0);
			FlagPset(((Regs[dest] & 0x80) != (Regs[src] & 0x80)) && ((Regs[dest] & 0x80) != (ans & 0x80)));
			FlagSset(ans > 127);
		}

		void RRC_Func(uint32_t src)
		{
			bool imm = src == Aim;
			if (imm) { src = A; }

			FlagCset((Regs[src] & 0x01) > 0);

			Regs[src] = (uint8_t)((FlagCget() ? 0x80 : 0) | (Regs[src] >> 1));

			if (!imm)
			{
				FlagSset((Regs[src] & 0x80) > 0);
				FlagZset(Regs[src] == 0);
				FlagPset(TableParity[Regs[src]]);
			}

			Flag3set((Regs[src] & 0x08) != 0);
			Flag5set((Regs[src] & 0x20) != 0);
			FlagHset(false);
			FlagNset(false);
		}

		void RR_Func(uint32_t src)
		{
			bool imm = src == Aim;
			if (imm) { src = A; }

			carry = (uint32_t)(FlagCget() ? 0x80 : 0);

			FlagCset((Regs[src] & 0x01) > 0);

			Regs[src] = (uint8_t)(carry | (Regs[src] >> 1));

			if (!imm)
			{
				FlagSset((Regs[src] & 0x80) > 0);
				FlagZset(Regs[src] == 0);
				FlagPset(TableParity[Regs[src]]);
			}

			Flag3set((Regs[src] & 0x08) != 0);
			Flag5set((Regs[src] & 0x20) != 0);
			FlagHset(false);
			FlagNset(false);
		}

		void RLC_Func(uint32_t src)
		{
			bool imm = src == Aim;
			if (imm) { src = A; }

			carry = (uint32_t)(((Regs[src] & 0x80) > 0) ? 1 : 0);
			FlagCset((Regs[src] & 0x80) > 0);

			Regs[src] = (uint8_t)(((Regs[src] << 1) & 0xFF) | carry);

			if (!imm)
			{
				FlagSset((Regs[src] & 0x80) > 0);
				FlagZset(Regs[src] == 0);
				FlagPset(TableParity[Regs[src]]);
			}

			Flag3set((Regs[src] & 0x08) != 0);
			Flag5set((Regs[src] & 0x20) != 0);
			FlagHset(false);
			FlagNset(false);
		}

		void RL_Func(uint32_t src)
		{
			bool imm = src == Aim;
			if (imm) { src = A; }

			carry = (uint32_t)(FlagCget() ? 1 : 0);
			FlagCset((Regs[src] & 0x80) > 0);

			Regs[src] = (uint8_t)(((Regs[src] << 1) & 0xFF) | carry);

			if (!imm)
			{
				FlagSset((Regs[src] & 0x80) > 0);
				FlagZset(Regs[src] == 0);
				FlagPset(TableParity[Regs[src]]);
			}

			Flag3set((Regs[src] & 0x08) != 0);
			Flag5set((Regs[src] & 0x20) != 0);
			FlagHset(false);
			FlagNset(false);
		}

		void INC8_Func(uint32_t src)
		{
			Reg16_d = Regs[src];
			Reg16_d += 1;

			FlagZset((Reg16_d & 0xFF) == 0);

			ans = (uint32_t)(Reg16_d & 0xFF);

			// redo for half carry flag
			Reg16_d = Regs[src] & 0xF;
			Reg16_d += 1;

			FlagHset((Reg16_d & 0x10) > 0);
			FlagNset(false);

			Regs[src] = (uint8_t)ans;

			FlagSset((Regs[src] & 0x80) > 0);
			FlagPset(Regs[src] == 0x80);
			Flag3set((Regs[src] & 0x08) != 0);
			Flag5set((Regs[src] & 0x20) != 0);
		}

		void DEC8_Func(uint32_t src)
		{
			Reg16_d = Regs[src];
			Reg16_d -= 1;

			FlagZset((Reg16_d & 0xFF) == 0);

			ans = (uint32_t)(Reg16_d & 0xFF);

			// redo for half carry flag
			Reg16_d = Regs[src] & 0xF;
			Reg16_d -= 1;

			FlagHset((Reg16_d & 0x10) > 0);
			FlagNset(true);

			Regs[src] = (uint8_t)ans;

			FlagSset((Regs[src] & 0x80) > 0);
			FlagPset(Regs[src] == 0x7F);
			Flag3set((Regs[src] & 0x08) != 0);
			Flag5set((Regs[src] & 0x20) != 0);
		}

		void INC16_Func(uint32_t src_l, uint32_t src_h)
		{
			Reg16_d = Regs[src_l] | (Regs[src_h] << 8);

			Reg16_d += 1;

			Regs[src_l] = (uint8_t)(Reg16_d & 0xFF);
			Regs[src_h] = (uint8_t)((Reg16_d & 0xFF00) >> 8);
		}

		void DEC16_Func(uint32_t src_l, uint32_t src_h)
		{
			Reg16_d = Regs[src_l] | (Regs[src_h] << 8);

			Reg16_d -= 1;

			Regs[src_l] = (uint8_t)(Reg16_d & 0xFF);
			Regs[src_h] = (uint8_t)((Reg16_d & 0xFF00) >> 8);
		}

		void ADC8_Func(uint32_t dest, uint32_t src)
		{
			Reg16_d = Regs[dest];
			carry = FlagCget() ? 1 : 0;

			Reg16_d += (Regs[src] + carry);

			FlagCset((Reg16_d & 0x100) > 0);
			FlagZset((Reg16_d & 0xFF) == 0);

			ans = (uint32_t)(Reg16_d & 0xFF);

			// redo for half carry flag
			Reg16_d = Regs[dest] & 0xF;
			Reg16_d += ((Regs[src] & 0xF) + carry);

			FlagHset((Reg16_d & 0x10) > 0);
			FlagNset(false);
			Flag3set((ans & 0x08) != 0);
			Flag5set((ans & 0x20) != 0);
			FlagPset(((Regs[dest] & 0x80) == (Regs[src] & 0x80)) && ((Regs[dest] & 0x80) != (ans & 0x80)));
			FlagSset(ans > 127);

			Regs[dest] = (uint8_t)ans;
		}

		void SBC8_Func(uint32_t dest, uint32_t src)
		{
			Reg16_d = Regs[dest];
			carry = FlagCget() ? 1 : 0;

			Reg16_d -= (Regs[src] + carry);

			FlagCset((Reg16_d & 0x100) > 0);
			FlagZset((Reg16_d & 0xFF) == 0);

			ans = (uint32_t)(Reg16_d & 0xFF);

			// redo for half carry flag
			Reg16_d = Regs[dest] & 0xF;
			Reg16_d -= ((Regs[src] & 0xF) + carry);

			FlagHset((Reg16_d & 0x10) > 0);
			FlagNset(true);
			Flag3set((ans & 0x08) != 0);
			Flag5set((ans & 0x20) != 0);
			FlagPset(((Regs[dest] & 0x80) != (Regs[src] & 0x80)) && ((Regs[dest] & 0x80) != (ans & 0x80)));
			FlagSset(ans > 127);

			Regs[dest] = (uint8_t)ans;
		}

		void DA_Func(uint32_t src)
		{
			uint32_t a = (Regs[src] & 0xFF);
			temp = a;

			if (FlagNget())
			{
				if (FlagHget() || ((a & 0x0F) > 0x09)) { temp -= 0x06; }
				if (FlagCget() || a > 0x99) { temp -= 0x60; }
			}
			else
			{
				if (FlagHget() || ((a & 0x0F) > 0x09)) { temp += 0x06; }
				if (FlagCget() || a > 0x99) { temp += 0x60; }
			}

			temp &= 0xFF;

			FlagCset(FlagCget() || (a > 0x99));
			FlagZset(temp == 0);
			FlagHset(((a ^ temp) & 0x10) != 0);
			FlagPset(TableParity[temp]);
			FlagSset(temp > 127);
			Flag3set((temp & 0x08) != 0);
			Flag5set((temp & 0x20) != 0);

			Regs[src] = (uint8_t)temp;
		}

		// used for signed operations
		void ADDS_Func(uint32_t dest_l, uint32_t dest_h, uint32_t src_l, uint32_t src_h)
		{
			Reg16_d = Regs[dest_l];
			Reg16_s = Regs[src_l];

			Reg16_d += Reg16_s;

			temp = 0;

			// since this is signed addition, calculate the high byte carry appropriately
			// note that flags are unaffected by this operation
			if ((Reg16_s & 0x80) > 0)
			{
				if (((Reg16_d & 0xFF) >= Regs[dest_l]))
				{
					temp = 0xFF;
				}
				else
				{
					temp = 0;
				}
			}
			else
			{
				temp = (uint32_t)(((Reg16_d & 0x100) > 0) ? 1 : 0);
			}

			uint32_t ans_l = (uint32_t)(Reg16_d & 0xFF);

			Regs[dest_l] = (uint8_t)ans_l;
			Regs[dest_h] += (uint8_t)temp;
			Regs[dest_h] &= 0xFF;
		}

		void EXCH_16_Func(uint32_t dest_l, uint32_t dest_h, uint32_t src_l, uint32_t src_h)
		{
			temp = Regs[dest_l];
			Regs[dest_l] = Regs[src_l];
			Regs[src_l] = (uint8_t)temp;

			temp = Regs[dest_h];
			Regs[dest_h] = Regs[src_h];
			Regs[src_h] = (uint8_t)temp;
		}

		void SBC_16_Func(uint32_t dest_l, uint32_t dest_h, uint32_t src_l, uint32_t src_h)
		{
			Reg16_d = Regs[dest_l] | (Regs[dest_h] << 8);
			Reg16_s = Regs[src_l] | (Regs[src_h] << 8);
			carry = FlagCget() ? 1 : 0;

			ans = Reg16_d - Reg16_s - carry;

			FlagNset(true);
			FlagCset((ans & 0x10000) > 0);
			FlagPset((((Reg16_d & 0x8000) > 0) != ((Reg16_s & 0x8000) > 0)) && (((Reg16_d & 0x8000) > 0) != ((ans & 0x8000) > 0)));
			FlagSset((uint32_t)(ans & 0xFFFF) > 32767);
			FlagZset((ans & 0xFFFF) == 0);
			Flag3set((ans & 0x0800) != 0);
			Flag5set((ans & 0x2000) != 0);

			// redo for half carry flag
			Reg16_d &= 0xFFF;
			Reg16_d -= ((Reg16_s & 0xFFF) + carry);

			FlagHset((Reg16_d & 0x1000) > 0);

			Regs[dest_l] = (uint8_t)(ans & 0xFF);
			Regs[dest_h] = (uint8_t)((ans >> 8) & 0xFF);
		}

		void ADC_16_Func(uint32_t dest_l, uint32_t dest_h, uint32_t src_l, uint32_t src_h)
		{
			Reg16_d = Regs[dest_l] | (Regs[dest_h] << 8);
			Reg16_s = Regs[src_l] | (Regs[src_h] << 8);

			ans = Reg16_d + Reg16_s + (FlagCget() ? 1 : 0);

			FlagHset(((Reg16_d & 0xFFF) + (Reg16_s & 0xFFF) + (FlagCget() ? 1 : 0)) > 0xFFF);
			FlagNset(false);
			FlagCset((ans & 0x10000) > 0);
			FlagPset((((Reg16_d & 0x8000) > 0) == ((Reg16_s & 0x8000) > 0)) && (((Reg16_d & 0x8000) > 0) != ((ans & 0x8000) > 0)));
			FlagSset((ans & 0xFFFF) > 32767);
			FlagZset((ans & 0xFFFF) == 0);
			Flag3set((ans & 0x0800) != 0);
			Flag5set((ans & 0x2000) != 0);

			Regs[dest_l] = (uint8_t)(ans & 0xFF);
			Regs[dest_h] = (uint8_t)((ans >> 8) & 0xFF);
		}

		void NEG_8_Func(uint32_t src)
		{
			Reg16_d = 0;
			Reg16_d -= Regs[src];

			FlagCset(Regs[src] != 0);
			FlagZset((Reg16_d & 0xFF) == 0);
			FlagPset(Regs[src] == 0x80);
			FlagSset((Reg16_d & 0xFF) > 127);

			ans = (uint32_t)(Reg16_d & 0xFF);
			// redo for half carry flag
			Reg16_d = 0;
			Reg16_d -= (Regs[src] & 0xF);
			FlagHset((Reg16_d & 0x10) > 0);
			Regs[src] = (uint8_t)ans;
			FlagNset(true);
			Flag3set((ans & 0x08) != 0);
			Flag5set((ans & 0x20) != 0);
		}

		void RRD_Func(uint32_t dest, uint32_t src)
		{
			Reg16_s = Regs[src];
			Reg16_d = Regs[dest];
			Regs[dest] = (uint8_t)(((Reg16_s & 0x0F) << 4) + ((Reg16_d & 0xF0) >> 4));
			Regs[src] = (uint8_t)((Reg16_s & 0xF0) + (Reg16_d & 0x0F));

			Reg16_s = Regs[src];
			FlagSset(Reg16_s > 127);
			FlagZset(Reg16_s == 0);
			FlagHset(false);
			FlagPset(TableParity[Reg16_s]);
			FlagNset(false);
			Flag3set((Reg16_s & 0x08) != 0);
			Flag5set((Reg16_s & 0x20) != 0);
		}

		void RLD_Func(uint32_t dest, uint32_t src)
		{
			Reg16_s = Regs[src];
			Reg16_d = Regs[dest];
			Regs[dest] = (uint8_t)((Reg16_s & 0x0F) + ((Reg16_d & 0x0F) << 4));
			Regs[src] = (uint8_t)((Reg16_s & 0xF0) + ((Reg16_d & 0xF0) >> 4));

			Reg16_s = Regs[src];
			FlagSset(Reg16_s > 127);
			FlagZset(Reg16_s == 0);
			FlagHset(false);
			FlagPset(TableParity[Reg16_s]);
			FlagNset(false);
			Flag3set((Reg16_s & 0x08) != 0);
			Flag5set((Reg16_s & 0x20) != 0);
		}

		// sets flags for LD/R 
		void SET_FL_LD_Func()
		{
			FlagPset((Regs[C] | (Regs[B] << 8)) != 0);
			FlagHset(false);
			FlagNset(false);
			Flag5set(((Regs[ALU] + Regs[A]) & 0x02) != 0);
			Flag3set(((Regs[ALU] + Regs[A]) & 0x08) != 0);
		}

		// set flags for CP/R
		void SET_FL_CP_Func()
		{
			uint32_t Reg8_d = Regs[A];
			uint32_t Reg8_s = Regs[ALU];

			// get half carry flag
			uint32_t temp = ((Reg8_d & 0xF) - (Reg8_s & 0xF));
			FlagHset((temp & 0x10) > 0);

			temp = (Reg8_d - Reg8_s) & 0xFF;
			FlagNset(true);
			FlagZset(temp == 0);
			FlagSset(temp > 127);
			FlagPset((Regs[C] | (Regs[B] << 8)) != 0);

			temp = (Reg8_d - Reg8_s - (FlagHget() ? 1 : 0)) & 0xFF;
			Flag5set((temp & 0x02) != 0);
			Flag3set((temp & 0x08) != 0);
		}

		// set flags for LD A, I/R
		void SET_FL_IR_Func(uint32_t dest)
		{
			if (dest == A)
			{
				FlagNset(false);
				FlagHset(false);
				FlagZset(Regs[A] == 0);
				FlagSset(Regs[A] > 127);
				FlagPset(IFF2);
				Flag5set((Regs[A] & 0x20) != 0);
				Flag3set((Regs[A] & 0x08) != 0);
			}
		}

		void FTCH_DB_Func()
		{
			Regs[DB] = ExternalDB;
		}

		#pragma endregion

		#pragma region Disassemble

		// disassemblies will also return strings of the same length
		const char* TraceHeader = "Z80A: PC, machine code, mnemonic, operands, registers (AF, BC, DE, HL, IX, IY, SP, Cy), flags (CNP3H5ZS)";
		const char* NMI_event = "                  ====NMI====                  ";
		const char* IRQ_event = "                  ====IRQ====                  ";
		const char* No_Reg = "                                                                                     ";
		const char* Reg_template = "AF:AAFF BC:BBCC DE:DDEE HL:HHLL Ix:IxIx Iy:IyIy SP:SPSP Cy:FEDCBA9876543210 CNP3H5ZSE";
		const char* Disasm_template = "PCPC: AA BB CC DD   Di Di, XXXXX               ";

		char replacer[32] = {};
		char* val_char_1 = nullptr;
		char* val_char_2 = nullptr;
		int temp_reg;


		void (*TraceCallback)(int);

		string CPURegisterState()
		{		
			val_char_1 = replacer;

			string reg_state = "AF:";
			temp_reg = (Regs[A] << 8) + Regs[F];
			sprintf_s(val_char_1, 5, "%04X", temp_reg);
			reg_state.append(val_char_1, 4);

			reg_state.append(" BC:");			
			temp_reg = (Regs[B] << 8) + Regs[C];
			sprintf_s(val_char_1, 5, "%04X", temp_reg);
			reg_state.append(val_char_1, 4);

			reg_state.append(" DE:");			
			temp_reg = (Regs[D] << 8) + Regs[E];
			sprintf_s(val_char_1, 5, "%04X", temp_reg);
			reg_state.append(val_char_1, 4);

			reg_state.append(" HL:");
			temp_reg = (Regs[H] << 8) + Regs[L];
			sprintf_s(val_char_1, 5, "%04X", temp_reg);
			reg_state.append(val_char_1, 4);

			reg_state.append(" Ix:");
			temp_reg = (Regs[Ixh] << 8) + Regs[Ixl];
			sprintf_s(val_char_1, 5, "%04X", temp_reg);
			reg_state.append(val_char_1, 4);

			reg_state.append(" Iy:");
			temp_reg = (Regs[Iyh] << 8) + Regs[Iyl];
			sprintf_s(val_char_1, 5, "%04X", temp_reg);
			reg_state.append(val_char_1, 4);

			reg_state.append(" SP:");
			temp_reg = (Regs[SPh] << 8) + Regs[SPl];
			sprintf_s(val_char_1, 5, "%04X", temp_reg);
			reg_state.append(val_char_1, 4);

			reg_state.append(" Cy:");			
			reg_state.append(val_char_1, sprintf_s(val_char_1, 32, "%16u", TotalExecutedCycles));
			reg_state.append(" ");
			
			reg_state.append(FlagCget() ? "C" : "c");
			reg_state.append(FlagNget() ? "N" : "n");
			reg_state.append(FlagPget() ? "P" : "p");
			reg_state.append(Flag3get() ? "3" : "-");
			reg_state.append(FlagHget() ? "H" : "h");
			reg_state.append(Flag5get() ? "5" : "-");
			reg_state.append(FlagZget() ? "Z" : "z");
			reg_state.append(FlagSget() ? "S" : "s");
			reg_state.append(FlagI ? "E" : "e");

			return reg_state;
		}

		string CPUDisassembly()
		{
			uint32_t bytes_read = 0;

			uint32_t* bytes_read_ptr = &bytes_read;

			string disasm = Disassemble(RegPCget(), bytes_read_ptr);
			string byte_code = "";

			val_char_1 = replacer;
			sprintf_s(val_char_1, 5, "%04X", RegPCget() & 0xFFFF);
			byte_code.append(val_char_1, 4);
			byte_code.append(": ");

			uint32_t i = 0;

			for (i = 0; i < bytes_read; i++)
			{
				bank_num = bank_offset = (RegPCget() + i) & 0xFFFF;
				bank_offset &= low_mask;
				bank_num = (bank_num >> bank_shift)& high_mask;

				char* val_char_1 = replacer;
				sprintf_s(val_char_1, 5, "%02X", MemoryMap[bank_num][bank_offset]);
				string val1(val_char_1, 2);
				
				byte_code.append(val1);
				byte_code.append(" ");
			}

			while (i < 4) 
			{
				byte_code.append("   ");
				i++;
			}

			byte_code.append("   ");

			byte_code.append(disasm);

			while (byte_code.length() < 48) 
			{
				byte_code.append(" ");
			}

			return byte_code;
		}

		string Result(string format, uint32_t* addr)
		{
			//d immediately succeeds the opcode
			//n immediate succeeds the opcode and the displacement (if present)
			//nn immediately succeeds the opcode and the displacement (if present)

			if (format.find("nn") != string::npos)
			{
				size_t str_loc = format.find("nn");

				bank_num = bank_offset = addr[0] & 0xFFFF;
				bank_offset &= low_mask;
				bank_num = (bank_num >> bank_shift) & high_mask;
				addr[0]++;
				
				val_char_1 = replacer;
				sprintf_s(val_char_1, 5, "%02X", MemoryMap[bank_num][bank_offset]);
				string val1(val_char_1, 2);

				bank_num = bank_offset = addr[0] & 0xFFFF;
				bank_offset &= low_mask;
				bank_num = (bank_num >> bank_shift)& high_mask;
				addr[0]++;

				val_char_2 = replacer;
				sprintf_s(val_char_2, 5, "%02X", MemoryMap[bank_num][bank_offset]);
				string val2(val_char_2, 2);

				format.erase(str_loc, 2);
				format.insert(str_loc, val1);
				format.insert(str_loc, val2);
			}
			
			if (format.find("n") != string::npos)
			{
				size_t str_loc = format.find("n");

				bank_num = bank_offset = addr[0] & 0xFFFF;
				bank_offset &= low_mask;
				bank_num = (bank_num >> bank_shift)& high_mask;
				addr[0]++;

				val_char_1 = replacer;
				sprintf_s(val_char_1, 5, "%02X", MemoryMap[bank_num][bank_offset]);
				string val1(val_char_1, 2);

				format.erase(str_loc, 1);
				format.insert(str_loc, val1);
			}

			if (format.find("+d") != string::npos)
			{
				size_t str_loc = format.find("+d");

				bank_num = bank_offset = addr[0] & 0xFFFF;
				bank_offset &= low_mask;
				bank_num = (bank_num >> bank_shift)& high_mask;
				addr[0]++;

				val_char_1 = replacer;
				sprintf_s(val_char_1, 5, "%+04d", (int8_t)MemoryMap[bank_num][bank_offset]);
				string val1(val_char_1, 4);

				format.erase(str_loc, 2);
				format.insert(str_loc, val1);
			}
			if (format.find("d") != string::npos)
			{
				size_t str_loc = format.find("d");

				bank_num = bank_offset = addr[0] & 0xFFFF;
				bank_offset &= low_mask;
				bank_num = (bank_num >> bank_shift)& high_mask;
				addr[0]++;

				val_char_1 = replacer;
				sprintf_s(val_char_1, 5, "%+04d", (int8_t)MemoryMap[bank_num][bank_offset]);
				string val1(val_char_1, 4);

				format.erase(str_loc, 1);
				format.insert(str_loc, val1);
			}
						
			return format;
		}

		string Disassemble(uint32_t addr, uint32_t* size)
		{
			uint32_t start_addr = addr;
			uint32_t extra_inc = 0;

			bank_num = bank_offset = addr & 0xFFFF;
			bank_offset &= low_mask;
			bank_num = (bank_num >> bank_shift) & high_mask;
			addr++;

			uint32_t A = MemoryMap[bank_num][bank_offset];
			string format;
			switch (A)
			{
			case 0xCB:
				bank_num = bank_offset = addr & 0xFFFF;
				bank_offset &= low_mask;
				bank_num = (bank_num >> bank_shift) & high_mask;
				addr++;

				A = MemoryMap[bank_num][bank_offset];
				format = mnemonicsCB[A];
				break;
			case 0xDD:
				bank_num = bank_offset = addr & 0xFFFF;
				bank_offset &= low_mask;
				bank_num = (bank_num >> bank_shift) & high_mask;
				addr++;

				A = MemoryMap[bank_num][bank_offset];
				switch (A)
				{
				case 0xCB:
					bank_num = bank_offset = (addr + 1) & 0xFFFF;
					bank_offset &= low_mask;
					bank_num = (bank_num >> bank_shift) & high_mask;

					format = mnemonicsDDCB[MemoryMap[bank_num][bank_offset]];
					extra_inc = 1;
					break;
				case 0xED:
					format = mnemonicsED[A];
					break;
				default:
					format = mnemonicsDD[A];
					break;
				}
				break;
			case 0xED:
				bank_num = bank_offset = addr & 0xFFFF;
				bank_offset &= low_mask;
				bank_num = (bank_num >> bank_shift) & high_mask;
				addr++;

				A = MemoryMap[bank_num][bank_offset];
				format = mnemonicsED[A];
				break;
			case 0xFD:
				bank_num = bank_offset = addr & 0xFFFF;
				bank_offset &= low_mask;
				bank_num = (bank_num >> bank_shift) & high_mask;
				addr++;

				A = MemoryMap[bank_num][bank_offset];
				switch (A)
				{
				case 0xCB:
					bank_num = bank_offset = (addr + 1) & 0xFFFF;
					bank_offset &= low_mask;
					bank_num = (bank_num >> bank_shift)& high_mask;

					format = mnemonicsFDCB[MemoryMap[bank_num][bank_offset]];
					extra_inc = 1;
					break;
				case 0xED:
					format = mnemonicsED[A];
					break;
				default:
					format = mnemonicsFD[A];
					break;
				}
				break;
			default: format = mnemonics[A]; break;
			}

			uint32_t* addr_ptr = &addr;
			string temp = Result(format, addr_ptr);

			addr += extra_inc;

			size[0] = addr - start_addr;
			// handle case of addr wrapping around at 16 bit boundary
			if (addr < start_addr)
			{
				size[0] = (0x10000 + addr) - start_addr;
			}

			return temp;
		}

		/*
		string Disassemble(MemoryDomain m, uuint32_t addr, out uint32_t length)
		{
			string ret = Disassemble((uint32_t)addr, a = > m.PeekByte(a), out length);
			return ret;
		}
		*/

		const string mnemonics[256] = 
		{
			"NOP", "LD BC, nn", "LD (BC), A", "INC BC", //0x04
			"INC B", "DEC B", "LD B, n", "RLCA", //0x08
			"EX AF, AF'", "ADD HL, BC", "LD A, (BC)", "DEC BC", //0x0C
			"INC C", "DEC C", "LD C, n", "RRCA", //0x10
			"DJNZ d", "LD DE, nn", "LD (DE), A", "INC DE", //0x14
			"INC D", "DEC D", "LD D, n", "RLA", //0x18
			"JR d", "ADD HL, DE", "LD A, (DE)", "DEC DE", //0x1C
			"INC E", "DEC E", "LD E, n", "RRA", //0x20
			"JR NZ, d", "LD HL, nn", "LD (nn), HL", "INC HL", //0x24
			"INC H", "DEC H", "LD H, n", "DAA", //0x28
			"JR Z, d", "ADD HL, HL", "LD HL, (nn)", "DEC HL", //0x2C
			"INC L", "DEC L", "LD L, n", "CPL", //0x30
			"JR NC, d", "LD SP, nn", "LD (nn), A", "INC SP", //0x34
			"INC (HL)", "DEC (HL)", "LD (HL), n", "SCF", //0x38
			"JR C, d", "ADD HL, SP", "LD A, (nn)", "DEC SP", //0x3C
			"INC A", "DEC A", "LD A, n", "CCF", //0x40
			"LD B, B", "LD B, C", "LD B, D", "LD B, E", //0x44
			"LD B, H", "LD B, L", "LD B, (HL)", "LD B, A", //0x48
			"LD C, B", "LD C, C", "LD C, D", "LD C, E", //0x4C
			"LD C, H", "LD C, L", "LD C, (HL)", "LD C, A", //0x50
			"LD D, B", "LD D, C", "LD D, D", "LD D, E", //0x54
			"LD D, H", "LD D, L", "LD D, (HL)", "LD D, A", //0x58
			"LD E, B", "LD E, C", "LD E, D", "LD E, E", //0x5C
			"LD E, H", "LD E, L", "LD E, (HL)", "LD E, A", //0x60
			"LD H, B", "LD H, C", "LD H, D", "LD H, E", //0x64
			"LD H, H", "LD H, L", "LD H, (HL)", "LD H, A", //0x68
			"LD L, B", "LD L, B", "LD L, D", "LD L, E", //0x6C
			"LD L, H", "LD L, L", "LD L, (HL)", "LD L, A", //0x70
			"LD (HL), B", "LD (HL), C", "LD (HL), D", "LD (HL), E", //0x74
			"LD (HL), H", "LD (HL), L", "HALT", "LD (HL), A", //0x78
			"LD A, B", "LD A, C", "LD A, D", "LD A, E", //0x7C
			"LD A, H", "LD A, L", "LD A, (HL)", "LD A, A", //0x80
			"ADD A, B", "ADD A, C", "ADD A, D", "ADD A, E", //0x84
			"ADD A, H", "ADD A, L", "ADD A, (HL)", "ADD A, A", //0x88
			"ADC A, B", "ADC A, C", "ADC A, D", "ADC A, E", //0x8C
			"ADC A, H", "ADC A, L", "ADC A, (HL)", "ADC A, A", //0x90
			"SUB A, B", "SUB A, C", "SUB A, D", "SUB A, E", //0x94
			"SUB A, H", "SUB A, L", "SUB A, (HL)", "SUB A, A", //0x98
			"SBC A, B", "SBC A, C", "SBC A, D", "SBC A, E", //0x9C
			"SBC A, H", "SBC A, L", "SBC A, (HL)", "SBC A, A", //0xA0
			"AND B", "AND C", "AND D", "AND E", //0xA4
			"AND H", "AND L", "AND (HL)", "AND A", //0xA8
			"XOR B", "XOR C", "XOR D", "XOR E", //0xAC
			"XOR H", "XOR L", "XOR (HL)", "XOR A", //0xB0
			"OR B", "OR C", "OR D", "OR E", //0xB4
			"OR H", "OR L", "OR (HL)", "OR A", //0xB8
			"CP B", "CP C", "CP D", "CP E", //0xBC
			"CP H", "CP L", "CP (HL)", "CP A", //0xC0
			"RET NZ", "POP BC", "JP NZ, nn", "JP nn", //0xC4
			"CALL NZ, nn", "PUSH BC", "ADD A, n", "RST $00", //0xC8
			"RET Z", "RET", "JP Z, nn", "[CB]", //0xCC
			"CALL Z, nn", "CALL nn", "ADC A, n", "RST $08", //0xD0
			"RET NC", "POP DE", "JP NC, nn", "OUT n, A", //0xD4
			"CALL NC, nn", "PUSH DE", "SUB n", "RST $10", //0xD8
			"RET C", "EXX", "JP C, nn", "IN A, n", //0xDC
			"CALL C, nn", "[DD]", "SBC A, n", "RST $18", //0xE0
			"RET PO", "POP HL", "JP PO, nn", "EX (SP), HL", //0xE4
			"CALL C, nn", "PUSH HL", "AND n", "RST $20", //0xE8
			"RET PE", "JP HL", "JP PE, nn", "EX DE, HL", //0xEC
			"CALL PE, nn", "[ED]", "XOR n", "RST $28", //0xF0
			"RET P", "POP AF", "JP P, nn", "DI", //0xF4
			"CALL P, nn", "PUSH AF", "OR n", "RST $30", //0xF8
			"RET M", "LD SP, HL", "JP M, nn", "EI", //0xFC
			"CALL M, nn", "[FD]", "CP n", "RST $38", //0x100
		};

		const string mnemonicsDD[256] =
		{
			"NOP", "LD BC, nn", "LD (BC), A", "INC BC", //0x04
			"INC B", "DEC B", "LD B, n", "RLCA", //0x08
			"EX AF, AF'", "ADD IX, BC", "LD A, (BC)", "DEC BC", //0x0C
			"INC C", "DEC C", "LD C, n", "RRCA", //0x10
			"DJNZ d", "LD DE, nn", "LD (DE), A", "INC DE", //0x14
			"INC D", "DEC D", "LD D, n", "RLA", //0x18
			"JR d", "ADD IX, DE", "LD A, (DE)", "DEC DE", //0x1C
			"INC E", "DEC E", "LD E, n", "RRA", //0x20
			"JR NZ, d", "LD IX, nn", "LD (nn), IX", "INC IX", //0x24
			"INC IXH", "DEC IXH", "LD IXH, n", "DAA", //0x28
			"JR Z, d", "ADD IX, IX", "LD IX, (nn)", "DEC IX", //0x2C
			"INC IXL", "DEC IXL", "LD IXL, n", "CPL", //0x30
			"JR NC, d", "LD SP, nn", "LD (nn), A", "INC SP", //0x34
			"INC (IX+d)", "DEC (IX+d)", "LD (IX+d), n", "SCF", //0x38
			"JR C, d", "ADD IX, SP", "LD A, (nn)", "DEC SP", //0x3C
			"INC A", "DEC A", "LD A, n", "CCF", //0x40
			"LD B, B", "LD B, C", "LD B, D", "LD B, E", //0x44
			"LD B, IXH", "LD B, IXL", "LD B, (IX+d)", "LD B, A", //0x48
			"LD C, B", "LD C, C", "LD C, D", "LD C, E", //0x4C
			"LD C, IXH", "LD C, IXL", "LD C, (IX+d)", "LD C, A", //0x50
			"LD D, B", "LD D, C", "LD D, D", "LD D, E", //0x54
			"LD D, IXH", "LD D, IXL", "LD D, (IX+d)", "LD D, A", //0x58
			"LD E, B", "LD E, C", "LD E, D", "LD E, E", //0x5C
			"LD E, IXH", "LD E, IXL", "LD E, (IX+d)", "LD E, A", //0x60
			"LD IXH, B", "LD IXH, C", "LD IXH, D", "LD IXH, E", //0x64
			"LD IXH, IXH", "LD IXH, IXL", "LD H, (IX+d)", "LD IXH, A", //0x68
			"LD IXL, B", "LD IXL, C", "LD IXL, D", "LD IXL, E", //0x6C
			"LD IXL, IXH", "LD IXL, IXL", "LD L, (IX+d)", "LD IXL, A", //0x70
			"LD (IX+d), B", "LD (IX+d), C", "LD (IX+d), D", "LD (IX+d), E", //0x74
			"LD (IX+d), H", "LD (IX+d), L", "HALT", "LD (IX+d), A", //0x78
			"LD A, B", "LD A, C", "LD A, D", "LD A, E", //0x7C
			"LD A, IXH", "LD A, IXL", "LD A, (IX+d)", "LD A, A", //0x80
			"ADD A, B", "ADD A, C", "ADD A, D", "ADD A, E", //0x84
			"ADD A, IXH", "ADD A, IXL", "ADD A, (IX+d)", "ADD A, A", //0x88
			"ADC A, B", "ADC A, C", "ADC A, D", "ADC A, E", //0x8C
			"ADC A, IXH", "ADC A, IXL", "ADC A, (IX+d)", "ADC A, A", //0x90
			"SUB A, B", "SUB A, C", "SUB A, D", "SUB A, E", //0x94
			"SUB A, IXH", "SUB A, IXL", "SUB A, (IX+d)", "SUB A, A", //0x98
			"SBC A, B", "SBC A, C", "SBC A, D", "SBC A, E", //0x9C
			"SBC A, IXH", "SBC A, IXL", "SBC A, (IX+d)", "SBC A, A", //0xA0
			"AND B", "AND C", "AND D", "AND E", //0xA4
			"AND IXH", "AND IXL", "AND (IX+d)", "AND A", //0xA8
			"XOR B", "XOR C", "XOR D", "XOR E", //0xAC
			"XOR IXH", "XOR IXL", "XOR (IX+d)", "XOR A", //0xB0
			"OR B", "OR C", "OR D", "OR E", //0xB4
			"OR IXH", "OR IXL", "OR (IX+d)", "OR A", //0xB8
			"CP B", "CP C", "CP D", "CP E", //0xBC
			"CP IXH", "CP IXL", "CP (IX+d)", "CP A", //0xC0
			"RET NZ", "POP BC", "JP NZ, nn", "JP nn", //0xC4
			"CALL NZ, nn", "PUSH BC", "ADD A, n", "RST $00", //0xC8
			"RET Z", "RET", "JP Z, nn", "[DD CB]", //0xCC
			"CALL Z, nn", "CALL nn", "ADC A, n", "RST $08", //0xD0
			"RET NC", "POP DE", "JP NC, nn", "OUT n, A", //0xD4
			"CALL NC, nn", "PUSH DE", "SUB n", "RST $10", //0xD8
			"RET C", "EXX", "JP C, nn", "IN A, n", //0xDC
			"CALL C, nn", "[!DD DD!]", "SBC A, n", "RST $18", //0xE0
			"RET PO", "POP IX", "JP PO, nn", "EX (SP), IX", //0xE4
			"CALL C, nn", "PUSH IX", "AND n", "RST $20", //0xE8
			"RET PE", "JP IX", "JP PE, nn", "EX DE, HL", //0xEC
			"CALL PE, nn", "[DD ED]", "XOR n", "RST $28", //0xF0
			"RET P", "POP AF", "JP P, nn", "DI", //0xF4
			"CALL P, nn", "PUSH AF", "OR n", "RST $30", //0xF8
			"RET M", "LD SP, IX", "JP M, nn", "EI", //0xFC
			"CALL M, nn", "[!!DD FD!!]", "CP n", "RST $38", //0x100
		};

		const string mnemonicsFD[256] =
		{
			"NOP", "LD BC, nn", "LD (BC), A", "INC BC", //0x04
			"INC B", "DEC B", "LD B, n", "RLCA", //0x08
			"EX AF, AF'", "ADD IY, BC", "LD A, (BC)", "DEC BC", //0x0C
			"INC C", "DEC C", "LD C, n", "RRCA", //0x10
			"DJNZ d", "LD DE, nn", "LD (DE), A", "INC DE", //0x14
			"INC D", "DEC D", "LD D, n", "RLA", //0x18
			"JR d", "ADD IY, DE", "LD A, (DE)", "DEC DE", //0x1C
			"INC E", "DEC E", "LD E, n", "RRA", //0x20
			"JR NZ, d", "LD IY, nn", "LD (nn), IY", "INC IY", //0x24
			"INC IYH", "DEC IYH", "LD IYH, n", "DAA", //0x28
			"JR Z, d", "ADD IY, IY", "LD IY, (nn)", "DEC IY", //0x2C
			"INC IYL", "DEC IYL", "LD IYL, n", "CPL", //0x30
			"JR NC, d", "LD SP, nn", "LD (nn), A", "INC SP", //0x34
			"INC (IY+d)", "DEC (IY+d)", "LD (IY+d), n", "SCF", //0x38
			"JR C, d", "ADD IY, SP", "LD A, (nn)", "DEC SP", //0x3C
			"INC A", "DEC A", "LD A, n", "CCF", //0x40
			"LD B, B", "LD B, C", "LD B, D", "LD B, E", //0x44
			"LD B, IYH", "LD B, IYL", "LD B, (IY+d)", "LD B, A", //0x48
			"LD C, B", "LD C, C", "LD C, D", "LD C, E", //0x4C
			"LD C, IYH", "LD C, IYL", "LD C, (IY+d)", "LD C, A", //0x50
			"LD D, B", "LD D, C", "LD D, D", "LD D, E", //0x54
			"LD D, IYH", "LD D, IYL", "LD D, (IY+d)", "LD D, A", //0x58
			"LD E, B", "LD E, C", "LD E, D", "LD E, E", //0x5C
			"LD E, IYH", "LD E, IYL", "LD E, (IY+d)", "LD E, A", //0x60
			"LD IYH, B", "LD IYH, C", "LD IYH, D", "LD IYH, E", //0x64
			"LD IYH, IYH", "LD IYH, IYL", "LD H, (IY+d)", "LD IYH, A", //0x68
			"LD IYL, B", "LD IYL, C", "LD IYL, D", "LD IYL, E", //0x6C
			"LD IYL, IYH", "LD IYL, IYL", "LD L, (IY+d)", "LD IYL, A", //0x70
			"LD (IY+d), B", "LD (IY+d), C", "LD (IY+d), D", "LD (IY+d), E", //0x74
			"LD (IY+d), H", "LD (IY+d), L", "HALT", "LD (IY+d), A", //0x78
			"LD A, B", "LD A, C", "LD A, D", "LD A, E", //0x7C
			"LD A, IYH", "LD A, IYL", "LD A, (IY+d)", "LD A, A", //0x80
			"ADD A, B", "ADD A, C", "ADD A, D", "ADD A, E", //0x84
			"ADD A, IYH", "ADD A, IYL", "ADD A, (IY+d)", "ADD A, A", //0x88
			"ADC A, B", "ADC A, C", "ADC A, D", "ADC A, E", //0x8C
			"ADC A, IYH", "ADC A, IYL", "ADC A, (IY+d)", "ADC A, A", //0x90
			"SUB A, B", "SUB A, C", "SUB A, D", "SUB A, E", //0x94
			"SUB A, IYH", "SUB A, IYL", "SUB A, (IY+d)", "SUB A, A", //0x98
			"SBC A, B", "SBC A, C", "SBC A, D", "SBC A, E", //0x9C
			"SBC A, IYH", "SBC A, IYL", "SBC A, (IY+d)", "SBC A, A", //0xA0
			"AND B", "AND C", "AND D", "AND E", //0xA4
			"AND IYH", "AND IYL", "AND (IY+d)", "AND A", //0xA8
			"XOR B", "XOR C", "XOR D", "XOR E", //0xAC
			"XOR IYH", "XOR IYL", "XOR (IY+d)", "XOR A", //0xB0
			"OR B", "OR C", "OR D", "OR E", //0xB4
			"OR IYH", "OR IYL", "OR (IY+d)", "OR A", //0xB8
			"CP B", "CP C", "CP D", "CP E", //0xBC
			"CP IYH", "CP IYL", "CP (IY+d)", "CP A", //0xC0
			"RET NZ", "POP BC", "JP NZ, nn", "JP nn", //0xC4
			"CALL NZ, nn", "PUSH BC", "ADD A, n", "RST $00", //0xC8
			"RET Z", "RET", "JP Z, nn", "[DD CB]", //0xCC
			"CALL Z, nn", "CALL nn", "ADC A, n", "RST $08", //0xD0
			"RET NC", "POP DE", "JP NC, nn", "OUT n, A", //0xD4
			"CALL NC, nn", "PUSH DE", "SUB n", "RST $10", //0xD8
			"RET C", "EXX", "JP C, nn", "IN A, n", //0xDC
			"CALL C, nn", "[!FD DD!]", "SBC A, n", "RST $18", //0xE0
			"RET PO", "POP IY", "JP PO, nn", "EX (SP), IY", //0xE4
			"CALL C, nn", "PUSH IY", "AND n", "RST $20", //0xE8
			"RET PE", "JP IY", "JP PE, nn", "EX DE, HL", //0xEC
			"CALL PE, nn", "[FD ED]", "XOR n", "RST $28", //0xF0
			"RET P", "POP AF", "JP P, nn", "DI", //0xF4
			"CALL P, nn", "PUSH AF", "OR n", "RST $30", //0xF8
			"RET M", "LD SP, IY", "JP M, nn", "EI", //0xFC
			"CALL M, nn", "[!FD FD!]", "CP n", "RST $38", //0x100
		};

		const string mnemonicsDDCB[256] =
		{
			"RLC (IX+d)->B", "RLC (IX+d)->C", "RLC (IX+d)->D", "RLC (IX+d)->E", "RLC (IX+d)->H", "RLC (IX+d)->L", "RLC (IX+d)", "RLC (IX+d)->A",
			"RRC (IX+d)->B", "RRC (IX+d)->C", "RRC (IX+d)->D", "RRC (IX+d)->E", "RRC (IX+d)->H", "RRC (IX+d)->L", "RRC (IX+d)", "RRC (IX+d)->A",
			"RL (IX+d)->B", "RL (IX+d)->C", "RL (IX+d)->D", "RL (IX+d)->E", "RL (IX+d)->H", "RL (IX+d)->L", "RL (IX+d)", "RL (IX+d)->A",
			"RR (IX+d)->B", "RR (IX+d)->C", "RR (IX+d)->D", "RR (IX+d)->E", "RR (IX+d)->H", "RR (IX+d)->L", "RR (IX+d)", "RR (IX+d)->A",
			"SLA (IX+d)->B", "SLA (IX+d)->C", "SLA (IX+d)->D", "SLA (IX+d)->E", "SLA (IX+d)->H", "SLA (IX+d)->L", "SLA (IX+d)", "SLA (IX+d)->A",
			"SRA (IX+d)->B", "SRA (IX+d)->C", "SRA (IX+d)->D", "SRA (IX+d)->E", "SRA (IX+d)->H", "SRA (IX+d)->L", "SRA (IX+d)", "SRA (IX+d)->A",
			"SL1 (IX+d)->B", "SL1 (IX+d)->C", "SL1 (IX+d)->D", "SL1 (IX+d)->E", "SL1 (IX+d)->H", "SL1 (IX+d)->L", "SL1 (IX+d)", "SL1 (IX+d)->A",
			"SRL (IX+d)->B", "SRL (IX+d)->C", "SRL (IX+d)->D", "SRL (IX+d)->E", "SRL (IX+d)->H", "SRL (IX+d)->L", "SRL (IX+d)", "SRL (IX+d)->A",
			"BIT 0, (IX+d)", "BIT 0, (IX+d)", "BIT 0, (IX+d)", "BIT 0, (IX+d)", "BIT 0, (IX+d)", "BIT 0, (IX+d)", "BIT 0, (IX+d)", "BIT 0, (IX+d)",
			"BIT 1, (IX+d)", "BIT 1, (IX+d)", "BIT 1, (IX+d)", "BIT 1, (IX+d)", "BIT 1, (IX+d)", "BIT 1, (IX+d)", "BIT 1, (IX+d)", "BIT 1, (IX+d)",
			"BIT 2, (IX+d)", "BIT 2, (IX+d)", "BIT 2, (IX+d)", "BIT 2, (IX+d)", "BIT 2, (IX+d)", "BIT 2, (IX+d)", "BIT 2, (IX+d)", "BIT 2, (IX+d)",
			"BIT 3, (IX+d)", "BIT 3, (IX+d)", "BIT 3, (IX+d)", "BIT 3, (IX+d)", "BIT 3, (IX+d)", "BIT 3, (IX+d)", "BIT 3, (IX+d)", "BIT 3, (IX+d)",
			"BIT 4, (IX+d)", "BIT 4, (IX+d)", "BIT 4, (IX+d)", "BIT 4, (IX+d)", "BIT 4, (IX+d)", "BIT 4, (IX+d)", "BIT 4, (IX+d)", "BIT 4, (IX+d)",
			"BIT 5, (IX+d)", "BIT 5, (IX+d)", "BIT 5, (IX+d)", "BIT 5, (IX+d)", "BIT 5, (IX+d)", "BIT 5, (IX+d)", "BIT 5, (IX+d)", "BIT 5, (IX+d)",
			"BIT 6, (IX+d)", "BIT 6, (IX+d)", "BIT 6, (IX+d)", "BIT 6, (IX+d)", "BIT 6, (IX+d)", "BIT 6, (IX+d)", "BIT 6, (IX+d)", "BIT 6, (IX+d)",
			"BIT 7, (IX+d)", "BIT 7, (IX+d)", "BIT 7, (IX+d)", "BIT 7, (IX+d)", "BIT 7, (IX+d)", "BIT 7, (IX+d)", "BIT 7, (IX+d)", "BIT 7, (IX+d)",
			"RES 0 (IX+d)->B", "RES 0 (IX+d)->C", "RES 0 (IX+d)->D", "RES 0 (IX+d)->E", "RES 0 (IX+d)->H", "RES 0 (IX+d)->L", "RES 0 (IX+d)", "RES 0 (IX+d)->A",
			"RES 1 (IX+d)->B", "RES 1 (IX+d)->C", "RES 1 (IX+d)->D", "RES 1 (IX+d)->E", "RES 1 (IX+d)->H", "RES 1 (IX+d)->L", "RES 1 (IX+d)", "RES 1 (IX+d)->A",
			"RES 2 (IX+d)->B", "RES 2 (IX+d)->C", "RES 2 (IX+d)->D", "RES 2 (IX+d)->E", "RES 2 (IX+d)->H", "RES 2 (IX+d)->L", "RES 2 (IX+d)", "RES 2 (IX+d)->A",
			"RES 3 (IX+d)->B", "RES 3 (IX+d)->C", "RES 3 (IX+d)->D", "RES 3 (IX+d)->E", "RES 3 (IX+d)->H", "RES 3 (IX+d)->L", "RES 3 (IX+d)", "RES 3 (IX+d)->A",
			"RES 4 (IX+d)->B", "RES 4 (IX+d)->C", "RES 4 (IX+d)->D", "RES 4 (IX+d)->E", "RES 4 (IX+d)->H", "RES 4 (IX+d)->L", "RES 4 (IX+d)", "RES 4 (IX+d)->A",
			"RES 5 (IX+d)->B", "RES 5 (IX+d)->C", "RES 5 (IX+d)->D", "RES 5 (IX+d)->E", "RES 5 (IX+d)->H", "RES 5 (IX+d)->L", "RES 5 (IX+d)", "RES 5 (IX+d)->A",
			"RES 6 (IX+d)->B", "RES 6 (IX+d)->C", "RES 6 (IX+d)->D", "RES 6 (IX+d)->E", "RES 6 (IX+d)->H", "RES 6 (IX+d)->L", "RES 6 (IX+d)", "RES 6 (IX+d)->A",
			"RES 7 (IX+d)->B", "RES 7 (IX+d)->C", "RES 7 (IX+d)->D", "RES 7 (IX+d)->E", "RES 7 (IX+d)->H", "RES 7 (IX+d)->L", "RES 7 (IX+d)", "RES 7 (IX+d)->A",
			"SET 0 (IX+d)->B", "SET 0 (IX+d)->C", "SET 0 (IX+d)->D", "SET 0 (IX+d)->E", "SET 0 (IX+d)->H", "SET 0 (IX+d)->L", "SET 0 (IX+d)", "SET 0 (IX+d)->A",
			"SET 1 (IX+d)->B", "SET 1 (IX+d)->C", "SET 1 (IX+d)->D", "SET 1 (IX+d)->E", "SET 1 (IX+d)->H", "SET 1 (IX+d)->L", "SET 1 (IX+d)", "SET 1 (IX+d)->A",
			"SET 2 (IX+d)->B", "SET 2 (IX+d)->C", "SET 2 (IX+d)->D", "SET 2 (IX+d)->E", "SET 2 (IX+d)->H", "SET 2 (IX+d)->L", "SET 2 (IX+d)", "SET 2 (IX+d)->A",
			"SET 3 (IX+d)->B", "SET 3 (IX+d)->C", "SET 3 (IX+d)->D", "SET 3 (IX+d)->E", "SET 3 (IX+d)->H", "SET 3 (IX+d)->L", "SET 3 (IX+d)", "SET 3 (IX+d)->A",
			"SET 4 (IX+d)->B", "SET 4 (IX+d)->C", "SET 4 (IX+d)->D", "SET 4 (IX+d)->E", "SET 4 (IX+d)->H", "SET 4 (IX+d)->L", "SET 4 (IX+d)", "SET 4 (IX+d)->A",
			"SET 5 (IX+d)->B", "SET 5 (IX+d)->C", "SET 5 (IX+d)->D", "SET 5 (IX+d)->E", "SET 5 (IX+d)->H", "SET 5 (IX+d)->L", "SET 5 (IX+d)", "SET 5 (IX+d)->A",
			"SET 6 (IX+d)->B", "SET 6 (IX+d)->C", "SET 6 (IX+d)->D", "SET 6 (IX+d)->E", "SET 6 (IX+d)->H", "SET 6 (IX+d)->L", "SET 6 (IX+d)", "SET 6 (IX+d)->A",
			"SET 7 (IX+d)->B", "SET 7 (IX+d)->C", "SET 7 (IX+d)->D", "SET 7 (IX+d)->E", "SET 7 (IX+d)->H", "SET 7 (IX+d)->L", "SET 7 (IX+d)", "SET 7 (IX+d)->A",
		};

		const string mnemonicsFDCB[256] =
		{
			"RLC (IY+d)->B", "RLC (IY+d)->C", "RLC (IY+d)->D", "RLC (IY+d)->E", "RLC (IY+d)->H", "RLC (IY+d)->L", "RLC (IY+d)", "RLC (IY+d)->A",
			"RRC (IY+d)->B", "RRC (IY+d)->C", "RRC (IY+d)->D", "RRC (IY+d)->E", "RRC (IY+d)->H", "RRC (IY+d)->L", "RRC (IY+d)", "RRC (IY+d)->A",
			"RL (IY+d)->B", "RL (IY+d)->C", "RL (IY+d)->D", "RL (IY+d)->E", "RL (IY+d)->H", "RL (IY+d)->L", "RL (IY+d)", "RL (IY+d)->A",
			"RR (IY+d)->B", "RR (IY+d)->C", "RR (IY+d)->D", "RR (IY+d)->E", "RR (IY+d)->H", "RR (IY+d)->L", "RR (IY+d)", "RR (IY+d)->A",
			"SLA (IY+d)->B", "SLA (IY+d)->C", "SLA (IY+d)->D", "SLA (IY+d)->E", "SLA (IY+d)->H", "SLA (IY+d)->L", "SLA (IY+d)", "SLA (IY+d)->A",
			"SRA (IY+d)->B", "SRA (IY+d)->C", "SRA (IY+d)->D", "SRA (IY+d)->E", "SRA (IY+d)->H", "SRA (IY+d)->L", "SRA (IY+d)", "SRA (IY+d)->A",
			"SL1 (IY+d)->B", "SL1 (IY+d)->C", "SL1 (IY+d)->D", "SL1 (IY+d)->E", "SL1 (IY+d)->H", "SL1 (IY+d)->L", "SL1 (IY+d)", "SL1 (IY+d)->A",
			"SRL (IY+d)->B", "SRL (IY+d)->C", "SRL (IY+d)->D", "SRL (IY+d)->E", "SRL (IY+d)->H", "SRL (IY+d)->L", "SRL (IY+d)", "SRL (IY+d)->A",
			"BIT 0, (IY+d)", "BIT 0, (IY+d)", "BIT 0, (IY+d)", "BIT 0, (IY+d)", "BIT 0, (IY+d)", "BIT 0, (IY+d)", "BIT 0, (IY+d)", "BIT 0, (IY+d)",
			"BIT 1, (IY+d)", "BIT 1, (IY+d)", "BIT 1, (IY+d)", "BIT 1, (IY+d)", "BIT 1, (IY+d)", "BIT 1, (IY+d)", "BIT 1, (IY+d)", "BIT 1, (IY+d)",
			"BIT 2, (IY+d)", "BIT 2, (IY+d)", "BIT 2, (IY+d)", "BIT 2, (IY+d)", "BIT 2, (IY+d)", "BIT 2, (IY+d)", "BIT 2, (IY+d)", "BIT 2, (IY+d)",
			"BIT 3, (IY+d)", "BIT 3, (IY+d)", "BIT 3, (IY+d)", "BIT 3, (IY+d)", "BIT 3, (IY+d)", "BIT 3, (IY+d)", "BIT 3, (IY+d)", "BIT 3, (IY+d)",
			"BIT 4, (IY+d)", "BIT 4, (IY+d)", "BIT 4, (IY+d)", "BIT 4, (IY+d)", "BIT 4, (IY+d)", "BIT 4, (IY+d)", "BIT 4, (IY+d)", "BIT 4, (IY+d)",
			"BIT 5, (IY+d)", "BIT 5, (IY+d)", "BIT 5, (IY+d)", "BIT 5, (IY+d)", "BIT 5, (IY+d)", "BIT 5, (IY+d)", "BIT 5, (IY+d)", "BIT 5, (IY+d)",
			"BIT 6, (IY+d)", "BIT 6, (IY+d)", "BIT 6, (IY+d)", "BIT 6, (IY+d)", "BIT 6, (IY+d)", "BIT 6, (IY+d)", "BIT 6, (IY+d)", "BIT 6, (IY+d)",
			"BIT 7, (IY+d)", "BIT 7, (IY+d)", "BIT 7, (IY+d)", "BIT 7, (IY+d)", "BIT 7, (IY+d)", "BIT 7, (IY+d)", "BIT 7, (IY+d)", "BIT 7, (IY+d)",
			"RES 0 (IY+d)->B", "RES 0 (IY+d)->C", "RES 0 (IY+d)->D", "RES 0 (IY+d)->E", "RES 0 (IY+d)->H", "RES 0 (IY+d)->L", "RES 0 (IY+d)", "RES 0 (IY+d)->A",
			"RES 1 (IY+d)->B", "RES 1 (IY+d)->C", "RES 1 (IY+d)->D", "RES 1 (IY+d)->E", "RES 1 (IY+d)->H", "RES 1 (IY+d)->L", "RES 1 (IY+d)", "RES 1 (IY+d)->A",
			"RES 2 (IY+d)->B", "RES 2 (IY+d)->C", "RES 2 (IY+d)->D", "RES 2 (IY+d)->E", "RES 2 (IY+d)->H", "RES 2 (IY+d)->L", "RES 2 (IY+d)", "RES 2 (IY+d)->A",
			"RES 3 (IY+d)->B", "RES 3 (IY+d)->C", "RES 3 (IY+d)->D", "RES 3 (IY+d)->E", "RES 3 (IY+d)->H", "RES 3 (IY+d)->L", "RES 3 (IY+d)", "RES 3 (IY+d)->A",
			"RES 4 (IY+d)->B", "RES 4 (IY+d)->C", "RES 4 (IY+d)->D", "RES 4 (IY+d)->E", "RES 4 (IY+d)->H", "RES 4 (IY+d)->L", "RES 4 (IY+d)", "RES 4 (IY+d)->A",
			"RES 5 (IY+d)->B", "RES 5 (IY+d)->C", "RES 5 (IY+d)->D", "RES 5 (IY+d)->E", "RES 5 (IY+d)->H", "RES 5 (IY+d)->L", "RES 5 (IY+d)", "RES 5 (IY+d)->A",
			"RES 6 (IY+d)->B", "RES 6 (IY+d)->C", "RES 6 (IY+d)->D", "RES 6 (IY+d)->E", "RES 6 (IY+d)->H", "RES 6 (IY+d)->L", "RES 6 (IY+d)", "RES 6 (IY+d)->A",
			"RES 7 (IY+d)->B", "RES 7 (IY+d)->C", "RES 7 (IY+d)->D", "RES 7 (IY+d)->E", "RES 7 (IY+d)->H", "RES 7 (IY+d)->L", "RES 7 (IY+d)", "RES 7 (IY+d)->A",
			"SET 0 (IY+d)->B", "SET 0 (IY+d)->C", "SET 0 (IY+d)->D", "SET 0 (IY+d)->E", "SET 0 (IY+d)->H", "SET 0 (IY+d)->L", "SET 0 (IY+d)", "SET 0 (IY+d)->A",
			"SET 1 (IY+d)->B", "SET 1 (IY+d)->C", "SET 1 (IY+d)->D", "SET 1 (IY+d)->E", "SET 1 (IY+d)->H", "SET 1 (IY+d)->L", "SET 1 (IY+d)", "SET 1 (IY+d)->A",
			"SET 2 (IY+d)->B", "SET 2 (IY+d)->C", "SET 2 (IY+d)->D", "SET 2 (IY+d)->E", "SET 2 (IY+d)->H", "SET 2 (IY+d)->L", "SET 2 (IY+d)", "SET 2 (IY+d)->A",
			"SET 3 (IY+d)->B", "SET 3 (IY+d)->C", "SET 3 (IY+d)->D", "SET 3 (IY+d)->E", "SET 3 (IY+d)->H", "SET 3 (IY+d)->L", "SET 3 (IY+d)", "SET 3 (IY+d)->A",
			"SET 4 (IY+d)->B", "SET 4 (IY+d)->C", "SET 4 (IY+d)->D", "SET 4 (IY+d)->E", "SET 4 (IY+d)->H", "SET 4 (IY+d)->L", "SET 4 (IY+d)", "SET 4 (IY+d)->A",
			"SET 5 (IY+d)->B", "SET 5 (IY+d)->C", "SET 5 (IY+d)->D", "SET 5 (IY+d)->E", "SET 5 (IY+d)->H", "SET 5 (IY+d)->L", "SET 5 (IY+d)", "SET 5 (IY+d)->A",
			"SET 6 (IY+d)->B", "SET 6 (IY+d)->C", "SET 6 (IY+d)->D", "SET 6 (IY+d)->E", "SET 6 (IY+d)->H", "SET 6 (IY+d)->L", "SET 6 (IY+d)", "SET 6 (IY+d)->A",
			"SET 7 (IY+d)->B", "SET 7 (IY+d)->C", "SET 7 (IY+d)->D", "SET 7 (IY+d)->E", "SET 7 (IY+d)->H", "SET 7 (IY+d)->L", "SET 7 (IY+d)", "SET 7 (IY+d)->A",
		};

		const string mnemonicsCB[256] =
		{
			"RLC B", "RLC C", "RLC D", "RLC E", "RLC H", "RLC L", "RLC (HL)", "RLC A",
			"RRC B", "RRC C", "RRC D", "RRC E", "RRC H", "RRC L", "RRC (HL)", "RRC A",
			"RL B", "RL C", "RL D", "RL E", "RL H", "RL L", "RL (HL)", "RL A",
			"RR B", "RR C", "RR D", "RR E", "RR H", "RR L", "RR (HL)", "RR A",
			"SLA B", "SLA C", "SLA D", "SLA E", "SLA H", "SLA L", "SLA (HL)", "SLA A",
			"SRA B", "SRA C", "SRA D", "SRA E", "SRA H", "SRA L", "SRA (HL)", "SRA A",
			"SL1 B", "SL1 C", "SL1 D", "SL1 E", "SL1 H", "SL1 L", "SL1 (HL)", "SL1 A",
			"SRL B", "SRL C", "SRL D", "SRL E", "SRL H", "SRL L", "SRL (HL)", "SRL A",
			"BIT 0, B", "BIT 0, C", "BIT 0, D", "BIT 0, E", "BIT 0, H", "BIT 0, L", "BIT 0, (HL)", "BIT 0, A",
			"BIT 1, B", "BIT 1, C", "BIT 1, D", "BIT 1, E", "BIT 1, H", "BIT 1, L", "BIT 1, (HL)", "BIT 1, A",
			"BIT 2, B", "BIT 2, C", "BIT 2, D", "BIT 2, E", "BIT 2, H", "BIT 2, L", "BIT 2, (HL)", "BIT 2, A",
			"BIT 3, B", "BIT 3, C", "BIT 3, D", "BIT 3, E", "BIT 3, H", "BIT 3, L", "BIT 3, (HL)", "BIT 3, A",
			"BIT 4, B", "BIT 4, C", "BIT 4, D", "BIT 4, E", "BIT 4, H", "BIT 4, L", "BIT 4, (HL)", "BIT 4, A",
			"BIT 5, B", "BIT 5, C", "BIT 5, D", "BIT 5, E", "BIT 5, H", "BIT 5, L", "BIT 5, (HL)", "BIT 5, A",
			"BIT 6, B", "BIT 6, C", "BIT 6, D", "BIT 6, E", "BIT 6, H", "BIT 6, L", "BIT 6, (HL)", "BIT 6, A",
			"BIT 7, B", "BIT 7, C", "BIT 7, D", "BIT 7, E", "BIT 7, H", "BIT 7, L", "BIT 7, (HL)", "BIT 7, A",
			"RES 0, B", "RES 0, C", "RES 0, D", "RES 0, E", "RES 0, H", "RES 0, L", "RES 0, (HL)", "RES 0, A",
			"RES 1, B", "RES 1, C", "RES 1, D", "RES 1, E", "RES 1, H", "RES 1, L", "RES 1, (HL)", "RES 1, A",
			"RES 2, B", "RES 2, C", "RES 2, D", "RES 2, E", "RES 2, H", "RES 2, L", "RES 2, (HL)", "RES 2, A",
			"RES 3, B", "RES 3, C", "RES 3, D", "RES 3, E", "RES 3, H", "RES 3, L", "RES 3, (HL)", "RES 3, A",
			"RES 4, B", "RES 4, C", "RES 4, D", "RES 4, E", "RES 4, H", "RES 4, L", "RES 4, (HL)", "RES 4, A",
			"RES 5, B", "RES 5, C", "RES 5, D", "RES 5, E", "RES 5, H", "RES 5, L", "RES 5, (HL)", "RES 5, A",
			"RES 6, B", "RES 6, C", "RES 6, D", "RES 6, E", "RES 6, H", "RES 6, L", "RES 6, (HL)", "RES 6, A",
			"RES 7, B", "RES 7, C", "RES 7, D", "RES 7, E", "RES 7, H", "RES 7, L", "RES 7, (HL)", "RES 7, A",
			"SET 0, B", "SET 0, C", "SET 0, D", "SET 0, E", "SET 0, H", "SET 0, L", "SET 0, (HL)", "SET 0, A",
			"SET 1, B", "SET 1, C", "SET 1, D", "SET 1, E", "SET 1, H", "SET 1, L", "SET 1, (HL)", "SET 1, A",
			"SET 2, B", "SET 2, C", "SET 2, D", "SET 2, E", "SET 2, H", "SET 2, L", "SET 2, (HL)", "SET 2, A",
			"SET 3, B", "SET 3, C", "SET 3, D", "SET 3, E", "SET 3, H", "SET 3, L", "SET 3, (HL)", "SET 3, A",
			"SET 4, B", "SET 4, C", "SET 4, D", "SET 4, E", "SET 4, H", "SET 4, L", "SET 4, (HL)", "SET 4, A",
			"SET 5, B", "SET 5, C", "SET 5, D", "SET 5, E", "SET 5, H", "SET 5, L", "SET 5, (HL)", "SET 5, A",
			"SET 6, B", "SET 6, C", "SET 6, D", "SET 6, E", "SET 6, H", "SET 6, L", "SET 6, (HL)", "SET 6, A",
			"SET 7, B", "SET 7, C", "SET 7, D", "SET 7, E", "SET 7, H", "SET 7, L", "SET 7, (HL)", "SET 7, A",
		};

		const string mnemonicsED[256] =
		{
			"NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP",
			"NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP",
			"NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP",
			"NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP",

			"IN B, C", "OUT C, B", "SBC HL, BC", "LD (nn), BC", //0x44
			"NEG", "RETN", "IM $0", "LD I, A", //0x48
			"IN C, C", "OUT C, C", "ADC HL, BC", "LD BC, (nn)", //0x4C
			"NEG", "RETI", "IM $0", "LD R, A", //0x50
			"IN D, C", "OUT C, D", "SBC HL, DE", "LD (nn), DE", //0x54
			"NEG", "RETN", "IM $1", "LD A, I", //0x58
			"IN E, C", "OUT C, E", "ADC HL, DE", "LD DE, (nn)", //0x5C
			"NEG", "RETI", "IM $2", "LD A, R", //0x60

			"IN H, C", "OUT C, H", "SBC HL, HL", "LD (nn), HL", //0x64
			"NEG", "RETN", "IM $0", "RRD", //0x68
			"IN L, C", "OUT C, L", "ADC HL, HL", "LD HL, (nn)", //0x6C
			"NEG", "RETI", "IM $0", "RLD", //0x70
			"IN 0, C", "OUT C, 0", "SBC HL, SP", "LD (nn), SP", //0x74
			"NEG", "RETN", "IM $1", "NOP", //0x78
			"IN A, C", "OUT C, A", "ADC HL, SP", "LD SP, (nn)", //0x7C
			"NEG", "RETI", "IM $2", "NOP", //0x80

			"NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", //0x90
			"NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", //0xA0
			"LDI", "CPI", "INI", "OUTI", //0xA4
			"NOP", "NOP", "NOP", "NOP", //0xA8
			"LDD", "CPD", "IND", "OUTD", //0xAC
			"NOP", "NOP", "NOP", "NOP", //0xB0
			"LDIR", "CPIR", "INIR", "OTIR", //0xB4
			"NOP", "NOP", "NOP", "NOP", //0xB8
			"LDDR", "CPDR", "INDR", "OTDR", //0xBC
			"NOP", "NOP", "NOP", "NOP", //0xC0

			"NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", //0xD0
			"NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", //0xE0
			"NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", //0xF0
			"NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", "NOP", //0x100
		};
		#pragma endregion
	};
}
