using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.ColecoVision
{
	public partial class ColecoVision : IInputPollable
	{
		public int LagCount
		{
			get { return _lagCount; }
			set { _lagCount = value; }
		}

		public bool IsLagFrame
		{
			get { return _isLag; }
			set { _isLag = value; }
		}

		public IInputCallbackSystem InputCallbacks
		{
			[FeatureNotImplemented]
			get { throw new NotImplementedException(); }
		}

		private int _lagCount = 0;
		private bool _isLag = true;
	}
}
