using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES
{
	public partial class QuickNES : IInputPollable
	{
		public int LagCount { get; set; }

		public bool IsLagFrame { get; set; }

		public IInputCallbackSystem InputCallbacks => _inputCallbacks;

		private readonly LibQuickNES.input_cb _inputCallback;

		private readonly InputCallbackSystem _inputCallbacks = [ ];

		private void InputCallback()
		{
			InputCallbacks.Call();
		}
	}
}
