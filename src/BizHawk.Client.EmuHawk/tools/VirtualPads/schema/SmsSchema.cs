using System;
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
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
		{
			if (((SMS)core).IsGameGear)
			{
				yield return StandardController(1, false);
				yield return Console(false);
			}
			else
			{
				yield return StandardController(1, true);
				yield return StandardController(2, true);
				yield return Console(true);
			}
		}

		private static PadSchema StandardController(int controller, bool isSms)
		{
			return new PadSchema
			{
				Size = new Size(174, 90),
				Buttons = StandardButtons(controller, isSms)
			};
		}

		private static IEnumerable<ButtonSchema> StandardButtons(int controller, bool isSms)
		{

			yield return ButtonSchema.Up(14, 12, controller);
			yield return ButtonSchema.Down(14, 56, controller);
			yield return ButtonSchema.Left(2, 34, controller);
			yield return ButtonSchema.Right(24, 34, controller);
			yield return new ButtonSchema(122, 34, controller, "B1", "1");
			yield return new ButtonSchema(146, 34, controller, "B2", "2");
			if (!isSms)
			{
				yield return new ButtonSchema(134, 12, controller, "Start", "S");
			}
		}

		private static PadSchema Console(bool isSms)
		{
			return new ConsoleSchema
			{
				Size = new Size(150, 50),
				Buttons = ConsoleButtons(isSms)
			};
		}

		private static IEnumerable<ButtonSchema> ConsoleButtons(bool isSms)
		{
			yield return new ButtonSchema(10, 15, "Reset");
			if (isSms)
			{
				yield return new ButtonSchema(58, 15, "Pause");
			}
		}
	}
}
