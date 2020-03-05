using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	[Schema("GB")]
	// ReSharper disable once UnusedMember.Global
	public class GbSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			switch (core.ControllerDefinition.Name)
			{
				case "Gameboy Controller + Tilt":
					yield return StandardControllerH();
					yield return ConsoleButtonsH();
					yield return TiltControls();
					break;
				case "Gameboy Controller H":
					yield return StandardControllerH();
					yield return ConsoleButtonsH();
					break;
				default:
					yield return StandardController();
					yield return ConsoleButtons();
					break;
			}
		}

		// Gambatte Controller
		private static PadSchema StandardController()
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(174, 79),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = "Up",
						DisplayName = "",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(14, 12),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Down",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(14, 56),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Left",
						DisplayName = "",
						Icon = Properties.Resources.Back,
						Location = new Point(2, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Right",
						DisplayName = "",
						Icon = Properties.Resources.Forward,
						Location = new Point(24, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "B",
						DisplayName = "B",
						Location = new Point(122, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "A",
						DisplayName = "A",
						Location = new Point(146, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Select",
						DisplayName = "s",
						Location = new Point(52, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Start",
						DisplayName = "S",
						Location = new Point(74, 34),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}

		private static PadSchema ConsoleButtons()
		{
			return new PadSchema
			{
				DisplayName = "Console",
				IsConsole = true,
				DefaultSize = new Size(75, 50),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = "Power",
						DisplayName = "Power",
						Location = new Point(10, 15),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}

		// GBHawk controllers
		private static PadSchema StandardControllerH()
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(174, 79),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = "P1 Up",
						DisplayName = "",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(14, 12),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P1 Down",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(14, 56),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P1 Left",
						DisplayName = "",
						Icon = Properties.Resources.Back,
						Location = new Point(2, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P1 Right",
						DisplayName = "",
						Icon = Properties.Resources.Forward,
						Location = new Point(24, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P1 B",
						DisplayName = "B",
						Location = new Point(122, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P1 A",
						DisplayName = "A",
						Location = new Point(146, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P1 Select",
						DisplayName = "s",
						Location = new Point(52, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P1 Start",
						DisplayName = "S",
						Location = new Point(74, 34),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}

		private static PadSchema ConsoleButtonsH()
		{
			return new PadSchema
			{
				DisplayName = "Console",
				IsConsole = true,
				DefaultSize = new Size(75, 50),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = "P1 Power",
						DisplayName = "Power",
						Location = new Point(10, 15),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}

		private static PadSchema TiltControls()
		{
			return new PadSchema
			{
				DisplayName = "Tilt",
				IsConsole = false,
				DefaultSize = new Size(356, 290),
				MaxSize = new Size(356, 290),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = "P1 Tilt X",
						Location = new Point(14, 17),
						Type = PadSchema.PadInputType.TargetedPair,
						TargetSize = new Size(256, 240),
						SecondaryNames = new[]
						{
							"P1 Tilt Y",
						}
					}
				}
			};
		}
	}
}
