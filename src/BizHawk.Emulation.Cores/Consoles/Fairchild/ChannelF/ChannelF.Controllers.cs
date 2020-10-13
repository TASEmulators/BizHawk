using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public partial class ChannelF
	{
		public bool[] StateConsole = new bool[4];
		public string[] ButtonsConsole =
		{
			"TIME", "MODE", "HOLD", "START"
		};

		public byte DataConsole
		{
			get
			{
				int w = 0;
				for (int i = 0; i < 4; i++)
				{
					byte mask = (byte) (1 << i);
					w = StateConsole[i] ? w | mask : w & ~mask;
				}

				return (byte)(w & 0xFF);
			}
		}

		public bool[] StateRight = new bool[8];
		public string[] ButtonsRight =
		{
			"Right", "Left", "Back", "Forward", "CCW", "CW", "Pull", "Push"
		};
		public byte DataRight
		{
			get
			{
				int w = 0;
				for (int i = 0; i < 8; i++)
				{
					byte mask = (byte)(1 << i);
					w = StateRight[i] ? w | mask : w & ~mask;
				}

				return (byte)(w & 0xFF);
			}
		}

		public bool[] StateLeft = new bool[8];
		public string[] ButtonsLeft =
		{
			"Right", "Left", "Back", "Forward", "CCW", "CW", "Pull", "Push"
		};
		public byte DataLeft
		{
			get
			{
				int w = 0;
				for (int i = 0; i < 8; i++)
				{
					byte mask = (byte)(1 << i);
					w = StateLeft[i] ? w | mask : w & ~mask;
				}

				return (byte)(w & 0xFF);
			}
		}


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
					var key = ButtonsConsole[i];
					bool prevState = StateConsole[i]; // CTRLConsole.Bit(i);
					bool currState = _controller.IsPressed(key);
					if (currState != prevState)
					{
						StateConsole[i] = currState;
						noInput = false;
					}
				}

				for (int i = 0; i < ButtonsRight.Length; i++)
				{
					var key = "P1 " + ButtonsRight[i];
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
					var key = "P2 " + ButtonsLeft[i];
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

		public ControllerDefinition ChannelFControllerDefinition
		{
			get
			{
				var definition = new ControllerDefinition("ChannelF Controller");

				string pre = "P1 ";

				// sticks
				var stickR = new List<string>
				{
					// P1 (right) stick
					pre + "Forward", pre + "Back", pre + "Left", pre + "Right", pre + "CCW", pre + "CW", pre + "Pull", pre + "Push"
				};

				foreach (var s in stickR)
				{
					definition.BoolButtons.Add(s);
					definition.CategoryLabels[s] = "Right Controller";
				}

				pre = "P2 ";

				var stickL = new List<string>
				{
					// P2 (left) stick
					pre + "Forward", pre + "Back", pre + "Left", pre + "Right", pre + "CCW", pre + "CW", pre + "Pull", pre + "Push"
				};

				foreach (var s in stickL)
				{
					definition.BoolButtons.Add(s);
					definition.CategoryLabels[s] = "Left Controller";
				}

				// console
				var consoleButtons = new List<string>
				{
					"RESET", "START", "HOLD", "MODE", "TIME"
				};

				foreach (var s in consoleButtons)
				{
					definition.BoolButtons.Add(s);
					definition.CategoryLabels[s] = "Console";
				}

				return definition;
			}
		}
	}
}
