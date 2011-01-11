using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MC68000
{
	public partial class MC68K
	{
		#region Read
		private sbyte ReadB(int address)
		{
			return this.m_Controller.ReadB(address &= 0x00FFFFFF);
		}

		private short ReadW(int address)
		{
			return this.m_Controller.ReadW(address &= 0x00FFFFFF);
		}

		private int ReadL(int address)
		{
			return this.m_Controller.ReadL(address &= 0x00FFFFFF);
		}
		#endregion Read

		#region Write
		private void WriteB(int address, sbyte value)
		{
			this.m_Controller.WriteB(address &= 0x00FFFFFF, value);
		}

		private void WriteW(int address, short value)
		{
			this.m_Controller.WriteW(address &= 0x00FFFFFF, value);
		}

		private void WriteL(int address, int value)
		{
			this.m_Controller.WriteL(address &= 0x00FFFFFF, value);
		}
		#endregion Write

		#region Fetch
		private sbyte FetchB()
		{
			return ReadB(this.m_PC++);
		}

		private short FetchW()
		{
			short data = ReadW(this.m_PC);
			this.m_PC += 2;
			return data;
		}

		private int FetchL()
		{
			int data = ReadL(this.m_PC);
			this.m_PC += 4;
			return data;
		}
		#endregion Fetch

		#region Peek
		private sbyte PeekB()
		{
			return ReadB(this.m_PC);
		}

		private short PeekW()
		{
			return ReadW(this.m_PC);
		}

		private int PeekL()
		{
			return ReadL(this.m_PC);
		}
		#endregion Peek

		private int FetchIndex()
		{
			short extension = FetchW();
			int da = (extension >> 15) & 0x1;
			int reg = (extension >> 12) & 0x7;
			int wl = (extension >> 11) & 0x1;
			int scale = (extension >> 9) & 0x3;
			sbyte displacement = (sbyte)extension;

			int indexReg = (scale == 0) ? 1 : ((scale == 1) ? 2 : ((scale == 2) ? 4 : 8));
			if (da == 0)
			{
				indexReg *= (wl == 0) ? (short)this.m_D[reg] : this.m_D[reg];
			}
			else
			{
				indexReg *= (wl == 0) ? (short)this.m_A[reg] : this.m_A[reg];
			}

			return displacement + indexReg;
		}

		private int PeekIndex()
		{
			short extension = PeekW();
			int da = extension >> 15 & 0x1;
			int reg = extension >> 12 & 0x7;
			int wl = extension >> 11 & 0x1;
			int scale = extension >> 9 & 0x3;
			sbyte displacement = (sbyte)extension;

			int indexReg = (scale == 0) ? 1 : ((scale == 1) ? 2 : ((scale == 2) ? 4 : 8));
			if (da == 0)
			{
				indexReg *= (wl == 0) ? (short)this.m_D[reg] : this.m_D[reg];
			}
			else
			{
				indexReg *= (wl == 0) ? (short)this.m_A[reg] : this.m_A[reg];
			}

			return displacement + indexReg;
		}

		#region FetchOperand
		private sbyte FetchOperandB(int mode, int register)
		{
			switch (mode)
			{
				case 0x0: // Dn
					{
						return (sbyte)this.m_D[register];
					}
				case 0x1: // An
					{
						return (sbyte)this.m_A[register];
					}
				case 0x2: // (An)
					{
						return ReadB(this.m_A[register]);
					}
				case 0x3: // (An)+
					{
						sbyte operand = ReadB(this.m_A[register]);
						this.m_A[register] += (register == 7) ? 2 : 1;
						return operand;
					}
				case 0x4: // -(An)
					{
						this.m_A[register] -= (register == 7) ? 2 : 1;
						return ReadB(this.m_A[register]);
					}
				case 0x5: //(d16,An)
					{
						return ReadB(this.m_A[register] + FetchW());
					}
				case 0x6: // (d8,An,Xn)
					{
						return this.ReadB(this.m_A[register] + FetchIndex());
					}
				case 0x7:
					switch (register)
					{
						case 0x0: // (xxx).W
							{
								return ReadB(FetchW());
							}
						case 0x1: // (xxx).L
							{
								return ReadB(FetchL());
							}
						case 0x2: // (d16,PC)
							{
								return ReadB(this.m_PC + FetchW());
							}
						case 0x3: // (d8,PC,Xn)
							{
								sbyte operand = ReadB(this.m_PC + PeekIndex());
								this.m_PC += 2;
								return operand;
							}
                        case 0x4: // #<data>
                            {
                                return (sbyte)FetchW();
                            }
						default:
							throw new ArgumentException("Addressing mode doesn't exist!");
					}
				default:
					throw new ArgumentException("Addressing mode doesn't exist!");
			}
		}

		private short FetchOperandW(int mode, int register)
		{
			switch (mode)
			{
				case 0x0: // Dn
					{
						return (short)this.m_D[register];
					}
				case 0x1: // An
					{
						return (short)this.m_A[register];
					}
				case 0x2: // (An)
					{
						return ReadW(this.m_A[register]);
					}
				case 0x3: // (An)+
					{
						short operand = ReadW(this.m_A[register]);
						this.m_A[register] += 2;
						return operand;
					}
				case 0x4: // -(An)
					{
						this.m_A[register] -= 2;
						return ReadW(this.m_A[register]);
					}
				case 0x5: //(d16,An)
					{
						return ReadW(this.m_A[register] + FetchW());
					}
				case 0x6: // (d8,An,Xn)
					{
						return ReadW(this.m_A[register] + FetchIndex());
					}
				case 0x7:
					switch (register)
					{
						case 0x0: // (xxx).W
							{
								return ReadW(FetchW());
							}
						case 0x1: // (xxx).L
							{
								return ReadW(FetchL());
							}
						case 0x4: // #<data>
							{
								return FetchW();
							}
						case 0x2: // (d16,PC)
							{
								return ReadW(this.m_PC + FetchW());
							}
						case 0x3: // (d8,PC,Xn)
							{
								short operand = ReadW(this.m_PC + PeekIndex());
								this.m_PC += 2;
								return operand;
							}
						default:
							throw new ArgumentException("Addressing mode doesn't exist!");
					}
				default:
					throw new ArgumentException("Addressing mode doesn't exist!");
			}
		}

		private int FetchOperandL(int mode, int register)
		{
			switch (mode)
			{
				case 0x0: // Dn
					{
						return this.m_D[register];
					}
				case 0x1: // An
					{
						return this.m_A[register];
					}
				case 0x2: // (An)
					{
						return ReadL(this.m_A[register]);
					}
				case 0x3: // (An)+
					{
						int operand = ReadL(this.m_A[register]);
						this.m_A[register] += 4;
						return operand;
					}
				case 0x4: // -(An)
					{
						this.m_A[register] -= 4;
						return ReadL(this.m_A[register]);
					}
				case 0x5: //(d16,An)
					{
						return ReadL(this.m_A[register] + FetchW());
					}
				case 0x6: // (d8,An,Xn)
					{
						return ReadL(this.m_A[register] + FetchIndex());
					}
				case 0x7:
					switch (register)
					{
						case 0x0: // (xxx).W
							{
								return ReadL(FetchW());
							}
						case 0x1: // (xxx).L
							{
								return ReadL(FetchL());
							}
						case 0x4: // #<data>
							{
								return FetchL();
							}
						case 0x2: // (d16,PC)
							{
								return ReadL(this.m_PC + FetchW());
							}
						case 0x3: // (d8,PC,Xn)
							{
								int operand = ReadL(this.m_PC + PeekIndex());
								this.m_PC += 2;
								return operand;
							}
						default:
							throw new ArgumentException("Addressing mode doesn't exist!");
					}
				default:
					throw new ArgumentException("Addressing mode doesn't exist!");
			}
		}
		#endregion FetchOperand

		#region FetchAddress
		private int FetchAddress(int mode, int register)
		{
			switch (mode)
			{
				case 0x0: // Dn
					{
						throw new ArgumentException("Invalid mode!");
					}
				case 0x1: // An
					{
						throw new ArgumentException("Invalid mode!");
					}
				case 0x2: // (An)
					{
						return this.m_A[register];
					}
				case 0x3: // (An)+
					{
						return this.m_A[register];
					}
				case 0x4: // -(An)
					{
						return this.m_A[register];
					}
				case 0x5: //(d16,An)
					{
						return (this.m_A[register]);
					}
				case 0x6: // (d8,An,Xn)
					{
						return (this.m_A[register] + FetchIndex());
					}
				case 0x7:
					switch (register)
					{
						case 0x0: // (xxx).W
							{
								return FetchW();
							}
						case 0x1: // (xxx).L
							{
								return FetchL();
							}
						case 0x4: // #<data>
							{
								throw new ArgumentException("Invalid mode!");
							}
						case 0x2: // (d16,PC)
							{
								return (this.m_PC + FetchW());
							}
						case 0x3: // (d8,PC,Xn)
							{
								int address = (this.m_PC + PeekIndex());
								this.m_PC += 2;
								return address;
							}
						default:
							throw new ArgumentException("Addressing mode doesn't exist!");
					}
				default:
					throw new NotImplementedException("Addressing mode doesn't exist!");
			}
		}
		#endregion FetchAddress

		#region SetOperand
		private void SetOperandB(int mode, int register, sbyte value)
		{
			switch (mode)
			{
				case 0x0: // Dn
					{
						Helpers.Inject(ref this.m_D[register], value);
						return;
					}
				case 0x1: // An
					{
						this.m_A[register] = value;
						return;
					}
				case 0x2: // (An)
					{
						WriteB(this.m_A[register], value);
						return;
					}
				case 0x3: // (An)+
					{
						WriteB(this.m_A[register]++, value);
						return;
					}
				case 0x4: // -(An)
					{
						WriteB(--this.m_A[register], value);
						return;
					}
				case 0x5: //(d16,An)
					{
						WriteB(this.m_A[register] + FetchW(), value);
						return;
					}
				case 0x6: // (d8,An,Xn)
					{
						WriteB(this.m_A[register] + FetchIndex(), value);
						return;
					}
				case 0x7:
					switch (register)
					{
						case 0x0: // (xxx).W
							{
								WriteB(FetchW(), value);
								return;
							}
						case 0x1: // (xxx).L
							{
								WriteB(FetchL(), value);
								return;
							}
						case 0x4: // #<data>
							{
								throw new ArgumentException("Invalid mode!");
							}
						case 0x2: // (d16,PC)
							{
								WriteB(this.m_PC + FetchW(), value);
								return;
							}
						case 0x3: // (d8,PC,Xn)
							{
								WriteB(this.m_PC + PeekIndex(), value);
								return;
							}
						default:
							throw new ArgumentException("Addressing mode doesn't exist!");
					}
				default:
					throw new NotImplementedException("Addressing mode doesn't exist!");
			}
		}

		private void SetOperandW(int mode, int register, short value)
		{
			switch (mode)
			{
				case 0x0: // Dn
					{
						Helpers.Inject(ref this.m_D[register], value);
						break;
					}
				case 0x1: // An
					{
						this.m_A[register] = value;
						return;
					}
				case 0x2: // (An)
					{
						WriteW(m_A[register], value);
						return;
					}
				case 0x3: // (An)+
					{
						WriteW(m_A[register], value);
						m_A[register] += 2;
						return;
					}
				case 0x4: // -(An)
					{
						m_A[register] -= 2;
						WriteW(m_A[register], value);
						return;
					}
				case 0x5: //(d16,An)
					{
						WriteW(this.m_A[register] + FetchW(), value);
						return;
					}
				case 0x6: // (d8,An,Xn)
					{
						WriteW(this.m_A[register] + FetchIndex(), value);
						return;
					}
				case 0x7:
					switch (register)
					{
						case 0x0: // (xxx).W
							{
								WriteW(FetchW(), value);
								return;
							}
						case 0x1: // (xxx).L
							{
								WriteW(FetchL(), value);
								return;
							}
						case 0x4: // #<data>
							{
								throw new ArgumentException("Invalid mode!");
							}
						case 0x2: // (d16,PC)
							{
								WriteW(this.m_PC + FetchW(), value);
								return;
							}
						case 0x3: // (d8,PC,Xn)
							{
								WriteW(this.m_PC + PeekIndex(), value);
								return;
							}
						default:
							throw new ArgumentException("Addressing mode doesn't exist!");
					}
				default:
					throw new NotImplementedException("Addressing mode doesn't exist!");
			}
		}

		private void SetOperandL(int mode, int register, int value)
		{
			switch (mode)
			{
				case 0x0: // Dn
					{
						this.m_D[register] = value;
						return;
					}
				case 0x1:
					{
						// When setting address registers, need to fill whole byte
						this.m_A[register] = value;
						return;
					}
				case 0x2: // (An)
					{
						WriteL(this.m_A[register], value);
						return;
					}
				case 0x3: // (An)+
					{
						WriteL(this.m_A[register], value);
						this.m_A[register] += 4;
						return;
					}
				case 0x4: // -(An)
					{
						this.m_A[register] -= 4;
						WriteL(this.m_A[register], value);
						return;
					}
				case 0x5: //(d16,An)
					{
						WriteL(this.m_A[register] + FetchW(), value);
						return;
					}
				case 0x6: // (d8,An,Xn)
					{
						WriteL(this.m_A[register] + FetchIndex(), value);
						return;
					}
				case 0x7:
					switch (register)
					{
						case 0x0: // (xxx).W
							{
								WriteL(FetchW(), value);
								return;
							}
						case 0x1: // (xxx).L
							{
								WriteL(FetchL(), value);
								return;
							}
						case 0x4: // #<data>
							{
								throw new ArgumentException("Invalid mode!");
							}
						case 0x2: // (d16,PC)
							{
								WriteL(this.m_PC + FetchW(), value);
								return;
							}
						case 0x3: // (d8,PC,Xn)
							{
								WriteL(this.m_A[register] + PeekIndex(), value);
								return;
							}
						default:
							throw new ArgumentException("Addressing mode doesn't exist!");
					}
				default:
					throw new NotImplementedException("Addressing mode doesn't exist!");
			}
		}
		#endregion SetOperand

		#region PeekOperand
		private sbyte PeekOperandB(int mode, int register)
		{
			switch (mode)
			{
				case 0x0: // Dn
					{
						return (sbyte)this.m_D[register];
					}
				case 0x1: // An
					{
						return (sbyte)this.m_A[register];
					}
				case 0x2: // (An)
					{
						return ReadB(this.m_A[register]);
					}
				case 0x3: // (An)+
					{
						return ReadB(this.m_A[register]);
					}
				case 0x4: // -(An)
					{
						return ReadB(this.m_A[register]);
					}
				case 0x5: //(d16,An)
					{
						return ReadB(this.m_A[register] + PeekW());
					}
				case 0x6: // (d8,An,Xn)
					{
						return this.ReadB(this.m_A[register] + PeekIndex());
					}
				case 0x7:
					switch (register)
					{
						case 0x0: // (xxx).W
							{
								return ReadB(PeekW());
							}
						case 0x1: // (xxx).L
							{
								return ReadB(PeekL());
							}
						case 0x4: // #<data>
							{
								return (sbyte)PeekW();
							}
						case 0x2: // (d16,PC)
							{
								return ReadB(this.m_PC + PeekW());
							}
						case 0x3: // (d8,PC,Xn)
							{
								return this.ReadB(this.m_PC + FetchIndex());
							}
						default:
							throw new ArgumentException("Addressing mode doesn't exist!");
					}
				default:
					throw new ArgumentException("Addressing mode doesn't exist!");
			}
		}

		private short PeekOperandW(int mode, int register)
		{
			switch (mode)
			{
				case 0x0: // Dn
					{
						return (short)this.m_D[register];
					}
				case 0x1: // An
					{
						return (short)this.m_A[register];
					}
				case 0x2: // (An)
					{
						return ReadW(this.m_A[register]);
					}
				case 0x3: // (An)+
					{
						return ReadW(this.m_A[register]);
					}
				case 0x4: // -(An)
					{
						return ReadW(this.m_A[register]);
					}
				case 0x5: //(d16,An)
					{
						return ReadW(this.m_A[register] + PeekW());
					}
				case 0x6: // (d8,An,Xn)
					{
						return ReadW(this.m_A[register] + PeekIndex());
					}
				case 0x7:
					switch (register)
					{
						case 0x0: // (xxx).W
							{
								return ReadW(PeekW());
							}
						case 0x1: // (xxx).L
							{
								return ReadW(PeekL());
							}
						case 0x4: // #<data>
							{
								return PeekW();
							}
						case 0x2: // (d16,PC)
							{
								return ReadW(this.m_PC + PeekW());
							}
						case 0x3: // (d8,PC,Xn)
							{
								return ReadW(this.m_PC + PeekIndex());
							}
						default:
							throw new ArgumentException("Addressing mode doesn't exist!");
					}
				default:
					throw new ArgumentException("Addressing mode doesn't exist!");
			}
		}

		private int PeekOperandL(int mode, int register)
		{
			switch (mode)
			{
				case 0x0: // Dn
					{
						return this.m_D[register];
					}
				case 0x1: // An
					{
						return this.m_A[register];
					}
				case 0x2: // (An)
					{
						return ReadL(this.m_A[register]);
					}
				case 0x3: // (An)+
					{
						return ReadL(this.m_A[register]);
					}
				case 0x4: // -(An)
					{
						return ReadL(this.m_A[register]);
					}
				case 0x5: //(d16,An)
					{
						return ReadL(this.m_A[register] + PeekW());
					}
				case 0x6: // (d8,An,Xn)
					{
						return this.ReadL(this.m_A[register] + FetchIndex());
					}
				case 0x7:
					switch (register)
					{
						case 0x0: // (xxx).W
							{
								return ReadL(PeekW());
							}
						case 0x1: // (xxx).L
							{
								return ReadL(PeekL());
							}
						case 0x4: // #<data>
							{
								return PeekL();
							}
						case 0x2: // (d16,PC)
							{
								return ReadL(this.m_PC + PeekW());
							}
						case 0x3: // (d8,PC,Xn)
							{
								return this.ReadB(this.m_PC + FetchIndex());
							}
						default:
							throw new ArgumentException("Addressing mode doesn't exist!");
					}
				default:
					throw new ArgumentException("Addressing mode doesn't exist!");
			}
		}
		#endregion PeekOperand
	}
}
