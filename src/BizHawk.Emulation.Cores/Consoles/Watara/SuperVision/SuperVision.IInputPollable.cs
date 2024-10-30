using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.SuperVision
{
	public partial class SuperVision : IInputPollable
	{
		public IInputCallbackSystem InputCallbacks { get; } = new InputCallbackSystem();

		private int _lagCount;
		private bool _isLag;

		/// <summary>
		/// Cycles through all the input callbacks
		/// This should be done once per frame
		/// </summary>
		private void PollInput()
		{
			for (var i = 0; i < _buttons.Length; i++)
			{
				var key = _buttons[i];
				_buttonsState[i] = _controller.IsPressed(key);
			}
		}

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
	}
}
