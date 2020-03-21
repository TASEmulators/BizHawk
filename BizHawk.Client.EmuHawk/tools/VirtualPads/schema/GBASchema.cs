using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.GBA;

namespace BizHawk.Client.EmuHawk
{
	[Schema("GBA")]
	// ReSharper disable once UnusedMember.Global
	public class GbaSchema : IVirtualPadSchema
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
					new ButtonSchema
					{
						Name = "Tilt X",
						Location = new Point(10, 15),
						Type = PadInputType.FloatSingle,
						TargetSize = new Size(226, 69),
						MinValue = short.MinValue,
						MaxValue = short.MaxValue
					},
					new ButtonSchema
					{
						Name = "Tilt Y",
						Location = new Point(10, 94),
						Type = PadInputType.FloatSingle,
						TargetSize = new Size(226, 69),
						MinValue = short.MinValue,
						MaxValue = short.MaxValue
					},
					new ButtonSchema
					{
						Name = "Tilt Z",
						Location = new Point(10, 173),
						Type = PadInputType.FloatSingle,
						TargetSize = new Size(226, 69),
						MinValue = short.MinValue,
						MaxValue = short.MaxValue
					},
					new ButtonSchema
					{
						Name = "Light Sensor",
						Location = new Point(10, 252),
						Type = PadInputType.FloatSingle,
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
					new ButtonSchema
					{
						Name = "Up",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(29, 17)
					},
					new ButtonSchema
					{
						Name = "Down",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(29, 61)
					},
					new ButtonSchema
					{
						Name = "Left",
						Icon = Properties.Resources.Back,
						Location = new Point(17, 39)
					},
					new ButtonSchema
					{
						Name = "Right",
						Icon = Properties.Resources.Forward,
						Location = new Point(39, 39)
					},
					new ButtonSchema
					{
						Name = "B",
						Location = new Point(130, 39)
					},
					new ButtonSchema
					{
						Name = "A",
						Location = new Point(154, 39)
					},
					new ButtonSchema
					{
						Name = "Select",
						DisplayName = "s",
						Location = new Point(64, 39)
					},
					new ButtonSchema
					{
						Name = "Start",
						DisplayName = "S",
						Location = new Point(86, 39)
					},
					new ButtonSchema
					{
						Name = "L",
						Location = new Point(2, 12)
					},
					new ButtonSchema
					{
						Name = "R",
						Location = new Point(166, 12)
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
