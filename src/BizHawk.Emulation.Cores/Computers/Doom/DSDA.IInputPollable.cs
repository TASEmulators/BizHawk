using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA : IInputPollable
	{
		public int LagCount { get; set; }

		public bool IsLagFrame { get; set; }

		public IInputCallbackSystem InputCallbacks => _inputCallbacks;

		private readonly CInterface.input_cb _inputCallback;

		private readonly InputCallbackSystem _inputCallbacks = [ ];

		private void InputCallback()
		{
			InputCallbacks.Call();
			IsLagFrame = false;
		}
	}
}
