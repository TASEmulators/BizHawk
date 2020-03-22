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
			return new ConsoleSchema
			{
				DefaultSize = new Size(60, 45),
				Buttons = new []
				{
					new ButtonSchema(8, 18) { Name = "Lid" }
				}
			};
		}

		private static PadSchema ControllerButtons()
		{
			return new PadSchema
			{
				DefaultSize = new Size(440, 260),
				Buttons = new []
				{
					ButtonSchema.Up(14, 79, "Up"),
					ButtonSchema.Down(14, 122, "Down"),
					ButtonSchema.Left(2, 100, "Left"),
					ButtonSchema.Right(24, 100, "Right"),
					new ButtonSchema(2, 10, "L"),
					new ButtonSchema(366, 10, "R"),
					new ButtonSchema(341, 179, "Start"),
					new ButtonSchema(341, 201, "Select"),
					new ButtonSchema(341, 100, "Y"),
					new ButtonSchema(365, 113, "B"),
					new ButtonSchema(341, 76, "X"),
					new ButtonSchema(366, 86, "A"),

					// Screen
					new TargetedPairSchema(72, 35, "TouchX")
					{
						TargetSize = new Size(256, 192)
					},
					new ButtonSchema(72, 10) { Name = "Touch" }
				}
			};
		}
	}
}
