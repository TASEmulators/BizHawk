using System.Collections.Generic;
using System.Linq;
using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using static BizHawk.Emulation.Cores.Nintendo.BSNES.BsnesApi;

namespace BizHawk.Emulation.Cores.Nintendo.BSNES
{
	public class BsnesControllers
	{
		private static IBsnesController GetController(BSNES_INPUT_DEVICE t, BsnesCore.SnesSyncSettings ss)
		{
			return t switch
			{
				BSNES_INPUT_DEVICE.None => new BsnesUnpluggedController(),
				BSNES_INPUT_DEVICE.Gamepad => new BsnesController(),
				BSNES_INPUT_DEVICE.ExtendedGamepad => new BsnesExtendedController(),
				BSNES_INPUT_DEVICE.Mouse => new BsnesMouseController
				{
					LimitAnalogChangeSensitivity = ss.LimitAnalogChangeSensitivity
				},
				BSNES_INPUT_DEVICE.SuperMultitap => new BsnesMultitapController(),
				BSNES_INPUT_DEVICE.Payload => new BsnesPayloadController(),
				BSNES_INPUT_DEVICE.SuperScope => new BsnesSuperScopeController(),
				BSNES_INPUT_DEVICE.Justifier => new BsnesJustifierController(false),
				BSNES_INPUT_DEVICE.Justifiers => new BsnesJustifierController(true),
				_ => throw new InvalidOperationException()
			};
		}

		private readonly IBsnesController[] _ports;
		private readonly ControlDefUnMerger[] _mergers;

		public ControllerDefinition Definition { get; }

		public BsnesControllers(BsnesCore.SnesSyncSettings ss, bool subframe = false)
		{
			_ports = new[]
			{
				GetController((BSNES_INPUT_DEVICE)ss.LeftPort, ss),
				GetController(ss.RightPort, ss)
			};

			Definition = ControllerDefinitionMerger.GetMerged(
				"SNES Controller",
				_ports.Select(p => p.Definition),
				out var tmp);
			_mergers = tmp.ToArray();

			// add buttons that the core itself will handle
			Definition.BoolButtons.Add("Reset");
			Definition.BoolButtons.Add("Power");
			if (subframe)
			{
				// When set, only emulate until the next input latch (or until the frame ends)
				Definition.BoolButtons.Add("Subframe");
				// Amount of instructions to execute before resetting; range hopefully set large enough
				Definition.AddAxis("Reset Instruction", 0.RangeTo(200000), 0);
			}

			Definition.MakeImmutable();
		}

		public short CoreInputPoll(IController controller, int port, int index, int id)
		{
			return _ports[port].GetState(_mergers[port].UnMerge(controller), index, id);
		}
	}

	public interface IBsnesController
	{
		/// <summary>
		/// Corresponds to an InputPoll call from the core; gets called potentially many times per frame
		/// </summary>
		/// <param name="index">bsnes specific value, sometimes multitap number</param>
		/// <param name="id">bsnes specific value, sometimes button number</param>
		short GetState(IController controller, int index, int id);

		ControllerDefinition Definition { get; }
	}

	internal class BsnesUnpluggedController : IBsnesController
	{
		private static readonly ControllerDefinition _definition = new("(SNES Controller fragment)");

		public ControllerDefinition Definition => _definition;

		public short GetState(IController controller, int index, int id) => 0;
	}

	internal class BsnesController : IBsnesController
	{
		private static readonly string[] Buttons =
		{
			"0Up", "0Down", "0Left", "0Right", "0B", "0A", "0Y", "0X", "0L", "0R", "0Select", "0Start"
		};

		private static readonly Dictionary<string, int> DisplayButtonOrder = new()
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

		private static readonly ControllerDefinition _definition = new("(SNES Controller fragment)")
		{
			BoolButtons = Buttons.OrderBy(b => DisplayButtonOrder[b]).ToList()
		};

		public ControllerDefinition Definition => _definition;

		public short GetState(IController controller, int index, int id)
		{
			if (id >= 12)
				return 0;

			return (short) (controller.IsPressed(Buttons[id]) ? 1 : 0);
		}
	}
	internal class BsnesExtendedController : IBsnesController
	{
		private static readonly string[] Buttons =
		{
			"0Up", "0Down", "0Left", "0Right", "0B", "0A", "0Y", "0X", "0L", "0R", "0Select", "0Start", "0Extra1", "0Extra2", "0Extra3", "0Extra4"
		};

		private static readonly Dictionary<string, int> DisplayButtonOrder = new()
		{
			["0B"] = 0,
			["0Y"] = 1,
			["0Select"] = 2,
			["0Start"] = 3,
			["0Up"] = 4,
			["0Down"] = 5,
			["0Left"] = 6,
			["0Right"] = 7,
			["0A"] = 8,
			["0X"] = 9,
			["0L"] = 10,
			["0R"] = 11,
			["0Extra1"] = 12,
			["0Extra2"] = 13,
			["0Extra3"] = 14,
			["0Extra4"] = 15
		};

		private static readonly ControllerDefinition _definition = new("(SNES Controller fragment)")
		{
			BoolButtons = Buttons.OrderBy(b => DisplayButtonOrder[b]).ToList()
		};

		public ControllerDefinition Definition => _definition;

		public short GetState(IController controller, int index, int id)
		{
			if (id >= 16)
				return 0;

			return (short) (controller.IsPressed(Buttons[id]) ? 1 : 0);
		}
	}

