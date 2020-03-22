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
			yield return Controller();
			yield return ConsoleButtons();
		}

		private static PadSchema Controller()
		{
			return new PadSchema
			{
				Size = new Size(174, 79),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 12),
					ButtonSchema.Down(14, 56),
					ButtonSchema.Left(2, 34),
					ButtonSchema.Right(24, 34),
					new ButtonSchema(74, 34, "B"),
					new ButtonSchema(98, 34, "A"),
					new ButtonSchema(146, 12, "Option") { DisplayName = "O" }
				}
			};
		}

		private static PadSchema ConsoleButtons()
		{
			return new ConsoleSchema
			{
				Size = new Size(75, 50),
				Buttons = new[]
				{
					new ButtonSchema(10, 15, "Power")
				}
			};
		}
	}
}
