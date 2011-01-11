using System;
using System.Collections.Generic;
using System.Diagnostics;

// BIG NOTICE!!!!!!

// This is the 68000 core from Sega360.
// IT IS GPL! It is a starter core so I can work on Genesis hardware emulation first.
// This core MUST BE, and WILL BE replaced with new code at a later date.

namespace MC68000
{
	public sealed partial class MC68K
	{
		public  int					m_PC;
		private int					m_SR;
		private ushort				m_IR;

		public int[]				m_D = new int[8];
		public int[]				m_A = new int[8];

		private int					m_Usp;
		private int					m_Ssp;

		private IMemoryController	m_Controller;

		public  long 				m_Cycles;

		private int					m_InterruptFlag;

		public MC68K(IMemoryController controller)
		{
			this.m_Controller = controller;
			this.BuildOpTable();
		}

		public void Interrupt(int vector)
		{
			this.m_InterruptFlag = vector;
		}

		private void HandleInterrupt()
		{
			int vector = 24 + this.m_InterruptFlag;
			this.m_InterruptFlag = 0;
			Trap(vector);
		}

		public void Reset()
		{
			this.m_SR = 0x2000;
			this.SP = ReadL(0x0);
			this.m_PC = ReadL(0x4);
			this.m_Cycles = 0;
		}

		private class OpHistory
		{
			public int PC;
			public Operation Operation;
			public OpHistory(int pc, Operation op)
			{
				this.PC = pc;
				this.Operation = op;
			}
		}

		List<OpHistory> m_OpList = new List<OpHistory>(200);
		public void Execute(long cycles)
		{
			this.m_Cycles = 0;
			do
			{
//                if (m_PC == 0x02DD8)
                    //m_PC = 0x02DD8;
				if (this.m_Cycles >= cycles)
				{
					return;
				}

				if (this.m_InterruptFlag > 0)
				{
					HandleInterrupt();
				}

				//if (this.m_PC == 0x4752)
				//{ }

				// Fetch
				this.m_IR = (ushort)FetchW();

				// Decode
				Operation op = this.m_OpTable[this.m_IR];

				//if (op == null)
				//{
				//    ShowOpList();
				//}

				//if (m_OpList.Count == m_OpList.Capacity)
				//{
				//    m_OpList.RemoveAt(0);
				//}
				//m_OpList.Add(new OpHistory(this.m_PC - 2, op));

				// Execute
				op();
			} while (true);
		}

        public void Step()
        {
            if (this.m_InterruptFlag > 0)
            {
                HandleInterrupt();
            }

            this.m_IR = (ushort)FetchW();
            Operation op = this.m_OpTable[this.m_IR];
            op();
        }

		private void ShowOpList()
		{
			for (int i = 0; i < m_OpList.Count; i++)
			{
				Console.WriteLine(Convert.ToString(m_OpList[i].PC, 16) + "\t" + m_OpList[i].Operation.Method.Name);
			}
		}

		public bool C // Carry
		{
			get { return (this.m_SR & 1) > 0; }
			set
			{
				if (value) { this.m_SR |= 1; }
				else { this.m_SR &= -2; }
			}
		}

        public bool V // Overflow
		{
			get { return (this.m_SR & 2) > 0; }
			set
			{
				if (value) { this.m_SR |= 2; }
				else { this.m_SR &= -3; }
			}
		}

        public bool Z // Zero
		{
			get { return (this.m_SR & 4) > 0; }
			set
			{
				if (value) { this.m_SR |= 4; }
				else { this.m_SR &= -5; }
			}
		}

        public bool N // Negative
		{
			get { return (this.m_SR & 8) > 0; }
			set
			{
				if (value) { this.m_SR |= 8; }
				else { this.m_SR &= -9; }
			}
		}

        public bool X // Extend
		{
			get { return (this.m_SR & 16) > 0; }
			set
			{
				if (value) { this.m_SR |= 16; }
				else { this.m_SR &= -17; }
			}
		}

        public bool S // Supervisor Mode
		{
			get { return (this.m_SR & 8192) > 0; }
			set
			{
				if (value) { this.m_SR |= 8192; }
				else { this.m_SR &= -8193; }
			}
		}

		private int SP
		{
			get { return this.m_A[7]; }
			set { this.m_A[7] = value; }
		}

		private bool TestCondition(int cCode)
		{
			switch (cCode)
			{
				case 0x0: // T
					return true;
				case 0x1: // F
					return false;
				case 0x2: // HI
					return !C && !Z;
				case 0x3: // LS
					return C || Z;
				case 0x4: // CC(HI)
					return !C;
				case 0x5: // CS(LO)
					return C;
				case 0x6: // NE
					return !Z;
				case 0x7: // EQ
					return Z;
				case 0x8: // VC
					return !V;
				case 0x9: // VS
					return V;
				case 0xA: // PL
					return !N;
				case 0xB: // MI
					return N;
				case 0xC: // GE
					return N && V || !N && !V;
				case 0xD: // LT
					return N && !V || !N && V;
				case 0xE: // GT
					return N && V && !Z || !N && !V && !Z;
				case 0xF: // LE
					return Z || N && !V || !N && V;
				default:
					throw new ArgumentException("Bad condition code");
			}
		}
	}
}
