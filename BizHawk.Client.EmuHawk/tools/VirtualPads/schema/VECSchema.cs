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
					ButtonSchema.Up($"P{controller} Up", 14, 12),
					ButtonSchema.Down($"P{controller} Down", 14, 56),
					ButtonSchema.Left($"P{controller} Left", 2, 34),
					ButtonSchema.Right($"P{controller} Right", 24, 34),
					new ButtonSchema
					{
						Name = $"P{controller} Button 1",
						DisplayName = "1",
						Location = new Point(74, 34)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Button 2",
						DisplayName = "2",
						Location = new Point(98, 34)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Button 3",
						DisplayName = "3",
						Location = new Point(122, 34)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Button 4",
						DisplayName = "4",
						Location = new Point(146, 34)
					}
				}
			};
		}

		private static PadSchema AnalogController(int controller)
		{
			var controllerDefRanges = new AnalogControls(controller).Definition.FloatRanges;
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(280, 380),
				Buttons = new[]
				{
					new ButtonSchema
					{
						Name = $"P{controller} Button 1",
						DisplayName = "1",
						Location = new Point(74, 34)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Button 2",
						DisplayName = "2",
						Location = new Point(98, 34)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Button 3",
						DisplayName = "3",
						Location = new Point(122, 34)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Button 4",
						DisplayName = "4",
						Location = new Point(146, 34)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Stick X",
						Location = new Point(2, 80),
						AxisRange = controllerDefRanges[0],
						SecondaryAxisRange = controllerDefRanges[1],
						Type = PadInputType.AnalogStick,
						SecondaryNames = new[]
						{
							$"P{controller} Stick Y"
						}
					}
				}
			};
		}
	}
}
