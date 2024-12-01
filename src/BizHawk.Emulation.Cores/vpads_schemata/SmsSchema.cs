using System.Collections.Generic;
using System.Drawing;
using BizHawk.Emulation.Common;

using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Cores.Sega.MasterSystem;

namespace BizHawk.Emulation.Cores
{

	[Schema(VSystemID.Raw.GG)]
	public class GameGearSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
		{
			yield return GGSchemaControls.StandardController(1);
			yield return GGSchemaControls.Console();
		}
	}

	internal abstract class GGSchemaControls
	{
		public static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				Size = new Size(174, 90),
				Buttons = StandardButtons(controller)
			};
		}

		public static IEnumerable<ButtonSchema> StandardButtons(int controller)
		{

			yield return ButtonSchema.Up(14, 12, controller);
			yield return ButtonSchema.Down(14, 56, controller);
			yield return ButtonSchema.Left(2, 34, controller);
			yield return ButtonSchema.Right(24, 34, controller);
			yield return new ButtonSchema(122, 34, controller, "B1", "1");
			yield return new ButtonSchema(146, 34, controller, "B2", "2");
			yield return new ButtonSchema(134, 12, controller, "Start", "S");
		}

		public static PadSchema Console()
		{
			return new ConsoleSchema
			{
				Size = new Size(150, 50),
				Buttons = ConsoleButtons()
			};
		}

		public static IEnumerable<ButtonSchema> ConsoleButtons()
		{
			yield return new ButtonSchema(10, 15, "Reset");
		}
	}

	[Schema(VSystemID.Raw.SG)]
	public class SG1000Schema : SMSSchema { } // are these really the same controller layouts? --yoshi

	[Schema(VSystemID.Raw.SMS)]
	public class SMSSchema : IVirtualPadSchema
	{
		private static string StandardControllerName => typeof(SmsController).DisplayName();
		private static string PaddleControllerName => typeof(SMSPaddleController).DisplayName();
		private static string SportControllerName => typeof(SMSSportsPadController).DisplayName();
		private static string LightGunControllerName => typeof(SMSLightPhaserController).DisplayName();

		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
		{
			var ss = ((SMS)core).GetSyncSettings().Clone();

			var port1 = SchemaFor(ss.Port1, 1);
			if (port1 != null)
			{
				yield return port1;
			}

			var port2 = SchemaFor(ss.Port2, 2);
			if (port2 != null)
			{
				yield return port2;
			}

			yield return ConsoleButtons();
		}

		private static PadSchema SchemaFor(SMSControllerTypes controllerName, int portNum)
		{
			if (controllerName == SMSControllerTypes.Standard)
			{
				return JoystickController(portNum);
			}

			if (controllerName == SMSControllerTypes.Paddle)
			{
				return PaddleController(portNum);
			}

			if (controllerName == SMSControllerTypes.SportsPad)
			{
				return SportController(portNum);
			}

			if (controllerName == SMSControllerTypes.Phaser)
			{
				return LightGunController(portNum);
			}

			return null;
		}

		private static PadSchema JoystickController(int controller)
		{
			return new PadSchema
			{
				DisplayName = $"Player {controller}",
				Size = new Size(174, 90),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 12, controller),
					ButtonSchema.Down(14, 56, controller),
					ButtonSchema.Left(2, 34, controller),
					ButtonSchema.Right(24, 34, controller),
					new ButtonSchema(122, 34, controller, "B1", "1"),
					new ButtonSchema(146, 34, controller, "B2", "2")
				}
			};
		}

		private static PadSchema PaddleController(int controller)
		{
			return new PadSchema
			{
				DisplayName = $"Player {controller}",
				Size = new Size(334, 94),
				Buttons = new PadSchemaControl[]
				{
					new ButtonSchema(5, 24, controller, "B1", "1"),
					new SingleAxisSchema(55, 17, controller, "Paddle")
					{
						TargetSize = new Size(128, 69),
						MaxValue = 255,
						MinValue = 0
					}
				}
			};
		}

		private static PadSchema SportController(int controller)
		{
			return new PadSchema
			{
				DisplayName = $"Player {controller}",
				Size = new Size(334, 94),
				Buttons = new PadSchemaControl[]
				{
					new ButtonSchema(5, 24, controller, "B1", "1"),
					new ButtonSchema(5, 48, controller, "B2", "2"),
					new SingleAxisSchema(55, 17, controller, "X")
					{
						TargetSize = new Size(128, 69),
						MaxValue = 63,
						MinValue = -64
					},
					new SingleAxisSchema(193, 17, controller, "Y")
					{
						TargetSize = new Size(128, 69),
						MaxValue = 63,
						MinValue = -64
					}
				}
			};
		}

		private static PadSchema LightGunController(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Light Gun",
				Size = new Size(356, 290),
				Buttons = new PadSchemaControl[]
				{
					new TargetedPairSchema(14, 17, $"P{controller} X")
					{
						TargetSize = new Size(127, 192)
					},
					new ButtonSchema(284, 17, controller, "Trigger")
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
					new ButtonSchema(60, 15, "Pause")
				}
			};
		}
	}
}
