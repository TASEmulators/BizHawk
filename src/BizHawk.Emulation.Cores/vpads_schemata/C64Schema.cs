using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores
{
	[Schema(VSystemID.Raw.C64)]
	public class C64Schema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
		{
			yield return StandardController(1);
			yield return StandardController(2);
			yield return Keyboard();
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
					new ButtonSchema(124, 24, controller, "Button", "B")
				}
			};
		}

		private static PadSchema Keyboard()
		{
			return new PadSchema
			{
				DisplayName = "Keyboard",
				Size = new Size(500, 150),
				Buttons = new[]
				{
					Key(16, 18, "Left Arrow", "←"),
					Key(46, 18, "1"),
					Key(70, 18, "2"),
					Key(94, 18, "3"),
					Key(118, 18, "4"),
					Key(142, 18, "5"),
					Key(166, 18, "6"),
					Key(190, 18, "7"),
					Key(214, 18, "8"),
					Key(238, 18, "9"),
					Key(262, 18, "0"),
					Key(286, 18, "Plus", "+"),
					Key(310, 18, "Minus", "-"),
					Key(330, 18, "Pound", "£"),
					Key(354, 18, "Clear/Home", "C/H"),
					Key(392, 18, "Insert/Delete", "I/D"),
					Key(450, 18, "F1", "F 1"),
					Key(450, 42, "F3", "F 3"),
					Key(450, 66, "F5", "F 5"),
					Key(450, 90, "F7", "F 7"),
					Key(16, 42, "Control", "CTRL"),
					Key(62, 42, "Q"),
					Key(88, 42, "W"),
					Key(116, 42, "E"),
					Key(140, 42, "R"),
					Key(166, 42, "T"),
					Key(190, 42, "Y"),
					Key(214, 42, "U"),
					Key(240, 42, "I"),
					Key(260, 42, "O"),
					Key(286, 42, "P"),
					Key(310, 42, "At", "@"),
					Key(338, 42, "Asterisk", "*"),
					Key(360, 42, "Up Arrow", "↑"),
					Key(390, 42, "Restore", "RST"),
					Key(12, 66, "Run/Stop", "R/S"),
					Key(50, 66, "Lck"),
					Key(86, 66, "A"),
					Key(110, 66, "S"),
					Key(134, 66, "D"),
					Key(160, 66, "F"),
					Key(184, 66, "G"),
					Key(210, 66, "H"),
					Key(236, 66, "J"),
					Key(258, 66, "K"),
					Key(282, 66, "L"),
					Key(306, 66, "Colon", ":"),
					Key(326, 66, "Semicolon", ";"),
					Key(346, 66, "Equal", "="),
					Key(370, 66, "Return"),
					new ButtonSchema(8, 90, "Key Commodore") { Icon = VGamepadButtonImage.C64Symbol },
					Key(44, 90, "Left Shift", "Shift"),
					Key(82, 90, "Z"),
					Key(106, 90, "X"),
					Key(130, 90, "C"),
					Key(154, 90, "V"),
					Key(178, 90, "B"),
					Key(202, 90, "N"),
					Key(226, 90, "M"),
					Key(252, 90, "Comma", ","),
					Key(272, 90, "Period", "."),
					Key(292, 90, "Slash", "/"),
					Key(314, 90, "Right Shift", "Shift"),
					Key(352, 90, "Cursor Up/Down", "Csr U"),
					Key(396, 90, "Cursor Left/Right", "Csr L"),
					Key(120, 114, "Space", "                          Space                          ")
				}
			};
		}

		private static ButtonSchema Key(int x, int y, string name, string displayName = null)
			=> new ButtonSchema(x, y, "Key " + name)
			{
				DisplayName = displayName ?? name
			};
	}
}
