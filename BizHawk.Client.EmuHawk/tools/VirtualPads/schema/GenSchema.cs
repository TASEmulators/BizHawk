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
				IsConsole = false,
				DefaultSize = new Size(174, 90),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 12, $"P{controller} Up"),
					ButtonSchema.Down(14, 56, $"P{controller} Down"),
					ButtonSchema.Left(2, 34, $"P{controller} Left"),
					ButtonSchema.Right(24, 34, $"P{controller} Right"),
					new ButtonSchema(98, 40)
					{
						Name = $"P{controller} A",
						DisplayName = "A"
					},
					new ButtonSchema(122, 40)
					{
						Name = $"P{controller} B",
						DisplayName = "B"
					},
					new ButtonSchema(146, 40)
					{
						Name = $"P{controller} C",
						DisplayName = "C"
					},
					new ButtonSchema(122, 12)
					{
						Name = $"P{controller} Start",
						DisplayName = "S"
					}
				}
			};
		}

		private static PadSchema SixButtonController(int controller)
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(174, 90),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 12, $"P{controller} Up"),
					ButtonSchema.Down(14, 56, $"P{controller} Down"),
					ButtonSchema.Left(2, 34, $"P{controller} Left"),
					ButtonSchema.Right(24, 34, $"P{controller} Right"),
					new ButtonSchema(98, 40)
					{
						Name = $"P{controller} A",
						DisplayName = "A"
					},
					new ButtonSchema(122, 40)
					{
						Name = $"P{controller} B",
						DisplayName = "B"
					},
					new ButtonSchema(146, 40)
					{
						Name = $"P{controller} C",
						DisplayName = "C"
					},
					new ButtonSchema(98, 65)
					{
						Name = $"P{controller} X",
						DisplayName = "X"
					},
					new ButtonSchema(122, 65)
					{
						Name = $"P{controller} Y",
						DisplayName = "Y"
					},
					new ButtonSchema(146, 65)
					{
						Name = $"P{controller} Z",
						DisplayName = "Z"
					},
					new ButtonSchema(122, 12)
					{
						Name = $"P{controller} Start",
						DisplayName = "S"
					}
				}
			};
		}

		private static PadSchema LightGun(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Light Gun",
				IsConsole = false,
				DefaultSize = new Size(356, 300),
				Buttons = new[]
				{
					new TargetedPairSchema(14, 17, $"P{controller} Lightgun X")
					{
						MaxValue = 10000,
						TargetSize = new Size(320, 240)
					},
					new ButtonSchema(284, 17)
					{
						Name = $"P{controller} Lightgun Trigger",
						DisplayName = "Trigger"
					},
					new ButtonSchema(284, 40)
					{
						Name = $"P{controller} Lightgun Start",
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
				IsConsole = false,
				DefaultSize = new Size(418, 290),
				Buttons = new[]
				{
					new AnalogSchema(14, 17, $"P{controller} Mouse X")
					{
						MaxValue = 255,
						TargetSize = new Size(520, 570)
					},
					new ButtonSchema(365, 17)
					{
						Name = $"P{controller} Mouse Left",
						DisplayName = "Left"
					},
					new ButtonSchema(365, 40)
					{
						Name = $"P{controller} Mouse Center",
						DisplayName = "Center"
					},
					new ButtonSchema(365, 63)
					{
						Name = $"P{controller} Mouse Right",
						DisplayName = "Right"
					},
					new ButtonSchema(365, 86)
					{
						Name = $"P{controller} Mouse Start",
						DisplayName = "Start"
					}
				}
			};
		}

		private static PadSchema ConsoleButtons()
		{
			return new PadSchema
			{
				DisplayName = "Console",
				IsConsole = true,
				DefaultSize = new Size(150, 50),
				Buttons = new[]
				{
					new ButtonSchema(10, 15) { Name = "Reset" },
					new ButtonSchema(58, 15) { Name = "Power" }
				}
			};
		}

		private static PadSchema Activator(int controller)
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(110, 110),
				Buttons = new[]
				{
					ButtonSchema.Up(47, 10, $"P{controller} Up"),
					ButtonSchema.Down(47, 73, $"P{controller} Down"),
					ButtonSchema.Left(15, 43, $"P{controller} Left"),
					ButtonSchema.Right(80, 43, $"P{controller} Right"),
					new ButtonSchema(70, 65)
					{
						Name = $"P{controller} A",
						DisplayName = "A"
					},
					new ButtonSchema(70, 20)
					{
						Name = $"P{controller} B",
						DisplayName = "B"
					},
					new ButtonSchema(22, 20)
					{
						Name = $"P{controller} C",
						DisplayName = "C"
					},
					new ButtonSchema(22, 65)
					{
						Name = $"P{controller} A",
						DisplayName = "A"
					},
					new ButtonSchema(47, 43)
					{
						Name = $"P{controller} Start",
						DisplayName = "S"
					}
				}
			};
		}

		private static PadSchema Xe1AP(int controller)
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(174, 90),
				Buttons = new[]
				{
					new ButtonSchema(98, 40)
					{
						Name = $"P{controller} A",
						DisplayName = "A"
					},
					new ButtonSchema(122, 40)
					{
						Name = $"P{controller} B",
						DisplayName = "B"
					},
					new ButtonSchema(146, 40)
					{
						Name = $"P{controller} C",
						DisplayName = "C"
					},
					new ButtonSchema(98, 65)
					{
						Name = $"P{controller} D",
						DisplayName = "D"
					},
					new ButtonSchema(122, 65)
					{
						Name = $"P{controller} E1",
						DisplayName = "E¹"
					},
					new ButtonSchema(152, 65)
					{
						Name = $"P{controller} E2",
						DisplayName = "E²"
					},
					new ButtonSchema(122, 12)
					{
						Name = $"P{controller} Start",
						DisplayName = "Start"
					},
					new ButtonSchema(162, 12)
					{
						Name = $"P{controller} Select",
						DisplayName = "Select"
					}
				}
			};
		}
	}
}
