using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	public partial class C64 : IInputPollable
	{
		public bool IsLagFrame
		{
			get { return _islag; }
		}

		public int LagCount
		{
			get { return _lagcount; }
			set { _lagcount = value; }
		}

		public IInputCallbackSystem InputCallbacks { get; private set; }

		private bool _islag = true;
		private int _lagcount = 0;
	}
}
