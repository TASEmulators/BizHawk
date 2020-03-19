using BizHawk.Emulation.Common;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Emulation.Cores.Consoles.NEC.PCFX
{
	public enum ControllerType
	{
		None = 0,
		Gamepad = 1,
		Mouse
	}

	public class TstControllerDeck
	{
		private readonly ControlDefUnMerger[] _cdums;
		private readonly IPortDevice[] _devices;

		private static readonly string[] _consoleButtons =
		{
				"Power",
				"Reset",
				"Previous Disk",
				"Next Disk"
		};

		public TstControllerDeck(IEnumerable<ControllerType> ports)
		{
			_devices = ports.Select<ControllerType, IPortDevice>(p =>
			{
				switch (p)
				{
					case ControllerType.Gamepad:
						return new Gamepad();
					case ControllerType.Mouse:
						return new Mouse();
					default:
						return new None();
				}
			}).ToArray();

			Definition = ControllerDefinitionMerger.GetMerged(
				_devices.Select(d => d.Definition),
				out var tmp);
			_cdums = tmp.ToArray();

			Definition.Name = "PC-FX Controller";
			Definition.BoolButtons.AddRange(_consoleButtons);
		}

		public uint[] GetData(IController c)
		{
			var ret = new uint[_devices.Length + 1];
			for (int i = 0; i < _devices.Length; i++)
				ret[i] = _devices[i].GetData(_cdums[i].UnMerge(c));

			uint console = 0;
			uint val = 1;
			foreach (var s in _consoleButtons)
			{
				if (c.IsPressed(s))
					console |= val;
				val <<= 1;
			}
			ret[_devices.Length] = console;
			return ret;
		}

		public ControllerDefinition Definition { get; }

		private interface IPortDevice
		{
			ControllerDefinition Definition { get; }
			uint GetData(IController c);
		}

		private class None : IPortDevice
		{
			private static readonly ControllerDefinition _definition = new ControllerDefinition();

			public ControllerDefinition Definition => _definition;

			public uint GetData(IController c)
			{
				return 0;
			}
		}

		private class Gamepad : IPortDevice
		{
			private static readonly ControllerDefinition _definition;
			static Gamepad()
			{
				_definition = new ControllerDefinition
				{
					BoolButtons = Buttons
					.Where(s => s != null)
					.Select((s, i) => new { s, i })
					.OrderBy(a => ButtonOrders[a.i])
					.Select(a => a.s)
					.ToList()
				};
			}

			private static readonly string[] Buttons =
			{
				"0I", "0II", "0III", "0IV", "0V", "0VI",
				"0Select", "0Run",
				"0Up", "0Right", "0Down", "0Left",
				"0Mode 1", null, "0Mode 2"
			};

			private static readonly int[] ButtonOrders =
			{
				5, 6, 7, 8, 9, 10,
				11, 12,
				1, 2, 3, 4,
				13, 14
			};

			public ControllerDefinition Definition => _definition;

			public uint GetData(IController c)
			{
				uint ret = 0;
				uint val = 1;
				foreach (var s in Buttons)
				{
					if (s != null && c.IsPressed(s))
						ret |= val;
					val <<= 1;
				}
				return ret;
			}
		}

		private class Mouse : IPortDevice
		{
			private static readonly ControllerDefinition _definition = new ControllerDefinition
			{
				BoolButtons = { "0Mouse Left", "0Mouse Right" },
				FloatControls = { "0X", "0Y" },
				FloatRanges =
				{
					new[] { -127f, 0f, 127f },
					new[] { -127f, 0f, 127f }
				}
			};

			public ControllerDefinition Definition => _definition;

			public uint GetData(IController c)
			{
				var dx = (byte)(int)c.GetFloat("0X");
				var dy = (byte)(int)c.GetFloat("0Y");
				uint ret = 0;
				if (c.IsPressed("0Mouse Left"))
					ret |= 0x10000;
				if (c.IsPressed("0Mouse Right"))
					ret |= 0x20000;
				ret |= dx;
				ret |= (uint)(dy << 8);
				return ret;
			}
		}
	}
}
