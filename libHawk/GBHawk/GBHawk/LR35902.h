#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

using namespace std;

namespace GBHawk
{
	class MemoryManager;
	
	class LR35902
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
		
		// these are not savestated as they are automatically adjusted from the memory map upon load
		uint32_t bank_num;
		uint32_t bank_offset;
		uint8_t* MemoryMap[64];
		uint8_t MemoryMapMask[64];

		void WriteMemory(uint32_t, uint8_t);
		uint8_t ReadMemory(uint32_t);
		uint8_t SpeedFunc(uint32_t);

		// State variables
		uint64_t TotalExecutedCycles;

		uint32_t EI_pending;
		bool interrupts_enabled;

		// variables for executing instructions
		int instr_pntr = 0;
		int opcode;
		bool CB_prefix;
		bool halted;
		bool stopped;
		bool jammed;
		int LY;

		// unsaved variables
		bool checker;
		uint8_t Regs[14] = {};

		bool was_FlagI, FlagI;


		uint32_t PRE_SRC;		
		// variables for executing instructions
		uint32_t stepper = 0;
		uint32_t instr_pntr = 0;
		uint32_t bus_pntr = 0;
		uint32_t mem_pntr = 0;
		uint32_t irq_pntr = 0;
		uint32_t IRQS;
		uint32_t Ztemp2_saver = 0;
		uint32_t IRQS_cond_offset;

		uint64_t TotalExecutedCycles;
		
		uint32_t* cur_instr_ofst = nullptr;
		uint32_t* cur_bus_ofst = nullptr;
		uint32_t* cur_mem_ofst = nullptr;
		uint32_t* cur_irqs_ofst = nullptr;
		
		// non-state variables
		bool checker;


		uint32_t Ztemp1, Ztemp2, Ztemp3, Ztemp4;
		uint32_t Reg16_d, Reg16_s, ans, temp, carry;
		uint32_t cur_instr[60] = {};	 // only used for building
		uint32_t instr_table[256 * 2 * 60 + 60 * 8] = {}; // compiled instruction table

		#pragma endregion

		#pragma region Constant Declarations
		// operations that can take place in an instruction
		const static uint32_t IDLE = 0;
		const static uint32_t OP = 1;
		const static uint32_t RD = 2;
		const static uint32_t WR = 3;
		const static uint32_t TR = 4;
		const static uint32_t ADD16 = 5;
		const static uint32_t ADD8 = 6;
		const static uint32_t SUB8 = 7;
		const static uint32_t ADC8 = 8;
		const static uint32_t SBC8 = 9;
		const static uint32_t INC16 = 10;
		const static uint32_t INC8 = 11;
		const static uint32_t DEC16 = 12;
		const static uint32_t DEC8 = 13;
		const static uint32_t RLC = 14;
		const static uint32_t RL = 15;
		const static uint32_t RRC = 16;
		const static uint32_t RR = 17;
		const static uint32_t CPL = 18;
		const static uint32_t DA = 19;
		const static uint32_t SCF = 20;
		const static uint32_t CCF = 21;
		const static uint32_t AND8 = 22;
		const static uint32_t XOR8 = 23;
		const static uint32_t OR8 = 24;
		const static uint32_t CP8 = 25;
		const static uint32_t SLA = 26;
		const static uint32_t SRA = 27;
		const static uint32_t SRL = 28;
		const static uint32_t SWAP = 29;
		const static uint32_t BIT = 30;
		const static uint32_t RES = 31;
		const static uint32_t SET = 32;
		const static uint32_t EI = 33;
		const static uint32_t DI = 34;
		const static uint32_t HALT = 35;
		const static uint32_t STOP = 36;
		const static uint32_t PREFIX = 37;
		const static uint32_t ASGN = 38;
		const static uint32_t ADDS = 39; // signed 16 bit operation used in 2 instructions
		const static uint32_t OP_G = 40; // glitchy opcode read performed by halt when interrupts disabled
		const static uint32_t JAM = 41;  // all undocumented opcodes jam the machine
		const static uint32_t RD_F = 42; // special read case to pop value into F
		const static uint32_t EI_RETI = 43; // reti has no delay in interrupt enable
		const static uint32_t INT_GET = 44;
		const static uint32_t HALT_CHK = 45; // when in halt mode, actually check I Flag here
		const static uint32_t IRQ_CLEAR = 46;
		const static uint32_t COND_CHECK = 47;
		const static uint32_t HALT_FUNC = 48;

		// test conditions
		const static uint32_t ALWAYS_T = 0;
		const static uint32_t ALWAYS_F = 1;
		const static uint32_t FLAG_Z = 2;
		const static uint32_t FLAG_NZ = 3;
		const static uint32_t FLAG_C = 4;
		const static uint32_t FLAG_NC = 5;
		
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

		#pragma endregion

		#pragma region LR35902 functions
		
		inline bool FlagCget() { return (Regs[5] & 0x10) != 0; };
		inline void FlagCset(bool value) { Regs[5] = (uint32_t)((Regs[5] & ~0x10) | (value ? 0x10 : 0x00)); }

		inline bool FlagHget() { return (Regs[5] & 0x20) != 0; };
		inline void FlagHset(bool value) { Regs[5] = (uint32_t)((Regs[5] & ~0x20) | (value ? 0x20 : 0x00)); }

		inline bool FlagNget() { return (Regs[5] & 0x40) != 0; };
		inline void FlagNset(bool value) { Regs[5] = (uint32_t)((Regs[5] & ~0x40) | (value ? 0x40 : 0x00)); }

		inline bool FlagZget() { return (Regs[5] & 0x80) != 0; };
		inline void FlagZset(bool value) { Regs[5] = (uint32_t)((Regs[5] & ~0x80) | (value ? 0x80 : 0x00)); }

		inline uint32_t RegPCget() { return (uint32_t)(Regs[0] | (Regs[1] << 8)); }
		inline void RegPCset(uint32_t value) { Regs[0] = (uint32_t)(value & 0xFF); Regs[1] = (uint32_t)((value >> 8) & 0xFF); }

		LR35902()
		{
			ResetRegisters();
			ResetInterrupts();
			BuildInstructionTable();
			TotalExecutedCycles = 8;
			stop_check = false;
			instr_pntr = 256 * 60 * 2; // point to reset
			stopped = jammed = halted = FlagI = false;
			EI_pending = 0;
			CB_prefix = false;
		}

		void Reset()
		{
			ResetRegisters();
			ResetInterrupts();
			BuildInstructionTable();
			TotalExecutedCycles = 8;
			stop_check = false;
			instr_pntr = 256 * 60 * 2; // point to reset
			stopped = jammed = halted = FlagI = false;
			EI_pending = 0;
			CB_prefix = false;
		}

		inline void FetchInstruction(uint32_t op) 
		{
			opcode = op;

			instr_pntr = 0;

			if (CB_prefix) { instr_pntr += 256 * 60; }

			instr_pntr += op * 60;

			CB_prefix = false;

			was_FlagI = FlagI;
		}

