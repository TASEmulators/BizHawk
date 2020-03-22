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
				DefaultSize = new Size(256, 240),
				MaxSize = new Size(256, 326),
				Buttons = new[]
				{
					new SingleFloatSchema(10, 15, "Tilt X")
					{
						TargetSize = new Size(226, 69),
						MinValue = short.MinValue,
						MaxValue = short.MaxValue
					},
					new SingleFloatSchema(10, 94, "Tilt Y")
					{
						TargetSize = new Size(226, 69),
						MinValue = short.MinValue,
						MaxValue = short.MaxValue
					},
					new SingleFloatSchema(10, 173, "Tilt Z")
					{
						TargetSize = new Size(226, 69),
						MinValue = short.MinValue,
						MaxValue = short.MaxValue
					},
					new SingleFloatSchema(10, 252, "Light Sensor")
					{
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
				DefaultSize = new Size(194, 90),
				Buttons = new[]
				{
					ButtonSchema.Up(29, 17, "Up"),
					ButtonSchema.Down(29, 61, "Down"),
					ButtonSchema.Left(17, 39, "Left"),
					ButtonSchema.Right(39, 39, "Right"),
					new ButtonSchema(130, 39, "B"),
					new ButtonSchema(154, 39, "A"),
					new ButtonSchema(64, 39, "Select") { DisplayName = "s" },
					new ButtonSchema(86, 39, "Start") {  DisplayName = "S" },
					new ButtonSchema(2, 12, "L"),
					new ButtonSchema(166, 12, "R")
				}
			};
		}

		private static PadSchema ConsoleButtons()
		{
			return new ConsoleSchema
			{
				DefaultSize = new Size(75, 50),
				Buttons = new[]
				{
					new ButtonSchema(10, 15, "Power")
				}
			};
		}
	}
}
