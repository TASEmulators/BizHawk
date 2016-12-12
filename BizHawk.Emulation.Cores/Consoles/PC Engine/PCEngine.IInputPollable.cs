using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.PCEngine
{
	public sealed partial class PCEngine : IInputPollable
	{
		public int LagCount
		{
			get { return _lagCount; }
			set { _lagCount = value; }
		}

		public bool IsLagFrame
		{
			get { return _isLag; }
			set { _isLag = value; }
		}

		public IInputCallbackSystem InputCallbacks
		{
			get { return _inputCallbacks; }
		}

		private readonly InputCallbackSystem _inputCallbacks = new InputCallbackSystem();
		private int _lagCount;
		private bool _lagged = true;
		private bool _isLag = false;
	}
}
