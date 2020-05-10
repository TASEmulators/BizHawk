namespace BizHawk.Emulation.Cores.Libretro
{
	partial class LibretroCore
	{
		//meanings (they are kind of hazy, but once we're done implementing this it will be completely defined by example)
		//port = console physical port?
		//device = logical device type
		//index = sub device index? (multitap?)
		//id = button id (or key id)
		public short CB_InputState(uint port, uint device, uint index, uint id)
		{
			//helpful debugging
			//Console.WriteLine("{0} {1} {2} {3}", port, device, index, id);

			switch ((LibretroApi.RETRO_DEVICE)device)
			{
				case LibretroApi.RETRO_DEVICE.POINTER:
					{
						switch ((LibretroApi.RETRO_DEVICE_ID_POINTER)id)
						{
							case LibretroApi.RETRO_DEVICE_ID_POINTER.X: return (short)_controller.AxisValue("Pointer X");
							case LibretroApi.RETRO_DEVICE_ID_POINTER.Y: return (short)_controller.AxisValue("Pointer Y");
							case LibretroApi.RETRO_DEVICE_ID_POINTER.PRESSED: return (short)(_controller.IsPressed("Pointer Pressed") ? 1 : 0);
						}
						return 0;
					}

				case LibretroApi.RETRO_DEVICE.KEYBOARD:
					{
						string button = "";
						switch ((LibretroApi.RETRO_KEY)id)
						{
							case LibretroApi.RETRO_KEY.BACKSPACE: button = "Backspace"; break;
							case LibretroApi.RETRO_KEY.TAB: button = "Tab"; break;
							case LibretroApi.RETRO_KEY.CLEAR: button = "Clear"; break;
							case LibretroApi.RETRO_KEY.RETURN: button = "Return"; break;
							case LibretroApi.RETRO_KEY.PAUSE: button = "Pause"; break;
							case LibretroApi.RETRO_KEY.ESCAPE: button = "Escape"; break;
							case LibretroApi.RETRO_KEY.SPACE: button = "Space"; break;
							case LibretroApi.RETRO_KEY.EXCLAIM: button = "Exclaim"; break;
							case LibretroApi.RETRO_KEY.QUOTEDBL: button = "QuoteDbl"; break;
							case LibretroApi.RETRO_KEY.HASH: button = "Hash"; break;
							case LibretroApi.RETRO_KEY.DOLLAR: button = "Dollar"; break;
							case LibretroApi.RETRO_KEY.AMPERSAND: button = "Ampersand"; break;
							case LibretroApi.RETRO_KEY.QUOTE: button = "Quote"; break;
							case LibretroApi.RETRO_KEY.LEFTPAREN: button = "LeftParen"; break;
							case LibretroApi.RETRO_KEY.RIGHTPAREN: button = "RightParen"; break;
							case LibretroApi.RETRO_KEY.ASTERISK: button = "Asterisk"; break;
							case LibretroApi.RETRO_KEY.PLUS: button = "Plus"; break;
							case LibretroApi.RETRO_KEY.COMMA: button = "Comma"; break;
							case LibretroApi.RETRO_KEY.MINUS: button = "Minus"; break;
							case LibretroApi.RETRO_KEY.PERIOD: button = "Period"; break;
							case LibretroApi.RETRO_KEY.SLASH: button = "Slash"; break;
							case LibretroApi.RETRO_KEY._0: button = "0"; break;
							case LibretroApi.RETRO_KEY._1: button = "1"; break;
							case LibretroApi.RETRO_KEY._2: button = "2"; break;
							case LibretroApi.RETRO_KEY._3: button = "3"; break;
							case LibretroApi.RETRO_KEY._4: button = "4"; break;
							case LibretroApi.RETRO_KEY._5: button = "5"; break;
							case LibretroApi.RETRO_KEY._6: button = "6"; break;
							case LibretroApi.RETRO_KEY._7: button = "7"; break;
							case LibretroApi.RETRO_KEY._8: button = "8"; break;
							case LibretroApi.RETRO_KEY._9: button = "9"; break;
							case LibretroApi.RETRO_KEY.COLON: button = "Colon"; break;
							case LibretroApi.RETRO_KEY.SEMICOLON: button = "Semicolon"; break;
							case LibretroApi.RETRO_KEY.LESS: button = "Less"; break;
							case LibretroApi.RETRO_KEY.EQUALS: button = "Equals"; break;
							case LibretroApi.RETRO_KEY.GREATER: button = "Greater"; break;
							case LibretroApi.RETRO_KEY.QUESTION: button = "Question"; break;
							case LibretroApi.RETRO_KEY.AT: button = "At"; break;
							case LibretroApi.RETRO_KEY.LEFTBRACKET: button = "LeftBracket"; break;
							case LibretroApi.RETRO_KEY.BACKSLASH: button = "Backslash"; break;
							case LibretroApi.RETRO_KEY.RIGHTBRACKET: button = "RightBracket"; break;
							case LibretroApi.RETRO_KEY.CARET: button = "Caret"; break;
							case LibretroApi.RETRO_KEY.UNDERSCORE: button = "Underscore"; break;
							case LibretroApi.RETRO_KEY.BACKQUOTE: button = "Backquote"; break;
							case LibretroApi.RETRO_KEY.a: button = "A"; break;
							case LibretroApi.RETRO_KEY.b: button = "B"; break;
							case LibretroApi.RETRO_KEY.c: button = "C"; break;
							case LibretroApi.RETRO_KEY.d: button = "D"; break;
							case LibretroApi.RETRO_KEY.e: button = "E"; break;
							case LibretroApi.RETRO_KEY.f: button = "F"; break;
							case LibretroApi.RETRO_KEY.g: button = "G"; break;
							case LibretroApi.RETRO_KEY.h: button = "H"; break;
							case LibretroApi.RETRO_KEY.i: button = "I"; break;
							case LibretroApi.RETRO_KEY.j: button = "J"; break;
							case LibretroApi.RETRO_KEY.k: button = "K"; break;
							case LibretroApi.RETRO_KEY.l: button = "L"; break;
							case LibretroApi.RETRO_KEY.m: button = "M"; break;
							case LibretroApi.RETRO_KEY.n: button = "N"; break;
							case LibretroApi.RETRO_KEY.o: button = "O"; break;
							case LibretroApi.RETRO_KEY.p: button = "P"; break;
							case LibretroApi.RETRO_KEY.q: button = "Q"; break;
							case LibretroApi.RETRO_KEY.r: button = "R"; break;
							case LibretroApi.RETRO_KEY.s: button = "S"; break;
							case LibretroApi.RETRO_KEY.t: button = "T"; break;
							case LibretroApi.RETRO_KEY.u: button = "U"; break;
							case LibretroApi.RETRO_KEY.v: button = "V"; break;
							case LibretroApi.RETRO_KEY.w: button = "W"; break;
							case LibretroApi.RETRO_KEY.x: button = "X"; break;
							case LibretroApi.RETRO_KEY.y: button = "Y"; break;
							case LibretroApi.RETRO_KEY.z: button = "Z"; break;
							case LibretroApi.RETRO_KEY.DELETE: button = "Delete"; break;

							case LibretroApi.RETRO_KEY.KP0: button = "KP0"; break;
							case LibretroApi.RETRO_KEY.KP1: button = "KP1"; break;
							case LibretroApi.RETRO_KEY.KP2: button = "KP2"; break;
							case LibretroApi.RETRO_KEY.KP3: button = "KP3"; break;
							case LibretroApi.RETRO_KEY.KP4: button = "KP4"; break;
							case LibretroApi.RETRO_KEY.KP5: button = "KP5"; break;
							case LibretroApi.RETRO_KEY.KP6: button = "KP6"; break;
							case LibretroApi.RETRO_KEY.KP7: button = "KP7"; break;
							case LibretroApi.RETRO_KEY.KP8: button = "KP8"; break;
							case LibretroApi.RETRO_KEY.KP9: button = "KP9"; break;
							case LibretroApi.RETRO_KEY.KP_PERIOD: button = "KP_Period"; break;
							case LibretroApi.RETRO_KEY.KP_DIVIDE: button = "KP_Divide"; break;
							case LibretroApi.RETRO_KEY.KP_MULTIPLY: button = "KP_Multiply"; break;
							case LibretroApi.RETRO_KEY.KP_MINUS: button = "KP_Minus"; break;
							case LibretroApi.RETRO_KEY.KP_PLUS: button = "KP_Plus"; break;
							case LibretroApi.RETRO_KEY.KP_ENTER: button = "KP_Enter"; break;
							case LibretroApi.RETRO_KEY.KP_EQUALS: button = "KP_Equals"; break;

							case LibretroApi.RETRO_KEY.UP: button = "Up"; break;
							case LibretroApi.RETRO_KEY.DOWN: button = "Down"; break;
							case LibretroApi.RETRO_KEY.RIGHT: button = "Right"; break;
							case LibretroApi.RETRO_KEY.LEFT: button = "Left"; break;
							case LibretroApi.RETRO_KEY.INSERT: button = "Insert"; break;
							case LibretroApi.RETRO_KEY.HOME: button = "Home"; break;
							case LibretroApi.RETRO_KEY.END: button = "End"; break;
							case LibretroApi.RETRO_KEY.PAGEUP: button = "PageUp"; break;
							case LibretroApi.RETRO_KEY.PAGEDOWN: button = "PageDown"; break;

							case LibretroApi.RETRO_KEY.F1: button = "F1"; break;
							case LibretroApi.RETRO_KEY.F2: button = "F2"; break;
							case LibretroApi.RETRO_KEY.F3: button = "F3"; break;
							case LibretroApi.RETRO_KEY.F4: button = "F4"; break;
							case LibretroApi.RETRO_KEY.F5: button = "F5"; break;
							case LibretroApi.RETRO_KEY.F6: button = "F6"; break;
							case LibretroApi.RETRO_KEY.F7: button = "F7"; break;
							case LibretroApi.RETRO_KEY.F8: button = "F8"; break;
							case LibretroApi.RETRO_KEY.F9: button = "F9"; break;
							case LibretroApi.RETRO_KEY.F10: button = "F10"; break;
							case LibretroApi.RETRO_KEY.F11: button = "F11"; break;
							case LibretroApi.RETRO_KEY.F12: button = "F12"; break;
							case LibretroApi.RETRO_KEY.F13: button = "F13"; break;
							case LibretroApi.RETRO_KEY.F14: button = "F14"; break;
							case LibretroApi.RETRO_KEY.F15: button = "F15"; break;

							case LibretroApi.RETRO_KEY.NUMLOCK: button = "NumLock"; break;
							case LibretroApi.RETRO_KEY.CAPSLOCK: button = "CapsLock"; break;
							case LibretroApi.RETRO_KEY.SCROLLOCK: button = "ScrollLock"; break;
							case LibretroApi.RETRO_KEY.RSHIFT: button = "RShift"; break;
							case LibretroApi.RETRO_KEY.LSHIFT: button = "LShift"; break;
							case LibretroApi.RETRO_KEY.RCTRL: button = "RCtrl"; break;
							case LibretroApi.RETRO_KEY.LCTRL: button = "LCtrl"; break;
							case LibretroApi.RETRO_KEY.RALT: button = "RAlt"; break;
							case LibretroApi.RETRO_KEY.LALT: button = "LAlt"; break;
							case LibretroApi.RETRO_KEY.RMETA: button = "RMeta"; break;
							case LibretroApi.RETRO_KEY.LMETA: button = "LMeta"; break;
							case LibretroApi.RETRO_KEY.LSUPER: button = "LSuper"; break;
							case LibretroApi.RETRO_KEY.RSUPER: button = "RSuper"; break;
							case LibretroApi.RETRO_KEY.MODE: button = "Mode"; break;
							case LibretroApi.RETRO_KEY.COMPOSE: button = "Compose"; break;

							case LibretroApi.RETRO_KEY.HELP: button = "Help"; break;
							case LibretroApi.RETRO_KEY.PRINT: button = "Print"; break;
							case LibretroApi.RETRO_KEY.SYSREQ: button = "SysReq"; break;
							case LibretroApi.RETRO_KEY.BREAK: button = "Break"; break;
							case LibretroApi.RETRO_KEY.MENU: button = "Menu"; break;
							case LibretroApi.RETRO_KEY.POWER: button = "Power"; break;
							case LibretroApi.RETRO_KEY.EURO: button = "Euro"; break;
							case LibretroApi.RETRO_KEY.UNDO: button = "Undo"; break;
						}

						return (short)(_controller.IsPressed("Key " + button) ? 1 : 0);
					}

				case LibretroApi.RETRO_DEVICE.JOYPAD:
					{
						//The JOYPAD is sometimes called RetroPad (and we'll call it that in user-facing stuff cos retroarch does)
						//It is essentially a Super Nintendo controller, but with additional L2/R2/L3/R3 buttons, similar to a PS1 DualShock.
					
						string button = "";
						switch ((LibretroApi.RETRO_DEVICE_ID_JOYPAD)id)
						{
							case LibretroApi.RETRO_DEVICE_ID_JOYPAD.A: button = "A"; break;
							case LibretroApi.RETRO_DEVICE_ID_JOYPAD.B: button = "B"; break;
							case LibretroApi.RETRO_DEVICE_ID_JOYPAD.X: button = "X"; break;
							case LibretroApi.RETRO_DEVICE_ID_JOYPAD.Y: button = "Y"; break;
							case LibretroApi.RETRO_DEVICE_ID_JOYPAD.UP: button = "Up"; break;
							case LibretroApi.RETRO_DEVICE_ID_JOYPAD.DOWN: button = "Down"; break;
							case LibretroApi.RETRO_DEVICE_ID_JOYPAD.LEFT: button = "Left"; break;
							case LibretroApi.RETRO_DEVICE_ID_JOYPAD.RIGHT: button = "Right"; break;
							case LibretroApi.RETRO_DEVICE_ID_JOYPAD.L: button = "L"; break;
							case LibretroApi.RETRO_DEVICE_ID_JOYPAD.R: button = "R"; break;
							case LibretroApi.RETRO_DEVICE_ID_JOYPAD.SELECT: button = "Select"; break;
							case LibretroApi.RETRO_DEVICE_ID_JOYPAD.START: button = "Start"; break;
						}

						return (short)(GetButton(port+1, "RetroPad", button) ? 1 : 0);
					}
				default:
					return 0;
			}
		}

		private bool GetButton(uint pnum, string type, string button)
		{
			string key = $"P{pnum} {type} {button}";
			bool b = _controller.IsPressed(key);
			if (b == true)
			{
				return true; //debugging placeholder
			}
			else return false;
		}

	}
}
