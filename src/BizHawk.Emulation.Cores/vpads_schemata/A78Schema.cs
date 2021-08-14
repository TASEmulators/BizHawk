using System;
using System.Collections.Generic;
using System.Drawing;
using BizHawk.Emulation.Common;

using BizHawk.Emulation.Cores.Atari.A7800Hawk;

namespace BizHawk.Emulation.Cores
{
	[Schema(VSystemID.Raw.A78)]
	// ReSharper disable once UnusedMember.Global
	public class A78Schema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
		{
			static Func<int, PadSchema> SchemaFor(PeripheralOption option) => option switch
			{
				PeripheralOption.Standard => JoystickController,
				PeripheralOption.ProLine => ProLineController,
				PeripheralOption.LightGun => LightGunController,
				_ => null
			};

			var ss = ((A7800Hawk) core).GetSyncSettings().Clone();

			var port1 = SchemaFor(ss.Port1);
			if (port1 is not null) yield return port1(1);

			var port2 = SchemaFor(ss.Port2);
			if (port2 is not null) yield return port2(2);

			yield return ConsoleButtons();
		}

		private static PadSchema JoystickController(int controller)
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
					new ButtonSchema(120, 24, controller, "Button", "1")
				}
			};
		}

		private static PadSchema ProLineController(int controller)
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
					new ButtonSchema(120, 24, controller, "Trigger", "1"),
					new ButtonSchema(145, 24, controller, "Trigger 2", "2")
				}
			};
		}

		private static PadSchema LightGunController(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Light Gun",
				Size = new Size(356, 290),
				Buttons = new PadSchemaControl[]
				{
					new TargetedPairSchema(14, 17, $"P{controller} X")
					{
						TargetSize = new Size(256, 240)
					},
					new ButtonSchema(284, 17, controller, "Trigger")
				}
			};
		}

		private static PadSchema ConsoleButtons()
		{
			return new ConsoleSchema
			{
				Size = new Size(215, 50),
				Buttons = new[]
				{
					new ButtonSchema(10, 15, "Select"),
					new ButtonSchema(60, 15, "Reset"),
					new ButtonSchema(108, 15, "Power"),
					new ButtonSchema(158, 15, "Pause"),
					new ButtonSchema(158, 15, "BW")
				}
			};
		}
	}
}
