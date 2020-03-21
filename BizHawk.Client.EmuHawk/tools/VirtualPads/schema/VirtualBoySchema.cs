using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	[Schema("VB")]
	// ReSharper disable once UnusedMember.Global
	public class VirtualBoySchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			yield return StandardController();
			yield return ConsoleButtons();
		}

		private static PadSchema StandardController()
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(222, 103),
				Buttons = new[]
				{
					ButtonSchema.Up("L_Up", 14, 36),
					ButtonSchema.Down("L_Down", 14, 80),
					ButtonSchema.Left("L_Left", 2, 58),
					ButtonSchema.Right("L_Right", 24, 58),
					new ButtonSchema
					{
						Name = "B",
						Location = new Point(122, 58)
					},
					new ButtonSchema
					{
						Name = "A",
						Location = new Point(146, 58)
					},
					new ButtonSchema
					{
						Name = "Select",
						DisplayName = "s",
						Location = new Point(52, 58)
					},
					new ButtonSchema
					{
						Name = "Start",
						DisplayName = "S",
						Location = new Point(74, 58)
					},
					ButtonSchema.Up("R_Up", 188, 36),
					ButtonSchema.Down("R_Down", 188, 80),
					ButtonSchema.Left("R_Left", 176, 58),
					ButtonSchema.Right("R_Right", 198, 58),
					new ButtonSchema
					{
						Name = "L",
						Location = new Point(24, 8)
					},
					new ButtonSchema
					{
						Name = "R",
						Location = new Point(176, 8)
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
						Location = new Point(10, 15)
					}
				}
			};
		}
	}
}
