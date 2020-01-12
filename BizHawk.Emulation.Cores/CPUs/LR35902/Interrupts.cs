using System;

namespace BizHawk.Emulation.Cores.Components.LR35902
{
	public partial class LR35902
	{
		private void INTERRUPT_()
		{
			cur_instr = new ushort[]
						{IDLE,
						DEC16, SPl, SPh,
						IDLE,
						WR, SPl, SPh, PCh,
						IDLE,
						DEC16, SPl, SPh,
						IDLE,
						WR, SPl, SPh, PCl,
						IDLE,
						IDLE,
						IDLE,
						IDLE,
						IDLE,
						ASGN, PCh, 0,
						IDLE,
						INT_GET, W,// NOTE: here is where we check for a cancelled IRQ
						TR, PCl, W,
						IRQ_CLEAR,
						IDLE,
						OP };
		}

		private void INTERRUPT_GBC_NOP()
		{
			cur_instr = new ushort[]
						{IDLE,
						DEC16, SPl, SPh,
						IDLE,
						WR, SPl, SPh, PCh,
						IDLE,
						DEC16, SPl, SPh,
						IDLE,
						WR, SPl, SPh, PCl,
						IDLE,
						IDLE,
						IDLE,
						IDLE,
						IDLE,
						IDLE,
						IDLE,
						IDLE,
						INT_GET, W,// NOTE: here is where we check for a cancelled IRQ
						IDLE,
						TR, PCl, W,
						IDLE,
						ASGN, PCh, 0,
						IRQ_CLEAR,
						IDLE,				
						OP };
		}

		private static ushort[] INT_vectors = new ushort[] {0x40, 0x48, 0x50, 0x58, 0x60, 0x00};

		public ushort int_src;
		public byte int_clear;
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
		}
	}
}