	internal class BsnesMouseController : IBsnesController
	{
		private static readonly ControllerDefinition _definition = new ControllerDefinition("(SNES Controller fragment)")
				{ BoolButtons = { "0Mouse Left", "0Mouse Right" } }
			.AddXYPair("0Mouse {0}", AxisPairOrientation.RightAndDown, (-127).RangeTo(127), 0);

		public ControllerDefinition Definition => _definition;
		public bool LimitAnalogChangeSensitivity { get; init; } = true;

		public short GetState(IController controller, int index, int id)
		{
			switch (id)
			{
				case 0:
					int x = controller.AxisValue("0Mouse X");
					if (LimitAnalogChangeSensitivity)
					{
						x = x.Clamp(-10, 10);
					}
					return (short) x;
				case 1:
					int y = controller.AxisValue("0Mouse Y");
					if (LimitAnalogChangeSensitivity)
					{
						y = y.Clamp(-10, 10);
					}
					return (short) y;
				case 2:
					return (short) (controller.IsPressed("0Mouse Left") ? 1 : 0);
				case 3:
					return (short) (controller.IsPressed("0Mouse Right") ? 1 : 0);
				default:
					return 0;
			}
		}
	}

	internal class BsnesMultitapController : IBsnesController
	{
		private static readonly string[] Buttons =
		{
			"Up", "Down", "Left", "Right", "B", "A", "Y", "X", "L", "R", "Select", "Start"
		};

		private static readonly Dictionary<string, int> DisplayButtonOrder = new()
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

		private static readonly ControllerDefinition _definition = new("(SNES Controller fragment)")
		{
			BoolButtons = Enumerable.Range(0, 4)
			.SelectMany(i => Buttons.OrderBy(b => DisplayButtonOrder[b])
				.Select(b => i + b))
			.ToList()
		};

		public ControllerDefinition Definition => _definition;

		public short GetState(IController controller, int index, int id)
		{
			if (id >= 12 || index >= 4)
				return 0;

			return (short) (controller.IsPressed(index + Buttons[id]) ? 1 : 0);
		}
	}

	/// <summary>
	/// "Virtual" controller that behaves like a multitap controller, but with 16 instead of 12 buttons per controller.
	/// </summary>
	internal class BsnesPayloadController : IBsnesController
	{
		private static readonly string[] Buttons =
		{
			"Up", "Down", "Left", "Right", "B", "A", "Y", "X", "L", "R", "Select", "Start", "Extra1", "Extra2", "Extra3", "Extra4"
		};

		private static readonly Dictionary<string, int> DisplayButtonOrder = new()
		{
			["B"] = 0,
			["Y"] = 1,
			["Select"] = 2,
			["Start"] = 3,
			["Up"] = 4,
			["Down"] = 5,
			["Left"] = 6,
			["Right"] = 7,
			["A"] = 8,
			["X"] = 9,
			["L"] = 10,
			["R"] = 11,
			["Extra1"] = 12,
			["Extra2"] = 13,
			["Extra3"] = 14,
			["Extra4"] = 15
		};

		private static readonly ControllerDefinition _definition = new("(SNES Controller fragment)")
		{
			BoolButtons = Enumerable.Range(0, 4)
				.SelectMany(i => Buttons.OrderBy(b => DisplayButtonOrder[b])
					.Select(b => i + b))
				.ToList()
		};

		public ControllerDefinition Definition => _definition;

		public short GetState(IController controller, int index, int id)
		{
			if (index >= 4 || id >= 16)
				return 0;

			return (short) (controller.IsPressed(index + Buttons[id]) ? 1 : 0);
		}
	}

	internal class BsnesSuperScopeController : IBsnesController
	{
		private static readonly ControllerDefinition _definition = new ControllerDefinition("(SNES Controller fragment)")
			{ BoolButtons = { "0Trigger", "0Cursor", "0Turbo", "0Pause", "0Offscreen" } }
			.AddLightGun("0Scope {0}");

		public ControllerDefinition Definition => _definition;

		public short GetState(IController controller, int index, int id)
		{
			if (id is 0 or 1 && controller.IsPressed("0Offscreen"))
				return -1;

			return id switch
			{
				0 => (short) controller.AxisValue("0Scope X"),
				1 => (short) controller.AxisValue("0Scope Y"),
				2 or 3 or 4 or 5 => (short) (controller.IsPressed(_definition.BoolButtons[id - 2]) ? 1 : 0),
				_ => 0
			};
		}
	}

	internal class BsnesJustifierController : IBsnesController
	{
		public BsnesJustifierController(bool chained)
		{
			Definition = chained
				? new ControllerDefinition("(SNES Controller fragment)")
					{ BoolButtons = { "0Trigger", "0Start", "0Offscreen", "1Trigger", "1Start", "1Offscreen" } }
					.AddLightGun("0Justifier {0}")
					.AddLightGun("1Justifier {0}")
				: new ControllerDefinition("(SNES Controller fragment)")
					{ BoolButtons = { "0Trigger", "0Start", "0Offscreen" } }
					.AddLightGun("0Justifier {0}");
			_chained = chained;
		}

		private readonly bool _chained;

		public ControllerDefinition Definition { get; }

		public short GetState(IController controller, int index, int id)
		{
			if (index == 1 && !_chained)
				return 0;

			if (id is 0 or 1 && controller.IsPressed($"{index}Offscreen"))
				return -1;

			return id switch
			{
				0 => (short) controller.AxisValue($"{index}Justifier X"),
				1 => (short) controller.AxisValue($"{index}Justifier Y"),
				2 or 3 => (short) (controller.IsPressed(Definition.BoolButtons[index * 3 + id - 2]) ? 1 : 0),
				_ => 0
			};
		}
	}
}
