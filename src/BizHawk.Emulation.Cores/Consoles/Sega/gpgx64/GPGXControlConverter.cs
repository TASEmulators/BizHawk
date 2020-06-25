using System;
using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public class GPGXControlConverter
	{
		// this isn't all done

		struct CName
		{
			public readonly string Name;
			public readonly LibGPGX.INPUT_KEYS Key;
			public CName(string name, LibGPGX.INPUT_KEYS key)
			{
				Name = name;
				Key = key;
			}
		}

		private static readonly CName[] Genesis3 =
		{
			new CName("Up", LibGPGX.INPUT_KEYS.INPUT_UP),
			new CName("Down", LibGPGX.INPUT_KEYS.INPUT_DOWN),
			new CName("Left", LibGPGX.INPUT_KEYS.INPUT_LEFT),
			new CName("Right", LibGPGX.INPUT_KEYS.INPUT_RIGHT),
			new CName("A", LibGPGX.INPUT_KEYS.INPUT_A),
			new CName("B", LibGPGX.INPUT_KEYS.INPUT_B),
			new CName("C", LibGPGX.INPUT_KEYS.INPUT_C),
			new CName("Start", LibGPGX.INPUT_KEYS.INPUT_START),
		};

		private static readonly CName[] Genesis6 = 
		{
			new CName("Up", LibGPGX.INPUT_KEYS.INPUT_UP),
			new CName("Down", LibGPGX.INPUT_KEYS.INPUT_DOWN),
			new CName("Left", LibGPGX.INPUT_KEYS.INPUT_LEFT),
			new CName("Right", LibGPGX.INPUT_KEYS.INPUT_RIGHT),
			new CName("A", LibGPGX.INPUT_KEYS.INPUT_A),
			new CName("B", LibGPGX.INPUT_KEYS.INPUT_B),
			new CName("C", LibGPGX.INPUT_KEYS.INPUT_C),
			new CName("Start", LibGPGX.INPUT_KEYS.INPUT_START),
			new CName("X", LibGPGX.INPUT_KEYS.INPUT_X),
			new CName("Y", LibGPGX.INPUT_KEYS.INPUT_Y),
			new CName("Z", LibGPGX.INPUT_KEYS.INPUT_Z),
			new CName("Mode", LibGPGX.INPUT_KEYS.INPUT_MODE),
		};

		private static readonly CName[] Mouse =
		{
			new CName("Mouse Left", LibGPGX.INPUT_KEYS.INPUT_MOUSE_LEFT),
			new CName("Mouse Center", LibGPGX.INPUT_KEYS.INPUT_MOUSE_CENTER),
			new CName("Mouse Right", LibGPGX.INPUT_KEYS.INPUT_MOUSE_RIGHT),
			new CName("Mouse Start", LibGPGX.INPUT_KEYS.INPUT_MOUSE_START),
		};

		private static readonly CName[] Lightgun =
		{
			new CName("Lightgun Trigger", LibGPGX.INPUT_KEYS.INPUT_MENACER_TRIGGER),
			new CName("Lightgun Start", LibGPGX.INPUT_KEYS.INPUT_MENACER_START),
		};

		private static readonly CName[] Activator = 
		{
			new CName("1L", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_1L),
			new CName("1U", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_1U),
			new CName("2L", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_2L),
			new CName("2U", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_2U),
			new CName("3L", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_3L),
			new CName("3U", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_3U),
			new CName("4L", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_4L),
			new CName("4U", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_4U),
			new CName("5L", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_5L),
			new CName("5U", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_5U),
			new CName("6L", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_6L),
			new CName("6U", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_6U),
			new CName("7L", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_7L),
			new CName("7U", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_7U),
			new CName("8L", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_8L),
			new CName("8U", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_8U),
		};

		private static readonly CName[] Xea1P =
		{
			new CName("XE A", LibGPGX.INPUT_KEYS.INPUT_XE_A),
			new CName("XE B", LibGPGX.INPUT_KEYS.INPUT_XE_B),
			new CName("XE C", LibGPGX.INPUT_KEYS.INPUT_XE_C),
			new CName("XE D", LibGPGX.INPUT_KEYS.INPUT_XE_D),
			new CName("XE Start", LibGPGX.INPUT_KEYS.INPUT_XE_START),
			new CName("XE Select", LibGPGX.INPUT_KEYS.INPUT_XE_SELECT),
			new CName("XE E1", LibGPGX.INPUT_KEYS.INPUT_XE_E1),
			new CName("XE E2", LibGPGX.INPUT_KEYS.INPUT_XE_E2),
		};

		private LibGPGX.InputData _target;
		private IController _source;

		private readonly List<Action> _converts = new List<Action>();

		public ControllerDefinition ControllerDef { get; }

		void AddToController(int idx, int player, IEnumerable<CName> buttons)
		{
			foreach (var button in buttons)
			{
				string name = $"P{player} {button.Name}";
				ControllerDef.BoolButtons.Add(name);
				var buttonFlag = button.Key;
				_converts.Add(() =>
				{
					if (_source.IsPressed(name))
					{
						_target.pad[idx] |= buttonFlag;
					}
				});
			}
		}

		private void DoMouseAnalog(int idx, int player)
		{
			ControllerDef.AddXYPair($"P{player} Mouse {{0}}", AxisPairOrientation.RightAndUp, (-256).RangeTo(255), 0); //TODO verify direction against hardware
			var nx = $"P{player} Mouse X";
			var ny = $"P{player} Mouse Y";
			_converts.Add(() =>
			{
				_target.analog[(2 * idx) + 0] = (short)_source.AxisValue(nx);
				_target.analog[(2 * idx) + 1] = (short)_source.AxisValue(ny);
			});
		}

		private void DoLightgunAnalog(int idx, int player)
		{
			// lightgun needs to be transformed to match the current screen resolution
			ControllerDef.AddXYPair($"P{player} Lightgun {{0}}", AxisPairOrientation.RightAndUp, 0.RangeTo(10000), 5000); //TODO verify direction against hardware
			var nx = $"P{player} Lightgun X";
			var ny = $"P{player} Lightgun Y";
			_converts.Add(() =>
			{
				_target.analog[(2 * idx) + 0] = (short)(_source.AxisValue(nx) / 10000.0f * (ScreenWidth - 1));
				_target.analog[(2 * idx) + 1] = (short)(_source.AxisValue(ny) / 10000.0f * (ScreenHeight - 1));
			});
		}

		private void DoXea1PAnalog(int idx, int player)
		{
			ControllerDef.AddXYZTriple($"P{player} Stick {{0}}", (-128).RangeTo(127), 0);
			var nx = $"P{player} Stick X";
			var ny = $"P{player} Stick Y";
			var nz = $"P{player} Stick Z";
			_converts.Add(() =>
			{
				_target.analog[(2 * idx) + 0] = (short)_source.AxisValue(nx);
				_target.analog[(2 * idx) + 1] = (short)_source.AxisValue(ny);

				// +2 is correct in how gpgx internally does this
				_target.analog[(2 * idx) + 2] = (short)(_source.AxisValue(nz));
			});
		}

		public GPGXControlConverter(LibGPGX.InputData input, bool cdButtons)
		{
			Console.WriteLine("Genesis Controller report:");
			foreach (var e in input.system)
				Console.WriteLine("  S:{0}", e);
			foreach (var e in input.dev)
				Console.WriteLine("  D:{0}", e);

			int player = 1;

			ControllerDef = new ControllerDefinition();

			ControllerDef.BoolButtons.Add("Power");
			ControllerDef.BoolButtons.Add("Reset");
			if (cdButtons)
			{
				ControllerDef.BoolButtons.Add("Previous Disk");
				ControllerDef.BoolButtons.Add("Next Disk");
			}

			for (int i = 0; i < LibGPGX.MAX_DEVICES; i++)
			{
				switch (input.dev[i])
				{
					case LibGPGX.INPUT_DEVICE.DEVICE_PAD3B:
						AddToController(i, player, Genesis3);
						player++;
						break;
					case LibGPGX.INPUT_DEVICE.DEVICE_PAD6B:
						AddToController(i, player, Genesis6);
						player++;
						break;
					case LibGPGX.INPUT_DEVICE.DEVICE_MOUSE:
						AddToController(i, player, Mouse);
						DoMouseAnalog(i, player);
						player++;
						break;
					case LibGPGX.INPUT_DEVICE.DEVICE_NONE:
						break;
					case LibGPGX.INPUT_DEVICE.DEVICE_LIGHTGUN:
						// supports menacers and justifiers
						AddToController(i, player, Lightgun);
						DoLightgunAnalog(i, player);
						player++;
						break;
					case LibGPGX.INPUT_DEVICE.DEVICE_PAD2B:
					case LibGPGX.INPUT_DEVICE.DEVICE_PADDLE:
					case LibGPGX.INPUT_DEVICE.DEVICE_SPORTSPAD:
					case LibGPGX.INPUT_DEVICE.DEVICE_TEREBI:
						throw new Exception("Master System only device?  Something went wrong.");
					case LibGPGX.INPUT_DEVICE.DEVICE_ACTIVATOR:
						AddToController(i, player, Activator);
						player++;
						break;
					case LibGPGX.INPUT_DEVICE.DEVICE_XE_A1P:
						AddToController(i, player, Xea1P);
						DoXea1PAnalog(i, player);
						player++;
						break;
					case LibGPGX.INPUT_DEVICE.DEVICE_PICO:
						// PICO isn't finished on the unmanaged side either
						throw new Exception("Sega PICO not implemented yet!");
					default:
						throw new Exception("Unknown Genesis control device!  Something went wrong.");
				}
			}

			ControllerDef.Name = "GPGX Genesis Controller";
		}

		public void Convert(IController source, LibGPGX.InputData target)
		{
			_source = source;
			_target = target;
			target.ClearAllBools();
			foreach (var f in _converts)
			{
				f();
			}
			_source = null;
			_target = null;
		}

		/// <summary>
		/// must be set for proper lightgun operation
		/// </summary>
		public int ScreenWidth { get; set; }
		/// <summary>
		/// must be set for proper lightgun operation
		/// </summary>
		public int ScreenHeight { get; set; }
	}
}
