using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public partial class Atari2600 : IInputPollable
	{
		public int LagCount
		{
			get { return _lagcount; }
			set { _lagcount = value; }
		}

		public bool IsLagFrame
		{
			get { return _islag; }
			set { _islag = value; }
		}

		public IInputCallbackSystem InputCallbacks { get; private set; }

		private bool _islag = true;
		private int _lagcount;
	}
}
