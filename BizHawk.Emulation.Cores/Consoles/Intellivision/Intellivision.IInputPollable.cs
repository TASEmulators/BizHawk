using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Intellivision
{
	public partial class Intellivision : IInputPollable
	{
		public int LagCount
		{
			get
			{
				return _lagcount;
			}

			set
			{
				_lagcount = value;
			}
		}

		public bool IsLagFrame
		{
			get
			{
				return _islag;
			}

			set
			{
				_islag = value;
			}
		}

		public IInputCallbackSystem InputCallbacks { get; } = new InputCallbackSystem();

		private bool _islag;
		private int _lagcount;
	}
}
