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
				IsConsole = false,
				DefaultSize = new Size(230, 100),
				Buttons = new[]
				{
					ButtonSchema.Up(34, 17, $"P{controller} Up"),
					ButtonSchema.Down(34, 61, $"P{controller} Down"),
					ButtonSchema.Left(22, 39, $"P{controller} Left"),
					ButtonSchema.Right(44, 39, $"P{controller} Right"),
					new ButtonSchema(74, 17)
					{
						Name = $"P{controller} Mode 1",
						DisplayName = "Mode 1"
					},
					new ButtonSchema(74, 40)
					{
						Name = $"P{controller} Mode 2",
						DisplayName = "Mode 2"
					},
					new ButtonSchema(77, 63)
					{
						Name = $"P{controller} Select",
						DisplayName = "s"
					},
					new ButtonSchema(101, 63)
					{
						Name = $"P{controller} Run",
						DisplayName = "R"
					},
					new ButtonSchema(140, 63)
					{
						Name = $"P{controller} IV",
						DisplayName = "IV"
					},
					new ButtonSchema(166, 53)
					{
						Name = $"P{controller} V",
						DisplayName = "V"
					},
					new ButtonSchema(192, 43)
					{
						Name = $"P{controller} VI",
						DisplayName = "VI"
					},
					new ButtonSchema(140, 40)
					{
						Name = $"P{controller} I",
						DisplayName = "I"
					},
					new ButtonSchema(166, 30)
					{
						Name = $"P{controller} II",
						DisplayName = "II"
					},
					new ButtonSchema(192, 20)
					{
						Name = $"P{controller} III",
						DisplayName = "III"
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
				DefaultSize = new Size(375, 320),
				Buttons = new[]
				{
					new TargetedPairSchema(14, 17, $"P{controller} X")
					{
						TargetSize = new Size(256, 256)
					},
					new ButtonSchema(300, 17)
					{
						Name = $"P{controller} Mouse Left",
						DisplayName = "Left"
					},
					new ButtonSchema(300, 47)
					{
						Name = $"P{controller} Mouse Right",
						DisplayName = "Right"
					}
				}
			};
		}
	}
}
