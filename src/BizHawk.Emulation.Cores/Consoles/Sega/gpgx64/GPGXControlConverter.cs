using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public class GPGXControlConverter
	{
		// this isn't all done

		private readonly struct CName(string name, LibGPGX.INPUT_KEYS key)
		{
			public readonly string Name = name;
			public readonly LibGPGX.INPUT_KEYS Key = key;
		}

		private static readonly CName[] SMS2B =
		[
			new("Up", LibGPGX.INPUT_KEYS.INPUT_UP),
			new("Down", LibGPGX.INPUT_KEYS.INPUT_DOWN),
			new("Left", LibGPGX.INPUT_KEYS.INPUT_LEFT),
			new("Right", LibGPGX.INPUT_KEYS.INPUT_RIGHT),
			new("B1", LibGPGX.INPUT_KEYS.INPUT_BUTTON1),
			new("B2", LibGPGX.INPUT_KEYS.INPUT_BUTTON2),
		];

		private static readonly CName[] GameGear =
		[
			new("Up", LibGPGX.INPUT_KEYS.INPUT_UP),
			new("Down", LibGPGX.INPUT_KEYS.INPUT_DOWN),
			new("Left", LibGPGX.INPUT_KEYS.INPUT_LEFT),
			new("Right", LibGPGX.INPUT_KEYS.INPUT_RIGHT),
			new("B1", LibGPGX.INPUT_KEYS.INPUT_BUTTON1),
			new("B2", LibGPGX.INPUT_KEYS.INPUT_BUTTON2),
			new("Start", LibGPGX.INPUT_KEYS.INPUT_START),
		];

		private static readonly CName[] Genesis3 =
		[
			new("Up", LibGPGX.INPUT_KEYS.INPUT_UP),
			new("Down", LibGPGX.INPUT_KEYS.INPUT_DOWN),
			new("Left", LibGPGX.INPUT_KEYS.INPUT_LEFT),
			new("Right", LibGPGX.INPUT_KEYS.INPUT_RIGHT),
			new("A", LibGPGX.INPUT_KEYS.INPUT_A),
			new("B", LibGPGX.INPUT_KEYS.INPUT_B),
			new("C", LibGPGX.INPUT_KEYS.INPUT_C),
			new("Start", LibGPGX.INPUT_KEYS.INPUT_START),
		];

		private static readonly CName[] Genesis6 = 
		[
			new("Up", LibGPGX.INPUT_KEYS.INPUT_UP),
			new("Down", LibGPGX.INPUT_KEYS.INPUT_DOWN),
			new("Left", LibGPGX.INPUT_KEYS.INPUT_LEFT),
			new("Right", LibGPGX.INPUT_KEYS.INPUT_RIGHT),
			new("A", LibGPGX.INPUT_KEYS.INPUT_A),
			new("B", LibGPGX.INPUT_KEYS.INPUT_B),
			new("C", LibGPGX.INPUT_KEYS.INPUT_C),
			new("Start", LibGPGX.INPUT_KEYS.INPUT_START),
			new("X", LibGPGX.INPUT_KEYS.INPUT_X),
			new("Y", LibGPGX.INPUT_KEYS.INPUT_Y),
			new("Z", LibGPGX.INPUT_KEYS.INPUT_Z),
			new("Mode", LibGPGX.INPUT_KEYS.INPUT_MODE),
		];

		private static readonly CName[] Mouse =
		[
			new("Mouse Left", LibGPGX.INPUT_KEYS.INPUT_MOUSE_LEFT),
			new("Mouse Center", LibGPGX.INPUT_KEYS.INPUT_MOUSE_CENTER),
			new("Mouse Right", LibGPGX.INPUT_KEYS.INPUT_MOUSE_RIGHT),
			new("Mouse Start", LibGPGX.INPUT_KEYS.INPUT_MOUSE_START),
		];

		private static readonly CName[] Lightgun =
		[
			new("Lightgun Trigger", LibGPGX.INPUT_KEYS.INPUT_MENACER_TRIGGER),
			new("Lightgun Start", LibGPGX.INPUT_KEYS.INPUT_MENACER_START),
			new("Lightgun B", LibGPGX.INPUT_KEYS.INPUT_MENACER_B),
			new("Lightgun C", LibGPGX.INPUT_KEYS.INPUT_MENACER_C),
			new("Lightgun Offscreen Shot", LibGPGX.INPUT_KEYS.INPUT_MENACER_TRIGGER),
		];

		private static readonly CName[] Activator = 
		[
			new("1L", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_1L),
			new("1U", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_1U),
			new("2L", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_2L),
			new("2U", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_2U),
			new("3L", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_3L),
			new("3U", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_3U),
			new("4L", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_4L),
			new("4U", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_4U),
			new("5L", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_5L),
			new("5U", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_5U),
			new("6L", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_6L),
			new("6U", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_6U),
			new("7L", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_7L),
			new("7U", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_7U),
			new("8L", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_8L),
			new("8U", LibGPGX.INPUT_KEYS.INPUT_ACTIVATOR_8U),
		];

		private static readonly CName[] Xea1P =
		[
			new("XE A", LibGPGX.INPUT_KEYS.INPUT_XE_A),
			new("XE B", LibGPGX.INPUT_KEYS.INPUT_XE_B),
			new("XE C", LibGPGX.INPUT_KEYS.INPUT_XE_C),
			new("XE D", LibGPGX.INPUT_KEYS.INPUT_XE_D),
			new("XE Start", LibGPGX.INPUT_KEYS.INPUT_XE_START),
			new("XE Select", LibGPGX.INPUT_KEYS.INPUT_XE_SELECT),
			new("XE E1", LibGPGX.INPUT_KEYS.INPUT_XE_E1),
			new("XE E2", LibGPGX.INPUT_KEYS.INPUT_XE_E2),
		];

		private static readonly CName[] Paddle =
		[
			new("B1", LibGPGX.INPUT_KEYS.INPUT_BUTTON1)
		];

		private LibGPGX.InputData _target;
		private IController _source;

		private readonly List<Action> _converts = [ ];

		public ControllerDefinition ControllerDef { get; }

		private void AddToController(int idx, int player, IEnumerable<CName> buttons)
		{
			foreach (var button in buttons)
			{
				var name = $"P{player} {button.Name}";
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
			// In "Genesis Technical Bulletin #27" (last seen at http://techdocs.exodusemulator.com/Console/SegaMegaDrive/Documentation.html), some example code for the Genesis is given which describes the 32 bits of Mouse data:
			// ` ignored YXYX     XXXXXXXX YYYYYYYY`
			// `0-------_oossCMRL_########_########`
			// Each axis is represented as 10 bits: 1 `s` bit for sign, 8 bits for the value, and 1 `o` bit indicating whether the value fell outside the range (i.e. abs(val)>=256).
			// So the range -256..256 includes every normal state, though nothing outside -10..10 is at all useful based on my in-game testing. (Games probably didn't have special checks for -0 or for the overflow bit being used with a value <=255.)
			// The game in question is Eye of the Beholder, you can FFW to the main menu and get a cursor right away.
			// --yoshi
			ControllerDef.AddXYPair($"P{player} Mouse {{0}}", AxisPairOrientation.RightAndUp, (-256).RangeTo(256), 0);
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
			var no = $"P{player} Lightgun Offscreen Shot";
			var nx = $"P{player} Lightgun X";
			var ny = $"P{player} Lightgun Y";
			_converts.Add(() =>
			{
				if (_source.IsPressed(no))
				{
					_target.analog[(2 * idx) + 0] = 512;
					_target.analog[(2 * idx) + 1] = 512;
				}
				else
				{
					_target.analog[(2 * idx) + 0] = (short)(_source.AxisValue(nx) / 10000.0f * (ScreenWidth - 1));
					_target.analog[(2 * idx) + 1] = (short)(_source.AxisValue(ny) / 10000.0f * (ScreenHeight - 1));
				}
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

		private void DoPaddleAnalog(int idx, int player)
		{
			ControllerDef.AddAxis($"P{player} Paddle", 0.RangeTo(255), 128);

			_converts.Add(() =>
			{
				_target.analog[2 * idx] = (byte)_source.AxisValue($"P{player} Paddle");
			});
		}

		public GPGXControlConverter(LibGPGX.InputData input, string systemId, bool cdButtons)
		{
			Console.WriteLine("GPGX Controller report:");
			foreach (var e in input.system)
				Console.WriteLine("  S:{0}", e);
			foreach (var e in input.dev)
				Console.WriteLine("  D:{0}", e);

			var player = 1;

			ControllerDef = new(systemId switch
			{
				VSystemID.Raw.SMS or VSystemID.Raw.SG => "SMS Controller",
				VSystemID.Raw.GG => "GG Controller",
				VSystemID.Raw.GEN => "GPGX Genesis Controller", // GPGX in controller def name is more for backwards compat sake
				_ => throw new InvalidOperationException(),
			});

			ControllerDef.BoolButtons.Add("Power");
			ControllerDef.BoolButtons.Add("Reset");

			if (systemId is VSystemID.Raw.SMS or VSystemID.Raw.SG)
			{
				ControllerDef.BoolButtons.Add("Pause");
			}

			if (cdButtons)
			{
				ControllerDef.BoolButtons.Add("Previous Disk");
				ControllerDef.BoolButtons.Add("Next Disk");
			}

			for (var i = 0; i < LibGPGX.MAX_DEVICES; i++)
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
						AddToController(i, player, systemId is VSystemID.Raw.SMS ? SMS2B : GameGear);
						player++;
						break;
					case LibGPGX.INPUT_DEVICE.DEVICE_PADDLE:
						AddToController(i, player, Paddle);
						DoPaddleAnalog(i, player);
						player++;
						break;
					case LibGPGX.INPUT_DEVICE.DEVICE_SPORTSPAD:
					case LibGPGX.INPUT_DEVICE.DEVICE_TEREBI:
						throw new NotImplementedException();
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
						throw new Exception("Unknown GPGX control device!  Something went wrong.");
				}
			}

			ControllerDef.MakeImmutable();
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
