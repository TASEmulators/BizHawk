using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Calculators
{
	public partial class TI83 : IInputPollable
	{
		private int _lagCount = 0;
		private bool _lagged = true;
		private bool _isLag = false;

		public int LagCount
		{
			get { return _lagCount; }
			set { _lagCount = value; }
		}

		public IInputCallbackSystem InputCallbacks { get; private set; }

		public bool IsLagFrame
		{
			get { return _isLag; }
			set { _isLag = value; }
		}
	}
}
