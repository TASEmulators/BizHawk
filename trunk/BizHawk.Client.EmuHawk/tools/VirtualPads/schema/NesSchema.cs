using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

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

				if (ss.Controls.Famicom)
				{
					yield return new VirtualPad(StandardController(1));
					yield return new VirtualPad(Famicom2ndController());

					switch (ss.Controls.FamicomExpPort)
					{
						default:
							break;
					}
				}
				else
				{
					int currentControlerNo = 1;
					switch (ss.Controls.NesLeftPort)
					{
						default:
						case "UnpluggedNES":
						case "UnpluggedFam":
							break;
						case "ControllerNES":
							yield return new VirtualPad(StandardController(1));
							currentControlerNo++;
							break;
						case "Zapper":
							yield return new VirtualPad(Zapper(1));
							currentControlerNo++;
							break;
						case "ArkanoidNES":
							yield return new VirtualPad(ArkanoidPaddle(1));
							currentControlerNo++;
							break;
						case "FourScore":
							yield return new VirtualPad(StandardController(1));
							yield return new VirtualPad(StandardController(2));
							currentControlerNo += 2;
							break;
					}

					switch (ss.Controls.NesRightPort)
					{
						default:
						case "UnpluggedNES":
							break;
						case "ControllerNES":
							yield return new VirtualPad(StandardController(currentControlerNo));
							break;
						case "Zapper":
							yield return new VirtualPad(Zapper(currentControlerNo));
							break;
						case "ArkanoidNES":
							yield return new VirtualPad(ArkanoidPaddle(currentControlerNo));
							break;
						case "FourScore":
							yield return new VirtualPad(StandardController(currentControlerNo));
							yield return new VirtualPad(StandardController(currentControlerNo + 1));
							currentControlerNo += 2;
							break;
					}

					if (currentControlerNo == 0)
					{
						yield return null;
					}
				}
			}
			else // Quicknes only supports 2 controllers and no other configuration
			{
				yield return new VirtualPad(StandardController(1));
				yield return new VirtualPad(StandardController(2));
			}
		}

		private static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Player " + controller,
				IsConsole = false,
				DefaultSize = new Size(174, 74),
				MaxSize = new Size(174, 74),
				Buttons = new[]
				{
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Up",
						DisplayName = "",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(23, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Down",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(23, 36),
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
						Location = new Point(44, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " B",
						DisplayName = "B",
						Location = new Point(124, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " A",
						DisplayName = "A",
						Location = new Point(147, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Select",
						DisplayName = "s",
						Location = new Point(72, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Start",
						DisplayName = "S",
						Location = new Point(93, 24),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}

		private static PadSchema Famicom2ndController()
		{
			int controller = 2;
			return new PadSchema
			{
				DisplayName = "Player 2",
				IsConsole = false,
				DefaultSize = new Size(174, 74),
				MaxSize = new Size(174, 74),
				Buttons = new[]
				{
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Up",
						DisplayName = "",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(23, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Down",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(23, 36),
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
						Location = new Point(44, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " B",
						DisplayName = "B",
						Location = new Point(124, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " A",
						DisplayName = "A",
						Location = new Point(147, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Microphone",
						DisplayName = "Mic",
						Location = new Point(72, 24),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}

		private static PadSchema Zapper(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Zapper",
				IsConsole = false,
				DefaultSize = new Size(356, 260),
				MaxSize = new Size(356, 260),
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

		private static PadSchema ArkanoidPaddle(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Arkanoid Paddle",
				IsConsole = false,
				DefaultSize = new Size(380, 110),
				MaxSize = new Size(380, 110),
				Buttons = new[]
				{
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Paddle",
						DisplayName = "Arkanoid Paddle",
						Location = new Point(14, 2),
						Type = PadSchema.PadInputType.FloatSingle,
						TargetSize = new Size(375, 75),
						MaxValue = 160
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Fire",
						DisplayName = "Fire",
						Location = new Point(14, 80),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}

		// TODO
		private static PadSchema PowerPad(int controller)
		{
			return new PadSchema();
		}
	}
}
