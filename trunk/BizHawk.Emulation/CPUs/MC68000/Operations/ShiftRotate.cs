using System;
using System.Collections.Generic;
using System.Text;

namespace MC68000
{
	public partial class MC68K
	{
		#region Arithmetic Shift
		private void ASL()
		{
			int count_register = (this.m_IR >> 9) & 0x7;
			int size = (this.m_IR >> 6) & 0x3;
			int ir = (this.m_IR >> 5) & 0x1;
			int register = this.m_IR & 0x7;

			int shift_count = (ir == 0) ?
				((count_register == 0) ? 8 : count_register) :
				(this.m_D[count_register] % 64);

			this.C = this.V = false;

			switch (size)
			{
				case 0:
					{
						sbyte value = (sbyte)this.m_D[register];
						bool msbit = value < 0;
						for (int i = 0; i < shift_count; i++)
						{
							this.C = this.X = value < 0;
							value <<= 1;
							this.V |= ((value < 0) != msbit);
						}
						Helpers.Inject(ref this.m_D[register], value);
						this.N = value < 0;
						this.Z = value == 0;
						this.m_Cycles += 6 + 2 * shift_count;
						return;
					}
				case 1:
					{
						short value = (short)this.m_D[register];
						bool msbit = value < 0;
						for (int i = 0; i < shift_count; i++)
						{
							this.C = this.X = value < 0;
							value <<= 1;
							this.V |= ((value < 0) != msbit);
						}
						Helpers.Inject(ref this.m_D[register], value);
						this.N = value < 0;
						this.Z = value == 0;
						this.m_Cycles += 6 + 2 * shift_count;
						return;
					}
				case 2:
					{
						int value = this.m_D[register];
						bool msbit = value < 0;
						for (int i = 0; i < shift_count; i++)
						{
							this.C = this.X = value < 0;
							value <<= 1;
							this.V |= ((value < 0) != msbit);
						}
						this.m_D[register] = value;
						this.N = value < 0;
						this.Z = value == 0;
						this.m_Cycles += 8 + 2 * shift_count;
						return;
					}
			}
		}

		private void ASR()
		{
			int count_register = (this.m_IR >> 9) & 0x7;
			int size = (this.m_IR >> 6) & 0x3;
			int ir = (this.m_IR >> 5) & 0x1;
			int register = this.m_IR & 0x7;

			int shift_count = (ir == 0) ?
				((count_register == 0) ? 8 : count_register) :
				(this.m_D[count_register] % 64);

			this.C = this.V = false;

			switch (size)
			{
				case 0:
					{
						sbyte value = (sbyte)this.m_D[register];
						for (int i = 0; i < shift_count; i++)
						{
							this.C = this.X = (value & 1) > 0;
							value >>= 1;
						}
						Helpers.Inject(ref this.m_D[register], value);
						this.N = value < 0;
						this.Z = value == 0;
						this.m_Cycles += 6 + 2 * shift_count;
						return;
					}
				case 1:
					{
						short value = (short)this.m_D[register];
						for (int i = 0; i < shift_count; i++)
						{
							this.C = this.X = (value & 1) > 0;
							value >>= 1;
						}
						Helpers.Inject(ref this.m_D[register], value);
						this.N = value < 0;
						this.Z = value == 0;
						this.m_Cycles += 6 + 2 * shift_count;
						return;
					}
				case 2:
					{
						int value = this.m_D[register];
						for (int i = 0; i < shift_count; i++)
						{
							this.C = this.X = (value & 1) > 0;
							value >>= 1;
						}
						this.m_D[register] = value;
						this.N = value < 0;
						this.Z = value == 0;
						this.m_Cycles += 8 + 2 * shift_count;
						return;
					}
			}
		}

		private void ASL_ASR_Memory()
		{
			int direction = (this.m_IR >> 8) & 0x1; // 0 = Right, 1 = Left
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			short value = PeekOperandW(mode, register);
			if (direction == 0)
			{
				this.C = this.X = (value & 1) > 0;
				this.V = false; // For right shift, MSB can't change
				value >>= 1;
			}
			else
			{
				bool msbit = (value < 0);
				this.C = this.X = value < 0;
				value <<= 1;
				this.V |= ((value < 0) != msbit);
			}
			this.N = value < 0;
			this.Z = value == 0;
			SetOperandW(mode, register, value);
			this.m_Cycles += 8 + Helpers.EACalcTimeBW(mode, register);
		}
		#endregion Arithmetic Shift

