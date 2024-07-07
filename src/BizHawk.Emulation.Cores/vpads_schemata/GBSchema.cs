using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores
{
	[Schema(VSystemID.Raw.GB)]
	public class GbSchema : IVirtualPadSchema
	{
		public virtual IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
		{
			switch (core.ControllerDefinition.Name)
			{
				case "Gameboy Controller + Tilt":
					yield return StandardControllerH(1);
					yield return ConsoleButtonsH();
					yield return TiltControls();
					break;
				case "Gameboy Controller H":
					yield return StandardControllerH(1);
					yield return ConsoleButtonsH();
					break;
				default:
					yield return StandardController();
					yield return ConsoleButtons();
					break;
			}
		}

		// Gambatte Controller
		private static PadSchema StandardController()
		{
			return new PadSchema
			{
				Size = new Size(174, 79),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 12),
					ButtonSchema.Down(14, 56),
					ButtonSchema.Left(2, 34),
					ButtonSchema.Right(24, 34),
					new ButtonSchema(122, 34, "B"),
					new ButtonSchema(146, 34, "A"),
					new ButtonSchema(52, 34, "Select") { DisplayName = "s" },
					new ButtonSchema(74, 34, "Start") { DisplayName = "S"  }
				}
			};
		}

		private static PadSchema ConsoleButtons()
		{
			return new ConsoleSchema
			{
				Size = new Size(75, 50),
				Buttons = new[]
				{
					new ButtonSchema(10, 15, "Power")
				}
			};
		}

		// GBHawk controllers
		protected static PadSchema StandardControllerH(int controller)
		{
			return new PadSchema
			{
				Size = new Size(174, 79),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 12, controller),
					ButtonSchema.Down(14, 56, controller), 
					ButtonSchema.Left(2, 34, controller), 
					ButtonSchema.Right(24, 34, controller), 
					new ButtonSchema(122, 34, controller, "B"),
					new ButtonSchema(146, 34, controller, "A"),
					new ButtonSchema(52, 34, controller, "Select", "s"),
					new ButtonSchema(74, 34, controller, "Start", "S")
				}
			};
		}

		protected static PadSchema ConsoleButtonsH()
		{
			return new ConsoleSchema
			{
				Size = new Size(75, 50),
				Buttons = new[]
				{
					new ButtonSchema(10, 15, "P1 Power") { DisplayName = "Power" }
				}
			};
		}

		private static PadSchema TiltControls()
		{
			return new PadSchema
			{
				DisplayName = "Tilt",
				Size = new Size(356, 290),
				Buttons = new[]
				{
					new TargetedPairSchema(14, 17, "P1 Tilt X")
					{
						TargetSize = new Size(256, 240)
					}
				}
			};
		}
	}

	[Schema(VSystemID.Raw.GBL)]
	public class Gb3XSchema : GbSchema
	{
		public override IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
		{
			yield return StandardControllerH(1);
			yield return StandardControllerH(2);
			yield return StandardControllerH(3);
			yield return ConsoleButtonsH();
		}
	}

	[Schema(VSystemID.Raw.GBL)]
	public class Gb4XSchema : GbSchema
	{
		public override IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
		{
			yield return StandardControllerH(1);
			yield return StandardControllerH(2);
			yield return StandardControllerH(3);
			yield return StandardControllerH(4);
			yield return ConsoleButtonsH();
		}
	}
}
