using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public partial class ChannelF : IInputPollable
	{
		public int LagCount { get; set; } = 0;

		public bool IsLagFrame { get; set; } = false;

		public IInputCallbackSystem InputCallbacks { get; } = new InputCallbackSystem();

		/// <summary>
		/// Cycles through all the input callbacks
		/// This should be done once per frame
		/// </summary>
		public bool PollInput()
		{
			bool noInput = true;

			InputCallbacks.Call();

			lock (this)
			{
				for (int i = 0; i < ButtonsConsole.Length; i++)
				{
					string key = ButtonsConsole[i];
					bool prevState = StateConsole[i]; // CTRLConsole.Bit(i);      
					bool currState = _controller.IsPressed(key);
					if (currState != prevState)
					{
						StateConsole[i] = currState;
						noInput = false;

						if (key == "RESET" && StateConsole[i])
						{
							CPU.Reset();
							for (int l = 0; l < OutputLatch.Length; l++)
							{
								OutputLatch[l] = 0;
							}
							return true;
						}
					}
				}

				for (int i = 0; i < ButtonsRight.Length; i++)
				{
					string key = "P1 " + ButtonsRight[i];
					bool prevState = StateRight[i];
					bool currState = _controller.IsPressed(key);
					if (currState != prevState)
					{
						StateRight[i] = currState;
						noInput = false;
					}
				}

				for (int i = 0; i < ButtonsLeft.Length; i++)
				{
					string key = "P2 " + ButtonsLeft[i];
					bool prevState = StateLeft[i];
					bool currState = _controller.IsPressed(key);
					if (currState != prevState)
					{
						StateLeft[i] = currState;
						noInput = false;
					}
				}
			}

			return noInput;
		}
	}
}