		#region Logical Shift
		private void LSL()
		{
			int count_register = (this.m_IR >> 9) & 0x7;
			int size = (this.m_IR >> 6) & 0x3;
			int ir = (this.m_IR >> 5) & 0x1;
			int register = this.m_IR & 0x7;

			int shift_count = (ir == 0) ?
				((count_register == 0) ? 8 : count_register) :
				(this.m_D[count_register] % 64);

			this.C = this.V = false;

			switch (size)
			{
				case 0:
					{
						byte value = (byte)this.m_D[register];
						for (int i = 0; i < shift_count; i++)
						{
							this.C = this.X = (value & 0x80) > 0;
							value <<= 1;
						}
						Helpers.Inject(ref this.m_D[register], value);
						this.N = (value & 0x80) > 0;
						this.Z = value == 0;
						this.m_Cycles += 6 + 2 * shift_count;
						return;
					}
				case 1:
					{
						ushort value = (ushort)this.m_D[register];
						for (int i = 0; i < shift_count; i++)
						{
							this.C = this.X = (value & 0x8000) > 0;
							value <<= 1;
						}
						Helpers.Inject(ref this.m_D[register], value);
						this.N = (value & 0x8000) > 0;
						this.Z = value == 0;
						this.m_Cycles += 6 + 2 * shift_count;
						return;
					}
				case 2:
					{
						uint value = (uint)this.m_D[register];
						for (int i = 0; i < shift_count; i++)
						{
							this.C = this.X = (value & 0x80000000) > 0;
							value <<= 1;
						}
						this.m_D[register] = (int)value;
						this.N = (value & 0x80000000) > 0;
						this.Z = value == 0;
						this.m_Cycles += 8 + 2 * shift_count;
						return;
					}
			}
		}

		private void LSR()
		{
			int count_register = (this.m_IR >> 9) & 0x7;
			int size = (this.m_IR >> 6) & 0x3;
			int ir = (this.m_IR >> 5) & 0x1;
			int register = this.m_IR & 0x7;

			int shift_count = (ir == 0) ?
				((count_register == 0) ? 8 : count_register) :
				(this.m_D[count_register] % 64);

			this.C = this.V = false;

			switch (size)
			{
				case 0:
					{
						byte value = (byte)this.m_D[register];
						for (int i = 0; i < shift_count; i++)
						{
							this.C = this.X = (value & 1) > 0;
							value >>= 1;
						}
						Helpers.Inject(ref this.m_D[register], value);
						this.N = (value & 0x80) > 0;
						this.Z = value == 0;
						this.m_Cycles += 6 + 2 * shift_count;
						return;
					}
				case 1:
					{
						ushort value = (ushort)this.m_D[register];
						for (int i = 0; i < shift_count; i++)
						{
							this.C = this.X = (value & 1) > 0;
							value >>= 1;
						}
						Helpers.Inject(ref this.m_D[register], value);
						this.N = (value & 0x8000) > 0;
						this.Z = value == 0;
						this.m_Cycles += 6 + 2 * shift_count;
						return;
					}
				case 2:
					{
						uint value = (uint)this.m_D[register];
						for (int i = 0; i < shift_count; i++)
						{
							this.C = this.X = (value & 1) > 0;
							value >>= 1;
						}
						this.m_D[register] = (int)value;
						this.N = (value & 0x80000000) > 0;
						this.Z = value == 0;
						this.m_Cycles += 8 + 2 * shift_count;
						return;
					}
			}
		}

		private void LSL_LSR_Memory()
		{
			int direction = (this.m_IR >> 8) & 0x1; // 0 = Right, 1 = Left
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			ushort value = (ushort)PeekOperandW(mode, register);
			if (direction == 0)
			{
				this.C = this.X = (value & 1) > 0;
				value >>= 1;
			}
			else
			{
				this.C = this.X = (value & 0x8000) > 0;
				value <<= 1;
			}
			this.V = false;
			this.N = (value & 0x8000) > 0;
			this.Z = value == 0;
			SetOperandW(mode, register, (short)value);
			this.m_Cycles += 8 + Helpers.EACalcTimeBW(mode, register);
		}
		#endregion Logical Shift

