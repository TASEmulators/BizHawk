using System;
using System.Collections.Generic;
using System.Text;

namespace MC68000
{
	public partial class MC68K
	{
		#region AND Helpers
		private sbyte And(sbyte a, sbyte b)
		{
			sbyte result = (sbyte)(a & b);

			this.V = this.C = false;
			this.N = result < 0;
			this.Z = result == 0;

			return result;
		}

		private short And(short a, short b)
		{
			short result = (short)(a & b);

			this.V = this.C = false;
			this.N = result < 0;
			this.Z = result == 0;

			return result;
		}

		private int And(int a, int b)
		{
			int result = (int)(a & b);

			this.V = this.C = false;
			this.N = result < 0;
			this.Z = result == 0;

			return result;
		}
		#endregion AND Helpers

		private void AND_Dest()
		{
			int src_register = (this.m_IR >> 9) & 0x7;
			int size = (this.m_IR >> 6) & 0x3;
			int dest_mode = (this.m_IR >> 3) & 0x7;
			int dest_register = this.m_IR & 0x7;

			switch (size)
			{
				case 0: // B
					{
						sbyte result = And((sbyte)this.m_D[src_register], PeekOperandB(dest_mode, dest_register));
						SetOperandB(dest_mode, dest_register, result);
						this.m_Cycles += 4 + Helpers.EACalcTimeBW(dest_mode, dest_register);
						return;
					}
				case 1: // W
					{
						short result = And((short)this.m_D[src_register], PeekOperandW(dest_mode, dest_register));
						SetOperandW(dest_mode, dest_register, result);
						this.m_Cycles += 4 + Helpers.EACalcTimeBW(dest_mode, dest_register);
						return;
					}
				case 2: // L
					{
						int result = And(this.m_D[src_register], PeekOperandL(dest_mode, dest_register));
						SetOperandL(dest_mode, dest_register, result);
						this.m_Cycles += 6 + Helpers.EACalcTimeL(dest_mode, dest_register);
						return;
					}
			}
		}

		private void AND_Source()
		{
			int dest_register = (this.m_IR >> 9) & 0x7;
			int size = (this.m_IR >> 6) & 0x3;
			int src_mode = (this.m_IR >> 3) & 0x7;
			int src_register = this.m_IR & 0x7;

			switch (size)
			{
				case 0: // B
					{
						sbyte result = And((sbyte)this.m_D[dest_register], FetchOperandB(src_mode, src_register));
						Helpers.Inject(ref this.m_D[dest_register], result);
						this.m_Cycles += 8 + Helpers.EACalcTimeBW(src_mode, src_register);
						return;
					}
				case 1: // W
					{
						short result = And((short)this.m_D[dest_register], FetchOperandW(src_mode, src_register));
						Helpers.Inject(ref this.m_D[dest_register], result);
						this.m_Cycles += 8 + Helpers.EACalcTimeBW(src_mode, src_register);
						return;
					}
				case 2: // L
					{
						int result = And(this.m_D[dest_register], FetchOperandL(src_mode, src_register));
						this.m_D[dest_register] = result;
						this.m_Cycles += 12 + Helpers.EACalcTimeL(src_mode, src_register);
						return;
					}
			}
		}

		private void ANDI()
		{
			int size = (this.m_IR >> 6) & 0x3;
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			switch (size)
			{
				case 0: // B
					{
						sbyte result = And((sbyte)FetchW(), PeekOperandB(mode, register));
						SetOperandB(mode, register, result);
						this.m_Cycles += (mode == 0) ? 8 : 12 + Helpers.EACalcTimeBW(mode, register);
						return;
					}
				case 1: // W
					{
						short result = And(FetchW(), PeekOperandW(mode, register));
						SetOperandW(mode, register, result);
						this.m_Cycles += (mode == 0) ? 8 : 12 + Helpers.EACalcTimeBW(mode, register);
						return;
					}
				case 2: // L
					{
						int result = And(FetchL(), PeekOperandL(mode, register));
						SetOperandL(mode, register, result);
						this.m_Cycles += (mode == 0) ? 14 : 20 + Helpers.EACalcTimeL(mode, register);
						return;
					}
			}
		}

