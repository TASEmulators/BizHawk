namespace BizHawk.Emulation.Cores.Components.MC6809
{
	public partial class MC6809
	{
		private void IRQ_()
		{
			Regs[ADDR] = 0xFFF8;
			PopulateCURINSTR(IDLE,
							SET_E,
							DEC16, SP,
							WR_DEC_LO, SP, PC,
							WR_DEC_HI, SP, PC,
							WR_DEC_LO, SP, US,
							WR_DEC_HI, SP, US,
							WR_DEC_LO, SP, Y,
							WR_DEC_HI, SP, Y,
							WR_DEC_LO, SP, X,
							WR_DEC_HI, SP, X,
							WR_DEC_LO, SP, DP,
							WR_DEC_LO, SP, B,
							WR_DEC_LO, SP, A,
							WR, SP, CC,
							SET_I,
							RD_INC, ALU, ADDR,
							RD_INC, ALU2, ADDR,
							SET_ADDR, PC, ALU, ALU2);

			IRQS = 19;
		}

		private void FIRQ_()
		{
			Regs[ADDR] = 0xFFF6;
			PopulateCURINSTR(IDLE,
							CLR_E,
							DEC16, SP,
							WR_DEC_LO, SP, PC,
							WR_DEC_HI, SP, PC,
							WR, SP, CC,
							SET_F_I,
							RD_INC, ALU, ADDR,
							RD_INC, ALU2, ADDR,
							SET_ADDR, PC, ALU, ALU2);

			IRQS = 10;
		}

		private void NMI_()
		{
			Regs[ADDR] = 0xFFFC;
			PopulateCURINSTR(IDLE,
							SET_E,
							DEC16, SP,
							WR_DEC_LO, SP, PC,
							WR_DEC_HI, SP, PC,
							WR_DEC_LO, SP, US,
							WR_DEC_HI, SP, US,
							WR_DEC_LO, SP, Y,
							WR_DEC_HI, SP, Y,
							WR_DEC_LO, SP, X,
							WR_DEC_HI, SP, X,
							WR_DEC_LO, SP, DP,
							WR_DEC_LO, SP, B,
							WR_DEC_LO, SP, A,
							WR, SP, CC,
							SET_F_I,
							RD_INC, ALU, ADDR,
							RD_INC, ALU2, ADDR,
							SET_ADDR, PC, ALU, ALU2);

			IRQS = 19;
		}

		public bool NMIPending;
		public bool FIRQPending;
		public bool IRQPending;
		public bool IN_SYNC;

		public Action IRQCallback = () => {};
		public Action FIRQCallback = () => {};
		public Action NMICallback = () => {};

		private void ResetInterrupts()
		{
			IN_SYNC = false;
		}
	}
}