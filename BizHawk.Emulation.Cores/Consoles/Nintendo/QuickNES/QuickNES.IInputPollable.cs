using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES
{
	public partial class QuickNES : IInputPollable
	{
		public int LagCount { get; set; }
		public bool IsLagFrame { get; private set; }

		public IInputCallbackSystem InputCallbacks
		{
			[FeatureNotImplemented]
			get { throw new NotImplementedException(); }
		}
	}
}
