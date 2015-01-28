using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class GambatteLink : IInputPollable
	{
		public int LagCount { get; set; }

		public bool IsLagFrame { get; private set; }

		public IInputCallbackSystem InputCallbacks
		{
			get { return _inputCallbacks; }
		}

		private InputCallbackSystem _inputCallbacks = new InputCallbackSystem();
	}
}
