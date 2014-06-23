using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;

namespace BizHawk.Client.EmuHawk
{
	[SchemaAttributes("NES")]
	public class NesSchema : IVirtualPadSchema
	{
		public IEnumerable<VirtualPad> GetPads()
		{
			if (Global.Emulator is NES)
			{
				var ss = (NES.NESSyncSettings)Global.Emulator.GetSyncSettings();

				PadSchema schemaL = null;
				switch(ss.Controls.NesLeftPort)
				{
					default:
					case "UnpluggedNES":
						break;
					case "ControllerNES":
						schemaL = StandardController(1);
						break;
					case "Zapper":
						schemaL = Zapper(1);
						break;
				}

				if (schemaL != null)
				{
					yield return new VirtualPad(schemaL)
					{
						Location = new Point(15, 15)
					};
				}

				PadSchema schemaR = null;
				switch (ss.Controls.NesRightPort)
				{
					default:
					case "UnpluggedNES":
						break;
					case "ControllerNES":
						schemaR = StandardController(2);
						break;
					case "Zapper":
						schemaR = Zapper(2);
						break;
				}

				if (schemaR != null)
				{
					yield return new VirtualPad(schemaR)
					{
						Location = new Point(200, 15)
					};
				}
			}
			else // Quicknes only supports 2 controllers and no other configuration
			{
				yield return new VirtualPad(StandardController(1))
				{
					Location = new Point(15, 15)
				};

				yield return new VirtualPad(StandardController(2))
				{
					Location = new Point(200, 15)
				};
			}
			
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
