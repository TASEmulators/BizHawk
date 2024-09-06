using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public partial class ChannelF : IInputPollable
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

		private int _lagCount;
		private bool _isLag;

		/// <summary>
		/// Cycles through all the input callbacks
		/// This should be done once per frame
		/// </summary>
		private void PollInput()
		{
			for (var i = 0; i < _buttonsConsole.Length; i++)
			{
				var key = _buttonsConsole[i];
				var prevState = _stateConsole[i];
				var currState = _controller.IsPressed(key);
				if (currState != prevState)
				{
					_stateConsole[i] = currState;

					if (key == "RESET" && _stateConsole[i])
					{
						ConsoleReset();
						for (var l = 0; l < _outputLatch.Length; l++)
						{
							_outputLatch[l] = 0;
						}

						return;
					}
				}
			}

			for (var i = 0; i < _buttonsRight.Length; i++)
			{
				var key = _buttonsRight[i];
				_stateRight[i] = _controller.IsPressed(key);
			}

			for (var i = 0; i < _buttonsLeft.Length; i++)
			{
				var key = _buttonsLeft[i];
				_stateLeft[i] = _controller.IsPressed(key);
			}
		}
	}
}
