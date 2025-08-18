﻿using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA : IInputPollable
	{
		private readonly InputCallbackSystem _inputCallbacks = [ ];
		public int LagCount { get; set; } = 0;
		public bool IsLagFrame { get; set; } = false;
		public IInputCallbackSystem InputCallbacks => _inputCallbacks;
	}
}
