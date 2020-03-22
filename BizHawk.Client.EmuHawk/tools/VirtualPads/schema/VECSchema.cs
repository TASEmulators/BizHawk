using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Vectrex;

namespace BizHawk.Client.EmuHawk
{
	[Schema("VEC")]
	// ReSharper disable once UnusedMember.Global
	public class VecSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			var vecSyncSettings = ((VectrexHawk)core).GetSyncSettings().Clone();
			var port1 = vecSyncSettings.Port1;
			var port2 = vecSyncSettings.Port2;

			if (port1 == "Vectrex Digital Controller")
			{
				yield return StandardController(1);
			}

			if (port2 == "Vectrex Digital Controller")
			{
				yield return StandardController(2);
			}

			if (port1 == "Vectrex Analog Controller")
			{
				yield return AnalogController(1);
			}

			if (port2 == "Vectrex Analog Controller")
			{
				yield return AnalogController(2);
			}
		}

		private static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				DefaultSize = new Size(200, 100),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 12, controller),
					ButtonSchema.Down(14, 56, controller),
					ButtonSchema.Left(2, 34, controller),
					ButtonSchema.Right(24, 34, controller),
					new ButtonSchema(74, 34, controller, "Button 1")
					{
						DisplayName = "1"
					},
					new ButtonSchema(98, 34, controller, "Button 2")
					{
						DisplayName = "2"
					},
					new ButtonSchema(122, 34, controller, "Button 3")
					{
						DisplayName = "3"
					},
					new ButtonSchema(146, 34, controller, "Button 4")
					{
						DisplayName = "4"
					}
				}
			};
		}

		private static PadSchema AnalogController(int controller)
		{
			var controllerDefRanges = new AnalogControls(controller).Definition.FloatRanges;
			return new PadSchema
			{
				DefaultSize = new Size(280, 380),
				Buttons = new[]
				{
					new ButtonSchema(74, 34, controller, "Button 1")
					{
						DisplayName = "1"
					},
					new ButtonSchema(98, 34, controller, "Button 2")
					{
						DisplayName = "2"
					},
					new ButtonSchema(122, 34, controller, "Button 3")
					{
						DisplayName = "3"
					},
					new ButtonSchema(146, 34, controller, "Button 4")
					{
						DisplayName = "4"
					},
					new AnalogSchema(2, 80, $"P{controller} Stick X")
					{
						AxisRange = controllerDefRanges[0],
						SecondaryAxisRange = controllerDefRanges[1]
					}
				}
			};
		}
	}
}
