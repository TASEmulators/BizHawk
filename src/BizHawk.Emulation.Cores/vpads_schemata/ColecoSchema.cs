using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.ColecoVision;

namespace BizHawk.Emulation.Cores
{
	[Schema(VSystemID.Raw.Coleco)]
	public class ColecoSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
		{
			var deck = ((ColecoVision.ColecoVision) core).ControllerDeck;
			var ports = new[] { deck.Port1.GetType(), deck.Port2.GetType() };

			for (int i = 0; i < 2; i++)
			{
				if (ports[i] == typeof(StandardController))
				{
					yield return StandardController(i + 1);
				}
				else if (ports[i] == typeof(ColecoTurboController))
				{
					yield return TurboController(i + 1);
				}
				else if (ports[i] == typeof(ColecoSuperActionController))
				{
					yield return SuperActionController(i + 1);
				}
			}

			// omitting console as it only has reset and power buttons
		}

		public static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				Size = new Size(128, 200),
				Buttons = StandardButtons(controller)
			};
		}

		private static PadSchema TurboController(int controller)
		{
			var defAxes = new ColecoTurboController(controller).Definition.Axes;
			return new PadSchema
			{
				Size = new Size(275, 260),
				Buttons = new PadSchemaControl[]
				{
					new AnalogSchema(6, 14, $"P{controller} Disc X")
					{
						Spec = defAxes.SpecAtIndex(0),
						SecondarySpec = defAxes.SpecAtIndex(1)
					},
					new ButtonSchema(6, 224, controller, "Pedal")
				}
			};
		}

		private static PadSchema SuperActionController(int controller)
		{
			return new PadSchema
			{
				Size = new Size(195, 260),
				Buttons = StandardButtons(controller).Concat(new PadSchemaControl[]
				{
					new SingleAxisSchema(6, 200, controller, "Disc X")
					{
						DisplayName = "Disc",
						TargetSize = new Size(180, 55),
						MinValue = -360,
						MaxValue = 360
					},
					new ButtonSchema(126, 15, controller, "Yellow"),
					new ButtonSchema(126, 40, controller, "Red"),
					new ButtonSchema(126, 65, controller, "Purple"),
					new ButtonSchema(126, 90, controller, "Blue")
				})
			};
		}

		private static IEnumerable<ButtonSchema> StandardButtons(int controller) => new ButtonSchema[]
		{
			ButtonSchema.Up(50, 11, controller),
			ButtonSchema.Down(50, 32, controller),
			ButtonSchema.Left(29, 22, controller),
			ButtonSchema.Right(71, 22, controller),
			new(27, 85, controller, "Key 1", "1"),
			new(50, 85, controller, "Key 2", "2"),
			new(73, 85, controller, "Key 3", "3"),
			new(27, 108, controller, "Key 4", "4"),
			new(50, 108, controller, "Key 5", "5"),
			new(73, 108, controller, "Key 6", "6"),
			new(27, 131, controller, "Key 7", "7"),
			new(50, 131, controller, "Key 8", "8"),
			new(73, 131, controller, "Key 9", "9"),
			new(27, 154, controller, "Star", "*"),
			new(50, 154, controller, "Key 0", "0"),
			new(73, 154, controller, "Pound", "#"),
		};
	}
}
