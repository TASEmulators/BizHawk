using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public class LibsnesControllerDeck
	{
		public enum ControllerType
		{
			Unplugged,
			Gamepad,
			Multitap,
			Mouse,
			SuperScope,
			Justifier,
			Payload
		}

		private static ILibsnesController Factory(ControllerType t, LibsnesCore.SnesSyncSettings ss)
		{
			switch (t)
			{
				case ControllerType.Unplugged:
					return new SnesUnpluggedController();
				case ControllerType.Gamepad:
					return new SnesController();
				case ControllerType.Multitap:
					return new SnesMultitapController();
				case ControllerType.Payload:
					return new SnesPayloadController();
				case ControllerType.Mouse:
					return new SnesMouseController
					{
						LimitAnalogChangeSensitivity = ss.LimitAnalogChangeSensitivity
					};
				case ControllerType.SuperScope:
					return new SnesSuperScopeController();
				case ControllerType.Justifier:
					return new SnesJustifierController();
				default:
					throw new InvalidOperationException();
			}
		}

		private readonly ILibsnesController[] _ports;
		private readonly ControlDefUnMerger[] _mergers;

		public ControllerDefinition Definition { get; private set; }

		public LibsnesControllerDeck(LibsnesCore.SnesSyncSettings ss)
		{
			_ports = new[]
			{
				Factory(ss.LeftPort, ss),
				Factory(ss.RightPort, ss)
			};

			List<ControlDefUnMerger> tmp;
			Definition = ControllerDefinitionMerger.GetMerged(_ports.Select(p => p.Definition), out tmp);
			_mergers = tmp.ToArray();

			// add buttons that the core itself will handle
			Definition.BoolButtons.Add("Reset");
			Definition.BoolButtons.Add("Power");
			Definition.Name = "SNES Controller";
		}

		public void NativeInit(LibsnesApi api)
		{
			for (int i = 0; i < 2; i++)
			{
				api.SetInputPortBeforeInit(i, _ports[i].PortType);
			}
		}

		public short CoreInputState(IController controller, int port, int device, int index, int id)
		{
			return _ports[port].GetState(_mergers[port].UnMerge(controller), index, id);
		}
	}

	public interface ILibsnesController
	{
		/// <summary>
		/// the type to pass back to the native init
		/// </summary>
		LibsnesApi.SNES_INPUT_PORT PortType { get; }

		/// <summary>
		/// respond to a native core poll
		/// </summary>
		/// <param name="controller">controller input from user, remapped</param>
		/// <param name="index">libsnes specific value, sometimes multitap number</param>
		/// <param name="id">libsnes specific value, sometimes button number</param>
		/// <returns></returns>
		short GetState(IController controller, int index, int id);

		ControllerDefinition Definition { get; }

		// due to the way things are implemented, right now, all of the ILibsnesControllers are stateless
		// but if one needed state, that would be doable
		// void SyncState(Serializer ser);
	}

	public class SnesController : ILibsnesController
	{
		public LibsnesApi.SNES_INPUT_PORT PortType { get; } = LibsnesApi.SNES_INPUT_PORT.Joypad;

		private static readonly string[] Buttons =
		{
			"0B",
			"0Y",
			"0Select",
			"0Start",
			"0Up",
			"0Down",
			"0Left",
			"0Right",
			"0A",
			"0X",
			"0L",
			"0R"
		};

		private static int ButtonOrder(string btn)
		{
			var order = new Dictionary<string, int>
			{
				["0Up"] = 0,
				["0Down"] = 1,
				["0Left"] = 2,
				["0Right"] = 3,

				["0Select"] = 4,
				["0Start"] = 5,

				["0Y"] = 6,
				["0B"] = 7,

				["0X"] = 8,
				["0A"] = 9,

				["0L"] = 10,
				["0R"] = 11
			};

			return order[btn];
		}

		private static readonly ControllerDefinition _definition = new ControllerDefinition
		{
			BoolButtons = Buttons.OrderBy(ButtonOrder).ToList()
		};

		public ControllerDefinition Definition { get; } = _definition;

		public short GetState(IController controller, int index, int id)
		{
			if (id >= 12)
			{
				return 0;
			}
			return (short)(controller.IsPressed(Buttons[id]) ? 1 : 0);
		}
	}

	public class SnesMultitapController : ILibsnesController
	{
		public LibsnesApi.SNES_INPUT_PORT PortType { get; } = LibsnesApi.SNES_INPUT_PORT.Multitap;

		private static readonly string[] Buttons =
		{
			"B",
			"Y",
			"Select",
			"Start",
			"Up",
			"Down",
			"Left",
			"Right",
			"A",
			"X",
			"L",
			"R"
		};

		private static int ButtonOrder(string btn)
		{
			var order = new Dictionary<string, int>
			{
				["Up"] = 0,
				["Down"] = 1,
				["Left"] = 2,
				["Right"] = 3,

				["Select"] = 4,
				["Start"] = 5,

				["Y"] = 6,
				["B"] = 7,

				["X"] = 8,
				["A"] = 9,

				["L"] = 10,
				["R"] = 11
			};

			return order[btn];
		}

		private static readonly ControllerDefinition _definition = new ControllerDefinition
		{
			BoolButtons = Enumerable.Range(0, 4)
			.SelectMany(i => Buttons
				.OrderBy(ButtonOrder)
				.Select(b => i + b))
			.Concat(new[] { "0Toggle Multitap" })
			.ToList()
		};

		public ControllerDefinition Definition { get; } = _definition;

		public short GetState(IController controller, int index, int id)
		{
			if (id == 16)
			{
				return (short)(controller.IsPressed("0Toggle Multitap") ? 1 : 0);
			}
			if (id >= 12)
			{
				return 0;
			}
			return (short)(controller.IsPressed(index + Buttons[id]) ? 1 : 0);
		}
	}

	public class SnesPayloadController : ILibsnesController
	{
		public LibsnesApi.SNES_INPUT_PORT PortType { get; } = LibsnesApi.SNES_INPUT_PORT.Multitap;

		private static readonly ControllerDefinition _definition = new ControllerDefinition
		{
			BoolButtons = Enumerable.Range(0, 32).Select(i => "0B" + i).ToList()
		};

		public ControllerDefinition Definition { get; } = _definition;

		public short GetState(IController controller, int index, int id)
		{
			return (short)(controller.IsPressed("0B" + (index << 4 & 16 | id)) ? 1 : 0);
		}
	}

	public class SnesUnpluggedController : ILibsnesController
	{
		public LibsnesApi.SNES_INPUT_PORT PortType { get; } = LibsnesApi.SNES_INPUT_PORT.None;

		private static readonly ControllerDefinition _definition = new ControllerDefinition();

		public ControllerDefinition Definition { get; } = _definition;

		public short GetState(IController controller, int index, int id)
		{
			return 0;
		}
	}

	public class SnesMouseController : ILibsnesController
	{
		public LibsnesApi.SNES_INPUT_PORT PortType => LibsnesApi.SNES_INPUT_PORT.Mouse;

		private static readonly ControllerDefinition _definition = new ControllerDefinition
		{
			BoolButtons = new List<string>
			{
				"0Mouse Left",
				"0Mouse Right"
			},
			FloatControls =
			{
				"0Mouse X",
				"0Mouse Y"
			},
			FloatRanges =
			{
				new[] { -127f, 0f, 127f },
				new[] { -127f, 0f, 127f }
			}
		};

		public ControllerDefinition Definition => _definition;

		public bool LimitAnalogChangeSensitivity { get; set; } = true;

		public short GetState(IController controller, int index, int id)
		{
			switch (id)
			{
				default:
					return 0;
				case 0:
					var x = (int)controller.GetFloat("0Mouse X");
					if (LimitAnalogChangeSensitivity)
					{
						x = x.Clamp(-10, 10);
					}

					return (short)x;
				case 1:
					var y = (int)controller.GetFloat("0Mouse Y");
					if (LimitAnalogChangeSensitivity)
					{
						y = y.Clamp(-10, 10);
					}

					return (short)y;
				case 2:
					return (short)(controller.IsPressed("0Mouse Left") ? 1 : 0);
				case 3:
					return (short)(controller.IsPressed("0Mouse Right") ? 1 : 0);
			}
		}
	}

	public class SnesSuperScopeController : ILibsnesController
	{
		public LibsnesApi.SNES_INPUT_PORT PortType => LibsnesApi.SNES_INPUT_PORT.SuperScope;

		private static readonly ControllerDefinition _definition = new ControllerDefinition
		{
			BoolButtons = new List<string>
			{
				"0Trigger",
				"0Cursor",
				"0Turbo",
				"0Pause"
			},
			FloatControls =
			{
				"0Scope X",
				"0Scope Y"
			},
			FloatRanges =
			{
				// problem: when you're in 240 line mode, the limit on Y needs to be 240.
				// when you're in 224 mode, it needs to be 224.  perhaps the deck needs to account for this...
				new[] { 0f, 128f, 256f },
				new[] { 0f, 0f, 240f }
			}
		};

		public ControllerDefinition Definition => _definition;

		public short GetState(IController controller, int index, int id)
		{
			switch (id)
			{
				default:
					return 0;
				case 0:
					var x = (int)controller.GetFloat("0Scope X");
					return (short)x;
				case 1:
					var y = (int)controller.GetFloat("0Scope Y");
					return (short)y;
				case 2:
					return (short)(controller.IsPressed("0Trigger") ? 1 : 0);
				case 3:
					return (short)(controller.IsPressed("0Cursor") ? 1 : 0);
				case 4:
					return (short)(controller.IsPressed("0Turbo") ? 1 : 0);
				case 5:
					return (short)(controller.IsPressed("0Pause") ? 1 : 0);
			}
		}
	}

	public class SnesJustifierController : ILibsnesController
	{
		public LibsnesApi.SNES_INPUT_PORT PortType => LibsnesApi.SNES_INPUT_PORT.Justifier;

		private static readonly ControllerDefinition _definition = new ControllerDefinition
		{
			BoolButtons = new List<string>
			{
				"0Trigger",
				"0Start",
				"1Trigger",
				"1Start"
			},
			FloatControls =
			{
				"0Justifier X",
				"0Justifier Y",
				"1Justifier X",
				"1Justifier Y",
			},
			FloatRanges =
			{
				// problem: when you're in 240 line mode, the limit on Y needs to be 240.
				// when you're in 224 mode, it needs to be 224.  perhaps the deck needs to account for this...
				new[] { 0f, 128f, 256f },
				new[] { 0f, 0f, 240f },
				new[] { 0f, 128f, 256f },
				new[] { 0f, 0f, 240f }
			}
		};

		public ControllerDefinition Definition => _definition;

		public short GetState(IController controller, int index, int id)
		{
			switch (id)
			{
				default:
					return 0;
				case 0:
					var x = (int)controller.GetFloat(index + "Justifier X");
					return (short)x;
				case 1:
					var y = (int)controller.GetFloat(index + "Justifier Y");
					return (short)y;
				case 2:
					return (short)(controller.IsPressed(index + "Trigger") ? 1 : 0);
				case 3:
					return (short)(controller.IsPressed(index + "Start") ? 1 : 0);
			}
		}
	}
}
