using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BizHawk.Emulation.Common.ControllerDefinition;

namespace BizHawk.Emulation.Cores.Consoles.Sega.Saturn
{
	public class SaturnusControllerDeck
	{
		private const int DataSize = 32;

		private static readonly Type[] Implementors =
		{
			typeof(None),
			typeof(Gamepad),
			typeof(ThreeDeeGamepad),
			typeof(Mouse),
			typeof(Wheel),
			typeof(Mission),
			typeof(DualMission),
			typeof(Keyboard)
		};

		private readonly IDevice[] _devices;
		private readonly ControlDefUnMerger[] _unmerger;
		private readonly byte[] _data;

		public ControllerDefinition Definition { get; }

		public SaturnusControllerDeck(bool[] multitap, Device[] devices, LibSaturnus core)
		{
			int count = 2 + multitap.Count(b => b) * 5;

			int[] dev = new int[12];
			int[] mt = new int[2];

			for (int i = 0; i < 12; i++)
				dev[i] = (int)(i < count ? devices[i] : Device.None);
			for (int i = 0; i < 2; i++)
				mt[i] = multitap[i] ? 1 : 0;

			core.SetupInput(dev, mt);

			_devices = dev.Take(count)
				.Select(i => Activator.CreateInstance(Implementors[i]))
				.Cast<IDevice>()
				.ToArray();
			_data = new byte[count * DataSize];

			List<ControlDefUnMerger> cdum;
			Definition = ControllerDefinitionMerger.GetMerged(_devices.Select(d => d.Definition),
				out cdum);
			_unmerger = cdum.ToArray();
		}

		public byte[] Poll(IController controller)
		{
			for (int i = 0, offset = 0; i < _devices.Length; i++, offset += DataSize)
				_devices[i].Update(_unmerger[i].UnMerge(controller), _data, offset);
			return _data;
		}

		public enum Device
		{
			None,
			Gamepad,
			[Display(Name = "3D Pad")]
			ThreeDeePad,
			Mouse,
			[Display(Name = "Racing Controller")]
			Wheel,
			[Display(Name = "Mission Stick")]
			Mission,
			[Display(Name = "Two Mission Sticks")]
			DualMission,
			Keyboard
		}

		private interface IDevice
		{
			void Update(IController controller, byte[] dest, int offset);
			ControllerDefinition Definition { get; }
		}

		private class None : IDevice
		{
			private static readonly ControllerDefinition NoneDefition = new ControllerDefinition();
			public ControllerDefinition Definition => NoneDefition;
			public void Update(IController controller, byte[] dest, int offset)
			{
			}

		}

		private abstract class ButtonedDevice : IDevice
		{
			private static readonly FloatRange AnalogFloatRange = new FloatRange(0, 128, 255);

			protected ButtonedDevice()
			{
				_bakedButtonNames = ButtonNames.Select(s => s != null ? "0" + s : null).ToArray();
				_bakedAnalogNames = AnalogNames.Select(s => "0" + s).ToArray();

				Definition = new ControllerDefinition
				{
					BoolButtons = _bakedButtonNames
						.Select((s, i) => new { s, i })
						.Where(a => a.s != null)
						.OrderBy(a => ButtonOrdinal(ButtonNames[a.i]))
						.Select(a => a.s)
						.ToList(),
				};
				Definition.FloatControls.AddRange(_bakedAnalogNames
					.Select((s, i) => new { s, i })
					.OrderBy(a => AnalogOrdinal(AnalogNames[a.i]))
					.Select(a => a.s));
				Definition.FloatRanges.AddRange(_bakedAnalogNames.Select(s => AnalogFloatRange));
			}

			private readonly string[] _bakedButtonNames;
			private readonly string[] _bakedAnalogNames;

			protected virtual string[] ButtonNames { get; } = new string[0];
			protected virtual int ButtonByteOffset { get; } = 0;
			protected virtual string[] AnalogNames { get; } = new string[0];
			protected virtual int AnalogByteOffset => (ButtonNames.Length + 7) / 8;
			public ControllerDefinition Definition { get; }

