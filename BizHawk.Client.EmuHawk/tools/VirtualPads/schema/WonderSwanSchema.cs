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
				IsConsole = false,
				DefaultSize = new Size(174, 210),
				Buttons = new[]
				{
					new ButtonSchema
					{
						Name = $"P{controller} Y1",
						DisplayName = "Y1",
						Location = new Point(23, 12)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Y4",
						DisplayName = "Y4",
						Location = new Point(9, 34)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Y2",
						DisplayName = "Y2",
						Location = new Point(38, 34)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Y3",
						DisplayName = "Y3",
						Location = new Point(23, 56)
					},

					new ButtonSchema
					{
						Name = $"P{controller} X1",
						DisplayName = "X1",
						Location = new Point(23, 92)
					},
					new ButtonSchema
					{
						Name = $"P{controller} X4",
						DisplayName = "X4",
						Location = new Point(9, 114)
					},
					new ButtonSchema
					{
						Name = $"P{controller} X2",
						DisplayName = "X2",
						Location = new Point(38, 114)
					},
					new ButtonSchema
					{
						Name = $"P{controller} X3",
						DisplayName = "X3",
						Location = new Point(23, 136)
					},

					new ButtonSchema
					{
						Name = $"P{controller} Start",
						DisplayName = "S",
						Location = new Point(80, 114)
					},

					new ButtonSchema
					{
						Name = $"P{controller} B",
						DisplayName = "B",
						Location = new Point(110, 114)
					},

					new ButtonSchema
					{
						Name = $"P{controller} A",
						DisplayName = "A",
						Location = new Point(133, 103)
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
					new ButtonSchema
					{
						Name = $"P{controller} A",
						DisplayName = "A",
						Location = new Point(23, 12)
					},
					new ButtonSchema
					{
						Name = $"P{controller} B",
						DisplayName = "B",
						Location = new Point(46, 22)
					},

					new ButtonSchema
					{
						Name = $"P{controller} Start",
						DisplayName = "S",
						Location = new Point(32, 58)
					},

					new ButtonSchema
					{
						Name = $"P{controller} Y2",
						DisplayName = "Y2",
						Location = new Point(23, 112)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Y1",
						DisplayName = "Y1",
						Location = new Point(9, 134)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Y3",
						DisplayName = "Y3",
						Location = new Point(38, 134)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Y4",
						DisplayName = "Y4",
						Location = new Point(23, 156)
					},

					new ButtonSchema
					{
						Name = $"P{controller} X2",
						DisplayName = "X2",
						Location = new Point(103, 112)
					},
					new ButtonSchema
					{
						Name = $"P{controller} X1",
						DisplayName = "X1",
						Location = new Point(89, 134)
					},
					new ButtonSchema
					{
						Name = $"P{controller} X3",
						DisplayName = "X3",
						Location = new Point(118, 134)
					},
					new ButtonSchema
					{
						Name = $"P{controller} X4",
						DisplayName = "X4",
						Location = new Point(103, 156)
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
					new ButtonSchema
					{
						Name = "Power",
						Location = new Point(7, 15)
					}
				}
			};
		}
	}
}
