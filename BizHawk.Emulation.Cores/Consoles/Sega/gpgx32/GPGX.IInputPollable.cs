using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public partial class GPGX : IInputPollable
	{
		public int LagCount { get; set; }

		public bool IsLagFrame { get; set; }

		public IInputCallbackSystem InputCallbacks { get { return _inputCallbacks; } }

		private LibGPGX.input_cb InputCallback = null;

		private readonly InputCallbackSystem _inputCallbacks = new InputCallbackSystem();

		private void input_callback()
		{
			InputCallbacks.Call();
			IsLagFrame = false;
		}
	}
}
