using System.Collections.Generic;
using System.Drawing;

using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Intellivision;

namespace BizHawk.Client.EmuHawk
{
	[Schema("INTV")]
	public class IntvSchema : IVirtualPadSchema
	{
		private string UnpluggedControllerName => typeof(UnpluggedController).DisplayName();
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
				DisplayName = "Player " + controller,
				IsConsole = false,
				DefaultSize = new Size(148, 332),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Key 1",
						DisplayName = "1",
						Location = new Point(25, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Key 2",
						DisplayName = "2",
						Location = new Point(51, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Key 3",
						DisplayName = "3",
						Location = new Point(77, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Key 4",
						DisplayName = "4",
						Location = new Point(25, 41),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Key 5",
						DisplayName = "5",
						Location = new Point(51, 41),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Key 6",
						DisplayName = "6",
						Location = new Point(77, 41),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Key 7",
						DisplayName = "7",
						Location = new Point(25, 67),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Key 8",
						DisplayName = "8",
						Location = new Point(51, 67),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Key 9",
						DisplayName = "9",
						Location = new Point(77, 67),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Clear",
						DisplayName = "C",
						Location = new Point(25, 93),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Key 0",
						DisplayName = "0",
						Location = new Point(51, 93),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Enter",
						DisplayName = "E",
						Location = new Point(77, 93),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Top",
						DisplayName = "T",
						Location = new Point(2, 41),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Top",
						DisplayName = "T",
						Location = new Point(100, 41),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " L",
						DisplayName = "L",
						Location = new Point(2, 67),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " R",
						DisplayName = "R",
						Location = new Point(100, 67),
						Type = PadSchema.PadInputType.Boolean
					},

					/************** Directional Pad *******************/

					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " N",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(51, 124),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " NNE",
						Icon = Properties.Resources.NNE,
						Location = new Point(63, 145),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " NNW",
						Icon = Properties.Resources.NNW,
						Location = new Point(39, 145),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " NE",
						Icon = Properties.Resources.NE,
						Location = new Point(75, 166),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " NW",
						Icon = Properties.Resources.NW,
						Location = new Point(27, 166),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " ENE",
						Icon = Properties.Resources.ENE,
						Location = new Point(87, 187),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " WNW",
						Icon = Properties.Resources.WNW,
						Location = new Point(15, 187),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " E",
						Icon = Properties.Resources.Forward,
						Location = new Point(99, 208),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " W",
						Icon = Properties.Resources.Back,
						Location = new Point(3, 208),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " ESE",
						Icon = Properties.Resources.ESE,
						Location = new Point(87, 229),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " WSW",
						Icon = Properties.Resources.WSW,
						Location = new Point(15, 229),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " SE",
						Icon = Properties.Resources.SE,
						Location = new Point(75, 250),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " SW",
						Icon = Properties.Resources.SW,
						Location = new Point(27, 250),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " SSE",
						Icon = Properties.Resources.SSE,
						Location = new Point(63, 271),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " SSW",
						Icon = Properties.Resources.SSW,
						Location = new Point(39, 271),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " S",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(51, 292),
						Type = PadSchema.PadInputType.Boolean
					},
				}
			};
		}

		private static PadSchema AnalogController(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Player " + controller,
				IsConsole = false,
				DefaultSize = new Size(280, 332),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Key 1",
						DisplayName = "1",
						Location = new Point(91, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Key 2",
						DisplayName = "2",
						Location = new Point(117, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Key 3",
						DisplayName = "3",
						Location = new Point(143, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Key 4",
						DisplayName = "4",
						Location = new Point(91, 41),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Key 5",
						DisplayName = "5",
						Location = new Point(117, 41),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Key 6",
						DisplayName = "6",
						Location = new Point(143, 41),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Key 7",
						DisplayName = "7",
						Location = new Point(91, 67),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Key 8",
						DisplayName = "8",
						Location = new Point(117, 67),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Key 9",
						DisplayName = "9",
						Location = new Point(143, 67),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Clear",
						DisplayName = "C",
						Location = new Point(91, 93),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Key 0",
						DisplayName = "0",
						Location = new Point(117, 93),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Enter",
						DisplayName = "E",
						Location = new Point(143, 93),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Top",
						DisplayName = "T",
						Location = new Point(68, 41),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Top",
						DisplayName = "T",
						Location = new Point(166, 41),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " L",
						DisplayName = "L",
						Location = new Point(68, 67),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " R",
						DisplayName = "R",
						Location = new Point(166, 67),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Disc X",
						MinValue = -127,
						MidValue = 0,
						MaxValue = 127,
						MinValueSec = 127,
						MidValueSec = 0,
						MaxValueSec = -127,
						DisplayName = "",
						Location = new Point(1, 121),
						Type = PadSchema.PadInputType.AnalogStick
					}
				}
			};
		}
	}
}
