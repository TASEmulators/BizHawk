using BizHawk.Emulation.Common;
using static BizHawk.Emulation.Cores.Libretro.LibretroApi;

namespace BizHawk.Emulation.Cores.Libretro
{
	public partial class LibretroHost : IInputPollable
	{
		// TBD
		// we could actually remove IInputPollable here
		// although that would prevent tastudio use entirely
		// maybe better overall as libretro has no place for movies

		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }

		[FeatureNotImplemented]
		public IInputCallbackSystem InputCallbacks => throw new NotImplementedException();

		private readonly short[] _joypad0States = new short[(int)RETRO_DEVICE_ID_JOYPAD.LAST];
		private readonly short[] _joypad1States = new short[(int)RETRO_DEVICE_ID_JOYPAD.LAST];
		private readonly short[] _pointerStates = new short[(int)RETRO_DEVICE_ID_POINTER.LAST];
		private readonly short[] _keyStates = new short[(int)RETRO_KEY.LAST];

		// todo
		// implement more input types
		// limit inputs according to user selection / core limitations (something with RETRO_ENVIRONMENT_SET_CONTROLLER_INFO?)
		void UpdateInput(IController controller)
		{
			SetInputs(controller, RETRO_DEVICE.JOYPAD, 0, _joypad0States);
			SetInputs(controller, RETRO_DEVICE.JOYPAD, 1, _joypad1States);
			SetInputs(controller, RETRO_DEVICE.POINTER, 0, _pointerStates);
			SetInputs(controller, RETRO_DEVICE.KEYBOARD, 0, _keyStates);
		}

		private void SetInputs(IController controller, RETRO_DEVICE device, int port, short[] inputBuffer)
		{
			// index is 0 always except for ANALOG devices, which we don't handle yet (impl TBD)
			for (int i = 0; i < inputBuffer.Length; i++)
			{
				inputBuffer[i] = InputState(controller, port, device, 0, i);
			}
			bridge.LibretroBridge_SetInput(cbHandler, device, port, inputBuffer);
		}

