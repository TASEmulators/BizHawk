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
					new ButtonSchema
					{
						Name = $"P{controller} Up",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(14, 12)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Down",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(14, 56)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Left",
						Icon = Properties.Resources.Back,
						Location = new Point(2, 34)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Right",
						Icon = Properties.Resources.Forward,
						Location = new Point(24, 34)
					},
					new ButtonSchema
					{
						Name = $"P{controller} A",
						DisplayName = "A",
						Location = new Point(98, 40)
					},
					new ButtonSchema
					{
						Name = $"P{controller} B",
						DisplayName = "B",
						Location = new Point(122, 40)
					},
					new ButtonSchema
					{
						Name = $"P{controller} C",
						DisplayName = "C",
						Location = new Point(146, 40)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Start",
						DisplayName = "S",
						Location = new Point(122, 12)
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
					new ButtonSchema
					{
						Name = $"P{controller} Up",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(14, 12)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Down",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(14, 56)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Left",
						Icon = Properties.Resources.Back,
						Location = new Point(2, 34)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Right",
						Icon = Properties.Resources.Forward,
						Location = new Point(24, 34)
					},
					new ButtonSchema
					{
						Name = $"P{controller} A",
						DisplayName = "A",
						Location = new Point(98, 40)
					},
					new ButtonSchema
					{
						Name = $"P{controller} B",
						DisplayName = "B",
						Location = new Point(122, 40)
					},
					new ButtonSchema
					{
						Name = $"P{controller} C",
						DisplayName = "C",
						Location = new Point(146, 40)
					},
					new ButtonSchema
					{
						Name = $"P{controller} X",
						DisplayName = "X",
						Location = new Point(98, 65)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Y",
						DisplayName = "Y",
						Location = new Point(122, 65)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Z",
						DisplayName = "Z",
						Location = new Point(146, 65)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Start",
						DisplayName = "S",
						Location = new Point(122, 12)
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
					new ButtonSchema
					{
						Name = $"P{controller} Lightgun X",
						Location = new Point(14, 17),
						Type = PadInputType.TargetedPair,
						MaxValue = 10000,
						TargetSize = new Size(320, 240),
						SecondaryNames = new[]
						{
							$"P{controller} Lightgun Y"
						}
					},
					new ButtonSchema
					{
						Name = $"P{controller} Lightgun Trigger",
						DisplayName = "Trigger",
						Location = new Point(284, 17)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Lightgun Start",
						DisplayName = "Start",
						Location = new Point(284, 40)
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
					new ButtonSchema
					{
						Name = $"P{controller} Mouse X",
						Location = new Point(14, 17),
						Type = PadInputType.AnalogStick,
						MaxValue = 255,
						TargetSize = new Size(520, 570),
						SecondaryNames = new[]
						{
							$"P{controller} Mouse Y"
						}
					},
					new ButtonSchema
					{
						Name = $"P{controller} Mouse Left",
						DisplayName = "Left",
						Location = new Point(365, 17)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Mouse Center",
						DisplayName = "Center",
						Location = new Point(365, 40)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Mouse Right",
						DisplayName = "Right",
						Location = new Point(365, 63)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Mouse Start",
						DisplayName = "Start",
						Location = new Point(365, 86)
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
					new ButtonSchema
					{
						Name = "Reset",
						Location = new Point(10, 15)
					},
					new ButtonSchema
					{
						Name = "Power",
						Location = new Point(58, 15)
					}
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
					new ButtonSchema
					{
						Name = $"P{controller} Up",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(47, 10)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Down",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(47, 73)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Left",
						Icon = Properties.Resources.Back,
						Location = new Point(15, 43)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Right",
						Icon = Properties.Resources.Forward,
						Location = new Point(80, 43)
					},
					new ButtonSchema
					{
						Name = $"P{controller} A",
						DisplayName = "A",
						Location = new Point(70, 65)
					},
					new ButtonSchema
					{
						Name = $"P{controller} B",
						DisplayName = "B",
						Location = new Point(70, 20)
					},
					new ButtonSchema
					{
						Name = $"P{controller} C",
						DisplayName = "C",
						Location = new Point(22, 20)
					},
					new ButtonSchema
					{
						Name = $"P{controller} A",
						DisplayName = "A",
						Location = new Point(22, 65)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Start",
						DisplayName = "S",
						Location = new Point(47, 43)
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
					new ButtonSchema
					{
						Name = $"P{controller} A",
						DisplayName = "A",
						Location = new Point(98, 40)
					},
					new ButtonSchema
					{
						Name = $"P{controller} B",
						DisplayName = "B",
						Location = new Point(122, 40)
					},
					new ButtonSchema
					{
						Name = $"P{controller} C",
						DisplayName = "C",
						Location = new Point(146, 40)
					},
					new ButtonSchema
					{
						Name = $"P{controller} D",
						DisplayName = "D",
						Location = new Point(98, 65)
					},
					new ButtonSchema
					{
						Name = $"P{controller} E1",
						DisplayName = "E¹",
						Location = new Point(122, 65)
					},
					new ButtonSchema
					{
						Name = $"P{controller} E2",
						DisplayName = "E²",
						Location = new Point(152, 65)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Start",
						DisplayName = "Start",
						Location = new Point(122, 12)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Select",
						DisplayName = "Select",
						Location = new Point(162, 12)
					}
				}
			};
		}
	}
}
