using System;
using System.Collections.Generic;
using System.Text;

namespace MC68000
{
	public partial class MC68K
	{
		private void EXG() // Exchange registers
		{
			this.m_Cycles += 6;

			switch ((this.m_IR >> 3) & 0x31)
			{
				case 8:
					Helpers.Swap(ref this.m_D[(this.m_IR >> 9) & 0x7], ref this.m_D[this.m_IR & 0x7]);
					return;

				case 9:
					Helpers.Swap(ref this.m_A[(this.m_IR >> 9) & 0x7], ref this.m_A[this.m_IR & 0x7]);
					return;

				case 17:
					Helpers.Swap(ref this.m_D[(this.m_IR >> 9) & 0x7], ref this.m_A[this.m_IR & 0x7]);
					return;
			}
		}

		private void LEA() // Load effective address
		{
			this.m_A[(this.m_IR >> 9) & 0x7] =
				FetchAddress((this.m_IR >> 3) & 0x7, this.m_IR & 0x7);

			switch ((this.m_IR >> 3) & 0x7)
			{
				case 0x2: this.m_Cycles += 4; break;
				case 0x5: this.m_Cycles += 8; break;
				case 0x6: this.m_Cycles += 12; break;
				case 0x7:
					switch (this.m_IR & 0x7)
					{
						case 0x0: this.m_Cycles += 8; break;
						case 0x1: this.m_Cycles += 12; break;
						case 0x2: this.m_Cycles += 8; break;
						case 0x3: this.m_Cycles += 12; break;
					}
					break;
			}
		}

		private void LINK()
		{
			this.SP -= 4;
			WriteL(this.SP, this.m_A[this.m_IR & 0x7]);
			this.m_A[this.m_IR & 0x7] = this.SP;
			this.SP += FetchW();

			this.m_Cycles += 16;
		}

		private void MOVE() // Move data from source to destination
		{
			int src_mode = (this.m_IR >> 3) & 0x7;
			int src_reg = this.m_IR & 0x7;
			int dest_mode = (this.m_IR >> 6) & 0x7;
			int dest_reg = (this.m_IR >> 9) & 0x7;

			int operand = 0;
			switch ((this.m_IR >> 12) & 0x3)
			{
				case 1: // B
					operand = FetchOperandB(src_mode, src_reg);
					SetOperandB(dest_mode, dest_reg, (sbyte)operand);
					this.m_Cycles += Helpers.MOVECyclesBW[src_mode + ((src_mode == 7) ? src_reg : 0),
						dest_mode + ((dest_mode == 7) ? dest_reg : 0)];
					break;
				case 3: // W
					operand = FetchOperandW(src_mode, src_reg);
					SetOperandW(dest_mode, dest_reg, (short)operand);
					this.m_Cycles += Helpers.MOVECyclesBW[src_mode + ((src_mode == 7) ? src_reg : 0),
						dest_mode + ((dest_mode == 7) ? dest_reg : 0)];
					break;
				case 2: // L
					operand = FetchOperandL(src_mode, src_reg);
					SetOperandL(dest_mode, dest_reg, operand);
					this.m_Cycles += Helpers.MOVECyclesL[src_mode + ((src_mode == 7) ? src_reg : 0),
						dest_mode + ((dest_mode == 7) ? dest_reg : 0)];
					break;
			}
			this.V = this.C = false;
			this.N = (operand < 0);
			this.Z = (operand == 0);
		}

