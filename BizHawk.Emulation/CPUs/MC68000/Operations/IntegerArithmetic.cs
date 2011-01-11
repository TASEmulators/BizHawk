using System;
using System.Collections.Generic;
using System.Text;

namespace MC68000
{
	public partial class MC68K
	{
		#region Add Helper Functions
		private sbyte Add(sbyte a, sbyte b, bool updateConditions, bool useX)
		{
			int result = useX ? (int)a + (int)b + (this.X ? 1 : 0) : (int)a + (int)b;

			if (updateConditions)
			{
				this.C = this.X = (result & 0x100) > 0;
				this.V = result > sbyte.MaxValue || result < sbyte.MinValue;
				this.N = result < 0;
				if (!useX) { this.Z = result == 0; }
			}

			return (sbyte)result;
		}

		private short Add(short a, short b, bool updateConditions, bool useX)
		{
			int result = useX ? (int)a + (int)b + (this.X ? 1 : 0) : (int)a + (int)b;

			if (updateConditions)
			{
				this.C = this.X = (result & 0x10000) > 0;
				this.V = result > short.MaxValue || result < short.MinValue;
				this.N = result < 0;
				if (!useX) { this.Z = result == 0; }
			}

			return (short)result;
		}

		private int Add(int a, int b, bool updateConditions, bool useX)
		{
			long result = useX ? (long)a + (long)b + (this.X ? 1 : 0) : (long)a + (long)b;

			if (updateConditions)
			{
				this.C = this.X = (result & 0x100000000) > 0;
				this.V = result > int.MaxValue || result < int.MinValue;
				this.N = result < 0;
				if (!useX) { this.Z = result == 0; }
			}

			return (int)result;
		}
		#endregion Add Helper Functions

		#region Sub Helper Functions
		private sbyte Sub(sbyte a, sbyte b, bool updateConditions, bool setX, bool useX)
		{
			int result = useX ? (int)b - (int)a - (this.X ? 1 : 0) : (int)b - (int)a;

			if (updateConditions)
			{
				this.C = (result & 0x100) > 0;
				this.V = result > sbyte.MaxValue || result < sbyte.MinValue;
				this.N = result < 0;
				if (!useX) { this.Z = result == 0; }
				if (setX) { this.X = this.C; }
			}

			return (sbyte)result;
		}

		private short Sub(short a, short b, bool updateConditions, bool setX, bool useX)
		{
			int result = useX ? (int)b - (int)a - (this.X ? 1 : 0) : (int)b - (int)a;

			if (updateConditions)
			{
				this.C = (result & 0x10000) > 0;
				this.V = result > short.MaxValue || result < short.MinValue;
				this.N = result < 0;
				if (!useX) { this.Z = result == 0; }
				if (setX) { this.X = this.C; }
			}

			return (short)result;
		}

		private int Sub(int a, int b, bool updateConditions, bool setX, bool useX)
		{
			long result = useX ? (long)b - (long)a - (this.X ? 1 : 0) : (long)b - (long)a;

			if (updateConditions)
			{
				this.C = (result & 0x100000000) > 0;
				this.V = result > int.MaxValue || result < int.MinValue;
				this.N = result < 0;
				if (!useX) { this.Z = result == 0; }
				if (setX) { this.X = this.C; }
			}

			return (int)result;
		}
		#endregion Sub Helper Functions

		private void ADD_Dest()
		{
			int src_register = (this.m_IR >> 9) & 0x7;
			int size = (this.m_IR >> 6) & 0x3;
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			switch (size)
			{
				case 0:
					{
						sbyte result = Add((sbyte)this.m_D[src_register], PeekOperandB(mode, register), true, false);
						SetOperandB(mode, register, result);
						this.m_Cycles += 8 + Helpers.EACalcTimeBW(mode, register);
						return;
					}
				case 1:
					{
						short result = Add((short)this.m_D[src_register], PeekOperandW(mode, register), true, false);
						SetOperandW(mode, register, result);
						this.m_Cycles += 8 + Helpers.EACalcTimeBW(mode, register);
						return;
					}
				case 2:
					{
						int result = Add(this.m_D[src_register], PeekOperandL(mode, register), true, false);
						SetOperandL(mode, register, result);
						this.m_Cycles += 12 + Helpers.EACalcTimeL(mode, register);
						return;
					}
			}
		}

