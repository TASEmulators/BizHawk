using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	[SchemaAttributes("WSWAN")]
	public class WonderSwanSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			yield return StandardController(1);
			yield return RotatedController(2);
			yield return ConsoleButtons();
		}

		private static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Standard",
				IsConsole = false,
				DefaultSize = new Size(174, 210),
				Buttons = new[]
				{
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Y1",
						DisplayName = "Y1",
						Location = new Point(23, 12),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Y4",
						DisplayName = "Y4",
						Location = new Point(9, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Y2",
						DisplayName = "Y2",
						Location = new Point(38, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Y3",
						DisplayName = "Y3",
						Location = new Point(23, 56),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " X1",
						DisplayName = "X1",
						Location = new Point(23, 92),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " X4",
						DisplayName = "X4",
						Location = new Point(9, 114),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " X2",
						DisplayName = "X2",
						Location = new Point(38, 114),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " X3",
						DisplayName = "X3",
						Location = new Point(23, 136),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Start",
						DisplayName = "S",
						Location = new Point(80, 114),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " B",
						DisplayName = "B",
						Location = new Point(110, 114),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " A",
						DisplayName = "A",
						Location = new Point(133, 103),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}

		private static PadSchema RotatedController(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Rotated",
				IsConsole = false,
				DefaultSize = new Size(174, 210),
				Buttons = new[]
				{
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " A",
						DisplayName = "A",
						Location = new Point(23, 12),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " B",
						DisplayName = "B",
						Location = new Point(46, 22),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Start",
						DisplayName = "S",
						Location = new Point(32, 58),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Y2",
						DisplayName = "Y2",
						Location = new Point(23, 112),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Y1",
						DisplayName = "Y1",
						Location = new Point(9, 134),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Y3",
						DisplayName = "Y3",
						Location = new Point(38, 134),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Y4",
						DisplayName = "Y4",
						Location = new Point(23, 156),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " X2",
						DisplayName = "X2",
						Location = new Point(103, 112),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " X1",
						DisplayName = "X1",
						Location = new Point(89, 134),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " X3",
						DisplayName = "X3",
						Location = new Point(118, 134),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " X4",
						DisplayName = "X4",
						Location = new Point(103, 156),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}

		private static PadSchema ConsoleButtons()
		{
			return new PadSchema
			{
				DisplayName = "Console",
				IsConsole = true,
				DefaultSize = new Size(75, 50),
				Buttons = new[]
				{
					new PadSchema.ButtonScema
					{
						Name = "Power",
						DisplayName = "Power",
						Location = new Point(7, 15),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}
	}
}
