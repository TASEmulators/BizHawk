using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;

namespace BizHawk.Client.EmuHawk
{
	[SchemaAttributes("NES")]
	public class NesSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas()
		{
			if (Global.Emulator is NES)
			{
				var ss = (NES.NESSyncSettings)Global.Emulator.GetSyncSettings();
				var core = (Global.Emulator as NES);
				var isFds = core.BoardName == "FDS";
				if (ss.Controls.Famicom)
				{
					yield return StandardController(1);
					yield return Famicom2ndController();

					switch (ss.Controls.FamicomExpPort)
					{
						default:
						case "UnpluggedFam":
							break;
						case "Zapper":
							yield return Zapper(3);
							break;
						case "ArkanoidFam":
							yield return ArkanoidPaddle(3);
							break;
						case "Famicom4P":
							yield return StandardController(3);
							yield return StandardController(4);
							break;
						case "FamilyBasicKeyboard":
							yield return FamicomFamilyKeyboard(3);
							break;
						case "OekaKids":
							yield return OekaKidsTablet(3);
							break;

					}
				}
				else
				{
					var currentControlerNo = 1;
					switch (ss.Controls.NesLeftPort)
					{
						default:
						case "UnpluggedNES":
							break;
						case "ControllerNES":
							yield return StandardController(1);
							currentControlerNo++;
							break;
						case "Zapper":
							yield return Zapper(1);
							currentControlerNo++;
							break;
						case "ArkanoidNES":
							yield return ArkanoidPaddle(1);
							currentControlerNo++;
							break;
						case "FourScore":
							yield return StandardController(1);
							yield return StandardController(2);
							currentControlerNo += 2;
							break;
						case "PowerPad":
							yield return PowerPad(1);
							currentControlerNo++;
							break;
					}

					switch (ss.Controls.NesRightPort)
					{
						default:
						case "UnpluggedNES":
							break;
						case "ControllerNES":
							yield return StandardController(currentControlerNo);
							break;
						case "Zapper":
							yield return Zapper(currentControlerNo);
							break;
						case "ArkanoidNES":
							yield return ArkanoidPaddle(currentControlerNo);
							break;
						case "FourScore":
							yield return StandardController(currentControlerNo);
							yield return StandardController(currentControlerNo + 1);
							currentControlerNo += 2;
							break;
						case "PowerPad":
							yield return PowerPad(currentControlerNo);
							break;
					}

					if (currentControlerNo == 0)
					{
						yield return null;
					}
				}

				if (isFds)
				{
					yield return FdsConsoleButtons(core.ControllerDefinition.BoolButtons.Count(b => b.StartsWith("FDS Insert ")));
				}
				else
				{
					yield return NesConsoleButtons();
				}
			}
			else // Quicknes only supports 2 controllers and no other configuration
			{
				yield return StandardController(1);
				yield return StandardController(2);
				yield return NesConsoleButtons();
			}
		}

