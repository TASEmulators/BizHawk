using System;

namespace BizHawk.Emulation.Common.Components.LR35902
{
	public partial class LR35902
	{
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

		private void INTERRUPT_GBC_NOP()
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
						IDLE,
						IDLE,
						IDLE,
						IDLE,
						OP };
		}

		private static ushort[] INT_vectors = new ushort[] {0x40, 0x48, 0x50, 0x58, 0x60, 0x00};

		public ushort int_src;
		public int stop_time;
		public bool stop_check;
		public bool is_GBC; // GBC automatically adds a NOP to avoid the HALT bug (according to Sinimas)
		public bool I_use; // in halt mode, the I flag is checked earlier then when deicision to IRQ is taken
		public bool skip_once;
		public bool Halt_bug_2;
		public bool Halt_bug_3;

		private void ResetInterrupts()
		{
			I_use = false;
			skip_once = false;
			Halt_bug_2 = false;
			Halt_bug_3 = false;
			NonMaskableInterrupt = false;
			NonMaskableInterruptPending = false;
			InterruptMode = 1;
		}
	}
}