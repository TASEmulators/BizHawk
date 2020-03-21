using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	[Schema("DGB")]
	// ReSharper disable once UnusedMember.Global
	public class DualGbSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			yield return StandardController(1);
			yield return StandardController(2);
		}

		private static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(174, 79),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Up",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(14, 12)
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Down",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(14, 56)
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Left",
						Icon = Properties.Resources.Back,
						Location = new Point(2, 34)
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Right",
						Icon = Properties.Resources.Forward,
						Location = new Point(24, 34)
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} B",
						DisplayName = "B",
						Location = new Point(122, 34)
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} A",
						DisplayName = "A",
						Location = new Point(146, 34)
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Select",
						DisplayName = "s",
						Location = new Point(52, 34)
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Start",
						DisplayName = "S",
						Location = new Point(74, 34)
					}
				}
			};
		}
	}
}