		private static PadSchema NesConsoleButtons()
		{
			return new PadSchema
			{
				DisplayName = "Console",
				IsConsole = true,
				DefaultSize = new Size(150, 50),
				Buttons = new[]
				{
					new PadSchema.ButtonScema
					{
						Name = "Reset",
						DisplayName = "Reset",
						Location = new Point(10, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "Power",
						DisplayName = "Power",
						Location = new Point(58, 15),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}

		private static PadSchema FdsConsoleButtons(int diskSize)
		{
			var buttons = new List<PadSchema.ButtonScema>
			{
				new PadSchema.ButtonScema
				{
					Name = "Reset",
					DisplayName = "Reset",
					Location = new Point(10, 15),
					Type = PadSchema.PadInputType.Boolean
				},
				new PadSchema.ButtonScema
				{
					Name = "Power",
					DisplayName = "Power",
					Location = new Point(58, 15),
					Type = PadSchema.PadInputType.Boolean
				},
				new PadSchema.ButtonScema
				{
					Name = "FDS Eject",
					DisplayName = "Eject",
					Location = new Point(108, 15),
					Type = PadSchema.PadInputType.Boolean
				}
			};

			for (var i = 0; i < diskSize; i++)
			{
				buttons.Add(new PadSchema.ButtonScema
				{
					Name = "FDS Insert " + i,
					DisplayName = "Insert " + i,
					Location = new Point(10 + (i * 58), 50),
					Type = PadSchema.PadInputType.Boolean
				});
			}

			var width = 20 + (diskSize * 58);
			if (width < 160)
			{
				width = 160;
			}

			return new PadSchema
			{
				DisplayName = "Console",
				IsConsole = true,
				DefaultSize = new Size(width, 100),
				Buttons = buttons
			};
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
			var controller = 2;
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
						Location = new Point(14, 17),
						Type = PadSchema.PadInputType.TargetedPair,
						TargetSize = new Size(256, 240),
						SecondaryNames = new []
						{
							"P" + controller + " Zapper Y",
							"P" + controller + " Fire"
						}
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Fire",
						DisplayName = "Fire",
						Location = new Point(284, 17),
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
						Location = new Point(14, 17),
						Type = PadSchema.PadInputType.FloatSingle,
						TargetSize = new Size(380, 69),
						MaxValue = 160
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Fire",
						DisplayName = "Fire",
						Location = new Point(14, 85),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}

		private static PadSchema PowerPad(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Power Pad",
				IsConsole = false,
				DefaultSize = new Size(154, 114),
				Buttons = new[]
				{
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " PP1",
						DisplayName = "1  ",
						Location = new Point(14, 17),
						Type = PadSchema.PadInputType.Boolean,
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " PP2",
						DisplayName = "2  ",
						Location = new Point(45, 17),
						Type = PadSchema.PadInputType.Boolean,
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " PP3",
						DisplayName = "3  ",
						Location = new Point(76, 17),
						Type = PadSchema.PadInputType.Boolean,
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " PP4",
						DisplayName = "4  ",
						Location = new Point(107, 17),
						Type = PadSchema.PadInputType.Boolean,
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " PP5",
						DisplayName = "5  ",
						Location = new Point(14, 48),
						Type = PadSchema.PadInputType.Boolean,
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " PP6",
						DisplayName = "6  ",
						Location = new Point(45, 48),
						Type = PadSchema.PadInputType.Boolean,
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " PP7",
						DisplayName = "7  ",
						Location = new Point(76, 48),
						Type = PadSchema.PadInputType.Boolean,
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " PP8",
						DisplayName = "8  ",
						Location = new Point(107, 48),
						Type = PadSchema.PadInputType.Boolean,
					},

					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " PP9",
						DisplayName = "9  ",
						Location = new Point(14, 79),
						Type = PadSchema.PadInputType.Boolean,
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " PP10",
						DisplayName = "10",
						Location = new Point(45, 79),
						Type = PadSchema.PadInputType.Boolean,
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " PP11",
						DisplayName = "11",
						Location = new Point(76, 79),
						Type = PadSchema.PadInputType.Boolean,
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " PP12",
						DisplayName = "12",
						Location = new Point(107, 79),
						Type = PadSchema.PadInputType.Boolean,
					}
				}
			};
		}

		private static PadSchema OekaKidsTablet(int controller)
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
						Name = "P" + controller + " Pen X",
						Location = new Point(14, 17),
						Type = PadSchema.PadInputType.TargetedPair,
						TargetSize = new Size(256, 240),
						SecondaryNames = new []
						{
							"P" + controller + " Pen Y",
							"P" + controller + " Click"
						}
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Click",
						DisplayName = "Click",
						Location = new Point(284, 17),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Touch",
						DisplayName = "Touch",
						Location = new Point(284, 48),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}

		private static PadSchema FamicomFamilyKeyboard(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Family Basic Keyoboard",
				IsConsole = false,
				DefaultSize = new Size(320, 240),
				Buttons = new[]
				{
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " F1",
						DisplayName = "    F1    ",
						Location = new Point(23, 15),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}
	}
}
