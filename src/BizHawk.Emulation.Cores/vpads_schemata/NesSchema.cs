using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SubNESHawk;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;

namespace BizHawk.Emulation.Cores
{
	[Schema(VSystemID.Raw.NES)]
	public class NesSchema : IVirtualPadSchema
	{
		/// <exception cref="Exception">found <c>ControllerSNES</c></exception>
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
		{
			if (core is NES || core is SubNESHawk)
			{
				NES.NESSyncSettings ss = null;
				bool isFds = false;
				int fdsButtonCount = 0;
				if (core is NES nesHawk)
				{
					ss = nesHawk.GetSyncSettings();
					isFds = nesHawk.IsFDS;
					fdsButtonCount = nesHawk.ControllerDefinition.BoolButtons.Count(b => b.StartsWithOrdinal("FDS Insert "));
				}
				else if (core is SubNESHawk subNesHawk)
				{
					ss = subNesHawk.GetSyncSettings();
					isFds = subNesHawk.IsFds;
					fdsButtonCount = subNesHawk.ControllerDefinition.BoolButtons.Count(b => b.StartsWithOrdinal("FDS Insert "));
				}

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
					var currentControllerNo = 1;
					switch (ss.Controls.NesLeftPort)
					{
						default:
						case "UnpluggedNES":
							break;
						case "ControllerNES":
							yield return StandardController(1);
							currentControllerNo++;
							break;
						case "Zapper":
							yield return Zapper(1);
							currentControllerNo++;
							break;
						case "ArkanoidNES":
							yield return ArkanoidPaddle(1);
							currentControllerNo++;
							break;
						case "FourScore":
							yield return StandardController(1);
							yield return StandardController(2);
							currentControllerNo += 2;
							break;
						case "PowerPad":
							yield return PowerPad(1);
							currentControllerNo++;
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
							yield return StandardController(currentControllerNo);
							break;
						case "Zapper":
							yield return Zapper(currentControllerNo);
							break;
						case "ArkanoidNES":
							yield return ArkanoidPaddle(currentControllerNo);
							break;
						case "FourScore":
							yield return StandardController(currentControllerNo);
							yield return StandardController(currentControllerNo + 1);
							currentControllerNo += 2;
							break;
						case "PowerPad":
							yield return PowerPad(currentControllerNo);
							break;
						case "ControllerSNES":
							throw new Exception("TODO");
					}

					if (currentControllerNo == 0)
					{
						yield return null;
					}
				}

				if (isFds)
				{
					yield return FdsConsoleButtons(fdsButtonCount);
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
				var playerNo = 1;
				switch (ss.Port1)
				{
					case QuickNES.Port1PeripheralOption.Gamepad:
						yield return StandardController(playerNo++);
						break;
					case QuickNES.Port1PeripheralOption.FourScore:
						throw new NotImplementedException("TODO");
				}
				switch (ss.Port2)
				{
					case QuickNES.Port2PeripheralOption.Gamepad:
						yield return StandardController(playerNo++);
						break;
					case QuickNES.Port2PeripheralOption.FourScore2:
						throw new NotImplementedException("TODO");
				}
				yield return NesConsoleButtons();
			}
		}

		private static PadSchema NesConsoleButtons()
		{
			return new ConsoleSchema
			{
				Size = new Size(150, 50),
				Buttons = new[]
				{
					new ButtonSchema(10, 15, "Reset"),
					new ButtonSchema(58, 15, "Power")
				}
			};
		}

		private static PadSchema FdsConsoleButtons(int diskSize)
		{
			var buttons = new List<ButtonSchema>
			{
				new ButtonSchema(10, 15, "Reset"),
				new ButtonSchema(58, 15, "Power"),
				new ButtonSchema(108, 15, "FDS Eject")
				{
					DisplayName = "Eject"
				}
			};

			for (var i = 0; i < diskSize; i++)
			{
				buttons.Add(new ButtonSchema(10 + (i * 58), 50, $"FDS Insert {i}")
				{
					DisplayName = $"Insert {i}"
				});
			}

			var width = 20 + (diskSize * 58);
			if (width < 160)
			{
				width = 160;
			}

			return new ConsoleSchema
			{
				Size = new Size(width, 100),
				Buttons = buttons
			};
		}

