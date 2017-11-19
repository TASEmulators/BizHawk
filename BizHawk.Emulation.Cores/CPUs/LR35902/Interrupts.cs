using System;

namespace BizHawk.Emulation.Common.Components.LR35902
{
	public partial class LR35902
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

		private void INTERRUPT_()
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						IDLE,
						IDLE,
						DEC16, SPl, SPh,
						IDLE,
						WR, SPl, SPh, PCh,
						IDLE,
						INT_GET, W,// NOTE: here is where we check for a cancelled IRQ
						DEC16, SPl, SPh,
						WR, SPl, SPh, PCl,
						IDLE,
						IDLE,
						IDLE,
						IDLE,
						TR, PCl, W,
						ASGN, PCh, 0,
						IDLE,
						OP };
		}

		private static ushort[] INT_vectors = new ushort[] {0x40, 0x48, 0x50, 0x58, 0x60, 0x00};

		public ushort int_src;

		private void ResetInterrupts()
		{
			IFF1 = false;
			IFF2 = false;
			NonMaskableInterrupt = false;
			NonMaskableInterruptPending = false;
			InterruptMode = 1;
		}
	}
}