			protected virtual int ButtonOrdinal(string name)
			{
				return 0;
			}
			protected virtual int AnalogOrdinal(string name)
			{
				return 0;
			}

			private void UpdateButtons(IController controller, byte[] dest, int offset)
			{
				int pos = offset + ButtonByteOffset;
				byte data = 0;
				int bit = 0;
				for (int i = 0; i < _bakedButtonNames.Length; i++)
				{
					if (_bakedButtonNames[i] != null && controller.IsPressed(_bakedButtonNames[i]))
						data |= (byte)(1 << bit);
					if (++bit == 8)
					{
						bit = 0;
						dest[pos++] = data;
						data = 0;
					}
				}
				if (bit != 0)
					dest[pos] = data;
			}

			private void UpdateAnalogs(IController controller, byte[] dest, int offset)
			{
				int pos = offset + AnalogByteOffset;
				for (int i = 0; i < _bakedAnalogNames.Length; i++)
				{
					var data = (byte)(int)controller.GetFloat(_bakedAnalogNames[i]);
					dest[pos++] = data;
				}
			}


			public virtual void Update(IController controller, byte[] dest, int offset)
			{
				UpdateButtons(controller, dest, offset);
				UpdateAnalogs(controller, dest, offset);
			}
		}

		private class Gamepad : ButtonedDevice
		{
			private static readonly string[] _buttonNames =
			{
				"Z", "Y", "X", "R",
				"Up", "Down", "Left", "Right",
				"B", "C", "A", "Start",
				null, null, null, "L"
			};

			protected override string[] ButtonNames => _buttonNames;

			protected override int ButtonOrdinal(string name)
			{
				switch (name)
				{
					default:
						return 0;
					case "A":
						return 1;
					case "B":
					case "C":
						return 2;
					case "X":
						return 3;
					case "Y":
						return 4;
					case "Z":
					case "L":
						return 5;
					case "R":
						return 6;
				}
			}
		}

		private class ThreeDeeGamepad : ButtonedDevice
		{
			private static readonly string[] _buttonNames =
			{
				"Up", "Down", "Left", "Right",
				"B", "C", "A", "Start",
				"Z", "Y", "X"
			};

			protected override string[] ButtonNames => _buttonNames;

			private static readonly string[] _analogNames =
			{
				"Stick Horizontal",
				"Stick Vertical",
				"Left Shoulder",
				"Right Shoulder"
			};

			protected override string[] AnalogNames => _analogNames;

			public ThreeDeeGamepad()
			{
				Definition.FloatRanges[2] = new FloatRange(0, 0, 255);
				Definition.FloatRanges[3] = new FloatRange(0, 0, 255);
			}

			public override void Update(IController controller, byte[] dest, int offset)
			{
				base.Update(controller, dest, offset);
				// set the "Mode" button to analog at all times
				dest[offset + 1] |= 0x10;
			}

			protected override int ButtonOrdinal(string name)
			{
				switch (name)
				{
					default:
						return 0;
					case "A":
						return 1;
					case "B":
					case "C":
						return 2;
					case "X":
						return 3;
					case "Y":
						return 4;
					case "Z":
						return 5;
				}
			}
		}

		private class Mouse : ButtonedDevice
		{
			private static readonly string[] _buttonNames =
			{
				"Mouse Left", "Mouse Right", "Mouse Center", "Start"
			};

			protected override string[] ButtonNames => _buttonNames;

			private static readonly string[] _analogNames =
			{
				"X", "Y"
			};

			protected override string[] AnalogNames => _analogNames;

			protected override int ButtonOrdinal(string name)
			{
				switch (name)
				{
					default:
					case "Mouse Left":
						return 0;
					case "Mouse Center":
						return 1;
					case "Mouse Right":
						return 2;
					case "Start":
						return 3;
				}
			}
		}

