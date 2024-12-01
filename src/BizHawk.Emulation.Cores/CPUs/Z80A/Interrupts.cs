namespace BizHawk.Emulation.Cores.Components.Z80A
{
	public partial class Z80A<TLink>
	{
		private bool iff1;
		public bool IFF1
		{
			get => iff1;
			set => iff1 = value;
		}

		private bool iff2;
		public bool IFF2
		{
			get => iff2;
			set => iff2 = value;
		}

		private bool nonMaskableInterrupt;
		public bool NonMaskableInterrupt
		{
			get => nonMaskableInterrupt;
			set { if (value && !nonMaskableInterrupt) nonMaskableInterruptPending = true; nonMaskableInterrupt = value; }
		}

		private bool nonMaskableInterruptPending;

		private int interruptMode;
		public int InterruptMode
		{
			get => interruptMode;
			set
			{
				if (value is < 0 or > 2) throw new ArgumentOutOfRangeException(paramName: nameof(value), value, message: "invalid interrupt mode");
				interruptMode = value;
			}
		}

		private void NMI_()
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						IDLE,
						IDLE,
						DEC16, SPl, SPh,
						TR, ALU, PCl,
						WAIT,
						WR_DEC, SPl, SPh, PCh,
						TR16, PCl, PCh, NMI_V, ZERO,
						WAIT,
						WR, SPl, SPh, ALU);

			PopulateBUSRQ(0, 0, 0, 0, 0, SPh, 0, 0, SPh, 0, 0);
			PopulateMEMRQ(0, 0, 0, 0, 0, SPh, 0, 0, SPh, 0, 0);
			IRQS = 11;
		}

		// Mode 0 interrupts only take effect if a CALL or RST is on the data bus
		// Otherwise operation just continues as normal
		// For now assume a NOP is on the data bus, in which case no stack operations occur

		//NOTE: TODO: When a CALL is present on the data bus, adjust WZ accordingly 
		private void INTERRUPT_0(ushort src)
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						IORQ,
						WAIT,
						IDLE,
						WAIT,
						RD_INC, ALU, PCl, PCh);

			PopulateBUSRQ(0, 0, 0, 0, PCh, 0, 0);
			PopulateMEMRQ(0, 0, 0, 0, PCh, 0, 0);
			IRQS = 7;
		}

		// Just jump to $0038
		private void INTERRUPT_1()
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						IORQ,
						WAIT,
						IDLE,
						TR, ALU, PCl,
						DEC16, SPl, SPh,
						IDLE,
						WAIT,
						WR_DEC, SPl, SPh, PCh,
						TR16, PCl, PCh, IRQ_V, ZERO,
						WAIT,
						WR, SPl, SPh, ALU);

			PopulateBUSRQ(0, 0, 0, 0, I, 0, 0, SPh, 0, 0, SPh, 0, 0);
			PopulateMEMRQ(0, 0, 0, 0, I, 0, 0, SPh, 0, 0, SPh, 0, 0);
			IRQS = 13;
		}

		// Interrupt mode 2 uses the I vector combined with a byte on the data bus
		private void INTERRUPT_2()
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						IORQ,
						WAIT,
						FTCH_DB,
						IDLE,
						DEC16, SPl, SPh,
						TR16, Z, W, DB, I,
						WAIT,
						WR_DEC, SPl, SPh, PCh,
						IDLE,
						WAIT,
						WR, SPl, SPh, PCl,
						IDLE,
						WAIT,
						RD_INC, PCl, Z, W,
						IDLE,
						WAIT,
						RD, PCh, Z, W);

			PopulateBUSRQ(0, 0, 0, 0, I, 0, 0, SPh, 0, 0, SPh, 0, 0, W, 0, 0, W, 0, 0);
			PopulateMEMRQ(0, 0, 0, 0, I, 0, 0, SPh, 0, 0, SPh, 0, 0, W, 0, 0, W, 0, 0);
			IRQS = 19;
		}

		private void ResetInterrupts()
		{
			IFF1 = false;
			IFF2 = false;
			NonMaskableInterrupt = false;
			nonMaskableInterruptPending = false;
			FlagI = false;
			InterruptMode = 1;
		}
	}
}