		private void ADD_Source()
		{
			int dest_register = (this.m_IR >> 9) & 0x7;
			int size = (this.m_IR >> 6) & 0x3;
			int src_mode = (this.m_IR >> 3) & 0x7;
			int src_register = this.m_IR & 0x7;

			switch (size)
			{
				case 0:
					{
						sbyte result = Add((sbyte)this.m_D[dest_register], FetchOperandB(src_mode, src_register), true, false);
						Helpers.Inject(ref this.m_D[dest_register], result);
						this.m_Cycles += 4 + Helpers.EACalcTimeBW(src_mode, src_register);
						return;
					}
				case 1:
					{
						short result = Add((short)this.m_D[dest_register], FetchOperandW(src_mode, src_register), true, false);
						Helpers.Inject(ref this.m_D[dest_register], result);
						this.m_Cycles += 4 + Helpers.EACalcTimeBW(src_mode, src_register);
						return;
					}
				case 2:
					{
						int result = Add(this.m_D[dest_register], FetchOperandL(src_mode, src_register), true, false);
						this.m_D[dest_register] = result;
						this.m_Cycles += 6 + Helpers.EACalcTimeL(src_mode, src_register);
						return;
					}
			}
		}

		private void ADDA()
		{
			int register = (this.m_IR >> 9) & 0x7;
			int opmode = (this.m_IR >> 6) & 0x7;
			int src_mode = (this.m_IR >> 3) & 0x7;
			int src_register = this.m_IR & 0x7;

			switch (opmode)
			{
				case 3: // W
					this.m_A[register] += FetchOperandW(src_mode, src_register);
					this.m_Cycles += 8 + Helpers.EACalcTimeBW(src_mode, src_register);
					return;
				case 7: // L
					this.m_A[register] += FetchOperandL(src_mode, src_register);
					this.m_Cycles += 6 + Helpers.EACalcTimeBW(src_mode, src_register);
					return;
			}
		}

		private void ADDI()
		{
			int size = (this.m_IR >> 6) & 0x3;
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			switch (size)
			{
				case 0: // B
					{
						sbyte result = Add((sbyte)FetchW(), PeekOperandB(mode, register), true, false);
						SetOperandB(mode, register, result);
						this.m_Cycles += (mode == 0) ? 8 : 12 + Helpers.EACalcTimeBW(mode, register);
						return;
					}
				case 1: // W
					{
						short result = Add(FetchW(), PeekOperandW(mode, register), true, false);
						SetOperandW(mode, register, result);
						this.m_Cycles += (mode == 0) ? 8 : 12 + Helpers.EACalcTimeBW(mode, register);
						return;
					}
				case 2: // L
					{
						int result = Add(FetchL(), PeekOperandL(mode, register), true, false);
						SetOperandL(mode, register, result);
						this.m_Cycles += (mode == 0) ? 16 : 20 + Helpers.EACalcTimeL(mode, register);
						return;
					}
			}
		}