		#region Rotate (without extend)
		private void ROL()
		{
			int count_register = (this.m_IR >> 9) & 0x7;
			int size = (this.m_IR >> 6) & 0x3;
			int ir = (this.m_IR >> 5) & 0x1;
			int register = this.m_IR & 0x7;

			int shift_count = (ir == 0) ?
				((count_register == 0) ? 8 : count_register) :
				(this.m_D[count_register] % 64);

			this.C = this.V = false;

			switch (size)
			{
				case 0:
					{
						byte value = (byte)this.m_D[register];
						for (int i = 0; i < shift_count; i++)
						{
							this.C = (value & 0x80) > 0;
							value = (byte)((value >> 7) | (value << 1));
						}
						Helpers.Inject(ref this.m_D[register], value);
						this.N = (value & 0x80) > 0;
						this.Z = value == 0;
						this.m_Cycles += 6 + 2 * shift_count;
						return;
					}
				case 1:
					{
						ushort value = (ushort)this.m_D[register];
						for (int i = 0; i < shift_count; i++)
						{
							this.C = (value & 0x8000) > 0;
							value = (ushort)((value >> 15) | (value << 1));
						}
						Helpers.Inject(ref this.m_D[register], value);
						this.N = (value & 0x8000) > 0;
						this.Z = value == 0;
						this.m_Cycles += 6 + 2 * shift_count;
						return;
					}
				case 2:
					{
						uint value = (uint)this.m_D[register];
						for (int i = 0; i < shift_count; i++)
						{
							this.C = (value & 0x80000000) > 0;
							value = (uint)((value >> 31) | (value << 1));
						}
						this.m_D[register] = (int)value;
						this.N = (value & 0x80000000) > 0;
						this.Z = value == 0;
						this.m_Cycles += 8 + 2 * shift_count;
						return;
					}
			}
		}

		private void ROR()
		{
			int count_register = (this.m_IR >> 9) & 0x7;
			int size = (this.m_IR >> 6) & 0x3;
			int ir = (this.m_IR >> 5) & 0x1;
			int register = this.m_IR & 0x7;

			int shift_count = (ir == 0) ?
				((count_register == 0) ? 8 : count_register) :
				(this.m_D[count_register] % 64);

			this.C = this.V = false;

			switch (size)
			{
				case 0:
					{
						byte value = (byte)this.m_D[register];
						for (int i = 0; i < shift_count; i++)
						{
							this.C = (value & 1) > 0;
							value = (byte)((value << 7) | (value >> 1));
						}
						Helpers.Inject(ref this.m_D[register], value);
						this.N = (value & 0x80) > 0;
						this.Z = value == 0;
						this.m_Cycles += 6 + 2 * shift_count;
						return;
					}
				case 1:
					{
						ushort value = (ushort)this.m_D[register];
						for (int i = 0; i < shift_count; i++)
						{
							this.C = (value & 1) > 0;
							value = (ushort)((value << 15) | (value >> 1));
						}
						Helpers.Inject(ref this.m_D[register], value);
						this.N = (value & 0x8000) > 0;
						this.Z = value == 0;
						this.m_Cycles += 6 + 2 * shift_count;
						return;
					}
				case 2:
					{
						uint value = (uint)this.m_D[register];
						for (int i = 0; i < shift_count; i++)
						{
							this.C = (value & 1) > 0;
							value = (uint)((value << 31) | (value >> 1));
						}
						this.m_D[register] = (int)value;
						this.N = (value & 0x80000000) > 0;
						this.Z = value == 0;
						this.m_Cycles += 8 + 2 * shift_count;
						return;
					}
			}
		}

		private void ROL_ROR_Memory()
		{
		    int direction = (this.m_IR >> 8) & 0x1; // 0 = Right, 1 = Left
		    int mode = (this.m_IR >> 3) & 0x7;
		    int register = this.m_IR & 0x7;

		    ushort value = (ushort)PeekOperandW(mode, register);
		    if (direction == 0)
		    {
		        this.C = (value & 1) > 0;
		        value = (ushort)((value >> 1) | (value << 15));
		    }
		    else
		    {
		        this.C = (value & 0x8000) > 0;
				value = (ushort)((value >> 15) | (value << 1));
			}
		    this.V = false;
		    this.N = (value & 0x8000) > 0;
		    this.Z = value == 0;
		    SetOperandW(mode, register, (short)value);
		    this.m_Cycles += 8 + Helpers.EACalcTimeBW(mode, register);
		}
		#endregion Rotate (without extend)

