using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	[Schema("WSWAN")]
	// ReSharper disable once UnusedMember.Global
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
				DefaultSize = new Size(174, 210),
				Buttons = new[]
				{
					new ButtonSchema(23, 12, controller, "Y1")
					{
						DisplayName = "Y1"
					},
					new ButtonSchema(9, 34, controller, "Y4")
					{
						DisplayName = "Y4"
					},
					new ButtonSchema(38, 34, controller, "Y2")
					{
						DisplayName = "Y2"
					},
					new ButtonSchema(23, 56, controller, "Y3")
					{
						DisplayName = "Y3"
					},
					new ButtonSchema(23, 92, controller, "X1")
					{
						DisplayName = "X1"
					},
					new ButtonSchema(9, 114, controller, "X4")
					{
						DisplayName = "X4"
					},
					new ButtonSchema(38, 114, controller, "X2")
					{
						DisplayName = "X2"
					},
					new ButtonSchema(23, 136, controller, "X3")
					{
						DisplayName = "X3"
					},
					new ButtonSchema(80, 114, controller, "Start")
					{
						DisplayName = "S"
					},
					new ButtonSchema(110, 114, controller, "B")
					{
						DisplayName = "B"
					},
					new ButtonSchema(133, 103, controller, "A")
					{
						DisplayName = "A"
					}
				}
			};
		}

		private static PadSchema RotatedController(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Rotated",
				DefaultSize = new Size(174, 210),
				Buttons = new[]
				{
					new ButtonSchema(23, 12, controller, "A")
					{
						DisplayName = "A"
					},
					new ButtonSchema(46, 22, controller, "B")
					{
						DisplayName = "B"
					},
					new ButtonSchema(32, 58, controller, "Start")
					{
						DisplayName = "S"
					},
					new ButtonSchema(23, 112, controller, "Y2")
					{
						DisplayName = "Y2"
					},
					new ButtonSchema(9, 134, controller, "Y1")
					{
						DisplayName = "Y1"
					},
					new ButtonSchema(38, 134, controller, "Y3")
					{
						DisplayName = "Y3"
					},
					new ButtonSchema(23, 156, controller, "Y4")
					{
						DisplayName = "Y4"
					},
					new ButtonSchema(103, 112, controller, "X2")
					{
						DisplayName = "X2"
					},
					new ButtonSchema(89, 134, controller, "X1")
					{
						DisplayName = "X1"
					},
					new ButtonSchema(118, 134, controller, "X3")
					{
						DisplayName = "X3"
					},
					new ButtonSchema(103, 156, controller, "X4")
					{
						DisplayName = "X4"
					}
				}
			};
		}

		private static PadSchema ConsoleButtons()
		{
			return new ConsoleSchema
			{
				DefaultSize = new Size(75, 50),
				Buttons = new[]
				{
					new ButtonSchema(7, 15, "Power")
				}
			};
		}
	}
}