		private void ADDQ() // Add Quick
		{
			int data = (this.m_IR >> 9) & 0x7;
			int size = (this.m_IR >> 6) & 0x3;
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			// With data, 0 means 8
			data = (data == 0) ? 8 : data;

			switch (size)
			{
				case 0: // B
					{
						if (mode == 1)
						{
							throw new ArgumentException("Byte operation not allowed on address registers");
						}
						sbyte result = Add(PeekOperandB(mode, register), (sbyte)data, (mode != 1), false);
						SetOperandB(mode, register, result);
						switch (mode) {
							case 0: this.m_Cycles += 4; break;
							case 1: this.m_Cycles += 4; break;
							default: this.m_Cycles += 8 + Helpers.EACalcTimeBW(mode, register); break;
						}
						return;
					}
				case 1: // W
					{
						if (mode == 1)
						{
							int result = Add(PeekOperandL(mode, register), data, false, false);
							SetOperandL(mode, register, result);
						}
						else
						{
							short result = Add(PeekOperandW(mode, register), (short)data, true, false);
							SetOperandW(mode, register, result);
						}
						switch (mode)
						{
							case 0: this.m_Cycles += 4; break;
							case 1: this.m_Cycles += 4; break;
							default: this.m_Cycles += 8 + Helpers.EACalcTimeBW(mode, register); break;
						}
						return;
					}
				case 2: // L
					{
						int result = Add(PeekOperandL(mode, register), data, (mode != 1), false);
						SetOperandL(mode, register, result);
						switch (mode)
						{
							case 0: this.m_Cycles += 8; break;
							case 1: this.m_Cycles += 8; break;
							default: this.m_Cycles += 12 + Helpers.EACalcTimeL(mode, register); break;
						}
						return;
					}
			}
		}

		private void ADDX()
		{
			int regRx = (this.m_IR >> 9) & 0x7;
			int size = (this.m_IR >> 6) & 0x3;
			int rm = (this.m_IR >> 3) & 0x1;
			int regRy = this.m_IR & 0x7;

			switch (size)
			{
				case 0:
					{
						if (rm == 0)
						{
							this.m_D[regRy] = Add((sbyte)this.m_D[regRx], (sbyte)this.m_D[regRy], true, true);
							this.m_Cycles += 4;
						}
						else
						{
							WriteB(this.m_A[regRy], Add(ReadB(this.m_A[regRx]), ReadB(this.m_A[regRy]), true, true));
							this.m_Cycles += 18;
						}
						return;
					}
				case 1:
					{
						if (rm == 0)
						{
							this.m_D[regRy] = Add((short)this.m_D[regRx], (short)this.m_D[regRy], true, true);
							this.m_Cycles += 4;
						}
						else
						{
							WriteW(this.m_A[regRy], Add(ReadW(this.m_A[regRx]), ReadW(this.m_A[regRy]), true, true));
							this.m_Cycles += 18;
						}
						return;
					}
				case 2:
					{
						if (rm == 0)
						{
							this.m_D[regRy] = Add(this.m_D[regRx], this.m_D[regRy], true, true);
							this.m_Cycles += 8;
						}
						else
						{
							WriteL(this.m_A[regRy], Add(ReadB(this.m_A[regRx]), ReadB(this.m_A[regRy]), true, true));
							this.m_Cycles += 30;
						}
						return;
					}
			}
		}

		private void CLR() // Clear an operand
		{
			int size = (this.m_IR >> 6) & 0x3;
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			this.C = this.N = this.V = false;
			this.Z = true;

			switch (size)
			{
				case 0: // B
					SetOperandB(mode, register, 0);
					this.m_Cycles += (mode == 0) ? 4 : 8 + Helpers.EACalcTimeBW(mode, register);
					return;
				case 1: // W
					SetOperandW(mode, register, 0);
					this.m_Cycles += (mode == 0) ? 4 : 8 + Helpers.EACalcTimeBW(mode, register);
					return;
				case 2: // L
					SetOperandL(mode, register, 0);
					this.m_Cycles += (mode == 0) ? 6 : 12 + Helpers.EACalcTimeL(mode, register);
					return;
			}
		}

		private void CMP() // Compare
		{
			int dest_register = (this.m_IR >> 9) & 0x7;
			int opmode = (this.m_IR >> 6) & 0x7;
			int src_mode = (this.m_IR >> 3) & 0x7;
			int src_register = this.m_IR & 0x7;

			switch (opmode)
			{
				case 0: // B
					Sub(FetchOperandB(src_mode, src_register), (sbyte)this.m_D[dest_register], true, false, false);
					this.m_Cycles += 4 + Helpers.EACalcTimeBW(src_mode, src_register);
					return;
				case 1: // W
					Sub(FetchOperandW(src_mode, src_register), (short)this.m_D[dest_register], true, false, false);
					this.m_Cycles += 4 + Helpers.EACalcTimeBW(src_mode, src_register);
					return;
				case 2: // L
					Sub(FetchOperandL(src_mode, src_register), this.m_D[dest_register], true, false, false);
					this.m_Cycles += 6 + Helpers.EACalcTimeL(src_mode, src_register);
					return;
			}
		}