		// Execute instructions
		void ExecuteOne(uint8_t* interrupt_src, uint8_t interrupt_enable)
		{
			switch (instr_table[instr_pntr++])
			{
			case IDLE:
				// do nothing
				break;
			case OP:
				// Read the opcode of the next instruction
				if (EI_pending > 0 && !CB_prefix)
				{
					EI_pending--;
					if (EI_pending == 0)
					{
						interrupts_enabled = true;
					}
				}

				if (I_use && interrupts_enabled && !CB_prefix && !jammed)
				{
					interrupts_enabled = false;

					if (TraceCallback) { TraceCallback(2); }

					// call interrupt processor 
					// lowest bit set is highest priority
					instr_pntr = 256 * 60 * 2 + 60 * 6; // point to Interrupt
				}
				else
				{
					//OnExecFetch ? .Invoke(RegPC);
					if (TraceCallback) { TraceCallback(0); }
					//CDLCallback ? .Invoke(RegPC, eCDLogMemFlags.FetchFirst);
					FetchInstruction(ReadMemory(RegPCget()));
					RegPCset(RegPCget() + 1);
				}
				I_use = false;
				break;
			case RD:
				Read_Func(instr_table[instr_pntr++], instr_table[instr_pntr++], instr_table[instr_pntr++]);
				break;
			case WR:
				Write_Func(instr_table[instr_pntr++], instr_table[instr_pntr++], instr_table[instr_pntr++]);
				break;
			case TR:
				TR_Func(instr_table[instr_pntr++], instr_table[instr_pntr++]);
				break;
			case ADD16:
				ADD16_Func(instr_table[instr_pntr++], instr_table[instr_pntr++], instr_table[instr_pntr++], instr_table[instr_pntr++]);
				break;
			case ADD8:
				ADD8_Func(instr_table[instr_pntr++], instr_table[instr_pntr++]);
				break;
			case SUB8:
				SUB8_Func(instr_table[instr_pntr++], instr_table[instr_pntr++]);
				break;
			case ADC8:
				ADC8_Func(instr_table[instr_pntr++], instr_table[instr_pntr++]);
				break;
			case SBC8:
				SBC8_Func(instr_table[instr_pntr++], instr_table[instr_pntr++]);
				break;
			case INC16:
				INC16_Func(instr_table[instr_pntr++], instr_table[instr_pntr++]);
				break;
			case INC8:
				INC8_Func(instr_table[instr_pntr++]);
				break;
			case DEC16:
				DEC16_Func(instr_table[instr_pntr++], instr_table[instr_pntr++]);
				break;
			case DEC8:
				DEC8_Func(instr_table[instr_pntr++]);
				break;
			case RLC:
				RLC_Func(instr_table[instr_pntr++]);
				break;
			case RL:
				RL_Func(instr_table[instr_pntr++]);
				break;
			case RRC:
				RRC_Func(instr_table[instr_pntr++]);
				break;
			case RR:
				RR_Func(instr_table[instr_pntr++]);
				break;
			case CPL:
				CPL_Func(instr_table[instr_pntr++]);
				break;
			case DA:
				DA_Func(instr_table[instr_pntr++]);
				break;
			case SCF:
				SCF_Func(instr_table[instr_pntr++]);
				break;
			case CCF:
				CCF_Func(instr_table[instr_pntr++]);
				break;
			case AND8:
				AND8_Func(instr_table[instr_pntr++], instr_table[instr_pntr++]);
				break;
			case XOR8:
				XOR8_Func(instr_table[instr_pntr++], instr_table[instr_pntr++]);
				break;
			case OR8:
				OR8_Func(instr_table[instr_pntr++], instr_table[instr_pntr++]);
				break;
			case CP8:
				CP8_Func(instr_table[instr_pntr++], instr_table[instr_pntr++]);
				break;
			case SLA:
				SLA_Func(instr_table[instr_pntr++]);
				break;
			case SRA:
				SRA_Func(instr_table[instr_pntr++]);
				break;
			case SRL:
				SRL_Func(instr_table[instr_pntr++]);
				break;
			case SWAP:
				SWAP_Func(instr_table[instr_pntr++]);
				break;
			case BIT:
				BIT_Func(instr_table[instr_pntr++], instr_table[instr_pntr++]);
				break;
			case RES:
				RES_Func(instr_table[instr_pntr++], instr_table[instr_pntr++]);
				break;
			case SET:
				SET_Func(instr_table[instr_pntr++], instr_table[instr_pntr++]);
				break;
			case EI:
				if (EI_pending == 0) { EI_pending = 2; }
				break;
			case DI:
				interrupts_enabled = false;
				EI_pending = 0;
				break;
			case HALT:
				halted = true;

				bool temp = false;

				if (instr_table[instr_pntr++] == 1)
				{
					temp = FlagI;
				}
				else
				{
					temp = I_use;
				}

				if (EI_pending > 0 && !CB_prefix)
				{
					EI_pending--;
					if (EI_pending == 0)
					{
						interrupts_enabled = true;
					}
				}

				// if the I flag is asserted at the time of halt, don't halt
				if (temp && interrupts_enabled && !CB_prefix && !jammed)
				{
					interrupts_enabled = false;

					if (TraceCallback) { TraceCallback(2); }

					halted = false;

					if (is_GBC)
					{
						// call the interrupt processor after 4 extra cycles
						if (!Halt_bug_3)
						{
							instr_pntr = 256 * 60 * 2 + 60 * 7; // point to Interrupt for GBC
						}
						else
						{
							instr_pntr = 256 * 60 * 2 + 60 * 6; // point to Interrupt
							Halt_bug_3 = false;
							//Console.WriteLine("Hit INT");
						}
					}
					else
					{
						// call interrupt processor
						instr_pntr = 256 * 60 * 2 + 60 * 6; // point to Interrupt
						Halt_bug_3 = false;
					}
				}
				else if (temp)
				{
					// even if interrupt servicing is disabled, any interrupt flag raised still resumes execution
					if (TraceCallback) { TraceCallback(1); }
					halted = false;

					if (is_GBC)
					{
						// extra 4 cycles for GBC
						if (Halt_bug_3)
						{
							//OnExecFetch ? .Invoke(RegPC);
							if (TraceCallback) { TraceCallback(0); }
							//CDLCallback ? .Invoke(RegPC, eCDLogMemFlags.FetchFirst);

							RegPCset(RegPCget() + 1);
							FetchInstruction(ReadMemory(RegPCget()));
							Halt_bug_3 = false;
							//Console.WriteLine("Hit un");
						}
						else
						{
							instr_pntr = 256 * 60 * 2 + 60; // point to halt loop
						}
					}
					else
					{
						//OnExecFetch ? .Invoke(RegPC);
						if (TraceCallback) { TraceCallback(0); }
						//CDLCallback ? .Invoke(RegPC, eCDLogMemFlags.FetchFirst);

						if (Halt_bug_3)
						{
							//special variant of halt bug where RegPC also isn't incremented post fetch
							RegPCset(RegPCget() + 1);
							FetchInstruction(ReadMemory(RegPCget()));
							Halt_bug_3 = false;
						}
						else
						{
							FetchInstruction(ReadMemory(RegPCget()));
							RegPCset(RegPCget() + 1);
						}
					}
				}
				else
				{
					if (skip_once)
					{
						instr_pntr = 256 * 60 * 2 + 60 * 2; // point to skipped loop
						skip_once = false;
					}
					else
					{
						if (is_GBC)
						{
							instr_pntr = 256 * 60 * 2 + 60 * 3; // point to GBC Halt loop
						}
						else
						{
							instr_pntr = 256 * 60 * 2 + 60 * 4; // point to spec Halt loop
						}
					}
				}
				I_use = false;
				break;
			case STOP:
				stopped = true;
				if (!stop_check)
				{
					stop_time = SpeedFunc(0);
					stop_check = true;
				}

				if (stop_time > 0)
				{
					stop_time--;
					if (stop_time == 0)
					{
						if (TraceCallback) { TraceCallback(3); }

						stopped = false;
						//OnExecFetch ? .Invoke(RegPC);
						if (TraceCallback) { TraceCallback(0); }
						//CDLCallback ? .Invoke(RegPC, eCDLogMemFlags.FetchFirst);
						FetchInstruction(ReadMemory(RegPCget()));
						RegPCset(RegPCget() + 1);
						stop_check = false;
					}
					else
					{
						instr_pntr = 256 * 60 * 2 + 60 * 5; // point to stop loop
					}
				}
				else if ((interrupt_src[0] & 0x10) > 0) // button pressed, not actually an interrupt though
				{
					if (TraceCallback) { TraceCallback(3); }

					stopped = false;
					//OnExecFetch ? .Invoke(RegPC);
					if (TraceCallback) { TraceCallback(0); }
					//CDLCallback ? .Invoke(RegPC, eCDLogMemFlags.FetchFirst);
					FetchInstruction(ReadMemory(RegPCget()));
					RegPCset(RegPCget() + 1);

					stop_check = false;
				}
				else
				{
					instr_pntr = 256 * 60 * 2 + 60 * 5; // point to stop loop
				}
				break;
			case PREFIX:
				CB_prefix = true;
				break;
			case ASGN:
				ASGN_Func(instr_table[instr_pntr++], instr_table[instr_pntr++]);
				break;
			case ADDS:
				ADDS_Func(instr_table[instr_pntr++], instr_table[instr_pntr++], instr_table[instr_pntr++], instr_table[instr_pntr++]);
				break;
			case OP_G:
				//OnExecFetch ? .Invoke(RegPC);
				if (TraceCallback) { TraceCallback(0); }
				//CDLCallback ? .Invoke(RegPC, eCDLogMemFlags.FetchFirst);

				FetchInstruction(ReadMemory(RegPCget())); // note no increment
				break;
			case JAM:
				jammed = true;
				instr_pntr--;
				break;
			case RD_F:
				Read_Func_F(instr_table[instr_pntr++], instr_table[instr_pntr++], instr_table[instr_pntr++]);
				break;
			case EI_RETI:
				EI_pending = 1;
				break;
			case INT_GET:
				// check if any interrupts got cancelled along the way
				// interrupt src = 5 sets the PC to zero as observed
				// also the triggering interrupt seems like it is held low (i.e. cannot trigger I flag) until the interrupt is serviced
				uint32_t bit_check = instr_table[instr_pntr++];
				//Console.WriteLine(interrupt_src + " " + interrupt_enable + " " + TotalExecutedCycles);

				if (((interrupt_src[0] & (1 << bit_check)) > 0) && ((interrupt_enable & (1 << bit_check)) > 0)) { int_src = bit_check; int_clear = (uint8_t)(1 << bit_check); }
				/*
				if (interrupt_src.Bit(0) && interrupt_enable.Bit(0)) { int_src = 0; int_clear = 1; }
				else if (interrupt_src.Bit(1) && interrupt_enable.Bit(1)) { int_src = 1; int_clear = 2; }
				else if (interrupt_src.Bit(2) && interrupt_enable.Bit(2)) { int_src = 2; int_clear = 4; }
				else if (interrupt_src.Bit(3) && interrupt_enable.Bit(3)) { int_src = 3; int_clear = 8; }
				else if (interrupt_src.Bit(4) && interrupt_enable.Bit(4)) { int_src = 4; int_clear = 16; }
				else { int_src = 5; int_clear = 0; }
				*/
				Regs[instr_table[instr_pntr++]] = INT_vectors[int_src];
				break;
			case HALT_CHK:
				I_use = FlagI;
				if (Halt_bug_2 && I_use)
				{
					RegPCset(RegPCget() - 1);
					Halt_bug_3 = true;
					//Console.WriteLine("Halt_bug_3");
					//Console.WriteLine(TotalExecutedCycles);
				}

				Halt_bug_2 = false;
				break;
			case IRQ_CLEAR:
				if ((interrupt_src[0] & (1 << int_src)) > 0) { interrupt_src -= int_clear; }

				if ((interrupt_src[0] & interrupt_enable) == 0) { FlagI = false; }

				// reset back to default state
				int_src = 5;
				int_clear = 0;
				break;
			case COND_CHECK:
				checker = false;
				switch (instr_table[instr_pntr++])
				{
				case ALWAYS_T:
					checker = true;
					break;
				case ALWAYS_F:
					checker = false;
					break;
				case FLAG_Z:
					checker = FlagZget();
					break;
				case FLAG_NZ:
					checker = !FlagZget();
					break;
				case FLAG_C:
					checker = FlagCget();
					break;
				case FLAG_NC:
					checker = !FlagCget();
					break;
				}

				// true condition is what is represented in the instruction vectors	
				// jump ahead if false
				if (checker)
				{
					instr_pntr++;
				}
				else
				{
					// 0 = JR COND, 1 = JP COND, 2 = RET COND, 3 = CALL
					switch (instr_table[instr_pntr++])
					{
					case 0:
						instr_pntr += 10;
						break;
					case 1:
						instr_pntr += 8;
						break;
					case 2:
						instr_pntr += 22;
						break;
					case 3:
						instr_pntr += 26;
						break;
					}
				}
				break;
			case HALT_FUNC:
				if (was_FlagI && (EI_pending == 0) && !interrupts_enabled)
				{
					// in GBC mode, the HALT bug is worked around by simply adding a NOP
					// so it just takes 4 cycles longer to reach the next instruction

					// otherwise, if interrupts are disabled,
					// a glitchy decrement to the program counter happens

					// either way, nothing needs to be done here
				}
				else
				{
					instr_pntr += 3;

					if (!is_GBC) { skip_once = true; }
					// If the interrupt flag is not currently set, but it does get set in the first check
					// then a bug is triggered 
					// With interrupts enabled, this runs the halt command twice 
					// when they are disabled, it reads the next byte twice
					if (!was_FlagI || (was_FlagI && !interrupts_enabled)) { Halt_bug_2 = true; }

				}
				break;
			}
			TotalExecutedCycles++;
		}

