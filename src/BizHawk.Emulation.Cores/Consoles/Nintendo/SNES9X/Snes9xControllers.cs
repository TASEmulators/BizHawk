using System.Collections.Generic;
using System.Linq;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.SNES;

namespace BizHawk.Emulation.Cores.Nintendo.SNES9X
{
	public class Snes9xControllers
	{
		private readonly List<ControlDefUnMerger> _cdums;
		private readonly List<IControlDevice> _controllers = new();

		internal readonly short[] inputState = new short[16 * 8];

		public ControllerDefinition ControllerDefinition { get; }

		public Snes9xControllers(Snes9x.SyncSettings ss)
		{
			switch (ss.LeftPort)
			{
				case LibSnes9x.LeftPortDevice.Joypad:
					_controllers.Add(new Joypad());
					break;
				case LibSnes9x.LeftPortDevice.Multitap:
					_controllers.Add(new Joypad());
					_controllers.Add(new Joypad());
					_controllers.Add(new Joypad());
					_controllers.Add(new Joypad());
					break;
			}

			switch (ss.RightPort)
			{
				case LibSnes9x.RightPortDevice.Joypad:
					_controllers.Add(new Joypad());
					break;
				case LibSnes9x.RightPortDevice.Multitap:
					_controllers.Add(new Joypad());
					_controllers.Add(new Joypad());
					_controllers.Add(new Joypad());
					_controllers.Add(new Joypad());
					break;
				case LibSnes9x.RightPortDevice.Mouse:
					_controllers.Add(new Mouse());
					break;
				case LibSnes9x.RightPortDevice.SuperScope:
					_controllers.Add(new SuperScope());
					break;
				case LibSnes9x.RightPortDevice.Justifier:
					_controllers.Add(new Justifier());
					break;
			}

			ControllerDefinition = ControllerDefinitionMerger.GetMerged(
				"SNES Controller",
				_controllers.Select(c => c.Definition),
				out _cdums);

			// add buttons that the core itself will handle
			ControllerDefinition.BoolButtons.Add("Reset");
			ControllerDefinition.BoolButtons.Add("Power");

			ControllerDefinition.MakeImmutable();
		}

		internal void UpdateControls(IController c)
		{
			Array.Clear(inputState, 0, 16 * 8);
			for (int i = 0, offset = 0; i < _controllers.Count; i++, offset += 16)
			{
				_controllers[i]
					.ApplyState(_cdums[i]
						.UnMerge(c), inputState, offset);
			}
		}

		private interface IControlDevice
		{
			ControllerDefinition Definition { get; }
			void ApplyState(IController controller, short[] input, int offset);
		}

		private class Joypad : IControlDevice
		{
			private static readonly string[] Buttons =
			{
				"0B", "0Y", "0Select", "0Start", "0Up", "0Down", "0Left", "0Right", "0A", "0X", "0L", "0R"
			};

			private static int ButtonOrder(string btn)
			{
				var order = new Dictionary<string, int>
				{
					["0Up"] = 0, ["0Down"] = 1, ["0Left"] = 2, ["0Right"] = 3, ["0Select"] = 4, ["0Start"] = 5, ["0Y"] = 6, ["0B"] = 7, ["0X"] = 8, ["0A"] = 9
					, ["0L"] = 10, ["0R"] = 11
				};

				return order[btn];
			}

			private static readonly ControllerDefinition _definition = new("(SNES Controller fragment)")
			{
				BoolButtons = Buttons.OrderBy(ButtonOrder)
					.ToList()
			};

			public ControllerDefinition Definition { get; } = _definition;

			public void ApplyState(IController controller, short[] input, int offset)
			{
				for (int i = 0; i < Buttons.Length; i++)
					input[offset + i] = (short)(controller.IsPressed(Buttons[i]) ? 1 : 0);
			}
		}

		private abstract class Analog : IControlDevice
		{
			public abstract ControllerDefinition Definition { get; }

			public void ApplyState(IController controller, short[] input, int offset)
			{
				foreach (var s in Definition.Axes.Keys)
					input[offset++] = (short)controller.AxisValue(s);
				foreach (var s in Definition.BoolButtons)
					input[offset++] = (short)(controller.IsPressed(s) ? 1 : 0);
			}
		}

		private class Mouse : Analog
		{
			private static readonly ControllerDefinition _definition
				= new ControllerDefinition("(SNES Controller fragment)") { BoolButtons = { "0Mouse Left", "0Mouse Right" } }
					.AddXYPair("0Mouse {0}", AxisPairOrientation.RightAndDown, (-127).RangeTo(127), 0); //TODO verify direction against hardware

			public override ControllerDefinition Definition => _definition;
		}

		private class SuperScope : Analog
		{
			private static readonly ControllerDefinition _definition
				= new ControllerDefinition("(SNES Controller fragment)") { BoolButtons = { "0Trigger", "0Cursor", "0Turbo", "0Pause", "0Offscreen" } }
					.AddLightGun("0Scope {0}");

			public override ControllerDefinition Definition => _definition;
		}

		private class Justifier : Analog
		{
			private static readonly ControllerDefinition _definition
				= new ControllerDefinition("(SNES Controller fragment)") { BoolButtons = { "0Trigger", "0Start", "0Offscreen" } }
					.AddLightGun("0Justifier {0}");

			public override ControllerDefinition Definition => _definition;
		}
	}
}
