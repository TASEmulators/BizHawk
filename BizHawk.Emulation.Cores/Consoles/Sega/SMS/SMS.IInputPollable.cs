using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public sealed partial class SMS : IInputPollable
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

		public IInputCallbackSystem InputCallbacks { get; private set; }

		private int _lagCount = 0;
		private bool _lagged = true;
		private bool _isLag = false;
	}
}
