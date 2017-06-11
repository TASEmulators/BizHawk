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
			private static readonly FloatRange AnalogFloatRange = new FloatRange(-32767, 0, 32767);

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
					var data = (int)controller.GetFloat(_bakedAnalogNames[i]);
					var datal = (short)Math.Max(data, 0);
					var datar = (short)Math.Max(-data, 0);
					dest[pos++] = (byte)datal;
					dest[pos++] = (byte)(datal >> 8);
					dest[pos++] = (byte)datar;
					dest[pos++] = (byte)(datar >> 8);
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
				"Third Axis"
			};

			protected override string[] AnalogNames => _analogNames;

			public override void Update(IController controller, byte[] dest, int offset)
			{
				base.Update(controller, dest, offset);
				// set the "Mode" button to analog at all times
				dest[offset + 1] |= 0x10;
			}
		}

		private class Mouse : ButtonedDevice
		{
			private static readonly FloatRange MouseFloatRange = new FloatRange(-32768, 0, 32767);

			private static readonly string[] _buttonNames =
			{
				"Left", "Right", "Middle", "Start"
			};

			protected override string[] ButtonNames => _buttonNames;
			protected override int ButtonByteOffset => 8;

			public Mouse()
			{
				Definition.FloatControls.AddRange(new[] { "0X", "0Y" });
				Definition.FloatRanges.AddRange(new[] { MouseFloatRange, MouseFloatRange });
			}

			private void SetMouseAxis(float value, byte[] dest, int offset)
			{
				var data = (short)value;
				dest[offset++] = 0;
				dest[offset++] = 0;
				dest[offset++] = (byte)data;
				dest[offset++] = (byte)(data >> 8);
			}

			public override void Update(IController controller, byte[] dest, int offset)
			{
				base.Update(controller, dest, offset);
				SetMouseAxis(controller.GetFloat("0X"), dest, offset + 0);
				SetMouseAxis(controller.GetFloat("0Y"), dest, offset + 4);
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
			protected override int AnalogByteOffset => 3;
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
				"Z",
				"S",
				"A",
				"W",
				"2",
				null,
				null,
				"C",
				"X",
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
				"R",
				"5",
				null,
				null,
				"N",
				"B",
				"H",
				"G",
				"Y",
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
				"L",
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
		}
	}
}
