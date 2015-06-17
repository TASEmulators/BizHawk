using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	public partial class AppleII : IInputPollable
	{
		public int LagCount { get; private set; }

		public bool IsLagFrame
		{
			get { return _machine.Lagged; }
			private set { _machine.Lagged = value; }
		}

		public IInputCallbackSystem InputCallbacks { [FeatureNotImplemented]get; private set; }
	}
}