		private void CMPA()
		{
			int dest_register = (this.m_IR >> 9) & 0x7;
			int opmode = (this.m_IR >> 6) & 0x7;
			int src_mode = (this.m_IR >> 3) & 0x7;
			int src_register = this.m_IR & 0x7;

			switch (opmode)
			{
				case 3: // W
					Sub((int)FetchOperandW(src_mode, src_register), this.m_A[dest_register], true, false, false);
					this.m_Cycles += 6 + Helpers.EACalcTimeBW(src_mode, src_register);
					return;
				case 7: // L
					Sub(FetchOperandL(src_mode, src_register), this.m_A[dest_register], true, false, false);
					this.m_Cycles += 6 + Helpers.EACalcTimeL(src_mode, src_register);
					return;
			}
		}

		private void CMPI()
		{
			int size = (this.m_IR >> 6) & 0x3;
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			switch (size)
			{
				case 0: // B
					Sub((sbyte)FetchW(), FetchOperandB(mode, register), true, false, false);
					this.m_Cycles += (mode == 0) ? 8 : 8 + Helpers.EACalcTimeBW(mode, register);
					return;
				case 1: // W
					Sub((short)FetchW(), FetchOperandW(mode, register), true, false, false);
					this.m_Cycles += (mode == 0) ? 8 : 8 + Helpers.EACalcTimeBW(mode, register);
					return;
				case 2: // L
					Sub(FetchL(), FetchOperandL(mode, register), true, false, false);
					this.m_Cycles += (mode == 0) ? 14 : 12 + Helpers.EACalcTimeL(mode, register);
					return;
			}
		}

		private void CMPM()
		{
			int registerAx = (this.m_IR >> 9) & 0x7;
			int size = (this.m_IR >> 6) & 0x3;
			int registerAy = this.m_IR & 0x7;

			switch (size)
			{
				case 0: // B
					Sub((sbyte)this.m_A[registerAy], (sbyte)this.m_A[registerAy], true, false, false);
					this.m_Cycles += 12;
					return;
				case 1: // W
					Sub((short)this.m_A[registerAy], (short)this.m_A[registerAy], true, false, false);
					this.m_Cycles += 12;
					return;
				case 2: // L
					Sub(this.m_A[registerAy], this.m_A[registerAy], true, false, false);
					this.m_Cycles += 20;
					return;
			}
		}

		private void DIVS() // Unsigned multiply
		{
			int dest_register = (this.m_IR >> 9) & 0x7;
			int src_mode = (this.m_IR >> 3) & 0x7;
			int src_register = this.m_IR & 0x7;

			// On 68000, only allowable size is Word
			int source = (short)FetchOperandW(src_mode, src_register);
			int dest = this.m_D[dest_register];

			this.C = false;
			if (source == 0)
			{
				throw new ArgumentException("Divide by zero...");
			}

			int quotient = dest / source;
			int remainder = dest % source;

			// Detect overflow
			if (quotient < short.MinValue || quotient > short.MaxValue)
			{
				this.V = true;
				throw new ArgumentException("Division overflow");
			}
			this.m_D[dest_register] = (remainder << 16) | quotient;

			this.m_Cycles += 158 + Helpers.EACalcTimeBW(src_mode, src_register);

			this.N = quotient < 0;
			this.Z = quotient == 0;
			this.V = false;
		}

