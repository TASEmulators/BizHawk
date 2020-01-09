using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.MSX
{
	public partial class MSX : IInputPollable
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

		public int _lagCount = 0;
		public bool _lagged = true;
		public bool _isLag = false;
	}
}
