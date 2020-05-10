using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public partial class GPGX : IInputPollable
	{
		public int LagCount { get; set; }

		public bool IsLagFrame { get; set; }

		public IInputCallbackSystem InputCallbacks => _inputCallbacks;

		private readonly LibGPGX.input_cb _inputCallback;

		private readonly InputCallbackSystem _inputCallbacks = new InputCallbackSystem();

		private void input_callback()
		{
			InputCallbacks.Call();
			IsLagFrame = false;
		}
	}
}
