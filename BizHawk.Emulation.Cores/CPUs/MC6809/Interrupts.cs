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
		public bool I_use; // in halt mode, the I flag is checked earlier then when deicision to IRQ is taken
		public bool skip_once;

		private void ResetInterrupts()
		{
			I_use = false;
			skip_once = false;
		}
	}
}