using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Lynx
{
	public partial class Lynx : IInputPollable
	{
		public int LagCount { get; set; }

		public bool IsLagFrame { get; set; }

		// TODO
		public IInputCallbackSystem InputCallbacks
		{
			[FeatureNotImplemented]
			get => throw new NotImplementedException();
		}
	}
}
