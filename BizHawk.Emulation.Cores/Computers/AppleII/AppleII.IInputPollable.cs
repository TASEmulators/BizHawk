using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	public partial class AppleII : IInputPollable
	{
		public int LagCount { get; set; }

		public bool IsLagFrame
		{
			get => _machine.Memory.Lagged;
			set => _machine.Memory.Lagged = value;
		}

		public IInputCallbackSystem InputCallbacks { get; } = new InputCallbackSystem();
	}
}
