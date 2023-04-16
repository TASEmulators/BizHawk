using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBAHawk : IInputPollable
	{
		private readonly LibmGBA.InputCallback InputCallback;
		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }
		public IInputCallbackSystem InputCallbacks { get; } = new InputCallbackSystem();
	}
}
