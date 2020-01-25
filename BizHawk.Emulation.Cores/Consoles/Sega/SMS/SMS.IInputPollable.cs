using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public partial class SMS : IInputPollable
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

		public IInputCallbackSystem InputCallbacks { get; private set; }

		public int _lagCount = 0;
		public bool _lagged = true;
		public bool _isLag = false;
	}
}
