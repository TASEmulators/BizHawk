using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Intellivision;

namespace BizHawk.Emulation.Cores
{
	[Schema(VSystemID.Raw.INTV)]
	public class IntvSchema : IVirtualPadSchema
	{
		private string StandardControllerName => typeof(StandardController).DisplayName();
		private string AnalogControllerName => typeof(FakeAnalogController).DisplayName();

		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
		{
			var intvSyncSettings = ((Intellivision.Intellivision) core).GetSyncSettings().Clone();
			var port1 = intvSyncSettings.Port1;
			var port2 = intvSyncSettings.Port2;

			if (port1 == StandardControllerName)
			{
				yield return StandardController(1);
			}
			else if (port1 == AnalogControllerName)
			{
				yield return AnalogController(1);
			}

			if (port2 == StandardControllerName)
			{
				yield return StandardController(2);
			}
			else if (port2 == AnalogControllerName)
			{
				yield return AnalogController(2);
			}
		}

		private static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				DisplayName = $"Player {controller}",
				Size = new Size(148, 332),
				Buttons = StandardButtons(controller).Concat(new[]
				{
					new ButtonSchema(51, 124, controller, "N") { Icon = VGamepadButtonImage.BlueArrN },
					new ButtonSchema(63, 145, controller, "NNE") { Icon = VGamepadButtonImage.BlueArrNNE },
					new ButtonSchema(39, 145, controller, "NNW") { Icon = VGamepadButtonImage.BlueArrNNW },
					new ButtonSchema(75, 166, controller, "NE") { Icon = VGamepadButtonImage.BlueArrNE },
					new ButtonSchema(27, 166, controller, "NW") { Icon = VGamepadButtonImage.BlueArrNW },
					new ButtonSchema(87, 187, controller, "ENE") { Icon = VGamepadButtonImage.BlueArrENE },
					new ButtonSchema(15, 187, controller, "WNW") { Icon = VGamepadButtonImage.BlueArrWNW },
					new ButtonSchema(99, 208, controller, "E") { Icon = VGamepadButtonImage.BlueArrE },
					new ButtonSchema(3, 208, controller, "W") { Icon = VGamepadButtonImage.BlueArrW },
					new ButtonSchema(87, 229, controller, "ESE") { Icon = VGamepadButtonImage.BlueArrESE },
					new ButtonSchema(15, 229, controller, "WSW") { Icon = VGamepadButtonImage.BlueArrWSW },
					new ButtonSchema(75, 250, controller, "SE") { Icon = VGamepadButtonImage.BlueArrSE },
					new ButtonSchema(27, 250, controller, "SW") { Icon = VGamepadButtonImage.BlueArrSW },
					new ButtonSchema(63, 271, controller, "SSE") { Icon = VGamepadButtonImage.BlueArrSSE },
					new ButtonSchema(39, 271, controller, "SSW") { Icon = VGamepadButtonImage.BlueArrSSW },
					new ButtonSchema(51, 292, controller, "S") { Icon = VGamepadButtonImage.BlueArrS }
				})
			};
		}

		private static PadSchema AnalogController(int controller)
		{
			var defAxes = new FakeAnalogController(controller).Definition.Axes;
			return new PadSchema
			{
				DisplayName = $"Player {controller}",
				Size = new Size(280, 332),
				Buttons = StandardButtons(controller).Concat(new PadSchemaControl[]
				{
					new AnalogSchema(1, 121, $"P{controller} Disc X")
					{
						Spec = defAxes.SpecAtIndex(0),
						SecondarySpec = defAxes.SpecAtIndex(1)
					}
				})
			};
		}

		private static IEnumerable<ButtonSchema> StandardButtons(int controller)
		{
			return new[]
			{
				Key(25, 15, controller , 1),
				Key(51, 15, controller, 2),
				Key(77, 15, controller, 3),
				Key(25, 41, controller, 4),
				Key(51, 41, controller, 5),
				Key(77, 41, controller, 6),
				Key(25, 67, controller, 7),
				Key(51, 67, controller, 8),
				Key(77, 67, controller, 9),
				new ButtonSchema(25, 93, controller, "Clear", "C"),
				Key(51, 93, controller, 0),
				new ButtonSchema(77, 93, controller, "Enter", "E"),
				new ButtonSchema(2, 41, controller, "Top", "T"),
				new ButtonSchema(100, 41, controller, "Top", "T"),
				new ButtonSchema(2, 67, controller, "L"),
				new ButtonSchema(100, 67, controller, "R")
			};
		}

		private static ButtonSchema Key(int x, int y, int controller, int button)
		{
			return new ButtonSchema(x, y, controller, $"Key {button}")
			{
				DisplayName = button.ToString()
			};
		}
	}
}
