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
					ButtonSchema.Up(14, 12, "Up"),
					ButtonSchema.Down(14, 56, "Down"),
					ButtonSchema.Left(2, 34, "Left"),
					ButtonSchema.Right(24, 34, "Right"),
					new ButtonSchema(74, 34)
					{
						Name = "B",
						DisplayName = "B"
					},
					new ButtonSchema(98, 34)
					{
						Name = "A",
						DisplayName = "A"
					},
					new ButtonSchema(146, 12)
					{
						Name = "Option",
						DisplayName = "O"
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
					new ButtonSchema(10, 15) { Name = "Power" }
				}
			};
		}
	}
}
