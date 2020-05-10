using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public partial class LibsnesCore : IInputPollable
	{
		public int LagCount { get; set; }

		public bool IsLagFrame { get; set; }

		// TODO: optimize managed to unmanaged using the ActiveChanged event
		public IInputCallbackSystem InputCallbacks { get; } = new InputCallbackSystem();
	}
}
