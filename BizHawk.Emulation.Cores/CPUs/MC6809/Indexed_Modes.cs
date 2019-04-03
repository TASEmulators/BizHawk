using System;

namespace BizHawk.Emulation.Common.Components.MC6809
{
	public partial class MC6809
	{
		public const ushort LEAX = 0;
		public const ushort LEAY = 1;
		public const ushort LEAS = 2;
		public const ushort LEAU = 3;
		public const ushort I_NEG = 4;
		public const ushort I_COM = 5;
		public const ushort I_LSR = 6;
		public const ushort I_ROR = 7;
		public const ushort I_ASR = 8;
		public const ushort I_ASL = 9;
		public const ushort I_ROL = 10;
		public const ushort I_DEC = 11;
		public const ushort I_INC = 12;
		public const ushort I_TST = 13;
		public const ushort I_JMP = 14;
		public const ushort I_CLR = 15;

		public ushort indexed_op;
		public ushort indexed_reg;

		public ushort temp;

		private void INDEX_OP(ushort oper)
		{
			indexed_op = oper;

			PopulateCURINSTR(RD_INC_OP, ALU, PC, IDX_DCDE);
		}

		private void INDEX_OP_JMP()
		{
			PopulateCURINSTR(TR, PC, IDX_EA);
		}

		private void INDEX_OP_LEA(ushort dest)
		{
			PopulateCURINSTR(TR, dest, IDX_EA,
							IDLE);
		}

		private void INDEX_OP_EX5()
		{
			PopulateCURINSTR(RD_INC_OP, ALU, PC, IDX_DCDE);
		}

		private void INDEX_OP_EX6(ushort oper)
		{
			PopulateCURINSTR(IDLE,
							RD, ALU, IDX_EA,
							oper, ALU,
							WR, IDX_EA, ALU);
		}

		private void INDEX_OP_EX7()
		{
			PopulateCURINSTR(RD_INC_OP, ALU, PC, IDX_DCDE);
		}


