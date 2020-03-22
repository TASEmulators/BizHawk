using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	[Schema("Lynx")]
	// ReSharper disable once UnusedMember.Global
	public class LynxSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			yield return StandardController();
		}

		private static PadSchema StandardController()
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(194, 90),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 12, "Up"),
					ButtonSchema.Down(14, 56, "Down"),
					ButtonSchema.Left(2, 34, "Left"),
					ButtonSchema.Right(24, 34, "Right"),
					new ButtonSchema(130, 62) { Name = "B" },
					new ButtonSchema(154, 62) { Name = "A" },
					new ButtonSchema(100, 12) { Name = "Option 1", DisplayName = "1" },
					new ButtonSchema(100, 62) { Name = "Option 2", DisplayName = "2" },
					new ButtonSchema(100, 37) { Name = "Pause" }
				}
			};
		}
	}
}
