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
					new PadSchema.ButtonSchema
					{
						Name = "Up",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(14, 12)
					},
					new PadSchema.ButtonSchema
					{
						Name = "Down",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(14, 56)
					},
					new PadSchema.ButtonSchema
					{
						Name = "Left",
						Icon = Properties.Resources.Back,
						Location = new Point(2, 34)
					},
					new PadSchema.ButtonSchema
					{
						Name = "Right",
						Icon = Properties.Resources.Forward,
						Location = new Point(24, 34)
					},
					new PadSchema.ButtonSchema
					{
						Name = "B",
						DisplayName = "B",
						Location = new Point(130, 62)
					},
					new PadSchema.ButtonSchema
					{
						Name = "A",
						DisplayName = "A",
						Location = new Point(154, 62)
					},
					new PadSchema.ButtonSchema
					{
						Name = "Option 1",
						DisplayName = "1",
						Location = new Point(100, 12)
					},
					new PadSchema.ButtonSchema
					{
						Name = "Option 2",
						DisplayName = "2",
						Location = new Point(100, 62)
					},
					new PadSchema.ButtonSchema
					{
						Name = "Pause",
						DisplayName = "Pause",
						Location = new Point(100, 37)
					}
				}
			};
		}
	}
}
