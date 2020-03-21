using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Sega.MasterSystem;

namespace BizHawk.Client.EmuHawk
{
	[Schema("SMS")]
	// ReSharper disable once UnusedMember.Global
	public class SmsSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			if (((SMS)core).IsGameGear)
			{
				yield return GGController(1);
				yield return GGConsoleButtons();
			}
			else
			{
				yield return StandardController(1);
				yield return StandardController(2);
				yield return SmsConsoleButtons();
			}
		}

		private static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(174, 90),
				Buttons = new[]
				{
					ButtonSchema.Up($"P{controller} Up", 14, 12),
					ButtonSchema.Down($"P{controller} Down", 14, 56),
					ButtonSchema.Left($"P{controller} Left", 2, 34),
					ButtonSchema.Right($"P{controller} Right", 24, 34),
					new ButtonSchema
					{
						Name = $"P{controller} B1",
						DisplayName = "1",
						Location = new Point(122, 34)
					},
					new ButtonSchema
					{
						Name = $"P{controller} B2",
						DisplayName = "2",
						Location = new Point(146, 34)
					}
				}
			};
		}

		private static PadSchema GGController(int controller)
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(174, 90),
				Buttons = new[]
				{
					ButtonSchema.Up($"P{controller} Up", 14, 12),
					ButtonSchema.Down($"P{controller} Down", 14, 56),
					ButtonSchema.Left($"P{controller} Left", 2, 34),
					ButtonSchema.Right($"P{controller} Right", 24, 34),
					new ButtonSchema
					{
						Name = $"P{controller} Start",
						DisplayName = "S",
						Location = new Point(134, 12)
					},
					new ButtonSchema
					{
						Name = $"P{controller} B1",
						DisplayName = "1",
						Location = new Point(122, 34)
					},
					new ButtonSchema
					{
						Name = $"P{controller} B2",
						DisplayName = "2",
						Location = new Point(146, 34)
					}
				}
			};
		}

		private static PadSchema SmsConsoleButtons()
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
						Name = "Pause",
						Location = new Point(58, 15)
					}
				}
			};
		}

		private static PadSchema GGConsoleButtons()
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
					}
				}
			};
		}
	}
}
