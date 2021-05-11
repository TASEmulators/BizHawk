using System;
using System.Collections.Generic;
using System.Linq;
using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;
using static BizHawk.Emulation.Cores.Nintendo.SNES.BsnesApi;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public class BsnesControllers
	{
		private static IBsnesController GetController(BSNES_INPUT_DEVICE t, BsnesCore.SnesSyncSettings ss)
		{
			switch (t)
			{
				case BSNES_INPUT_DEVICE.None:
					return new BsnesUnpluggedController();
				case BSNES_INPUT_DEVICE.Gamepad:
					return new BsnesController();
				case BSNES_INPUT_DEVICE.Mouse:
					return new BsnesMouseController
					{
						LimitAnalogChangeSensitivity = ss.LimitAnalogChangeSensitivity
					};
				case BSNES_INPUT_DEVICE.SuperMultitap:
					return new BsnesMultitapController();
				case BSNES_INPUT_DEVICE.SuperScope:
					return new BsnesSuperScopeController();
				case BSNES_INPUT_DEVICE.Justifier:
					return new BsnesJustifierController(false);
				case BSNES_INPUT_DEVICE.Justifiers:
					return new BsnesJustifierController(true);
				default:
					throw new InvalidOperationException();
			}
		}

		// TODO this should probably be private, only hacked it public
		public readonly IBsnesController[] _ports;
		private readonly ControlDefUnMerger[] _mergers;

		public ControllerDefinition Definition { get; }

		public BsnesControllers(BsnesCore.SnesSyncSettings ss)
		{
			_ports = new[]
			{
				GetController(ss.LeftPort, ss),
				GetController(ss.RightPort, ss)
			};

			Definition = ControllerDefinitionMerger.GetMerged(_ports.Select(p => p.Definition), out var tmp);
			_mergers = tmp.ToArray();

			// add buttons that the core itself will handle
			Definition.BoolButtons.Add("Reset");
			Definition.BoolButtons.Add("Power");
			Definition.Name = "SNES Controller";
		}

		public void CoreInputPoll(IController controller)
		{
			// i hope this is correct lol
			for (int i = 0; i < 2; i++)
			{
				_ports[i].UpdateState(_mergers[i].UnMerge(controller));
			}
		}

		public short CoreInputState(int port, int index, int id)
		{
			return _ports[port].GetState(index, id);
		}
	}

	public interface IBsnesController
	{
		/// <summary>
		/// the type to pass back to the native init
		/// </summary>
		BSNES_INPUT_DEVICE DeviceType { get; }

		// Updates the internal state; gets called once per frame from the core
		void UpdateState(IController controller);

		/// <summary>
		/// Returns the internal state; gets called potentially many times per frame
		/// </summary>
		/// <param name="index">bsnes specific value, sometimes multitap number</param>
		/// <param name="id">bsnes specific value, sometimes button number</param>
		short GetState(int index, int id);

		ControllerDefinition Definition { get; }
	}

	internal class BsnesController : IBsnesController
	{
		public BSNES_INPUT_DEVICE DeviceType => BSNES_INPUT_DEVICE.Gamepad;

		private readonly bool[] _state = new bool[12];

		private static readonly string[] Buttons =
		{
			"0Up", "0Down", "0Left", "0Right", "0B", "0A", "0Y", "0X", "0L", "0R", "0Select", "0Start"
		};

		private static readonly Dictionary<string, int> ButtonsOrder = new()
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

		private static readonly ControllerDefinition _definition = new()
		{
			BoolButtons = Buttons.OrderBy(b => ButtonsOrder[b]).ToList()
		};

		public ControllerDefinition Definition => _definition;

		public void UpdateState(IController controller)
		{
			for (int i = 0; i < 12; i++)
			{
				_state[i] = controller.IsPressed(Buttons[i]);
			}
		}

		public short GetState(int index, int id)
		{
			if (id >= 12)
				return 0;

			return (short) (_state[id] ? 1 : 0);
		}
	}

	public class BsnesMultitapController : IBsnesController
	{
		public BSNES_INPUT_DEVICE DeviceType => BSNES_INPUT_DEVICE.SuperMultitap;

		private readonly bool[,] _state = new bool[4, 12];

		private static readonly string[] Buttons =
		{
			"Up", "Down", "Left", "Right", "B", "A", "Y", "X", "L", "R", "Select", "Start"
		};

		private static readonly Dictionary<string, int> ButtonsOrder = new()
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
			["R"] = 10,
			["L"] = 11
		};

		private static readonly ControllerDefinition _definition = new()
		{
			BoolButtons = Enumerable.Range(0, 4)
			.SelectMany(i => Buttons.OrderBy(b => ButtonsOrder[b])
				.Select(b => i + b))
			.ToList()
		};

		public ControllerDefinition Definition => _definition;

		public void UpdateState(IController controller)
		{
			for (int port = 0; port < 4; port++)
			for (int i = 0; i < 12; i++)
			{
				_state[port, i] = controller.IsPressed(port + Buttons[i]);
			}
		}

		public short GetState(int index, int id)
		{
			if (id >= 12 || index >= 4)
				return 0;

			return (short) (_state[index, id] ? 1 : 0);
		}
	}

	public class BsnesUnpluggedController : IBsnesController
	{
		public BSNES_INPUT_DEVICE DeviceType => BSNES_INPUT_DEVICE.None;

		private static readonly ControllerDefinition _definition = new();

		public ControllerDefinition Definition => _definition;

		public void UpdateState(IController controller) { }

		public short GetState(int index, int id) => 0;
	}

	public class BsnesMouseController : IBsnesController
	{
		public BSNES_INPUT_DEVICE DeviceType => BSNES_INPUT_DEVICE.Mouse;

		private readonly short[] _state = new short[4];

		private static readonly ControllerDefinition _definition = new ControllerDefinition
			{ BoolButtons = { "0Mouse Left", "0Mouse Right" } }
			.AddXYPair("0Mouse {0}", AxisPairOrientation.RightAndDown, (-127).RangeTo(127), 0); //TODO verify direction against hardware, R+D inferred from behaviour in Mario Paint

		public ControllerDefinition Definition => _definition;
		public bool LimitAnalogChangeSensitivity { get; init; } = true;

		public void UpdateState(IController controller)
		{
			int x = controller.AxisValue("0Mouse X");
			if (LimitAnalogChangeSensitivity)
			{
				x = x.Clamp(-10, 10);
			}
			_state[0] = (short) x;

			int y = controller.AxisValue("0Mouse Y");
			if (LimitAnalogChangeSensitivity)
			{
				y = y.Clamp(-10, 10);
			}
			_state[1] = (short) y;

			_state[2] = (short) (controller.IsPressed("0Mouse Left") ? 1 : 0);
			_state[3] = (short) (controller.IsPressed("0Mouse Right") ? 1 : 0);
		}

		public short GetState(int index, int id)
		{
			if (id >= 4)
				return 0;

			return _state[id];
		}
	}

	public class BsnesSuperScopeController : IBsnesController
	{
		public BSNES_INPUT_DEVICE DeviceType => BSNES_INPUT_DEVICE.SuperScope;

		private readonly short[] _state = new short[6];

		private static readonly ControllerDefinition _definition = new ControllerDefinition
			{ BoolButtons = { "0Trigger", "0Cursor", "0Turbo", "0Pause" } }
			.AddLightGun("0Scope {0}");

		public ControllerDefinition Definition => _definition;

		public void UpdateState(IController controller)
		{
			_state[0] = (short) controller.AxisValue("0Scope X");
			_state[1] = (short) controller.AxisValue("0Scope Y");
			for (int i = 2; i < 6; i++)
			{
				_state[i] = (short) (controller.IsPressed(_definition.BoolButtons[i]) ? 1 : 0);
			}
		}

		public short GetState(int index, int id)
		{
			if (id >= 6)
				return 0;

			return _state[id];
		}
	}

	public class BsnesJustifierController : IBsnesController
	{
		public BsnesJustifierController(bool chained)
		{
			Definition = chained
				? new ControllerDefinition
					{ BoolButtons = { "0Trigger", "0Start", "1Trigger", "1Start" } }
					.AddLightGun("0Justifier {0}")
					.AddLightGun("1Justifier {0}")
				: new ControllerDefinition
					{BoolButtons = { "0Trigger", "0Start"} }
					.AddLightGun("0Justifier {0}");
			_state = new short[chained ? 8 : 4];
			_chained = chained;
		}

		private readonly bool _chained;
		private readonly short[] _state;

		public BSNES_INPUT_DEVICE DeviceType => _chained ? BSNES_INPUT_DEVICE.Justifiers : BSNES_INPUT_DEVICE.Justifier;

		public ControllerDefinition Definition { get; }

		public void UpdateState(IController controller)
		{
			_state[0] = (short) controller.AxisValue("0Justifier X");
			_state[1] = (short) controller.AxisValue("0Justifier Y");
			_state[2] = (short) (controller.IsPressed(Definition.BoolButtons[0]) ? 1 : 0);
			_state[3] = (short) (controller.IsPressed(Definition.BoolButtons[1]) ? 1 : 0);
			if (_chained)
			{
				_state[4] = (short) controller.AxisValue("1Justifier X");
				_state[5] = (short) controller.AxisValue("1Justifier Y");
				_state[6] = (short) (controller.IsPressed(Definition.BoolButtons[2]) ? 1 : 0);
				_state[7] = (short) (controller.IsPressed(Definition.BoolButtons[3]) ? 1 : 0);
			}
		}
		public short GetState(int index, int id)
		{
			if (index >= 2 || id >= 4 || (index == 1 && !_chained))
				return 0;

			return _state[index * 4 + id];
		}
	}
}
