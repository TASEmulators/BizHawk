using System.Collections.Generic;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.DOS
{
	public partial class DOSBox
	{
		// A class to store the current state of the mouse for delta and button activation calculation
		private class MouseState
		{
			public int PosX = 0;
			public int PosY = 0;
			public bool LeftButtonHeld = false;
			public bool MiddleButtonHeld = false;
			public bool RightButtonHeld = false;
		}

		private MouseState _lastMouseState = new MouseState();

		private static readonly (string Name, LibDOSBox.DOSBoxKeyboard Key)[] _keyboardMap = CreateKeyboardMap();

		private static (string Name, LibDOSBox.DOSBoxKeyboard Value)[] CreateKeyboardMap()
		{
			var keyboardMap = new List<(string, LibDOSBox.DOSBoxKeyboard)>();
			// ReSharper disable once LoopCanBeConvertedToQuery
			foreach (var k in Enum.GetValues(typeof(LibDOSBox.DOSBoxKeyboard)))
			{
				if (k is LibDOSBox.DOSBoxKeyboard.Key_None) continue;
				var name = Enum.GetName(typeof(LibDOSBox.DOSBoxKeyboard), k)!.Replace('_', ' ');
				keyboardMap.Add((name, (LibDOSBox.DOSBoxKeyboard) k));
			}

			return keyboardMap.ToArray();
		}

		private static ControllerDefinition CreateControllerDefinition(SyncSettings settings, int floppyDiskCount, int cdROMCount)
		{
			var controller = new ControllerDefinition("DOSBox Controller");

			// Adding joystick buttons
			if (settings.EnableJoystick1)
			{
				foreach (var button in JoystickButtonCollection)
				{
					controller.BoolButtons.Add("P1 " + Inputs.Joystick + " " + button);
				}
			}

			if (settings.EnableJoystick2)
			{
				foreach (var button in JoystickButtonCollection)
				{
					controller.BoolButtons.Add("P2 " + Inputs.Joystick + " " + button);
				}
			}

			// Adding mouse inputs
			if (settings.EnableMouse)
			{
				controller.BoolButtons.Add(Inputs.Mouse + " " + MouseInputs.LeftButton);
				controller.BoolButtons.Add(Inputs.Mouse + " " + MouseInputs.MiddleButton);
				controller.BoolButtons.Add(Inputs.Mouse + " " + MouseInputs.RightButton);

				// Although most apps (including windows) use speed to compute the mouse position, some games (SimCity) use the absolute value
				// The absolute values needs to be 1:1 with the video size, as this is translated 1:1 to the core.
				// The effect is that the position on BK's window translates perfectly to the core
				controller.AddAxis(Inputs.Mouse + " " + MouseInputs.PosX, (0).RangeTo(LibDOSBox.SVGA_MAX_WIDTH), LibDOSBox.SVGA_MAX_WIDTH / 2);
				controller.AddAxis(Inputs.Mouse + " " + MouseInputs.PosY, (0).RangeTo(LibDOSBox.SVGA_MAX_HEIGHT), LibDOSBox.SVGA_MAX_HEIGHT / 2);

				// Range above 180 results in minimal mouse movement values bigger than 1, and we need 1 as a basis before sensitivity is applied
				// To adjust sensitivity, use the corresponding sync setting (global sensitivity for raw deltas is a TODO)
				controller.AddAxis(Inputs.Mouse + " " + MouseInputs.SpeedX, (-180).RangeTo(180), 0);
				controller.AddAxis(Inputs.Mouse + " " + MouseInputs.SpeedY, (-180).RangeTo(180), 0);
			}

			// Adding drive management buttons
			if (floppyDiskCount > 1)
			{
				controller.BoolButtons.Add(Inputs.PrevFloppyDisk);
				controller.BoolButtons.Add(Inputs.NextFloppyDisk);
				controller.BoolButtons.Add(Inputs.SwapFloppyDisk);
			}

			if (cdROMCount > 1)
			{
				controller.BoolButtons.Add(Inputs.PrevCDROM);
				controller.BoolButtons.Add(Inputs.NextCDROM);
				controller.BoolButtons.Add(Inputs.SwapCDROM);
			}

			foreach (var (name, _) in _keyboardMap)
			{
				controller.BoolButtons.Add(name);
				controller.CategoryLabels[name] = "Keyboard";
			}

			return controller.MakeImmutable();
		}

		private static string[] JoystickButtonCollection = [
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
			public const string Button1 = "Button 1";
			public const string Button2 = "Button 2";
		}

		private static class MouseInputs
		{
			public const string LeftButton = "Left Button";
			public const string RightButton = "Right Button";
			public const string MiddleButton = "Middle Button";
			public const string PosX = "Position X";
			public const string PosY = "Position Y";
			public const string SpeedX = "Speed X"; // How many pixels has it changed in a single frame
			public const string SpeedY = "Speed Y";
		}

		private static class Inputs
		{
			public const string Joystick = "Joystick";
			public const string Mouse = "Mouse";
			public const string PrevFloppyDisk = "Previous Floppy Disk";
			public const string NextFloppyDisk = "Next Floppy Disk";
			public const string SwapFloppyDisk = "Swap Floppy Disk";
			public const string PrevCDROM = "Previous CDROM";
			public const string NextCDROM = "Next CDROM";
			public const string SwapCDROM = "Swap CDROM";
		}
	}
}
