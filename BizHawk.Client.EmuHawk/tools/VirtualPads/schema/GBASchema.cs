using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.GBA;

namespace BizHawk.Client.EmuHawk
{
	[Schema("GBA")]
	// ReSharper disable once UnusedMember.Global
	public class GBASchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			yield return StandardController();
			yield return ConsoleButtons();

			if (core is MGBAHawk)
			{
				yield return TiltControls();
			}
		}

		private static PadSchema TiltControls()
		{
			return new PadSchema
			{
				DisplayName = "Tilt Controls",
				IsConsole = false,
				DefaultSize = new Size(256, 240),
				MaxSize = new Size(256, 326),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = "Tilt X",
						DisplayName = "Tilt X",
						Location = new Point(10, 15),
						Type = PadSchema.PadInputType.FloatSingle,
						TargetSize = new Size(226, 69),
						MinValue = short.MinValue,
						MaxValue = short.MaxValue
					},
					new PadSchema.ButtonSchema
					{
						Name = "Tilt Y",
						DisplayName = "Tilt Y",
						Location = new Point(10, 94),
						Type = PadSchema.PadInputType.FloatSingle,
						TargetSize = new Size(226, 69),
						MinValue = short.MinValue,
						MaxValue = short.MaxValue
					},
					new PadSchema.ButtonSchema
					{
						Name = "Tilt Z",
						DisplayName = "Tilt Z",
						Location = new Point(10, 173),
						Type = PadSchema.PadInputType.FloatSingle,
						TargetSize = new Size(226, 69),
						MinValue = short.MinValue,
						MaxValue = short.MaxValue
					},
					new PadSchema.ButtonSchema
					{
						Name = "Light Sensor",
						DisplayName = "Light Sensor",
						Location = new Point(10, 252),
						Type = PadSchema.PadInputType.FloatSingle,
						TargetSize = new Size(226, 69),
						MaxValue = byte.MaxValue
					}
				}
			};
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
						DisplayName = "",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(29, 17),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Down",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(29, 61),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Left",
						DisplayName = "",
						Icon = Properties.Resources.Back,
						Location = new Point(17, 39),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Right",
						DisplayName = "",
						Icon = Properties.Resources.Forward,
						Location = new Point(39, 39),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "B",
						DisplayName = "B",
						Location = new Point(130, 39),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "A",
						DisplayName = "A",
						Location = new Point(154, 39),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Select",
						DisplayName = "s",
						Location = new Point(64, 39),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Start",
						DisplayName = "S",
						Location = new Point(86, 39),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "L",
						DisplayName = "L",
						Location = new Point(2, 12),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "R",
						DisplayName = "R",
						Location = new Point(166, 12),
						Type = PadSchema.PadInputType.Boolean
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
					new PadSchema.ButtonSchema
					{
						Name = "Power",
						DisplayName = "Power",
						Location = new Point(10, 15),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}
	}
}
