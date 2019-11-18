using System;

namespace BizHawk.Emulation.Common.Components.I8048
{
	public partial class I8048
	{
		private void IRQ_()
		{
			Regs[ADDR] = 0xFFF8;
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							RD_INC, ALU, ADDR,
							RD_INC, ALU2, ADDR,
							SET_ADDR, PC, ALU, ALU2);

			IRQS = 19;
		}

		public bool IRQPending;
		public bool TIRQPending;
		public bool IntEn;
		public bool TimIntEn;

		public Action IRQCallback = delegate () { };

		private void ResetInterrupts()
		{
			IntEn = true;
		}
	}
}