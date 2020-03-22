using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.ColecoVision;

namespace BizHawk.Client.EmuHawk
{
	[Schema("Coleco")]
	// ReSharper disable once UnusedMember.Global
	public class ColecoSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			var deck = ((ColecoVision)core).ControllerDeck;
			var ports = new[] { deck.Port1.GetType(), deck.Port2.GetType() };

			for (int i = 0; i < 2; i++)
			{
				if (ports[i] == typeof(UnpluggedController))
				{
					break;
				}

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
		}

		public static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(128, 200),
				Buttons = new[]
				{
					ButtonSchema.Up(50, 11, $"P{controller} Up"),
					ButtonSchema.Down(50, 32, $"P{controller} Down"),
					ButtonSchema.Left(29, 22, $"P{controller} Left"),
					ButtonSchema.Right(71, 22, $"P{controller} Right"),
					new ButtonSchema(3, 42)
					{
						Name = $"P{controller} L",
						DisplayName = "L"
					},
					new ButtonSchema(100, 42)
					{
						Name = $"P{controller} R",
						DisplayName = "R"
					},
					new ButtonSchema(27, 85)
					{
						Name = $"P{controller} Key 1",
						DisplayName = "1"
					},
					new ButtonSchema(50, 85)
					{
						Name = $"P{controller} Key 2",
						DisplayName = "2"
					},
					new ButtonSchema(73, 85)
					{
						Name = $"P{controller} Key 3",
						DisplayName = "3"
					},

					new ButtonSchema(27, 108)
					{
						Name = $"P{controller} Key 4",
						DisplayName = "4"
					},
					new ButtonSchema(50, 108)
					{
						Name = $"P{controller} Key 5",
						DisplayName = "5"
					},
					new ButtonSchema(73, 108)
					{
						Name = $"P{controller} Key 6",
						DisplayName = "6"
					},

					new ButtonSchema(27, 131)
					{
						Name = $"P{controller} Key 7",
						DisplayName = "7"
					},
					new ButtonSchema(50, 131)
					{
						Name = $"P{controller} Key 8",
						DisplayName = "8"
					},
					new ButtonSchema(73, 131)
					{
						Name = $"P{controller} Key 9",
						DisplayName = "9"
					},
					new ButtonSchema(27, 154)
					{
						Name = $"P{controller} Star",
						DisplayName = "*"
					},
					new ButtonSchema(50, 154)
					{
						Name = $"P{controller} Key 0",
						DisplayName = "0"
					},
					new ButtonSchema(73, 154)
					{
						Name = $"P{controller} Pound",
						DisplayName = "#"
					}
				}
			};
		}

		private static PadSchema TurboController(int controller)
		{
			var controllerDefRanges = new ColecoTurboController(controller).Definition.FloatRanges;
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(275, 260),
				Buttons = new[]
				{
					new AnalogSchema(6, 14, $"P{controller} Disc X")
					{
						AxisRange = controllerDefRanges[0],
						SecondaryAxisRange = controllerDefRanges[1]
					},
					new ButtonSchema(6, 224)
					{
						Name = $"P{controller} Pedal",
						DisplayName = "Pedal"
					}
				}
			};
		}

		private static PadSchema SuperActionController(int controller)
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(195, 260),
				Buttons = new[]
				{
					ButtonSchema.Up(50, 11, $"P{controller} Up"),
					ButtonSchema.Down(50, 32, $"P{controller} Down"),
					ButtonSchema.Left(29, 22, $"P{controller} Left"),
					ButtonSchema.Right(71, 22, $"P{controller} Right"),
					new ButtonSchema(27, 85)
					{
						Name = $"P{controller} Key 1",
						DisplayName = "1"
					},
					new ButtonSchema(50, 85)
					{
						Name = $"P{controller} Key 2",
						DisplayName = "2"
					},
					new ButtonSchema(73, 85)
					{
						Name = $"P{controller} Key 3",
						DisplayName = "3"
					},

					new ButtonSchema(27, 108)
					{
						Name = $"P{controller} Key 4",
						DisplayName = "4"
					},
					new ButtonSchema(50, 108)
					{
						Name = $"P{controller} Key 5",
						DisplayName = "5"
					},
					new ButtonSchema(73, 108)
					{
						Name = $"P{controller} Key 6",
						DisplayName = "6"
					},

					new ButtonSchema(27, 131)
					{
						Name = $"P{controller} Key 7",
						DisplayName = "7"
					},
					new ButtonSchema(50, 131)
					{
						Name = $"P{controller} Key 8",
						DisplayName = "8"
					},
					new ButtonSchema(73, 131)
					{
						Name = $"P{controller} Key 9",
						DisplayName = "9"
					},

					new ButtonSchema(27, 154)
					{
						Name = $"P{controller} Star",
						DisplayName = "*"
					},
					new ButtonSchema(50, 154)
					{
						Name = $"P{controller} Key 0",
						DisplayName = "0"
					},
					new ButtonSchema(73, 154)
					{
						Name = $"P{controller} Pound",
						DisplayName = "#"
					},

					new SingleFloatSchema(6, 200)
					{
						Name = $"P{controller} Disc X",
						DisplayName = "Disc",
						TargetSize = new Size(180, 55),
						MinValue = -360,
						MaxValue = 360
					},

					new ButtonSchema(126, 15)
					{
						Name = $"P{controller} Yellow",
						DisplayName = "Yellow"
					},
					new ButtonSchema(126, 40)
					{
						Name = $"P{controller} Red",
						DisplayName = "Red"
					},
					new ButtonSchema(126, 65)
					{
						Name = $"P{controller} Purple",
						DisplayName = "Purple"
					},
					new ButtonSchema(126, 90)
					{
						Name = $"P{controller} Blue",
						DisplayName = "Blue"
					}
				}
			};
		}
	}
}
