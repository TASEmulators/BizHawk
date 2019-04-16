using System;
using System.Collections.Generic;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public partial class ChannelF
	{
		public ControllerDefinition ChannelFControllerDefinition
		{
			get
			{
				ControllerDefinition definition = new ControllerDefinition();
				definition.Name = "ChannelF Controller";

				// sticks
				List<string> stickL = new List<string>
				{
					// P1 (left) stick
					"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Button Up", "P1 Button Down", "P1 Rotate Left", "P1 Rotate Right"
				};

				foreach (var s in stickL)
				{
					definition.BoolButtons.Add(s);
					definition.CategoryLabels[s] = "Left Controller";
				}

				List<string> stickR = new List<string>
				{
					// P1 (left) stick
					"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Button Up", "P2 Button Down", "P2 Rotate Left", "P2 Rotate Right"
				};

				foreach (var s in stickR)
				{
					definition.BoolButtons.Add(s);
					definition.CategoryLabels[s] = "Right Controller";
				}

				// console
				List<string> consoleButtons = new List<string>
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
