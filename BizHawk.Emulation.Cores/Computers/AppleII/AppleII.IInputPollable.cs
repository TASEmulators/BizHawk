using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	public partial class AppleII : IInputPollable
	{
		private int _lagcount;
		public int LagCount
		{
			get => _lagcount;
			set => _lagcount = value;
		}

		public bool IsLagFrame
		{
			get => _machine.Memory.Lagged;
			set => _machine.Memory.Lagged = value;
		}

		public IInputCallbackSystem InputCallbacks { get; } = new InputCallbackSystem();
	}
}
