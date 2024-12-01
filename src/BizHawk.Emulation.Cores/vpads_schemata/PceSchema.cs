using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores
{
	[Schema(VSystemID.Raw.SGXCD)]
	public class SgxCdSchema : PceSchema { }

	[Schema(VSystemID.Raw.PCECD)]
	public class PceCdSchema : PceSchema { }

	[Schema(VSystemID.Raw.SGX)]
	public class SgxSchema : PceSchema { }

	[Schema(VSystemID.Raw.PCE)]
	public class PceSchema : IVirtualPadSchema
	{
		public virtual IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
		{
			return core switch
			{
				PCEngine.PCEngine pce => PceHawkSchemas(pce, showMessageBox),
				NymaCore nyma => NymaSchemas(nyma, showMessageBox),
				_ => Enumerable.Empty<PadSchema>()
			};
		}

		private static IEnumerable<PadSchema> PceHawkSchemas(PCEngine.PCEngine core, Action<string> showMessageBox)
		{
			var ss = core.GetSyncSettings();

			var padSchemas = new[]
				{
					ss.Port1,
					ss.Port2,
					ss.Port3,
					ss.Port4,
					ss.Port5
				}
				.Where(p => p != PceControllerType.Unplugged)
				.Select((p, i) => PceHawkGenerateSchemaForPort(p, i + 1, showMessageBox))
				.Where(s => s != null);

			return padSchemas;
		}

		private static PadSchema PceHawkGenerateSchemaForPort(PceControllerType type, int controller, Action<string> showMessageBox)
		{
			switch (type)
			{
				default:
					showMessageBox($"{type} is not supported yet");
					return null;
				case PceControllerType.Unplugged:
					return null;
				case PceControllerType.GamePad:
					return StandardHawkController(controller);
			}
		}

		private static PadSchema StandardHawkController(int controller)
		{
			return new PadSchema
			{
				Size = new Size(174, 90),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 12, controller),
					ButtonSchema.Down(14, 56, controller),
					ButtonSchema.Left(2, 34, controller),
					ButtonSchema.Right(24, 34, controller),
					new ButtonSchema(122, 34, controller, "B2", "II"),
					new ButtonSchema(146, 34, controller, "B1", "I"),
					new ButtonSchema(52, 34, controller, "Select", "s"),
					new ButtonSchema(74, 34, controller, "Run", "R")
				}
			};
		}

		private static IEnumerable<PadSchema> NymaSchemas(NymaCore nyma, Action<string> showMessageBox)
		{
			foreach (NymaCore.PortResult result in nyma.ActualPortData)
			{
				var num = int.Parse(result.Port.ShortName.Last().ToString());
				var device = result.Device.ShortName;
				if (device == "gamepad")
				{
					yield return StandardController(num);
				}
				else if (device == "mouse")
				{
					yield return Mouse(num);
				}
				else if (device != "none")
				{
					showMessageBox($"Controller type {device} not supported yet.");
				}
			}

			yield return ConsoleButtons();
		}

		private static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				Size = new Size(230, 100),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 12, controller),
					ButtonSchema.Down(14, 56, controller),
					ButtonSchema.Left(2, 34, controller),
					ButtonSchema.Right(24, 34, controller),
					new ButtonSchema(77, 17, controller, "Mode: Set 2-button", "2 Btn"),
					new ButtonSchema(77, 40, controller, "Mode: Set 6-button", "6 Btn"),
					new ButtonSchema(140, 63, controller, "IV"),
					new ButtonSchema(166, 53, controller, "V"),
					new ButtonSchema(192, 43, controller, "VI"),
					new ButtonSchema(140, 40, controller, "I"),
					new ButtonSchema(166, 30, controller, "II"),
					new ButtonSchema(192, 20, controller, "III"),
					new ButtonSchema(77, 63, controller, "Select", "s"),
					new ButtonSchema(101, 63, controller, "Run", "R")
				}
			};
		}

		private static PadSchema Mouse(int controller)
		{
			var range = new AxisSpec((-127).RangeTo(127), 0);
			return new PadSchema
			{
				Size = new Size(345, 225),
				Buttons = new PadSchemaControl[]
				{
					new AnalogSchema(6, 14, $"P{controller} Motion Left / Right")
					{
						SecondaryName = $"P{controller} Motion Up / Down",
						Spec = range,
						SecondarySpec = range
					},
					new ButtonSchema(275, 15, controller, "Left Button")
					{
						DisplayName = "Left"
					},
					new ButtonSchema(275, 45, controller, "Right Button")
					{
						DisplayName = "Right"
					},
					new ButtonSchema(275, 75, controller, "Select"),
					new ButtonSchema(275, 105, controller, "Run")
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
	}
}