		private void DIVU() // Unsigned multiply
		{
			int dest_register = (this.m_IR >> 9) & 0x7;
			int src_mode = (this.m_IR >> 3) & 0x7;
			int src_register = this.m_IR & 0x7;

			// On 68000, only allowable size is Word
			uint source = (uint)FetchOperandW(src_mode, src_register);
			uint dest = (uint)this.m_D[dest_register];

			this.C = false;
			if (source == 0)
			{
				throw new ArgumentException("Divide by zero...");
			}

			uint quotient = dest / source;
			uint remainder = dest % source;

			// Detect overflow
			if (quotient < ushort.MinValue || quotient > ushort.MaxValue ||
				remainder < ushort.MinValue || remainder > ushort.MaxValue)
			{
				this.V = true;
				throw new ArgumentException("Division overflow");
			}
			this.m_D[dest_register] = (int)((remainder << 16) | quotient);

			this.m_Cycles += 140 + Helpers.EACalcTimeBW(src_mode, src_register);

			this.N = quotient < 0;
			this.Z = quotient == 0;
			this.V = false;
		}

		private void EXT() // Sign extend
		{
			this.m_Cycles += 4;

			switch ((this.m_IR >> 6) & 0x7)
			{
				case 2: // Byte to word
					Helpers.Inject(ref this.m_D[this.m_IR & 0x7], (short)((sbyte)this.m_D[this.m_IR & 0x7]));
					break;
				case 3: // Word to long
					this.m_D[this.m_IR & 0x7] = (short)this.m_D[this.m_IR & 0x7];
					break;
			}
		}

		private void MULS() // Unsigned multiply
		{
			int dest_register = (this.m_IR >> 9) & 0x7;
			int src_mode = (this.m_IR >> 3) & 0x7;
			int src_register = this.m_IR & 0x7;

			// On 68000, only allowable size is Word
			short operand = FetchOperandW(src_mode, src_register);
			short currentValue = (short)this.m_D[dest_register];

			int newValue = operand * currentValue;
			this.m_D[dest_register] = newValue;

			this.m_Cycles += 70 + Helpers.EACalcTimeBW(src_mode, src_register);

			this.N = newValue < 0;
			this.Z = newValue == 0;
			this.V = false; // Can't get an overflow
			this.C = false;
		}

		private void MULU() // Unsigned multiply
		{
			int dest_register = (this.m_IR >> 9) & 0x7;
			int src_mode = (this.m_IR >> 3) & 0x7;
			int src_register = this.m_IR & 0x7;

			// On 68000, only allowable size is Word
			ushort operand = (ushort)FetchOperandW(src_mode, src_register);
			ushort currentValue = (ushort)this.m_D[dest_register];

			uint newValue = (uint)(operand * currentValue);
			this.m_D[dest_register] = (int)newValue;

			this.m_Cycles += 70 + Helpers.EACalcTimeBW(src_mode, src_register);

			this.N = (int)newValue < 0;
			this.Z = (newValue == 0);
			this.V = false; // Can't get an overflow
			this.C = false;
		}

		private void NEG()
		{
			int size = (this.m_IR >> 6) & 0x3;
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			switch (size)
			{
				case 0:
					SetOperandB(mode, register, Sub(PeekOperandB(mode, register), (sbyte)0, true, true, false));
					this.m_Cycles += (mode == 0) ? 4 : 8 + Helpers.EACalcTimeBW(mode, register);
					return;
				case 1:
					SetOperandW(mode, register, Sub(PeekOperandW(mode, register), (short)0, true, true, false));
					this.m_Cycles += (mode == 0) ? 4 : 8 + Helpers.EACalcTimeBW(mode, register);
					return;
				case 2:
					SetOperandL(mode, register, Sub(PeekOperandL(mode, register), (int)0, true, true, false));
					this.m_Cycles += (mode == 0) ? 6 : 12 + Helpers.EACalcTimeL(mode, register);
					return;
			}
		}

