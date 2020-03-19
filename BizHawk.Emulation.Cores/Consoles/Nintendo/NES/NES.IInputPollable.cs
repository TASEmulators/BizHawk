using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public partial class NES : IInputPollable
	{
		public int LagCount
		{
			get => _lagcount;
			set => _lagcount = value;
		}

		public bool IsLagFrame
		{
			get => islag;
			set => islag = value;
		}

		public IInputCallbackSystem InputCallbacks => _inputCallbacks;

		private int _lagcount;
		private bool lagged = true;
		private bool islag = false;

		private readonly InputCallbackSystem _inputCallbacks = new InputCallbackSystem();
	}
}
