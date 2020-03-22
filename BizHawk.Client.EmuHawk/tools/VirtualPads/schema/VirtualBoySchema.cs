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
				DefaultSize = new Size(222, 103),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 36, "L_Up"),
					ButtonSchema.Down(14, 80, "L_Down"),
					ButtonSchema.Left(2, 58, "L_Left"),
					ButtonSchema.Right(24, 58, "L_Right"),
					new ButtonSchema(122, 58) { Name = "B" },
					new ButtonSchema(146, 58) { Name = "A" },
					new ButtonSchema(52, 58) { Name = "Select",  DisplayName = "s"},
					new ButtonSchema(74, 58) { Name = "Start",  DisplayName = "S" },
					ButtonSchema.Up(188, 36, "R_Up"),
					ButtonSchema.Down(188, 80, "R_Down"),
					ButtonSchema.Left(176, 58, "R_Left"),
					ButtonSchema.Right(198, 58, "R_Right"),
					new ButtonSchema(24, 8) { Name = "L" },
					new ButtonSchema(176, 8) { Name = "R" }
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
					new ButtonSchema(10, 15) { Name = "Power" }
				}
			};
		}
	}
}
