using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	public partial class C64 : IInputPollable
	{
		public bool IsLagFrame { get; set; }
        public int LagCount { get; set; }

        [SaveState.DoNotSave]
        public IInputCallbackSystem InputCallbacks { get; private set; }
	}
}
