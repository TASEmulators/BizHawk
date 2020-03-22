using System.Collections.Generic;
using System.Drawing;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.NEC.PCFX;

namespace BizHawk.Client.EmuHawk
{
	[Schema("PCFX")]
	// ReSharper disable once UnusedMember.Global
	public class PcfxSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			var ss = ((Tst)core).GetSyncSettings();

			var schemas = new List<PadSchema>();
			if (ss.Port1 != ControllerType.None || ss.Port2 != ControllerType.None)
			{
				switch (ss.Port1)
				{
					case ControllerType.Gamepad:
						schemas.Add(StandardController(1));
						break;
					case ControllerType.Mouse:
						schemas.Add(Mouse(1));
						break;
				}

				int controllerNum = ss.Port1 != ControllerType.None ? 2 : 1;
				switch (ss.Port2)
				{
					case ControllerType.Gamepad:
						schemas.Add(StandardController(controllerNum));
						break;
					case ControllerType.Mouse:
						schemas.Add(Mouse(controllerNum));
						break;
				}
			}

			return schemas;
		}

		private static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				Size = new Size(230, 100),
				Buttons = new[]
				{
					ButtonSchema.Up(34, 17, controller),
					ButtonSchema.Down(34, 61, controller),
					ButtonSchema.Left(22, 39, controller),
					ButtonSchema.Right(44, 39, controller),
					new ButtonSchema(74, 17, controller, "Mode 1"),
					new ButtonSchema(74, 40, controller, "Mode 2"),
					new ButtonSchema(77, 63, controller, "Select") { DisplayName = "s" },
					new ButtonSchema(101, 63, controller, "Run") { DisplayName = "R" },
					new ButtonSchema(140, 63, controller, "IV"),
					new ButtonSchema(166, 53, controller, "V"),
					new ButtonSchema(192, 43, controller, "VI"),
					new ButtonSchema(140, 40, controller, "I"),
					new ButtonSchema(166, 30, controller, "II"),
					new ButtonSchema(192, 20, controller, "III")
				}
			};
		}

		private static PadSchema Mouse(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Mouse",
				Size = new Size(375, 320),
				Buttons = new[]
				{
					new TargetedPairSchema(14, 17, $"P{controller} X")
					{
						TargetSize = new Size(256, 256)
					},
					new ButtonSchema(300, 17, controller, "Mouse Left")
					{
						DisplayName = "Left"
					},
					new ButtonSchema(300, 47, "Mouse Right")
					{
						DisplayName = "Right"
					}
				}
			};
		}
	}
}
