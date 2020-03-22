using System.Collections.Generic;
using System.Drawing;

using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Intellivision;

namespace BizHawk.Client.EmuHawk
{
	[Schema("INTV")]
	// ReSharper disable once UnusedMember.Global
	public class IntvSchema : IVirtualPadSchema
	{
		private string StandardControllerName => typeof(StandardController).DisplayName();
		private string AnalogControllerName => typeof(FakeAnalogController).DisplayName();

		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			var intvSyncSettings = ((Intellivision)core).GetSyncSettings().Clone();
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
				Buttons = new[]
				{
					new ButtonSchema(25, 15, controller , "Key 1")
					{
						DisplayName = "1"
					},
					new ButtonSchema(51, 15, controller , "Key 2")
					{
						DisplayName = "2"
					},
					new ButtonSchema(77, 15, controller , "Key 3")
					{
						DisplayName = "3"
					},
					new ButtonSchema(25, 41, controller , "Key 4")
					{
						DisplayName = "4"
					},
					new ButtonSchema(51, 41, controller , "Key 5")
					{
						DisplayName = "5"
					},
					new ButtonSchema(77, 41, controller , "Key 6")
					{
						DisplayName = "6"
					},
					new ButtonSchema(25, 67, controller , "Key 7")
					{
						DisplayName = "7"
					},
					new ButtonSchema(51, 67, controller , "Key 8")
					{
						DisplayName = "8"
					},
					new ButtonSchema(77, 67, controller , "Key 9")
					{
						DisplayName = "9"
					},
					new ButtonSchema(25, 93, controller, "Clear")
					{
						DisplayName = "C"
					},
					new ButtonSchema(51, 93, controller , "Key 0")
					{
						DisplayName = "0"
					},
					new ButtonSchema(77, 93, controller, "Enter")
					{
						DisplayName = "E"
					},
					new ButtonSchema(2, 41, controller, "Top")
					{
						DisplayName = "T"
					},
					new ButtonSchema(100, 41, controller, "Top")
					{
						DisplayName = "T"
					},
					new ButtonSchema(2, 67, controller, "L")
					{
						DisplayName = "L"
					},
					new ButtonSchema(100, 67, controller, "R")
					{
						DisplayName = "R"
					},

					/************** Directional Pad *******************/
					new ButtonSchema(51, 124, controller, "N")
					{
						Icon = Properties.Resources.BlueUp
					},
					new ButtonSchema(63, 145, controller, "NNE")
					{
						Icon = Properties.Resources.NNE
					},
					new ButtonSchema(39, 145, controller, "NNW")
					{
						Icon = Properties.Resources.NNW
					},
					new ButtonSchema(75, 166, controller, "NE")
					{
						Icon = Properties.Resources.NE
					},
					new ButtonSchema(27, 166, controller, "NW")
					{
						Icon = Properties.Resources.NW
					},
					new ButtonSchema(87, 187, controller, "ENE")
					{
						Icon = Properties.Resources.ENE
					},
					new ButtonSchema(15, 187, controller, "WNW")
					{
						Icon = Properties.Resources.WNW
					},
					new ButtonSchema(99, 208, controller, "E")
					{
						Icon = Properties.Resources.Forward
					},
					new ButtonSchema(3, 208, controller, "W")
					{
						Icon = Properties.Resources.Back
					},
					new ButtonSchema(87, 229, controller, "ESE")
					{
						Icon = Properties.Resources.ESE
					},
					new ButtonSchema(15, 229, controller, "WSW")
					{
						Icon = Properties.Resources.WSW
					},
					new ButtonSchema(75, 250, controller, "SE")
					{
						Icon = Properties.Resources.SE
					},
					new ButtonSchema(27, 250, controller, "SW")
					{
						Icon = Properties.Resources.SW
					},
					new ButtonSchema(63, 271, controller, "SSE")
					{
						Icon = Properties.Resources.SSE
					},
					new ButtonSchema(39, 271, controller, "SSW")
					{
						Icon = Properties.Resources.SSW
					},
					new ButtonSchema(51, 292, controller, "S")
					{
						Icon = Properties.Resources.BlueDown
					}
				}
			};
		}

		private static PadSchema AnalogController(int controller)
		{
			var controllerDefRanges = new FakeAnalogController(controller).Definition.FloatRanges;
			return new PadSchema
			{
				DisplayName = $"Player {controller}",
				Size = new Size(280, 332),
				Buttons = new[]
				{
					new ButtonSchema(91, 15, controller, "Key 1")
					{
						DisplayName = "1"
					},
					new ButtonSchema(117, 15, controller, "Key 2")
					{
						DisplayName = "2"
					},
					new ButtonSchema(143, 15, controller, "Key 3")
					{
						DisplayName = "3"
					},
					new ButtonSchema(91, 41, controller, "Key 4")
					{
						DisplayName = "4"
					},
					new ButtonSchema(117, 41, controller, "Key 5")
					{
						DisplayName = "5"
					},
					new ButtonSchema(143, 41, controller, "Key 6")
					{
						DisplayName = "6"
					},
					new ButtonSchema(91, 67, controller, "Key 7")
					{
						DisplayName = "7"
					},
					new ButtonSchema(117, 67, controller, "Key 8")
					{
						DisplayName = "8"
					},
					new ButtonSchema(143, 67, controller, "Key 9")
					{
						DisplayName = "9"
					},
					new ButtonSchema(91, 93, controller, "Clear")
					{
						DisplayName = "C"
					},
					new ButtonSchema(117, 93, controller, "Key 0")
					{
						DisplayName = "0"
					},
					new ButtonSchema(143, 93, controller, "Enter")
					{
						DisplayName = "E"
					},
					new ButtonSchema(68, 41, controller, "Top")
					{
						DisplayName = "T"
					},
					new ButtonSchema(166, 41, controller, "Top")
					{
						DisplayName = "T"
					},
					new ButtonSchema(68, 67, controller, "L")
					{
						DisplayName = "L"
					},
					new ButtonSchema(166, 67, controller, "R")
					{
						DisplayName = "R"
					},
					new AnalogSchema(1, 121, $"P{controller} Disc X")
					{
						AxisRange = controllerDefRanges[0],
						SecondaryAxisRange = controllerDefRanges[1]
					}
				}
			};
		}
	}
}
