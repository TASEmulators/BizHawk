namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	/// <summary>
	/// Vectors of Instruction Operations
	/// </summary>
	public sealed partial class F3850
	{
		private void LR_A_KU()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, A, Kh,				// A <- (r12)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		private void LR_A_KL()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, A, Kl,              // A <- (r13)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		private void LR_A_QU()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, A, Qh,              // A <- (r14)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		private void LR_A_QL()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, A, Ql,              // A <- (r15)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		private void LR_KU_A()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, Kh, A,              // r12 <- (A)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		private void LR_KL_A()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, Kl, A,              // r13 <- (A)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		private void LR_QU_A()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, Qh, A,              // r14 <- (A)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		private void LR_QL_A()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, Ql, A,              // r15 <- (A)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

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

		private void LR_A_IS()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, A, ISAR,			// A <- (ISAR)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		private void LR_IS_A()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, ISAR, A,			// ISAR <- (A)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

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

		private void LR_P0_Q()
		{
			PopulateCURINSTR(
				// L
				OP_LR8, DB, Ql,             // DB <- (r15)
				OP_EI,                      // Set ICB Flag
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

		private void SHIFT_R(byte index)
		{
			PopulateCURINSTR(
				// S
				OP_SHFT_R, A, index,		// A >> (index)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		private void SHIFT_L(byte index)
		{
			PopulateCURINSTR(
				// S
				OP_SHFT_L, A, index,		// A << (index)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

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

		private void COM()
		{
			PopulateCURINSTR(
				// S
				OP_XOR8, A, BYTE,           // A <- A XOR 0xFF (compliment accumulator)
											//OP_COM,						// A <- A XOR 0xFF (compliment accumulator)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		private void LNK()
		{
			PopulateCURINSTR(
				// S
				OP_LNK,						// A <- A + FlagC
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

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
				OP_EI,						// Set ICB Flag
				IDLE,
				END);
		}

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
				OP_EI,                      // Set ICB Flag
				IDLE,
				END);
		}

		private void LR_J_W()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, J, W,				// r9 <- (W)    
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		private void INC()
		{
			PopulateCURINSTR(
				// S
				OP_INC8, A,	ONE,			// A <- A + 1 
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		private void LI()
		{
			PopulateCURINSTR(
				// L
				ROMC_03_L,					// DB <- ((PC0)); PC0++
				IDLE,
				IDLE, //OP_CLEAR_FLAGS,
				OP_LR8, A, DB,				// A <- (DB)
				IDLE, //OP_SET_FLAGS_SZ, A,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

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

		private void CI()
		{
			PopulateCURINSTR(
				// L
				ROMC_03_L,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				OP_CI,				// Set flags for A <- (DB) + (-A) + 1 (do not store result in A)
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}


		private void ILLEGAL()
		{
			PopulateCURINSTR(
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

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
				OP_EI,                      // Set ICB Flag
				IDLE,
				END);
		}

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
				OP_EI,                      // Set ICB Flag
				IDLE,
				END);
		}

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
				OP_EI,                      // Set ICB Flag
				IDLE,
				END);
		}

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

		private void NOP()
		{
			PopulateCURINSTR(
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

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

		private void DS(byte rIndex)
		{
			// only scratch registers 0-16
			rIndex = (byte)(rIndex & 0x0F);

			PopulateCURINSTR(
				// L
				IDLE,
				OP_SUB8, rIndex, ONE,
				//OP_ADD8, rIndex, BYTE,
				IDLE,
				ROMC_00_L,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}
		private void DS_ISAR()
		{
			PopulateCURINSTR(
				// L
				IDLE,
				OP_SUB8, Regs[ISAR], ONE,	// r[ISAR] = r[ISAR] + 0xff
				IDLE,
				ROMC_00_L,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		private void DS_ISAR_INC()
		{
			PopulateCURINSTR(
				// L
				IDLE,
				OP_SUB8, Regs[ISAR], ONE,  // r[ISAR] = r[ISAR] + 0xff
				IDLE,
				OP_IS_INC,                  // Inc ISAR
				ROMC_00_L,                  // DB <- ((PC0)); PC0++
				END);
		}

		private void DS_ISAR_DEC()
		{
			PopulateCURINSTR(
				// L
				IDLE,
				OP_SUB8, Regs[ISAR], ONE,  // r[ISAR] = r[ISAR] + 0xff
				IDLE,
				OP_IS_DEC,                  // Dec ISAR
				ROMC_00_L,                  // DB <- ((PC0)); PC0++
				END);
		}

		private void LR_A_R(byte rIndex)
		{
			// only scratch registers 0-16
			rIndex = (byte)(rIndex & 0x0F);

			PopulateCURINSTR(
				// S
				OP_LR8, A, rIndex,			// A <- (rIndex)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		private void LR_A_ISAR()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, A, Regs[ISAR],		// A <- ((ISAR))
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		private void LR_A_ISAR_INC()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, A, Regs[ISAR],      // A <- ((ISAR))
				OP_IS_INC,                  // Inc ISAR
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				END);
		}

		private void LR_A_ISAR_DEC()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, A, Regs[ISAR],      // A <- ((ISAR))
				OP_IS_DEC,                  // Dec ISAR
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				END);
		}

		private void LR_R_A(byte rIndex)
		{
			// only scratch registers 0-16
			rIndex = (byte)(rIndex & 0x0F);

			PopulateCURINSTR(
				// S
				OP_LR8, rIndex, A,			// rIndex <- (A)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		private void LR_ISAR_A()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, Regs[ISAR], A,      // rIndex <- (A)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		private void LR_ISAR_A_INC()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, Regs[ISAR], A,      // rIndex <- (A)
				OP_IS_INC,                  // Inc ISAR
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				END);
		}

		private void LR_ISAR_A_DEC()
		{
			PopulateCURINSTR(
				// S
				OP_LR8, Regs[ISAR], A,      // rIndex <- (A)
				OP_IS_DEC,                  // Dec ISAR
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				END);
		}

		private void LISU(byte octal)
		{
			PopulateCURINSTR(
				// S
				OP_LISU, octal,             // set the upper octal ISAR bits (b3,b4,b5)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		private void LISL(byte octal)
		{
			PopulateCURINSTR(
				// S
				OP_LISL, octal,             // set the lower octal ISAR bits (b0,b1,b2)
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		private void LIS(byte index)
		{
			PopulateCURINSTR(
				// S
				OP_LIS, index,				// A <- index
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		private void BT(byte index)
		{
			PopulateCURINSTR(
				// S
				ROMC_1C_S,					// Idle
				IDLE,
				IDLE,
				OP_BT, index);				// no END as there is branching logic within OP_BT
		}

		private void AM()
		{
			PopulateCURINSTR(
				// L
				ROMC_02,                    // DB <- ((DC0)); DC0++
				IDLE,
				IDLE,
				OP_ADD8, A, DB,             // A <- (DB)
				IDLE,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

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

		private void BR7()
		{
			PopulateCURINSTR(
				OP_BR7);  // no END as there is branching logic within OP_BR7
		}

		private void BF(byte index)
		{
			PopulateCURINSTR(
				// S
				ROMC_1C_S,					// Idle
				IDLE,
				IDLE,
				OP_BF, index);				// no END as there is branching logic within OP_BF
		}

		private void INS_0(byte index)
		{
			PopulateCURINSTR(
				// S
				ROMC_1C_S,                  // Idle
				IDLE,
				OP_IN, A, index,            // A <- ((Port index - 0/1))
				OP_LR_A_DB_IO, A, A,       // A <- (A) - flags set as result of IN or INS operation
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

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

		private void OUTS_0(byte index)
		{
			PopulateCURINSTR(
				// S
				ROMC_1C_S,					// Idle
				IDLE,
				OP_OUT, index, A,			// Port <- (A)
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

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
				OP_EI,                      // Set ICB Flag
				IDLE,
				END);
		}

		private void AS(byte rIndex)
		{
			// only scratch registers 0-15
			rIndex = (byte) (rIndex & 0x0F);

			PopulateCURINSTR(
				// S
				OP_ADD8, A, rIndex,         // A <- (A) + (rIndex)
				IDLE,
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				END);
		}

		private void AS_IS()
		{
			PopulateCURINSTR(
				// S
				IDLE,
				OP_ADD8, A, Regs[ISAR],		// A <- (A) + ((ISAR));
				IDLE,
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				END);
		}

		private void AS_IS_INC()
		{
			PopulateCURINSTR(
				// S
				IDLE,
				OP_ADD8, A, Regs[ISAR],     // A <- (A) + ((ISAR));
				OP_IS_INC,					// Inc ISAR
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				END);
		}

		private void AS_IS_DEC()
		{
			PopulateCURINSTR(
				// S
				IDLE,
				OP_ADD8, A, Regs[ISAR],     // A <- (A) + ((ISAR));
				OP_IS_DEC,                  // Dec ISAR
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				END);
		}

		private void ASD(byte rIndex)
		{
			// only scratch registers 0-15
			rIndex = (byte)(rIndex & 0x0F);

			PopulateCURINSTR(
				// S
				ROMC_1C_S,					// Idle
				IDLE,
				OP_ADD8D, A, rIndex,
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		private void ASD_IS()
		{
			PopulateCURINSTR(
				// S
				ROMC_1C_S,					// Idle
				IDLE,
				OP_ADD8D, A, Regs[ISAR],
				IDLE,
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		private void ASD_IS_INC()
		{
			PopulateCURINSTR(
				// S
				ROMC_1C_S,                  // Idle
				IDLE,
				OP_ADD8D, A, Regs[ISAR],
				OP_IS_INC,					// Inc ISAR
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		private void ASD_IS_DEC()
		{
			PopulateCURINSTR(
				// S
				ROMC_1C_S,                  // Idle
				IDLE,
				OP_ADD8D, A, Regs[ISAR],
				OP_IS_DEC,                  // Dec ISAR
				// S
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}

		private void XS(byte rIndex)
		{
			// only scratch registers 0-15
			rIndex = (byte)(rIndex & 0x0F);

			PopulateCURINSTR(
				// S
				IDLE,
				OP_XOR8, A, rIndex,         // A <- (A) XOR (reg)
				IDLE,
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				END);
		}

		private void XS_IS()
		{
			PopulateCURINSTR(
				// S
				OP_XOR8, A, Regs[ISAR],     // A <- (A) XOR ((ISAR))
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		private void XS_IS_INC()
		{
			PopulateCURINSTR(
				// S
				OP_XOR8, A, Regs[ISAR],     // A <- (A) XOR ((ISAR))
				OP_IS_INC,                  // Inc ISAR
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				END);
		}

		private void XS_IS_DEC()
		{
			PopulateCURINSTR(
				// S
				OP_XOR8, A, Regs[ISAR],     // A <- (A) XOR ((ISAR))
				OP_IS_DEC,                  // Dec ISAR
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				END);
		}

		private void NS(byte rIndex)
		{
			// only scratch registers 0-15
			rIndex = (byte)(rIndex & 0x0F);

			PopulateCURINSTR(
				// S
				OP_AND8, A, rIndex,			// A <- (A) AND (reg)
				IDLE,
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				END);
		}

		private void NS_IS()
		{
			PopulateCURINSTR(
				// S
				OP_AND8, A, Regs[ISAR],     // A <- (A) AND ((ISAR))
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		private void NS_IS_INC()
		{
			PopulateCURINSTR(
				// S
				OP_AND8, A, Regs[ISAR],     // A <- (A) AND ((ISAR))
				OP_IS_INC,                  // Inc ISAR
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}

		private void NS_IS_DEC()
		{
			PopulateCURINSTR(
				// S
				OP_AND8, A, Regs[ISAR],     // A <- (A) AND ((ISAR))
				OP_IS_DEC,                  // Dec ISAR
				ROMC_00_S,                  // DB <- ((PC0)); PC0++
				IDLE,
				END);
		}


		private void DO_BRANCH()
		{
			PopulateCURINSTR(
				// L
				IDLE,
				ROMC_01,			// forward or backward displacement
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				// S
				ROMC_00_S,          // DB <- ((PC0)); PC0++	
				IDLE,
				IDLE,
				END);
		}

		private void DONT_BRANCH()
		{
			PopulateCURINSTR(
				// S
				IDLE,
				ROMC_03_S,			// immediate operand fetch
				IDLE,
				IDLE,
				// S
				ROMC_00_S,          // DB <- ((PC0)); PC0++
				IDLE,
				IDLE,
				END);
		}
	}
}
