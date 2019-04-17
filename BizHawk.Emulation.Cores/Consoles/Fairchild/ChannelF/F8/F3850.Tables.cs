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
				OP_LR8, A, Kh,		// S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void LR_A_KL()
		{
			PopulateCURINSTR(
				OP_LR8, A, Kl,    // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void LR_A_QU()
		{
			PopulateCURINSTR(
				OP_LR8, A, Qh,     // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void LR_A_QL()
		{
			PopulateCURINSTR(
				OP_LR8, A, Ql,     // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void LR_KU_A()
		{
			PopulateCURINSTR(
				OP_LR8, Kh, A,     // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void LR_KL_A()
		{
			PopulateCURINSTR(
				OP_LR8, Kl, A,     // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void LR_QU_A()
		{
			PopulateCURINSTR(
				OP_LR8, Qh, A,     // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void LR_QL_A()
		{
			PopulateCURINSTR(
				OP_LR8, Ql, A,    // S
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
				OP_LR8, Kh, DB,
				IDLE,
				IDLE,
				ROMC_0B,    // L
				IDLE,
				IDLE,
				OP_LR8, Kl, DB,
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
				OP_LR8, DB, Kh,     // L
				IDLE,
				IDLE,
				ROMC_15,
				IDLE,
				IDLE,
				OP_LR8, DB, Kl,     // L
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
				OP_LR8, A, ISAR,     // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void LR_IS_A()
		{
			PopulateCURINSTR(
				OP_LR8, ISAR, A,     // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void LR_PK()
		{
			PopulateCURINSTR(
				OP_LR8, DB, Kh,      // L
				IDLE,
				IDLE,
				ROMC_12,
				IDLE,
				IDLE,
				OP_LR8, DB, Kl,    // L
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
				OP_LR8, DB, Ql,     // L
				IDLE,
				IDLE,
				ROMC_17,
				IDLE,
				IDLE,
				OP_LR8, DB, Qh,     // L
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
				OP_LR8, Qh, DB,
				IDLE,
				IDLE,
				ROMC_09,     // L
				IDLE,
				IDLE,
				OP_LR8, Ql, DB,
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
				OP_LR8, DB, Qh,     // L
				IDLE,
				IDLE,
				ROMC_16,
				IDLE,
				IDLE,
				OP_LR8, DB, Ql,     // L
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
				OP_LR8, DB, Hh,     // L
				IDLE,
				IDLE,
				ROMC_16,
				IDLE,
				IDLE,
				OP_LR8, DB, Hl,     // L
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
				OP_LR8, Hh, DB,
				IDLE,
				IDLE,
				ROMC_09,     // L
				IDLE,
				IDLE,
				OP_LR8, Hl, DB,
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
				OP_LR8, DB, A,
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
				OP_LR8, DB, A,     // L
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
				OP_XOR8C, A, DB,  // S
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
				OP_LR8, W, J,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void LR_J_W()
		{
			PopulateCURINSTR(
				OP_LR8, J, W,      // S
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

		private void LI()
		{
			PopulateCURINSTR(
				ROMC_03_L,     // L
				IDLE,
				IDLE,
				OP_LR8, A, DB,
				IDLE,
				IDLE,
				ROMC_00_S,		// S
				IDLE,
				IDLE,
				END);
		}

		private void NI()
		{
			PopulateCURINSTR(
				ROMC_03_L,     // L
				IDLE,
				IDLE,
				OP_AND8, A, DB,
				IDLE,
				IDLE,
				ROMC_00_S,		// S
				IDLE,
				IDLE,
				END);
		}

		private void OI()
		{
			PopulateCURINSTR(
				ROMC_03_L,     // L
				IDLE,
				IDLE,
				OP_OR8, A, DB,
				IDLE,
				IDLE,
				ROMC_00_S,		// S
				IDLE,
				IDLE,
				END);
		}

		private void XI()
		{
			PopulateCURINSTR(
				ROMC_03_L,     // L
				IDLE,
				IDLE,
				OP_XOR8, A, DB,
				IDLE,
				IDLE,
				ROMC_00_S,      // S
				IDLE,
				IDLE,
				END);
		}

		private void AI()
		{
			PopulateCURINSTR(
				ROMC_03_L,     // L
				IDLE,
				IDLE,
				OP_ADD8, A, DB,
				IDLE,
				IDLE,
				ROMC_00_S,      // S
				IDLE,
				IDLE,
				END);
		}

		private void CI()
		{
			PopulateCURINSTR(
				ROMC_03_L,     // L
				IDLE,
				IDLE,
				OP_CI, A,
				IDLE,
				IDLE,
				ROMC_00_S,      // S
				IDLE,
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

		private void IN()
		{
			PopulateCURINSTR(
				ROMC_03_L,     // L
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				ROMC_1B,     // L
				IDLE,
				IDLE,
				OP_LR8_IO, A, DB,
				IDLE,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void OUT()
		{
			PopulateCURINSTR(
				ROMC_03_L,     // L
				IDLE,
				IDLE,
				OP_LR8, DB, A,
				IDLE,
				IDLE,
				ROMC_1A,     // L
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void PI()
		{
			PopulateCURINSTR(
				ROMC_03_L,     // L
				IDLE,
				IDLE,
				OP_LR8, A, DB,
				IDLE,
				IDLE,
				ROMC_0D,     // S
				IDLE,
				IDLE,
				IDLE,
				ROMC_0C,     // L
				IDLE,
				IDLE,
				OP_LR8, DB, A,
				IDLE,
				IDLE,
				ROMC_14,     // L
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void JMP()
		{
			PopulateCURINSTR(
				ROMC_03_L,     // L
				IDLE,
				IDLE,
				OP_LR8, A, DB,
				IDLE,
				IDLE,
				ROMC_0C,     // L
				IDLE,
				IDLE,
				OP_LR8, DB, A,
				IDLE,
				IDLE,
				ROMC_14,    // L
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void DCI()
		{
			PopulateCURINSTR(
				ROMC_11,    // L
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				ROMC_03_S,     // S
				IDLE,
				IDLE,
				IDLE,
				ROMC_0E,		// L
				IDLE,
				IDLE,  
				IDLE,
				IDLE,
				IDLE,
				ROMC_03_S,     // S
				IDLE,
				IDLE,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void NOP()
		{
			PopulateCURINSTR(
				ROMC_00_S,     // S
				IDLE,
				IDLE,
				END);
		}

		private void XDC()
		{
			PopulateCURINSTR(
				ROMC_1D,     // S
				IDLE,
				IDLE,
				IDLE,
				ROMC_00_S,     // S
				IDLE,
				IDLE,
				END);
		}

		private void DS(ushort rIndex)
		{
			PopulateCURINSTR(
				OP_LR8, rIndex, BYTE, // L
				IDLE,
				ROMC_00_L,
				IDLE,
				IDLE,
				END);
		}
		private void DS_ISAR()
		{
			PopulateCURINSTR(
				OP_DS_IS, // L
				IDLE,
				ROMC_00_L,
				IDLE,
				IDLE,
				END);
		}

		private void DS_ISAR_INC()
		{
			PopulateCURINSTR(
				OP_DS_IS, // L
				OP_IS_INC,
				ROMC_00_L,
				IDLE,
				IDLE,
				END);
		}

		private void DS_ISAR_DEC()
		{
			PopulateCURINSTR(
				OP_DS_IS, // L
				OP_IS_DEC,
				ROMC_00_L,
				IDLE,
				IDLE,
				END);
		}

		private void LR_A_R(ushort rIndex)
		{
			PopulateCURINSTR(
				OP_LR8, A, rIndex,  // S
				ROMC_00_S,     
				IDLE,
				END);
		}

		private void LR_A_ISAR()
		{
			PopulateCURINSTR(
				OP_LR_A_IS, A,  // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void LR_A_ISAR_INC()
		{
			PopulateCURINSTR(
				OP_LR_A_IS, A,  // S
				OP_IS_INC,
				ROMC_00_S,
				END);
		}

		private void LR_A_ISAR_DEC()
		{
			PopulateCURINSTR(
				OP_LR_A_IS, A,  // S
				OP_IS_DEC,
				ROMC_00_S,
				END);
		}

		private void LR_R_A(ushort rIndex)
		{
			PopulateCURINSTR(
				OP_LR8, rIndex, A,  // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void LR_ISAR_A()
		{
			PopulateCURINSTR(
				OP_LR_IS_A, A,  // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void LR_ISAR_A_INC()
		{
			PopulateCURINSTR(
				OP_LR_IS_A, A,  // S
				OP_IS_INC,
				ROMC_00_S,
				END);
		}

		private void LR_ISAR_A_DEC()
		{
			PopulateCURINSTR(
				OP_LR_IS_A, A,  // S
				OP_IS_DEC,
				ROMC_00_S,
				END);
		}

		private void LISU(ushort octal)
		{
			PopulateCURINSTR(
				OP_LISU, octal,  // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void LISL(ushort octal)
		{
			PopulateCURINSTR(
				OP_LISL, octal,  // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void LIS(ushort index)
		{
			PopulateCURINSTR(
				OP_LR8, A, index,  // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void BT(ushort index)
		{
			PopulateCURINSTR(
				ROMC_1C_S,  // S
				IDLE,
				IDLE,
				OP_BT, index);	// no END as there is branching logic within OP_BT
		}

		private void AM()
		{
			PopulateCURINSTR(
				ROMC_02,  // L
				IDLE,
				IDLE,
				OP_ADD8, A, DB,
				IDLE,
				IDLE,
				ROMC_00_S,	// S
				IDLE,
				IDLE,
				END);
		}

		private void AMD()
		{
			PopulateCURINSTR(
				ROMC_02,  // L
				IDLE,
				IDLE,
				OP_ADD8D, A, DB,
				IDLE,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void NM()
		{
			PopulateCURINSTR(
				ROMC_02,  // L
				IDLE,
				IDLE,
				OP_AND8, A, DB,
				IDLE,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void OM()
		{
			PopulateCURINSTR(
				ROMC_02,  // L
				IDLE,
				IDLE,
				OP_OR8, A, DB,
				IDLE,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void XM()
		{
			PopulateCURINSTR(
				ROMC_02,  // L
				IDLE,
				IDLE,
				OP_XOR8, A, DB,
				IDLE,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void CM()
		{
			PopulateCURINSTR(
				ROMC_02,  // L
				IDLE,
				IDLE,
				OP_CM,
				IDLE,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void ADC()
		{
			PopulateCURINSTR(
				OP_LR8, DB, A,  // L
				IDLE,
				IDLE,
				ROMC_0A,
				IDLE,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void BR7()
		{
			PopulateCURINSTR(
				OP_BR7);  // no END as there is branching logic within OP_BR7
		}

		private void BF(ushort index)
		{
			PopulateCURINSTR(
				ROMC_1C_S,  // S
				IDLE,
				IDLE,
				OP_BF, index);  // no END as there is branching logic within OP_BF
		}

		private void INS_0(ushort index)
		{
			PopulateCURINSTR(
				ROMC_1C_S,  // S
				IDLE,
				OP_IN, A, index,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void INS_1(ushort index)
		{
			PopulateCURINSTR(
				ROMC_1C_L,  // L
				IDLE,
				IDLE,
				OP_LR8, IO, index,
				IDLE,
				IDLE,
				ROMC_1B,  // L
				IDLE,
				IDLE,
				OP_LR8_IO, A, DB,
				IDLE,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void OUTS_0(ushort index)
		{
			PopulateCURINSTR(
				ROMC_1C_S,  // S
				IDLE,
				OP_OUT, index, A,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void OUTS_1(ushort index)
		{
			PopulateCURINSTR(
				ROMC_1C_L,  // L
				IDLE,
				IDLE,
				OP_LR8, IO, index,
				OP_LR8, DB, A,
				IDLE,
				ROMC_1A,  // L
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				ROMC_00_S,  // S
				IDLE,
				IDLE,
				END);
		}

		private void AS(ushort rIndex)
		{
			PopulateCURINSTR(
				OP_ADD8, A, rIndex,  // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void AS_IS()
		{
			PopulateCURINSTR(
				OP_AS_IS,  // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void AS_IS_INC()
		{
			PopulateCURINSTR(
				OP_AS_IS,  // S
				OP_IS_INC,
				ROMC_00_S,
				END);
		}

		private void AS_IS_DEC()
		{
			PopulateCURINSTR(
				OP_AS_IS,  // S
				OP_IS_DEC,
				ROMC_00_S,
				END);
		}

		private void ASD(ushort rIndex)
		{
			PopulateCURINSTR(
				ROMC_1C_S,  // S
				IDLE,
				OP_ADD8D, A, rIndex,
				IDLE,
				ROMC_00_S,	// S
				IDLE,
				IDLE,
				END);
		}

		private void ASD_IS()
		{
			PopulateCURINSTR(
				ROMC_1C_S,  // S
				IDLE,
				OP_ASD_IS,
				IDLE,
				ROMC_00_S,	// S
				IDLE,
				IDLE,
				END);
		}

		private void ASD_IS_INC()
		{
			PopulateCURINSTR(
				ROMC_1C_S,  // S
				IDLE,
				OP_ASD_IS,
				OP_IS_INC,
				ROMC_00_S,		// S
				IDLE,
				IDLE,
				END);
		}

		private void ASD_IS_DEC()
		{
			PopulateCURINSTR(
				ROMC_1C_S,  // S
				IDLE,
				OP_ASD_IS,
				OP_IS_DEC,
				ROMC_00_S,      // S
				IDLE,
				IDLE,
				END);
		}

		private void XS(ushort rIndex)
		{
			PopulateCURINSTR(
				OP_XOR8, A, rIndex,  // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void XS_IS()
		{
			PopulateCURINSTR(
				OP_XS_IS,  // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void XS_IS_INC()
		{
			PopulateCURINSTR(
				OP_XS_IS,  // S
				OP_IS_INC,
				ROMC_00_S,
				END);
		}

		private void XS_IS_DEC()
		{
			PopulateCURINSTR(
				OP_XS_IS,  // S
				OP_IS_DEC,
				ROMC_00_S,
				END);
		}

		private void NS(ushort rIndex)
		{
			PopulateCURINSTR(
				OP_AND8, A, rIndex,  // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void NS_IS()
		{
			PopulateCURINSTR(
				OP_NS_IS,  // S
				ROMC_00_S,
				IDLE,
				END);
		}

		private void NS_IS_INC()
		{
			PopulateCURINSTR(
				OP_NS_IS,  // S
				OP_IS_INC,
				ROMC_00_S,
				END);
		}

		private void NS_IS_DEC()
		{
			PopulateCURINSTR(
				OP_NS_IS,  // S
				OP_IS_DEC,
				ROMC_00_S,
				END);
		}
	}
}
