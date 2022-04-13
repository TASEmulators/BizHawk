using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Libretro
{
	public partial class LibretroEmulator : IInputPollable
	{
		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }

		[FeatureNotImplemented]
		public IInputCallbackSystem InputCallbacks => throw new NotImplementedException();

		// todo: make this better
		void UpdateInput(IController controller)
		{
			short[] input = new short[(int)LibretroApi.RETRO_DEVICE_ID_JOYPAD.LAST];
			// joypad port 0
			for (uint i = 0; i < input.Length; i++)
			{
				input[i] = retro_input_state(controller, 0, (uint)LibretroApi.RETRO_DEVICE.JOYPAD, 0, i);
			}
			bridge.LibretroBridge_SetInput(cbHandler, LibretroApi.RETRO_DEVICE.JOYPAD, 0, input);
			// joypad port 1
			for (uint i = 0; i < input.Length; i++)
			{
				input[i] = retro_input_state(controller, 0, (uint)LibretroApi.RETRO_DEVICE.JOYPAD, 1, i);
			}
			bridge.LibretroBridge_SetInput(cbHandler, LibretroApi.RETRO_DEVICE.JOYPAD, 1, input);
			input = new short[(int)LibretroApi.RETRO_DEVICE_ID_POINTER.LAST];
			// pointer port 0
			for (uint i = 0; i < input.Length; i++)
			{
				input[i] = retro_input_state(controller, 0, (uint)LibretroApi.RETRO_DEVICE.POINTER, 0, i);
			}
			bridge.LibretroBridge_SetInput(cbHandler, LibretroApi.RETRO_DEVICE.POINTER, 0, input);
			input = new short[(int)LibretroApi.RETRO_KEY.LAST];
			// keyboard port 0
			for (uint i = 0; i < input.Length; i++)
			{
				input[i] = retro_input_state(controller, 0, (uint)LibretroApi.RETRO_DEVICE.KEYBOARD, 0, i);
			}
			bridge.LibretroBridge_SetInput(cbHandler, LibretroApi.RETRO_DEVICE.KEYBOARD, 0, input);
		}

		//meanings (they are kind of hazy, but once we're done implementing this it will be completely defined by example)
		//port = console physical port?
		//device = logical device type
		//index = sub device index? (multitap?)
		//id = button id (or key id)
		private static short retro_input_state(IController controller, uint port, uint device, uint index, uint id)
		{
			//helpful debugging
			//Console.WriteLine("{0} {1} {2} {3}", port, device, index, id);

			switch ((LibretroApi.RETRO_DEVICE)device)
			{
				case LibretroApi.RETRO_DEVICE.POINTER:
					{
						return (LibretroApi.RETRO_DEVICE_ID_POINTER)id switch
						{
							LibretroApi.RETRO_DEVICE_ID_POINTER.X => (short)controller.AxisValue("Pointer X"),
							LibretroApi.RETRO_DEVICE_ID_POINTER.Y => (short)controller.AxisValue("Pointer Y"),
							LibretroApi.RETRO_DEVICE_ID_POINTER.PRESSED => (short)(controller.IsPressed("Pointer Pressed") ? 1 : 0),
							_ => 0,
						};
					}

				case LibretroApi.RETRO_DEVICE.KEYBOARD:
					{
						string button = (LibretroApi.RETRO_KEY)id switch
						{
							LibretroApi.RETRO_KEY.BACKSPACE => "Backspace",
							LibretroApi.RETRO_KEY.TAB => "Tab",
							LibretroApi.RETRO_KEY.CLEAR => "Clear",
							LibretroApi.RETRO_KEY.RETURN => "Return",
							LibretroApi.RETRO_KEY.PAUSE => "Pause",
							LibretroApi.RETRO_KEY.ESCAPE => "Escape",
							LibretroApi.RETRO_KEY.SPACE => "Space",
							LibretroApi.RETRO_KEY.EXCLAIM => "Exclaim",
							LibretroApi.RETRO_KEY.QUOTEDBL => "QuoteDbl",
							LibretroApi.RETRO_KEY.HASH => "Hash",
							LibretroApi.RETRO_KEY.DOLLAR => "Dollar",
							LibretroApi.RETRO_KEY.AMPERSAND => "Ampersand",
							LibretroApi.RETRO_KEY.QUOTE => "Quote",
							LibretroApi.RETRO_KEY.LEFTPAREN => "LeftParen",
							LibretroApi.RETRO_KEY.RIGHTPAREN => "RightParen",
							LibretroApi.RETRO_KEY.ASTERISK => "Asterisk",
							LibretroApi.RETRO_KEY.PLUS => "Plus",
							LibretroApi.RETRO_KEY.COMMA => "Comma",
							LibretroApi.RETRO_KEY.MINUS => "Minus",
							LibretroApi.RETRO_KEY.PERIOD => "Period",
							LibretroApi.RETRO_KEY.SLASH => "Slash",
							LibretroApi.RETRO_KEY._0 => "0",
							LibretroApi.RETRO_KEY._1 => "1",
							LibretroApi.RETRO_KEY._2 => "2",
							LibretroApi.RETRO_KEY._3 => "3",
							LibretroApi.RETRO_KEY._4 => "4",
							LibretroApi.RETRO_KEY._5 => "5",
							LibretroApi.RETRO_KEY._6 => "6",
							LibretroApi.RETRO_KEY._7 => "7",
							LibretroApi.RETRO_KEY._8 => "8",
							LibretroApi.RETRO_KEY._9 => "9",
							LibretroApi.RETRO_KEY.COLON => "Colon",
							LibretroApi.RETRO_KEY.SEMICOLON => "Semicolon",
							LibretroApi.RETRO_KEY.LESS => "Less",
							LibretroApi.RETRO_KEY.EQUALS => "Equals",
							LibretroApi.RETRO_KEY.GREATER => "Greater",
							LibretroApi.RETRO_KEY.QUESTION => "Question",
							LibretroApi.RETRO_KEY.AT => "At",
							LibretroApi.RETRO_KEY.LEFTBRACKET => "LeftBracket",
							LibretroApi.RETRO_KEY.BACKSLASH => "Backslash",
							LibretroApi.RETRO_KEY.RIGHTBRACKET => "RightBracket",
							LibretroApi.RETRO_KEY.CARET => "Caret",
							LibretroApi.RETRO_KEY.UNDERSCORE => "Underscore",
							LibretroApi.RETRO_KEY.BACKQUOTE => "Backquote",
							LibretroApi.RETRO_KEY.a => "A",
							LibretroApi.RETRO_KEY.b => "B",
							LibretroApi.RETRO_KEY.c => "C",
							LibretroApi.RETRO_KEY.d => "D",
							LibretroApi.RETRO_KEY.e => "E",
							LibretroApi.RETRO_KEY.f => "F",
							LibretroApi.RETRO_KEY.g => "G",
							LibretroApi.RETRO_KEY.h => "H",
							LibretroApi.RETRO_KEY.i => "I",
							LibretroApi.RETRO_KEY.j => "J",
							LibretroApi.RETRO_KEY.k => "K",
							LibretroApi.RETRO_KEY.l => "L",
							LibretroApi.RETRO_KEY.m => "M",
							LibretroApi.RETRO_KEY.n => "N",
							LibretroApi.RETRO_KEY.o => "O",
							LibretroApi.RETRO_KEY.p => "P",
							LibretroApi.RETRO_KEY.q => "Q",
							LibretroApi.RETRO_KEY.r => "R",
							LibretroApi.RETRO_KEY.s => "S",
							LibretroApi.RETRO_KEY.t => "T",
							LibretroApi.RETRO_KEY.u => "U",
							LibretroApi.RETRO_KEY.v => "V",
							LibretroApi.RETRO_KEY.w => "W",
							LibretroApi.RETRO_KEY.x => "X",
							LibretroApi.RETRO_KEY.y => "Y",
							LibretroApi.RETRO_KEY.z => "Z",
							LibretroApi.RETRO_KEY.DELETE => "Delete",
							LibretroApi.RETRO_KEY.KP0 => "KP0",
							LibretroApi.RETRO_KEY.KP1 => "KP1",
							LibretroApi.RETRO_KEY.KP2 => "KP2",
							LibretroApi.RETRO_KEY.KP3 => "KP3",
							LibretroApi.RETRO_KEY.KP4 => "KP4",
							LibretroApi.RETRO_KEY.KP5 => "KP5",
							LibretroApi.RETRO_KEY.KP6 => "KP6",
							LibretroApi.RETRO_KEY.KP7 => "KP7",
							LibretroApi.RETRO_KEY.KP8 => "KP8",
							LibretroApi.RETRO_KEY.KP9 => "KP9",
							LibretroApi.RETRO_KEY.KP_PERIOD => "KP_Period",
							LibretroApi.RETRO_KEY.KP_DIVIDE => "KP_Divide",
							LibretroApi.RETRO_KEY.KP_MULTIPLY => "KP_Multiply",
							LibretroApi.RETRO_KEY.KP_MINUS => "KP_Minus",
							LibretroApi.RETRO_KEY.KP_PLUS => "KP_Plus",
							LibretroApi.RETRO_KEY.KP_ENTER => "KP_Enter",
							LibretroApi.RETRO_KEY.KP_EQUALS => "KP_Equals",
							LibretroApi.RETRO_KEY.UP => "Up",
							LibretroApi.RETRO_KEY.DOWN => "Down",
							LibretroApi.RETRO_KEY.RIGHT => "Right",
							LibretroApi.RETRO_KEY.LEFT => "Left",
							LibretroApi.RETRO_KEY.INSERT => "Insert",
							LibretroApi.RETRO_KEY.HOME => "Home",
							LibretroApi.RETRO_KEY.END => "End",
							LibretroApi.RETRO_KEY.PAGEUP => "PageUp",
							LibretroApi.RETRO_KEY.PAGEDOWN => "PageDown",
							LibretroApi.RETRO_KEY.F1 => "F1",
							LibretroApi.RETRO_KEY.F2 => "F2",
							LibretroApi.RETRO_KEY.F3 => "F3",
							LibretroApi.RETRO_KEY.F4 => "F4",
							LibretroApi.RETRO_KEY.F5 => "F5",
							LibretroApi.RETRO_KEY.F6 => "F6",
							LibretroApi.RETRO_KEY.F7 => "F7",
							LibretroApi.RETRO_KEY.F8 => "F8",
							LibretroApi.RETRO_KEY.F9 => "F9",
							LibretroApi.RETRO_KEY.F10 => "F10",
							LibretroApi.RETRO_KEY.F11 => "F11",
							LibretroApi.RETRO_KEY.F12 => "F12",
							LibretroApi.RETRO_KEY.F13 => "F13",
							LibretroApi.RETRO_KEY.F14 => "F14",
							LibretroApi.RETRO_KEY.F15 => "F15",
							LibretroApi.RETRO_KEY.NUMLOCK => "NumLock",
							LibretroApi.RETRO_KEY.CAPSLOCK => "CapsLock",
							LibretroApi.RETRO_KEY.SCROLLOCK => "ScrollLock",
							LibretroApi.RETRO_KEY.RSHIFT => "RShift",
							LibretroApi.RETRO_KEY.LSHIFT => "LShift",
							LibretroApi.RETRO_KEY.RCTRL => "RCtrl",
							LibretroApi.RETRO_KEY.LCTRL => "LCtrl",
							LibretroApi.RETRO_KEY.RALT => "RAlt",
							LibretroApi.RETRO_KEY.LALT => "LAlt",
							LibretroApi.RETRO_KEY.RMETA => "RMeta",
							LibretroApi.RETRO_KEY.LMETA => "LMeta",
							LibretroApi.RETRO_KEY.LSUPER => "LSuper",
							LibretroApi.RETRO_KEY.RSUPER => "RSuper",
							LibretroApi.RETRO_KEY.MODE => "Mode",
							LibretroApi.RETRO_KEY.COMPOSE => "Compose",
							LibretroApi.RETRO_KEY.HELP => "Help",
							LibretroApi.RETRO_KEY.PRINT => "Print",
							LibretroApi.RETRO_KEY.SYSREQ => "SysReq",
							LibretroApi.RETRO_KEY.BREAK => "Break",
							LibretroApi.RETRO_KEY.MENU => "Menu",
							LibretroApi.RETRO_KEY.POWER => "Power",
							LibretroApi.RETRO_KEY.EURO => "Euro",
							LibretroApi.RETRO_KEY.UNDO => "Undo",
							_ => "",
						};

						return (short)(controller.IsPressed("Key " + button) ? 1 : 0);
					}

				case LibretroApi.RETRO_DEVICE.JOYPAD:
					{
						string button = (LibretroApi.RETRO_DEVICE_ID_JOYPAD)id switch
						{
							LibretroApi.RETRO_DEVICE_ID_JOYPAD.A => "A",
							LibretroApi.RETRO_DEVICE_ID_JOYPAD.B => "B",
							LibretroApi.RETRO_DEVICE_ID_JOYPAD.X => "X",
							LibretroApi.RETRO_DEVICE_ID_JOYPAD.Y => "Y",
							LibretroApi.RETRO_DEVICE_ID_JOYPAD.UP => "Up",
							LibretroApi.RETRO_DEVICE_ID_JOYPAD.DOWN => "Down",
							LibretroApi.RETRO_DEVICE_ID_JOYPAD.LEFT => "Left",
							LibretroApi.RETRO_DEVICE_ID_JOYPAD.RIGHT => "Right",
							LibretroApi.RETRO_DEVICE_ID_JOYPAD.L => "L",
							LibretroApi.RETRO_DEVICE_ID_JOYPAD.R => "R",
							LibretroApi.RETRO_DEVICE_ID_JOYPAD.SELECT => "Select",
							LibretroApi.RETRO_DEVICE_ID_JOYPAD.START => "Start",
							_ => "",
						};

						return (short)(GetButton(controller, port + 1, "RetroPad", button) ? 1 : 0);
					}
				default:
					return 0;
			}
		}

		private static bool GetButton(IController controller, uint pnum, string type, string button)
		{
			string key = $"P{pnum} {type} {button}";
			return controller.IsPressed(key);
		}

	}
}
