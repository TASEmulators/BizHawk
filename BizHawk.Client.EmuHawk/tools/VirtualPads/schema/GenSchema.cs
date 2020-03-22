using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;

namespace BizHawk.Client.EmuHawk
{
	[Schema("GEN")]
	// ReSharper disable once UnusedMember.Global
	public class GenSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			if (core is GPGX gpgx)
			{
				return GpgxPadSchemas(gpgx);
			}

			return PicoPadSchemas();
		}

		private IEnumerable<PadSchema> PicoPadSchemas()
		{
			yield return SixButtonController(1);
			yield return SixButtonController(2);
			yield return ConsoleButtons();
		}

		private IEnumerable<PadSchema> GpgxPadSchemas(GPGX core)
		{
			var devices = core.GetDevices();
			int player = 1;
			foreach (var dev in devices)
			{
				switch (dev)
				{
					case LibGPGX.INPUT_DEVICE.DEVICE_NONE:
						continue; // do not increment player number because no device was attached
					case LibGPGX.INPUT_DEVICE.DEVICE_PAD3B:
						yield return ThreeButtonController(player);
						break;
					case LibGPGX.INPUT_DEVICE.DEVICE_PAD6B:
						yield return SixButtonController(player);
						break;
					case LibGPGX.INPUT_DEVICE.DEVICE_LIGHTGUN:
						yield return LightGun(player);
						break;
					case LibGPGX.INPUT_DEVICE.DEVICE_MOUSE:
						yield return Mouse(player);
						break;
					case LibGPGX.INPUT_DEVICE.DEVICE_ACTIVATOR:
						yield return Activator(player);
						break;
					case LibGPGX.INPUT_DEVICE.DEVICE_XE_A1P:
						yield return Xe1AP(player);
						break;
					default:
						// TO DO
						break;
				}

				player++;
			}

			yield return ConsoleButtons();
		}

		private static PadSchema ThreeButtonController(int controller)
		{
			return new PadSchema
			{
				DefaultSize = new Size(174, 90),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 12, controller),
					ButtonSchema.Down(14, 56, controller),
					ButtonSchema.Left(2, 34, controller),
					ButtonSchema.Right(24, 34, controller),
					new ButtonSchema(98, 40, controller, "A"),
					new ButtonSchema(122, 40, controller, "B"),
					new ButtonSchema(146, 40, controller, "C"),
					new ButtonSchema(122, 12, controller, "Start") { DisplayName = "S" }
				}
			};
		}

		private static PadSchema SixButtonController(int controller)
		{
			return new PadSchema
			{
				DefaultSize = new Size(174, 90),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 12, controller),
					ButtonSchema.Down(14, 56, controller),
					ButtonSchema.Left(2, 34, controller),
					ButtonSchema.Right(24, 34, controller),
					new ButtonSchema(98, 40, controller, "A"),
					new ButtonSchema(122, 40, controller, "B"),
					new ButtonSchema(146, 40, controller, "C"),
					new ButtonSchema(98, 65, controller, "X"),
					new ButtonSchema(122, 65, controller, "Y"),
					new ButtonSchema(146, 65, controller, "Z"),
					new ButtonSchema(122, 12, controller, "Start") { DisplayName = "S" }
				}
			};
		}

		private static PadSchema LightGun(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Light Gun",
				DefaultSize = new Size(356, 300),
				Buttons = new[]
				{
					new TargetedPairSchema(14, 17, $"P{controller} Lightgun X")
					{
						MaxValue = 10000,
						TargetSize = new Size(320, 240)
					},
					new ButtonSchema(284, 17, controller, "Lightgun Trigger")
					{
						DisplayName = "Trigger"
					},
					new ButtonSchema(284, 40, controller, "Lightgun Start")
					{
						DisplayName = "Start"
					}
				}
			};
		}

		private static PadSchema Mouse(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Mouse",
				DefaultSize = new Size(418, 290),
				Buttons = new[]
				{
					new AnalogSchema(14, 17, $"P{controller} Mouse X")
					{
						MaxValue = 255,
						TargetSize = new Size(520, 570)
					},
					new ButtonSchema(365, 17, controller, "Mouse Left")
					{
						DisplayName = "Left"
					},
					new ButtonSchema(365, 40, controller, "Mouse Center")
					{
						DisplayName = "Center"
					},
					new ButtonSchema(365, 63, "Mouse Right")
					{
						DisplayName = "Right"
					},
					new ButtonSchema(365, 86, "Mouse Start")
					{
						DisplayName = "Start"
					}
				}
			};
		}

		private static PadSchema ConsoleButtons()
		{
			return new ConsoleSchema
			{
				DefaultSize = new Size(150, 50),
				Buttons = new[]
				{
					new ButtonSchema(10, 15, "Reset"),
					new ButtonSchema(58, 15, "Power")
				}
			};
		}

		private static PadSchema Activator(int controller)
		{
			return new PadSchema
			{
				DefaultSize = new Size(110, 110),
				Buttons = new[]
				{
					ButtonSchema.Up(47, 10, controller),
					ButtonSchema.Down(47, 73, controller),
					ButtonSchema.Left(15, 43, controller),
					ButtonSchema.Right(80, 43, controller),
					new ButtonSchema(70, 65, controller, "A"),
					new ButtonSchema(70, 20, controller, "B"),
					new ButtonSchema(22, 20, controller, "C"),
					new ButtonSchema(22, 65, controller, "A"),
					new ButtonSchema(47, 43, controller, "Start") { DisplayName = "S" }
				}
			};
		}

		private static PadSchema Xe1AP(int controller)
		{
			return new PadSchema
			{
				DefaultSize = new Size(174, 90),
				Buttons = new[]
				{
					new ButtonSchema(98, 40, controller, "A"),
					new ButtonSchema(122, 40, controller, "B"),
					new ButtonSchema(146, 40, controller, "C"),
					new ButtonSchema(98, 65, controller, "D"),
					new ButtonSchema(122, 65, controller, "E1") { DisplayName = "E¹" },
					new ButtonSchema(152, 65, controller, "E2") { DisplayName = "E²" },
					new ButtonSchema(122, 12, controller, "Start"),
					new ButtonSchema(162, 12, controller, "Select")
				}
			};
		}
	}
}
