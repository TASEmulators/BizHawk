namespace BizHawk.Emulation.Cores.Components.LR35902
{
	public partial class LR35902
	{
		private void INTERRUPT_()
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						IDLE,
						IDLE,
						IDLE,
						IDLE,
						IDLE,
						IDLE,
						DEC16, SPl, SPh,
						IDLE,
						WR, SPl, SPh, PCh,
						ASGN, PCh, 0,
						DEC16, SPl, SPh,
						INT_GET, 1, W,
						WR, SPl, SPh, PCl,
						IRQ_CLEAR,
						IDLE,
						TR, PCl, W,
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
						IDLE,
						IDLE,
						IDLE,
						IDLE,
						IDLE,
						IDLE,
						IDLE,
						IDLE,
						DEC16, SPl, SPh,
						IDLE,
						WR, SPl, SPh, PCh,
						ASGN, PCh, 0,
						DEC16, SPl, SPh,
						INT_GET, 1, W,
						WR, SPl, SPh, PCl,
						IRQ_CLEAR,
						IDLE,
						TR, PCl, W,
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
		public bool Halt_bug_4;
		public bool Halt_bug_5;

		private void ResetInterrupts()
		{
			I_use = false;
			skip_once = false;
			Halt_bug_2 = false;
			Halt_bug_3 = false;
			Halt_bug_4 = false;
			Halt_bug_5 = false;
			interrupts_enabled = false;

			int_src = 5;
			int_clear = 0;
		}
	}
}