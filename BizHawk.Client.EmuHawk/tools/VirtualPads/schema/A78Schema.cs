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
				IsConsole = false,
				DefaultSize = new Size(174, 74),
				MaxSize = new Size(174, 74),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Up",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(23, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Down",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(23, 36),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Left",
						Icon = Properties.Resources.Back,
						Location = new Point(2, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Right",
						Icon = Properties.Resources.Forward,
						Location = new Point(44, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Trigger",
						DisplayName = "1",
						Location = new Point(120, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Trigger 2",
						DisplayName = "2",
						Location = new Point(145, 24),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}

		private static PadSchema JoystickController(int controller)
		{
			return new PadSchema
			{
				DisplayName = $"Player {controller}",
				IsConsole = false,
				DefaultSize = new Size(174, 74),
				MaxSize = new Size(174, 74),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Up",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(23, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Down",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(23, 36),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Left",
						Icon = Properties.Resources.Back,
						Location = new Point(2, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Right",
						Icon = Properties.Resources.Forward,
						Location = new Point(44, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Button",
						DisplayName = "1",
						Location = new Point(120, 24),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}

		private static PadSchema PaddleController(int controller)
		{
			return new PadSchema
			{
				DisplayName = $"Player {controller}",
				IsConsole = false,
				DefaultSize = new Size(250, 74),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Paddle",
						DisplayName = "Paddle",
						Location = new Point(23, 15),
						Type = PadSchema.PadInputType.FloatSingle
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Trigger",
						DisplayName = "1",
						Location = new Point(12, 90),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}

		private static PadSchema LightGunController(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Light Gun",
				IsConsole = false,
				DefaultSize = new Size(356, 290),
				MaxSize = new Size(356, 290),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} VPos",
						Location = new Point(14, 17),
						Type = PadSchema.PadInputType.TargetedPair,
						TargetSize = new Size(256, 240),
						SecondaryNames = new[]
						{
							$"P{controller} HPos",
						}
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Trigger",
						DisplayName = "Trigger",
						Location = new Point(284, 17),
						Type = PadSchema.PadInputType.Boolean
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
				DefaultSize = new Size(215, 50),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = "Select",
						Location = new Point(10, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Reset",
						Location = new Point(60, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Power",
						Location = new Point(108, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Pause",
						Location = new Point(158, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "BW",
						Location = new Point(158, 15),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}
	}
}
