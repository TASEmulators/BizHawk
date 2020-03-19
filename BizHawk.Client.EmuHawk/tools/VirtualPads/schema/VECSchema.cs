using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Vectrex;

namespace BizHawk.Client.EmuHawk
{
	[Schema("VEC")]
	// ReSharper disable once UnusedMember.Global
	public class VecSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			var vecSyncSettings = ((VectrexHawk)core).GetSyncSettings().Clone();
			var port1 = vecSyncSettings.Port1;
			var port2 = vecSyncSettings.Port2;

			if (port1 == "Vectrex Digital Controller")
			{
				yield return StandardController(1);
			}

			if (port2 == "Vectrex Digital Controller")
			{
				yield return StandardController(2);
			}

			if (port1 == "Vectrex Analog Controller")
			{
				yield return AnalogController(1);
			}

			if (port2 == "Vectrex Analog Controller")
			{
				yield return AnalogController(2);
			}
		}

		private static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(200, 100),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Up",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(14, 12),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Down",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(14, 56),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Left",
						Icon = Properties.Resources.Back,
						Location = new Point(2, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Right",
						Icon = Properties.Resources.Forward,
						Location = new Point(24, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Button 1",
						DisplayName = "1",
						Location = new Point(74, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Button 2",
						DisplayName = "2",
						Location = new Point(98, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Button 3",
						DisplayName = "3",
						Location = new Point(122, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Button 4",
						DisplayName = "4",
						Location = new Point(146, 34),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}

		private static PadSchema AnalogController(int controller)
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(280, 380),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Button 1",
						DisplayName = "1",
						Location = new Point(74, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Button 2",
						DisplayName = "2",
						Location = new Point(98, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Button 3",
						DisplayName = "3",
						Location = new Point(122, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Button 4",
						DisplayName = "4",
						Location = new Point(146, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Stick X",
						Location = new Point(2, 80),
						MinValue = 127,
						MidValue = 0,
						MaxValue = -128,
						MinValueSec = -128,
						MidValueSec = 0,
						MaxValueSec = 127,
						Type = PadSchema.PadInputType.AnalogStick,
						SecondaryNames = new[]
						{
							$"P{controller} Stick Y",
						}
					}
				}
			};
		}
	}
}
