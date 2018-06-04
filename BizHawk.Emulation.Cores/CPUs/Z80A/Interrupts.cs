using System;

namespace BizHawk.Emulation.Cores.Components.Z80A
{
	public partial class Z80A
	{
		private bool iff1;
		public bool IFF1 { get { return iff1; } set { iff1 = value; } }

		private bool iff2;
		public bool IFF2 { get { return iff2; } set { iff2 = value; } }

		private bool nonMaskableInterrupt;
		public bool NonMaskableInterrupt
		{
			get { return nonMaskableInterrupt; }
			set { if (value && !nonMaskableInterrupt) NonMaskableInterruptPending = true; nonMaskableInterrupt = value; }
		}

		private bool nonMaskableInterruptPending;
		public bool NonMaskableInterruptPending { get { return nonMaskableInterruptPending; } set { nonMaskableInterruptPending = value; } }

		private int interruptMode;
		public int InterruptMode
		{
			get { return interruptMode; }
			set { if (value < 0 || value > 2) throw new ArgumentOutOfRangeException(); interruptMode = value; }
		}

		public Action IRQCallback = delegate () { };
		public Action NMICallback = delegate () { };

		private void NMI_()
		{
			cur_instr = new ushort[]
						{DEC16, SPl, SPh,
						TR, ALU, PCl,
						WAIT,
						WR_DEC, SPl, SPh, PCh,
						TR16, PCl, PCh, NMI_V, ZERO,
						WAIT,
						WR, SPl, SPh, ALU,
						IDLE,						
						WAIT,
						OP_F,
						OP };

			BUSRQ = new ushort[] { 0, SPl, 0, 0, SPl, 0, 0, PCl, 0, 0, 0 };
		}

		// Mode 0 interrupts only take effect if a CALL or RST is on the data bus
		// Otherwise operation just continues as normal
		// For now assume a NOP is on the data bus, in which case no stack operations occur

		//NOTE: TODO: When a CALL is present on the data bus, adjust WZ accordingly 
		private void INTERRUPT_0(ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						WAIT,
						RD_INC, ALU, PCl, PCh,
						IDLE,
						WAIT,
						OP_F,
						OP };

			BUSRQ = new ushort[] { PCl, 0, 0, PCl, 0, 0, 0 };
		}

		// Just jump to $0038
		private void INTERRUPT_1()
		{
			cur_instr = new ushort[]
						{IDLE,
						ASGN, ALU, PCl,
						DEC16, SPl, SPh,
						IDLE,
						WAIT,
						WR_DEC, SPl, SPh, PCh,
						TR16, PCl, PCh, IRQ_V, ZERO,
						WAIT,
						WR, SPl, SPh, ALU,
						IDLE,
						WAIT,
						OP_F,
						OP };

			BUSRQ = new ushort[] { 0, 0, 0, SPl, 0, 0, SPl, 0, 0, PCl, 0, 0, 0 };
		}

		// Interrupt mode 2 uses the I vector combined with a byte on the data bus
		private void INTERRUPT_2()
		{
			cur_instr = new ushort[]
						{FTCH_DB,
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
						RD, PCh, Z, W,
						IDLE,
						WAIT,
						OP_F,
						OP };

			BUSRQ = new ushort[] { 0, 0, 0, SPl, 0, 0, SPl, 0, 0, Z, 0, 0, Z, 0 ,0 ,PCl, 0, 0, 0 };
		}

		private static ushort[] INT_vectors = new ushort[] {0x40, 0x48, 0x50, 0x58, 0x60};

		private void ResetInterrupts()
		{
			IFF1 = false;
			IFF2 = false;
			NonMaskableInterrupt = false;
			NonMaskableInterruptPending = false;
			FlagI = false;
			InterruptMode = 1;
		}
	}
}