		private void MOVEA() // Move Address
		{
			// W
			if ((this.m_IR >> 12 & 0x3) == 3)
			{
				this.m_A[this.m_IR >> 9 & 0x7] =
					FetchOperandW(this.m_IR >> 3 & 0x7, this.m_IR & 0x7);
				// TODO Need to check these clock cycles
				this.m_Cycles += Helpers.MOVECyclesBW[(this.m_IR >> 3 & 0x7) + (((this.m_IR >> 3 & 0x7) == 7) ? (this.m_IR & 0x7) : 0), 1];
			}
			// L
			else
			{
				this.m_A[this.m_IR >> 9 & 0x7] =
					FetchOperandL(this.m_IR >> 3 & 0x7, this.m_IR & 0x7);
				// TODO Need to check these clock cycles
				this.m_Cycles += Helpers.MOVECyclesL[(this.m_IR >> 3 & 0x7) + (((this.m_IR >> 3 & 0x7) == 7) ? (this.m_IR & 0x7) : 0), 1];
			}
		}

		private void MOVEM_Mem2Reg()
		{
			int size = (this.m_IR >> 6) & 0x1;
			int src_mode = (this.m_IR >> 3) & 0x7;
			int src_register = this.m_IR & 0x7;

			ushort regMap = (ushort)FetchW();
			int count = 0;

			int address = FetchAddress(src_mode, src_register);
			switch (size)
			{
				case 0: // W
					{
						for (int i = 0; i < 8; i++)
						{
							if ((regMap & 0x1) > 0)
							{
								this.m_D[i] = ReadW(address);
								address += 2;
								count++;
							}
							regMap = (ushort)(regMap >> 1);
						}
						for (int i = 0; i < 8; i++)
						{
							if ((regMap & 0x1) > 0)
							{
								this.m_A[i] = ReadW(address);
								address += 2;
								count++;
							}
							regMap = (ushort)(regMap >> 1);
						}
						if (src_mode == 3) // Postincrement mode
						{
							this.m_A[src_register] = address;
						}

						this.m_Cycles += count * 4;
						break;
					}
				case 1: // L
					{
						for (int i = 0; i < 8; i++)
						{
							if ((regMap & 0x1) > 0)
							{
								this.m_D[i] = (int)ReadL(address);
								address += 4;
								count++;
							}
							regMap = (ushort)(regMap >> 1);
						}
						for (int i = 0; i < 8; i++)
						{
							if ((regMap & 0x1) > 0)
							{
								this.m_A[i] = (int)ReadL(address);
								address += 4;
								count++;
							}
							regMap = (ushort)(regMap >> 1);
						}
						if (src_mode == 3) // Postincrement mode
						{
							this.m_A[src_register] = address;
						}

						this.m_Cycles += count * 8;
						break;
					}
			}

			switch (src_mode)
			{
				case 0x2: this.m_Cycles += 12; break;
				case 0x3: this.m_Cycles += 12; break;
				case 0x5: this.m_Cycles += 16; break;
				case 0x6: this.m_Cycles += 18; break;
				case 0x7:
					switch (src_register)
					{
						case 0x0: this.m_Cycles += 16; break;
						case 0x1: this.m_Cycles += 20; break;
						case 0x2: this.m_Cycles += 16; break;
						case 0x3: this.m_Cycles += 18; break;
					}
					break;
			}
		}

