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
					ButtonSchema.Up("Up", 14, 12),
					ButtonSchema.Down("Down", 14, 56),
					ButtonSchema.Left("Left", 2, 34),
					ButtonSchema.Right($"Right", 24, 34),
					new ButtonSchema
					{
						Name = "B",
						Location = new Point(130, 62)
					},
					new ButtonSchema
					{
						Name = "A",
						Location = new Point(154, 62)
					},
					new ButtonSchema
					{
						Name = "Option 1",
						DisplayName = "1",
						Location = new Point(100, 12)
					},
					new ButtonSchema
					{
						Name = "Option 2",
						DisplayName = "2",
						Location = new Point(100, 62)
					},
					new ButtonSchema
					{
						Name = "Pause",
						Location = new Point(100, 37)
					}
				}
			};
		}
	}
}
