using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Atari.Stella;

namespace BizHawk.Emulation.Cores.Atari.Stella
{
	public partial class Stella : IInputPollable
	{
		public int LagCount { get; set; }

		public bool IsLagFrame { get; set; }

		public IInputCallbackSystem InputCallbacks => _inputCallbacks;

		private readonly CInterface.input_cb _inputCallback;

		private readonly InputCallbackSystem _inputCallbacks = new InputCallbackSystem();

		private void input_callback()
		{
			InputCallbacks.Call();
			IsLagFrame = false;
		}
	}
}