		private void NEGX()
		{
			int size = (this.m_IR >> 6) & 0x3;
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			switch (size)
			{
				case 0:
					SetOperandB(mode, register, Sub(PeekOperandB(mode, register), (sbyte)0, true, true, false));
					this.m_Cycles += (mode == 0) ? 4 : 8 + Helpers.EACalcTimeBW(mode, register);
					return;
				case 1:
					SetOperandW(mode, register, Sub(PeekOperandW(mode, register), (short)0, true, true, false));
					this.m_Cycles += (mode == 0) ? 4 : 8 + Helpers.EACalcTimeBW(mode, register);
					return;
				case 2:
					SetOperandL(mode, register, Sub(PeekOperandL(mode, register), (int)0, true, true, false));
					this.m_Cycles += (mode == 0) ? 6 : 12 + Helpers.EACalcTimeL(mode, register);
					return;
			}
		}

		private void SUB_Dest()
		{
			int src_register = (this.m_IR >> 9) & 0x7;
			int size = (this.m_IR >> 6) & 0x3;
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			switch (size)
			{
				case 0:
					{
						sbyte result = Sub((sbyte)this.m_D[src_register], PeekOperandB(mode, register), true, true, false);
						SetOperandB(mode, register, result);
						this.m_Cycles += 8 + Helpers.EACalcTimeBW(mode, src_register);
						return;
					}
				case 1:
					{
						short result = Sub((short)this.m_D[src_register], PeekOperandW(mode, register), true, true, false);
						SetOperandW(mode, register, result);
						this.m_Cycles += 8 + Helpers.EACalcTimeBW(mode, src_register);
						return;
					}
				case 2:
					{
						int result = Sub(this.m_D[src_register], PeekOperandL(mode, register), true, true, false);
						SetOperandL(mode, register, result);
						this.m_Cycles += 12 + Helpers.EACalcTimeL(mode, src_register);
						return;
					}
			}
		}

		private void SUB_Source()
		{
			int dest_register = (this.m_IR >> 9) & 0x7;
			int size = (this.m_IR >> 6) & 0x3;
			int src_mode = (this.m_IR >> 3) & 0x7;
			int src_register = this.m_IR & 0x7;

			switch (size)
			{
				case 0:
					{
						sbyte result = Sub(FetchOperandB(src_mode, src_register), (sbyte)this.m_D[dest_register], true, true, false);
						Helpers.Inject(ref this.m_D[dest_register], result);
						this.m_Cycles += 4 + Helpers.EACalcTimeBW(src_mode, src_register);
						return;
					}
				case 1:
					{
						short result = Sub(FetchOperandW(src_mode, src_register), (short)this.m_D[dest_register], true, true, false);
						Helpers.Inject(ref this.m_D[dest_register], result);
						this.m_Cycles += 4 + Helpers.EACalcTimeBW(src_mode, src_register);
						return;
					}
				case 2:
					{
						int result = Sub(FetchOperandL(src_mode, src_register), this.m_D[dest_register], true, true, false);
						this.m_D[dest_register] = (int)result;
						this.m_Cycles += 6 + Helpers.EACalcTimeL(src_mode, src_register);
						return;
					}
			}
		}

		private void SUBA()
		{
			int dest_register = (this.m_IR >> 9) & 0x7;
			int opmode = (this.m_IR >> 6) & 0x7;
			int src_mode = (this.m_IR >> 3) & 0x7;
			int src_register = this.m_IR & 0x7;

			switch (opmode)
			{
				case 3: // W
					{
						int operand = FetchOperandW(src_mode, src_register); // Sign-extended
						this.m_A[dest_register] -= operand;
						this.m_Cycles += 8 + Helpers.EACalcTimeBW(src_mode, src_register);
						break;
					}
				case 7: // L
					{
						int operand = FetchOperandL(src_mode, src_register);
						this.m_A[dest_register] -= operand;
						this.m_Cycles += 6 + Helpers.EACalcTimeL(src_mode, src_register);
						break;
					}
			}
		}

