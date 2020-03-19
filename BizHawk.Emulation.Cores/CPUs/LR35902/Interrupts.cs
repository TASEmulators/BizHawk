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
						INT_GET, 4, W,
						DEC16, SPl, SPh,
						INT_GET, 3, W,
						WR, SPl, SPh, PCl,
						INT_GET, 2, W,
						IDLE,
						INT_GET, 1, W,
						IDLE,
						INT_GET, 0, W,
						ASGN, PCh, 0,
						IDLE,
						IDLE,
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
						INT_GET, 4, W,
						INT_GET, 3, W,
						INT_GET, 2, W,
						INT_GET, 1, W,
						INT_GET, 0, W,
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
			interrupts_enabled = false;

			int_src = 5;
			int_clear = 0;
		}
	}
}