		//meanings (they are kind of hazy, but once we're done implementing this it will be completely defined by example)
		//port = console physical port?
		//device = logical device type
		//index = sub device index? (multitap?) (only actually used for the ANALOG device however?)
		//id = button id (or key id)
		private static short InputState(IController controller, int port, RETRO_DEVICE device, int index, int id)
		{
			switch (device)
			{
				case RETRO_DEVICE.POINTER:
					{
						return (RETRO_DEVICE_ID_POINTER)id switch
						{
							RETRO_DEVICE_ID_POINTER.X => (short)controller.AxisValue("Pointer X"),
							RETRO_DEVICE_ID_POINTER.Y => (short)controller.AxisValue("Pointer Y"),
							RETRO_DEVICE_ID_POINTER.PRESSED => (short)(controller.IsPressed("Pointer Pressed") ? 1 : 0),
							RETRO_DEVICE_ID_POINTER.COUNT => (short)(controller.IsPressed("Pointer Pressed") ? 1 : 0), // i think this means "number of presses"? we don't support multitouch anyways so
							_ => throw new InvalidOperationException($"Invalid {nameof(RETRO_DEVICE_ID_POINTER)}")
						};
					}

				case RETRO_DEVICE.KEYBOARD:
					{
						var button = (RETRO_KEY)id switch
						{
							RETRO_KEY.BACKSPACE => "Backspace",
							RETRO_KEY.TAB => "Tab",
							RETRO_KEY.CLEAR => "Clear",
							RETRO_KEY.RETURN => "Return",
							RETRO_KEY.PAUSE => "Pause",
							RETRO_KEY.ESCAPE => "Escape",
							RETRO_KEY.SPACE => "Space",
							RETRO_KEY.EXCLAIM => "Exclaim",
							RETRO_KEY.QUOTEDBL => "QuoteDbl",
							RETRO_KEY.HASH => "Hash",
							RETRO_KEY.DOLLAR => "Dollar",
							RETRO_KEY.AMPERSAND => "Ampersand",
							RETRO_KEY.QUOTE => "Quote",
							RETRO_KEY.LEFTPAREN => "LeftParen",
							RETRO_KEY.RIGHTPAREN => "RightParen",
							RETRO_KEY.ASTERISK => "Asterisk",
							RETRO_KEY.PLUS => "Plus",
							RETRO_KEY.COMMA => "Comma",
							RETRO_KEY.MINUS => "Minus",
							RETRO_KEY.PERIOD => "Period",
							RETRO_KEY.SLASH => "Slash",
							RETRO_KEY._0 => "0",
							RETRO_KEY._1 => "1",
							RETRO_KEY._2 => "2",
							RETRO_KEY._3 => "3",
							RETRO_KEY._4 => "4",
							RETRO_KEY._5 => "5",
							RETRO_KEY._6 => "6",
							RETRO_KEY._7 => "7",
							RETRO_KEY._8 => "8",
							RETRO_KEY._9 => "9",
							RETRO_KEY.COLON => "Colon",
							RETRO_KEY.SEMICOLON => "Semicolon",
							RETRO_KEY.LESS => "Less",
							RETRO_KEY.EQUALS => "Equals",
							RETRO_KEY.GREATER => "Greater",
							RETRO_KEY.QUESTION => "Question",
							RETRO_KEY.AT => "At",
							RETRO_KEY.LEFTBRACKET => "LeftBracket",
							RETRO_KEY.BACKSLASH => "Backslash",
							RETRO_KEY.RIGHTBRACKET => "RightBracket",
							RETRO_KEY.CARET => "Caret",
							RETRO_KEY.UNDERSCORE => "Underscore",
							RETRO_KEY.BACKQUOTE => "Backquote",
							RETRO_KEY.a => "A",
							RETRO_KEY.b => "B",
							RETRO_KEY.c => "C",
							RETRO_KEY.d => "D",
							RETRO_KEY.e => "E",
							RETRO_KEY.f => "F",
							RETRO_KEY.g => "G",
							RETRO_KEY.h => "H",
							RETRO_KEY.i => "I",
							RETRO_KEY.j => "J",
							RETRO_KEY.k => "K",
							RETRO_KEY.l => "L",
							RETRO_KEY.m => "M",
							RETRO_KEY.n => "N",
							RETRO_KEY.o => "O",
							RETRO_KEY.p => "P",
							RETRO_KEY.q => "Q",
							RETRO_KEY.r => "R",
							RETRO_KEY.s => "S",
							RETRO_KEY.t => "T",
							RETRO_KEY.u => "U",
							RETRO_KEY.v => "V",
							RETRO_KEY.w => "W",
							RETRO_KEY.x => "X",
							RETRO_KEY.y => "Y",
							RETRO_KEY.z => "Z",
							RETRO_KEY.DELETE => "Delete",
							RETRO_KEY.KP0 => "KP0",
							RETRO_KEY.KP1 => "KP1",
							RETRO_KEY.KP2 => "KP2",
							RETRO_KEY.KP3 => "KP3",
							RETRO_KEY.KP4 => "KP4",
							RETRO_KEY.KP5 => "KP5",
							RETRO_KEY.KP6 => "KP6",
							RETRO_KEY.KP7 => "KP7",
							RETRO_KEY.KP8 => "KP8",
							RETRO_KEY.KP9 => "KP9",
							RETRO_KEY.KP_PERIOD => "KP_Period",
							RETRO_KEY.KP_DIVIDE => "KP_Divide",
							RETRO_KEY.KP_MULTIPLY => "KP_Multiply",
							RETRO_KEY.KP_MINUS => "KP_Minus",
							RETRO_KEY.KP_PLUS => "KP_Plus",
							RETRO_KEY.KP_ENTER => "KP_Enter",
							RETRO_KEY.KP_EQUALS => "KP_Equals",
							RETRO_KEY.UP => "Up",
							RETRO_KEY.DOWN => "Down",
							RETRO_KEY.RIGHT => "Right",
							RETRO_KEY.LEFT => "Left",
							RETRO_KEY.INSERT => "Insert",
							RETRO_KEY.HOME => "Home",
							RETRO_KEY.END => "End",
							RETRO_KEY.PAGEUP => "PageUp",
							RETRO_KEY.PAGEDOWN => "PageDown",
							RETRO_KEY.F1 => "F1",
							RETRO_KEY.F2 => "F2",
							RETRO_KEY.F3 => "F3",
							RETRO_KEY.F4 => "F4",
							RETRO_KEY.F5 => "F5",
							RETRO_KEY.F6 => "F6",
							RETRO_KEY.F7 => "F7",
							RETRO_KEY.F8 => "F8",
							RETRO_KEY.F9 => "F9",
							RETRO_KEY.F10 => "F10",
							RETRO_KEY.F11 => "F11",
							RETRO_KEY.F12 => "F12",
							RETRO_KEY.F13 => "F13",
							RETRO_KEY.F14 => "F14",
							RETRO_KEY.F15 => "F15",
							RETRO_KEY.NUMLOCK => "NumLock",
							RETRO_KEY.CAPSLOCK => "CapsLock",
							RETRO_KEY.SCROLLOCK => "ScrollLock",
							RETRO_KEY.RSHIFT => "RShift",
							RETRO_KEY.LSHIFT => "LShift",
							RETRO_KEY.RCTRL => "RCtrl",
							RETRO_KEY.LCTRL => "LCtrl",
							RETRO_KEY.RALT => "RAlt",
							RETRO_KEY.LALT => "LAlt",
							RETRO_KEY.RMETA => "RMeta",
							RETRO_KEY.LMETA => "LMeta",
							RETRO_KEY.LSUPER => "LSuper",
							RETRO_KEY.RSUPER => "RSuper",
							RETRO_KEY.MODE => "Mode",
							RETRO_KEY.COMPOSE => "Compose",
							RETRO_KEY.HELP => "Help",
							RETRO_KEY.PRINT => "Print",
							RETRO_KEY.SYSREQ => "SysReq",
							RETRO_KEY.BREAK => "Break",
							RETRO_KEY.MENU => "Menu",
							RETRO_KEY.POWER => "Power",
							RETRO_KEY.EURO => "Euro",
							RETRO_KEY.UNDO => "Undo",
							_ => "", // annoyingly a lot of gaps are present in RETRO_KEY, so can't just throw here
						};

						return (short)(controller.IsPressed("Key " + button) ? 1 : 0);
					}

				case RETRO_DEVICE.JOYPAD:
					{
						var button = (RETRO_DEVICE_ID_JOYPAD)id switch
						{
							RETRO_DEVICE_ID_JOYPAD.A => "A",
							RETRO_DEVICE_ID_JOYPAD.B => "B",
							RETRO_DEVICE_ID_JOYPAD.X => "X",
							RETRO_DEVICE_ID_JOYPAD.Y => "Y",
							RETRO_DEVICE_ID_JOYPAD.UP => "Up",
							RETRO_DEVICE_ID_JOYPAD.DOWN => "Down",
							RETRO_DEVICE_ID_JOYPAD.LEFT => "Left",
							RETRO_DEVICE_ID_JOYPAD.RIGHT => "Right",
							RETRO_DEVICE_ID_JOYPAD.L => "L",
							RETRO_DEVICE_ID_JOYPAD.R => "R",
							RETRO_DEVICE_ID_JOYPAD.SELECT => "Select",
							RETRO_DEVICE_ID_JOYPAD.START => "Start",
							RETRO_DEVICE_ID_JOYPAD.L2 => "L2",
							RETRO_DEVICE_ID_JOYPAD.R2 => "R2",
							RETRO_DEVICE_ID_JOYPAD.L3 => "L3",
							RETRO_DEVICE_ID_JOYPAD.R3 => "R3",
							_ => throw new InvalidOperationException($"Invalid {nameof(RETRO_DEVICE_ID_JOYPAD)}"),
						};

						return (short)(GetButton(controller, port + 1, "RetroPad", button) ? 1 : 0);
					}
				default:
					throw new InvalidOperationException($"Invalid or unimplemented {nameof(RETRO_DEVICE)}");
			}
		}

		private static bool GetButton(IController controller, int pnum, string type, string button)
		{
			var key = $"P{pnum} {type} {button}";
			return controller.IsPressed(key);
		}
	}
}
