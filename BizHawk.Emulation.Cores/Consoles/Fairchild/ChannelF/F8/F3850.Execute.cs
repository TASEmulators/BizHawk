using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public sealed partial class F3850
	{
		public const int MaxInstructionLength = 40;

		public long TotalExecutedCycles;

		public int instr_pntr = 0;
		public ushort[] cur_instr = new ushort[MaxInstructionLength];		// fixed size - do not change at runtime
		public ushort[] cur_romc = new ushort[MaxInstructionLength];        // fixed size - do not change at runtime
		public byte opcode;
		public byte databus;
		public ushort iobus;

		public void FetchInstruction()
		{
			switch (opcode)
			{
				case 0x00: LR_A_KU(); break;            // LR A, (KU) 
				case 0x01: LR_A_KL(); break;            // LR A, (KL) 
				case 0x02: LR_A_QU(); break;            // LR A, (QU) 
				case 0x03: LR_A_QL(); break;            // LR A, (QL) 
				case 0x04: LR_KU_A(); break;            // LR KU, (A) 
				case 0x05: LR_KL_A(); break;            // LR KL, (A) 
				case 0x06: LR_QU_A(); break;            // LR QU, (A) 
				case 0x07: LR_QL_A(); break;            // LR QL, (A) 
				case 0x08: LR_K_P(); break;             // LR K, (P) 
				case 0x09: LR_P_K(); break;             // LR P, (K) 
				case 0x0A: LR_A_IS(); break;            // LR A, (ISAR) 
				case 0x0B: LR_IS_A(); break;            // LR ISAR, (A) 
				case 0x0C: LR_PK(); break;              // LR PC1, (PC0); LR PC0l <- (r13); LR PC0h, (r12)
				case 0x0D: LR_P0_Q(); break;            // LR PC0l, (r15); LR PC0h <- (r14)
				case 0x0E: LR_Q_DC(); break;            // LR r14, (DC0h); r15 <- (DC0l)
				case 0x0F: LR_DC_Q(); break;            // LR DC0h, (r14); DC0l <- (r15)
				case 0x10: LR_DC_Q(); break;            // LR DC0h, (r10); DC0l <- (r11)
				case 0x11: LR_H_DC(); break;            // LR r10, (DC0h); r11 <- (DC0l)
				case 0x12: SHIFT_R(1); break;            // Shift (A) right one bit position (zero fill)
				case 0x13: SHIFT_L(1); break;            // Shift (A) left one bit position (zero fill)
				case 0x14: SHIFT_R(4); break;            // Shift (A) right four bit positions (zero fill)
				case 0x15: SHIFT_L(4); break;            // Shift (A) left four bit positions (zero fill)
				case 0x16: LM(); break;                 // A <- ((DC0))
				case 0x17: ST(); break;                 // (DC) <- (A)
				case 0x18: COM(); break;               // A <- A ^ 255
				case 0x1A: DI(); break;                // Clear ICB
				case 0x1B: EI(); break;                // Set ICB
				case 0x1C: POP(); break;                // PC0 <- PC1
				case 0x1D: LR_W_J(); break;             // W <- (r9)
				case 0x1E: LR_J_W(); break;             // r9 <- (W)
				case 0x1F: INC(); break;				// A <- (A) + 1

				default: ILLEGAL(); break;				// Illegal Opcode
			}
		}
		

		
	}
}
