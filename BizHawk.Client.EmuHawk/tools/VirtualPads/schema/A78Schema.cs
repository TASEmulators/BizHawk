using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Cores.Atari.A7800Hawk;

namespace BizHawk.Client.EmuHawk
{
	[Schema("A78")]
	// ReSharper disable once UnusedMember.Global
	public class A78Schema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			return Atari7800HawkSchema.GetPadSchemas((A7800Hawk)core);
		}
	}

	internal static class Atari7800HawkSchema
	{
		private static string StandardControllerName => typeof(StandardController).DisplayName();
		private static string ProLineControllerName => typeof(ProLineController).DisplayName();

		public static IEnumerable<PadSchema> GetPadSchemas(A7800Hawk core)
		{
			var a78SyncSettings = core.GetSyncSettings().Clone();
			var port1 = a78SyncSettings.Port1;
			var port2 = a78SyncSettings.Port2;

			if (port1 == StandardControllerName)
			{
				yield return JoystickController(1);
			}

			if (port2 == StandardControllerName)
			{
				yield return JoystickController(2);
			}

			if (port1 == ProLineControllerName)
			{
				yield return ProLineController(1);
			}

			if (port2 == ProLineControllerName)
			{
				yield return ProLineController(2);
			}

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
					new ButtonSchema(120, 24, controller, "Trigger") { DisplayName = "1" },
					new ButtonSchema(145, 24, controller, "Trigger 2") { DisplayName = "2" }
				}
			};
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
					ButtonSchema.Right(54, 24, controller),
					new ButtonSchema(120, 24, controller, "Button")
					{
						DisplayName = "1"
					}
				}
			};
		}

		private static PadSchema PaddleController(int controller)
		{
			return new PadSchema
			{
				DisplayName = $"Player {controller}",
				Size = new Size(250, 74),
				Buttons = new[]
				{
					new SingleFloatSchema(23, 15, controller, "Paddle"),
					new ButtonSchema(12, 90, controller, "Trigger") { DisplayName = "1" }
				}
			};
		}

		private static PadSchema LightGunController(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Light Gun",
				Size = new Size(356, 290),
				Buttons = new[]
				{
					new TargetedPairSchema(14, 17, $"P{controller} VPos")
					{
						TargetSize = new Size(256, 240),
						SecondaryNames = new[]
						{
							$"P{controller} HPos"
						}
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
