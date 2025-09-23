using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.MSX
{
	public partial class MSX : IInputPollable
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

		public IInputCallbackSystem InputCallbacks { get; } = new InputCallbackSystem();

		private int _lagCount = 0;

		internal bool _lagged = true;

		private bool _isLag = false;
	}
}
