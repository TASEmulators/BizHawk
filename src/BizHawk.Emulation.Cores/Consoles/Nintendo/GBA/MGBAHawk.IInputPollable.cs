using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBAHawk : IInputPollable
	{
		private readonly LibmGBA.InputCallback InputCallback;
		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }
		private void InputCb()
		{
			// most things are already handled in the core, this is just for event.oninputpoll()
			InputCallbacks.Call();
		}
		private InputCallbackSystem _inputCallbacks = new InputCallbackSystem();
		public IInputCallbackSystem InputCallbacks => _inputCallbacks;
	}
}
