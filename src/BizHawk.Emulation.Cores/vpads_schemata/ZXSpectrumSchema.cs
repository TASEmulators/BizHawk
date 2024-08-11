using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores
{
	[Schema(VSystemID.Raw.ZXSpectrum)]
	internal class ZxSpectrumSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
		{
			yield return Joystick(1);
			yield return Joystick(2);
			yield return Joystick(3);
			yield return Keyboard();
			//yield return TapeDevice();
		}

		private static PadSchema Joystick(int controller)
		{
			return new PadSchema
			{
				DisplayName = $"Joystick {controller}",
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

		private class ButtonLayout
		{
			public string Name { get; set; }
			public string DisName { get; set; }
			public double WidthFactor { get; set; }
			public int Row { get; set; }
			public bool IsActive { get; } = true;
		}

		private static PadSchema Keyboard()
		{
			var bls = new List<ButtonLayout>
			{
				new ButtonLayout { Name = "Key True Video", DisName = "TV", Row = 0, WidthFactor = 1 },
				new ButtonLayout { Name = "Key Inv Video", DisName = "IV", Row = 0, WidthFactor = 1 },
				new ButtonLayout { Name = "Key 1", DisName = "1", Row = 0, WidthFactor = 1 },
				new ButtonLayout { Name = "Key 2", DisName = "2", Row = 0, WidthFactor = 1 },
				new ButtonLayout { Name = "Key 3", DisName = "3", Row = 0, WidthFactor = 1 },
				new ButtonLayout { Name = "Key 4", DisName = "4", Row = 0, WidthFactor = 1 },
				new ButtonLayout { Name = "Key 5", DisName = "5", Row = 0, WidthFactor = 1 },
				new ButtonLayout { Name = "Key 6", DisName = "6", Row = 0, WidthFactor = 1 },
				new ButtonLayout { Name = "Key 7", DisName = "7", Row = 0, WidthFactor = 1 },
				new ButtonLayout { Name = "Key 8", DisName = "8", Row = 0, WidthFactor = 1 },
				new ButtonLayout { Name = "Key 9", DisName = "9", Row = 0, WidthFactor = 1 },
				new ButtonLayout { Name = "Key 0", DisName = "0", Row = 0, WidthFactor = 1 },
				new ButtonLayout { Name = "Key Break", DisName = "BREAK", Row = 0, WidthFactor = 1.5 },

				new ButtonLayout { Name = "Key Delete", DisName = "DEL", Row = 1, WidthFactor = 1.5 },
				new ButtonLayout { Name = "Key Graph", DisName = "GR", Row = 1, WidthFactor = 1 },
				new ButtonLayout { Name = "Key Q", DisName = "Q", Row = 1, WidthFactor = 1 },
				new ButtonLayout { Name = "Key W", DisName = "W", Row = 1, WidthFactor = 1 },
				new ButtonLayout { Name = "Key E", DisName = "E", Row = 1, WidthFactor = 1 },
				new ButtonLayout { Name = "Key R", DisName = "R", Row = 1, WidthFactor = 1 },
				new ButtonLayout { Name = "Key T", DisName = "T", Row = 1, WidthFactor = 1 },
				new ButtonLayout { Name = "Key Y", DisName = "Y", Row = 1, WidthFactor = 1 },
				new ButtonLayout { Name = "Key U", DisName = "U", Row = 1, WidthFactor = 1 },
				new ButtonLayout { Name = "Key I", DisName = "I", Row = 1, WidthFactor = 1 },
				new ButtonLayout { Name = "Key O", DisName = "O", Row = 1, WidthFactor = 1 },
				new ButtonLayout { Name = "Key P", DisName = "P", Row = 1, WidthFactor = 1 },

				new ButtonLayout { Name = "Key Extend Mode", DisName = "EM", Row = 2, WidthFactor = 1.5 },
				new ButtonLayout { Name = "Key Edit", DisName = "ED", Row = 2, WidthFactor = 1.25},
				new ButtonLayout { Name = "Key A", DisName = "A", Row = 2, WidthFactor = 1 },
				new ButtonLayout { Name = "Key S", DisName = "S", Row = 2, WidthFactor = 1 },
				new ButtonLayout { Name = "Key D", DisName = "D", Row = 2, WidthFactor = 1 },
				new ButtonLayout { Name = "Key F", DisName = "F", Row = 2, WidthFactor = 1 },
				new ButtonLayout { Name = "Key G", DisName = "G", Row = 2, WidthFactor = 1 },
				new ButtonLayout { Name = "Key H", DisName = "H", Row = 2, WidthFactor = 1 },
				new ButtonLayout { Name = "Key J", DisName = "J", Row = 2, WidthFactor = 1 },
				new ButtonLayout { Name = "Key K", DisName = "K", Row = 2, WidthFactor = 1 },
				new ButtonLayout { Name = "Key L", DisName = "L", Row = 2, WidthFactor = 1 },
				new ButtonLayout { Name = "Key Return", DisName = "ENTER", Row = 2, WidthFactor = 1.75 },

				new ButtonLayout { Name = "Key Caps Shift", DisName = "CAPS-S", Row = 3, WidthFactor = 2.25 },
				new ButtonLayout { Name = "Key Caps Lock", DisName = "CL", Row = 3, WidthFactor = 1},
				new ButtonLayout { Name = "Key Z", DisName = "Z", Row = 3, WidthFactor = 1 },
				new ButtonLayout { Name = "Key X", DisName = "X", Row = 3, WidthFactor = 1 },
				new ButtonLayout { Name = "Key C", DisName = "C", Row = 3, WidthFactor = 1 },
				new ButtonLayout { Name = "Key V", DisName = "V", Row = 3, WidthFactor = 1 },
				new ButtonLayout { Name = "Key B", DisName = "B", Row = 3, WidthFactor = 1 },
				new ButtonLayout { Name = "Key N", DisName = "N", Row = 3, WidthFactor = 1 },
				new ButtonLayout { Name = "Key M", DisName = "M", Row = 3, WidthFactor = 1 },
				new ButtonLayout { Name = "Key Period", DisName = ".", Row = 3, WidthFactor = 1},
				new ButtonLayout { Name = "Key Caps Shift", DisName = "CAPS-S", Row = 3, WidthFactor = 2.25 },

				new ButtonLayout { Name = "Key Symbol Shift", DisName = "SS", Row = 4, WidthFactor = 1 },
				new ButtonLayout { Name = "Key Semi-Colon", DisName = ";", Row = 4, WidthFactor = 1},
				new ButtonLayout { Name = "Key Quote", DisName = "\"", Row = 4, WidthFactor = 1 },
				new ButtonLayout { Name = "Key Left Cursor", DisName = "←", Row = 4, WidthFactor = 1 },
				new ButtonLayout { Name = "Key Right Cursor", DisName = "→", Row = 4, WidthFactor = 1 },
				new ButtonLayout { Name = "Key Space", DisName = "SPACE", Row = 4, WidthFactor = 4.5 },
				new ButtonLayout { Name = "Key Up Cursor", DisName = "↑", Row = 4, WidthFactor = 1 },
				new ButtonLayout { Name = "Key Down Cursor", DisName = "↓", Row = 4, WidthFactor = 1 },
				new ButtonLayout { Name = "Key Comma", DisName = ",", Row = 4, WidthFactor = 1 },
				new ButtonLayout { Name = "Key Symbol Shift", DisName = "SS", Row = 4, WidthFactor = 1 }
			};

			var ps = new PadSchema
			{
				DisplayName = "Keyboard",
				Size = new Size(500, 170)
			};

			var btns = new List<ButtonSchema>();

			int rowHeight = 29; //24
			int stdButtonWidth = 29; //24
			int yPos = 18;
			int xPos = 22;
			int currRow = 0;

			foreach (var b in bls)
			{
				if (b.Row > currRow)
				{
					currRow++;
					yPos += rowHeight;
					xPos = 22;
				}

				int txtLength = b.DisName.Length;
				int btnSize = System.Convert.ToInt32(stdButtonWidth * b.WidthFactor);
				

				string disp = b.DisName;
				if (txtLength == 1)
					disp = $" {disp}";

				disp = b.DisName switch
				{
					"SPACE" => $"            {disp}            ",
					"I" => $" {disp} ",
					"W" => b.DisName,
					_ => disp
				};

				if (b.IsActive)
				{
					var btn = new ButtonSchema(xPos, yPos, b.Name)
					{
						DisplayName = disp
					};
					btns.Add(btn);
				}

				xPos += btnSize;
			}

			ps.Buttons = btns.ToArray();
			return ps;
		}

		private static PadSchema TapeDevice()
		{
			return new PadSchema
			{
				DisplayName = "DATACORDER",
				Size = new Size(174, 74),
				Buttons = new[]
				{
					new ButtonSchema(23, 22, "Play Tape") { Icon = VGamepadButtonImage.Play },
					new ButtonSchema(53, 22, "Stop Tape") { Icon = VGamepadButtonImage.Stop },
					new ButtonSchema(83, 22, "RTZ Tape") { Icon = VGamepadButtonImage.SkipBack },
					new ButtonSchema(23, 52, "Insert Next Tape")
					{
						DisplayName = "NEXT TAPE"
					},
					new ButtonSchema(100, 52, "Insert Previous Tape")
					{
						DisplayName = "PREV TAPE"
					}
				}
			};
		}
	}
}