		#region EOR Helpers
		private sbyte Eor(sbyte a, sbyte b)
		{
			sbyte result = (sbyte)(a ^ b);

			this.V = this.C = false;
			this.N = result < 0;
			this.Z = result == 0;

			return result;
		}

		private short Eor(short a, short b)
		{
			short result = (short)(a ^ b);

			this.V = this.C = false;
			this.N = result < 0;
			this.Z = result == 0;

			return result;
		}

		private int Eor(int a, int b)
		{
			int result = (int)(a ^ b);

			this.V = this.C = false;
			this.N = result < 0;
			this.Z = result == 0;

			return result;
		}
		#endregion EOR Helpers

		private void EOR()
		{
			int src_register = (this.m_IR >> 9) & 0x7;
			int size = (this.m_IR >> 6) & 0x3;
			int dest_mode = (this.m_IR >> 3) & 0x7;
			int dest_register = this.m_IR & 0x7;

			switch (size)
			{
				case 0: // B
					{
						sbyte result = Eor((sbyte)this.m_D[src_register], PeekOperandB(dest_mode, dest_register));
						SetOperandB(dest_mode, dest_register, result);
						this.m_Cycles += (dest_mode == 0) ? 4 : 8 + Helpers.EACalcTimeBW(dest_mode, dest_register);
						return;
					}
				case 1: // W
					{
						short result = Eor((short)this.m_D[src_register], PeekOperandW(dest_mode, dest_register));
						SetOperandW(dest_mode, dest_register, result);
						this.m_Cycles += (dest_mode == 0) ? 4 : 8 + Helpers.EACalcTimeBW(dest_mode, dest_register);
						return;
					}
				case 2: // L
					{
						int result = Eor(this.m_D[src_register], PeekOperandL(dest_mode, dest_register));
						SetOperandL(dest_mode, dest_register, result);
						this.m_Cycles += (dest_mode == 0) ? 8 : 12 + Helpers.EACalcTimeL(dest_mode, dest_register);
						return;
					}
			}
		}

		private void EORI()
		{
			int size = (this.m_IR >> 6) & 0x3;
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			switch (size)
			{
				case 0: // B
					{
						sbyte result = Eor((sbyte)FetchW(), PeekOperandB(mode, register));
						SetOperandB(mode, register, result);
						this.m_Cycles += (mode == 0) ? 8 : 12 + Helpers.EACalcTimeBW(mode, register);
						return;
					}
				case 1: // W
					{
						short result = Eor(FetchW(), PeekOperandW(mode, register));
						SetOperandW(mode, register, result);
						this.m_Cycles += (mode == 0) ? 8 : 12 + Helpers.EACalcTimeBW(mode, register);
						return;
					}
				case 2: // L
					{
						int result = Eor(FetchL(), PeekOperandL(mode, register));
						SetOperandL(mode, register, result);
						this.m_Cycles += (mode == 0) ? 16 : 20 + Helpers.EACalcTimeL(mode, register);
						return;
					}
			}
		}

		#region OR Helpers
		private sbyte Or(sbyte a, sbyte b)
		{
			sbyte result = (sbyte)(a | b);

			this.V = this.C = false;
			this.N = result < 0;
			this.Z = result == 0;

			return result;
		}

		private short Or(short a, short b)
		{
			short result = (short)(a | b);

			this.V = this.C = false;
			this.N = result < 0;
			this.Z = result == 0;

			return result;
		}

		private int Or(int a, int b)
		{
			int result = (int)(a | b);

			this.V = this.C = false;
			this.N = result < 0;
			this.Z = result == 0;

			return result;
		}
		#endregion OR Helpers

