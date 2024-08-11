using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.GBA;

namespace BizHawk.Emulation.Cores
{
	[Schema(VSystemID.Raw.GBA)]
	public class GbaSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
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
				Size = new Size(256, 340),
				Buttons = new[]
				{
					Tilt(10, 15, "X"),
					Tilt(10, 94, "Y"),
					Tilt(10, 173, "Z"),
					new SingleAxisSchema(10, 252, "Light Sensor")
					{
						TargetSize = new Size(226, 69),
						MaxValue = byte.MaxValue
					}
				}
			};
		}

		private static SingleAxisSchema Tilt(int x, int y, string direction)
			=> new SingleAxisSchema(x, y, "Tilt " + direction)
			{
				TargetSize = new Size(226, 69),
				MinValue = short.MinValue,
				MaxValue = short.MaxValue
			};

		private static PadSchema StandardController()
		{
			return new PadSchema
			{
				Size = new Size(194, 90),
				Buttons = new[]
				{
					ButtonSchema.Up(29, 17),
					ButtonSchema.Down(29, 61),
					ButtonSchema.Left(17, 39),
					ButtonSchema.Right(39, 39),
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
				Size = new Size(75, 50),
				Buttons = new[]
				{
					new ButtonSchema(10, 15, "Power")
				}
			};
		}
	}
}
