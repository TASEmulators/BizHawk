namespace BizHawk.Emulation.Cores.Components.MC6800
{
	public partial class MC6800
	{
		public const ushort I_NEG = 0;
		public const ushort I_COM = 1;
		public const ushort I_LSR = 2;
		public const ushort I_ROR = 3;
		public const ushort I_ASR = 4;
		public const ushort I_ASL = 5;
		public const ushort I_ROL = 6;
		public const ushort I_DEC = 7;
		public const ushort I_INC = 8;
		public const ushort I_TST = 9;
		public const ushort I_JMP = 10;
		public const ushort I_CLR = 11;
		public const ushort I_SUB = 12;
		public const ushort I_CMP = 13;
		public const ushort I_SBC = 14;
		public const ushort I_AND = 15;
		public const ushort I_BIT = 16;
		public const ushort I_LD = 17;
		public const ushort I_ST = 18;
		public const ushort I_XOR = 19;
		public const ushort I_ADC = 20;
		public const ushort I_OR = 21;
		public const ushort I_ADD = 22;
		public const ushort I_CMP16 = 23;
		public const ushort I_JSR = 24;
		public const ushort I_LD16 = 25;
		public const ushort I_ST16 = 26;

		public ushort indexed_op;
		public ushort indexed_reg;
		public ushort indexed_op_reg;

		private void INDEX_OP(ushort oper)
		{
			indexed_op = oper;

			PopulateCURINSTR(RD_INC_OP, ALU, PC, IDX_DCDE);

			IRQS = -1;
		}

		private void INDEX_OP_REG(ushort oper, ushort src)
		{
			indexed_op = oper;
			indexed_op_reg = src;

			PopulateCURINSTR(RD_INC_OP, ALU, PC, IDX_DCDE);

			IRQS = -1;
		}

		private void INDEX_OP_JMP()
		{
			PopulateCURINSTR(TR, PC, IDX_EA);

			IRQS = 1;
		}

		private void INDEX_OP_JSR()
		{
			PopulateCURINSTR(TR, ADDR, PC,
							IDLE,
							IDLE,
							TR, PC, IDX_EA,
							WR_DEC_LO, SP, ADDR,
							WR_DEC_HI, SP, ADDR);

			IRQS = 6;
		}

		private void INDEX_OP_LD()
		{
			PopulateCURINSTR(IDLE,
							RD_INC, ALU, IDX_EA,
							RD_INC_OP, ALU2, IDX_EA, LD_16, indexed_op_reg, ALU, ALU2);

			IRQS = 3;
		}

		private void INDEX_OP_ST()
		{
			PopulateCURINSTR(IDLE,
							WR_HI_INC, IDX_EA, indexed_op_reg,
							WR_DEC_LO, IDX_EA, indexed_op_reg);

			IRQS = 3;
		}

		private void INDEX_OP_LDD()
		{
			PopulateCURINSTR(IDLE,
							RD_INC, A, IDX_EA,
							RD_INC_OP, B, IDX_EA, LD_16, ADDR, A, B);

			IRQS = 3;
		}

		private void INDEX_OP_STD()
		{
			PopulateCURINSTR(SET_ADDR, ADDR, A, A,
							WR_HI_INC, IDX_EA, ADDR,
							WR_DEC_LO, IDX_EA, B);

			IRQS = 3;
		}

		private void INDEX_OP_EX4(ushort oper)
		{
			PopulateCURINSTR(IDLE,
							RD_INC_OP, ALU, IDX_EA, oper, indexed_op_reg, ALU);

			IRQS = 2;
		}

		private void INDEX_OP_EX4_ST()
		{
			PopulateCURINSTR(IDLE,
							WR, IDX_EA, indexed_op_reg);

			IRQS = 2;
		}

		private void INDEX_OP_EX6(ushort oper)
		{
			PopulateCURINSTR(IDLE,
							RD, ALU, IDX_EA,
							oper, ALU,
							WR, IDX_EA, ALU);

			IRQS = 4;
		}

		private void INDEX_OP_EX6D(ushort oper)
		{
			PopulateCURINSTR(IDLE,
							RD_INC, ALU, IDX_EA,
							RD_INC_OP, ALU2, IDX_EA, SET_ADDR, ADDR, ALU, ALU2,
							oper, ADDR);

			IRQS = 4;
		}

		private void INDEX_CMP_EX6(ushort oper)
		{
			PopulateCURINSTR(IDLE,
							RD_INC, ALU, IDX_EA,
							RD_INC_OP, ALU2, IDX_EA, SET_ADDR, ADDR, ALU, ALU2,
							oper, indexed_op_reg, ADDR);

			IRQS = 4;
		}

		// ALU holds the post byte
		public void Index_decode()
		{
			Regs[IDX_EA] = (ushort)(Regs[X] + Regs[ALU]);

			PopulateCURINSTR(IDX_OP_BLD);

			instr_pntr = 0;
			irq_pntr = 100;
		}

		public void Index_Op_Builder()
		{
			switch(indexed_op)
			{
				case I_NEG: INDEX_OP_EX6(NEG);					break; // NEG
				case I_COM: INDEX_OP_EX6(COM);					break; // COM
				case I_LSR: INDEX_OP_EX6(LSR);					break; // LSR
				case I_ROR: INDEX_OP_EX6(ROR);					break; // ROR
				case I_ASR: INDEX_OP_EX6(ASR);					break; // ASR
				case I_ASL: INDEX_OP_EX6(ASL);					break; // ASL
				case I_ROL: INDEX_OP_EX6(ROL);					break; // ROL
				case I_DEC: INDEX_OP_EX6(DEC8);					break; // DEC
				case I_INC: INDEX_OP_EX6(INC8);					break; // INC
				case I_TST: INDEX_OP_EX6(TST);					break; // TST
				case I_JMP: INDEX_OP_JMP();						break; // JMP
				case I_CLR: INDEX_OP_EX6(CLR);					break; // CLR
				case I_SUB: INDEX_OP_EX4(SUB8);					break; // SUB A,B
				case I_CMP: INDEX_OP_EX4(CMP8);					break; // CMP A,B
				case I_SBC: INDEX_OP_EX4(SBC8);					break; // SBC A,B
				case I_AND: INDEX_OP_EX4(AND8);					break; // AND A,B
				case I_BIT: INDEX_OP_EX4(BIT);					break; // BIT A,B
				case I_LD: INDEX_OP_EX4(LD_8);					break; // LD A,B
				case I_ST: INDEX_OP_EX4_ST();					break; // ST A,B
				case I_XOR: INDEX_OP_EX4(XOR8);					break; // XOR A,B
				case I_ADC: INDEX_OP_EX4(ADC8);					break; // ADC A,B
				case I_OR: INDEX_OP_EX4(OR8);					break; // OR A,B
				case I_ADD: INDEX_OP_EX4(ADD8);					break; // ADD A,B
				case I_CMP16: INDEX_CMP_EX6(CMP16);				break; // CMP X, SP
				case I_JSR: INDEX_OP_JSR();						break; // JSR
				case I_LD16: INDEX_OP_LD();						break; // LD X, SP
				case I_ST16: INDEX_OP_ST();						break; // ST X, SP
			}

			instr_pntr = 0;
			irq_pntr = -1;
		}
	}
}