		private static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				DisplayName = $"Player {controller}",
				Size = new Size(174, 74),
				Buttons = new[]
				{
					ButtonSchema.Up(23, 15, controller),
					ButtonSchema.Down(23, 36, controller),
					ButtonSchema.Left(2, 24, controller),
					ButtonSchema.Right(44, 24, controller),
					new ButtonSchema(124, 24, controller, "B"),
					new ButtonSchema(147, 24, controller, "A"),
					new ButtonSchema(72, 24, controller, "Select", "s"),
					new ButtonSchema(93, 24, controller, "Start", "S")
				}
			};
		}

		private static PadSchema Famicom2ndController()
		{
			var controller = 2;
			return new PadSchema
			{
				DisplayName = "Player 2",
				Size = new Size(174, 74),
				Buttons = new[]
				{
					ButtonSchema.Up(23, 15, controller),
					ButtonSchema.Down(23, 36, controller),
					ButtonSchema.Left(2, 24, controller),
					ButtonSchema.Right(44, 24, controller),
					new ButtonSchema(124, 24, controller, "B"),
					new ButtonSchema(147, 24, controller, "A"),
					new ButtonSchema(72, 24, controller, "Microphone", "Mic")
				}
			};
		}

		private static PadSchema Zapper(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Zapper",
				Size = new Size(356, 290),
				Buttons = new PadSchemaControl[]
				{
					new TargetedPairSchema(14, 17, $"P{controller} Zapper X")
					{
						TargetSize = new Size(256, 240)
					},
					new ButtonSchema(284, 17, controller, "Fire")
				}
			};
		}

		private static PadSchema ArkanoidPaddle(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Arkanoid Paddle",
				Size = new Size(380, 110),
				Buttons = new PadSchemaControl[]
				{
					new SingleAxisSchema(14, 17, controller, "Paddle")
					{
						DisplayName = "Arkanoid Paddle",
						TargetSize = new Size(380, 69),
						MaxValue = 160
					},
					new ButtonSchema(14, 85, controller, "Fire")
				}
			};
		}

		private static PadSchema PowerPad(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Power Pad",
				Size = new Size(154, 114),
				Buttons = new[]
				{
					PowerButton(14, 17, controller, 1),
					PowerButton(45, 17, controller, 2),
					PowerButton(76, 17, controller, 3),
					PowerButton(107, 17, controller, 4),
					PowerButton(14, 48, controller, 5),
					PowerButton(45, 48, controller, 6),
					PowerButton(76, 48, controller, 7),
					PowerButton(107, 48, controller, 8),
					PowerButton(14, 79, controller, 9),
					PowerButton(45, 79, controller, 10),
					PowerButton(76, 79, controller, 11),
					PowerButton(107, 79, controller, 12)
				}
			};
		}

		private static ButtonSchema PowerButton(int x, int y, int controller, int button)
		{
			return new ButtonSchema(x, y, controller, $"PP{button}")
			{
				DisplayName = button < 10 ? $"{button}  " : button.ToString()
			};
		}

		private static PadSchema OekaKidsTablet(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Tablet",
				Size = new Size(356, 290),
				Buttons = new PadSchemaControl[]
				{
					new TargetedPairSchema(14, 17, $"P{controller} Pen X")
					{
						TargetSize = new Size(256, 240)
					},
					new ButtonSchema(284, 17, controller, "Click"),
					new ButtonSchema(284, 48, controller, "Touch")
				}
			};
		}

		private static PadSchema FamicomFamilyKeyboard(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Family Basic Keyboard",
				Size = new Size(560, 180),
				Buttons = new[]
				{
					new ButtonSchema(23, 15, controller, "F1", "    F1    "),
					new ButtonSchema(76, 15, controller, "F2", "    F2    "),
					new ButtonSchema(129, 15, controller, "F3", "    F3    "),
					new ButtonSchema(182, 15, controller, "F4", "    F4    "),
					new ButtonSchema(235, 15, controller, "F5", "    F5    "),
					new ButtonSchema(288, 15, controller, "F6", "    F6    "),
					new ButtonSchema(341, 15, controller, "F7", "    F7    "),
					new ButtonSchema(394, 15, controller, "F8", "    F8    "),
					new ButtonSchema(36, 38, controller, "1"),
					new ButtonSchema(60, 38, controller, "2"),
					new ButtonSchema(84, 38, controller, "3"),
					new ButtonSchema(108, 38, controller, "4"),
					new ButtonSchema(132, 38, controller, "5"),
					new ButtonSchema(156, 38, controller, "6"),
					new ButtonSchema(180, 38, controller, "7"),
					new ButtonSchema(204, 38, controller, "8"),
					new ButtonSchema(228, 38, controller, "9"),
					new ButtonSchema(252, 38, controller, "0"),
					new ButtonSchema(276, 38, controller, "Minus", "-"),
					new ButtonSchema(296, 38, controller, "Caret", "^"),
					new ButtonSchema(320, 38, controller, "Yen", "¥"),
					new ButtonSchema(344, 38, controller, "Stop", "STOP"),
					new ButtonSchema(15, 61, controller, "Escape", "ESC"),
					new ButtonSchema(54, 61, controller, "Q"),
					new ButtonSchema(80, 61, controller, "W"),
					new ButtonSchema(108, 61, controller, "E"),
					new ButtonSchema(132, 61, controller, "R"),
					new ButtonSchema(158, 61, controller, "T"),
					new ButtonSchema(182, 61, controller, "Y"),
					new ButtonSchema(206, 61, controller, "U"),
					new ButtonSchema(232, 61, controller, "I"),
					new ButtonSchema(252, 61, controller, "O"),
					new ButtonSchema(278, 61, controller, "P"),
					new ButtonSchema(302, 61, controller, "At", "@"),
					new ButtonSchema(330, 61, controller, "Left Bracket", "["),
					new ButtonSchema(350, 61, controller, "Return", "RETURN"),
					new ButtonSchema(30, 84, controller, "Control", "CTR"),
					new ButtonSchema(70, 84, controller, "A"),
					new ButtonSchema(94, 84, controller, "S"),
					new ButtonSchema(118, 84, controller, "D"),
					new ButtonSchema(144, 84, controller, "F"),
					new ButtonSchema(168, 84, controller, "G"),
					new ButtonSchema(194, 84, controller, "H"),
					new ButtonSchema(220, 84, controller, "J"),
					new ButtonSchema(242, 84, controller, "K"),
					new ButtonSchema(266, 84, controller, "L"),
					new ButtonSchema(290, 84, controller, "Semicolon", ";"),
					new ButtonSchema(311, 84, controller, "Colon", ":"),
					new ButtonSchema(332, 84, controller, "Right Bracket", "]"),
					new ButtonSchema(352, 84, controller, "カナ"),
					new ButtonSchema(10, 107, controller, "Left Shift", "SHIFT"),
					new ButtonSchema(58, 107, controller, "Z"),
					new ButtonSchema(82, 107, controller, "X"),
					new ButtonSchema(106, 107, controller, "C"),
					new ButtonSchema(130, 107, controller, "V"),
					new ButtonSchema(154, 107, controller, "B"),
					new ButtonSchema(178, 107, controller, "N"),
					new ButtonSchema(203, 107, controller, "M"),
					new ButtonSchema(229, 107, controller, "Comma", ","),
					new ButtonSchema(249, 107, controller, "Period", "."),
					new ButtonSchema(270, 107, controller, "Slash", "/"),
					new ButtonSchema(292, 107, controller, "Underscore", "_"),
					new ButtonSchema(316, 107, controller, "Right Shift", "SHIFT"),
					new ButtonSchema(82, 130, controller, "Graph", "GRPH"),
					new ButtonSchema(130, 130, controller, "Space", "                  SPACE                  "),
					new ButtonSchema(420, 46, controller, "Clear/Home", " CLR\nHOME"),
					new ButtonSchema(470, 46, controller, "Insert", "\nINS"),
					new ButtonSchema(506, 46, controller, "Delete", "\nDEL"),
					new ButtonSchema(468, 86, controller, "Up", "  ↑  "),
					new ButtonSchema(468, 134, controller, "Down", "  ↓  "),
					new ButtonSchema(446, 110, controller, "Left", "  ←  "),
					new ButtonSchema(488, 110, controller, "Right", "  ➝  ")
				}
			};
		}
	}
}
