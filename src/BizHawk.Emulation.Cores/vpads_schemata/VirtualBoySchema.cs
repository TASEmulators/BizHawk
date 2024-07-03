using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores
{
	[Schema(VSystemID.Raw.VB)]
	public class VirtualBoySchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
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
					ButtonSchema.Up(14, 36, "P1 L_Up"),
					ButtonSchema.Down(14, 80, "P1 L_Down"),
					ButtonSchema.Left(2, 58, "P1 L_Left"),
					ButtonSchema.Right(24, 58, "P1 L_Right"),
					new ButtonSchema(122, 58, 1, "B"),
					new ButtonSchema(146, 58, 1, "A"),
					new ButtonSchema(52, 58, 1, "Select") { DisplayName = "s"},
					new ButtonSchema(74, 58, 1, "Start") { DisplayName = "S" },
					ButtonSchema.Up(188, 36, "P1 R_Up"),
					ButtonSchema.Down(188, 80, "P1 R_Down"),
					ButtonSchema.Left(176, 58, "P1 R_Left"),
					ButtonSchema.Right(198, 58, "P1 R_Right"),
					new ButtonSchema(24, 8, 1, "L"),
					new ButtonSchema(176, 8, 1, "R")
				}
			};
		}

		private static PadSchema ConsoleButtons()
		{
			return new ConsoleSchema
			{
				Size = new Size(235, 50),
				Buttons = new[]
				{
					new ButtonSchema(10, 15, "Power"),
					new ButtonSchema(60, 15, 2, "Battery Voltage: Set Low")
					{
						DisplayName = "Low Battery",
					},
					new ButtonSchema(135, 15, 2, "Battery Voltage: Set Normal")
					{
						DisplayName = "Normal Battery",
					}
				}
			};
		}
	}
}
