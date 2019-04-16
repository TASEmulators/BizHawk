using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BizHawk.Common;
using BizHawk.Emulation.Common;

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
				OP_LR_8, A, SR12,		// S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void LR_A_KL()
		{
			PopulateCURINSTR(
				OP_LR_8, A, SR13,    // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void LR_A_QU()
		{
			PopulateCURINSTR(
				OP_LR_8, A, SR14,     // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void LR_A_QL()
		{
			PopulateCURINSTR(
				OP_LR_8, A, SR15,     // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void LR_KU_A()
		{
			PopulateCURINSTR(
				OP_LR_8, SR12, A,     // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void LR_KL_A()
		{
			PopulateCURINSTR(
				OP_LR_8, SR13, A,     // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void LR_QU_A()
		{
			PopulateCURINSTR(
				OP_LR_8, SR14, A,     // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void LR_QL_A()
		{
			PopulateCURINSTR(
				OP_LR_8, SR15, A,    // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void LR_K_P()
		{
			PopulateCURINSTR(
				ROMC_07,     // L
				IDLE,
				IDLE,
				OP_LR_8, SR12, DB,
				IDLE,
				IDLE,
				ROMC_0B,    // L
				IDLE,
				IDLE,
				OP_LR_8, SR13, DB,
				IDLE,
				IDLE,
				ROMC_00_S,	// S
				IDLE,
				IDLE,
				END);
		}

		private void LR_P_K()
		{
			PopulateCURINSTR(
				OP_LR_8, DB, SR12,     // L
				IDLE,
				IDLE,
				ROMC_15,
				IDLE,
				IDLE,
				OP_LR_8, DB, SR13,     // L
				IDLE,
				IDLE,
				ROMC_18,
				IDLE,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void LR_A_IS()
		{
			PopulateCURINSTR(
				OP_LR_8, A, ISAR,     // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void LR_IS_A()
		{
			PopulateCURINSTR(
				OP_LR_8, ISAR, A,     // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void LR_PK()
		{
			PopulateCURINSTR(
				OP_LR_8, DB, SR13,      // L
				IDLE,
				IDLE,
				ROMC_12,
				IDLE,
				IDLE,
				OP_LR_8, DB, SR12,    // L
				IDLE,
				IDLE,
				ROMC_14,
				IDLE,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void LR_P0_Q()
		{
			PopulateCURINSTR(
				OP_LR_8, DB, SR15,     // L
				IDLE,
				IDLE,
				ROMC_17,
				IDLE,
				IDLE,
				OP_LR_8, DB, SR14,     // L
				IDLE,
				IDLE,
				ROMC_14,
				IDLE,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void LR_Q_DC()
		{
			PopulateCURINSTR(
				ROMC_06,     // L
				IDLE,
				IDLE,
				OP_LR_8, SR14, DB,
				IDLE,
				IDLE,
				ROMC_09,     // L
				IDLE,
				IDLE,
				OP_LR_8, SR15, DB,
				IDLE,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void LR_DC_Q()
		{
			PopulateCURINSTR(
				OP_LR_8, DB, SR14,     // L
				IDLE,
				IDLE,
				ROMC_16,
				IDLE,
				IDLE,
				OP_LR_8, DB, SR15,     // L
				IDLE,
				IDLE,
				ROMC_19,
				IDLE,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void LR_DC_H()
		{
			PopulateCURINSTR(
				OP_LR_8, DB, SR10,     // L
				IDLE,
				IDLE,
				ROMC_16,
				IDLE,
				IDLE,
				OP_LR_8, DB, SR11,     // L
				IDLE,
				IDLE,
				ROMC_19,
				IDLE,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void LR_H_DC()
		{
			PopulateCURINSTR(
				ROMC_06,     // L
				IDLE,
				IDLE,
				OP_LR_8, SR10, DB,
				IDLE,
				IDLE,
				ROMC_09,     // L
				IDLE,
				IDLE,
				OP_LR_8, SR11, DB,
				IDLE,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void SHIFT_R(ushort index)
		{
			PopulateCURINSTR(
				OP_SHFT_R, A, index,  // S
				ROMC_00_S,  
				IDLE,
				END);
		}

		private void SHIFT_L(ushort index)
		{
			PopulateCURINSTR(
				OP_SHFT_L, A, index,  // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void LM()
		{
			PopulateCURINSTR(
				ROMC_02,     // L
				IDLE,
				IDLE,
				OP_LR_8, DB, A,
				IDLE,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void ST()
		{
			PopulateCURINSTR(
				OP_LR_8, DB, A,     // L
				IDLE,
				IDLE,
				ROMC_05,
				IDLE,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void COM()
		{
			PopulateCURINSTR(
				OP_COM,  // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void LNK()
		{
			PopulateCURINSTR(
				OP_LNK,  // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void DI()
		{
			PopulateCURINSTR(
				ROMC_1C_S,  // S
				OP_DI,
				IDLE,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void EI()
		{
			PopulateCURINSTR(
				ROMC_1C_S,  // S
				OP_EI,
				IDLE,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void POP()
		{
			PopulateCURINSTR(
				ROMC_04,  // S
				IDLE,
				IDLE,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void LR_W_J()
		{
			PopulateCURINSTR(
				ROMC_1C_S,  // S
				IDLE,
				OP_LR_8, W, SR9,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void LR_J_W()
		{
			PopulateCURINSTR(
				OP_LR_8, SR9, W,      // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void INC()
		{
			PopulateCURINSTR(
				OP_INC8, A,     // S
				ROMC_00_S,
				IDLE,
				END);
		}


		private void ILLEGAL()
		{
			PopulateCURINSTR(
				ROMC_00_S,     // S
				IDLE,
				IDLE,
				END);
		}
	}
}
