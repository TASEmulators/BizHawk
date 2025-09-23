﻿using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.O2Hawk
{
	public partial class O2Hawk : IInputPollable
	{
		public int LagCount
		{
			get => _lagcount;
			set => _lagcount = value;
		}

		public bool IsLagFrame
		{
			get => _islag;
			set => _islag = value;
		}

		public IInputCallbackSystem InputCallbacks { get; } = new InputCallbackSystem();

		private bool _islag = true;

		private int _lagcount;
	}
}
