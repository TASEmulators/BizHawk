using System.Collections.Generic;
using System.Drawing;

namespace BizHawk.Client.EmuHawk
{
	public class NesSchema : IVirtualPadSchema
	{
		public IEnumerable<VirtualPad> GetPads()
		{
			yield return new VirtualPad(StandardController(1))
				{
					Location = new Point(15, 15)
				};

			yield return new VirtualPad(Zapper(2))
				{
					Location = new Point(200, 15)
				};
		}

		private static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(174, 74),
				Buttons = new[]
				{
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Up",
						DisplayName = "",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(14, 2),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Down",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(14, 46),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Left",
						DisplayName = "",
						Icon = Properties.Resources.Back,
						Location = new Point(2, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Right",
						DisplayName = "",
						Icon = Properties.Resources.Forward,
						Location = new Point(24, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " B",
						DisplayName = "B",
						Location = new Point(122, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " A",
						DisplayName = "A",
						Location = new Point(146, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Select",
						DisplayName = "s",
						Location = new Point(52, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Start",
						DisplayName = "S",
						Location = new Point(74, 24),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}

		private static PadSchema Zapper(int controller)
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(356, 260),
				Buttons = new[]
				{
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Zapper X",
						Location = new Point(14, 2),
						Type = PadSchema.PadInputType.TargetedPair,
						TargetSize = new Size(256, 240),
						SecondaryNames = new []
						{
							"P" + controller + " Zapper Y",
							"P" + controller + " Fire",
						}
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Fire",
						DisplayName = "Fire",
						Location = new Point(284, 2),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}

		// TODO
		private static PadSchema ArkanoidPaddle()
		{
			return new PadSchema();
		}

		// TODO
		private static PadSchema PowerPad()
		{
			return new PadSchema();
		}
	}
}
