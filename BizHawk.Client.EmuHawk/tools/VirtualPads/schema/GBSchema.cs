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
					ButtonSchema.Up("Up", 14, 12),
					ButtonSchema.Down("Down", 14, 56),
					ButtonSchema.Left("Left", 2, 34),
					ButtonSchema.Right("Right", 24, 34),
					new ButtonSchema
					{
						Name = "B",
						Location = new Point(122, 34)
					},
					new ButtonSchema
					{
						Name = "A",
						Location = new Point(146, 34)
					},
					new ButtonSchema
					{
						Name = "Select",
						DisplayName = "s",
						Location = new Point(52, 34)
					},
					new ButtonSchema
					{
						Name = "Start",
						DisplayName = "S",
						Location = new Point(74, 34)
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
					new ButtonSchema
					{
						Name = "Power",
						Location = new Point(10, 15)
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
					ButtonSchema.Up("P1 Up", 14, 12),
					ButtonSchema.Down("P1 Down", 14, 56), 
					ButtonSchema.Left("P1 Left", 2, 34), 
					ButtonSchema.Right("P1 Right", 24, 34), 
					new ButtonSchema
					{
						Name = "P1 B",
						DisplayName = "B",
						Location = new Point(122, 34)
					},
					new ButtonSchema
					{
						Name = "P1 A",
						DisplayName = "A",
						Location = new Point(146, 34)
					},
					new ButtonSchema
					{
						Name = "P1 Select",
						DisplayName = "s",
						Location = new Point(52, 34)
					},
					new ButtonSchema
					{
						Name = "P1 Start",
						DisplayName = "S",
						Location = new Point(74, 34)
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
					new ButtonSchema
					{
						Name = "P1 Power",
						Location = new Point(10, 15)
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
					new ButtonSchema
					{
						Name = "P1 Tilt X",
						Location = new Point(14, 17),
						Type = PadInputType.TargetedPair,
						TargetSize = new Size(256, 240),
						SecondaryNames = new[]
						{
							"P1 Tilt Y"
						}
					}
				}
			};
		}
	}
}