		private class Wheel : ButtonedDevice
		{
			private static readonly string[] _buttonNames =
			{
				"Up", "Down", null, null,
				"B", "C", "A", "Start",
				"Z", "Y", "X"
			};

			protected override string[] ButtonNames => _buttonNames;

			private static readonly string[] _analogNames =
			{
				"Wheel"
			};

			protected override string[] AnalogNames => _analogNames;

			protected override int ButtonOrdinal(string name)
			{
				switch (name)
				{
					default:
						return 0;
					case "A":
						return 1;
					case "B":
					case "C":
						return 2;
					case "X":
						return 3;
					case "Y":
						return 4;
					case "Z":
						return 5;
				}
			}
		}

		private class Mission : ButtonedDevice
		{
			private static readonly string[] _buttonNames =
			{
				"B", "C", "A", "Start",
				"Z", "Y", "X", "R",
				null, null, null, "L"
			};

			protected override string[] ButtonNames => _buttonNames;

			private static readonly string[] _analogNames =
			{
				"Stick Horizontal",
				"Stick Vertical",
				"Throttle"
			};

			protected override string[] AnalogNames => _analogNames;
			protected override int AnalogByteOffset => 4;

			protected override int ButtonOrdinal(string name)
			{
				switch (name)
				{
					default:
						return 0;
					case "Start":
						return 1;
					case "A":
						return 2;
					case "B":
					case "C":
						return 3;
					case "X":
						return 4;
					case "Y":
						return 5;
					case "Z":
					case "L":
						return 6;
					case "R":
						return 7;
				}
			}
		}

		private class DualMission : Mission
		{
			private static readonly string[] _analogNames =
			{
				"Right Stick Horizontal",
				"Right Stick Vertical",
				"Right Throttle",
				"Left Stick Horizontal",
				"Left Stick Vertical",
				"Left Throttle"
			};

			protected override string[] AnalogNames => _analogNames;
		}

		private class Keyboard : ButtonedDevice
		{
			// TODO: LEDs, which are actually data sent back by the core
			private static readonly string[] _buttonNames =
			{
				null,
				"F9",
				null,
				"F5",
				"F3",
				"F1",
				"F2",
				"F12",
				null,
				"F10",
				"F8",
				"F6",
				"F4",
				"Tab",
				"Grave`",
				null,
				null,
				"LeftAlt",
				"LeftShift",
				null,
				"LeftCtrl",
				"Q",
				"1(One)",
				"RightAlt",
				"RightCtrl",
				"KeypadEnter",
				"Z(Key)",
				"S",
				"A(Key)",
				"W",
				"2",
				null,
				null,
				"C(Key)",
				"X(Key)",
				"D",
				"E",
				"4",
				"3",
				null,
				null,
				"Space",
				"V",
				"F",
				"T",
				"R(Key)",
				"5",
				null,
				null,
				"N",
				"B(Key)",
				"H",
				"G",
				"Y(Key)",
				"6",
				null,
				null,
				null,
				"M",
				"J",
				"U",
				"7",
				"8",
				null,
				null,
				"Comma,",
				"K",
				"I",
				"O",
				"0(Zero)",
				"9",
				null,
				null,
				"Period.",
				"Slash/",
				"L(Key)",
				"Semicolon;",
				"P",
				"Minus-",
				null,
				null,
				null,
				"Quote'",
				null,
				"LeftBracket[",
				"Equals=",
				null,
				null,
				"CapsLock",
				"RightShift",
				"Enter",
				"RightBracket]",
				null,
				"Backslash\\",
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				"Backspace",
				null,
				null,
				"KeypadEnd/1",
				null,
				"KeypadLeft/4",
				"KeypadHome/7",
				null,
				null,
				null,
				"KeypadInsert/0",
				"KeypadDelete",
				"KeypadDown/2",
				"KeypadCenter/5",
				"KeypadRight/6",
				"KeypadUp/8",
				"Escape",
				"NumLock",
				"F11",
				"KeypadPlus",
				"KeypadPagedown/3",
				"KeypadMinus",
				"KeypadAsterisk(Multiply)",
				"KeypadPageup/9",
				"ScrollLock",
				null,
				"KeypadSlash(Divide)",
				"Insert",
				"Pause",
				"F7",
				"PrintScreen",
				"Delete",
				"CursorLeft",
				"Home",
				"End",
				"Up",
				"Down",
				"PageUp",
				"PageDown",
				"Right",
			};

