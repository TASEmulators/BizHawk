using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Amiga
{
	public partial class PUAE
	{
		private LibPUAE.ControllerType[] _ports { get; set; }
		private static readonly (string Name, LibPUAE.AllButtons Button)[] _joystickMap = CreateJoystickMap();
		private static readonly (string Name, LibPUAE.AllButtons Button)[] _cd32padMap = CreateCd32padMap();
		private static readonly (string Name, LibPUAE.PUAEKeyboard Key)[] _keyboardMap = CreateKeyboardMap();

		private static (string Name, LibPUAE.AllButtons Value)[] CreateJoystickMap()
		{
			var joystickMap = new List<(string, LibPUAE.AllButtons)>();
			// ReSharper disable once LoopCanBeConvertedToQuery
			foreach (var b in Enum.GetValues(typeof(LibPUAE.AllButtons)))
			{
				if (((short)b & LibPUAE.JoystickMask) == 0)
					continue;

				var name = Enum.GetName(typeof(LibPUAE.AllButtons), b)!.Replace('_', ' ');
				joystickMap.Add((name, (LibPUAE.AllButtons)b));
			}
			return joystickMap.ToArray();
		}

		private static (string Name, LibPUAE.AllButtons Value)[] CreateCd32padMap()
		{
			var joystickMap = new List<(string, LibPUAE.AllButtons)>();
			// ReSharper disable once LoopCanBeConvertedToQuery
			foreach (var b in Enum.GetValues(typeof(LibPUAE.AllButtons)))
			{
				if (((short)b & LibPUAE.Cd32padMask) == 0)
					continue;

				var name = Enum.GetName(typeof(LibPUAE.AllButtons), b)!.Replace('_', ' ');
				joystickMap.Add((name, (LibPUAE.AllButtons)b));
			}
			return joystickMap.ToArray();
		}

		private static (string Name, LibPUAE.PUAEKeyboard Value)[] CreateKeyboardMap()
		{
			var keyboardMap = new List<(string, LibPUAE.PUAEKeyboard)>();
			// ReSharper disable once LoopCanBeConvertedToQuery
			foreach (var k in Enum.GetValues(typeof(LibPUAE.PUAEKeyboard)))
			{
				var name = Enum.GetName(typeof(LibPUAE.PUAEKeyboard), k)!.Replace('_', ' ');
				keyboardMap.Add((name, (LibPUAE.PUAEKeyboard)k));
			}

			return keyboardMap.ToArray();
		}

		private static ControllerDefinition CreateControllerDefinition(PUAESyncSettings settings)
		{
			var controller = new ControllerDefinition("Amiga Controller");

			for (int port = 1; port <= 2; port++)
			{
				LibPUAE.ControllerType type = port == 1
					? settings.ControllerPort1
					: settings.ControllerPort2;

				switch (type)
				{
					case LibPUAE.ControllerType.Joystick:
						{
							foreach (var (name, _) in _joystickMap)
							{
								controller.BoolButtons.Add($"P{port} {Inputs.Joystick} {name}");
							}
							break;
						}
					case LibPUAE.ControllerType.CD32_pad:
						{
							foreach (var (name, _) in _cd32padMap)
							{
								controller.BoolButtons.Add($"P{port} {Inputs.Cd32Pad} {name}");
							}
							break;
						}
					case LibPUAE.ControllerType.Mouse:
						{
							controller.BoolButtons.AddRange(
							[
								$"P{port} {Inputs.MouseLeftButton}",
								$"P{port} {Inputs.MouseMiddleButton}",
								$"P{port} {Inputs.MouseRightButton}"
							]);
							controller
								.AddAxis($"P{port} {Inputs.MouseX}", 0.RangeTo(LibPUAE.PAL_WIDTH), LibPUAE.PAL_WIDTH / 2)
								.AddAxis($"P{port} {Inputs.MouseY}", 0.RangeTo(LibPUAE.PAL_HEIGHT), LibPUAE.PAL_HEIGHT / 2);
							break;
						}
				}
			}

			controller.BoolButtons.AddRange(
			[
				Inputs.NextDrive, Inputs.NextSlot, Inputs.InsertDisk, Inputs.EjectDisk
			]);

			foreach (var (name, _) in _keyboardMap)
			{
				controller.BoolButtons.Add(name);
				controller.CategoryLabels[name] = "Keyboard";
			}

			return controller.MakeImmutable();
		}

		private static class Inputs
		{
			public const string Joystick = "Joystick";
			public const string Cd32Pad = "CD32 pad";
			public const string MouseLeftButton = "Mouse Left Button";
			public const string MouseRightButton = "Mouse Right Button";
			public const string MouseMiddleButton = "Mouse Middle Button";
			public const string MouseX = "Mouse X";
			public const string MouseY = "Mouse Y";
			public const string EjectDisk = "Eject Disk";
			public const string InsertDisk = "Insert Disk";
			public const string NextDrive = "Next Drive";
			public const string NextSlot = "Next Slot";
		}
	}
}