		/// <summary>
		/// Optimization method to set cur_instr
		/// </summary>	
		void PopulateCURINSTR(uint32_t d0 = 0, uint32_t d1 = 0, uint32_t d2 = 0, uint32_t d3 = 0, uint32_t d4 = 0, uint32_t d5 = 0, uint32_t d6 = 0, uint32_t d7 = 0, uint32_t d8 = 0,
			uint32_t d9 = 0, uint32_t d10 = 0, uint32_t d11 = 0, uint32_t d12 = 0, uint32_t d13 = 0, uint32_t d14 = 0, uint32_t d15 = 0, uint32_t d16 = 0, uint32_t d17 = 0, uint32_t d18 = 0,
			uint32_t d19 = 0, uint32_t d20 = 0, uint32_t d21 = 0, uint32_t d22 = 0, uint32_t d23 = 0, uint32_t d24 = 0, uint32_t d25 = 0, uint32_t d26 = 0, uint32_t d27 = 0, uint32_t d28 = 0,
			uint32_t d29 = 0, uint32_t d30 = 0, uint32_t d31 = 0, uint32_t d32 = 0, uint32_t d33 = 0, uint32_t d34 = 0, uint32_t d35 = 0, uint32_t d36 = 0, uint32_t d37 = 0, uint32_t d38 = 0,
			uint32_t d39 = 0, uint32_t d40 = 0, uint32_t d41 = 0, uint32_t d42 = 0, uint32_t d43 = 0, uint32_t d44 = 0, uint32_t d45 = 0, uint32_t d46 = 0, uint32_t d47 = 0, uint32_t d48 = 0,
			uint32_t d49 = 0, uint32_t d50 = 0, uint32_t d51 = 0, uint32_t d52 = 0, uint32_t d53 = 0, uint32_t d54 = 0, uint32_t d55 = 0, uint32_t d56 = 0, uint32_t d57 = 0, uint32_t d58 = 0)
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
			cur_instr[36] = d36; cur_instr[37] = d37; cur_instr[38] = d38;
			cur_instr[39] = d39; cur_instr[40] = d40; cur_instr[41] = d41;
			cur_instr[42] = d42; cur_instr[43] = d43; cur_instr[44] = d44;
			cur_instr[45] = d45; cur_instr[46] = d46; cur_instr[47] = d47;
			cur_instr[48] = d48; cur_instr[49] = d49; cur_instr[50] = d50;
			cur_instr[51] = d51; cur_instr[52] = d52; cur_instr[53] = d53;
			cur_instr[54] = d54; cur_instr[55] = d55; cur_instr[56] = d56;
			cur_instr[57] = d57; cur_instr[58] = d58;
		}

		void ResetRegisters()
		{
			for (int i = 0; i < 14; i++)
			{
				Regs[i] = 0;
			}
		}

		#pragma endregion

		#pragma region Interrupts
		void INTERRUPT_()
		{
			PopulateCURINSTR(
				IDLE,
				DEC16, SPl, SPh,
				IDLE,
				WR, SPl, SPh, PCh,
				INT_GET, 4, W,
				DEC16, SPl, SPh,
				INT_GET, 3, W,
				WR, SPl, SPh, PCl,
				INT_GET, 2, W,
				IDLE,
				INT_GET, 1, W,
				IDLE,
				INT_GET, 0, W,
				ASGN, PCh, 0,
				IDLE,
				IDLE,
				TR, PCl, W,
				IRQ_CLEAR,
				IDLE,
				OP );
		}

		void INTERRUPT_GBC_NOP()
		{
			PopulateCURINSTR(
				IDLE,
				DEC16, SPl, SPh,
				IDLE,
				WR, SPl, SPh, PCh,
				IDLE,
				DEC16, SPl, SPh,
				IDLE,
				WR, SPl, SPh, PCl,
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				INT_GET, 4, W,
				INT_GET, 3, W,
				INT_GET, 2, W,
				INT_GET, 1, W,
				INT_GET, 0, W,
				TR, PCl, W,
				IDLE,
				ASGN, PCh, 0,
				IRQ_CLEAR,
				IDLE,
				OP );
		}

		uint8_t INT_vectors[6] = { 0x40, 0x48, 0x50, 0x58, 0x60, 0x00 };

		uint32_t int_src;
		uint8_t int_clear;
		int stop_time;
		bool stop_check;
		bool is_GBC; // GBC automatically adds a NOP to avoid the HALT bug (according to Sinimas)
		bool I_use; // in halt mode, the I flag is checked earlier then when deicision to IRQ is taken
		bool skip_once;
		bool Halt_bug_2;
		bool Halt_bug_3;

		void ResetInterrupts()
		{
			I_use = false;
			skip_once = false;
			Halt_bug_2 = false;
			Halt_bug_3 = false;
			interrupts_enabled = false;

			int_src = 5;
			int_clear = 0;
		}
		#pragma endregion

		#pragma region Indirect Ops

		void INT_OP_IND(uint32_t operation, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				RD, Z, src_l, src_h,
				IDLE,
				operation, Z,
				IDLE,
				WR, src_l, src_h, Z,
				IDLE,
				IDLE,
				HALT_CHK,
				OP );
		}

		void BIT_OP_IND(uint32_t operation, uint32_t bit, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				RD, Z, src_l, src_h,
				IDLE,
				operation, bit, Z,
				IDLE,
				WR, src_l, src_h, Z,
				IDLE,
				IDLE,
				HALT_CHK,
				OP );
		}

		void BIT_TE_IND(uint32_t operation, uint32_t bit, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				RD, Z, src_l, src_h,
				IDLE,
				operation, bit, Z,
				HALT_CHK,
				OP );
		}

		void REG_OP_IND_INC(uint32_t operation, uint32_t dest, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				RD, Z, src_l, src_h,
				operation, dest, Z,
				INC16, src_l, src_h,
				HALT_CHK,
				OP );
		}

		void REG_OP_IND(uint32_t operation, uint32_t dest, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				RD, Z, src_l, src_h,
				IDLE,
				operation, dest, Z,
				HALT_CHK,
				OP );
		}

		void LD_R_IM(uint32_t dest_l, uint32_t dest_h, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				RD, W, src_l, src_h,
				IDLE,
				INC16, src_l, src_h,
				IDLE,
				RD, Z, src_l, src_h,
				IDLE,
				INC16, src_l, src_h,
				IDLE,
				WR, W, Z, dest_l,
				IDLE,
				INC16, W, Z,
				IDLE,
				WR, W, Z, dest_h,
				IDLE,
				IDLE,
				HALT_CHK,
				OP );
		}

		void LD_8_IND_INC(uint32_t dest_l, uint32_t dest_h, uint32_t src)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				WR, dest_l, dest_h, src,
				IDLE,
				INC16, dest_l, dest_h,
				HALT_CHK,
				OP );
		}

		void LD_8_IND_DEC(uint32_t dest_l, uint32_t dest_h, uint32_t src)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				WR, dest_l, dest_h, src,
				IDLE,
				DEC16, dest_l, dest_h,
				HALT_CHK,
				OP );
		}

		void LD_8_IND(uint32_t dest_l, uint32_t dest_h, uint32_t src)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				WR, dest_l, dest_h, src,
				IDLE,
				IDLE,
				HALT_CHK,
				OP );
		}

		void LD_8_IND_IND(uint32_t dest_l, uint32_t dest_h, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				RD, Z, src_l, src_h,
				IDLE,
				INC16, src_l, src_h,
				IDLE,
				WR, dest_l, dest_h, Z,
				IDLE,
				IDLE,
				HALT_CHK,
				OP );
		}

		void LD_IND_8_INC(uint32_t dest, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				RD, dest, src_l, src_h,
				IDLE,
				INC16, src_l, src_h,
				HALT_CHK,
				OP );
		}

		void LD_IND_8_INC_HL(uint32_t dest, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				RD, dest, src_l, src_h,
				IDLE,
				INC16, src_l, src_h,
				HALT_CHK,
				OP );
		}

		void LD_IND_8_DEC_HL(uint32_t dest, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				RD, dest, src_l, src_h,
				IDLE,
				DEC16, src_l, src_h,
				HALT_CHK,
				OP );
		}

		void LD_IND_16(uint32_t dest_l, uint32_t dest_h, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				RD, dest_l, src_l, src_h,
				IDLE,
				INC16, src_l, src_h,
				IDLE,
				RD, dest_h, src_l, src_h,
				IDLE,
				INC16, src_l, src_h,
				HALT_CHK,
				OP );
		}

		void INC_8_IND(uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				RD, Z, src_l, src_h,
				IDLE,
				INC8, Z,
				IDLE,
				WR,  src_l, src_h, Z,
				IDLE,
				IDLE,
				HALT_CHK,
				OP );
		}

		void DEC_8_IND(uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				RD, Z, src_l, src_h,
				IDLE,
				DEC8, Z,
				IDLE,
				WR, src_l, src_h, Z,
				IDLE,
				IDLE,
				HALT_CHK,
				OP );
		}


		void LD_8_IND_FF(uint32_t dest, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				RD, W, src_l, src_h,
				INC16, src_l, src_h,
				IDLE,
				ASGN, Z , 0xFF,
				RD, dest, W, Z,
				IDLE,
				IDLE,
				HALT_CHK,
				OP );
		}

		void LD_FF_IND_8(uint32_t dest_l, uint32_t dest_h, uint32_t src)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				RD, W, dest_l, dest_h,
				INC16, dest_l, dest_h,
				IDLE,
				ASGN, Z , 0xFF,
				WR, W, Z, src,
				IDLE,
				IDLE,
				HALT_CHK,
				OP );
		}

		void LD_8_IND_FFC(uint32_t dest, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				ASGN, Z , 0xFF,
				RD, dest, C, Z,
				IDLE,
				IDLE,
				HALT_CHK,
				OP );
		}

		void LD_FFC_IND_8(uint32_t dest_l, uint32_t dest_h, uint32_t src)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				ASGN, Z , 0xFF,
				WR, C, Z, src,
				IDLE,
				IDLE,
				HALT_CHK,
				OP );
		}

		void LD_16_IND_FF(uint32_t dest, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				RD, W, src_l, src_h,
				IDLE,
				INC16, src_l, src_h,
				IDLE,
				RD, Z, src_l, src_h,
				IDLE,
				INC16, src_l, src_h,
				IDLE,
				RD, dest, W, Z,
				IDLE,
				IDLE,
				HALT_CHK,
				OP );
		}

		void LD_FF_IND_16(uint32_t dest_l, uint32_t dest_h, uint32_t src)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				RD, W, dest_l, dest_h,
				IDLE,
				INC16, dest_l, dest_h,
				IDLE,
				RD, Z, dest_l, dest_h,
				IDLE,
				INC16, dest_l, dest_h,
				IDLE,
				WR, W, Z, src,
				IDLE,
				IDLE,
				HALT_CHK,
				OP );
		}

		#pragma endregion

		#pragma region Direct Ops

		// this contains the vectors of instruction operations
		// NOTE: This list is NOT confirmed accurate for each individual cycle

		void NOP_()
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				HALT_CHK,
				OP );
		}

		void INC_16(uint32_t srcL, uint32_t srcH)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				INC16,
				srcL,
				srcH,
				IDLE,
				IDLE,
				HALT_CHK,
				OP );
		}

		void DEC_16(uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				DEC16,
				src_l,
				src_h,
				IDLE,
				IDLE,
				HALT_CHK,
				OP );
		}

		void ADD_16(uint32_t dest_l, uint32_t dest_h, uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				ADD16, dest_l, dest_h, src_l, src_h,
				IDLE,
				IDLE,
				HALT_CHK,
				OP );
		}

		void REG_OP(uint32_t operation, uint32_t dest, uint32_t src)
		{
			PopulateCURINSTR(
				operation, dest, src,
				IDLE,
				HALT_CHK,
				OP );
		}

		void STOP_()
		{
			PopulateCURINSTR(
				RD, Z, PCl, PCh,
				INC16, PCl, PCh,
				IDLE,
				STOP );
		}

		void HALT_()
		{
			PopulateCURINSTR(
				HALT_FUNC,
				IDLE,
				IDLE,
				OP_G,
				HALT_CHK,
				IDLE,
				HALT, 0 );
		}

		void JR_COND(uint32_t cond)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				RD, W, PCl, PCh,
				INC16, PCl, PCh,
				COND_CHECK, cond, (uint32_t)0,
				IDLE,
				ASGN, Z, 0,
				IDLE,
				ADDS, PCl, PCh, W, Z,
				HALT_CHK,
				OP );
		}

		void JP_COND(uint32_t cond)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				RD, W, PCl, PCh,
				IDLE,
				INC16, PCl, PCh,
				IDLE,
				RD, Z, PCl, PCh,
				INC16, PCl, PCh,
				COND_CHECK, cond, (uint32_t)1,
				IDLE,
				TR, PCl, W,
				IDLE,
				TR, PCh, Z,
				HALT_CHK,
				OP );
		}

		void RET_()
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				RD, PCl, SPl, SPh,
				IDLE,
				INC16, SPl, SPh,
				IDLE,
				RD, PCh, SPl, SPh,
				IDLE,
				INC16, SPl, SPh,
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				HALT_CHK,
				OP );
		}

		void RETI_()
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				RD, PCl, SPl, SPh,
				IDLE,
				INC16, SPl, SPh,
				IDLE,
				RD, PCh, SPl, SPh,
				IDLE,
				INC16, SPl, SPh,
				IDLE,
				EI_RETI,
				IDLE,
				IDLE,
				HALT_CHK,
				OP );
		}


		void RET_COND(uint32_t cond)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				COND_CHECK, cond, (uint32_t)2,
				IDLE,
				RD, PCl, SPl, SPh,
				IDLE,
				INC16, SPl, SPh,
				IDLE,
				RD, PCh, SPl, SPh,
				IDLE,
				INC16, SPl, SPh,
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				HALT_CHK,
				OP );
		}

		void CALL_COND(uint32_t cond)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				RD, W, PCl, PCh,
				INC16, PCl, PCh,
				IDLE,
				IDLE,
				RD, Z, PCl, PCh,
				INC16, PCl, PCh,
				COND_CHECK, cond, (uint32_t)3,
				DEC16, SPl, SPh,
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				WR, SPl, SPh, PCh,
				IDLE,
				IDLE,
				DEC16, SPl, SPh,
				WR, SPl, SPh, PCl,
				TR, PCl, W,
				TR, PCh, Z,
				HALT_CHK,
				OP );
		}

		void INT_OP(uint32_t operation, uint32_t src)
		{
			PopulateCURINSTR(
				operation, src,
				IDLE,
				HALT_CHK,
				OP );
		}

		void BIT_OP(uint32_t operation, uint32_t bit, uint32_t src)
		{
			PopulateCURINSTR(
				operation, bit, src,
				IDLE,
				HALT_CHK,
				OP );
		}

		void PUSH_(uint32_t src_l, uint32_t src_h)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				DEC16, SPl, SPh,
				IDLE,
				WR, SPl, SPh, src_h,
				IDLE,
				DEC16, SPl, SPh,
				IDLE,
				WR, SPl, SPh, src_l,
				IDLE,
				IDLE,
				HALT_CHK,
				OP );
		}

		// NOTE: this is the only instruction that can write to F
		// but the bottom 4 bits of F are always 0, so instead of putting a special check for every read op
		// let's just put a special operation here specifically for F
		void POP_(uint32_t src_l, uint32_t src_h)
		{
			if (src_l != F)
			{
				PopulateCURINSTR(
					IDLE,
					IDLE,
					IDLE,
					RD, src_l, SPl, SPh,
					IDLE,
					INC16, SPl, SPh,
					IDLE,
					RD, src_h, SPl, SPh,
					IDLE,
					INC16, SPl, SPh,
					HALT_CHK,
					OP );
			}
			else
			{
				PopulateCURINSTR(
					IDLE,
					IDLE,
					IDLE,
					RD_F, src_l, SPl, SPh,
					IDLE,
					INC16, SPl, SPh,
					IDLE,
					RD, src_h, SPl, SPh,
					IDLE,
					INC16, SPl, SPh,
					HALT_CHK,
					OP );
			}
		}

		void RST_(uint32_t n)
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				DEC16, SPl, SPh,
				IDLE,
				IDLE,
				IDLE,
				WR, SPl, SPh, PCh,
				DEC16, SPl, SPh,
				IDLE,
				IDLE,
				WR, SPl, SPh, PCl,
				ASGN, PCh, 0,
				ASGN, PCl, n,
				HALT_CHK,
				OP );
		}

		void PREFIX_()
		{
			PopulateCURINSTR(
				PREFIX,
				IDLE,
				IDLE,
				OP );
		}

		void DI_()
		{
			PopulateCURINSTR(
				DI,
				IDLE,
				HALT_CHK,
				OP );
		}

		void EI_()
		{
			PopulateCURINSTR(
				EI,
				IDLE,
				HALT_CHK,
				OP );
		}

		void JP_HL()
		{
			PopulateCURINSTR(
				TR, PCl, L,
				TR, PCh, H,
				HALT_CHK,
				OP );
		}

		void ADD_SP()
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				RD, W, PCl, PCh,
				IDLE,
				INC16, PCl, PCh,
				IDLE,
				ASGN, Z, 0,
				IDLE,
				ADDS, SPl, SPh, W, Z,
				IDLE,
				IDLE,
				HALT_CHK,
				OP );
		}

		void LD_SP_HL()
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				TR, SPl, L,
				IDLE,
				TR, SPh, H,
				HALT_CHK,
				OP );
		}

		void LD_HL_SPn()
		{
			PopulateCURINSTR(
				IDLE,
				IDLE,
				IDLE,
				RD, W, PCl, PCh,
				IDLE,
				INC16, PCl, PCh,
				TR, H, SPh,
				TR, L, SPl,
				ASGN, Z, 0,
				ADDS, L, H, W, Z,
				HALT_CHK,
				OP );
		}

		void JAM_()
		{
			PopulateCURINSTR(
				JAM,
				IDLE,
				IDLE,
				IDLE );
		}

		#pragma endregion

		#pragma region Decode

		void BuildInstructionTable()
		{
			for (int i = 0; i < 256; i++)
			{
				switch (i)
				{
				case 0x00: NOP_();									break; // NOP
				case 0x01: LD_IND_16(C, B, PCl, PCh);				break; // LD BC, nn
				case 0x02: LD_8_IND(C, B, A);						break; // LD (BC), A
				case 0x03: INC_16(C, B);							break; // INC BC
				case 0x04: INT_OP(INC8, B);							break; // INC B
				case 0x05: INT_OP(DEC8, B);							break; // DEC B
				case 0x06: LD_IND_8_INC(B, PCl, PCh);				break; // LD B, n
				case 0x07: INT_OP(RLC, Aim);						break; // RLCA
				case 0x08: LD_R_IM(SPl, SPh, PCl, PCh);				break; // LD (imm), SP
				case 0x09: ADD_16(L, H, C, B);						break; // ADD HL, BC
				case 0x0A: REG_OP_IND(TR, A, C, B);					break; // LD A, (BC)
				case 0x0B: DEC_16(C, B);							break; // DEC BC
				case 0x0C: INT_OP(INC8, C);							break; // INC C
				case 0x0D: INT_OP(DEC8, C);							break; // DEC C
				case 0x0E: LD_IND_8_INC(C, PCl, PCh);				break; // LD C, n
				case 0x0F: INT_OP(RRC, Aim);						break; // RRCA
				case 0x10: STOP_();									break; // STOP
				case 0x11: LD_IND_16(E, D, PCl, PCh);				break; // LD DE, nn
				case 0x12: LD_8_IND(E, D, A);						break; // LD (DE), A
				case 0x13: INC_16(E, D);							break; // INC DE
				case 0x14: INT_OP(INC8, D);							break; // INC D
				case 0x15: INT_OP(DEC8, D);							break; // DEC D
				case 0x16: LD_IND_8_INC(D, PCl, PCh);				break; // LD D, n
				case 0x17: INT_OP(RL, Aim);							break; // RLA
				case 0x18: JR_COND(ALWAYS_T);						break; // JR, r8
				case 0x19: ADD_16(L, H, E, D);						break; // ADD HL, DE
				case 0x1A: REG_OP_IND(TR, A, E, D);					break; // LD A, (DE)
				case 0x1B: DEC_16(E, D);							break; // DEC DE
				case 0x1C: INT_OP(INC8, E);							break; // INC E
				case 0x1D: INT_OP(DEC8, E);							break; // DEC E
				case 0x1E: LD_IND_8_INC(E, PCl, PCh);				break; // LD E, n
				case 0x1F: INT_OP(RR, Aim);							break; // RRA
				case 0x20: JR_COND(FLAG_NZ);						break; // JR NZ, r8
				case 0x21: LD_IND_16(L, H, PCl, PCh);				break; // LD HL, nn
				case 0x22: LD_8_IND_INC(L, H, A);					break; // LD (HL+), A
				case 0x23: INC_16(L, H);							break; // INC HL
				case 0x24: INT_OP(INC8, H);							break; // INC H
				case 0x25: INT_OP(DEC8, H);							break; // DEC H
				case 0x26: LD_IND_8_INC(H, PCl, PCh);				break; // LD H, n
				case 0x27: INT_OP(DA, A);							break; // DAA
				case 0x28: JR_COND(FLAG_Z);							break; // JR Z, r8
				case 0x29: ADD_16(L, H, L, H);						break; // ADD HL, HL
				case 0x2A: LD_IND_8_INC_HL(A, L, H);				break; // LD A, (HL+)
				case 0x2B: DEC_16(L, H);							break; // DEC HL
				case 0x2C: INT_OP(INC8, L);							break; // INC L
				case 0x2D: INT_OP(DEC8, L);							break; // DEC L
				case 0x2E: LD_IND_8_INC(L, PCl, PCh);				break; // LD L, n
				case 0x2F: INT_OP(CPL, A);							break; // CPL
				case 0x30: JR_COND(FLAG_NC);						break; // JR NC, r8
				case 0x31: LD_IND_16(SPl, SPh, PCl, PCh);			break; // LD SP, nn
				case 0x32: LD_8_IND_DEC(L, H, A);					break; // LD (HL-), A
				case 0x33: INC_16(SPl, SPh);						break; // INC SP
				case 0x34: INC_8_IND(L, H);							break; // INC (HL)
				case 0x35: DEC_8_IND(L, H);							break; // DEC (HL)
				case 0x36: LD_8_IND_IND(L, H, PCl, PCh);			break; // LD (HL), n
				case 0x37: INT_OP(SCF, A);							break; // SCF
				case 0x38: JR_COND(FLAG_C);							break; // JR C, r8
				case 0x39: ADD_16(L, H, SPl, SPh);					break; // ADD HL, SP
				case 0x3A: LD_IND_8_DEC_HL(A, L, H);				break; // LD A, (HL-)
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
				case 0x46: REG_OP_IND(TR, B, L, H);					break; // LD B, (HL)
				case 0x47: REG_OP(TR, B, A);						break; // LD B, A
				case 0x48: REG_OP(TR, C, B);						break; // LD C, B
				case 0x49: REG_OP(TR, C, C);						break; // LD C, C
				case 0x4A: REG_OP(TR, C, D);						break; // LD C, D
				case 0x4B: REG_OP(TR, C, E);						break; // LD C, E
				case 0x4C: REG_OP(TR, C, H);						break; // LD C, H
				case 0x4D: REG_OP(TR, C, L);						break; // LD C, L
				case 0x4E: REG_OP_IND(TR, C, L, H);					break; // LD C, (HL)
				case 0x4F: REG_OP(TR, C, A);						break; // LD C, A
				case 0x50: REG_OP(TR, D, B);						break; // LD D, B
				case 0x51: REG_OP(TR, D, C);						break; // LD D, C
				case 0x52: REG_OP(TR, D, D);						break; // LD D, D
				case 0x53: REG_OP(TR, D, E);						break; // LD D, E
				case 0x54: REG_OP(TR, D, H);						break; // LD D, H
				case 0x55: REG_OP(TR, D, L);						break; // LD D, L
				case 0x56: REG_OP_IND(TR, D, L, H);					break; // LD D, (HL)
				case 0x57: REG_OP(TR, D, A);						break; // LD D, A
				case 0x58: REG_OP(TR, E, B);						break; // LD E, B
				case 0x59: REG_OP(TR, E, C);						break; // LD E, C
				case 0x5A: REG_OP(TR, E, D);						break; // LD E, D
				case 0x5B: REG_OP(TR, E, E);						break; // LD E, E
				case 0x5C: REG_OP(TR, E, H);						break; // LD E, H
				case 0x5D: REG_OP(TR, E, L);						break; // LD E, L
				case 0x5E: REG_OP_IND(TR, E, L, H);					break; // LD E, (HL)
				case 0x5F: REG_OP(TR, E, A);						break; // LD E, A
				case 0x60: REG_OP(TR, H, B);						break; // LD H, B
				case 0x61: REG_OP(TR, H, C);						break; // LD H, C
				case 0x62: REG_OP(TR, H, D);						break; // LD H, D
				case 0x63: REG_OP(TR, H, E);						break; // LD H, E
				case 0x64: REG_OP(TR, H, H);						break; // LD H, H
				case 0x65: REG_OP(TR, H, L);						break; // LD H, L
				case 0x66: REG_OP_IND(TR, H, L, H);					break; // LD H, (HL)
				case 0x67: REG_OP(TR, H, A);						break; // LD H, A
				case 0x68: REG_OP(TR, L, B);						break; // LD L, B
				case 0x69: REG_OP(TR, L, C);						break; // LD L, C
				case 0x6A: REG_OP(TR, L, D);						break; // LD L, D
				case 0x6B: REG_OP(TR, L, E);						break; // LD L, E
				case 0x6C: REG_OP(TR, L, H);						break; // LD L, H
				case 0x6D: REG_OP(TR, L, L);						break; // LD L, L
				case 0x6E: REG_OP_IND(TR, L, L, H);					break; // LD L, (HL)
				case 0x6F: REG_OP(TR, L, A);						break; // LD L, A
				case 0x70: LD_8_IND(L, H, B);						break; // LD (HL), B
				case 0x71: LD_8_IND(L, H, C);						break; // LD (HL), C
				case 0x72: LD_8_IND(L, H, D);						break; // LD (HL), D
				case 0x73: LD_8_IND(L, H, E);						break; // LD (HL), E
				case 0x74: LD_8_IND(L, H, H);						break; // LD (HL), H
				case 0x75: LD_8_IND(L, H, L);						break; // LD (HL), L
				case 0x76: HALT_();									break; // HALT
				case 0x77: LD_8_IND(L, H, A);						break; // LD (HL), A
				case 0x78: REG_OP(TR, A, B);						break; // LD A, B
				case 0x79: REG_OP(TR, A, C);						break; // LD A, C
				case 0x7A: REG_OP(TR, A, D);						break; // LD A, D
				case 0x7B: REG_OP(TR, A, E);						break; // LD A, E
				case 0x7C: REG_OP(TR, A, H);						break; // LD A, H
				case 0x7D: REG_OP(TR, A, L);						break; // LD A, L
				case 0x7E: REG_OP_IND(TR, A, L, H);					break; // LD A, (HL)
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
				case 0xC0: RET_COND(FLAG_NZ);						break; // Ret NZ
				case 0xC1: POP_(C, B);								break; // POP BC
				case 0xC2: JP_COND(FLAG_NZ);						break; // JP NZ
				case 0xC3: JP_COND(ALWAYS_T);						break; // JP
				case 0xC4: CALL_COND(FLAG_NZ);						break; // CALL NZ
				case 0xC5: PUSH_(C, B);								break; // PUSH BC
				case 0xC6: REG_OP_IND_INC(ADD8, A, PCl, PCh);		break; // ADD A, n
				case 0xC7: RST_(0);									break; // RST 0
				case 0xC8: RET_COND(FLAG_Z);						break; // RET Z
				case 0xC9: RET_();									break; // RET
				case 0xCA: JP_COND(FLAG_Z);							break; // JP Z
				case 0xCB: PREFIX_();								break; // PREFIX
				case 0xCC: CALL_COND(FLAG_Z);						break; // CALL Z
				case 0xCD: CALL_COND(ALWAYS_T);						break; // CALL
				case 0xCE: REG_OP_IND_INC(ADC8, A, PCl, PCh);		break; // ADC A, n
				case 0xCF: RST_(0x08);								break; // RST 0x08
				case 0xD0: RET_COND(FLAG_NC);						break; // Ret NC
				case 0xD1: POP_(E, D);								break; // POP DE
				case 0xD2: JP_COND(FLAG_NC);						break; // JP NC
				case 0xD3: JAM_();									break; // JAM
				case 0xD4: CALL_COND(FLAG_NC);						break; // CALL NC
				case 0xD5: PUSH_(E, D);								break; // PUSH DE
				case 0xD6: REG_OP_IND_INC(SUB8, A, PCl, PCh);		break; // SUB A, n
				case 0xD7: RST_(0x10);								break; // RST 0x10
				case 0xD8: RET_COND(FLAG_C);						break; // RET C
				case 0xD9: RETI_();									break; // RETI
				case 0xDA: JP_COND(FLAG_C);							break; // JP C
				case 0xDB: JAM_();									break; // JAM
				case 0xDC: CALL_COND(FLAG_C);						break; // CALL C
				case 0xDD: JAM_();									break; // JAM
				case 0xDE: REG_OP_IND_INC(SBC8, A, PCl, PCh);		break; // SBC A, n
				case 0xDF: RST_(0x18);								break; // RST 0x18
				case 0xE0: LD_FF_IND_8(PCl, PCh, A);				break; // LD(n), A
				case 0xE1: POP_(L, H);								break; // POP HL
				case 0xE2: LD_FFC_IND_8(PCl, PCh, A);				break; // LD(C), A
				case 0xE3: JAM_();									break; // JAM
				case 0xE4: JAM_();                                  break; // JAM
				case 0xE5: PUSH_(L, H);								break; // PUSH HL
				case 0xE6: REG_OP_IND_INC(AND8, A, PCl, PCh);		break; // AND A, n
				case 0xE7: RST_(0x20);								break; // RST 0x20
				case 0xE8: ADD_SP();								break; // ADD SP,n
				case 0xE9: JP_HL();									break; // JP (HL)
				case 0xEA: LD_FF_IND_16(PCl, PCh, A);				break; // LD(nn), A
				case 0xEB: JAM_();									break; // JAM
				case 0xEC: JAM_();									break; // JAM
				case 0xED: JAM_();									break; // JAM
				case 0xEE: REG_OP_IND_INC(XOR8, A, PCl, PCh);		break; // XOR A, n
				case 0xEF: RST_(0x28);								break; // RST 0x28
				case 0xF0: LD_8_IND_FF(A, PCl, PCh);				break; // A, LD(n)
				case 0xF1: POP_(F, A);								break; // POP AF
				case 0xF2: LD_8_IND_FFC(A, PCl, PCh);				break; // A, LD(C)
				case 0xF3: DI_();									break; // DI
				case 0xF4: JAM_();									break; // JAM
				case 0xF5: PUSH_(F, A);								break; // PUSH AF
				case 0xF6: REG_OP_IND_INC(OR8, A, PCl, PCh);		break; // OR A, n
				case 0xF7: RST_(0x30);								break; // RST 0x30
				case 0xF8: LD_HL_SPn();								break; // LD HL, SP+n
				case 0xF9: LD_SP_HL();								break; // LD, SP, HL
				case 0xFA: LD_16_IND_FF(A, PCl, PCh);				break; // A, LD(nn)
				case 0xFB: EI_();									break; // EI
				case 0xFC: JAM_();									break; // JAM
				case 0xFD: JAM_();									break; // JAM
				case 0xFE: REG_OP_IND_INC(CP8, A, PCl, PCh);		break; // XOR A, n
				case 0xFF: RST_(0x38);								break; // RST 0x38
				}

				for (int j = 0; j < 60; j++)
				{
					instr_table[i * 60 + j] = cur_instr[j];
				}

				switch (i)
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
				case 0x30: INT_OP(SWAP, B);							break; // SWAP B
				case 0x31: INT_OP(SWAP, C);							break; // SWAP C
				case 0x32: INT_OP(SWAP, D);							break; // SWAP D
				case 0x33: INT_OP(SWAP, E);							break; // SWAP E
				case 0x34: INT_OP(SWAP, H);							break; // SWAP H
				case 0x35: INT_OP(SWAP, L);							break; // SWAP L
				case 0x36: INT_OP_IND(SWAP, L, H);					break; // SWAP (HL)
				case 0x37: INT_OP(SWAP, A);							break; // SWAP A
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

				for (int j = 0; j < 60; j++)
				{
					instr_table[256 * 60 + i * 60 + j] = cur_instr[j];
				}
			}

			// other miscellaneous vectors

			// reset
			instr_table[256 * 60 * 2] = IDLE;
			instr_table[256 * 60 * 2 + 1] = IDLE;
			instr_table[256 * 60 * 2 + 2] = HALT_CHK;
			instr_table[256 * 60 * 2 + 3] = OP;

			// halt loop
			instr_table[256 * 60 * 2 + 60] = IDLE;
			instr_table[256 * 60 * 2 + 60 + 1] = IDLE;
			instr_table[256 * 60 * 2 + 60 + 2] = IDLE;
			instr_table[256 * 60 * 2 + 60 + 3] = OP;

			// skipped loop
			instr_table[256 * 60 * 2 + 60 * 2] = IDLE;
			instr_table[256 * 60 * 2 + 60 * 2 + 1] = IDLE;
			instr_table[256 * 60 * 2 + 60 * 2 + 2] = IDLE;
			instr_table[256 * 60 * 2 + 60 * 2 + 3] = HALT;
			instr_table[256 * 60 * 2 + 60 * 2 + 4] = (uint32_t)0;

			// GBC Halt loop
			instr_table[256 * 60 * 2 + 60 * 3] = IDLE;
			instr_table[256 * 60 * 2 + 60 * 3 + 1] = IDLE;
			instr_table[256 * 60 * 2 + 60 * 3 + 2] = HALT_CHK;
			instr_table[256 * 60 * 2 + 60 * 3 + 3] = HALT;
			instr_table[256 * 60 * 2 + 60 * 3 + 4] = (uint32_t)0;

			// spec Halt loop
			instr_table[256 * 60 * 2 + 60 * 4] = HALT_CHK;
			instr_table[256 * 60 * 2 + 60 * 4 + 1] = IDLE;
			instr_table[256 * 60 * 2 + 60 * 4 + 2] = IDLE;
			instr_table[256 * 60 * 2 + 60 * 4 + 3] = HALT;
			instr_table[256 * 60 * 2 + 60 * 4 + 4] = (uint32_t)0;

			// Stop loop
			instr_table[256 * 60 * 2 + 60 * 5] = IDLE;
			instr_table[256 * 60 * 2 + 60 * 5 + 1] = IDLE;
			instr_table[256 * 60 * 2 + 60 * 5 + 2] = IDLE;
			instr_table[256 * 60 * 2 + 60 * 5 + 3] = STOP;

			// interrupt vectors
			INTERRUPT_();

			for (int i = 0; i < 60; i++)
			{
				instr_table[256 * 60 * 2 + 60 * 6 + i] = cur_instr[i];
			}

			INTERRUPT_GBC_NOP();

			for (int i = 0; i < 60; i++)
			{
				instr_table[256 * 60 * 2 + 60 * 7 + i] = cur_instr[i];
			}

		}

		#pragma endregion

		#pragma region Operations

		// local variables for operations, not stated
		uint32_t Reg16_d, Reg16_s, c;
		uint32_t ans, ans_l, ans_h, temp;
		uint8_t a_d;
		bool imm;


		void Read_Func(uint32_t dest, uint32_t src_l, uint32_t src_h)
		{
			uint32_t addr = (uint32_t)(Regs[src_l] | (Regs[src_h]) << 8);
			//if (CDLCallback != null)
			//{
				//if (src_l == PCl) CDLCallback(addr, eCDLogMemFlags.FetchOperand);
				//else CDLCallback(addr, eCDLogMemFlags.Data);
			//}
			Regs[dest] = ReadMemory(addr);
		}

		// special read for POP AF that always clears the lower 4 bits of F 
		void Read_Func_F(uint32_t dest, uint32_t src_l, uint32_t src_h)
		{
			Regs[dest] = (uint32_t)(ReadMemory((uint32_t)(Regs[src_l] | (Regs[src_h]) << 8)) & 0xF0);
		}

		void Write_Func(uint32_t dest_l, uint32_t dest_h, uint32_t src)
		{
			uint32_t addr = (uint32_t)(Regs[dest_l] | (Regs[dest_h]) << 8);
			//CDLCallback ? .Invoke(addr, eCDLogMemFlags.Write | eCDLogMemFlags.Data);
			WriteMemory(addr, (uint8_t)Regs[src]);
		}

		void TR_Func(uint32_t dest, uint32_t src)
		{
			Regs[dest] = Regs[src];
		}

		void ADD16_Func(uint32_t dest_l, uint32_t dest_h, uint32_t src_l, uint32_t src_h)
		{
			Reg16_d = Regs[dest_l] | (Regs[dest_h] << 8);
			Reg16_s = Regs[src_l] | (Regs[src_h] << 8);

			Reg16_d += Reg16_s;

			FlagCset((Reg16_d & 0x10000) > 0);

			ans_l = (uint32_t)(Reg16_d & 0xFF);
			ans_h = (uint32_t)((Reg16_d & 0xFF00) >> 8);

			// redo for half carry flag
			Reg16_d = Regs[dest_l] | ((Regs[dest_h] & 0x0F) << 8);
			Reg16_s = Regs[src_l] | ((Regs[src_h] & 0x0F) << 8);

			Reg16_d += Reg16_s;

			FlagHset((Reg16_d & 0x1000) > 0);
			FlagNset(false);

			Regs[dest_l] = ans_l;
			Regs[dest_h] = ans_h;
		}

		void ADD8_Func(uint32_t dest, uint32_t src)
		{
			Reg16_d = Regs[dest];
			Reg16_d += Regs[src];

			FlagCset((Reg16_d & 0x100) > 0);
			FlagZset((Reg16_d & 0xFF) == 0);

			ans = (uint32_t)(Reg16_d & 0xFF);

			// redo for half carry flag
			Reg16_d = Regs[dest] & 0xF;
			Reg16_d += (Regs[src] & 0xF);

			FlagHset((Reg16_d & 0x10) > 0);

			FlagNset(false);

			Regs[dest] = ans;
		}

		void SUB8_Func(uint32_t dest, uint32_t src)
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

			Regs[dest] = ans;
		}

		void BIT_Func(uint32_t bit, uint32_t src)
		{
			FlagZset(!Regs[src].Bit(bit));
			FlagHset(true);
			FlagNset(false);
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

		void SWAP_Func(uint32_t src)
		{
			temp = (uint32_t)((Regs[src] << 4) & 0xF0);
			Regs[src] = (uint32_t)(temp | (Regs[src] >> 4));

			FlagZset(Regs[src] == 0);
			FlagHset(false);
			FlagNset(false);
			FlagCset(false);
		}

		void SLA_Func(uint32_t src)
		{
			FlagCset(Regs[src].Bit(7));

			Regs[src] = (uint32_t)((Regs[src] << 1) & 0xFF);

			FlagZset(Regs[src] == 0);
			FlagHset(false);
			FlagNset(false);
		}

		void SRA_Func(uint32_t src)
		{
			FlagCset(Regs[src].Bit(0));

			temp = (uint32_t)(Regs[src] & 0x80); // MSB doesn't change in this operation

			Regs[src] = (uint32_t)((Regs[src] >> 1) | temp);

			FlagZset(Regs[src] == 0);
			FlagHset(false);
			FlagNset(false);
		}

		void SRL_Func(uint32_t src)
		{
			FlagCset(Regs[src].Bit(0));

			Regs[src] = (uint32_t)(Regs[src] >> 1);

			FlagZset(Regs[src] == 0);
			FlagHset(false);
			FlagNset(false);
		}

		void CPL_Func(uint32_t src)
		{
			Regs[src] = (uint32_t)((~Regs[src]) & 0xFF);

			FlagHset(true);
			FlagNset(true);
		}

		void CCF_Func(uint32_t src)
		{
			FlagCset(!FlagCget());
			FlagHset(false);
			FlagNset(false);
		}

		void SCF_Func(uint32_t src)
		{
			FlagCset(true);
			FlagHset(false);
			FlagNset(false);
		}

		void AND8_Func(uint32_t dest, uint32_t src)
		{
			Regs[dest] = (uint32_t)(Regs[dest] & Regs[src]);

			FlagZset(Regs[dest] == 0);
			FlagCset(false);
			FlagHset(true);
			FlagNset(false);
		}

		void OR8_Func(uint32_t dest, uint32_t src)
		{
			Regs[dest] = (uint32_t)(Regs[dest] | Regs[src]);

			FlagZset(Regs[dest] == 0);
			FlagCset(false);
			FlagHset(false);
			FlagNset(false);
		}

		void XOR8_Func(uint32_t dest, uint32_t src)
		{
			Regs[dest] = (uint32_t)(Regs[dest] ^ Regs[src]);

			FlagZset(Regs[dest] == 0);
			FlagCset(false);
			FlagHset(false);
			FlagNset(false);
		}

		void CP8_Func(uint32_t dest, uint32_t src)
		{
			Reg16_d = Regs[dest];
			Reg16_d -= Regs[src];

			FlagCset((Reg16_d & 0x100) > 0);
			FlagZset((Reg16_d & 0xFF) == 0);

			// redo for half carry flag
			Reg16_d = Regs[dest] & 0xF;
			Reg16_d -= (Regs[src] & 0xF);

			FlagHset((Reg16_d & 0x10) > 0);

			FlagNset(true);
		}

		void RRC_Func(uint32_t src)
		{
			imm = src == Aim;
			if (imm) { src = A; }

			FlagCset(Regs[src].Bit(0));

			Regs[src] = (uint32_t)((FlagCget() ? 0x80 : 0) | (Regs[src] >> 1));

			FlagZset(imm ? false : (Regs[src] == 0));
			FlagHset(false);
			FlagNset(false);
		}

		void RR_Func(uint32_t src)
		{
			imm = src == Aim;
			if (imm) { src = A; }

			c = FlagCget() ? 0x80 : 0;

			FlagCset(Regs[src].Bit(0));

			Regs[src] = (uint32_t)(c | (Regs[src] >> 1));

			FlagZset(imm ? false : (Regs[src] == 0));
			FlagHset(false);
			FlagNset(false);
		}

		void RLC_Func(uint32_t src)
		{
			imm = src == Aim;
			if (imm) { src = A; }

			c = Regs[src].Bit(7) ? 1 : 0;
			FlagCset(Regs[src].Bit(7));

			Regs[src] = (uint32_t)(((Regs[src] << 1) & 0xFF) | c);

			FlagZset(imm ? false : (Regs[src] == 0));
			FlagHset(false);
			FlagNset(false);
		}

		void RL_Func(uint32_t src)
		{
			imm = src == Aim;
			if (imm) { src = A; }

			c = FlagCget() ? 1 : 0;
			FlagCset(Regs[src].Bit(7));

			Regs[src] = (uint32_t)(((Regs[src] << 1) & 0xFF) | c);

			FlagZset(imm ? false : (Regs[src] == 0));
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

			Regs[src] = ans;
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

			Regs[src] = ans;
		}

		void INC16_Func(uint32_t src_l, uint32_t src_h)
		{
			Reg16_d = Regs[src_l] | (Regs[src_h] << 8);

			Reg16_d += 1;

			Regs[src_l] = (uint32_t)(Reg16_d & 0xFF);
			Regs[src_h] = (uint32_t)((Reg16_d & 0xFF00) >> 8);
		}

		void DEC16_Func(uint32_t src_l, uint32_t src_h)
		{
			Reg16_d = Regs[src_l] | (Regs[src_h] << 8);

			Reg16_d -= 1;

			Regs[src_l] = (uint32_t)(Reg16_d & 0xFF);
			Regs[src_h] = (uint32_t)((Reg16_d & 0xFF00) >> 8);
		}

		void ADC8_Func(uint32_t dest, uint32_t src)
		{
			Reg16_d = Regs[dest];
			c = FlagCget() ? 1 : 0;

			Reg16_d += (Regs[src] + c);

			FlagCset((Reg16_d & 0x100) > 0);
			FlagZset((Reg16_d & 0xFF) == 0);

			ans = (uint32_t)(Reg16_d & 0xFF);

			// redo for half carry flag
			Reg16_d = Regs[dest] & 0xF;
			Reg16_d += ((Regs[src] & 0xF) + c);

			FlagHset((Reg16_d & 0x10) > 0);
			FlagNset(false);

			Regs[dest] = ans;
		}

		void SBC8_Func(uint32_t dest, uint32_t src)
		{
			Reg16_d = Regs[dest];
			c = FlagCget() ? 1 : 0;

			Reg16_d -= (Regs[src] + c);

			FlagCset((Reg16_d & 0x100) > 0);
			FlagZset((Reg16_d & 0xFF) == 0);

			ans = (uint32_t)(Reg16_d & 0xFF);

			// redo for half carry flag
			Reg16_d = Regs[dest] & 0xF;
			Reg16_d -= ((Regs[src] & 0xF) + c);

			FlagHset((Reg16_d & 0x10) > 0);
			FlagNset(true);

			Regs[dest] = ans;
		}

		// DA code courtesy of AWJ: http://forums.nesdev.com/viewtopic.php?f=20&t=15944
		void DA_Func(uint32_t src)
		{
			a_d = (uint8_t)Regs[src];

			if (!FlagNget())
			{  // after an addition, adjust if (half-)carry occurred or if result is out of bounds
				if (FlagCget() || a_d > 0x99) { a_d += 0x60; FlagCset(true); }
				if (FlagHget() || (a_d & 0x0f) > 0x09) { a_d += 0x6; }
			}
			else
			{  // after a subtraction, only adjust if (half-)carry occurred
				if (FlagCget()) { a_d -= 0x60; }
				if (FlagHget()) { a_d -= 0x6; }
			}

			a_d &= 0xFF;

			Regs[src] = a_d;

			FlagZset(a_d == 0);
			FlagHset(false);
		}

		// used for signed operations
		void ADDS_Func(uint32_t dest_l, uint32_t dest_h, uint32_t src_l, uint32_t src_h)
		{
			Reg16_d = Regs[dest_l];
			Reg16_s = Regs[src_l];

			Reg16_d += Reg16_s;

			temp = 0;

			// since this is signed addition, calculate the high byte carry appropriately
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

			ans_l = (uint32_t)(Reg16_d & 0xFF);

			// JR operations do not effect flags
			if (dest_l != PCl)
			{
				FlagCset((Reg16_d & 0x100) > 0);

				// redo for half carry flag
				Reg16_d = Regs[dest_l] & 0xF;
				Reg16_d += Regs[src_l] & 0xF;

				FlagHset((Reg16_d & 0x10) > 0);
				FlagNset(false);
				FlagZset(false);
			}

			Regs[dest_l] = ans_l;
			Regs[dest_h] += temp;
			Regs[dest_h] &= 0xFF;

		}

		#pragma endregion

		#pragma region Disassemble

		// disassemblies will also return strings of the same length
		const char* TraceHeader = "Z80A: PC, machine code, mnemonic, operands, registers (AF, BC, DE, HL, IX, IY, SP, Cy), flags (CNP3H5ZS)";
		const char* Un_halt_event = "                 ==Un-halted==                 ";
		const char* IRQ_event     = "                  ====IRQ====                  ";
		const char* Un_halt_event = "                ==Un-stopped==                 ";
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

			reg_state.append(" SP:");
			temp_reg = (Regs[SPh] << 8) + Regs[SPl];
			sprintf_s(val_char_1, 5, "%04X", temp_reg);
			reg_state.append(val_char_1, 4);

			reg_state.append(" Cy:");			
			reg_state.append(val_char_1, sprintf_s(val_char_1, 32, "%16u", (uint64_t)TotalExecutedCycles));
			reg_state.append(" ");
			
			reg_state.append(FlagCget() ? "C" : "c");
			reg_state.append(FlagNget() ? "N" : "n");
			reg_state.append(FlagHget() ? "H" : "h");
			reg_state.append(FlagZget() ? "Z" : "z");
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
				format = mnemonics[A + 256];
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

		const string mnemonics[512] =
		{
			"NOP", "LD   BC,d16", "LD   (BC),A", "INC  BC",  "INC  B",  "DEC  B",  "LD   B,d8",  "RLCA", // 00
			"LD   (a16),SP", "ADD  HL,BC", "LD   A,(BC)", "DEC  BC", "INC  C", "DEC  C", "LD   C,d8", "RRCA", // 08
			"STOP 0", "LD   DE,d16", "LD   (DE),A", "INC  DE", "INC  D", "DEC  D", "LD   D,d8", "RLA", // 10
			"JR   r8", "ADD  HL,DE", "LD   A,(DE)", "DEC  DE", "INC  E", "DEC  E", "LD   E,d8", "RRA", // 18
			"JR   NZ,r8", "LD   HL,d16", "LD   (HL+),A", "INC  HL", "INC  H", "DEC  H", "LD   H,d8", "DAA", // 20
			"JR   Z,r8", "ADD  HL,HL", "LD   A,(HL+)", "DEC  HL", "INC  L", "DEC  L", "LD   L,d8", "CPL", // 28
			"JR   NC,r8", "LD   SP,d16", "LD   (HL-),A", "INC  SP", "INC  (HL)", "DEC  (HL)", "LD   (HL),d8", "SCF", // 30
			"JR   C,r8", "ADD  HL,SP", "LD   A,(HL-)", "DEC  SP", "INC  A", "DEC  A", "LD   A,d8", "CCF", // 38
			"LD   B,B", "LD   B,C", "LD   B,D", "LD   B,E", "LD   B,H", "LD   B,L", "LD   B,(HL)", "LD   B,A", // 40
			"LD   C,B", "LD   C,C", "LD   C,D", "LD   C,E", "LD   C,H", "LD   C,L", "LD   C,(HL)", "LD   C,A", // 48
			"LD   D,B", "LD   D,C", "LD   D,D", "LD   D,E", "LD   D,H", "LD   D,L", "LD   D,(HL)", "LD   D,A", // 50
			"LD   E,B", "LD   E,C", "LD   E,D", "LD   E,E", "LD   E,H", "LD   E,L", "LD   E,(HL)", "LD   E,A", // 58
			"LD   H,B", "LD   H,C", "LD   H,D", "LD   H,E", "LD   H,H", "LD   H,L", "LD   H,(HL)", "LD   H,A", // 60
			"LD   L,B", "LD   L,C", "LD   L,D", "LD   L,E", "LD   L,H", "LD   L,L", "LD   L,(HL)", "LD   L,A", // 68
			"LD   (HL),B", "LD   (HL),C", "LD   (HL),D", "LD   (HL),E", "LD   (HL),H", "LD   (HL),L", "HALT", "LD   (HL),A", // 70
			"LD   A,B", "LD   A,C", "LD   A,D", "LD   A,E", "LD   A,H", "LD   A,L", "LD   A,(HL)", "LD   A,A", // 78
			"ADD  A,B", "ADD  A,C", "ADD  A,D", "ADD  A,E", "ADD  A,H", "ADD  A,L", "ADD  A,(HL)", "ADD  A,A", // 80
			"ADC  A,B", "ADC  A,C", "ADC  A,D", "ADC  A,E", "ADC  A,H", "ADC  A,L", "ADC  A,(HL)", "ADC  A,A", // 88
			"SUB  B", "SUB  C", "SUB  D", "SUB  E", "SUB  H",  "SUB  L", "SUB  (HL)", "SUB  A", // 90
			"SBC  A,B", "SBC  A,C", "SBC  A,D", "SBC  A,E", "SBC  A,H", "SBC  A,L", "SBC  A,(HL)", "SBC  A,A", // 98
			"AND  B", "AND  C", "AND  D",  "AND  E", "AND  H", "AND  L", "AND  (HL)", "AND  A", // A0
			"XOR  B", "XOR  C", "XOR  D", "XOR  E", "XOR  H", "XOR  L", "XOR  (HL)", "XOR  A", // A8
			"OR   B", "OR   C", "OR   D", "OR   E", "OR   H", "OR   L", "OR   (HL)", "OR   A", // B0
			"CP   B", "CP   C", "CP   D", "CP   E", "CP   H", "CP   L", "CP   (HL)", "CP   A", // B8
			"RET  NZ", "POP  BC",  "JP   NZ,a16", "JP   a16", "CALL NZ,a16", "PUSH BC", "ADD  A,d8", "RST  00H", // C0
			"RET  Z", "RET", "JP   Z,a16", "PREFIX CB", "CALL Z,a16", "CALL a16", "ADC  A,d8", "RST  08H", // C8
			"RET  NC", "POP  DE", "JP   NC,a16", "???", "CALL NC,a16", "PUSH DE", "SUB  d8", "RST  10H", // D0
			"RET  C", "RETI", "JP   C,a16", "???", "CALL C,a16", "???", "SBC  A,d8", "RST  18H", // D8
			"LDH  (a8),A", "POP  HL", "LD   (C),A", "???", "???", "PUSH HL", "AND  d8", "RST  20H", // E0
			"ADD  SP,r8", "JP   (HL)", "LD   (a16),A", "???", "???", "???", "XOR  d8", "RST  28H", // E8
			"LDH  A,(a8)", "POP  AF", "LD   A,(C)", "DI", "???", "PUSH AF", "OR   d8", "RST  30H", // F0
			"LD   HL,SP+r8", "LD   SP,HL", "LD   A,(a16)", "EI   ", "???", "???", "CP   d8", "RST  38H", // F8

			"RLC  B", // 00
			"RLC  C", // 01
			"RLC  D", // 02
			"RLC  E", // 03
			"RLC  H", // 04
			"RLC  L", // 05
			"RLC  (HL)", // 06
			"RLC  A", // 07
			"RRC  B", // 08
			"RRC  C", // 09
			"RRC  D", // 0a
			"RRC  E", // 0b
			"RRC  H", // 0c
			"RRC  L", // 0d
			"RRC  (HL)", // 0e
			"RRC  A", // 0f
			"RL   B", // 10
			"RL   C", // 11
			"RL   D", // 12
			"RL   E", // 13
			"RL   H", // 14
			"RL   L", // 15
			"RL   (HL)", // 16
			"RL   A", // 17
			"RR   B", // 18
			"RR   C", // 19
			"RR   D", // 1a
			"RR   E", // 1b
			"RR   H", // 1c
			"RR   L", // 1d
			"RR   (HL)", // 1e
			"RR   A", // 1f
			"SLA  B", // 20
			"SLA  C", // 21
			"SLA  D", // 22
			"SLA  E", // 23
			"SLA  H", // 24
			"SLA  L", // 25
			"SLA  (HL)", // 26
			"SLA  A", // 27
			"SRA  B", // 28
			"SRA  C", // 29
			"SRA  D", // 2a
			"SRA  E", // 2b
			"SRA  H", // 2c
			"SRA  L", // 2d
			"SRA  (HL)", // 2e
			"SRA  A", // 2f
			"SWAP B", // 30
			"SWAP C", // 31
			"SWAP D", // 32
			"SWAP E", // 33
			"SWAP H", // 34
			"SWAP L", // 35
			"SWAP (HL)", // 36
			"SWAP A", // 37
			"SRL  B", // 38
			"SRL  C", // 39
			"SRL  D", // 3a
			"SRL  E", // 3b
			"SRL  H", // 3c
			"SRL  L", // 3d
			"SRL  (HL)", // 3e
			"SRL  A", // 3f
			"BIT  0,B", // 40
			"BIT  0,C", // 41
			"BIT  0,D", // 42
			"BIT  0,E", // 43
			"BIT  0,H", // 44
			"BIT  0,L", // 45
			"BIT  0,(HL)", // 46
			"BIT  0,A", // 47
			"BIT  1,B", // 48
			"BIT  1,C", // 49
			"BIT  1,D", // 4a
			"BIT  1,E", // 4b
			"BIT  1,H", // 4c
			"BIT  1,L", // 4d
			"BIT  1,(HL)", // 4e
			"BIT  1,A", // 4f
			"BIT  2,B", // 50
			"BIT  2,C", // 51
			"BIT  2,D", // 52
			"BIT  2,E", // 53
			"BIT  2,H", // 54
			"BIT  2,L", // 55
			"BIT  2,(HL)", // 56
			"BIT  2,A", // 57
			"BIT  3,B", // 58
			"BIT  3,C", // 59
			"BIT  3,D", // 5a
			"BIT  3,E", // 5b
			"BIT  3,H", // 5c
			"BIT  3,L", // 5d
			"BIT  3,(HL)", // 5e
			"BIT  3,A", // 5f
			"BIT  4,B", // 60
			"BIT  4,C", // 61
			"BIT  4,D", // 62
			"BIT  4,E", // 63
			"BIT  4,H", // 64
			"BIT  4,L", // 65
			"BIT  4,(HL)", // 66
			"BIT  4,A", // 67
			"BIT  5,B", // 68
			"BIT  5,C", // 69
			"BIT  5,D", // 6a
			"BIT  5,E", // 6b
			"BIT  5,H", // 6c
			"BIT  5,L", // 6d
			"BIT  5,(HL)", // 6e
			"BIT  5,A", // 6f
			"BIT  6,B", // 70
			"BIT  6,C", // 71
			"BIT  6,D", // 72
			"BIT  6,E", // 73
			"BIT  6,H", // 74
			"BIT  6,L", // 75
			"BIT  6,(HL)", // 76
			"BIT  6,A", // 77
			"BIT  7,B", // 78
			"BIT  7,C", // 79
			"BIT  7,D", // 7a
			"BIT  7,E", // 7b
			"BIT  7,H", // 7c
			"BIT  7,L", // 7d
			"BIT  7,(HL)", // 7e
			"BIT  7,A", // 7f
			"RES  0,B", // 80
			"RES  0,C", // 81
			"RES  0,D", // 82
			"RES  0,E", // 83
			"RES  0,H", // 84
			"RES  0,L", // 85
			"RES  0,(HL)", // 86
			"RES  0,A", // 87
			"RES  1,B", // 88
			"RES  1,C", // 89
			"RES  1,D", // 8a
			"RES  1,E", // 8b
			"RES  1,H", // 8c
			"RES  1,L", // 8d
			"RES  1,(HL)", // 8e
			"RES  1,A", // 8f
			"RES  2,B", // 90
			"RES  2,C", // 91
			"RES  2,D", // 92
			"RES  2,E", // 93
			"RES  2,H", // 94
			"RES  2,L", // 95
			"RES  2,(HL)", // 96
			"RES  2,A", // 97
			"RES  3,B", // 98
			"RES  3,C", // 99
			"RES  3,D", // 9a
			"RES  3,E", // 9b
			"RES  3,H", // 9c
			"RES  3,L", // 9d
			"RES  3,(HL)", // 9e
			"RES  3,A", // 9f
			"RES  4,B", // a0
			"RES  4,C", // a1
			"RES  4,D", // a2
			"RES  4,E", // a3
			"RES  4,H", // a4
			"RES  4,L", // a5
			"RES  4,(HL)", // a6
			"RES  4,A", // a7
			"RES  5,B", // a8
			"RES  5,C", // a9
			"RES  5,D", // aa
			"RES  5,E", // ab
			"RES  5,H", // ac
			"RES  5,L", // ad
			"RES  5,(HL)", // ae
			"RES  5,A", // af
			"RES  6,B", // b0
			"RES  6,C", // b1
			"RES  6,D", // b2
			"RES  6,E", // b3
			"RES  6,H", // b4
			"RES  6,L", // b5
			"RES  6,(HL)", // b6
			"RES  6,A", // b7
			"RES  7,B", // b8
			"RES  7,C", // b9
			"RES  7,D", // ba
			"RES  7,E", // bb
			"RES  7,H", // bc
			"RES  7,L", // bd
			"RES  7,(HL)", // be
			"RES  7,A", // bf
			"SET  0,B", // c0
			"SET  0,C", // c1
			"SET  0,D", // c2
			"SET  0,E", // c3
			"SET  0,H", // c4
			"SET  0,L", // c5
			"SET  0,(HL)", // c6
			"SET  0,A", // c7
			"SET  1,B", // c8
			"SET  1,C", // c9
			"SET  1,D", // ca
			"SET  1,E", // cb
			"SET  1,H", // cc
			"SET  1,L", // cd
			"SET  1,(HL)", // ce
			"SET  1,A", // cf
			"SET  2,B", // d0
			"SET  2,C", // d1
			"SET  2,D", // d2
			"SET  2,E", // d3
			"SET  2,H", // d4
			"SET  2,L", // d5
			"SET  2,(HL)", // d6
			"SET  2,A", // d7
			"SET  3,B", // d8
			"SET  3,C", // d9
			"SET  3,D", // da
			"SET  3,E", // db
			"SET  3,H", // dc
			"SET  3,L", // dd
			"SET  3,(HL)", // de
			"SET  3,A", // df
			"SET  4,B", // e0
			"SET  4,C", // e1
			"SET  4,D", // e2
			"SET  4,E", // e3
			"SET  4,H", // e4
			"SET  4,L", // e5
			"SET  4,(HL)", // e6
			"SET  4,A", // e7
			"SET  5,B", // e8
			"SET  5,C", // e9
			"SET  5,D", // ea
			"SET  5,E", // eb
			"SET  5,H", // ec
			"SET  5,L", // ed
			"SET  5,(HL)", // ee
			"SET  5,A", // ef
			"SET  6,B", // f0
			"SET  6,C", // f1
			"SET  6,D", // f2
			"SET  6,E", // f3
			"SET  6,H", // f4
			"SET  6,L", // f5
			"SET  6,(HL)", // f6
			"SET  6,A", // f7
			"SET  7,B", // f8
			"SET  7,C", // f9
			"SET  7,D", // fa
			"SET  7,E", // fb
			"SET  7,H", // fc
			"SET  7,L", // fd
			"SET  7,(HL)", // fe
			"SET  7,A", // ff
		};

		#pragma endregion

		#pragma region State Save / Load

		uint8_t* SaveState(uint8_t* saver)
		{
			*saver = (uint8_t)(NO_prefix ? 1 : 0); saver++;
			*saver = (uint8_t)(CB_prefix ? 1 : 0); saver++;
			*saver = (uint8_t)(IX_prefix ? 1 : 0); saver++;
			*saver = (uint8_t)(EXTD_prefix ? 1 : 0); saver++;
			*saver = (uint8_t)(IY_prefix ? 1 : 0); saver++;
			*saver = (uint8_t)(IXCB_prefix ? 1 : 0); saver++;
			*saver = (uint8_t)(IYCB_prefix ? 1 : 0); saver++;
			*saver = (uint8_t)(halted ? 1 : 0); saver++;
			*saver = (uint8_t)(I_skip ? 1 : 0); saver++;
			*saver = (uint8_t)(FlagI ? 1 : 0); saver++;
			*saver = (uint8_t)(FlagW ? 1 : 0); saver++;
			*saver = (uint8_t)(IFF1 ? 1 : 0); saver++;
			*saver = (uint8_t)(IFF2 ? 1 : 0); saver++;
			*saver = (uint8_t)(nonMaskableInterrupt ? 1 : 0); saver++;
			*saver = (uint8_t)(nonMaskableInterruptPending ? 1 : 0); saver++;
			*saver = (uint8_t)(jp_cond_chk ? 1 : 0); saver++;
			*saver = (uint8_t)(cond_chk_fail ? 1 : 0); saver++;

			*saver = opcode; saver++;
			*saver = temp_R; saver++;
			*saver = EI_pending; saver++;
			*saver = interruptMode; saver++;
			*saver = ExternalDB; saver++;
			*saver = instr_bank; saver++;

			for (int i = 0; i < 36; i++) { *saver = Regs[i]; saver++; }

			*saver = (uint8_t)(PRE_SRC & 0xFF); saver++; *saver = (uint8_t)((PRE_SRC >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((PRE_SRC >> 16) & 0xFF); saver++; *saver = (uint8_t)((PRE_SRC >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(instr_pntr & 0xFF); saver++; *saver = (uint8_t)((instr_pntr >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((instr_pntr >> 16) & 0xFF); saver++; *saver = (uint8_t)((instr_pntr >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(bus_pntr & 0xFF); saver++; *saver = (uint8_t)((bus_pntr >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((bus_pntr >> 16) & 0xFF); saver++; *saver = (uint8_t)((bus_pntr >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(mem_pntr & 0xFF); saver++; *saver = (uint8_t)((mem_pntr >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((mem_pntr >> 16) & 0xFF); saver++; *saver = (uint8_t)((mem_pntr >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(irq_pntr & 0xFF); saver++; *saver = (uint8_t)((irq_pntr >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((irq_pntr >> 16) & 0xFF); saver++; *saver = (uint8_t)((irq_pntr >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(IRQS & 0xFF); saver++; *saver = (uint8_t)((IRQS >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((IRQS >> 16) & 0xFF); saver++; *saver = (uint8_t)((IRQS >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(Ztemp2_saver & 0xFF); saver++; *saver = (uint8_t)((Ztemp2_saver >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((Ztemp2_saver >> 16) & 0xFF); saver++; *saver = (uint8_t)((Ztemp2_saver >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(IRQS_cond_offset & 0xFF); saver++; *saver = (uint8_t)((IRQS_cond_offset >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((IRQS_cond_offset >> 16) & 0xFF); saver++; *saver = (uint8_t)((IRQS_cond_offset >> 24) & 0xFF); saver++;

			*saver = (uint8_t)(TotalExecutedCycles & 0xFF); saver++; *saver = (uint8_t)((TotalExecutedCycles >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((TotalExecutedCycles >> 16) & 0xFF); saver++; *saver = (uint8_t)((TotalExecutedCycles >> 24) & 0xFF); saver++;
			*saver = (uint8_t)((TotalExecutedCycles >> 16) & 0x32); saver++; *saver = (uint8_t)((TotalExecutedCycles >> 40) & 0xFF); saver++;
			*saver = (uint8_t)((TotalExecutedCycles >> 16) & 0x48); saver++; *saver = (uint8_t)((TotalExecutedCycles >> 56) & 0xFF); saver++;

			return saver;
		}

		uint8_t* LoadState(uint8_t* loader)
		{
			NO_prefix = *loader == 1; loader++;
			CB_prefix = *loader == 1; loader++;
			IX_prefix = *loader == 1; loader++;
			EXTD_prefix = *loader == 1; loader++;
			IY_prefix = *loader == 1; loader++;
			IXCB_prefix = *loader == 1; loader++;
			IYCB_prefix = *loader == 1; loader++;
			halted = *loader == 1; loader++;
			I_skip = *loader == 1; loader++;
			FlagI = *loader == 1; loader++;
			FlagW = *loader == 1; loader++;
			IFF1 = *loader == 1; loader++;
			IFF2 = *loader == 1; loader++;
			nonMaskableInterrupt = *loader == 1; loader++;
			nonMaskableInterruptPending = *loader == 1; loader++;
			jp_cond_chk = *loader == 1; loader++;
			cond_chk_fail = *loader == 1; loader++;

			opcode = *loader; loader++;
			temp_R = *loader; loader++;
			EI_pending = *loader; loader++;
			interruptMode = *loader; loader++;
			ExternalDB = *loader; loader++;
			instr_bank = *loader; loader++;

			for (int i = 0; i < 36; i++) { Regs[i] = *loader; loader++; }

			PRE_SRC = *loader; loader++; PRE_SRC |= (*loader << 8); loader++;
			PRE_SRC |= (*loader << 16); loader++; PRE_SRC |= (*loader << 24); loader++;

			instr_pntr = *loader; loader++; instr_pntr |= (*loader << 8); loader++;
			instr_pntr |= (*loader << 16); loader++; instr_pntr |= (*loader << 24); loader++;

			bus_pntr = *loader; loader++; bus_pntr |= (*loader << 8); loader++;
			bus_pntr |= (*loader << 16); loader++; bus_pntr |= (*loader << 24); loader++;

			mem_pntr = *loader; loader++; mem_pntr |= (*loader << 8); loader++;
			mem_pntr |= (*loader << 16); loader++; mem_pntr |= (*loader << 24); loader++;

			irq_pntr = *loader; loader++; irq_pntr |= (*loader << 8); loader++;
			irq_pntr |= (*loader << 16); loader++; irq_pntr |= (*loader << 24); loader++;

			IRQS = *loader; loader++; IRQS |= (*loader << 8); loader++;
			IRQS |= (*loader << 16); loader++; IRQS |= (*loader << 24); loader++;

			Ztemp2_saver = *loader; loader++; Ztemp2_saver |= (*loader << 8); loader++;
			Ztemp2_saver |= (*loader << 16); loader++; Ztemp2_saver |= (*loader << 24); loader++;

			IRQS_cond_offset = *loader; loader++; IRQS_cond_offset |= (*loader << 8); loader++;
			IRQS_cond_offset |= (*loader << 16); loader++; IRQS_cond_offset |= (*loader << 24); loader++;

			// load instruction pointers based on state
			if (instr_bank == 0) 
			{
				cur_instr_ofst = &NoIndex[opcode * 38];
				cur_bus_ofst = &NoIndexBUSRQ[opcode * 19];
				cur_mem_ofst = &NoIndexMEMRQ[opcode * 19];
				cur_irqs_ofst = &NoIndexIRQS[opcode];
			}
			else if (instr_bank == 1) 
			{
				cur_instr_ofst = &CBIndex[opcode * 38];
				cur_bus_ofst = &CBIndexBUSRQ[opcode * 19];
				cur_mem_ofst = &CBIndexMEMRQ[opcode * 19];
				cur_irqs_ofst = &CBIndexIRQS[opcode];
			}
			else if (instr_bank == 2)
			{
				cur_instr_ofst = &EXTIndex[opcode * 38];
				cur_bus_ofst = &EXTIndexBUSRQ[opcode * 19];
				cur_mem_ofst = &EXTIndexMEMRQ[opcode * 19];
				cur_irqs_ofst = &EXTIndexIRQS[opcode];
			}
			else if (instr_bank == 3)
			{
				cur_instr_ofst = &IXIndex[opcode * 38];
				cur_bus_ofst = &IXIndexBUSRQ[opcode * 19];
				cur_mem_ofst = &IXIndexMEMRQ[opcode * 19];
				cur_irqs_ofst = &IXIndexIRQS[opcode];
			}
			else if (instr_bank == 4)
			{
				cur_instr_ofst = &IYIndex[opcode * 38];
				cur_bus_ofst = &IYIndexBUSRQ[opcode * 19];
				cur_mem_ofst = &IYIndexMEMRQ[opcode * 19];
				cur_irqs_ofst = &IYIndexIRQS[opcode];
			}
			else if (instr_bank == 5)
			{
				cur_instr_ofst = &IXYCBIndex[opcode * 38];
				cur_bus_ofst = &IXYCBIndexBUSRQ[opcode * 19];
				cur_mem_ofst = &IXYCBIndexMEMRQ[opcode * 19];
				cur_irqs_ofst = &IXYCBIndexIRQS[opcode];
			}
			else if (instr_bank == 6)
			{
				cur_instr_ofst = &Reset_CPU[0];
				cur_bus_ofst = &Reset_BUSRQ[0];
				cur_mem_ofst = &Reset_MEMRQ[0];
				cur_irqs_ofst = &Reset_IRQS;
			}
			else if (instr_bank == 7)
			{
				cur_instr_ofst = &LD_OP_R_INST[0];
				cur_instr_ofst[14] = Ztemp2_saver;
				cur_bus_ofst = &LD_OP_R_BUSRQ[0];
				cur_mem_ofst = &LD_OP_R_MEMRQ[0];
				cur_irqs_ofst = &LD_OP_R_IRQS;
			}
			else if (instr_bank == 8)
			{
				cur_instr_ofst = &LD_CP_R_INST[0];
				cur_instr_ofst[14] = Ztemp2_saver;
				cur_bus_ofst = &LD_CP_R_BUSRQ[0];
				cur_mem_ofst = &LD_CP_R_MEMRQ[0];
				cur_irqs_ofst = &LD_CP_R_IRQS;
			}
			else if (instr_bank == 9)
			{
				cur_instr_ofst = &REP_OP_I_INST[0];
				cur_instr_ofst[8] = Ztemp2_saver;
				cur_bus_ofst = &REP_OP_I_BUSRQ[0];
				cur_mem_ofst = &REP_OP_I_MEMRQ[0];
				cur_irqs_ofst = &REP_OP_I_IRQS;
			}
			else if (instr_bank == 10)
			{
				cur_instr_ofst = &REP_OP_O_INST[0];
				cur_bus_ofst = &REP_OP_O_BUSRQ[0];
				cur_mem_ofst = &REP_OP_O_MEMRQ[0];
				cur_irqs_ofst = &REP_OP_O_IRQS;
			}
			else if (instr_bank == 11)
			{
				cur_instr_ofst = &NO_HALT_INST[0];
				cur_bus_ofst = &NO_HALT_BUSRQ[0];
				cur_mem_ofst = &NO_HALT_MEMRQ[0];
				cur_irqs_ofst = &NO_HALT_IRQS;
			}
			else if (instr_bank == 12)
			{
				cur_instr_ofst = &NMI_INST[0];
				cur_bus_ofst = &NMI_BUSRQ[0];
				cur_mem_ofst = &NMI_MEMRQ[0];
				cur_irqs_ofst = &NMI_IRQS;
			}
			else if (instr_bank == 13)
			{
				cur_instr_ofst = &IRQ0_INST[0];
				cur_bus_ofst = &IRQ0_BUSRQ[0];
				cur_mem_ofst = &IRQ0_MEMRQ[0];
				cur_irqs_ofst = &IRQ0_IRQS;
			}
			else if (instr_bank == 14)
			{
				cur_instr_ofst = &IRQ1_INST[0];
				cur_bus_ofst = &IRQ1_BUSRQ[0];
				cur_mem_ofst = &IRQ1_MEMRQ[0];
				cur_irqs_ofst = &IRQ1_IRQS;
			}
			else if (instr_bank == 15)
			{
				cur_instr_ofst = &IRQ2_INST[0];
				cur_bus_ofst = &IRQ2_BUSRQ[0];
				cur_mem_ofst = &IRQ2_MEMRQ[0];
				cur_irqs_ofst = &IRQ2_IRQS;
			}

			if (cond_chk_fail) 
			{
				cur_irqs_ofst = &False_IRQS[IRQS_cond_offset];
			}

			TotalExecutedCycles = *loader; loader++; TotalExecutedCycles |= ((uint64_t)*loader << 8); loader++;
			TotalExecutedCycles |= ((uint64_t)*loader << 16); loader++; TotalExecutedCycles |= ((uint64_t)*loader << 24); loader++;
			TotalExecutedCycles |= ((uint64_t)*loader << 32); loader++; TotalExecutedCycles |= ((uint64_t)*loader << 40); loader++;
			TotalExecutedCycles |= ((uint64_t)*loader << 48); loader++; TotalExecutedCycles |= ((uint64_t)*loader << 56); loader++;

			return loader;
		}

		#pragma endregion
	};
}
