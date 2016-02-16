using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	public partial class AppleII : IInputPollable
	{
		public int LagCount { get; set; }

		public bool IsLagFrame
		{
			get { return _machine.Lagged; }
			set { _machine.Lagged = value; }
		}

		public IInputCallbackSystem InputCallbacks { get; private set; }
	}
}
