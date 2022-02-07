using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public partial class ChannelF
	{
		public ControllerDefinition ChannelFControllerDefinition
		{
			get
			{
				ControllerDefinition definition = new("ChannelF Controller");

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
					"TIME", "MODE", "HOLD", "START", "RESET"
				};

				foreach (var s in consoleButtons)
				{
					definition.BoolButtons.Add(s);
					definition.CategoryLabels[s] = "Console";
				}

				return definition.MakeImmutable();
			}
		}

		public bool[] StateConsole = new bool[5];
		public string[] ButtonsConsole =
		{
			"TIME", "MODE", "HOLD", "START", "RESET"
		};

		public byte DataConsole
		{
			get
			{
				int w = 0;
				for (int i = 0; i < 5; i++)
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
	}
}
