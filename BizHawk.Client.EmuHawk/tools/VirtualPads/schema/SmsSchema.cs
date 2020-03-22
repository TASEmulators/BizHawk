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
				DefaultSize = new Size(174, 90),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 12, $"P{controller} Up"),
					ButtonSchema.Down(14, 56, $"P{controller} Down"),
					ButtonSchema.Left(2, 34, $"P{controller} Left"),
					ButtonSchema.Right(24, 34, $"P{controller} Right"),
					new ButtonSchema(122, 34, controller, "B1")
					{
						DisplayName = "1"
					},
					new ButtonSchema(146, 34, controller, "B2")
					{
						DisplayName = "2"
					}
				}
			};
		}

		private static PadSchema GGController(int controller)
		{
			return new PadSchema
			{
				DefaultSize = new Size(174, 90),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 12, $"P{controller} Up"),
					ButtonSchema.Down(14, 56, $"P{controller} Down"),
					ButtonSchema.Left(2, 34, $"P{controller} Left"),
					ButtonSchema.Right(24, 34, $"P{controller} Right"),
					new ButtonSchema(134, 12, controller, "Start")
					{
						DisplayName = "S"
					},
					new ButtonSchema(122, 34, controller, "B1")
					{
						DisplayName = "1"
					},
					new ButtonSchema(146, 34, controller, "B2")
					{
						DisplayName = "2"
					}
				}
			};
		}

		private static PadSchema SmsConsoleButtons()
		{
			return new ConsoleSchema
			{
				DefaultSize = new Size(150, 50),
				Buttons = new[]
				{
					new ButtonSchema(10, 15, "Reset"),
					new ButtonSchema(58, 15, "Pause")
				}
			};
		}

		private static PadSchema GGConsoleButtons()
		{
			return new ConsoleSchema
			{
				DefaultSize = new Size(150, 50),
				Buttons = new[]
				{
					new ButtonSchema(10, 15, "Reset")
				}
			};
		}
	}
}
