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
					ButtonSchema.Up(14, 12, "Up"),
					ButtonSchema.Down(14, 56, "Down"),
					ButtonSchema.Left(2, 34, "Left"),
					ButtonSchema.Right(24, 34, "Right"),
					new ButtonSchema(122, 34) { Name = "B" },
					new ButtonSchema(146, 34) { Name = "A" },
					new ButtonSchema(52, 34)  { Name = "Select",  DisplayName = "s" },
					new ButtonSchema(74, 34) { Name = "Start",  DisplayName = "S"  }
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
					new ButtonSchema(10, 15) { Name = "Power" }
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
					ButtonSchema.Up(14, 12, "P1 Up"),
					ButtonSchema.Down(14, 56, "P1 Down"), 
					ButtonSchema.Left(2, 34, "P1 Left"), 
					ButtonSchema.Right(24, 34, "P1 Right"), 
					new ButtonSchema(122, 34)
					{
						Name = "P1 B",
						DisplayName = "B"
					},
					new ButtonSchema(146, 34)
					{
						Name = "P1 A",
						DisplayName = "A"
					},
					new ButtonSchema(52, 34)
					{
						Name = "P1 Select",
						DisplayName = "s"
					},
					new ButtonSchema(74, 34)
					{
						Name = "P1 Start",
						DisplayName = "S"
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
					new ButtonSchema(10, 15) { Name = "P1 Power" }
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
					new ButtonSchema(14, 17)
					{
						Name = "P1 Tilt X",
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
