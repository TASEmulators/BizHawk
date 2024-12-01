namespace BizHawk.Emulation.Cores.Components.FairchildF8
{
	/// <summary>
	/// Vectors of Instruction Operations
	/// </summary>
	public sealed partial class F3850<TLink>
	{
		/// <summary>
		/// LR - LOAD REGISTER 
		/// The LR group of instructions move one or two bytes of data between a source and destination register.
		/// No status bits are modified. 
		/// </summary>
		private void LR_A_KU()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, A, Kh,				// A <- (r12)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		/// <summary>
		/// LR - LOAD REGISTER 
		/// The LR group of instructions move one or two bytes of data between a source and destination register.
		/// No status bits are modified. 
		/// </summary>
		private void LR_A_KL()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, A, Kl,              // A <- (r13)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		/// <summary>
		/// LR - LOAD REGISTER 
		/// The LR group of instructions move one or two bytes of data between a source and destination register.
		/// No status bits are modified. 
		/// </summary>
		private void LR_A_QU()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, A, Qh,              // A <- (r14)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		/// <summary>
		/// LR - LOAD REGISTER 
		/// The LR group of instructions move one or two bytes of data between a source and destination register.
		/// No status bits are modified. 
		/// </summary>
		private void LR_A_QL()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, A, Ql,              // A <- (r15)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		/// <summary>
		/// LR - LOAD REGISTER 
		/// The LR group of instructions move one or two bytes of data between a source and destination register.
		/// No status bits are modified. 
		/// </summary>
		private void LR_KU_A()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, Kh, A,              // r12 <- (A)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		/// <summary>
		/// LR - LOAD REGISTER 
		/// The LR group of instructions move one or two bytes of data between a source and destination register.
		/// No status bits are modified. 
		/// </summary>
		private void LR_KL_A()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, Kl, A,              // r13 <- (A)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		/// <summary>
		/// LR - LOAD REGISTER 
		/// The LR group of instructions move one or two bytes of data between a source and destination register.
		/// No status bits are modified. 
		/// </summary>
		private void LR_QU_A()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, Qh, A,              // r14 <- (A)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		/// <summary>
		/// LR - LOAD REGISTER 
		/// The LR group of instructions move one or two bytes of data between a source and destination register.
		/// No status bits are modified. 
		/// </summary>
		private void LR_QL_A()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, Ql, A,              // r15 <- (A)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		/// <summary>
		/// LR - LOAD REGISTER 
		/// The LR group of instructions move one or two bytes of data between a source and destination register.
		/// No status bits are modified. 
		/// </summary>
		private void LR_K_P()
		{
			PopulateCURINSTR(
				// L
				ROMC_07,					// DB <- (PC1h)
				IDLE,
				IDLE,
				OP_LR8, Kh, DB,				// r12 <- (DB)
				IDLE,
				IDLE,
				// L
				ROMC_0B,                    // DB <- (PC1l)
				IDLE,
				IDLE,
				OP_LR8, Kl, DB,             // r13 <- (DB)
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// LR - LOAD REGISTER 
		/// The LR group of instructions move one or two bytes of data between a source and destination register.
		/// No status bits are modified. 
		/// </summary>
		private void LR_P_K()
		{
			PopulateCURINSTR(
				// L
				OP_LR8, DB, Kh,				// DB <- (r12)
				IDLE,
				IDLE,
				ROMC_15,					// PC1h <- (DB)
				IDLE,
				IDLE,
				// L
				OP_LR8, DB, Kl,             // DB <- (r13)
				IDLE,
				IDLE,
				ROMC_18,					// PC1l <- (DB)
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// LR - LOAD REGISTER 
		/// The LR group of instructions move one or two bytes of data between a source and destination register.
		/// No status bits are modified. 
		/// </summary>
		private void LR_A_IS()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, A, ISAR,			// A <- (ISAR)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		/// <summary>
		/// LR - LOAD REGISTER 
		/// The LR group of instructions move one or two bytes of data between a source and destination register.
		/// No status bits are modified. 
		/// </summary>
		private void LR_IS_A()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, ISAR, A,			// ISAR <- (A)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		/// <summary>
		/// PK - CALL TO SUBROUTINE DIRECT AND RETURN FROM SUBROUTINE DIRECT
		/// The contents of the Program Counter Registers (PCO) are stored in the Stack Registers (PC1), 
		/// then the contents of the Scratchpad K Registers (Registers 12 and 13 of scratchpad memory) are transferred into the Program Counter Registers.
		/// No status bits are modified. 
		/// </summary>
		private void PK()
		{
			PopulateCURINSTR(
				// L
				OP_LR8, DB, Kl,				// DB <- (r13)
				IDLE,
				IDLE,
				ROMC_12,					// PC1 <- (PC0); PC0l <- (DB)
				IDLE,
				IDLE,
				// L
				OP_LR8, DB, Kh,				// DB <- (r12)
				IDLE,
				IDLE,
				ROMC_14,					// PC0h <- (DB)
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// LR - LOAD REGISTER 
		/// The LR group of instructions move one or two bytes of data between a source and destination register.
		/// No status bits are modified. 
		/// </summary>
		private void LR_P0_Q()
		{
			PopulateCURINSTR(
				// L
				OP_LR8, DB, Ql,				// DB <- (r15)
				//OP_EI,                    // Set ICB Flag
				IDLE,
				IDLE,
				ROMC_17,					// PC0l <- (DB)
				IDLE,
				IDLE,
				// L
				OP_LR8, DB, Qh,				// DB <- (r14)
				IDLE,
				IDLE,
				ROMC_14,					// PC0h <- DB
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// LR - LOAD REGISTER 
		/// The LR group of instructions move one or two bytes of data between a source and destination register.
		/// No status bits are modified. 
		/// </summary>
		private void LR_Q_DC()
		{
			PopulateCURINSTR(
				// L
				ROMC_06,					// DB <- (DC0h)
				IDLE,
				IDLE,
				OP_LR8, Qh, DB,				// r14 <- (DB)
				IDLE,
				IDLE,
				// L
				ROMC_09,					// DB <- (DC0l)
				IDLE,
				IDLE,
				OP_LR8, Ql, DB,				// r15 <- (DB)
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// LR - LOAD REGISTER 
		/// The LR group of instructions move one or two bytes of data between a source and destination register.
		/// No status bits are modified. 
		/// </summary>
		private void LR_DC_Q()
		{
			PopulateCURINSTR(
				// L
				OP_LR8, DB, Qh,				// DB <- (r14)
				IDLE,
				IDLE,
				ROMC_16,					// DC0h <- (DB)
				IDLE,
				IDLE,
				// L
				OP_LR8, DB, Ql,				// DB <- (r15)
				IDLE,
				IDLE,
				ROMC_19,					// DC0l <- (DB)
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// LR - LOAD REGISTER 
		/// The LR group of instructions move one or two bytes of data between a source and destination register.
		/// No status bits are modified. 
		/// </summary>
		private void LR_DC_H()
		{
			PopulateCURINSTR(
				// L
				OP_LR8, DB, Hh,				// DB <- (r10)
				IDLE,
				IDLE,
				ROMC_16,                    // DC0h <- (DB)
				IDLE,
				IDLE,
				// L
				OP_LR8, DB, Hl,				// DB <- (r11)
				IDLE,
				IDLE,
				ROMC_19,                    // DC0l <- (DB)
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// LR - LOAD REGISTER 
		/// The LR group of instructions move one or two bytes of data between a source and destination register.
		/// No status bits are modified. 
		/// </summary>
		private void LR_H_DC()
		{
			PopulateCURINSTR(
				// L
				ROMC_06,					// DB <- (DC0h)
				IDLE,
				IDLE,
				OP_LR8, Hh, DB,				// r10 <- (DB)
				IDLE,
				IDLE,
				// L
				ROMC_09,					// DB <- (DC0l)
				IDLE,
				IDLE,
				OP_LR8, Hl, DB,				// r11 <- (DB)
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// SR - SHIFT RIGHT
		/// The contents of the accumulator are shifted right either one or four bit positions, depending on the value of the SR instruction operand.
		/// Statuses modified: ZERO, SIGN 
		/// Statuses reset: OVF, CARRY 
		/// Statuses unaffected: ICB
		/// </summary>
		private void SR(byte index)
		{
			PopulateCURINSTR(
				// S
				OP_SHFT_R, A, index,		// A >> (index)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		/// <summary>
		/// SL - SHIFT LEFT
		/// The contents of the accumulator are shifted left either one or four bit positions, depending upon the value of the SL instruction operand. 
		/// Statuses modified: ZERO, SIGN
		/// Statuses reset: OVF, CARRY
		/// Statuses unaffected: ICB
		/// </summary>
		private void SL(byte index)
		{
			PopulateCURINSTR(
				// S
				OP_SHFT_L, A, index,		// A << (index)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		/// <summary>
		/// LM - LOAD ACCUMULATOR FROM MEMORY
		/// The contents of the memory byte addressed by the DCO registers are loaded into the accumulator. 
		/// The contents of the DCO registers are incremented as a resu It of the LM instruction execution.
		/// No status bits are modified. 
		/// </summary>
		private void LM()
		{
			PopulateCURINSTR(
				// L
				ROMC_02,					// DB <- ((DC0)); DC0++
				IDLE,
				IDLE,
				OP_LR8, A, DB,				// A <- (DB)
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// ST - STORE TO MEMORY
		/// The contents of the accumulator are stored in the memory location addressed by the Data Counter (DCO) registers.
		/// The DC registers' contents are incremented as a result of the instruction execution.
		/// No status bits are modified. 
		/// </summary>
		private void ST()
		{
			PopulateCURINSTR(
				// L
				OP_LR8, DB, A,				// DB <- (A)
				IDLE,
				IDLE,
				ROMC_05,					// ((DC0)) <- (DB); DC0++
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// COM - COMPLEMENT
		/// The accumulator is loaded with its one's complement.
		/// Statuses modified: ZERO, SIGN
		/// Statuses reset: OVF, CARRY
		/// Status unaffected: ICB
		/// </summary>
		private void COM()
		{
			PopulateCURINSTR(
				// S
				OP_COM,						// A <- A XOR 0xFF (compliment accumulator)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		/// <summary>
		/// LNK - LlNK CARRY TO THE ACCUMULATOR
		/// The carry bit is binary added to the least significant bit of the accumulator. The result is stored in the accumulator.
		/// Statuses modified: OVF, ZERO, CARRY, SIGN
		/// Statuses unaffected: ICB
		/// </summary>
		private void LNK()
		{
			PopulateCURINSTR(
				// S
				OP_LNK,						// A <- A + FlagC
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		/// <summary>
		/// DI - DISABLE INTERRUPT
		/// The interrupt control bit, ICB, is reset; no interrupt requests will be acknowledged by the 3850 CPU
		/// Statuses reset: ICB 
		/// Statuses unaffected: OVF, ZERO, CARRY, SIGN
		/// </summary>
		private void DI()
		{
			PopulateCURINSTR(
				// S
				ROMC_1C_S,					// Idle
				OP_DI,						// Clear ICB
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// EI - ENABLE INTERRUPT
		/// The interrupt control bit is set. Interrupt requests will now be acknowledged by the CPU.
		/// ICB is set to 1. All other status bits are unaffected. 
		/// </summary>
		private void EI()
		{
			PopulateCURINSTR(
				// S
				ROMC_1C_S,                  // Idle
				OP_EI,						// Set ICB
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// POP - RETURN FROM SUBROUTINE
		/// The contents of the Stack Registers (PC1) are transferred to the Program Counter Registers (PCO).
		/// No status bits are modified.
		/// </summary>
		private void POP()
		{
			PopulateCURINSTR(
				// S
				ROMC_04,					// PC0 <- (PC1)
				IDLE,
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				//OP_EI,                      // Set ICB Flag
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// LR - LOAD REGISTER 
		/// The LR group of instructions move one or two bytes of data between a source and destination register.
		/// No status bits are modified. 
		/// </summary>
		private void LR_W_J()
		{
			PopulateCURINSTR(
				// S
				ROMC_1C_S,                  // Idle
				IDLE,
				OP_LR8, W, J,				// W <- (r9)
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				//OP_EI,                      // Set ICB Flag
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// LR - LOAD REGISTER 
		/// The LR group of instructions move one or two bytes of data between a source and destination register.
		/// No status bits are modified. 
		/// </summary>
		private void LR_J_W()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, J, W,				// r9 <- (W)    
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		/// <summary>
		/// INC - INCREMENT ACCUMULATOR 
		/// The content of the accumulator is increased by one binary count.
		/// Statuses modified: OVF, ZERO, CARRY, SIGN
		/// Statuses unaffected: ICB
		/// </summary>
		private void INC()
		{
			PopulateCURINSTR(
				// S
				OP_INC8, A,	ONE,			// A <- A + 1 
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		/// <summary>
		/// LI - LOAD IMMEDIATE 
		/// The value provided by the operand of the LI instruction is !oaded into the accumuator. 
		/// No status bits are affected. 
		/// </summary>
		private void LI()
		{
			PopulateCURINSTR(
				// L
				ROMC_03_L,					// DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				OP_LR8, A, DB,				// A <- (DB)
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// NI - AND IMMEDIATE
		/// An 8-bit value provided by the operand of the NI instruction is ANDed with the contents of the accumulator. 
		/// The results are stored in the accumulator. 
		/// Statuses reset to 0: OVF, CARRY
		/// Statuses modified: ZERO, SIGN 
		/// Statuses unaffected: ICB 
		/// </summary>
		private void NI()
		{
			PopulateCURINSTR(
				// L
				ROMC_03_L,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				OP_AND8, A, DB,				// A <- (A) AND (DB)
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// OI - OR IMMEDIATE
		/// An 8-bit value provided by the operand of the 1/0 instruction is ORed with the contents of the accumulator. 
		/// The results are stored in the accumulator. 
		/// Statuses modified: ZERO, SIGN
		/// Statuses reset: OVF, CARRY 
		/// Statuses unaffected: ICB
		/// </summary>
		private void OI()
		{
			PopulateCURINSTR(
				// L
				ROMC_03_L,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				OP_OR8, A, DB,				// A <- (A) OR (DB)
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// XI - EXCLUSIVE-OR IMMEDIATE
		/// The contents of the 8-bit value provided by the operand of the XI instruction are EXCLUSIVE-ORed with the contents of the accumulator. 
		/// The results are stored in the accumulator. 
		/// Statuses modified: ZERO, SIGN
		/// Statuses reset: OVF, CARRY
		/// Statuses unaffected: ICB
		/// </summary>
		private void XI()
		{
			PopulateCURINSTR(
				// L
				ROMC_03_L,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				OP_XOR8, A, DB,				// A <- (A) XOR (DB)
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// AI - ADD IMMEDIATE TO ACCUMULATOR
		/// The 8-bit (two hexadecimal digit) value provided by the instruction operand is added to the current contents of the accumulator. 
		/// Binary addition is performed.
		/// Statuses modified: OVF, ZERO, CARRY, SIGN
		/// Statuses unaffected: ICB
		/// </summary>
		private void AI()
		{
			PopulateCURINSTR(
				// L
				ROMC_03_L,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				OP_ADD8, A, DB,				// A <- (A) + (DB)
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// Compare Immediate
		/// The contents of the accumulator are subtracted from the operand of the CI instruction. 
		/// The result is not saved but the status bits are set or reset to reflect the results of the operation
		/// Statuses modified: OVF, ZERO, CARRY, SIGN
		/// Statuses unaffected: ICB
		/// </summary>
		private void CI()
		{
			PopulateCURINSTR(
				// L
				ROMC_03_L,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				OP_CI,						// Set flags for A <- (DB) + (-A) + 1 (do not store result in A)
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// Illegal Opcode - just do a short cycle NOP
		/// </summary>
		private void ILLEGAL()
		{
			NOP();
		}

		/// <summary>
		/// IN - INPUT LONG ADDRESS
		/// The data input to the 1/0 port specified by the operand of the IN instruction is stored in the accumulator.
		/// Statuses modified: ZERO, SIGN 
		/// Statuses reset: OVF, CARRY 
		/// Statuses unaffected: ICB 
		/// </summary>
		private void IN()
		{
			PopulateCURINSTR(
				// L
				ROMC_03_L,					// DB/IO <- ((PC0)); PC0++
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				// L
				ROMC_1B,					// DB <- ((IO));   
				IDLE,
				IDLE,
				OP_LR_A_DB_IO, A, DB,		// A <- (DB) - flags set as result of IN or INS operation
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// OUT - OUTPUT LONG ADDRESS 
		/// The I/O port addressed by the operand of the OUT instruction is loaded with the contents of the accumulator. 
		/// No status bits are modified. 
		/// </summary>
		private void OUT()
		{
			PopulateCURINSTR(
				// L
				ROMC_03_L,                  // DB/IO <- ((PC0)); PC0++
				IDLE,
				IDLE,
				OP_LR8, DB, A,				// DB <- (A)
				IDLE,
				IDLE,
				// L
				ROMC_1A,					// ((IO)) <- DB
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				//OP_EI,                      // Set ICB Flag
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// PI - CALL TO SUBROUTINE IMMEDIATE
		/// The contents of the Program Counters are stored in the Stack Registers, PC1, then the 16-bit address contained in the operand of the 
		/// PI instruction is loaded into the Program Counters.· The accumulator is used as a temporary storage register during transfer of the most significant byte of the address. 
		/// Previous accumulator results will be altered.
		/// No status bits are modified. 
		/// </summary>
		private void PI()
		{
			PopulateCURINSTR(
				// L
				ROMC_03_L,                  // DB/IO <- ((PC0)); PC0++
				IDLE,
				IDLE,
				OP_LR8, A, DB,				// A <- (DB)
				IDLE,
				IDLE,
				// S
				ROMC_0D,					// PC1 <- PC0 + 1
				IDLE,
				IDLE,
				IDLE,
				// L
				ROMC_0C,					// DB <- ((PC0)); PC0l <- (DB)
				IDLE,
				IDLE,
				OP_LR8, DB, A,				// DB <- (A)
				IDLE,
				IDLE,
				// L
				ROMC_14,					// PC0h <- (DB)
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				//OP_EI,                      // Set ICB Flag
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// JMP - Branch Immediate
		/// As the result of a JMP instruction execution, a branch to the memory location addressed by the second and third bytes of the instruction occurs. 
		/// The second byte contains the high order eight bits of the memory address; 
		/// the third byte contains the low order eight bits of the memory address.
		/// No status bits are affected. 
		/// </summary>
		private void JMP()
		{
			PopulateCURINSTR(
				// L
				ROMC_03_L,					// DB/IO <- ((PC0)); PC0++
				IDLE,
				IDLE,
				OP_LR8, A, DB,				// A <- (DB)
				IDLE,
				IDLE,
				// L
				ROMC_0C,                    // DB <- ((PC0)); PC0l <- DB
				IDLE,
				IDLE,
				OP_LR8, DB, A,				// DB <- (A)
				IDLE,
				IDLE,
				// L
				ROMC_14,                    // PC0h <- (DB)
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				//OP_EI,                      // Set ICB Flag
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// DCI - LOAD DC IMMEDIATE
		/// The DCI instruction is a three-byte instruction. The contents of the second byte replace the high order byte of the DC0 registers; 
		/// the contents of the third byte replace the low order byte of the DCO registers.
		/// The status bits are not affected. 
		/// </summary>
		private void DCI()
		{
			PopulateCURINSTR(
				// L
				ROMC_11,					// DB <- ((PC0)); DC0h <- DB
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				// S
				ROMC_03_S,					// DB/IO <- ((PC0)); PC0++
				IDLE,
				IDLE,
				IDLE,
				// L
				ROMC_0E,					// DB <- ((PC0)); DC0l <- (DB)
				IDLE,
				IDLE,  
				IDLE,
				IDLE,
				IDLE,
				// S
				ROMC_03_S,                  // DB/IO <- ((PC0)); PC0++
				IDLE,
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// NOP - NO OPERATION
		/// No function is performed. 
		/// No status bits are modified.
		/// </summary>
		private void NOP()
		{
			PopulateCURINSTR(
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// XDC - EXCHANGE DATA COUNTERS
		/// Execution of the instruction XDC causes the contents of the auxiliary data counter registers (DC1) to be exchanged with the contents of the data counter registers (DCO). 
		/// This instruction is only significant when a 3852 or 3853 Memory Interface device is part of the system configuration.
		/// No status bits are modified. 
		/// </summary>
		private void XDC()
		{
			PopulateCURINSTR(
				// S
				ROMC_1D,					// DC0 <-> DC1
				IDLE,
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// OS - DECREMENT SCRATCHPAD CONTENT
		/// The content of the scratchpad register addressed by the operand (Sreg) is decremented by one binary count. 
		/// The decrement is performed by adding H'FF' to the scratchpad register.
		/// Statuses modified: OVF, ZERO, CARRY, SIGN
		/// Statuses unaffected: ICB
		/// </summary>
		private void DS(byte rIndex)
		{
			// only scratch registers 0-16
			//rIndex = (byte)(rIndex & 0x0F);

			PopulateCURINSTR(
				// L
				IDLE,
				//OP_SUB8, rIndex, ONE,
				OP_ADD8, rIndex, BYTE,
				IDLE,
				ROMC_00_L,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		/// <summary>
		/// Same as DS, but the register pointed to by ISAR is affected
		/// </summary>
		private void DS_ISAR()
		{
			PopulateCURINSTR(
				// L
				IDLE,
				OP_ADD8, Regs[ISAR], BYTE,
				IDLE,
				ROMC_00_L,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		/// <summary>
		/// Same as DS, but the register pointed to by ISAR is affected, then the ISAR itself is incremented
		/// </summary>
		private void DS_ISAR_INC()
		{
			PopulateCURINSTR(
				// L
				IDLE,
				OP_ADD8, Regs[ISAR], BYTE,
				IDLE,
				OP_IS_INC,                  // Inc ISAR
				ROMC_00_L,                  // DB <- ((PC0)); PC0++
				END);
		}

		/// <summary>
		/// Same as DS, but the register pointed to by ISAR is affected, then the ISAR itself is decremented
		/// </summary>
		private void DS_ISAR_DEC()
		{
			PopulateCURINSTR(
				// L
				IDLE,
				OP_ADD8, Regs[ISAR], BYTE,
				IDLE,
				OP_IS_DEC,                  // Dec ISAR
				ROMC_00_L,                  // DB <- ((PC0)); PC0++
				END);
		}

		private void LR_A_R(byte rIndex)
		{
			// only scratch registers 0-16
			//rIndex = (byte)(rIndex & 0x0F);

			PopulateCURINSTR(
				// S
				OP_LR8, A, rIndex,			// A <- (rIndex)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		/// <summary>
		/// LR - LOAD REGISTER 
		/// The LR group of instructions move one or two bytes of data between a source and destination register.
		/// No status bits are modified. 
		/// </summary>
		private void LR_A_ISAR()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, A, Regs[ISAR],		// A <- ((ISAR))
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		/// <summary>
		/// LR - LOAD REGISTER 
		/// The LR group of instructions move one or two bytes of data between a source and destination register.
		/// ISAR incremented
		/// No status bits are modified. 
		/// </summary>
		private void LR_A_ISAR_INC()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, A, Regs[ISAR],      // A <- ((ISAR))
				OP_IS_INC,                  // Inc ISAR
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				END);
		}

		/// <summary>
		/// LR - LOAD REGISTER 
		/// The LR group of instructions move one or two bytes of data between a source and destination register.
		/// ISAR deccremented
		/// No status bits are modified. 
		/// </summary>
		private void LR_A_ISAR_DEC()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, A, Regs[ISAR],      // A <- ((ISAR))
				OP_IS_DEC,                  // Dec ISAR
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				END);
		}

		/// <summary>
		/// LR - LOAD REGISTER 
		/// The LR group of instructions move one or two bytes of data between a source and destination register.
		/// No status bits are modified. 
		/// </summary>
		private void LR_R_A(byte rIndex)
		{
			// only scratch registers 0-16
			//rIndex = (byte)(rIndex & 0x0F);

			PopulateCURINSTR(
				// S
				OP_LR8, rIndex, A,			// rIndex <- (A)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		/// <summary>
		/// LR - LOAD REGISTER 
		/// The LR group of instructions move one or two bytes of data between a source and destination register.
		/// No status bits are modified. 
		/// </summary>
		private void LR_ISAR_A()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, Regs[ISAR], A,      // rIndex <- (A)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		/// <summary>
		/// LR - LOAD REGISTER 
		/// The LR group of instructions move one or two bytes of data between a source and destination register.
		/// ISAR incremented
		/// No status bits are modified. 
		/// </summary>
		private void LR_ISAR_A_INC()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, Regs[ISAR], A,      // rIndex <- (A)
				OP_IS_INC,                  // Inc ISAR
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				END);
		}

		/// <summary>
		/// LR - LOAD REGISTER 
		/// The LR group of instructions move one or two bytes of data between a source and destination register.
		/// ISAR decremented
		/// No status bits are modified. 
		/// </summary>
		private void LR_ISAR_A_DEC()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, Regs[ISAR], A,      // rIndex <- (A)
				OP_IS_DEC,                  // Dec ISAR
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				END);
		}

		/// <summary>
		/// LISU - LOAD UPPER OCTAL DIGIT OF ISAR
		/// A 3-bit value provided by the LlSU instruction operand is loaded into the three most significant bits of the ISAR. The three least significant bits of the ISAR are not altered. 
		/// No status bits are affected. 
		/// </summary>
		private void LISU(byte octal)
		{
			PopulateCURINSTR(
				// S
				OP_LISU, octal,             // set the upper octal ISAR bits (b3,b4,b5)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		/// <summary>
		/// LlSL - LOAD LOWER OCTAL DIGIT OF ISAR
		/// A 3-bit value provided by the USL instruction operand is loaded into the three least significant bits of the ISAR. The three most significant bits of the ISAR are not altered.
		/// No status bits are modified. 
		/// </summary>
		private void LISL(byte octal)
		{
			PopulateCURINSTR(
				// S
				OP_LISL, octal,             // set the lower octal ISAR bits (b0,b1,b2)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		/// <summary>
		/// LIS - LOAD IMMEDIATE SHORT 
		/// A 4-bit value provided by the LIS instruction operand is loaded into the four least significant bits of the accumulator. 
		/// The most significant four bits of the accumulator are set to "0".
		/// No status bits are modified. 
		/// </summary>
		private void LIS(byte index)
		{
			PopulateCURINSTR(
				// S
				OP_LIS, index,				// A <- index
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		/// <summary>
		/// Branch on TRUE
		/// </summary>
		private void BT(byte bit)
		{
			PopulateCURINSTR(
				// S
				ROMC_1C_S,                  // Idle
				IDLE,
				IDLE,
				OP_BT, bit);
		}

		/// <summary>
		/// Branch on FALSE
		/// </summary>
		private void BF(byte bit)
		{
			PopulateCURINSTR(
				// S
				ROMC_1C_S,                  // Idle
				IDLE,
				IDLE,
				OP_BF, bit);
		}		

		/// <summary>
		/// AM - ADD (BINARY) MEMORY TO ACCUMULATOR 
		/// The content of the memory iocation addressed by the DC0 registers is added to the accumulator. The sum is returned in the accumulator. 
		/// Memory is not altered. Binary addition is performed. The contents of the DCO registers are incremented by 1
		/// Statuses modified: OVF, ZERO, CARRY, SIGN
		/// Statuses unaffected: ICB 
		/// </summary>
		private void AM()
		{
			PopulateCURINSTR(
				// L
				ROMC_02,                    // DB <- ((DC0)); DC0++
				IDLE,
				IDLE,
				OP_ADD8, A, DB,             // A <- A + (DB)
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// AMD - DECIMAL ADD. MEMORY TO ACCUMULATOR
		/// The accumulator and the memory location addressed by the DCO registers are assumed to contain two BCD digits. 
		/// The content of the address memory byte is added to the contents of the accumulator to give a BCD result in the accumulator
		/// Statuses modified: CARRY, ZERO
		/// Statuses not significant: OVF, SIGN 
		/// Statuses unaffected: ICB 
		/// </summary>
		private void AMD()
		{
			PopulateCURINSTR(
				// L
				ROMC_02,                    // DB <- ((DC0)); DC0++
				IDLE,
				IDLE,
				OP_ADD8D, A, DB,			// A <- (A) + (DB) decimal
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// NM - LOGICAL AND FROM MEMORY
		/// The content of memory addressed by the data counter registers is ANDed with the content of the accumulator. 
		/// The results are stored in the accumulator. The contents of the data counter registers are incremented. 
		/// Statuses reset to 0: OVF, CARRY 
		/// Statuses modified: ZERO, SIGN 
		/// Statuses unaffected: ICB 
		/// </summary>
		private void NM()
		{
			PopulateCURINSTR(
				// L
				ROMC_02,                    // DB <- ((DC0)); DC0++
				IDLE,
				IDLE,
				OP_AND8, A, DB,             // A <- (A) AND (DB)
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// OM - LOGICAL IIOR" FROM MEMORY
		/// The content of memory byte addressed by the data counter registers is ORed with the content of the accumulator. 
		/// The results are stored in the accumulator. The data counter registers are incremented. 
		/// Statuses modified: ZERO, SIGN
		/// Statuses reset: OVF, CARRY
		/// Statuses unaffected: ICB 
		/// </summary>
		private void OM()
		{
			PopulateCURINSTR(
				// L
				ROMC_02,                    // DB <- ((DC0)); DC0++
				IDLE,
				IDLE,
				OP_OR8, A, DB,              // A <- (A) OR (DB)
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// XM - EXCLUSIVE-OR FROM MEMORY
		/// The content of the memory location addressed by the DCO registers is EXCLUSIVE-ORed with the contents of the accumulator. 
		/// The results are stored in the accumulator. The DCO registers are incremented.
		/// Statuses modified: ZERO, SIGN 
		/// Statuses reset: OVF, CARRY
		/// Statuses unaffected: ICB
		/// </summary>
		private void XM()
		{
			PopulateCURINSTR(
				// L
				ROMC_02,                    // DB <- ((DC0)); DC0++
				IDLE,
				IDLE,
				OP_XOR8, A, DB,             // A <- (A) XOR (DB)
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// CM - COMPARE MEMORY TO ACCUMULATOR 
		/// The CM instruction is the same as the CI instruction except the memory contents addressed by the DCO registers, 
		/// instead of an immediate value, are compared to the contents of the accumu lator.
		/// Statuses modified: OVF, ZERO, CARRY, SIGN
		/// Statuses unaffected: ICB
		/// </summary>
		private void CM()
		{
			PopulateCURINSTR(
				// L
				ROMC_02,                    // DB <- ((DC0)); DC0++
				IDLE,
				IDLE,
				OP_CI, A,
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// ADC - ADD ACCUMULATOR TO DATA COUNTER
		/// The contents of the accumulator are treated as a signed binary number, and are added to the contents of every DCO register. 
		/// The result is stored in the DCO registers. The accumulator contents do not change.
		/// </summary>
		private void ADC()
		{
			PopulateCURINSTR(
				// L
				OP_LR8, DB, A,				// DB <- (A)
				IDLE,
				IDLE,
				ROMC_0A,
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// Branch if any of the 3 low bits of ISAR are reset
		/// Testing of ISAR is immediate so we will have to lose a CPU tick in the next phase
		/// </summary>
		private void BR7()
		{
			PopulateCURINSTR(
				OP_BR7); 
		}
		
		/// <summary>
		/// INS - INPUT SHORT ADDRESS
		/// Data input to the I/O port specified by the operand of the INS instruction is loaded into the accumulator. 
		/// An I/O port with an address within the range 0 through 1 may be accessed by this instruction
		/// Statuses modified: ZERO, SIGN
		/// Statuses reset: OVF, CARRY
		/// Statuses unaffected: ICB
		/// </summary>
		private void INS_0(byte index)
		{
			Regs[IO] = index;               // latch port index early

			PopulateCURINSTR(
				// S
				ROMC_1C_S,                  // Idle
				OP_IN, ALU0, IO,            // A <- ((Port index - 0/1))
				IDLE,				
				OP_LR_A_DB_IO, A, ALU0,     // A <- (A) - flags set as result of IN or INS operation
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// INS - INPUT SHORT ADDRESS
		/// Data input to the I/O port specified by the operand of the INS instruction is loaded into the accumulator. 
		/// An I/O port with an address within the range 4 through 15 may be accessed by this instruction
		/// Statuses modified: ZERO, SIGN
		/// Statuses reset: OVF, CARRY
		/// Statuses unaffected: ICB
		/// </summary>
		private void INS_1(byte index)
		{
			Regs[IO] = index;				// latch port index early

			PopulateCURINSTR(
				// L
				ROMC_1C_L,					// Idle
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				// L
				ROMC_1B,					// DB <- ((IO))
				IDLE,
				IDLE,
				OP_LR_A_DB_IO, A, DB,       // A <- (DB) - flags set as result of IN or INS operation
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// OUTS - OUTPUT SHORT ADDRESS
		/// The I/O port addressed by the operand of the OUTS instruction object code is loaded with the contents of the accumulator. 
		/// I/O ports with addresses from 0 to 1 may be accessed by this instruction. (Outs O or 1 is CPU port only.)
		/// No status bits are modified. 
		/// </summary>
		private void OUTS_0(byte index)
		{
			Regs[IO] = index;               // latch port index early

			PopulateCURINSTR(
				// S
				ROMC_1C_S,					// Idle
				IDLE,
				OP_OUT, IO, A,				// Port <- (A)
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// OUTS - OUTPUT SHORT ADDRESS
		/// The I/O port addressed by the operand of the OUTS instruction object code is loaded with the contents of the accumulator. 
		/// I/O ports with addresses from 3 to 15 may be accessed by this instruction.
		/// No status bits are modified. 
		/// </summary>
		private void OUTS_1(byte index)
		{
			Regs[IO] = index;               // latch port index early

			PopulateCURINSTR(
				// L
				ROMC_1C_L,					// Idle
				IDLE,
				IDLE,
				OP_LR8, DB, A,				// DB <- (A)
				IDLE,
				IDLE,
				// L
				ROMC_1A,					// ((IO)) <- (DB)
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				//OP_EI,                      // Set ICB Flag
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// AS - BINARY ADDITION, SCRATCHPAD MEMORY TO ACCUMULATOR
		/// The content of the scratchpad register referenced by the instruction operand (Sreg) is added to the accumulator using binary addition. 
		/// The result of the binary addition is stored in the accumulator. 
		/// Statuses modified: OVF, ZERO, CARRY, SIGN
		/// Statuses unaffected: ICB 
		/// </summary>
		private void AS(byte rIndex)
		{
			// only scratch registers 0-15
			//rIndex = (byte) (rIndex & 0x0F);

			PopulateCURINSTR(
				// S
				OP_ADD8, A, rIndex,         // A <- (A) + (rIndex)
				IDLE,
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				END);
		}

		/// <summary>
		/// AS - BINARY ADDITION, SCRATCHPAD MEMORY TO ACCUMULATOR
		/// The content of the scratchpad register referenced indirectly by ISAR is added to the accumulator using binary addition. 
		/// The result of the binary addition is stored in the accumulator. 
		/// Statuses modified: OVF, ZERO, CARRY, SIGN
		/// Statuses unaffected: ICB 
		/// </summary>
		private void AS_IS()
		{
			PopulateCURINSTR(
				// S
				OP_ADD8, A, Regs[ISAR],		// A <- (A) + ((ISAR));
				IDLE,
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				END);
		}

		/// <summary>
		/// AS - BINARY ADDITION, SCRATCHPAD MEMORY TO ACCUMULATOR
		/// The content of the scratchpad register referenced indirectly by ISAR is added to the accumulator using binary addition. 
		/// The result of the binary addition is stored in the accumulator. 
		/// The low order three bits of ISAR are incremented after the scratchpad register is accessed.
		/// Statuses modified: OVF, ZERO, CARRY, SIGN
		/// Statuses unaffected: ICB 
		/// </summary>
		private void AS_IS_INC()
		{
			PopulateCURINSTR(
				// S
				OP_ADD8, A, Regs[ISAR],     // A <- (A) + ((ISAR));
				OP_IS_INC,					// Inc ISAR
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				END);
		}

		/// <summary>
		/// AS - BINARY ADDITION, SCRATCHPAD MEMORY TO ACCUMULATOR
		/// The content of the scratchpad register referenced indirectly by ISAR is added to the accumulator using binary addition. 
		/// The result of the binary addition is stored in the accumulator. 
		/// The low order three bits of ISAR are decremented after the scratchpad register is accessed.
		/// Statuses modified: OVF, ZERO, CARRY, SIGN
		/// Statuses unaffected: ICB 
		/// </summary>
		private void AS_IS_DEC()
		{
			PopulateCURINSTR(
				// S
				OP_ADD8, A, Regs[ISAR],     // A <- (A) + ((ISAR));
				OP_IS_DEC,                  // Dec ISAR
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				END);
		}

		/// <summary>
		/// ASD - DECIMAL ADD, SCRATCHPAD TO ACCUMULATOR
		/// The ASD instruction is similar to the AMD instruction, except that instead of adding the contents of the memory byte addressed by the DCO registers, 
		/// the content of the scratchpad byte addressed by operand (Sreg) is added to the accumulator. 
		/// Statuses modified: CARRY, ZERO
		/// Statuses not significant: OVF, SIGN
		/// Statuses unaffected: ICB
		/// </summary>
		private void ASD(byte rIndex)
		{
			// only scratch registers 0-15
			//rIndex = (byte)(rIndex & 0x0F);

			PopulateCURINSTR(
				// S
				OP_ADD8D, A, rIndex,				
				IDLE,
				ROMC_1C_S,                  // Idle
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// ASD - DECIMAL ADD, SCRATCHPAD TO ACCUMULATOR
		/// The ASD instruction is similar to the AMD instruction, except that instead of adding the contents of the memory byte addressed by the DCO registers, 
		/// the content of the scratchpad byte referenced indirectly by ISAR is added to the accumulator. 
		/// Statuses modified: CARRY, ZERO
		/// Statuses not significant: OVF, SIGN
		/// Statuses unaffected: ICB
		/// </summary>
		private void ASD_IS()
		{
			PopulateCURINSTR(
				// S
				OP_ADD8D, A, Regs[ISAR],				
				IDLE,
				ROMC_1C_S,                  // Idle
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// ASD - DECIMAL ADD, SCRATCHPAD TO ACCUMULATOR
		/// The ASD instruction is similar to the AMD instruction, except that instead of adding the contents of the memory byte addressed by the DCO registers, 
		/// the content of the scratchpad byte referenced indirectly by ISAR is added to the accumulator. 
		/// The low order three bits of ISAR are incremented after the scratchpad register is accessed.
		/// Statuses modified: CARRY, ZERO
		/// Statuses not significant: OVF, SIGN
		/// Statuses unaffected: ICB
		/// </summary>
		private void ASD_IS_INC()
		{
			PopulateCURINSTR(
				// S
				OP_ADD8D, A, Regs[ISAR],
				OP_IS_INC,                  // Inc ISAR
				ROMC_1C_S,                  // Idle
				IDLE,				
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// ASD - DECIMAL ADD, SCRATCHPAD TO ACCUMULATOR
		/// The ASD instruction is similar to the AMD instruction, except that instead of adding the contents of the memory byte addressed by the DCO registers, 
		/// the content of the scratchpad byte referenced indirectly by ISAR is added to the accumulator. 
		/// The low order three bits of ISAR are decremented after the scratchpad register is accessed.
		/// Statuses modified: CARRY, ZERO
		/// Statuses not significant: OVF, SIGN
		/// Statuses unaffected: ICB
		/// </summary>
		private void ASD_IS_DEC()
		{
			PopulateCURINSTR(
				// S
				OP_ADD8D, A, Regs[ISAR],
				OP_IS_DEC,                  // Dec ISAR
				ROMC_1C_S,                  // Idle
				IDLE,				
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// XS - EXCLUSIVE-OR FROM SCRATCHPAD
		/// The content of the scratchpad register referenced by the operand (Sreg) is EXCLUSIVE-ORed with the contents of the accumulator.
		/// Statuses modified: ZERO, SIGN
		/// Statuses reset: OVF, CARRY 
		/// Statuses unaffected: ICB
		/// </summary>
		private void XS(byte rIndex)
		{
			// only scratch registers 0-15
			//rIndex = (byte)(rIndex & 0x0F);

			PopulateCURINSTR(
				// S
				OP_XOR8, A, rIndex,         // A <- (A) XOR (reg)
				IDLE,
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				END);
		}

		/// <summary>
		/// XS - EXCLUSIVE-OR FROM SCRATCHPAD
		/// The content of the register referenced by ISAR is EXCLUSIVE-ORed with the contents of the accumulator.
		/// Statuses modified: ZERO, SIGN
		/// Statuses reset: OVF, CARRY 
		/// Statuses unaffected: ICB
		/// </summary>
		private void XS_IS()
		{
			PopulateCURINSTR(
				// S
				OP_XOR8, A, Regs[ISAR],     // A <- (A) XOR ((ISAR))
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		/// <summary>
		/// XS - EXCLUSIVE-OR FROM SCRATCHPAD
		/// The content of the register referenced by ISAR is EXCLUSIVE-ORed with the contents of the accumulator.
		/// ISAR is incremented
		/// Statuses modified: ZERO, SIGN
		/// Statuses reset: OVF, CARRY 
		/// Statuses unaffected: ICB
		/// </summary>
		private void XS_IS_INC()
		{
			PopulateCURINSTR(
				// S
				OP_XOR8, A, Regs[ISAR],     // A <- (A) XOR ((ISAR))
				OP_IS_INC,                  // Inc ISAR
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				END);
		}

		/// <summary>
		/// XS - EXCLUSIVE-OR FROM SCRATCHPAD
		/// The content of the register referenced by ISAR is EXCLUSIVE-ORed with the contents of the accumulator.
		/// ISAR is deccremented
		/// Statuses modified: ZERO, SIGN
		/// Statuses reset: OVF, CARRY 
		/// Statuses unaffected: ICB
		/// </summary>
		private void XS_IS_DEC()
		{
			PopulateCURINSTR(
				// S
				OP_XOR8, A, Regs[ISAR],     // A <- (A) XOR ((ISAR))
				OP_IS_DEC,                  // Dec ISAR
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				END);
		}

		/// <summary>
		/// NS - LOGICAL AND FROM SCRATCHPAD MEMORY
		/// The content of the scratch pad register addressed by the operand (Sreg) is ANDed with the content of the accumulator. 
		/// The results are stored in the accumulator. 
		/// Statuses reset to 0: OVF, CARRY
		/// Statuses modified: ZERO, SIGN
		/// Statuses unaffected: ICB
		/// </summary>
		private void NS(byte rIndex)
		{
			// only scratch registers 0-15
			//rIndex = (byte)(rIndex & 0x0F);

			PopulateCURINSTR(
				// S
				OP_AND8, A, rIndex,			// A <- (A) AND (reg)
				IDLE,
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				END);
		}

		/// <summary>
		/// NS - LOGICAL AND FROM SCRATCHPAD MEMORY
		/// The content of the scratch pad register addressed by the Register ISAR is pointing at is ANDed with the content of the accumulator. 
		/// The results are stored in the accumulator. 
		/// Statuses reset to 0: OVF, CARRY
		/// Statuses modified: ZERO, SIGN
		/// Statuses unaffected: ICB
		/// </summary>
		private void NS_IS()
		{
			PopulateCURINSTR(
				// S
				OP_AND8, A, Regs[ISAR],     // A <- (A) AND ((ISAR))
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		/// <summary>
		/// NS - LOGICAL AND FROM SCRATCHPAD MEMORY
		/// The content of the scratch pad register addressed by the Register ISAR is pointing at is ANDed with the content of the accumulator. 
		/// The results are stored in the accumulator. 
		/// ISAR is incremented
		/// Statuses reset to 0: OVF, CARRY
		/// Statuses modified: ZERO, SIGN
		/// Statuses unaffected: ICB
		/// </summary>
		private void NS_IS_INC()
		{
			PopulateCURINSTR(
				// S
				OP_AND8, A, Regs[ISAR],     // A <- (A) AND ((ISAR))
				OP_IS_INC,                  // Inc ISAR
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				END);
		}

		/// <summary>
		/// NS - LOGICAL AND FROM SCRATCHPAD MEMORY
		/// The content of the scratch pad register addressed by the Register ISAR is pointing at is ANDed with the content of the accumulator. 
		/// The results are stored in the accumulator. 
		/// ISAR is decremented
		/// Statuses reset to 0: OVF, CARRY
		/// Statuses modified: ZERO, SIGN
		/// Statuses unaffected: ICB
		/// </summary>
		private void NS_IS_DEC()
		{
			PopulateCURINSTR(
				// S
				OP_AND8, A, Regs[ISAR],     // A <- (A) AND ((ISAR))
				OP_IS_DEC,                  // Dec ISAR
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				END);
		}

		/// <summary>
		/// Branching Operation
		/// </summary>
		private void DO_BRANCH(int instPtr)
		{
			instr_pntr = instPtr;
			PopulateCURINSTR(
				// L
				IDLE,
				ROMC_01,					// PC0 <- PC0 + (DB)
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				// S
				ROMC_00_S,					// DB <- ((PC0)); PC0++	
				IDLE,
				IDLE,
				END);
		}

		/// <summary>
		/// No-Branching Operation
		/// </summary>
		private void DONT_BRANCH(int instPtr)
		{
			instr_pntr = instPtr;
			PopulateCURINSTR(
				// S
				IDLE,
				ROMC_03_S,					// Immediate operand fetch
				IDLE,
				IDLE,
				// S
				ROMC_00_S,					// DB <- ((PC0)); PC0++	
				IDLE,
				IDLE,
				END);
		}
	}
}
