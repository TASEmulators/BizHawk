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
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Up",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(50, 11),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Down",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(50, 32),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Left",
						Icon = Properties.Resources.Back,
						Location = new Point(29, 22),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Right",
						Icon = Properties.Resources.Forward,
						Location = new Point(71, 22),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} L",
						DisplayName = "L",
						Location = new Point(3, 42),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} R",
						DisplayName = "R",
						Location = new Point(100, 42),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Key 1",
						DisplayName = "1",
						Location = new Point(27, 85),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Key 2",
						DisplayName = "2",
						Location = new Point(50, 85),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Key 3",
						DisplayName = "3",
						Location = new Point(73, 85),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Key 4",
						DisplayName = "4",
						Location = new Point(27, 108),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Key 5",
						DisplayName = "5",
						Location = new Point(50, 108),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Key 6",
						DisplayName = "6",
						Location = new Point(73, 108),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Key 7",
						DisplayName = "7",
						Location = new Point(27, 131),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Key 8",
						DisplayName = "8",
						Location = new Point(50, 131),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Key 9",
						DisplayName = "9",
						Location = new Point(73, 131),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Star",
						DisplayName = "*",
						Location = new Point(27, 154),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Key 0",
						DisplayName = "0",
						Location = new Point(50, 154),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Pound",
						DisplayName = "#",
						Location = new Point(73, 154),
						Type = PadSchema.PadInputType.Boolean
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
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Disc X",
						AxisRange = controllerDefRanges[0],
						SecondaryAxisRange = controllerDefRanges[1],
						Location = new Point(6, 14),
						Type = PadSchema.PadInputType.AnalogStick
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Pedal",
						DisplayName = "Pedal",
						Location = new Point(6, 224),
						Type = PadSchema.PadInputType.Boolean
					},
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
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Up",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(50, 11),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Down",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(50, 32),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Left",
						Icon = Properties.Resources.Back,
						Location = new Point(29, 22),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Right",
						Icon = Properties.Resources.Forward,
						Location = new Point(71, 22),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Key 1",
						DisplayName = "1",
						Location = new Point(27, 85),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Key 2",
						DisplayName = "2",
						Location = new Point(50, 85),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Key 3",
						DisplayName = "3",
						Location = new Point(73, 85),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Key 4",
						DisplayName = "4",
						Location = new Point(27, 108),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Key 5",
						DisplayName = "5",
						Location = new Point(50, 108),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Key 6",
						DisplayName = "6",
						Location = new Point(73, 108),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Key 7",
						DisplayName = "7",
						Location = new Point(27, 131),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Key 8",
						DisplayName = "8",
						Location = new Point(50, 131),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Key 9",
						DisplayName = "9",
						Location = new Point(73, 131),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Star",
						DisplayName = "*",
						Location = new Point(27, 154),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Key 0",
						DisplayName = "0",
						Location = new Point(50, 154),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Pound",
						DisplayName = "#",
						Location = new Point(73, 154),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Disc X",
						DisplayName = "Disc",
						Location = new Point(6, 200),
						TargetSize = new Size(180, 55),
						MinValue = -360,
						MaxValue = 360,
						Type = PadSchema.PadInputType.FloatSingle
					},

					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Yellow",
						DisplayName = "Yellow",
						Location = new Point(126, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Red",
						DisplayName = "Red",
						Location = new Point(126, 40),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Purple",
						DisplayName = "Purple",
						Location = new Point(126, 65),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Blue",
						DisplayName = "Blue",
						Location = new Point(126, 90),
						Type = PadSchema.PadInputType.Boolean
					},
				}
			};
		}
	}
}
