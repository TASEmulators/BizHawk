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
				DefaultSize = new Size(174, 79),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 12, "Up"),
					ButtonSchema.Down(14, 56, "Down"),
					ButtonSchema.Left(2, 34, "Left"),
					ButtonSchema.Right(24, 34, "Right"),
					new ButtonSchema(122, 34, "B"),
					new ButtonSchema(146, 34, "A"),
					new ButtonSchema(52, 34, "Select") { DisplayName = "s" },
					new ButtonSchema(74, 34, "Start") { DisplayName = "S"  }
				}
			};
		}

		private static PadSchema ConsoleButtons()
		{
			return new ConsoleSchema
			{
				DefaultSize = new Size(75, 50),
				Buttons = new[]
				{
					new ButtonSchema(10, 15, "Power")
				}
			};
		}

		// GBHawk controllers
		private static PadSchema StandardControllerH()
		{
			return new PadSchema
			{
				DefaultSize = new Size(174, 79),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 12, "P1 Up"),
					ButtonSchema.Down(14, 56, "P1 Down"), 
					ButtonSchema.Left(2, 34, "P1 Left"), 
					ButtonSchema.Right(24, 34, "P1 Right"), 
					new ButtonSchema(122, 34, "P1 B") { DisplayName = "B" },
					new ButtonSchema(146, 34, "P1 A") { DisplayName = "A" },
					new ButtonSchema(52, 34, "P1 Select") { DisplayName = "s" },
					new ButtonSchema(74, 34, "P1 Start") { DisplayName = "S" }
				}
			};
		}

		private static PadSchema ConsoleButtonsH()
		{
			return new ConsoleSchema
			{
				DefaultSize = new Size(75, 50),
				Buttons = new[]
				{
					new ButtonSchema(10, 15, "P1 Power") { DisplayName = "Power" }
				}
			};
		}

		private static PadSchema TiltControls()
		{
			return new PadSchema
			{
				DisplayName = "Tilt",
				DefaultSize = new Size(356, 290),
				MaxSize = new Size(356, 290),
				Buttons = new[]
				{
					new TargetedPairSchema(14, 17, "P1 Tilt X")
					{
						TargetSize = new Size(256, 240)
					}
				}
			};
		}
	}
}
