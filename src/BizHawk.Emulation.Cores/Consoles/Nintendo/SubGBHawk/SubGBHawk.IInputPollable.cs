using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SubGBHawk
{
	public partial class SubGBHawk : IInputPollable
	{
		public int LagCount
		{
			get => _lagCount;
			set => _lagCount = value;
		}

		public bool IsLagFrame
		{
			get => _isLag;
			set => _isLag = value;
		}

		public IInputCallbackSystem InputCallbacks => _GBCore.InputCallbacks;

		public bool _isLag = true;
		private int _lagCount;
	}
}
