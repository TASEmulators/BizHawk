using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA
	{
		private LibDSDA.ControllerType[] _ports { get; set; }
		private static readonly (string Name, LibDSDA.AllButtons Button)[] _joystickMap = CreateJoystickMap();
		private static readonly (string Name, LibDSDA.AllButtons Button)[] _cd32padMap = CreateCd32padMap();
		private static readonly (string Name, LibDSDA.DSDAKeyboard Key)[] _keyboardMap = CreateKeyboardMap();

		private static (string Name, LibDSDA.AllButtons Value)[] CreateJoystickMap()
		{
			var joystickMap = new List<(string, LibDSDA.AllButtons)>();
			// ReSharper disable once LoopCanBeConvertedToQuery
			foreach (var b in Enum.GetValues(typeof(LibDSDA.AllButtons)))
			{
				if (((short)b & LibDSDA.JoystickMask) == 0)
					continue;

				var name = Enum.GetName(typeof(LibDSDA.AllButtons), b)!.Replace('_', ' ');
				joystickMap.Add((name, (LibDSDA.AllButtons)b));
			}
			return joystickMap.ToArray();
		}

		private static (string Name, LibDSDA.AllButtons Value)[] CreateCd32padMap()
		{
			var joystickMap = new List<(string, LibDSDA.AllButtons)>();
			// ReSharper disable once LoopCanBeConvertedToQuery
			foreach (var b in Enum.GetValues(typeof(LibDSDA.AllButtons)))
			{
				if (((short)b & LibDSDA.Cd32padMask) == 0)
					continue;

				var name = Enum.GetName(typeof(LibDSDA.AllButtons), b)!.Replace('_', ' ');
				joystickMap.Add((name, (LibDSDA.AllButtons)b));
			}
			return joystickMap.ToArray();
		}

		private static (string Name, LibDSDA.DSDAKeyboard Value)[] CreateKeyboardMap()
		{
			var keyboardMap = new List<(string, LibDSDA.DSDAKeyboard)>();
			// ReSharper disable once LoopCanBeConvertedToQuery
			foreach (var k in Enum.GetValues(typeof(LibDSDA.DSDAKeyboard)))
			{
				var name = Enum.GetName(typeof(LibDSDA.DSDAKeyboard), k)!.Replace('_', ' ');
				keyboardMap.Add((name, (LibDSDA.DSDAKeyboard)k));
			}

			return keyboardMap.ToArray();
		}

		private static ControllerDefinition CreateControllerDefinition(DSDASyncSettings settings)
		{
			var controller = new ControllerDefinition("Amiga Controller");

			for (int port = 1; port <= 2; port++)
			{
				LibDSDA.ControllerType type = port == 1
					? settings.ControllerPort1
					: settings.ControllerPort2;

				switch (type)
				{
					case LibDSDA.ControllerType.Joystick:
						{
							foreach (var (name, _) in _joystickMap)
							{
								controller.BoolButtons.Add($"P{port} {Inputs.Joystick} {name}");
							}
							break;
						}
					case LibDSDA.ControllerType.CD32_pad:
						{
							foreach (var (name, _) in _cd32padMap)
							{
								controller.BoolButtons.Add($"P{port} {Inputs.Cd32Pad} {name}");
							}
							break;
						}
					case LibDSDA.ControllerType.Mouse:
						{
							controller.BoolButtons.AddRange(
							[
								$"P{port} {Inputs.MouseLeftButton}",
								$"P{port} {Inputs.MouseMiddleButton}",
								$"P{port} {Inputs.MouseRightButton}"
							]);
							controller
								.AddAxis($"P{port} {Inputs.MouseX}", 0.RangeTo(LibDSDA.PAL_WIDTH), LibDSDA.PAL_WIDTH / 2)
								.AddAxis($"P{port} {Inputs.MouseY}", 0.RangeTo(LibDSDA.PAL_HEIGHT), LibDSDA.PAL_HEIGHT / 2);
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