		private void SUBI()
		{
			int size = (this.m_IR >> 6) & 0x3;
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			switch (size)
			{
				case 0: // B
					{
						sbyte result = Sub((sbyte)FetchW(), PeekOperandB(mode, register), true, true, false);
						SetOperandB(mode, register, result);
						this.m_Cycles += (mode == 0) ? 8 : 12 + Helpers.EACalcTimeBW(mode, register);
						return;
					}
				case 1: // W
					{
						short result = Sub(FetchW(), PeekOperandW(mode, register), true, true, false);
						SetOperandW(mode, register, result);
						this.m_Cycles += (mode == 0) ? 8 : 12 + Helpers.EACalcTimeBW(mode, register);
						return;
					}
				case 2: // L
					{
						int result = Sub(FetchL(), PeekOperandL(mode, register), true, true, false);
						SetOperandL(mode, register, result);
						this.m_Cycles += (mode == 0) ? 16 : 20 + Helpers.EACalcTimeL(mode, register);
						return;
					}
			}
		}

		private void SUBQ() // Add Quick
		{
			int data = (this.m_IR >> 9) & 0x7;
			int size = (this.m_IR >> 6) & 0x3;
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			// With data, 0 means 8
			data = (data == 0) ? 8 : data;

			switch (size)
			{
				case 0: // B
					{
						if (mode == 1)
						{
							throw new ArgumentException("Byte operation not allowed on address registers");
						}
						sbyte result = Sub((sbyte)data, PeekOperandB(mode, register), (mode != 1), true, false);
						SetOperandB(mode, register, result);
						switch (mode)
						{
							case 0: this.m_Cycles += 4; break;
							case 1: this.m_Cycles += 8; break;
							default: this.m_Cycles += 8 + Helpers.EACalcTimeBW(mode, register); break;
						}
						return;
					}
				case 1: // W
					{
						if (mode == 1)
						{
							int result = Sub(data, PeekOperandL(mode, register), false, true, false);
							SetOperandL(mode, register, result);
						}
						else
						{
							short result = Sub((short)data, PeekOperandW(mode, register), true, true, false);
							SetOperandW(mode, register, result);
						}
						switch (mode)
						{
							case 0: this.m_Cycles += 4; break;
							case 1: this.m_Cycles += 8; break;
							default: this.m_Cycles += 8 + Helpers.EACalcTimeBW(mode, register); break;
						}
						return;
					}
				case 2: // L
					{
						int result = Sub(data, PeekOperandL(mode, register), (mode != 1), true, false);
						SetOperandL(mode, register, result);
						switch (mode)
						{
							case 0: this.m_Cycles += 8; break;
							case 1: this.m_Cycles += 8; break;
							default: this.m_Cycles += 12 + Helpers.EACalcTimeL(mode, register); break;
						}
						return;
					}
			}
		}

		private void SUBX()
		{
			int regRx = (this.m_IR >> 9) & 0x7;
			int size = (this.m_IR >> 6) & 0x3;
			int rm = (this.m_IR >> 3) & 0x1;
			int regRy = this.m_IR & 0x7;

			switch (size)
			{
				case 0:
					{
						if (rm == 0)
						{
							this.m_D[regRy] = Sub((sbyte)this.m_D[regRx], (sbyte)this.m_D[regRy], true, true, true);
							this.m_Cycles += 4;
						}
						else
						{
							WriteB(this.m_A[regRy], Sub(ReadB(this.m_A[regRx]), ReadB(this.m_A[regRy]), true, true, true));
							this.m_Cycles += 18;
						}
						return;
					}
				case 1:
					{
						if (rm == 0)
						{
							this.m_D[regRy] = Sub((short)this.m_D[regRx], (short)this.m_D[regRy], true, true, true);
							this.m_Cycles += 4;
						}
						else
						{
							WriteW(this.m_A[regRy], Sub(ReadW(this.m_A[regRx]), ReadW(this.m_A[regRy]), true, true, true));
							this.m_Cycles += 18;
						}
						return;
					}
				case 2:
					{
						if (rm == 0)
						{
							this.m_D[regRy] = Sub(this.m_D[regRx], this.m_D[regRy], true, true, true);
							this.m_Cycles += 8;
						}
						else
						{
							WriteL(this.m_A[regRy], Sub(ReadB(this.m_A[regRx]), ReadB(this.m_A[regRy]), true, true, true));
							this.m_Cycles += 30;
						}
						return;
					}
			}
		}
	}
}
