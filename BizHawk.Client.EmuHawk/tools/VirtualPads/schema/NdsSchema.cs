using System.Collections.Generic;
using System.Drawing;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk.tools.VirtualPads.schema
{
	[Schema("NDS")]
	// ReSharper disable once UnusedMember.Global
	public class NdsSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			yield return ControllerButtons();
			yield return Console();
		}

		private static PadSchema Console()
		{
			return new PadSchema
			{
				IsConsole = true,
				DefaultSize = new Size(50, 35),
				Buttons = new []
				{
					new ButtonSchema
					{
						Name = "Lid",
						Location = new Point(8, 8)
					}
				}
			};
		}

		private static PadSchema ControllerButtons()
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(440, 260),
				Buttons = new []
				{
					new ButtonSchema
					{
						Name = "Up",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(14, 79)
					},
					new ButtonSchema
					{
						Name = "Down",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(14, 122)
					},
					new ButtonSchema
					{
						Name = "Left",
						Icon = Properties.Resources.Back,
						Location = new Point(2, 100)
					},
					new ButtonSchema
					{
						Name = "Right",
						Icon = Properties.Resources.Forward,
						Location = new Point(24, 100)
					},
					new ButtonSchema
					{
						Name = "L",
						Location = new Point(2, 10)
					},
					new ButtonSchema
					{
						Name = "R",
						Location = new Point(366, 10)
					},
					new ButtonSchema
					{
						Name = "Start",
						Location = new Point(341, 179)
					},
					new ButtonSchema
					{
						Name = "Select",
						Location = new Point(341, 201)
					},

					new ButtonSchema
					{
						Name = "Y",
						Location = new Point(341, 100)
					},
					new ButtonSchema
					{
						Name = "B",
						Location = new Point(365, 113)
					},
					new ButtonSchema
					{
						Name = "X",
						Location = new Point(341, 76)
					},
					new ButtonSchema
					{
						Name = "A",
						DisplayName = "A",
						Location = new Point(366, 86)
					},

					// Screen
					new ButtonSchema
					{
						Name = "TouchX",
						Location = new Point(72, 35),
						Type = PadInputType.TargetedPair,
						TargetSize = new Size(256, 192),
						SecondaryNames = new[]
						{
							"TouchY"
						}
					},
					new ButtonSchema
					{
						Name = "Touch",
						Location = new Point(72, 10)
					}
				}
			};
		}
	}
}