		#region Rotate (with extend)
		private void ROXL()
		{
			int count_register = (this.m_IR >> 9) & 0x7;
			int size = (this.m_IR >> 6) & 0x3;
			int ir = (this.m_IR >> 5) & 0x1;
			int register = this.m_IR & 0x7;

			int shift_count = (ir == 0) ?
				((count_register == 0) ? 8 : count_register) :
				(this.m_D[count_register] % 64);

			this.V = false;
			this.C = this.X;

			switch (size)
			{
				case 0:
					{
						byte value = (byte)this.m_D[register];
						for (int i = 0; i < shift_count; i++)
						{
							this.C = (value & 0x80) > 0;
							value = (byte)((value << 1) | ((this.X) ? 1: 0));
							this.X = this.C;
						}
						Helpers.Inject(ref this.m_D[register], value);
						this.N = (value & 0x80) > 0;
						this.Z = value == 0;
						this.m_Cycles += 6 + 2 * shift_count;
						return;
					}
				case 1:
					{
						ushort value = (ushort)this.m_D[register];
						for (int i = 0; i < shift_count; i++)
						{
							this.C = (value & 0x8000) > 0;
							value = (ushort)((value << 1) | ((this.X) ? 1 : 0));
							this.X = this.C;
						}
						Helpers.Inject(ref this.m_D[register], value);
						this.N = (value & 0x8000) > 0;
						this.Z = value == 0;
						this.m_Cycles += 6 + 2 * shift_count;
						return;
					}
				case 2:
					{
						uint value = (uint)this.m_D[register];
						for (int i = 0; i < shift_count; i++)
						{
							this.C = (value & 0x80000000) > 0;
							value = (uint)((value << 1) | ((this.X) ? (uint)1 : (uint)0));
							this.X = this.C;
						}
						this.m_D[register] = (int)value;
						this.N = (value & 0x80000000) > 0;
						this.Z = value == 0;
						this.m_Cycles += 8 + 2 * shift_count;
						return;
					}
			}
		}

		private void ROXR()
		{
			int count_register = (this.m_IR >> 9) & 0x7;
			int size = (this.m_IR >> 6) & 0x3;
			int ir = (this.m_IR >> 5) & 0x1;
			int register = this.m_IR & 0x7;

			int shift_count = (ir == 0) ?
				((count_register == 0) ? 8 : count_register) :
				(this.m_D[count_register] % 64);

			this.V = false;
			this.C = this.X;

			switch (size)
			{
				case 0:
					{
						byte value = (byte)this.m_D[register];
						for (int i = 0; i < shift_count; i++)
						{
							this.C = (value & 1) > 0;
							value = (byte)(((this.X) ? 0x80 : 0) | (value >> 1));
							this.X = this.C;
						}
						Helpers.Inject(ref this.m_D[register], value);
						this.N = (value & 0x80) > 0;
						this.Z = value == 0;
						this.m_Cycles += 6 + 2 * shift_count;
						return;
					}
				case 1:
					{
						ushort value = (ushort)this.m_D[register];
						for (int i = 0; i < shift_count; i++)
						{
							this.C = (value & 1) > 0;
							value = (ushort)(((this.X) ? 0x8000 : 0) | (value >> 1));
						}
						Helpers.Inject(ref this.m_D[register], value);
						this.N = (value & 0x8000) > 0;
						this.Z = value == 0;
						this.m_Cycles += 6 + 2 * shift_count;
						return;
					}
				case 2:
					{
						uint value = (uint)this.m_D[register];
						for (int i = 0; i < shift_count; i++)
						{
							this.C = (value & 1) > 0;
							value = (uint)(((this.X) ? 0x80000000 : 0) | (value >> 1));
						}
						this.m_D[register] = (int)value;
						this.N = (value & 0x80000000) > 0;
						this.Z = value == 0;
						this.m_Cycles += 8 + 2 * shift_count;
						return;
					}
			}
		}

		private void ROXL_ROXR_Memory()
		{
			int direction = (this.m_IR >> 8) & 0x1; // 0 = Right, 1 = Left
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			ushort value = (ushort)PeekOperandW(mode, register);
			if (direction == 0)
			{
				this.C = (value & 1) > 0;
				value = (ushort)(((this.X) ? 0x8000 : 0) | (value >> 1));
				this.X = this.C;
			}
			else
			{
				this.C = (value & 0x8000) > 0;
				value = (ushort)((value << 1) | ((this.X) ? 1 : 0));
				this.X = this.C;
			}
			this.V = false;
			this.N = (value & 0x8000) > 0;
			this.Z = value == 0;
			SetOperandW(mode, register, (short)value);
			this.m_Cycles += 8 + Helpers.EACalcTimeBW(mode, register);
		}
		#endregion Rotate (with extend)

		private void SWAP() // Swap halves of a register
		{
			int register = this.m_IR & 0x7;
			this.m_D[register] = (int)(((uint)this.m_D[register] << 16) |
				((uint)this.m_D[register] >> 16));

			this.N = (this.m_D[register] < 0);
			this.Z = (this.m_D[register] == 0);
			this.V = this.C = false;

			this.m_Cycles += 4;
		}
	}
}
