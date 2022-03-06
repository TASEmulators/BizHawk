namespace BizHawk.Client.Common
{
	public static class DistinctKeyNameOverrides
	{
		public static string GetName(in DistinctKey k)
			=> k switch
			{
				DistinctKey.Back => "Backspace",
				DistinctKey.Enter => "Enter",
				DistinctKey.CapsLock => "CapsLock",
				DistinctKey.PageDown => "PageDown",
				DistinctKey.D0 => "Number0",
				DistinctKey.D1 => "Number1",
				DistinctKey.D2 => "Number2",
				DistinctKey.D3 => "Number3",
				DistinctKey.D4 => "Number4",
				DistinctKey.D5 => "Number5",
				DistinctKey.D6 => "Number6",
				DistinctKey.D7 => "Number7",
				DistinctKey.D8 => "Number8",
				DistinctKey.D9 => "Number9",
				DistinctKey.LWin => "LeftWin",
				DistinctKey.RWin => "RightWin",
				DistinctKey.NumPad0 => "Keypad0",
				DistinctKey.NumPad1 => "Keypad1",
				DistinctKey.NumPad2 => "Keypad2",
				DistinctKey.NumPad3 => "Keypad3",
				DistinctKey.NumPad4 => "Keypad4",
				DistinctKey.NumPad5 => "Keypad5",
				DistinctKey.NumPad6 => "Keypad6",
				DistinctKey.NumPad7 => "Keypad7",
				DistinctKey.NumPad8 => "Keypad8",
				DistinctKey.NumPad9 => "Keypad9",
				DistinctKey.Multiply => "KeypadMultiply",
				DistinctKey.Add => "KeypadAdd",
				DistinctKey.Separator => "KeypadComma",
				DistinctKey.Subtract => "KeypadSubtract",
				DistinctKey.Decimal => "KeypadDecimal",
				DistinctKey.Divide => "KeypadDivide",
				DistinctKey.Scroll => "ScrollLock",
				DistinctKey.OemSemicolon => "Semicolon",
				DistinctKey.OemPlus => "Equals",
				DistinctKey.OemComma => "Comma",
				DistinctKey.OemMinus => "Minus",
				DistinctKey.OemPeriod => "Period",
				DistinctKey.OemQuestion => "Slash",
				DistinctKey.OemTilde => "Backtick",
				DistinctKey.OemOpenBrackets => "LeftBracket",
				DistinctKey.OemPipe => "Backslash",
				DistinctKey.OemCloseBrackets => "RightBracket",
				DistinctKey.OemQuotes => "Apostrophe",
				DistinctKey.OemBackslash => "OEM102",
				DistinctKey.NumPadEnter => "KeypadEnter",
				_ => k.ToString()
			};
	}
}