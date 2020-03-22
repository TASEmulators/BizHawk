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
				DefaultSize = new Size(148, 332),
				Buttons = new[]
				{
					new ButtonSchema(25, 15)
					{
						Name = $"P{controller} Key 1",
						DisplayName = "1"

					},
					new ButtonSchema(51, 15)
					{
						Name = $"P{controller} Key 2",
						DisplayName = "2"
					},
					new ButtonSchema(77, 15)
					{
						Name = $"P{controller} Key 3",
						DisplayName = "3"
					},
					new ButtonSchema(25, 41)
					{
						Name = $"P{controller} Key 4",
						DisplayName = "4"
					},
					new ButtonSchema(51, 41)
					{
						Name = $"P{controller} Key 5",
						DisplayName = "5"
					},
					new ButtonSchema(77, 41)
					{
						Name = $"P{controller} Key 6",
						DisplayName = "6"
					},
					new ButtonSchema(25, 67)
					{
						Name = $"P{controller} Key 7",
						DisplayName = "7"
					},
					new ButtonSchema(51, 67)
					{
						Name = $"P{controller} Key 8",
						DisplayName = "8"
					},
					new ButtonSchema(77, 67)
					{
						Name = $"P{controller} Key 9",
						DisplayName = "9"
					},
					new ButtonSchema(25, 93)
					{
						Name = $"P{controller} Clear",
						DisplayName = "C"
					},
					new ButtonSchema(51, 93)
					{
						Name = $"P{controller} Key 0",
						DisplayName = "0"
					},
					new ButtonSchema(77, 93)
					{
						Name = $"P{controller} Enter",
						DisplayName = "E"
					},

					new ButtonSchema(2, 41)
					{
						Name = $"P{controller} Top",
						DisplayName = "T"
					},
					new ButtonSchema(100, 41)
					{
						Name = $"P{controller} Top",
						DisplayName = "T"
					},

					new ButtonSchema(2, 67)
					{
						Name = $"P{controller} L",
						DisplayName = "L"
					},
					new ButtonSchema(100, 67)
					{
						Name = $"P{controller} R",
						DisplayName = "R"
					},

					/************** Directional Pad *******************/

					new ButtonSchema(51, 124)
					{
						Name = $"P{controller} N",
						Icon = Properties.Resources.BlueUp
					},
					new ButtonSchema(63, 145)
					{
						Name = $"P{controller} NNE",
						Icon = Properties.Resources.NNE
					},
					new ButtonSchema(39, 145)
					{
						Name = $"P{controller} NNW",
						Icon = Properties.Resources.NNW
					},
					new ButtonSchema(75, 166)
					{
						Name = $"P{controller} NE",
						Icon = Properties.Resources.NE
					},
					new ButtonSchema(27, 166)
					{
						Name = $"P{controller} NW",
						Icon = Properties.Resources.NW
					},
					new ButtonSchema(87, 187)
					{
						Name = $"P{controller} ENE",
						Icon = Properties.Resources.ENE
					},
					new ButtonSchema(15, 187)
					{
						Name = $"P{controller} WNW",
						Icon = Properties.Resources.WNW
					},
					new ButtonSchema(99, 208)
					{
						Name = $"P{controller} E",
						Icon = Properties.Resources.Forward
					},
					new ButtonSchema(3, 208)
					{
						Name = $"P{controller} W",
						Icon = Properties.Resources.Back
					},
					new ButtonSchema(87, 229)
					{
						Name = $"P{controller} ESE",
						Icon = Properties.Resources.ESE
					},
					new ButtonSchema(15, 229)
					{
						Name = $"P{controller} WSW",
						Icon = Properties.Resources.WSW
					},
					new ButtonSchema(75, 250)
					{
						Name = $"P{controller} SE",
						Icon = Properties.Resources.SE
					},
					new ButtonSchema(27, 250)
					{
						Name = $"P{controller} SW",
						Icon = Properties.Resources.SW
					},
					new ButtonSchema(63, 271)
					{
						Name = $"P{controller} SSE",
						Icon = Properties.Resources.SSE
					},
					new ButtonSchema(39, 271)
					{
						Name = $"P{controller} SSW",
						Icon = Properties.Resources.SSW
					},
					new ButtonSchema(51, 292)
					{
						Name = $"P{controller} S",
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
				DefaultSize = new Size(280, 332),
				Buttons = new[]
				{
					new ButtonSchema(91, 15)
					{
						Name = $"P{controller} Key 1",
						DisplayName = "1"
					},
					new ButtonSchema(117, 15)
					{
						Name = $"P{controller} Key 2",
						DisplayName = "2"
					},
					new ButtonSchema(143, 15)
					{
						Name = $"P{controller} Key 3",
						DisplayName = "3"
					},
					new ButtonSchema(91, 41)
					{
						Name = $"P{controller} Key 4",
						DisplayName = "4"
					},
					new ButtonSchema(117, 41)
					{
						Name = $"P{controller} Key 5",
						DisplayName = "5"
					},
					new ButtonSchema(143, 41)
					{
						Name = $"P{controller} Key 6",
						DisplayName = "6"
					},
					new ButtonSchema(91, 67)
					{
						Name = $"P{controller} Key 7",
						DisplayName = "7"
					},
					new ButtonSchema(117, 67)
					{
						Name = $"P{controller} Key 8",
						DisplayName = "8"
					},
					new ButtonSchema(143, 67)
					{
						Name = $"P{controller} Key 9",
						DisplayName = "9"
					},
					new ButtonSchema(91, 93)
					{
						Name = $"P{controller} Clear",
						DisplayName = "C"
					},
					new ButtonSchema(117, 93)
					{
						Name = $"P{controller} Key 0",
						DisplayName = "0"
					},
					new ButtonSchema(143, 93)
					{
						Name = $"P{controller} Enter",
						DisplayName = "E"
					},

					new ButtonSchema(68, 41)
					{
						Name = $"P{controller} Top",
						DisplayName = "T"
					},
					new ButtonSchema(166, 41)
					{
						Name = $"P{controller} Top",
						DisplayName = "T"
					},
					new ButtonSchema(68, 67)
					{
						Name = $"P{controller} L",
						DisplayName = "L"
					},
					new ButtonSchema(166, 67)
					{
						Name = $"P{controller} R",
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
