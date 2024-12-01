namespace BizHawk.Emulation.Cores.Components.I8048
{
	public partial class I8048
	{
		private void IRQ_(ushort src)
		{
			if (src == 0)
			{
				Regs[ALU] = 0x0003;
			}
			else
			{
				Regs[ALU] = 0x0007;
			}

			PopulateCURINSTR(DM,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							PUSH,
							IDLE,
							TR, PC, ALU);

			IRQS = 10;
		}

		public bool IRQPending;
		public bool TIRQPending;
		public bool IntEn;
		public bool TimIntEn;
		public bool INT_MSTR;
		public bool test_pass;

		public Action IRQCallback = () => {};

		private void ResetInterrupts()
		{
			IntEn = false;
			TimIntEn = false;
			INT_MSTR = true;
			IRQPending = false;
			TIRQPending = false;
		}
	}
}