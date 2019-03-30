using System;

namespace BizHawk.Emulation.Common.Components.MC6809
{
	public partial class MC6809
	{
		private void INTERRUPT_()
		{

		}

		private void INTERRUPT_GBC_NOP()
		{

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
		}
	}
}