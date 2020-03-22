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
					new SingleFloatSchema(10, 15)
					{
						Name = "Tilt X",
						TargetSize = new Size(226, 69),
						MinValue = short.MinValue,
						MaxValue = short.MaxValue
					},
					new SingleFloatSchema(10, 94)
					{
						Name = "Tilt Y",
						TargetSize = new Size(226, 69),
						MinValue = short.MinValue,
						MaxValue = short.MaxValue
					},
					new SingleFloatSchema(10, 173)
					{
						Name = "Tilt Z",
						TargetSize = new Size(226, 69),
						MinValue = short.MinValue,
						MaxValue = short.MaxValue
					},
					new SingleFloatSchema(10, 252)
					{
						Name = "Light Sensor",
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
					ButtonSchema.Up(29, 17, "Up"),
					ButtonSchema.Down(29, 61, "Down"),
					ButtonSchema.Left(17, 39, "Left"),
					ButtonSchema.Right(39, 39, "Right"),
					new ButtonSchema(130, 39) { Name = "B" },
					new ButtonSchema(154, 39) { Name = "A" },
					new ButtonSchema(64, 39) { Name = "Select", DisplayName = "s" },
					new ButtonSchema(86, 39) { Name = "Start",  DisplayName = "S" },
					new ButtonSchema(2, 12) { Name = "L" },
					new ButtonSchema(166, 12) { Name = "R" }
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
					new ButtonSchema(10, 15) { Name = "Power" }
				}
			};
		}
	}
}
