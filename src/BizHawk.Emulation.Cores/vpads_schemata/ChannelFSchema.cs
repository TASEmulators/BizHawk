
using BizHawk.Emulation.Common;
using System.Collections.Generic;
using System.Drawing;

namespace BizHawk.Emulation.Cores
{
	[Schema(VSystemID.Raw.ChannelF)]
	internal class ChannelFSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
		{
			yield return Joystick(1);
			yield return Joystick(2);
			yield return Console();
		}

		private static PadSchema Joystick(int controller)
		{
			string post = controller == 2 ? " (LEFT)" : " (RIGHT)";

			return new PadSchema
			{
				DisplayName = $"Controller {controller} {post}",
				Size = new Size(150, 160),
				Buttons = new[]
				{
					new ButtonSchema(10, 25, controller, "CCW", "CCW"),
					new ButtonSchema(92, 25, controller, "CW", " CW "),

					new ButtonSchema(60, 35, controller, "Forward", "U"),
					new ButtonSchema(60, 57, controller, "Back", "D"),
					new ButtonSchema(38, 57, controller, "Left", "L"),
					new ButtonSchema(85, 57, controller, "Right", "R"),			
					

					new ButtonSchema(10, 80, controller, "Pull", "PULL"),
					new ButtonSchema(92, 80, controller, "Push", "PUSH")
				}
			};
		}

		private static PadSchema Console()
		{
			return new PadSchema
			{
				DisplayName = "Console",
				Size = new Size(70, 160),
				Buttons = new[]
				{
					new ButtonSchema(10, 25, "TIME"),
					new ButtonSchema(10, 50, "MODE"),
					new ButtonSchema(10, 75, "HOLD"),
					new ButtonSchema(10, 100, "START"),
					new ButtonSchema(10, 125, "RESET")
				}
			};
		}
	}
}
