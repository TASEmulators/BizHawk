using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;

namespace BizHawk.Client.EmuHawk
{
	[SchemaAttributes("NES")]
	public class NesSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			if (core is NES)
			{
				var nes = core as NES;
				var ss = nes.GetSyncSettings();

				var isFds = nes.IsFDS;
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
						case "ControllerSNES":
							throw new Exception("TODO");
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
						case "ControllerSNES":
							throw new Exception("TODO");
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
			else
				// Quicknes Can support none, one or two controllers.
			{
				var ss = ((QuickNES)core).GetSyncSettings();
				if (ss.LeftPortConnected == true && ss.RightPortConnected == true)
				{
					//Set both controllers
					yield return StandardController(1);
					yield return StandardController(2);
				}
				else if (ss.LeftPortConnected == true && ss.RightPortConnected == false)
				{
					yield return StandardController(1);
				}
				else if (ss.LeftPortConnected == false && ss.RightPortConnected == true)
				{
					yield return StandardController(1);
				}
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
				DefaultSize = new Size(356, 290),
				MaxSize = new Size(356, 290),
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
				DisplayName = "Tablet",
				IsConsole = false,
				DefaultSize = new Size(356, 290),
				MaxSize = new Size(356, 290),
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
				DisplayName = "Family Basic Keyboard",
				IsConsole = false,
				DefaultSize = new Size(560, 180),
				Buttons = new[]
				{
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " F1",
						DisplayName = "    F1    ",
						Location = new Point(23, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " F2",
						DisplayName = "    F2    ",
						Location = new Point(76, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " F3",
						DisplayName = "    F3    ",
						Location = new Point(129, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " F4",
						DisplayName = "    F4    ",
						Location = new Point(182, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " F5",
						DisplayName = "    F5    ",
						Location = new Point(235, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " F6",
						DisplayName = "    F6    ",
						Location = new Point(288, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " F7",
						DisplayName = "    F7    ",
						Location = new Point(341, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " F8",
						DisplayName = "    F8    ",
						Location = new Point(394, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " 1",
						DisplayName = "1",
						Location = new Point(36, 38),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " 2",
						DisplayName = "2",
						Location = new Point(60, 38),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " 3",
						DisplayName = "3",
						Location = new Point(84, 38),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " 4",
						DisplayName = "4",
						Location = new Point(108, 38),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " 5",
						DisplayName = "5",
						Location = new Point(132, 38),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " 6",
						DisplayName = "6",
						Location = new Point(156, 38),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " 7",
						DisplayName = "7",
						Location = new Point(180, 38),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " 8",
						DisplayName = "8",
						Location = new Point(204, 38),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " 9",
						DisplayName = "9",
						Location = new Point(228, 38),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " 0",
						DisplayName = "0",
						Location = new Point(252, 38),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Minus",
						DisplayName = "-",
						Location = new Point(276, 38),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Caret",
						DisplayName = "^",
						Location = new Point(296, 38),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Yen",
						DisplayName = "¥",
						Location = new Point(320, 38),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Stop",
						DisplayName = "STOP",
						Location = new Point(344, 38),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Escape",
						DisplayName = "ESC",
						Location = new Point(15, 61),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Q",
						DisplayName = "Q",
						Location = new Point(54, 61),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " W",
						DisplayName = "W",
						Location = new Point(80, 61),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " E",
						DisplayName = "E",
						Location = new Point(108, 61),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " R",
						DisplayName = "R",
						Location = new Point(132, 61),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " T",
						DisplayName = "T",
						Location = new Point(158, 61),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Y",
						DisplayName = "Y",
						Location = new Point(182, 61),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " U",
						DisplayName = "U",
						Location = new Point(206, 61),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " I",
						DisplayName = "I",
						Location = new Point(232, 61),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " O",
						DisplayName = "O",
						Location = new Point(252, 61),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " P",
						DisplayName = "P",
						Location = new Point(278, 61),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " At",
						DisplayName = "@",
						Location = new Point(302, 61),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Left Bracket",
						DisplayName = "[",
						Location = new Point(330, 61),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Return",
						DisplayName = "RETURN",
						Location = new Point(350, 61),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Control",
						DisplayName = "CTR",
						Location = new Point(30, 84),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " A",
						DisplayName = "A",
						Location = new Point(70, 84),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " S",
						DisplayName = "S",
						Location = new Point(94, 84),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " D",
						DisplayName = "D",
						Location = new Point(118, 84),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " F",
						DisplayName = "F",
						Location = new Point(144, 84),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " G",
						DisplayName = "G",
						Location = new Point(168, 84),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " H",
						DisplayName = "H",
						Location = new Point(194, 84),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " J",
						DisplayName = "J",
						Location = new Point(220, 84),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " K",
						DisplayName = "K",
						Location = new Point(242, 84),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " L",
						DisplayName = "L",
						Location = new Point(266, 84),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Semicolon",
						DisplayName = ";",
						Location = new Point(290, 84),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Colon",
						DisplayName = ":",
						Location = new Point(311, 84),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Right Bracket",
						DisplayName = "]",
						Location = new Point(332, 84),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " カナ",
						DisplayName = "カナ",
						Location = new Point(352, 84),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Left Shift",
						DisplayName = "SHIFT",
						Location = new Point(10, 107),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Z",
						DisplayName = "Z",
						Location = new Point(58, 107),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " X",
						DisplayName = "X",
						Location = new Point(82, 107),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " C",
						DisplayName = "C",
						Location = new Point(106, 107),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " V",
						DisplayName = "V",
						Location = new Point(130, 107),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " B",
						DisplayName = "B",
						Location = new Point(154, 107),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " N",
						DisplayName = "N",
						Location = new Point(178, 107),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " M",
						DisplayName = "M",
						Location = new Point(203, 107),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Comma",
						DisplayName = ",",
						Location = new Point(229, 107),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Period",
						DisplayName = ".",
						Location = new Point(249, 107),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Slash",
						DisplayName = "/",
						Location = new Point(270, 107),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Underscore",
						DisplayName = "_",
						Location = new Point(292, 107),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Right Shift",
						DisplayName = "SHIFT",
						Location = new Point(316, 107),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Graph",
						DisplayName = "GRPH",
						Location = new Point(82, 130),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Space",
						DisplayName = "                  SPACE                  ",
						Location = new Point(130, 130),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Clear/Home",
						DisplayName = " CLR\nHOME",
						Location = new Point(420, 46),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Insert",
						DisplayName = "\nINS",
						Location = new Point(470, 46),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Delete",
						DisplayName = "\nDEL",
						Location = new Point(506, 46),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Up",
						DisplayName = "  ↑  ",
						Location = new Point(468, 86),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Down",
						DisplayName = "  ↓  ",
						Location = new Point(468, 134),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Left",
						DisplayName = "  ←  ",
						Location = new Point(446, 110),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Right",
						DisplayName = "  ➝  ",
						Location = new Point(488, 110),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}
	}
}
