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
				// case BSNES_INPUT_DEVICE.SuperScope:
					// return new BsnesSuperScopeController();
				// case BSNES_INPUT_DEVICE.Justifier:
					// return new BsnesJustifierController();
				default:
					throw new InvalidOperationException();
			}
		}

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

		// updates the state; corresponding to a poll request from the core
		void UpdateState(IController controller);

		/// <summary>
		/// respond to a native core poll
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

		private static readonly Dictionary<int, string> ButtonsDict = new()
		{
			{0, "0Up"},
			{1, "0Down"},
			{2, "0Left"},
			{3, "0Right"},
			{10, "0Select"},
			{11, "0Start"},
			{6, "0Y"},
			{4, "0B"},
			{7, "0X"},
			{5, "0A"},
			{8, "0L"},
			{9, "0R"},
		};

		private static readonly ControllerDefinition _definition = new()
		{
			BoolButtons = new List<string>(ButtonsDict.Values)
		};

		public ControllerDefinition Definition => _definition;

		public void UpdateState(IController controller)
		{
			for (int i = 0; i < 12; i++)
			{
				_state[i] = controller.IsPressed(ButtonsDict[i]);
			}
		}

		public short GetState(int index, int id)
		{
			if (id >= 12)
				return 0;

			return (short) (_state[id] ? 1 : 0);
		}
	}

	// TODO: no idea how these controllers map and what TAStudio / frontend / core expects, so this needs to be re-mapped
	public class BsnesMultitapController : IBsnesController
	{
		public BSNES_INPUT_DEVICE DeviceType => BSNES_INPUT_DEVICE.SuperMultitap;

		private readonly bool[] _state = new bool[12 * 4];

		private static readonly string[] Buttons =
		{
			"Up",
			"Down",
			"Left",
			"Right",
			"Select",
			"Start",
			"Y",
			"B",
			"X",
			"A",
			"R",
			"L"
		};

		private static readonly ControllerDefinition _definition = new()
		{
			BoolButtons = Enumerable.Range(0, 4)
			.SelectMany(i => Buttons
				.Select(b => i + b))
			.ToList()
		};

		public ControllerDefinition Definition => _definition;

		public void UpdateState(IController controller)
		{
			for (int i = 0; i < _state.Length; i++)
			{
				_state[i] = controller.IsPressed(_definition.BoolButtons[i]);
			}
		}

		public short GetState(int index, int id)
		{
			if (id >= 12 * 4)
				return 0;

			return (short)(_state[id] ? 1 : 0);
		}
	}

	public class BsnesUnpluggedController : IBsnesController
	{
		public BSNES_INPUT_DEVICE DeviceType => BSNES_INPUT_DEVICE.None;

		private static readonly ControllerDefinition _definition = new();

		public ControllerDefinition Definition => _definition;

		public void UpdateState(IController controller) { }

		public short GetState(int index, int id)
		{
			return 0;
		}
	}

	public class BsnesMouseController : IBsnesController
	{
		public BSNES_INPUT_DEVICE DeviceType => BSNES_INPUT_DEVICE.Mouse;

		private readonly short[] _state = new short[4];

		private static readonly ControllerDefinition _definition
			= new ControllerDefinition { BoolButtons = { "0Mouse Left", "0Mouse Right" } }
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
			_state[3] = (short)(controller.IsPressed("0Mouse Right") ? 1 : 0);
		}

		public short GetState(int index, int id)
		{
			if (id >= 4)
				return 0;

			return _state[id];
		}
	}

	// public class SnesSuperScopeController : ILibsnesController
	// {
	// 	public LibsnesApi.SNES_INPUT_PORT PortType => LibsnesApi.SNES_INPUT_PORT.SuperScope;
	//
	// 	private static readonly ControllerDefinition _definition
	// 		= new ControllerDefinition { BoolButtons = { "0Trigger", "0Cursor", "0Turbo", "0Pause" } }
	// 			.AddLightGun("0Scope {0}");
	//
	// 	public ControllerDefinition Definition => _definition;
	//
	// 	public short GetState(IController controller, int index, int id)
	// 	{
	// 		switch (id)
	// 		{
	// 			default:
	// 				return 0;
	// 			case 0:
	// 				var x = controller.AxisValue("0Scope X");
	// 				return (short)x;
	// 			case 1:
	// 				var y = controller.AxisValue("0Scope Y");
	// 				return (short)y;
	// 			case 2:
	// 				return (short)(controller.IsPressed("0Trigger") ? 1 : 0);
	// 			case 3:
	// 				return (short)(controller.IsPressed("0Cursor") ? 1 : 0);
	// 			case 4:
	// 				return (short)(controller.IsPressed("0Turbo") ? 1 : 0);
	// 			case 5:
	// 				return (short)(controller.IsPressed("0Pause") ? 1 : 0);
	// 		}
	// 	}
	// }
	//
	// public class SnesJustifierController : ILibsnesController
	// {
	// 	public LibsnesApi.SNES_INPUT_PORT PortType => LibsnesApi.SNES_INPUT_PORT.Justifier;
	//
	// 	private static readonly ControllerDefinition _definition
	// 		= new ControllerDefinition { BoolButtons = { "0Trigger", "0Start", "1Trigger", "1Start" } }
	// 			.AddLightGun("0Justifier {0}")
	// 			.AddLightGun("1Justifier {0}");
	//
	// 	public ControllerDefinition Definition => _definition;
	//
	// 	public short GetState(IController controller, int index, int id)
	// 	{
	// 		switch (id)
	// 		{
	// 			default:
	// 				return 0;
	// 			case 0:
	// 				var x = controller.AxisValue(index + "Justifier X");
	// 				return (short)x;
	// 			case 1:
	// 				var y = controller.AxisValue(index + "Justifier Y");
	// 				return (short)y;
	// 			case 2:
	// 				return (short)(controller.IsPressed(index + "Trigger") ? 1 : 0);
	// 			case 3:
	// 				return (short)(controller.IsPressed(index + "Start") ? 1 : 0);
	// 		}
	// 	}
	// }
}