			protected override string[] ButtonNames => _buttonNames;

			protected override int ButtonOrdinal(string name)
			{
				switch (name)
				{
					default: return 0;
					case "Escape": return 1;
					case "F1": return 2;
					case "F2": return 3;
					case "F3": return 4;
					case "F4": return 5;
					case "F5": return 6;
					case "F6": return 7;
					case "F7": return 8;
					case "F8": return 9;
					case "F9": return 10;
					case "F10": return 11;
					case "F11": return 12;
					case "F12": return 13;

					case "Grave`": return 100;
					case "1(One)": return 101;
					case "2": return 102;
					case "3": return 103;
					case "4": return 104;
					case "5": return 105;
					case "6": return 106;
					case "7": return 107;
					case "8": return 108;
					case "9": return 109;
					case "0(Zero)": return 110;
					case "Minus-": return 111;
					case "Equals=": return 112;
					case "Backslash\\": return 113;
					case "Backspace": return 114;

					case "Tab": return 200;
					case "Q": return 201;
					case "W": return 202;
					case "E": return 203;
					case "R(Key)": return 204;
					case "T": return 205;
					case "Y(Key)": return 206;
					case "U": return 207;
					case "I": return 208;
					case "O": return 209;
					case "P": return 210;
					case "LeftBracket[": return 211;
					case "RightBracket]": return 212;
					case "Enter": return 213;

					case "CapsLock": return 300;
					case "A(Key)": return 301;
					case "S": return 302;
					case "D": return 303;
					case "F": return 304;
					case "G": return 305;
					case "H": return 306;
					case "J": return 307;
					case "K": return 308;
					case "L(Key)": return 309;
					case "Semicolon;": return 310;
					case "Quote'": return 311;

					case "LeftShift": return 400;
					case "Z(Key)": return 401;
					case "X(Key)": return 402;
					case "C(Key)": return 403;
					case "V": return 404;
					case "B(Key)": return 405;
					case "N": return 406;
					case "M": return 407;
					case "Comma,": return 408;
					case "Period.": return 409;
					case "Slash/": return 410;
					case "RightShift": return 411;

					case "LeftCtrl": return 500;
					case "LeftAlt": return 501;
					case "Space": return 502;
					case "RightAlt": return 503;
					case "RightCtrl": return 504;

					case "PrintScreen": return 1000;
					case "ScrollLock": return 1001;
					case "Pause": return 1002;

					case "Insert": return 1100;
					case "Delete": return 1101;
					case "Home": return 1102;
					case "End": return 1103;
					case "PageUp": return 1104;
					case "PageDown": return 1105;

					case "Up": return 1200;
					case "Down": return 1201;
					case "CursorLeft": return 1202;
					case "Right": return 1203;

					case "NumLock": return 1300;
					case "KeypadSlash(Divide)": return 1301;
					case "KeypadAsterisk(Multiply)": return 1302;
					case "KeypadMinus": return 1303;
					case "KeypadHome/7": return 1304;
					case "KeypadUp/8": return 1305;
					case "KeypadPageup/9": return 1306;
					case "KeypadPlus": return 1307;
					case "KeypadLeft/4": return 1308;
					case "KeypadCenter/5": return 1309;
					case "KeypadRight/6": return 1310;
					case "KeypadEnd/1": return 1311;
					case "KeypadDown/2": return 1312;
					case "KeypadPagedown/3": return 1313;
					case "KeypadEnter": return 1314;
					case "KeypadInsert/0": return 1315;
					case "KeypadDelete": return 1316;
				}
			}
		}
	}
}
