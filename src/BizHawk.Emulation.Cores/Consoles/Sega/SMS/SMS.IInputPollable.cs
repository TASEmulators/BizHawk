﻿using BizHawk.Emulation.Common;

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

		public IInputCallbackSystem InputCallbacks { get; } = new InputCallbackSystem();

		private int _lagCount = 0;

		private bool _lagged = true;

		internal bool _isLag = false;
	}
}
