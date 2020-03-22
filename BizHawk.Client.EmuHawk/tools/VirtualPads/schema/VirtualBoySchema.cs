using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	[Schema("VB")]
	// ReSharper disable once UnusedMember.Global
	public class VirtualBoySchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			yield return StandardController();
			yield return ConsoleButtons();
		}

		private static PadSchema StandardController()
		{
			return new PadSchema
			{
				Size = new Size(222, 103),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 36, "L_Up"),
					ButtonSchema.Down(14, 80, "L_Down"),
					ButtonSchema.Left(2, 58, "L_Left"),
					ButtonSchema.Right(24, 58, "L_Right"),
					new ButtonSchema(122, 58, "B"),
					new ButtonSchema(146, 58, "A"),
					new ButtonSchema(52, 58, "Select") { DisplayName = "s"},
					new ButtonSchema(74, 58, "Start") { DisplayName = "S" },
					ButtonSchema.Up(188, 36, "R_Up"),
					ButtonSchema.Down(188, 80, "R_Down"),
					ButtonSchema.Left(176, 58, "R_Left"),
					ButtonSchema.Right(198, 58, "R_Right"),
					new ButtonSchema(24, 8, "L"),
					new ButtonSchema(176, 8, "R")
				}
			};
		}

		private static PadSchema ConsoleButtons()
		{
			return new ConsoleSchema
			{
				Size = new Size(75, 50),
				Buttons = new[]
				{
					new ButtonSchema(10, 15, "Power")
				}
			};
		}
	}
}
