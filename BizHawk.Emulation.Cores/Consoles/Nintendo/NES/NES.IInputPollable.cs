using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public partial class NES : IInputPollable
	{
		public int LagCount
		{
			get { return _lagcount; }
			set { _lagcount = value; }
		}

		public bool IsLagFrame
		{
			get { return islag; }
			set { islag = value; }
		}

		public IInputCallbackSystem InputCallbacks
		{
			get { return _inputCallbacks; }
		}

		private int _lagcount;
		private bool lagged = true;
		private bool islag = false;

		private readonly InputCallbackSystem _inputCallbacks = new InputCallbackSystem();
	}
}
