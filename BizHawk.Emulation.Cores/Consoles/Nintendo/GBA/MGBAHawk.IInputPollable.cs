using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBAHawk : IInputPollable
	{
		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }

		[FeatureNotImplemented]
		public IInputCallbackSystem InputCallbacks => throw new NotImplementedException();
	}
}
