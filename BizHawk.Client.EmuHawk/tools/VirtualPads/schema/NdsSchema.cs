using System.Collections.Generic;
using System.Drawing;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk.tools.VirtualPads.schema
{
	[Schema("NDS")]
	// ReSharper disable once UnusedMember.Global
	public class NdsSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			yield return ControllerButtons();
			yield return Console();
		}

		private static PadSchema Console()
		{
			return new PadSchema
			{
				IsConsole = true,
				DefaultSize = new Size(50, 35),
				Buttons = new []
				{
					new ButtonSchema(8, 8) { Name = "Lid" }
				}
			};
		}

		private static PadSchema ControllerButtons()
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(440, 260),
				Buttons = new []
				{
					ButtonSchema.Up(14, 79, "Up"),
					ButtonSchema.Down(14, 122, "Down"),
					ButtonSchema.Left(2, 100, "Left"),
					ButtonSchema.Right(24, 100, "Right"),
					new ButtonSchema(2, 10) { Name = "L" },
					new ButtonSchema(366, 10) { Name = "R" },
					new ButtonSchema(341, 179) { Name = "Start" },
					new ButtonSchema(341, 201) { Name = "Select" },
					new ButtonSchema(341, 100) { Name = "Y" },
					new ButtonSchema(365, 113) { Name = "B" },
					new ButtonSchema(341, 76) { Name = "X" },
					new ButtonSchema(366, 86) { Name = "A" },

					// Screen
					new ButtonSchema(72, 35)
					{
						Name = "TouchX",
						Type = PadInputType.TargetedPair,
						TargetSize = new Size(256, 192),
						SecondaryNames = new[] { "TouchY" }
					},
					new ButtonSchema(72, 10) { Name = "Touch" }
				}
			};
		}
	}
}