		private void MOVEM_Reg2Mem()
		{
			int size = (this.m_IR >> 6) & 0x1;
			int src_mode = (this.m_IR >> 3) & 0x7;
			int src_register = this.m_IR & 0x7;

			ushort regMap = (ushort)FetchW();

			int count = 0;
			int address = FetchAddress(src_mode, src_register);
			switch (size)
			{
				case 0: // W
					{
						if (src_mode == 4) // Pre-decrement mode
						{
							for (int i = 7; i >= 0; i--)
							{
								if ((regMap & 0x1) > 0)
								{
									address -= 2;
									WriteW(address, (sbyte)this.m_A[i]);
									count++;
								}
								regMap = (ushort)(regMap >> 1);
							}
							for (int i = 7; i >= 0; i--)
							{
								if ((regMap & 0x1) > 0)
								{
									address -= 2;
									WriteW(address, (sbyte)this.m_D[i]);
									count++;
								}
								regMap = (ushort)(regMap >> 1);
							}
							this.m_A[src_register] = address;
						}
						else
						{
							for (int i = 0; i < 8; i++)
							{
								if ((regMap & 0x1) > 0)
								{
									WriteW(address, (sbyte)this.m_D[i]);
									address += 2;
									count++;
								}
								regMap = (ushort)(regMap >> 1);
							}
							for (int i = 0; i < 8; i++)
							{
								if ((regMap & 0x1) > 0)
								{
									WriteW(address, (sbyte)this.m_A[i]);
									address += 2;
									count++;
								}
								regMap = (ushort)(regMap >> 1);
							}
						}
						this.m_Cycles += 4 * count;
						break;
					}
				case 1: // L
					{
						if (src_mode == 4) // Pre-decrement mode
						{
							for (int i = 7; i >= 0; i--)
							{
								if ((regMap & 0x1) > 0)
								{
									address -= 4;
									WriteL(address, this.m_A[i]);
									count++;
								}
								regMap = (ushort)(regMap >> 1);
							}
							for (int i = 7; i >= 0; i--)
							{
								if ((regMap & 0x1) > 0)
								{
									address -= 4;
									WriteL(address, this.m_D[i]);
									count++;
								}
								regMap = (ushort)(regMap >> 1);
							}
							this.m_A[src_register] = address;
						}
						else
						{
							for (int i = 0; i < 8; i++)
							{
								if ((regMap & 0x1) > 0)
								{
									WriteL(address, this.m_D[i]);
									address += 4;
									count++;
								}
								regMap = (ushort)(regMap >> 1);
							}
							for (int i = 0; i < 8; i++)
							{
								if ((regMap & 0x1) > 0)
								{
									WriteL(address, this.m_A[i]);
									address += 4;
									count++;
								}
								regMap = (ushort)(regMap >> 1);
							}
						}
						this.m_Cycles += 8 * count;
						break;
					}
			}

			switch (src_mode)
			{
				case 0x2: this.m_Cycles += 8; break;
				case 0x4: this.m_Cycles += 8; break;
				case 0x5: this.m_Cycles += 12; break;
				case 0x6: this.m_Cycles += 14; break;
				case 0x7:
					switch (src_register)
					{
						case 0x0: this.m_Cycles += 12; break;
						case 0x1: this.m_Cycles += 16; break;
					}
					break;
			}
		}

		private void MOVEP()
		{
			int dataregister = (this.m_IR >> 9) & 0x7;
			int opmode = (this.m_IR >> 6) & 0x7;
			int addressregister = this.m_IR & 0x7;
			short displacement = FetchW();

			throw new NotImplementedException();
		}

		private void MOVEQ() // Move quick
		{
			// Data byte is sign-extended to 32 bits
			int data = (sbyte)this.m_IR;

			this.N = (data < 0);
			this.Z = (data == 0);
			this.V = this.C = false;

			this.m_D[(this.m_IR >> 9) & 0x7] = data;

			this.m_Cycles += 4;
		}

		private void PEA() // Push effective address
		{
			this.SP -= 4;
			WriteL(this.SP, FetchAddress((this.m_IR >> 3) & 0x7, this.m_IR & 0x7));

			switch ((this.m_IR >> 3) & 0x7)
			{
				case 0x2: this.m_Cycles += 12; break;
				case 0x5: this.m_Cycles += 16; break;
				case 0x6: this.m_Cycles += 20; break;
				case 0x7:
					switch (this.m_IR & 0x7)
					{
						case 0x0: this.m_Cycles += 16; break;
						case 0x1: this.m_Cycles += 20; break;
						case 0x2: this.m_Cycles += 16; break;
						case 0x3: this.m_Cycles += 20; break;
					}
					break;
			}
		}

		private void UNLK()
		{
			this.SP = this.m_A[this.m_IR & 0x7];
			this.m_A[this.m_IR & 0x7] = ReadL(this.SP);
			this.SP += 4;

			this.m_Cycles += 12;
		}
	}
}
