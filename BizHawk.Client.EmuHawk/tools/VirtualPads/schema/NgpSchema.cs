using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	[Schema("NGP")]
	// ReSharper disable once UnusedMember.Global
	public class NgpSchema : IVirtualPadSchema
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
				DefaultSize = new Size(174, 79),
				Buttons = new[]
				{
					ButtonSchema.Up("Up", 14, 12),
					ButtonSchema.Down("Down", 14, 56),
					ButtonSchema.Left("Left", 2, 34),
					ButtonSchema.Right("Right", 24, 34),
					new ButtonSchema
					{
						Name = "B",
						DisplayName = "B",
						Location = new Point(74, 34)
					},
					new ButtonSchema
					{
						Name = "A",
						DisplayName = "A",
						Location = new Point(98, 34)
					},
					new ButtonSchema
					{
						Name = "Option",
						DisplayName = "O",
						Location = new Point(146, 12)
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
