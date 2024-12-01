using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;

namespace BizHawk.Emulation.Cores
{
	[Schema(VSystemID.Raw.GEN)]
	public class GenSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
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
				Size = new Size(174, 90),
				Buttons = ThreeButtons(controller)
			};
		}

		private static PadSchema SixButtonController(int controller)
		{
			return new PadSchema
			{
				Size = new Size(174, 90),
				Buttons = ThreeButtons(controller).Concat(new[]
				{
					new ButtonSchema(98, 65, controller, "X"),
					new ButtonSchema(122, 65, controller, "Y"),
					new ButtonSchema(146, 65, controller, "Z")
				})
			};
		}

		private static IEnumerable<ButtonSchema> ThreeButtons(int controller)
		{
			return new[]
			{
				ButtonSchema.Up(14, 12, controller),
				ButtonSchema.Down(14, 56, controller),
				ButtonSchema.Left(2, 34, controller),
				ButtonSchema.Right(24, 34, controller),
				new ButtonSchema(98, 40, controller, "A"),
				new ButtonSchema(122, 40, controller, "B"),
				new ButtonSchema(146, 40, controller, "C"),
				new ButtonSchema(122, 12, controller, "Start", "S")
			};
		}

		private static PadSchema LightGun(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Light Gun",
				Size = new Size(356, 300),
				Buttons = new PadSchemaControl[]
				{
					new TargetedPairSchema(14, 17, $"P{controller} Lightgun X", 10000, 10000) //TODO (10000, 10000) matches previous behaviour - what was intended here?
					{
						TargetSize = new Size(320, 240)
					},
					new ButtonSchema(284, 17, controller, "Lightgun Trigger", "Trigger"),
					new ButtonSchema(284, 40, controller, "Lightgun Start", "Start")
				}
			};
		}

		private static PadSchema Mouse(int controller)
		{
			AxisSpec range = new((-256).RangeTo(256), 0);
			return new PadSchema
			{
				DisplayName = "Mouse",
				Size = new Size(418, 290),
				Buttons = new PadSchemaControl[]
				{
					new AnalogSchema(14, 17, $"P{controller} Mouse X") { Spec = range, SecondarySpec = range },
					new ButtonSchema(365, 17, controller, "Mouse Left", "Left"),
					new ButtonSchema(365, 40, controller, "Mouse Center", "Center"),
					new ButtonSchema(365, 63, controller, "Mouse Right", "Right"),
					new ButtonSchema(365, 86, controller, "Mouse Start", "Start")
				}
			};
		}

		private static PadSchema ConsoleButtons()
		{
			return new ConsoleSchema
			{
				Size = new Size(150, 50),
				Buttons = new[]
				{
					new ButtonSchema(10, 15, "Reset"),
					new ButtonSchema(58, 15, "Power")
				}
			};
		}

		// TODO: don't know what L/U is, couldn't find documentation, went with U
		// Also, which number corresponds to which direction?
		private static PadSchema Activator(int controller)
		{
			return new PadSchema
			{
				Size = new Size(110, 110),
				Buttons = new[]
				{
					new ButtonSchema(15, 43, controller, "1U") { Icon = VGamepadButtonImage.BlueArrW },
					new ButtonSchema(22, 20, controller, "2U") { Icon = VGamepadButtonImage.BlueArrNW },
					new ButtonSchema(47, 10, controller, "3U") { Icon = VGamepadButtonImage.BlueArrN },
					new ButtonSchema(70, 20, controller, "4U") { Icon = VGamepadButtonImage.BlueArrNE },
					new ButtonSchema(80, 43, controller, "5U") { Icon = VGamepadButtonImage.BlueArrE },
					new ButtonSchema(70, 65, controller, "6U") { Icon = VGamepadButtonImage.BlueArrSE },
					new ButtonSchema(47, 73, controller, "7U") { Icon = VGamepadButtonImage.BlueArrS },
					new ButtonSchema(22, 65, controller, "8U") { Icon = VGamepadButtonImage.BlueArrSW }
				}
			};
		}

		private static PadSchema Xe1AP(int controller)
		{
			return new PadSchema
			{
				Size = new Size(174, 90),
				Buttons = new[]
				{
					new ButtonSchema(98, 40, controller, "A"),
					new ButtonSchema(122, 40, controller, "B"),
					new ButtonSchema(146, 40, controller, "C"),
					new ButtonSchema(98, 65, controller, "D"),
					new ButtonSchema(122, 65, controller, "E1", "E¹"),
					new ButtonSchema(152, 65, controller, "E2", "E²"),
					new ButtonSchema(122, 12, controller, "Start"),
					new ButtonSchema(162, 12, controller, "Select")
				}
			};
		}
	}
}