		// ALU holds the post byte
		public void Index_decode()
		{
			switch ((Regs[ALU] >> 5) & 3)
			{
				case 0: indexed_reg = X; break;
				case 1: indexed_reg = Y; break;
				case 2: indexed_reg = US; break;
				case 3: indexed_reg = SP; break;
			}

			if ((Regs[ALU] & 0x80) == 0)
			{
				temp = (ushort)(Regs[ALU] & 0x1F);
				if ((Regs[ALU] & 0x10) == 0x10)
				{
					temp |= 0xFFE0;
				}

				Regs[IDX_EA] = (ushort)(Regs[indexed_reg] + temp);

				PopulateCURINSTR(IDX_OP_BLD);
			}
			else
			{
				if ((Regs[ALU] & 0x10) == 0x10)
				{
					switch (Regs[ALU] & 0xF)
					{
						case 0x0:
							// Illegal
							break;
						case 0x1:
							Regs[ADDR] = Regs[indexed_reg];
							PopulateCURINSTR(INC16, indexed_reg,
											INC16, indexed_reg,
											RD_INC, ALU, ADDR,
											RD_INC, ALU2, ADDR,
											SET_ADDR, IDX_EA, ALU, ALU2,
											IDX_OP_BLD);
							break;
						case 0x2:
							// Illegal
							break;
						case 0x3:
							Regs[ADDR] = (ushort)(Regs[indexed_reg] - 2);
							PopulateCURINSTR(DEC16, indexed_reg,
											DEC16, indexed_reg,
											RD_INC, ALU, ADDR,
											RD_INC, ALU2, ADDR,
											SET_ADDR, IDX_EA, ALU, ALU2,
											IDX_OP_BLD);
							break;
						case 0x4:
							Regs[ADDR] = Regs[indexed_reg];
							PopulateCURINSTR(RD_INC, ALU, ADDR,
											RD_INC_OP, ALU2, ADDR, SET_ADDR, IDX_EA, ALU, ALU2,
											IDX_OP_BLD);
							break;
						case 0x5:
							Regs[ADDR]  = (ushort)(Regs[indexed_reg] + (((Regs[B] & 0x80) == 0x80) ? (Regs[B] | 0xFF00) : Regs[B]));
							PopulateCURINSTR(RD_INC, ALU, ADDR,
											RD_INC, ALU2, ADDR, 
											SET_ADDR, IDX_EA, ALU, ALU2,
											IDX_OP_BLD);
							break;
						case 0x6:
							Regs[ADDR] = (ushort)(Regs[indexed_reg] + (((Regs[A] & 0x80) == 0x80) ? (Regs[A] | 0xFF00) : Regs[A]));
							PopulateCURINSTR(RD_INC, ALU, ADDR,
											RD_INC, ALU2, ADDR,
											SET_ADDR, IDX_EA, ALU, ALU2,
											IDX_OP_BLD);
							break;
						case 0x7:
							// Illegal
							break;
						case 0x8:
							Regs[ADDR] = Regs[indexed_reg];
							PopulateCURINSTR(RD_INC_OP, ALU2, PC, ADD8BR, ADDR, ALU2,
											RD_INC, ALU, ADDR,
											RD_INC_OP, ALU2, ADDR, SET_ADDR, IDX_EA, ALU, ALU2,
											IDX_OP_BLD);
							break;
						case 0x9:
							Regs[ADDR] = Regs[indexed_reg];
							PopulateCURINSTR(RD_INC, ALU, PC,
											RD_INC, ALU2, PC,
											SET_ADDR, IDX_EA, ALU, ALU2,
											ADD16BR, ADDR, IDX_EA,
											RD_INC, ALU, ADDR,
											RD_INC_OP, ALU2, ADDR, SET_ADDR, IDX_EA, ALU, ALU2,
											IDX_OP_BLD);
							break;
						case 0xA:
							// Illegal
							break;
						case 0xB:
							Regs[ADDR] = Regs[indexed_reg];
							PopulateCURINSTR(IDLE,
											IDLE,
											SET_ADDR, IDX_EA, A, B,
											ADD16BR, ADDR, IDX_EA,
											RD_INC, ALU, ADDR,
											RD_INC_OP, ALU2, ADDR, SET_ADDR, IDX_EA, ALU, ALU2,
											IDX_OP_BLD);
							break;
						case 0xC:
							indexed_reg = PC;
							Regs[ADDR] = Regs[indexed_reg];
							PopulateCURINSTR(RD_INC_OP, ALU2, PC, ADD8BR, ADDR, ALU2,
											RD_INC, ALU, ADDR,
											RD_INC_OP, ALU2, ADDR, SET_ADDR, IDX_EA, ALU, ALU2,
											IDX_OP_BLD);
							break;
						case 0xD:
							indexed_reg = PC;
							Regs[ADDR] = Regs[indexed_reg];
							PopulateCURINSTR(IDLE,
											RD_INC, ALU, PC,
											RD_INC, ALU2, PC,
											SET_ADDR, IDX_EA, ALU, ALU2,
											ADD16BR, ADDR, IDX_EA,
											RD_INC, ALU, ADDR,
											RD_INC_OP, ALU2, ADDR, SET_ADDR, IDX_EA, ALU, ALU2,
											IDX_OP_BLD);
							break;
						case 0xE:
							// Illegal
							break;
						case 0xF:
							if ((Regs[ALU] >> 5) == 0)
							{
								PopulateCURINSTR(RD_INC, ALU, PC,
												RD_INC_OP, ALU2, PC, SET_ADDR, ADDR, ALU, ALU2,
												RD_INC, ALU, ADDR,
												RD_INC_OP, ALU2, ADDR, SET_ADDR, IDX_EA, ALU, ALU2,
												IDX_OP_BLD);
							}
							else 
							{
								// illegal
							}
							break;
					}
				}
				else
				{
					switch (Regs[ALU] & 0xF)
					{
						case 0x0:
							Regs[IDX_EA] = Regs[indexed_reg];
							PopulateCURINSTR(INC16, indexed_reg,
											IDX_OP_BLD);
							break;
						case 0x1:
							Regs[IDX_EA] = Regs[indexed_reg];
							PopulateCURINSTR(INC16, indexed_reg,
											INC16, indexed_reg,
											IDX_OP_BLD);
							break;
						case 0x2:
							Regs[IDX_EA] = (ushort)(Regs[indexed_reg] - 1);
							PopulateCURINSTR(DEC16, indexed_reg,
											IDX_OP_BLD);
							break;
						case 0x3:
							Regs[IDX_EA] = (ushort)(Regs[indexed_reg] - 2);
							PopulateCURINSTR(DEC16, indexed_reg,
											DEC16, indexed_reg,
											IDX_OP_BLD);
							break;
						case 0x4:
							Regs[IDX_EA] = Regs[indexed_reg];
							Index_Op_Builder();
							break;
						case 0x5:
							Regs[IDX_EA] = (ushort)(Regs[indexed_reg] + (((Regs[B] & 0x80) == 0x80) ? (Regs[B] | 0xFF00) : Regs[B]));
							PopulateCURINSTR(IDX_OP_BLD);
							break;
						case 0x6:
							Regs[IDX_EA] = (ushort)(Regs[indexed_reg] + (((Regs[A] & 0x80) == 0x80) ? (Regs[A] | 0xFF00) : Regs[A]));
							PopulateCURINSTR(IDX_OP_BLD);
							break;
						case 0x7:
							// Illegal
							break;
						case 0x8:
							PopulateCURINSTR(RD_INC_OP, ALU2, PC, EA_8);
							break;
						case 0x9:
							PopulateCURINSTR(RD_INC, ALU, PC,
											RD_INC, ALU2, PC,
											SET_ADDR, ADDR, ALU, ALU2,
											EA_16);
							break;
						case 0xA:
							// Illegal
							break;
						case 0xB:
							PopulateCURINSTR(IDLE,
											IDLE,
											SET_ADDR, ADDR, A, B,
											EA_16);
							break;
						case 0xC:
							indexed_reg = PC;
							PopulateCURINSTR(RD_INC_OP, ALU2, PC, EA_8);
							break;
						case 0xD:
							indexed_reg = PC;
							PopulateCURINSTR(IDLE,
											RD_INC, ALU, PC,
											RD_INC, ALU2, PC,
											SET_ADDR, ADDR, ALU, ALU2,
											EA_16);
							break;
						case 0xE:
							// Illegal
							break;
						case 0xF:
							// Illegal
							break;
					}
				}			
			}
		}

		public void Index_Op_Builder()
		{
			switch(indexed_op)
			{
				case LEAX: INDEX_OP_LEA(X);						break; // LEAX
				case LEAY: INDEX_OP_LEA(Y);						break; // LEAY
				case LEAS: INDEX_OP_LEA(SP);					break; // LEAS
				case LEAU: INDEX_OP_LEA(US);					break; // LEAU
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
			}
		}
	}
}
