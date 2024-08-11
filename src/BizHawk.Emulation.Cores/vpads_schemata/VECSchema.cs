using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Vectrex;

namespace BizHawk.Emulation.Cores
{
	[Schema(VSystemID.Raw.VEC)]
	public class VecSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
		{
			var vecSyncSettings = ((VectrexHawk)core).GetSyncSettings().Clone();
			var port1 = vecSyncSettings.Port1;
			var port2 = vecSyncSettings.Port2;

			switch (port1)
			{
				case "Vectrex Digital Controller":
					yield return StandardController(1);
					break;
				case "Vectrex Analog Controller":
					yield return AnalogController(1);
					break;
			}

			switch (port2)
			{
				case "Vectrex Digital Controller":
					yield return StandardController(2);
					break;
				case "Vectrex Analog Controller":
					yield return AnalogController(2);
					break;
			}

			yield return ConsoleButtons();
		}

		private static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				Size = new Size(200, 100),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 12, controller),
					ButtonSchema.Down(14, 56, controller),
					ButtonSchema.Left(2, 34, controller),
					ButtonSchema.Right(24, 34, controller),
					Button(74, 34, controller, 1),
					Button(98, 34, controller, 2),
					Button(122, 34, controller, 3),
					Button(146, 34, controller, 4)
				}
			};
		}

		private static PadSchema AnalogController(int controller)
		{
			var defAxes = new AnalogControls(controller).Definition.Axes;
			return new PadSchema
			{
				Size = new Size(280, 300),
				Buttons = new PadSchemaControl[]
				{
					Button(74, 34, controller, 1),
					Button(98, 34, controller, 2),
					Button(122, 34, controller, 3),
					Button(146, 34, controller, 4),
					new AnalogSchema(2, 80, $"P{controller} Stick X")
					{
						Spec = defAxes.SpecAtIndex(0),
						SecondarySpec = defAxes.SpecAtIndex(1)
					}
				}
			};
		}

		private static ButtonSchema Button(int x, int y, int controller, int button)
		{
			return new ButtonSchema(x, y, controller, $"Button {button}", button.ToString());
		}

		private static PadSchema ConsoleButtons()
		{
			return new ConsoleSchema
			{
				Size = new Size(150, 50),
				Buttons = new[]
				{
					new ButtonSchema(10, 15, "Reset"),
					new ButtonSchema(58, 15, "Power")
				}
			};
		}
	}
}
