using System.Collections.Generic;

using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.DOS
{
	public partial class DOSBox
	{
		private static readonly (string Name, LibDOSBox.DOSBoxKeyboard Key)[] _keyboardMap = CreateKeyboardMap();

		private static (string Name, LibDOSBox.DOSBoxKeyboard Value)[] CreateKeyboardMap()
		{
			var keyboardMap = new List<(string, LibDOSBox.DOSBoxKeyboard)>();
			// ReSharper disable once LoopCanBeConvertedToQuery
			foreach (var k in Enum.GetValues(typeof(LibDOSBox.DOSBoxKeyboard)))
			{
				var name = Enum.GetName(typeof(LibDOSBox.DOSBoxKeyboard), k)!.Replace('_', ' ');
				keyboardMap.Add((name, (LibDOSBox.DOSBoxKeyboard) k));
			}

			return keyboardMap.ToArray();
		}

		private static ControllerDefinition CreateControllerDefinition(SyncSettings settings)
		{
			var controller = new ControllerDefinition("DOSBox Controller");

			// Adding joystick buttons
			if (settings.EnableJoystick1)
				foreach (var button in JoystickButtonCollection)
					controller.BoolButtons.Add("P1 " + Inputs.Joystick + " " + button);

			if (settings.EnableJoystick2)
				foreach (var button in JoystickButtonCollection)
					controller.BoolButtons.Add("P2 " + Inputs.Joystick + " " + button);

			// Adding drive management buttons
			controller.BoolButtons.AddRange(
			[
				Inputs.NextFloppyDisk, Inputs.NextCDROM, Inputs.NextHardDiskDrive
			]);

			foreach (var (name, _) in _keyboardMap)
			{
				controller.BoolButtons.Add(name);
				controller.CategoryLabels[name] = "Keyboard";
			}

			return controller.MakeImmutable();
		}

		private static string[] JoystickButtonCollection =
		[
			JoystickButtons.Up,
			JoystickButtons.Down,
			JoystickButtons.Left,
			JoystickButtons.Right,
			JoystickButtons.Button1,
			JoystickButtons.Button2
		];

		private static class JoystickButtons
		{
			public const string Up = "Up";
			public const string Down = "Down";
			public const string Left = "Left";
			public const string Right = "Right";
			public const string Button1 = "Button1";
			public const string Button2 = "Button2";
		}

		private static class Inputs
		{
			public const string Joystick = "Joystick";
			public const string MouseLeftButton = "Mouse Left Button";
			public const string MouseRightButton = "Mouse Right Button";
			public const string MouseMiddleButton = "Mouse Middle Button";
			public const string MouseX = "Mouse X";
			public const string MouseY = "Mouse Y";
			public const string NextFloppyDisk = "Next Floppy Disk";
			public const string NextCDROM = "Next CDROM";
			public const string NextHardDiskDrive = "Next HardDisk Drive";
		}
	}
}
