using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBAHawk : IInputPollable
	{
		private readonly LibmGBA.InputCallback InputCallback;
		private readonly LibmGBA.RumbleCallback RumbleCallback;
		private IController _controller;

		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }
		public IInputCallbackSystem InputCallbacks { get; } = new InputCallbackSystem();

		private void SetRumble(int value)
			=> _controller.SetHapticChannelStrength("Rumble", value);
	}
}