		private void OR_Dest()
		{
			int src_register = (this.m_IR >> 9) & 0x7;
			int size = (this.m_IR >> 6) & 0x3;
			int dest_mode = (this.m_IR >> 3) & 0x7;
			int dest_register = this.m_IR & 0x7;

			switch (size)
			{
				case 0: // B
					{
						sbyte result = Or((sbyte)this.m_D[src_register], PeekOperandB(dest_mode, dest_register));
						SetOperandB(dest_mode, dest_register, result);
						this.m_Cycles += 8 + Helpers.EACalcTimeBW(dest_mode, dest_register);
						return;
					}
				case 1: // W
					{
						short result = Or((short)this.m_D[src_register], PeekOperandW(dest_mode, dest_register));
						SetOperandW(dest_mode, dest_register, result);
						this.m_Cycles += 8 + Helpers.EACalcTimeBW(dest_mode, dest_register);
						return;
					}
				case 2: // L
					{
						int result = Or(this.m_D[src_register], PeekOperandL(dest_mode, dest_register));
						SetOperandL(dest_mode, dest_register, result);
						this.m_Cycles += 12 + Helpers.EACalcTimeL(dest_mode, dest_register);
						return;
					}
			}
		}

		private void OR_Source()
		{
			int dest_register = (this.m_IR >> 9) & 0x7;
			int size = (this.m_IR >> 6) & 0x3;
			int src_mode = (this.m_IR >> 3) & 0x7;
			int src_register = this.m_IR & 0x7;

			switch (size)
			{
				case 0: // B
					{
						sbyte result = Or((sbyte)this.m_D[dest_register], FetchOperandB(src_mode, src_register));
						Helpers.Inject(ref this.m_D[dest_register], result);
						this.m_Cycles += 4 + Helpers.EACalcTimeBW(src_mode, src_register);
						return;
					}
				case 1: // W
					{
						short result = Or((short)this.m_D[dest_register], FetchOperandW(src_mode, src_register));
						Helpers.Inject(ref this.m_D[dest_register], result);
						this.m_Cycles += 4 + Helpers.EACalcTimeBW(src_mode, src_register);
						return;
					}
				case 2: // L
					{
						int result = Or(this.m_D[dest_register], FetchOperandL(src_mode, src_register));
						this.m_D[dest_register] = result;
						this.m_Cycles += 6 + Helpers.EACalcTimeL(src_mode, src_register);
						return;
					}
			}
		}

		private void ORI()
		{
			int size = (this.m_IR >> 6) & 0x3;
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			switch (size)
			{
				case 0: // B
					{
						sbyte result = Or((sbyte)FetchW(), PeekOperandB(mode, register));
						SetOperandB(mode, register, result);
						this.m_Cycles += (mode == 0) ? 8 : 12 + Helpers.EACalcTimeBW(mode, register);
						return;
					}
				case 1: // W
					{
						short result = Or(FetchW(), PeekOperandW(mode, register));
						SetOperandW(mode, register, result);
						this.m_Cycles += (mode == 0) ? 8 : 12 + Helpers.EACalcTimeBW(mode, register);
						return;
					}
				case 2: // L
					{
						int result = Or(FetchL(), PeekOperandL(mode, register));
						SetOperandL(mode, register, result);
						this.m_Cycles += (mode == 0) ? 16 : 20 + Helpers.EACalcTimeL(mode, register);
						return;
					}
			}
		}

		private void NOT()
		{
			int size = (this.m_IR >> 6) & 0x3;
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			long result = 0;
			this.V = this.C = false;
			switch (size)
			{
				case 0:
					result = ~PeekOperandB(mode, register);
					SetOperandB(mode, register, (sbyte)result);
					this.m_Cycles += (mode == 0) ? 4 : 8 + Helpers.EACalcTimeBW(mode, register);
					break;
				case 1:
					result = ~PeekOperandW(mode, register);
					SetOperandW(mode, register, (short)result);
					this.m_Cycles += (mode == 0) ? 4 : 8 + Helpers.EACalcTimeBW(mode, register);
					break;
				case 2:
					result = ~PeekOperandL(mode, register);
					SetOperandL(mode, register, (int)result);
					this.m_Cycles += (mode == 0) ? 6 : 12 + Helpers.EACalcTimeL(mode, register);
					break;
			}
			this.N = result < 0;
			this.Z = result == 0;
		}
	}
}
