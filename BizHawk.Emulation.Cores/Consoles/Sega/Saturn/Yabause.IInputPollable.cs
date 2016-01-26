using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.Saturn
{
	public partial class Yabause : IInputPollable
	{
		public int LagCount { get; set; }

		public bool IsLagFrame { get; set; }

		// TODO: optimize managed to unmanaged using the ActiveChanged event
		public IInputCallbackSystem InputCallbacks
		{
			[FeatureNotImplemented]get { return _inputCallbacks; }
		}

		private readonly InputCallbackSystem _inputCallbacks = new InputCallbackSystem();
	}
}
